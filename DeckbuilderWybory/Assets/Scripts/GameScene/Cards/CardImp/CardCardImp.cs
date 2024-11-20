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

    private DatabaseReference dbRefCard;
    private DatabaseReference dbRefPlayerStats;
    private DatabaseReference dbRefPlayerDeck;

    private int cost;
    private string cardType;
    private int playerBudget;
    private int chosenRegion;
    private string enemyId;
    private string source;
    private string target;

    List<string> selectedCardIds = new();

    private bool supportChange;
    private bool isBonusRegion;
    private bool cardsChange;
    private bool onHandChanged;

    private Dictionary<int, OptionDataCard> cardsOptionsDictionary = new();
    private Dictionary<int, OptionDataCard> cardsBonusOptionsDictionary = new();
    private Dictionary<int, OptionData> supportOptionsDictionary = new();
    private Dictionary<int, OptionData> supportBonusOptionsDictionary = new();

    void Start()
    {
        playerListManager.Initialize(lobbyId, playerId);
    }


    public async void CardLibrary(string cardIdDropped, bool ignoreCost)
    {
        cost = -1;
        cardType = string.Empty;
        playerBudget = -1;
        chosenRegion = -1;
        enemyId = string.Empty;
        source = string.Empty;
        target = string.Empty ;

        supportChange = false;
        isBonusRegion = false;
        cardsChange = false;
        onHandChanged = false;

        cardsOptionsDictionary.Clear();
        cardsBonusOptionsDictionary.Clear();

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
                    if (playerBudget < cost)
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
                await SupportAction(cardIdDropped);
            }

            if (cardsChange)
            {
    
                await CardsAction(cardIdDropped);
            }

            if(!ignoreCost)
            {
                await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget - cost);
            }

            dbRefPlayerDeck = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck").Child(cardIdDropped);

            if(!onHandChanged)
            {
                await dbRefPlayerDeck.Child("onHand").SetValueAsync(false);
            }
            await dbRefPlayerDeck.Child("played").SetValueAsync(true);
        }


    }
   
    private async Task SupportAction(string cardId)
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
            return;
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
        }

    private async Task CardsAction(string cardId)
    {
        var isBonus = isBonusRegion;
        var optionsToApply = isBonus ? cardsBonusOptionsDictionary : cardsOptionsDictionary;

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No options to apply.");
            return;
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
                    await deckController.ExchangeCards(playerId,cardId);
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
                        return;
                    }
                    await cardUtilities.ChangeEnemyStat(enemyId, -budgetValue, "money", playerBudget);
                } else
                {
                    if (data.Source == "player-deck") { source = playerId; }
                    if (data.Target == "player") { target = playerId; }

                   if(cardId == "CA070")
                    {
                        string cardFromHand = selectedCardIds[0];
                        selectedCardIds.Clear();
                        selectedCardIds = await cardSelectionUI.ShowCardSelection(playerId, data.CardNumber, cardId, false);
                        Debug.Log($"Wybrane karty: {string.Join(", ", selectedCardIds)}");
                        string cardFromDeck = selectedCardIds[0];
                        await deckController.ExchangeFromHandToDeck(source,cardFromHand,cardFromDeck);

                    } else if (cardId == "CA077")
                    {
                        selectedCardIds = await cardSelectionUI.ShowCardSelection(playerId, data.CardNumber, cardId, false);
                        Debug.Log($"Wybrane karty: {string.Join(", ", selectedCardIds)}");

                        cardTypeManager.OnCardDropped(selectedCardIds[0],true);
                    }
                    else
                    {
                        for (int i = 0; i < data.CardNumber; i++)
                        {
                            await deckController.GetCardFromDeck(source, target);
                        }

                        // obs³uga karty ca033
                    }
                }
            } else if (data.Target == "player-deck") {

                selectedCardIds = await cardSelectionUI.ShowCardSelection(playerId, data.CardNumber, cardId, true);
                Debug.Log($"Wybrane karty: {string.Join(", ", selectedCardIds)}");
                if (data.Source == "player") { source = playerId; }

                if(cardId == "CA031")
                {
                    foreach (string selectedCardId in selectedCardIds)
                    {
                        await deckController.RejectCard(source, selectedCardId);
                    }
                }
            } else if (data.Target == "enemy-chosen")
            {
                selectedCardIds = await cardSelectionUI.ShowCardSelection(playerId, data.CardNumber, cardId, true);
                Debug.Log($"Wybrane karty: {string.Join(", ", selectedCardIds)}");
                enemyId = await playerListManager.SelectEnemyPlayer();
                if (string.IsNullOrEmpty(enemyId))
                {
                    Debug.LogError("Failed to select an enemy player.");
                    return;
                }
                target = enemyId;
                if(data.Source == "player") { source = playerId; }
                for (int i = 0; i < data.CardNumber; i++)
                {
                    await deckController.GetCardFromHand(source, target, selectedCardIds);
                }
            }
        }
        }

    private async Task<int> ValueAsCost()
    {
        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("Lobby ID is null or empty.");
            return -1;
        }

        dbRefPlayerDeck = FirebaseInitializer.DatabaseReference
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

        List<string> availableCards = new();
        foreach (var cardSnapshot in snapshot.Children)
        {
            var onHandSnapshot = cardSnapshot.Child("onHand");
            var playedSnapshot = cardSnapshot.Child("played");

            if (onHandSnapshot.Exists && bool.TryParse(onHandSnapshot.Value.ToString(), out bool onHand) && !onHand)
            {
                if (playedSnapshot.Exists && bool.TryParse(playedSnapshot.Value.ToString(), out bool played) && !played)
                {
                    availableCards.Add(cardSnapshot.Key);
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
        string selectedCardId = availableCards[randomIndex];

        string cardLetters = selectedCardId.Substring(0, 2);
        string type = "";

        switch (cardLetters)
        {
            case "AD":
                type = "addRemove";
                break;
            case "AS":
                type = "asMuchAs";
                break;
            case "CA":
                type = "cards";
                break;
            case "OP":
                type = "options";
                break;
            case "RA":
                type = "random";
                break;
            case "UN":
                type = "unique";
                break;
        }

        dbRefCard = FirebaseInitializer.DatabaseReference.Child("cards").Child("id").Child(type).Child(selectedCardId).Child("cost");

        var costSnapshot = await dbRefCard.GetValueAsync();

        if (!costSnapshot.Exists)
        {
            Debug.LogError($"Cost for card {selectedCardId} not found.");
            return -1;
        }

        int cardCost = Convert.ToInt32(costSnapshot.Value);

        return cardCost;
    }

    public async Task PlayCardFromDeck(List<string> selectedCardIds, string playerId)
    {
        if (selectedCardIds == null || selectedCardIds.Count == 0)
        {
            Debug.LogError("Brak wybranych kart.");
            return;
        }

        string selectedCardId = selectedCardIds[0];
        string cardLetters = selectedCardId.Substring(0, 2);
        string type = "";

        switch (cardLetters)
        {
            case "AD":
                type = "addRemove";
                break;
            case "AS":
                type = "asMuchAs";
                break;
            case "CA":
                type = "cards";
                break;
            case "OP":
                type = "options";
                break;
            case "RA":
                type = "random";
                break;
            case "UN":
                type = "unique";
                break;
        } 

        DatabaseReference cardCostRef = FirebaseInitializer.DatabaseReference
            .Child("cards")
            .Child("id")
            .Child(type)
            .Child(selectedCardId)
            .Child("cost");

        try
        {
            await cardCostRef.SetValueAsync(0);
            cardTypeManager.OnCardDropped(selectedCardId, true);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Wyst¹pi³ b³¹d podczas zmiany kosztu karty: {ex.Message}");
        }
    }

}


