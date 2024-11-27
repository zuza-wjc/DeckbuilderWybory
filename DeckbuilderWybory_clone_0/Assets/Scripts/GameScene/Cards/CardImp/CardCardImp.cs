using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using Unity.Mathematics;
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
    public TurnController turnController;

    private System.Random random = new System.Random();

    void Start()
    {
        playerListManager.Initialize(lobbyId, playerId);
    }


    public async void CardLibrary(string instanceId, string cardIdDropped, bool ignoreCost)
    {
        bool tmp = await cardUtilities.CheckCardLimit(playerId);

        if (tmp)
        {
            Debug.Log("Limit kart w turze to 1");
            return;
        }
        DatabaseReference dbRefCard, dbRefPlayerStats, dbRefPlayerDeck;
            int cost, playerBudget, chosenRegion = -1;
            string cardType, enemyId = string.Empty, source = string.Empty, target = string.Empty;
            bool supportChange = false, isBonusRegion = false, cardsChange = false, onHandChanged = false;

            Dictionary<int, OptionDataCard> cardsOptionsDictionary = new();
            Dictionary<int, OptionDataCard> cardsBonusOptionsDictionary = new();
            Dictionary<int, OptionData> supportOptionsDictionary = new();
            Dictionary<int, OptionData> supportBonusOptionsDictionary = new();

            cardsOptionsDictionary.Clear();
            cardsBonusOptionsDictionary.Clear();
            supportBonusOptionsDictionary.Clear();
            supportOptionsDictionary.Clear();

            List<KeyValuePair<string, string>> selectedCardIds = new();
            selectedCardIds.Clear();

            if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
            {
                Debug.LogError("Firebase is not initialized properly!");
                return;
            }

            dbRefCard = FirebaseInitializer.DatabaseReference.Child("cards").Child("id").Child("cards").Child(cardIdDropped);
            DataSnapshot snapshot = await dbRefCard.GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError("No data for: " + cardIdDropped + ".");
                return;
            }

            cost = snapshot.Child("cost").Exists ? Convert.ToInt32(snapshot.Child("cost").Value) : throw new Exception("Branch cost does not exist.");

        if (DataTransfer.IsFirstCardInTurn)
        {
            if (await cardUtilities.CheckIncreaseCost(playerId))
            {
                double increasedCost = 1.5 * cost;

                if (cost >= 0)
                {
                    if (cost % 2 != 0)
                    {
                        cost = (int)Math.Ceiling(increasedCost);
                    }
                    else
                    {
                        cost = (int)increasedCost;
                    }
                }
                else
                {
                    Debug.LogWarning("Cost is negative, not increasing cost.");
                }
            }
        }

        if (await cardUtilities.CheckIncreaseCostAllTurn(playerId))
        {
            double increasedCost = 1.5 * cost;

            if (cost >= 0)
            {
                if (cost % 2 != 0)
                {
                    cost = (int)Math.Ceiling(increasedCost);
                }
                else
                {
                    cost = (int)increasedCost;
                }
            }
            else
            {
                Debug.LogWarning("Cost is negative, not increasing cost.");
            }
        }

        if (await cardUtilities.CheckDecreaseCost(playerId))
        {
            double decreasedCost = 0.5 * cost;

            if (cost >= 0)
            {
                if (cost % 2 != 0)
                {
                    cost = (int)Math.Floor(decreasedCost);
                }
                else
                {
                    cost = (int)decreasedCost;
                }
            }
            else
            {
                Debug.LogWarning("Cost is negative, not decreasing cost.");
            }
        }
        cardType = snapshot.Child("type").Exists ? snapshot.Child("type").Value.ToString() : throw new Exception("Branch type does not exist.");

            cardsChange = snapshot.Child("cardsOnHand").Exists;
            if (cardsChange)
            {
                cardUtilities.ProcessBonusOptionsCard(snapshot.Child("cardsOnHand"), cardsBonusOptionsDictionary);
                cardUtilities.ProcessOptionsCard(snapshot.Child("cardsOnHand"), cardsOptionsDictionary);
            }

            supportChange = snapshot.Child("support").Exists;
            if (supportChange)
            {
                if (await cardUtilities.CheckSupportBlock(playerId))
                {
                    Debug.Log("support block");
                    return;
                }

                cardUtilities.ProcessBonusOptions(snapshot.Child("support"), supportBonusOptionsDictionary);
                cardUtilities.ProcessOptions(snapshot.Child("support"), supportOptionsDictionary);
            }

            dbRefPlayerStats = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("stats");
            DataSnapshot playerStatsSnapshot = await dbRefPlayerStats.GetValueAsync();

            if (!playerStatsSnapshot.Exists)
            {
                Debug.LogError("No data for: " + cardIdDropped + ".");
                return;
            }

            playerBudget = playerStatsSnapshot.Child("money").Exists ? Convert.ToInt32(playerStatsSnapshot.Child("money").Value) : throw new Exception("Branch money does not exist.");

            if (!ignoreCost && playerBudget < cost)
            {
                Debug.LogError("Brak bud¿etu aby zagraæ kartê.");
                return;
            }

        ignoreCost = await cardUtilities.CheckIgnoreCost(playerId);

        if (!(await cardUtilities.CheckBlockedCard(playerId)))
        {

            if (supportChange)
            {
                isBonusRegion = await SupportAction(cardIdDropped, isBonusRegion, chosenRegion, cardType, supportOptionsDictionary, supportBonusOptionsDictionary);
            }

            if (cardsChange)
            {
                (dbRefPlayerStats, playerBudget) = await CardsAction(instanceId, dbRefPlayerStats, cardIdDropped, isBonusRegion, cardsOptionsDictionary, cardsBonusOptionsDictionary, enemyId, playerBudget, source, target, selectedCardIds);
            }
        }

            if (!ignoreCost)
            {
                await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget - cost);
            }

            dbRefPlayerDeck = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck").Child(instanceId);

            if (!onHandChanged)
            {
                await dbRefPlayerDeck.Child("onHand").SetValueAsync(false);
            }
            await dbRefPlayerDeck.Child("played").SetValueAsync(true);

        DataTransfer.IsFirstCardInTurn = false;
        await cardUtilities.CheckIfPlayed2Cards(playerId);
        tmp = await cardUtilities.CheckCardLimit(playerId);
    }

    private async Task<bool> SupportAction(string cardId, bool isBonusRegion, int chosenRegion, string cardType,
    Dictionary<int, OptionData> supportOptionsDictionary, Dictionary<int, OptionData> supportBonusOptionsDictionary)
    {
        if (cardId == "CA085")
        {
            chosenRegion = await mapManager.SelectArea();
            isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
        }

        var optionsToApply = isBonusRegion ? supportBonusOptionsDictionary : supportOptionsDictionary;

        if (optionsToApply?.Values?.Any() != true)
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
                    if (await CheckIfAnyEnemyProtected())
                    {
                        Debug.Log("Gracz jest chroniony nie mo¿na zagraæ karty");
                        return (dbRefPlayerStats, -1);
                    }
                    else
                    {
                        await deckController.ExchangeCards(playerId, instanceId);
                    }
                } else
                {
                    selectedCardIds = await cardSelectionUI.ShowCardSelection(playerId, data.CardNumber, instanceId, true);
                    Debug.Log($"Wybrane karty: {string.Join(", ", selectedCardIds.Select(card => card.Key))}");
                }

            } else if (data.Target == "player")
            {
                 if (cardId == "CA073")
                {
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("Failed to select an enemy player.");
                        return (dbRefPlayerStats, -1);
                    }
                    if (await cardUtilities.CheckIfProtected(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony nie mo¿na zagraæ karty");
                        return (dbRefPlayerStats, -1);
                    } else if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony nie mo¿na zagraæ karty");
                        return (dbRefPlayerStats, -1);
                    }
                    else
                    {
                        string playerCard = await RandomCardFromDeck(playerId);
                        string enemyCard = await RandomCardFromDeck(enemyId);
                        string keepCard, destroyCard;
                        (keepCard, destroyCard) = await cardSelectionUI.ShowCardSelectionForPlayerAndEnemy(playerId, playerCard, enemyId, enemyCard);
                        Debug.Log($"Selected card: {keepCard}, Card to destroy: {destroyCard}");
                        if (destroyCard == playerCard)
                        {
                            await deckController.RejectCard(playerId, destroyCard);
                            await AddCardToDeck(keepCard, enemyId);
                        }
                        await deckController.RejectCard(enemyId, enemyCard);
                    }

                } else if (cardId == "CA017")
                {
                    int budgetValue = await ValueAsCost();
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("Failed to select an enemy player.");
                        return (dbRefPlayerStats, -1);
                    }
                    playerBudget = await cardUtilities.ChangeEnemyStat(enemyId, -budgetValue, "money", playerBudget);
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
                        if (await cardUtilities.CheckIfProtected(enemyId, -1))
                        {
                            Debug.Log("Gracz jest chroniony nie mo¿na zagraæ karty");
                            return(dbRefPlayerStats,-1);
                        } else if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                        {
                            Debug.Log("Gracz jest chroniony nie mo¿na zagraæ karty");
                            return (dbRefPlayerStats, -1);
                        }
                        else
                        {
                            await deckController.GetCardFromHand(playerId, enemyId, selectedCardIds);
                            Debug.Log("Now choosing from enemy hand");
                            selectedCardIds = await cardSelectionUI.ShowCardSelection(enemyId, data.CardNumber, instanceId, true, selectedCardIds);
                            await deckController.GetCardFromHand(enemyId, playerId, selectedCardIds);
                        }
                    } else
                    {
                        for (int i = 0; i < data.CardNumber; i++)
                        {
                            await deckController.GetCardFromDeck(source, target);
                        }

                        if(cardId == "CA030")
                        {
                            turnController.PassTurn();
                        }

                    }
                }
            } else if (data.Target == "player-deck") {

                if (cardId == "CA030")
                {
                    if(DataTransfer.IsFirstCardInTurn)
                    {
                        int cardsOnHand = await cardUtilities.CountCardsOnHand(playerId);
                        for (int i = 0; i < cardsOnHand-1; i++)
                        {
                            await deckController.RejectRandomCard(playerId,instanceId);
                        }

                    } else
                    {
                        Debug.Log("Karta ta mo¿e byæ zagrana tylko jako pierwsza w turze");
                        return(dbRefPlayerStats, -1);
                    }
                }
                else
                {
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

            }
            else if (data.Target == "enemy-chosen")
            {
                if(cardId == "CA066")
                {
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("Failed to select an enemy player.");
                        return (dbRefPlayerStats, -1);
                    }
                    await cardSelectionUI.ShowCardsForViewing(enemyId);
                }
                else if (cardId == "CA068")
                {
                    await deckController.GetRandomCardsFromDeck(enemyId, data.CardNumber, selectedCardIds);
                } else
                {
                    selectedCardIds = await cardSelectionUI.ShowCardSelection(playerId, data.CardNumber, instanceId, true);
                    Debug.Log($"Wybrane karty: {string.Join(", ", selectedCardIds.Select(card => card.Key))}");
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("Failed to select an enemy player.");
                        return (dbRefPlayerStats, -1);
                    }
                    if(await cardUtilities.CheckIfProtected(enemyId,-1))
                    {
                        Debug.Log("Gracz jest chroniony nie mo¿na zagraæ karty");
                        return (dbRefPlayerStats, -1);
                    } else if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony nie mo¿na zagraæ karty");
                        return (dbRefPlayerStats, -1);
                    }
                    else
                    {
                        target = enemyId;
                        if (data.Source == "player") { source = playerId; }
                        await deckController.GetCardFromHand(source, target, selectedCardIds);
                    }
                }

            }
            else if (data.Target == "enemy-deck")
            {
                enemyId = await playerListManager.SelectEnemyPlayer();
                if (string.IsNullOrEmpty(enemyId))
                {
                    Debug.LogError("Failed to select an enemy player.");
                    return (dbRefPlayerStats, -1);
                }
                if (await cardUtilities.CheckIfProtected(enemyId, -1))
                {
                    Debug.Log("Gracz jest chroniony nie mo¿na zagraæ karty");
                    return (dbRefPlayerStats, -1);
                }
                else if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                {
                    Debug.Log("Gracz jest chroniony nie mo¿na zagraæ karty");
                    return (dbRefPlayerStats, -1);
                }
                else
                {
                    selectedCardIds = await cardSelectionUI.ShowCardSelection(enemyId, data.CardNumber, instanceId, true);
                    await deckController.ReturnCardToDeck(enemyId, selectedCardIds[0].Key);
                }
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

        List<KeyValuePair<string, string>> availableCards = snapshot.Children
            .Where(cardSnapshot =>
                bool.TryParse(cardSnapshot.Child("onHand").Value?.ToString(), out bool onHand) && !onHand &&
                bool.TryParse(cardSnapshot.Child("played").Value?.ToString(), out bool played) && !played &&
                !string.IsNullOrEmpty(cardSnapshot.Child("cardId").Value?.ToString()))
            .Select(cardSnapshot => new KeyValuePair<string, string>(cardSnapshot.Key, cardSnapshot.Child("cardId").Value.ToString()))
            .ToList();

        if (!availableCards.Any())
        {
            Debug.LogWarning("No available cards to draw.");
            return -1;
        }

        int randomIndex = random.Next(availableCards.Count);
        var selectedCard = availableCards[randomIndex];

        string cardLetters = selectedCard.Value.Substring(0, 2);
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
            Debug.LogError($"Unknown card type for card ID: {selectedCard.Value}");
            return -1;
        }

        DatabaseReference dbRefCard = FirebaseInitializer.DatabaseReference
            .Child("cards")
            .Child("id")
            .Child(type)
            .Child(selectedCard.Value)
            .Child("cost");

        var costSnapshot = await dbRefCard.GetValueAsync();
        if (!costSnapshot.Exists || !int.TryParse(costSnapshot.Value.ToString(), out int cardCost))
        {
            Debug.LogError($"Failed to retrieve or parse cost for card {selectedCard.Value}.");
            return -1;
        }

        return cardCost;
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

        var enemyIds = playersSnapshot.Children
            .Where(playerSnapshot => playerSnapshot.Key != playerId)
            .Select(playerSnapshot => playerSnapshot.Key)
            .ToList();

        if (!enemyIds.Any())
        {
            Debug.LogWarning("No enemies available in the session.");
            return null;
        }

        int randomIndex = random.Next(enemyIds.Count);
        string randomEnemyId = enemyIds[randomIndex];
        Debug.Log($"Random enemy selected: {randomEnemyId}");
        return randomEnemyId;
    }

    private async Task<string> RandomCardFromDeck(string playerId)
    {
        if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("Invalid player ID or Lobby ID.");
            return null;
        }

        DatabaseReference playerDeckRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("deck");

        var playerDeckSnapshot = await playerDeckRef.GetValueAsync();
        if (!playerDeckSnapshot.Exists)
        {
            Debug.LogError($"Deck not found for player {playerId} in lobby {lobbyId}.");
            return null;
        }

        var eligibleCards = playerDeckSnapshot.Children
            .Where(cardSnapshot =>
                bool.TryParse(cardSnapshot.Child("onHand").Value?.ToString(), out bool isOnHand) && !isOnHand &&
                bool.TryParse(cardSnapshot.Child("played").Value?.ToString(), out bool isPlayed) && !isPlayed)
            .Select(cardSnapshot => cardSnapshot.Key)
            .ToList();

        if (!eligibleCards.Any())
        {
            Debug.LogWarning("No eligible cards found in deck.");
            return null;
        }

        int randomIndex = random.Next(eligibleCards.Count);
        return eligibleCards[randomIndex];
    }

    private async Task AddCardToDeck(string instanceId, string enemyId)
    {
        if (string.IsNullOrEmpty(instanceId) || string.IsNullOrEmpty(enemyId)) return;

        string lobbyId = DataTransfer.LobbyId;
        DatabaseReference enemyCardRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId)
            .Child("deck")
            .Child(instanceId);

        var enemyCardSnapshot = await enemyCardRef.GetValueAsync();
        if (!enemyCardSnapshot.Exists)
        {
            Debug.LogWarning($"Card with instanceId {instanceId} not found in enemy's deck.");
            return;
        }

        DatabaseReference playerDeckRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("deck")
            .Child(instanceId);

        await playerDeckRef.SetValueAsync(enemyCardSnapshot.Value);
        await playerDeckRef.Child("played").SetValueAsync(false);
    }

    public async Task<bool> CheckIfAnyEnemyProtected()
    {
        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("Lobby ID is null or empty.");
            return false;
        }

        DatabaseReference playersRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        var snapshot = await playersRef.GetValueAsync();
        if (!snapshot.Exists)
        {
            Debug.LogError($"No players found in lobby {lobbyId}.");
            return false;
        }

        foreach (var playerSnapshot in snapshot.Children)
        {
            string enemyId = playerSnapshot.Key;
            if (enemyId == playerId) continue;

            bool isProtected = await cardUtilities.CheckIfProtected(enemyId, -1);
            if (isProtected)
            {
                Debug.Log($"Enemy {enemyId} is protected. Action cannot proceed.");
                return true;
            }

            bool isProtectedOneCard = await cardUtilities.CheckIfProtectedOneCard(enemyId, -1);
            if (isProtectedOneCard)
            {
                Debug.Log($"Enemy {enemyId} is protected by one card. Action cannot proceed.");
                return true;
            }
        }

        return false;
    }

}


