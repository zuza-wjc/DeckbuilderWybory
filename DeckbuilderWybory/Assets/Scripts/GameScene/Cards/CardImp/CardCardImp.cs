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
    public ErrorPanelController errorPanelController;

    private System.Random random = new System.Random();

    public HistoryController historyController;

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
            errorPanelController.ShowError("card_limit");
            return;
        }
        DatabaseReference dbRefCard, dbRefPlayerStats, dbRefPlayerDeck;
            int cost, playerBudget, chosenRegion = -1;
            string cardType, enemyId = string.Empty, source = string.Empty, target = string.Empty;
            bool supportChange = false, isBonusRegion = false, cardsChange = false, onHandChanged = false,errorCheck = false;

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
            errorPanelController.ShowError("general_error");
            return;
            }

            dbRefCard = FirebaseInitializer.DatabaseReference.Child("cards").Child("id").Child("cards").Child(cardIdDropped);
            DataSnapshot snapshot = await dbRefCard.GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError("No data for: " + cardIdDropped + ".");
            errorPanelController.ShowError("general_error");
            return;
            }

        cost = snapshot.Child("cost").Exists ? Convert.ToInt32(snapshot.Child("cost").Value) : -1;

        if (cost < -1)
        {
            Debug.LogError("Branch cost does not exist");
            errorPanelController.ShowError("general_error");
            return;
        }

        if (DataTransfer.IsFirstCardInTurn)
        {
            var (isIncreaseCost, increaseCostEntriesCount) = await cardUtilities.CheckIncreaseCost(playerId);
            if (isIncreaseCost)
            {
                double multiplier = 1 + 0.5 * increaseCostEntriesCount;
                double increasedCost = multiplier * cost;
                cost = (cost % 2 != 0) ? (int)Math.Ceiling(increasedCost) : (int)increasedCost;
            }
        }

        var (isIncreaseCostAllTurn, increaseCostAllTurnEntriesCount) = await cardUtilities.CheckIncreaseCostAllTurn(playerId);
        if (isIncreaseCostAllTurn)
        {
            double multiplier = 1 + 0.5 * increaseCostAllTurnEntriesCount;
            double increasedCost = multiplier * cost;
            cost = (cost % 2 != 0) ? (int)Math.Ceiling(increasedCost) : (int)increasedCost;
        }

        var (hasValidEntries, validEntriesCount) = await cardUtilities.CheckDecreaseCost(playerId);

        if (hasValidEntries && validEntriesCount > 0)
        {
            double multiplier = 1.0 / validEntriesCount;
            double decreasedCost = 0.5 * cost * multiplier;

            cost = (int)Math.Round(decreasedCost);
        }

        cardType = snapshot.Child("type").Exists ? snapshot.Child("type").Value.ToString() : string.Empty;

        if (cardType == string.Empty)
        {
            Debug.LogError("Branch type does not exist");
            errorPanelController.ShowError("general_error");
            return;
        }

        string desc = snapshot.Child("playDescriptionPositive").Exists ? snapshot.Child("playDescriptionPositive").Value.ToString() : string.Empty;

        if (desc == string.Empty)
        {
            Debug.LogError("B��d w pobieraniu warto�ci playDescriptionPositive");
            errorPanelController.ShowError("general_error");
            return;
        }

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
                errorPanelController.ShowError("action_blocked");
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
            errorPanelController.ShowError("general_error");
            return;
            }

        playerBudget = playerStatsSnapshot.Child("money").Exists ? Convert.ToInt32(playerStatsSnapshot.Child("money").Value) : -1;

        if (playerBudget < 0)
        {
            Debug.LogError("Błąd w pobieraniu wartości playerBudget");
            errorPanelController.ShowError("general_error");
            return;

        }

        if (!ignoreCost && playerBudget < cost)
        {
            Debug.LogError("Brak budżetu aby zagrać kartę.");
            errorPanelController.ShowError("no_budget");
            return;
        }

        bool ignoreCostCard = await cardUtilities.CheckIgnoreCost(playerId);

        if (!(await cardUtilities.CheckBlockedCard(playerId)))
        {
            if (!ignoreCost && !ignoreCostCard)
            {
                await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget - cost);
                playerBudget -= cost;
            }
            else
            {
                ignoreCost = false;
            }

            if (supportChange)
            {
                (isBonusRegion,errorCheck) = await SupportAction(cardIdDropped, isBonusRegion, chosenRegion, cardType, supportOptionsDictionary, supportBonusOptionsDictionary);
                if (errorCheck)
                {
                    DataSnapshot currentBudgetSnapshot = await dbRefPlayerStats.Child("money").GetValueAsync();
                    if (currentBudgetSnapshot.Exists)
                    {
                        int currentBudget = Convert.ToInt32(currentBudgetSnapshot.Value);
                        int updatedBudget = currentBudget + cost;
                        await dbRefPlayerStats.Child("money").SetValueAsync(updatedBudget);
                    }
                    else
                    {
                        Debug.LogError("Failed to fetch current player budget.");
                    }
                    return;
                }

            }

            if (cardsChange)
            {
                (dbRefPlayerStats, playerBudget, enemyId) = await CardsAction(instanceId, dbRefPlayerStats, cardIdDropped, isBonusRegion, cardsOptionsDictionary, cardsBonusOptionsDictionary, enemyId, playerBudget, source, target, selectedCardIds);

                if (playerBudget == -1)
                {
                    DataSnapshot currentBudgetSnapshot = await dbRefPlayerStats.Child("money").GetValueAsync();
                    if (currentBudgetSnapshot.Exists)
                    {
                        int currentBudget = Convert.ToInt32(currentBudgetSnapshot.Value);
                        int updatedBudget = currentBudget + cost;
                        await dbRefPlayerStats.Child("money").SetValueAsync(updatedBudget);
                    }
                    else
                    {
                        Debug.LogError("Failed to fetch current player budget.");
                    }
                    return;
                }
            }
        } else
        {
            Debug.Log("Karta została zablokowana");
            errorPanelController.ShowError("action_blocked");

            DataSnapshot currentBudgetSnapshot = await dbRefPlayerStats.Child("money").GetValueAsync();
            if (currentBudgetSnapshot.Exists)
            {
                int currentBudget = Convert.ToInt32(currentBudgetSnapshot.Value);
                int updatedBudget = currentBudget + cost;
                await dbRefPlayerStats.Child("money").SetValueAsync(updatedBudget);
            }
            else
            {
                Debug.LogError("Failed to fetch current player budget.");
            }

            DatabaseReference dbRefDeck = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck").Child(instanceId);
            await dbRefDeck.Child("onHand").SetValueAsync(false);
            await dbRefDeck.Child("played").SetValueAsync(true);

            DataTransfer.IsFirstCardInTurn = false;

            await cardUtilities.CheckIfPlayed2Cards(playerId);

            tmp = await cardUtilities.CheckCardLimit(playerId);

            return;
        }

        if (ignoreCost)
        {
            DataSnapshot currentBudgetSnapshot = await dbRefPlayerStats.Child("money").GetValueAsync();
            if (currentBudgetSnapshot.Exists)
            {
                int currentBudget = Convert.ToInt32(currentBudgetSnapshot.Value);
                int updatedBudget = currentBudget + cost;
                await dbRefPlayerStats.Child("money").SetValueAsync(updatedBudget);
            }
            else
            {
                Debug.LogError("Failed to fetch current player budget.");
                return;
            }
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

        await historyController.AddCardToHistory(cardIdDropped, playerId, desc, enemyId);
    }

    private async Task<(bool,bool)> SupportAction(string cardId, bool isBonusRegion, int chosenRegion, string cardType,
    Dictionary<int, OptionData> supportOptionsDictionary, Dictionary<int, OptionData> supportBonusOptionsDictionary)
    {
        if (cardId == "CA085")
        {
            if (!DataTransfer.IsPlayerTurn)
            {
                errorPanelController.ShowError("turn_over");
                return (false, false);
            }
            chosenRegion = await mapManager.SelectArea();
            isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
        }

        var optionsToApply = isBonusRegion ? supportBonusOptionsDictionary : supportOptionsDictionary;

        if (optionsToApply?.Values?.Any() != true)
        {
            Debug.LogError("No support options available.");
            errorPanelController.ShowError("general_error");
            return (false,false);
        }

        foreach (var data in optionsToApply.Values)
        {
            if (data.Target == "player-region")
            {
                if (chosenRegion < 0)
                {
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (false, false);
                    }
                    chosenRegion = await mapManager.SelectArea();
                }
                if (!DataTransfer.IsPlayerTurn)
                {
                    errorPanelController.ShowError("turn_over");
                    return (false, false);
                }
                bool checkError = await cardUtilities.ChangeSupport(playerId, data.Number, chosenRegion, cardId, mapManager);
                if(checkError)
                {
                    return (false,false);
                }
                isBonusRegion = false;
            }
        }
        return (isBonusRegion,false);
    }


    private async Task<(DatabaseReference dbRefPlayerStats, int playerBudget, string)> CardsAction(string instanceId,DatabaseReference dbRefPlayerStats,string cardId, bool isBonusRegion,
        Dictionary<int, OptionDataCard> cardsOptionsDictionary,Dictionary<int, OptionDataCard> cardsBonusOptionsDictionary,string enemyId, int playerBudget,string source,string target,
        List<KeyValuePair<string, string>> selectedCardIds)
    {
        var isBonus = isBonusRegion;
        var optionsToApply = isBonus ? cardsBonusOptionsDictionary : cardsOptionsDictionary;

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No options to apply.");
            errorPanelController.ShowError("general_error");
            return (dbRefPlayerStats,-1, enemyId);
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
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    if (await CheckIfAnyEnemyProtected())
                    {
                        Debug.Log("Gracz jest chroniony nie można zagrać karty");
                        errorPanelController.ShowError("player_protected");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    else
                    {
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        await deckController.ExchangeCards(playerId, instanceId);
                    }
                } else
                {
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    int cardsOnHand = await cardUtilities.CountCardsOnHand(playerId);
                    if (cardsOnHand - 1 < data.CardNumber)
                    {
                        Debug.Log("Za mało kart na ręce aby zagrać kartę");
                        errorPanelController.ShowError("cards_lack");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    else if (cardsOnHand == -1)
                    {
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    selectedCardIds = await cardSelectionUI.ShowCardSelection(playerId, data.CardNumber, instanceId, true);
                }

            } else if (data.Target == "player")
            {
                 if (cardId == "CA073")
                {
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("Failed to select an enemy player.");
                        errorPanelController.ShowError("general_error");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    if (await cardUtilities.CheckIfProtected(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony nie można zagrać karty");
                        errorPanelController.ShowError("player_protected");
                        return (dbRefPlayerStats, -1, enemyId);
                    } else if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                    {
                        Debug.Log("Karta została skontrowana.");
                        errorPanelController.ShowError("counter");
                        return (dbRefPlayerStats, playerBudget, enemyId);
                    }
                    else
                    {
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        string playerCard = await RandomCardFromDeck(playerId);
                        if(playerCard == null)
                        {
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        string enemyCard = await RandomCardFromDeck(enemyId);
                        if(enemyCard == null)
                        {
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        string keepCard, destroyCard;
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        (keepCard, destroyCard) = await cardSelectionUI.ShowCardSelectionForPlayerAndEnemy(playerId, playerCard, enemyId, enemyCard);
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        if (destroyCard == playerCard)
                        {
                            bool checkError = await deckController.RejectCard(playerId, destroyCard);
                            if(checkError)
                            {
                                return (dbRefPlayerStats, -1, enemyId);
                            }
                            checkError = await AddCardToDeck(keepCard, enemyId);
                            if (checkError)
                            {
                                return (dbRefPlayerStats, -1, enemyId);
                            }
                        }
                        bool errorCheck =  await deckController.RejectCard(enemyId, enemyCard);
                        if (errorCheck)
                        {
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                    }

                } else if (cardId == "CA017")
                {
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    int budgetValue = await ValueAsCost();
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("Failed to select an enemy player.");
                        errorPanelController.ShowError("general_error");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    playerBudget = await cardUtilities.ChangeEnemyStat(enemyId, -budgetValue, "money", playerBudget);
                    if(playerBudget == -1)
                    {
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
                } else
                {
                    if (data.Source == "player-deck") { source = playerId; }
                    if (data.Target == "player") { target = playerId; }

                    if (cardId == "CA070")
                    {
                        string cardFromHandInstanceId = selectedCardIds[0].Key;

                        selectedCardIds.Clear();
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        int cardsOnHand = await cardUtilities.CountCardsOnHand(playerId);
                        if (cardsOnHand - 1 < data.CardNumber)
                        {
                            Debug.Log("Za mało kart na ręce aby zagrać kartę");
                            errorPanelController.ShowError("cards_lack");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        else if (cardsOnHand == -1)
                        {
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        selectedCardIds = await cardSelectionUI.ShowCardSelection(playerId, data.CardNumber, cardFromHandInstanceId, false);
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        if (selectedCardIds.Count > 0)
                        {
                            string cardFromDeckInstanceId = selectedCardIds[0].Key;
                            bool checkError = await deckController.ExchangeFromHandToDeck(source, cardFromHandInstanceId, cardFromDeckInstanceId);
                            if (checkError) { return (dbRefPlayerStats, -1, enemyId); }
                        }
                        else
                        {
                            Debug.LogWarning("Nie wybrano żadnej karty z decku.");
                            errorPanelController.ShowError("no_selection");
                            return (dbRefPlayerStats, -1, enemyId);
                        }


                    }
                    else if (cardId == "CA077")
                    {
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        int cardsInDeck = await deckController.CountCardsInDeck(playerId);
                        if (cardsInDeck < data.CardNumber)
                        {
                            Debug.Log("Za mało kart w talii aby zagrać kartę");
                            errorPanelController.ShowError("cards_lack");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        else if (cardsInDeck == -1)
                        {
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        selectedCardIds = await cardSelectionUI.ShowCardSelection(playerId, data.CardNumber, instanceId, false);
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        if (selectedCardIds.Count > 0)
                        {
                            string cardFromDeckInstanceId = selectedCardIds[0].Key;
                            string cardFromDeckCardId = selectedCardIds[0].Value;

                            cardTypeManager.OnCardDropped(cardFromDeckInstanceId, cardFromDeckCardId, true);
                        }
                        else
                        {
                            Debug.LogWarning("Nie wybrano żadnej karty z decku.");
                            errorPanelController.ShowError("no_selection");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                    }
                    else if (cardId == "CA033")
                    {
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        bool errorCheck = await deckController.GetRandomCardsFromHand(playerId, enemyId, data.CardNumber, selectedCardIds);
                        if (errorCheck)
                        {
                            return (dbRefPlayerStats, -1, enemyId);
                        }

                    }
                    else if (cardId == "CA067")
                    {
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        enemyId = await RandomEnemy();
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        if (await cardUtilities.CheckIfProtected(enemyId, -1))
                        {
                            Debug.Log("Gracz jest chroniony nie można zagrać karty");
                            errorPanelController.ShowError("player_protected");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        else if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                        {
                            Debug.Log("Karta została skontrowana.");
                            errorPanelController.ShowError("counter");
                            return (dbRefPlayerStats, playerBudget, enemyId);
                        }
                        else
                        {
                            if (!DataTransfer.IsPlayerTurn)
                            {
                                errorPanelController.ShowError("turn_over");
                                return (dbRefPlayerStats, -1, enemyId);
                            }
                            bool checkError = await deckController.GetCardFromHand(playerId, enemyId, selectedCardIds);
                            if (checkError)
                            {
                                return (dbRefPlayerStats, -1, enemyId);
                            }
                            int cardsOnHand = await cardUtilities.CountCardsOnHand(enemyId);
                            if (cardsOnHand - 1 < data.CardNumber)
                            {
                                Debug.Log("Za mało kart na ręce aby zagrać kartę");
                                errorPanelController.ShowError("cards_lack");
                                return (dbRefPlayerStats, -1, enemyId);
                            }
                            else if (cardsOnHand == -1)
                            {
                                return (dbRefPlayerStats, -1, enemyId);
                            }
                            selectedCardIds = await cardSelectionUI.ShowCardSelection(enemyId, data.CardNumber, instanceId, true, selectedCardIds);
                            checkError = await deckController.GetCardFromHand(enemyId, playerId, selectedCardIds);
                            if (checkError)
                            {
                                return (dbRefPlayerStats, -1, enemyId);
                            }
                        }
                    }
                    else
                    {
                        bool checkError = false;
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        for (int i = 0; i < data.CardNumber; i++)
                        {
                            checkError = await deckController.GetCardFromDeck(source, target);
                            if (checkError)
                            {
                                break;
                            }
                        }

                        if (checkError)
                        {
                            return (dbRefPlayerStats, -1, enemyId);
                        }

                        if (cardId == "CA030")
                        {
                            turnController.EndTurn();
                        }

                    }
                }
            } else if (data.Target == "player-deck") {

                if (cardId == "CA030")
                {
                    if(DataTransfer.IsFirstCardInTurn)
                    {
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        int cardsOnHand = await cardUtilities.CountCardsOnHand(playerId);
                        if(cardsOnHand == -1)
                        {
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        bool checkError = false;
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        for (int i = 0; i < cardsOnHand-1; i++)
                        {
                           checkError= await deckController.RejectRandomCardFromHand(playerId,instanceId);
                            if(checkError) { break; }
                        }

                        if(checkError) { return (dbRefPlayerStats, -1, enemyId); }

                    } else
                    {
                        Debug.Log("Karta ta może być zagrana tylko jako pierwsza w turze");
                        errorPanelController.ShowError("not_first");
                        return(dbRefPlayerStats, -1, enemyId);
                    }
                }
                else
                {
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    int cardsOnHand = await cardUtilities.CountCardsOnHand(playerId);
                    if(cardsOnHand-1 < data.CardNumber)
                    {
                        Debug.Log("Za mało kart na ręce aby zagrać kartę");
                        errorPanelController.ShowError("cards_lack");
                        return(dbRefPlayerStats, -1, enemyId);
                    } else if(cardsOnHand == -1)
                    {
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }

                    if(cardId == "CA070")
                    {
                        int cardsInDeck = await deckController.CountCardsInDeck(playerId);
                        if (cardsInDeck < data.CardNumber)
                        {
                            Debug.Log("Za mało kart w talii aby zagrać kartę");
                            errorPanelController.ShowError("cards_lack");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        else if (cardsInDeck == -1)
                        {
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                    }

                    selectedCardIds = await cardSelectionUI.ShowCardSelection(playerId, data.CardNumber, instanceId, true);

                    if (data.Source == "player")
                    {
                        source = playerId;
                    }

                    if (cardId == "CA031")
                    {
                        bool checkError = false;
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        int cardsInDeck = await deckController.CountCardsInDeck(playerId);
                        if (cardsInDeck < data.CardNumber)
                        {
                            Debug.Log("Za mało kart w talii aby zagrać kartę");
                            errorPanelController.ShowError("cards_lack");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        else if (cardsInDeck == -1)
                        {
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        foreach (var selectedCard in selectedCardIds)
                        {
                            string selectedInstanceId = selectedCard.Key;

                            checkError = await deckController.RejectCard(source, selectedInstanceId);
                            if(checkError) { break; }  
                        }

                        if(checkError) { return(dbRefPlayerStats, -1, enemyId); }
                    }
                }

            }
            else if (data.Target == "enemy-chosen")
            {
                if(cardId == "CA066")
                {
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("Failed to select an enemy player.");
                        errorPanelController.ShowError("general_error");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    await cardSelectionUI.ShowCardsForViewing(enemyId);
                }
                else if (cardId == "CA068")
                {
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    bool checkError = await deckController.GetRandomCardsFromDeck(enemyId, data.CardNumber, selectedCardIds);
                    if(checkError)
                    {
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                } else
                {
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    int cardsOnHand = await cardUtilities.CountCardsOnHand(playerId);
                    if (cardsOnHand - 1 < data.CardNumber)
                    {
                        Debug.Log("Za mało kart na ręce aby zagrać kartę");
                        errorPanelController.ShowError("cards_lack");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    else if (cardsOnHand == -1)
                    {
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    selectedCardIds = await cardSelectionUI.ShowCardSelection(playerId, data.CardNumber, instanceId, true);
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("Failed to select an enemy player.");
                        errorPanelController.ShowError("general_error");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    if (await cardUtilities.CheckIfProtected(enemyId,-1))
                    {
                        Debug.Log("Gracz jest chroniony nie można zagrać karty");
                        errorPanelController.ShowError("player_protected");
                        return (dbRefPlayerStats, -1, enemyId);
                    } else if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                    {
                        Debug.Log("Karta została skontrowana.");
                        errorPanelController.ShowError("counter");
                        return (dbRefPlayerStats, playerBudget, enemyId);
                    }
                    else
                    {
                        target = enemyId;
                        if (data.Source == "player") { source = playerId; }
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        bool errorCheck = await deckController.GetCardFromHand(source, target, selectedCardIds);
                        if(errorCheck)
                        {
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                    }
                }

            }
            else if (data.Target == "enemy-deck")
            {
                if (cardId == "CA087")
                {
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    enemyId = await playerListManager.RandomizeEnemy();
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("Failed to select an enemy player.");
                        errorPanelController.ShowError("general_error");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    if (await cardUtilities.CheckIfProtected(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony, nie można zagrać karty");
                        errorPanelController.ShowError("player_protected");
                        return (dbRefPlayerStats, -1, enemyId);
                    }

                    if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                    {
                        Debug.Log("Karta została skontrowana.");
                        errorPanelController.ShowError("counter");
                        return (dbRefPlayerStats, playerBudget, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    int playerCards = await deckController.CountCardsInDeck(playerId);
                    if (playerCards == -1)
                    {
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    int enemyCards = await deckController.CountCardsInDeck(enemyId);
                    if (enemyCards == -1)
                    {
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    if (enemyCards > playerCards && enemyCards > 0)
                    {
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        bool errorCheck = await deckController.RejectRandomCard(enemyId);
                        if (errorCheck) { return (dbRefPlayerStats, -1, enemyId); }
                    }
                }
                else
                {
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("Failed to select an enemy player.");
                        errorPanelController.ShowError("general_error");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    if (await cardUtilities.CheckIfProtected(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony nie można zagrać karty");
                        errorPanelController.ShowError("player_protected");
                        return (dbRefPlayerStats, -1, enemyId);
                    }
                    else if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                    {
                        Debug.Log("Karta została skontrowana.");
                        errorPanelController.ShowError("counter");
                        return (dbRefPlayerStats, playerBudget, enemyId);
                    }
                    else
                    {
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        int cardsOnHand = await cardUtilities.CountCardsOnHand(enemyId);
                        if (cardsOnHand - 1 < data.CardNumber)
                        {
                            Debug.Log("Za mało kart na ręce aby zagrać kartę");
                            errorPanelController.ShowError("cards_lack");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        else if (cardsOnHand == -1)
                        {
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        selectedCardIds = await cardSelectionUI.ShowCardSelection(enemyId, data.CardNumber, instanceId, true);
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, -1, enemyId);
                        }
                        bool errorCheck = await deckController.ReturnCardToDeck(enemyId, selectedCardIds[0].Key);
                        if (errorCheck) { return (dbRefPlayerStats, -1, enemyId); }
                    }
                }
            }
        }
        return (dbRefPlayerStats, playerBudget, enemyId);
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
            errorPanelController.ShowError("general_error");
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
            errorPanelController.ShowError("general_error");
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
            errorPanelController.ShowError("no_card");
            return null;
        }

        int randomIndex = random.Next(eligibleCards.Count);
        return eligibleCards[randomIndex];
    }

    private async Task<bool> AddCardToDeck(string instanceId, string enemyId)
    {
        if (string.IsNullOrEmpty(instanceId) || string.IsNullOrEmpty(enemyId))
        {
            Debug.Log("instanceId or enemyId is empty");
            errorPanelController.ShowError("general_error");
            return true;
        }

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
            errorPanelController.ShowError("general_error");
            return true;
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

        return false;
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
                Debug.Log("Karta została skontrowana.");
                return true;
            }
        }

        return false;
    }


}


