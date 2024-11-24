using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using UnityEngine;

public class CardCardImp : MonoBehaviour
{
    private readonly string lobbyId = DataTransfer.LobbyId;
    private readonly string playerId = DataTransfer.PlayerId;

    public PlayerListManager playerListManager;
    public CardUtilities cardUtilities;
    public MapManager mapManager;
    public DeckController deckController;
    public CardSelectionUI cardSelectionUI;
    public CardTypeManager cardTypeManager;

    void Start()
    {
        playerListManager.Initialize(lobbyId, playerId);
    }


    public async void CardLibrary(string instanceId, string cardIdDropped, bool ignoreCost)
    {
        DatabaseReference dbRefCard;
        DatabaseReference dbRefPlayerStats;
        DatabaseReference dbRefPlayerDeck;

        int cost;
        string cardType;
        int playerBudget;
        int chosenRegion = -1;
        string enemyId = string.Empty;
        string source = string.Empty;
        string target = string.Empty;

        List<KeyValuePair<string, string>> selectedCardIds = new List<KeyValuePair<string, string>>();


        bool supportChange = false;
        bool isBonusRegion = false;
        bool cardsChange = false;
        bool onHandChanged = false;

        Dictionary<int, OptionDataCard> cardsOptionsDictionary = new();
        Dictionary<int, OptionDataCard> cardsBonusOptionsDictionary = new();
        Dictionary<int, OptionData> supportOptionsDictionary = new();
        Dictionary<int, OptionData> supportBonusOptionsDictionary = new();

        cardsOptionsDictionary.Clear();
        cardsBonusOptionsDictionary.Clear();
        supportBonusOptionsDictionary.Clear();
        supportOptionsDictionary.Clear();

        selectedCardIds.Clear();

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRefCard = FirebaseInitializer.DatabaseReference.Child("cards").Child("id").Child("cards").Child(cardIdDropped);

        DataSnapshot snapshot = await dbRefCard.GetValueAsync();

        if (snapshot.Exists)
        {

            DataSnapshot costSnapshot = snapshot.Child("cost");
            if (costSnapshot.Exists)
            {
                cost = Convert.ToInt32(costSnapshot.Value);
            }
            else
            {
                Debug.LogError("Branch cost does not exist.");
                return;
            }

            DataSnapshot typeSnapshot = snapshot.Child("type");
            if (typeSnapshot.Exists)
            {
                cardType = typeSnapshot.Value.ToString();
            }
            else
            {
                Debug.LogError("Branch type does not exist.");
                return;
            }

            DataSnapshot cardsSnapshot = snapshot.Child("cardsOnHand");
            if (cardsSnapshot.Exists)
            {

                cardsChange = true;

                cardUtilities.ProcessBonusOptionsCard(cardsSnapshot, cardsBonusOptionsDictionary);
                cardUtilities.ProcessOptionsCard(cardsSnapshot, cardsOptionsDictionary);

            }

            DataSnapshot supportSnapshot = snapshot.Child("support");
            if (supportSnapshot.Exists)
            {

                supportChange = true;

                cardUtilities.ProcessBonusOptions(supportSnapshot, supportBonusOptionsDictionary);
                cardUtilities.ProcessOptions(supportSnapshot, supportOptionsDictionary);

            }

            dbRefPlayerStats = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("stats");

            DataSnapshot playerStatsSnapshot = await dbRefPlayerStats.GetValueAsync();

            if (playerStatsSnapshot.Exists)
            {
                DataSnapshot moneySnapshot = playerStatsSnapshot.Child("money");
                if (moneySnapshot.Exists)
                {
                    playerBudget = Convert.ToInt32(moneySnapshot.Value);
                    if (!ignoreCost && playerBudget < cost)
                    {
                        Debug.LogError("Brak bud¿etu aby zagraæ kartê.");
                        return;
                    }
                }
                else
                {
                    Debug.LogError("Branch money does not exist.");
                    return;
                }
            }
            else
            {
                Debug.LogError("No data for: " + cardIdDropped + ".");
                return;
            }

            if (supportChange)
            {
                isBonusRegion = await SupportAction(cardIdDropped,isBonusRegion,chosenRegion,cardType,supportOptionsDictionary,supportBonusOptionsDictionary);
            }

            if (cardsChange)
            {
    
                (dbRefPlayerStats,playerBudget) = await CardsAction(instanceId,dbRefPlayerStats,cardIdDropped,isBonusRegion,cardsOptionsDictionary,cardsBonusOptionsDictionary,enemyId,playerBudget,source,target,
                    selectedCardIds);
            }

            if(!ignoreCost)
            {
                await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget - cost);
            }

            dbRefPlayerDeck = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck").Child(instanceId);

            if(!onHandChanged)
            {
                await dbRefPlayerDeck.Child("onHand").SetValueAsync(false);
            }
            await dbRefPlayerDeck.Child("played").SetValueAsync(true);
        }


    }
   
    private async Task<bool> SupportAction(string cardId, bool isBonusRegion, int chosenRegion,string cardType,Dictionary<int, OptionData> supportOptionsDictionary, Dictionary<int, OptionData> supportBonusOptionsDictionary)
    {
        if (cardId == "CA085")
        {
            chosenRegion = await mapManager.SelectArea();
            isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
        }

        var isBonus = isBonusRegion;
        var optionsToApply = isBonus ? supportBonusOptionsDictionary : supportOptionsDictionary;

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No support options available.");
            return false;
        }

        foreach (var data in optionsToApply.Values)
        {
            if (data.Target == "player-region")
            {
                if (chosenRegion < 0)
                {
                    chosenRegion = await mapManager.SelectArea();
                }
                await cardUtilities.ChangeSupport(playerId, data.Number, chosenRegion, cardId, mapManager);
                isBonusRegion = false;
            }
        }
        return isBonusRegion;
     }

    private async Task<(DatabaseReference dbRefPlayerStats, int playerBudget)> CardsAction(string instanceId,DatabaseReference dbRefPlayerStats,string cardId, bool isBonusRegion,
        Dictionary<int, OptionDataCard> cardsOptionsDictionary,Dictionary<int, OptionDataCard> cardsBonusOptionsDictionary,string enemyId, int playerBudget,string source,string target,
        List<KeyValuePair<string, string>> selectedCardIds)
    {
        var isBonus = isBonusRegion;
        var optionsToApply = isBonus ? cardsBonusOptionsDictionary : cardsOptionsDictionary;

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No options to apply.");
            return (dbRefPlayerStats,-1);
        }

        if (isBonus)
        {
            Debug.Log("Bonus region detected.");
        }


        foreach (var data in optionsToApply.Values)
        {
            if (data.Target == "enemy-random")
            {
                if(data.TargetNumber == 8)
                {
                    await deckController.ExchangeCards(playerId,instanceId);
                } else
                {
                    selectedCardIds = await cardSelectionUI.ShowCardSelection(playerId, data.CardNumber, instanceId, true);
                    Debug.Log($"Wybrane karty: {string.Join(", ", selectedCardIds.Select(card => card.Key))}");
                }

            } else if (data.Target == "player")
            {
                if (cardId == "CA017")
                {
                    int budgetValue = await ValueAsCost();
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("Failed to select an enemy player.");
                        return (dbRefPlayerStats, -1);
                    }
                    await cardUtilities.ChangeEnemyStat(enemyId, -budgetValue, "money", playerBudget);
                    await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
                } else
                {
                    if (data.Source == "player-deck") { source = playerId; }
                    if (data.Target == "player") { target = playerId; }

                   if(cardId == "CA070")
                    {
                        string cardFromHandInstanceId = selectedCardIds[0].Key;

                        selectedCardIds.Clear();

                        selectedCardIds = await cardSelectionUI.ShowCardSelection(playerId, data.CardNumber, cardFromHandInstanceId, false);

                        Debug.Log($"Wybrane karty z decku: {string.Join(", ", selectedCardIds.Select(card => card.Key))}");

                        if (selectedCardIds.Count > 0)
                        {
                            string cardFromDeckInstanceId = selectedCardIds[0].Key;
                            await deckController.ExchangeFromHandToDeck(source, cardFromHandInstanceId, cardFromDeckInstanceId);
                        }
                        else
                        {
                            Debug.LogWarning("Nie wybrano ¿adnej karty z decku.");
                        }


                    }
                    else if (cardId == "CA077")
                    {
                        selectedCardIds = await cardSelectionUI.ShowCardSelection(playerId, data.CardNumber, instanceId, false);

                        Debug.Log($"Wybrane karty: {string.Join(", ", selectedCardIds.Select(card => card.Key))}");

                        if (selectedCardIds.Count > 0)
                        {
                            string cardFromDeckInstanceId = selectedCardIds[0].Key;
                            string cardFromDeckCardId = selectedCardIds[0].Value;

                            cardTypeManager.OnCardDropped(cardFromDeckInstanceId, cardFromDeckCardId, true);
                        }
                        else
                        {
                            Debug.LogWarning("Nie wybrano ¿adnej karty.");
                        }

                    }
                    else if (cardId == "CA033")
                    {
                        await deckController.GetRandomCardsFromHand(playerId, enemyId, data.CardNumber,selectedCardIds);

                    } else if (cardId == "CA067")
                    {
                        enemyId = await RandomEnemy();
                        await deckController.GetCardFromHand(playerId, enemyId, selectedCardIds);
                        Debug.Log("Now choosing from enemy hand");
                        selectedCardIds = await cardSelectionUI.ShowCardSelection(enemyId, data.CardNumber, instanceId, true, selectedCardIds);
                        await deckController.GetCardFromHand(enemyId, playerId, selectedCardIds);
                    } else
                    {
                        for (int i = 0; i < data.CardNumber; i++)
                        {
                            await deckController.GetCardFromDeck(source, target);
                        }

                    }
                }
            } else if (data.Target == "player-deck") {

                selectedCardIds = await cardSelectionUI.ShowCardSelection(playerId, data.CardNumber, instanceId, true);

                Debug.Log($"Wybrane karty: {string.Join(", ", selectedCardIds.Select(card => card.Key))}");

                if (data.Source == "player")
                {
                    source = playerId;
                }

                if (cardId == "CA031")
                {
                    foreach (var selectedCard in selectedCardIds)
                    {
                        string selectedInstanceId = selectedCard.Key;

                        await deckController.RejectCard(source, selectedInstanceId);
                    }
                }

            }
            else if (data.Target == "enemy-chosen")
            {
                selectedCardIds = await cardSelectionUI.ShowCardSelection(playerId, data.CardNumber, instanceId, true);
                Debug.Log($"Wybrane karty: {string.Join(", ", selectedCardIds.Select(card => card.Key))}");
                enemyId = await playerListManager.SelectEnemyPlayer();
                if (string.IsNullOrEmpty(enemyId))
                {
                    Debug.LogError("Failed to select an enemy player.");
                    return (dbRefPlayerStats, -1);
                }
                target = enemyId;
                if(data.Source == "player") { source = playerId; }
                await deckController.GetCardFromHand(source, target, selectedCardIds);
            }
        }
        return (dbRefPlayerStats, playerBudget);
     }

    private async Task<int> ValueAsCost()
    {
        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("Lobby ID is null or empty.");
            return -1;
        }

        DatabaseReference dbRefPlayerDeck = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("deck");

        var snapshot = await dbRefPlayerDeck.GetValueAsync();
        if (!snapshot.Exists)
        {
            Debug.LogError($"Player deck not found for player {playerId} in lobby {lobbyId}.");
            return -1;
        }

        List<KeyValuePair<string, string>> availableCards = new();

        foreach (var cardSnapshot in snapshot.Children)
        {
            if (cardSnapshot.Child("onHand").Exists &&
                bool.TryParse(cardSnapshot.Child("onHand").Value.ToString(), out bool onHand) && !onHand &&
                cardSnapshot.Child("played").Exists &&
                bool.TryParse(cardSnapshot.Child("played").Value.ToString(), out bool played) && !played)
            {
                string instanceId = cardSnapshot.Key;
                string cardId = cardSnapshot.Child("cardId").Value?.ToString();
                if (!string.IsNullOrEmpty(cardId))
                {
                    availableCards.Add(new KeyValuePair<string, string>(instanceId, cardId));
                }
            }
        }

        if (availableCards.Count == 0)
        {
            Debug.LogWarning("No available cards to draw.");
            return -1;
        }

        System.Random random = new();
        int randomIndex = random.Next(availableCards.Count);
        var selectedCard = availableCards[randomIndex];
        string selectedCardId = selectedCard.Value;

        string cardLetters = selectedCardId.Substring(0, 2);
        string type = cardLetters switch
        {
            "AD" => "addRemove",
            "AS" => "asMuchAs",
            "CA" => "cards",
            "OP" => "options",
            "RA" => "random",
            "UN" => "unique",
            _ => "unknown"
        };

        if (type == "unknown")
        {
            Debug.LogError($"Unknown card type for card ID: {selectedCardId}");
            return -1;
        }

        DatabaseReference dbRefCard = FirebaseInitializer.DatabaseReference
            .Child("cards")
            .Child("id")
            .Child(type)
            .Child(selectedCardId)
            .Child("cost");

        var costSnapshot = await dbRefCard.GetValueAsync();
        if (!costSnapshot.Exists)
        {
            Debug.LogError($"Cost for card {selectedCardId} not found.");
            return -1;
        }

        if (int.TryParse(costSnapshot.Value.ToString(), out int cardCost))
        {
            return cardCost;
        }
        else
        {
            Debug.LogError($"Failed to parse cost for card {selectedCardId}.");
            return -1;
        }
    }

    private async Task<string> RandomEnemy()
    {

        if (string.IsNullOrEmpty(lobbyId) || string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("Lobby ID or Player ID is null or empty.");
            return null;
        }

        DatabaseReference playersRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        var playersSnapshot = await playersRef.GetValueAsync();
        if (!playersSnapshot.Exists)
        {
            Debug.LogError($"No players found in lobby {lobbyId}.");
            return null;
        }

        List<string> enemyIds = new();
        foreach (var playerSnapshot in playersSnapshot.Children)
        {
            string id = playerSnapshot.Key;
            if (id != playerId)
            {
                enemyIds.Add(id);
            }
        }

        if (enemyIds.Count == 0)
        {
            Debug.LogWarning("No enemies available in the session.");
            return null;
        }

        System.Random random = new();
        int randomIndex = random.Next(enemyIds.Count);
        string randomEnemyId = enemyIds[randomIndex];

        Debug.Log($"Random enemy selected: {randomEnemyId}");
        return randomEnemyId;
    }

}


