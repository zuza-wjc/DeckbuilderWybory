using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using UnityEngine;

public class UniqueCardImp : MonoBehaviour
{
    private readonly string lobbyId = DataTransfer.LobbyId;
    private readonly string playerId = DataTransfer.PlayerId;

    public PlayerListManager playerListManager;
    public MapManager mapManager;
    public CardUtilities cardUtilities;
    public CardSelectionUI cardSelectionUI;
    public CardTypeManager cardTypeManager;
    public DeckController deckController;
    public TurnController turnController;
    public ErrorPanelController errorPanelController;
    public HistoryController historyController;
    void Start()
    {
        playerListManager.Initialize(lobbyId, playerId);
    }
    public async void CardLibrary(string instanceId, string cardIdDropped, bool ignoreCost)
    {
        try
        {
            bool cardLimitExceeded = await cardUtilities.CheckCardLimit(playerId);
            if (cardLimitExceeded)
            {
                Debug.Log("Limit kart w turze to 1");
                errorPanelController.ShowError("card_limit");
                return;
            }

            if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
            {
                Debug.LogError("Firebase is not initialized properly!");
                errorPanelController.ShowError("general_error");
                return;
            }

            DatabaseReference dbRefCard = FirebaseInitializer.DatabaseReference
                .Child("cards")
                .Child("id")
                .Child("unique")
                .Child(cardIdDropped);

            DataSnapshot snapshot = await dbRefCard.GetValueAsync();
            if (!snapshot.Exists)
            {
                Debug.LogError("No data for: " + cardIdDropped + ".");
                errorPanelController.ShowError("general_error");
                return;
            }

            int cost = snapshot.Child("cost").Exists ? Convert.ToInt32(snapshot.Child("cost").Value) : -1;

            if (cost < -1)
            {
                Debug.LogError("Branch cost does not exist");
                errorPanelController.ShowError("general_error");
                return;
            }

            string cardType = snapshot.Child("type").Exists ? snapshot.Child("type").Value.ToString() : string.Empty;

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


            cost = await AdjustCardCost(cost);

            DatabaseReference dbRefPlayerStats = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(playerId)
                .Child("stats");

            DataSnapshot playerStatsSnapshot = await dbRefPlayerStats.GetValueAsync();
            if (!playerStatsSnapshot.Exists)
            {
                Debug.LogError("No data for player stats.");
                errorPanelController.ShowError("general_error");
                return;
            }

            int playerBudget = playerStatsSnapshot.Child("money").Exists ? Convert.ToInt32(playerStatsSnapshot.Child("money").Value) : -1;

            if (playerBudget < 0)
            {
                Debug.LogError("Błąd w pobieraniu wartości playerBudget");
                errorPanelController.ShowError("general_error");
                return;

            }
            int playerIncome = playerStatsSnapshot.Child("income").Exists ? Convert.ToInt32(playerStatsSnapshot.Child("income").Value) : -1;

            if (playerIncome < 0)
            {
                Debug.LogError("Błąd w pobieraniu wartości playerIncome");
                errorPanelController.ShowError("general_error");
                return;
            }

            if (!ignoreCost && playerBudget < cost)
            {
                Debug.LogError("Brak budżetu aby zagrać kartę.");
                errorPanelController.ShowError("no_budget");
                return;
            }

            if (!ignoreCost)
            {
                await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget - cost);
                playerBudget -= cost;
            }
            string enemyId = string.Empty;

            (playerBudget,enemyId) = await SwitchCase(instanceId, dbRefPlayerStats, playerIncome, playerBudget, cardIdDropped, -1, false, cardType, enemyId);

            if(playerBudget == -1)
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
            
            if (ignoreCost)
            {
                await HandleIgnoreCost(dbRefPlayerStats, cost);
            }

            DatabaseReference dbRefPlayerDeck = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(playerId)
                .Child("deck")
                .Child(instanceId);

            if (cardIdDropped != "UN032")
            {

                await dbRefPlayerDeck.Child("onHand").SetValueAsync(false);
                await dbRefPlayerDeck.Child("played").SetValueAsync(true);
            }
            

            DataTransfer.IsFirstCardInTurn = false;
            await cardUtilities.CheckIfPlayed2Cards(playerId);

            cardLimitExceeded = await cardUtilities.CheckCardLimit(playerId);

            await historyController.AddCardToHistory(cardIdDropped, playerId, desc, enemyId);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in CardLibrary method: {ex.Message}");
            errorPanelController.ShowError("general_error");
            return;
        }
    }

    private async Task<int> AdjustCardCost(int cost)
    {
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

        return cost;
    }

    private async Task HandleIgnoreCost(DatabaseReference dbRefPlayerStats, int cost)
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
    }

    private async Task<(int,string)> SwitchCase(string instanceId, DatabaseReference dbRefPlayerStats, int playerIncome, int playerBudget, string cardId, int chosenRegion, bool isBonusRegion, string cardType, string enemyId)
    {
        bool errorCheck = false;
        bool checkError = false;
        try
        {
            switch (cardId)
            {
                case "UN018":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    if (await CheckBlockAndLog()) return (-1, enemyId);
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    chosenRegion = await mapManager.SelectArea();
                    isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    enemyId = await playerListManager.SelectEnemyPlayerInArea(chosenRegion);
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    errorCheck = await ProcessEnemySupport(chosenRegion, enemyId, isBonusRegion);
                    if (errorCheck) return (-1, enemyId);
                    break;

                case "UN089":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    if (await CheckBlockAndLog()) return (-1, enemyId);
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    chosenRegion = await mapManager.SelectArea();
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    errorCheck = await ExchangeSupportMaxMin(playerId, chosenRegion);
                    if(errorCheck) { return (-1, enemyId); }
                    break;

                case "UN025":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    if (await CheckBlockAndLog()) return (-1, enemyId);
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    chosenRegion = await mapManager.SelectArea();
                    isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    enemyId = await playerListManager.SelectEnemyPlayerInArea(chosenRegion);
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    errorCheck = await ExchangeSupport(chosenRegion, enemyId, isBonusRegion);
                    if(errorCheck) { return (-1, enemyId); }
                    break;

                case "UN039":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    int cardsOnHand = await cardUtilities.CountCardsOnHand(playerId);
                    if (cardsOnHand - 1 < 1)
                    {
                        Debug.Log("Za mało kart na ręce aby zagrać kartę");
                        errorPanelController.ShowError("cards_lack");
                        return (-1, enemyId);
                    }
                    else if (cardsOnHand == -1)
                    {
                        return (-1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    List<KeyValuePair<string, string>> selectedCards = await cardSelectionUI.ShowCardSelection(playerId, 1, instanceId, true);
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    if (selectedCards.Count > 0)
                    {
                        string selectedInstanceId = selectedCards[0].Key;
                        string selectedCardId = selectedCards[0].Value;

                        cardTypeManager.OnCardDropped(selectedInstanceId, selectedCardId, true);
                    }
                    else
                    {
                        Debug.LogWarning("Nie wybrano żadnej karty.");
                        errorPanelController.ShowError("no_selection");
                        return (-1, enemyId);
                    }
                    break;

                case "UN055":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    if (!(await cardUtilities.CheckBudgetBlock(playerId)))
                    {
                        playerBudget += playerIncome;
                        await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
                        await cardUtilities.CheckAndAddCopyBudget(playerId, playerIncome);
                    } else
                    {
                        Debug.Log("budget block");
                        errorPanelController.ShowError("action_blocked");
                        return (-1, enemyId);
                    }
                    break;

                case "UN086":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    if (await cardUtilities.CheckSupportBlock(playerId))
                    {
                        Debug.Log("Support block");
                        errorPanelController.ShowError("action_blocked");
                        return (-1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    if (await cardUtilities.CheckIncomeBlock(playerId))
                    {
                        Debug.Log("Income block");
                        errorPanelController.ShowError("action_blocked");
                        return (-1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    chosenRegion = await mapManager.SelectArea();
                    isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    checkError = await ChangeIncomePerCard(chosenRegion, isBonusRegion, instanceId);
                    if(checkError)
                    {
                        return (-1, enemyId);
                    }
                    break;

                case "UN019":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    chosenRegion = await mapManager.SelectArea();
                    isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    errorCheck = await ProtectRegion(chosenRegion);
                    if(errorCheck)
                    {
                        return (-1, enemyId);
                    }
                    if (isBonusRegion)
                    {
                        errorCheck = await deckController.GetCardFromDeck(playerId, playerId);
                    }
                    break;

                case "UN021":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    checkError = await CopySupport(enemyId);
                    if(checkError)
                    {
                        return (-1, enemyId);
                    }
                    break;

                case "UN022":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    checkError = await CopyBudget(enemyId);
                    if (checkError)
                    {
                        return (-1, enemyId);
                    }
                    break;

                case "UN024":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    errorCheck = await BlockCard(enemyId);
                    if(errorCheck) { return (-1, enemyId); }
                    break;

                case "UN032":
                    if (DataTransfer.IsFirstCardInTurn)
                    {
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (-1, enemyId);
                        }
                        checkError = await ProtectPlayer();
                        if(checkError) { return (-1, enemyId); }
                        DatabaseReference dbRefPlayerDeck = FirebaseInitializer.DatabaseReference
                        .Child("sessions")
                        .Child(lobbyId)
                        .Child("players")
                        .Child(playerId)
                        .Child("deck")
                        .Child(instanceId);
                        await dbRefPlayerDeck.Child("onHand").SetValueAsync(false);
                        await dbRefPlayerDeck.Child("played").SetValueAsync(true);
                        turnController.EndTurn();
                    }
                    else
                    {
                        Debug.Log("Karta może być zagrana tylko jako pierwsza w turze");
                        errorPanelController.ShowError("not_first");
                        return (-1, enemyId);
                    }
                    break;

                case "UN078":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    if (await cardUtilities.CheckIfProtected(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony, nie można zagrać karty");
                        errorPanelController.ShowError("player_protected");
                        return (-1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony, nie można zagrać karty");
                        errorPanelController.ShowError("player_protected");
                        return (-1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    errorCheck = await IncreaseCost(enemyId);
                    if (errorCheck) return (-1, enemyId);
                    break;

                case "UN080":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    errorCheck = await BlockSupportAction(enemyId);
                    if(errorCheck)
                    {
                        return (-1, enemyId);
                    }
                    playerBudget = await cardUtilities.ChangeEnemyStat(enemyId, 15, "money", playerBudget);
                    if(playerBudget == -1)
                    {
                        return (-1, enemyId);
                    }
                    break;

                case "UN079":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    if (await cardUtilities.CheckIfProtected(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony, nie można zagrać karty");
                        errorPanelController.ShowError("player_protected");
                        return (-1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony na jednej karcie, nie można zagrać karty");
                        errorPanelController.ShowError("player_protected");
                        return (-1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    errorCheck = await IncreaseCostAllTurn(enemyId);
                    if(errorCheck) { return (-1, enemyId); }
                    break;

                case "UN050":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    errorCheck = await DecreaseCost();
                    if(errorCheck)
                    {
                        return (-1, enemyId);
                    }
                    break;

                case "UN052":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    errorCheck = await ProtectPlayer();
                    if(errorCheck)
                    {
                        return (-1, enemyId);
                    }
                    break;

                case "UN082":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    if (await cardUtilities.CheckIfProtected(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony, nie można zagrać karty");
                        errorPanelController.ShowError("player_protected");
                        return (-1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony na jednej karcie, nie można zagrać karty");
                        errorPanelController.ShowError("player_protected");
                        return (-1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    errorCheck = await BlockBudgetAction(enemyId);
                    if (errorCheck) { return (-1, enemyId); }
                    break;

                case "UN083":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    if (await cardUtilities.CheckIfProtected(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony, nie można zagrać karty");
                        errorPanelController.ShowError("player_protected");
                        return (-1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony na jednej karcie, nie można zagrać karty");
                        errorPanelController.ShowError("player_protected");
                        return (-1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    errorCheck = await BlockIncomeAction(enemyId);
                    if (errorCheck) { return (-1, enemyId); }
                    break;

                case "UN074":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    errorCheck = await ProtectPlayerOneCard();
                    if(errorCheck) { return (-1, enemyId); }
                    break;

                case "UN076":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    errorCheck = await LimitCards();
                    if(errorCheck)
                    {
                        return (-1, enemyId);
                    }
                    break;

                case "UN084":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    errorCheck = await BonusSupport();
                    if(errorCheck)
                    {
                        return (-1, enemyId);
                    }
                    break;

                case "UN088":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    enemyId = await playerListManager.RandomizeEnemy();
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    if (await cardUtilities.CheckIfProtected(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony, nie można zagrać karty");
                        errorPanelController.ShowError("player_protected");
                        return (-1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony, nie można zagrać karty");
                        errorPanelController.ShowError("player_protected");
                        return (-1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    int playerCards = await deckController.CountCardsInDeck(playerId);
                    if (playerCards == -1)
                    {
                        return (-1, enemyId);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (-1, enemyId);
                    }
                    int enemyCards = await deckController.CountCardsInDeck(enemyId);
                    if (enemyCards == -1)
                    {
                        return (-1, enemyId);
                    }
                    if(enemyCards < playerCards)
                    {
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (-1, enemyId);
                        }
                        errorCheck = await ChangeDrawCardLimit(enemyId);
                        if (errorCheck)
                        {
                            return (-1, enemyId);
                        }
                    }
                    break;

                default:
                    Debug.LogError("Unknown card ID: " + cardId + ".");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing card: {ex.Message}");
        }

        return (playerBudget,enemyId);
    }

    private async Task<bool> CheckBlockAndLog()
    {
        if (await cardUtilities.CheckSupportBlock(playerId))
        {
            Debug.Log("support block");
            errorPanelController.ShowError("action_blocked");
            return true;
        }
        return false;
    }

    private (int, string) HandleNoEnemyFound()
    {
        Debug.LogError("Failed to select an enemy player.");
        errorPanelController.ShowError("general_error");
        return (-1,string.Empty);
    }

    private async Task<bool> ProcessEnemySupport(int chosenRegion, string enemyId, bool isBonusRegion)
    {
        int supportValue = await GetEnemySupportFromRegion(enemyId, chosenRegion);
        if(supportValue == -1)
        {
            return true;
        }

        if (isBonusRegion) supportValue--;

        if (supportValue != 0)
        {
            try
            {
                chosenRegion = await mapManager.SelectArea();
                bool errorCheck = await ChangeSupportNoLoss(enemyId, supportValue, chosenRegion);
                if(errorCheck)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing enemy support: {ex.Message}");
                errorPanelController.ShowError("general_error");
                return true;
            }
        }

        return false;
    }

    private async Task<int> GetEnemySupportFromRegion(string enemyId, int areaId)
    {
        if (string.IsNullOrEmpty(enemyId))
        {
            Debug.LogError($"Player ID is null or empty. ID: {enemyId}");
            errorPanelController.ShowError("general_error");
            return -1;
        }

        var dbRefSupport = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId)
            .Child("stats")
            .Child("support")
            .Child(areaId.ToString());

        try
        {
            var snapshot = await dbRefSupport.GetValueAsync();
            if (snapshot.Exists && int.TryParse(snapshot.Value.ToString(), out int support))
            {
                await dbRefSupport.SetValueAsync(0);
                return support;
            }
            else
            {
                Debug.LogWarning($"Support data not found or invalid for player {enemyId} in area {areaId}.");
                errorPanelController.ShowError("general_error");
                return -1;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error occurred while retrieving data for player {enemyId}: {ex.Message}");
            errorPanelController.ShowError("general_error");
        }

        return -1;
    }

    private async Task<bool> ChangeSupportNoLoss(string playerId, int value, int areaId)
    {
        DatabaseReference dbRefSupport = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("stats")
            .Child("support")
            .Child(areaId.ToString());

        var snapshot = await dbRefSupport.GetValueAsync();
        if (!snapshot.Exists)
        {
            Debug.LogError("No support data found for the given region in the player's stats.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        if (!int.TryParse(snapshot.Value.ToString(), out int support))
        {
            Debug.LogError("Failed to parse support value from the database.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        var maxSupportTask = mapManager.GetMaxSupportForRegion(areaId);
        var currentSupportTask = mapManager.GetCurrentSupportForRegion(areaId, playerId);

        await Task.WhenAll(maxSupportTask, currentSupportTask);

        int maxAreaSupport = await maxSupportTask;
        int currentAreaSupport = await currentSupportTask;

        int availableSupport = maxAreaSupport - currentAreaSupport;

        if (availableSupport < value)
        {
            Debug.Log("Not enough available space for support in this region.");
            errorPanelController.ShowError("no_support_available");
            return true;
        }

        if (await cardUtilities.CheckIfRegionProtected(playerId, areaId, value))
        {
            Debug.Log("Region is protected, unable to play the card.");
            errorPanelController.ShowError("region_protected");
            return true;
        }

        if (await cardUtilities.CheckIfProtected(playerId, value))
        {
            Debug.Log("Player is protected, unable to play the card.");
            errorPanelController.ShowError("player_protected");
            return true;
        }

        if (await cardUtilities.CheckIfProtectedOneCard(playerId, value))
        {
            Debug.Log("Player is protected by a one-card protection, unable to play the card.");
            errorPanelController.ShowError("region_protected");
            return true;
        }

        await cardUtilities.CheckIfBudgetPenalty(areaId);

        await cardUtilities.CheckBonusBudget(playerId, value);
        value = await cardUtilities.CheckBonusSupport(playerId, value);

        support += value;
        await dbRefSupport.SetValueAsync(support);

        await cardUtilities.CheckAndAddCopySupport(playerId, areaId, value, mapManager);

        return false;
    }

    private async Task<bool> ExchangeSupportMaxMin(string cardHolderId, int chosenRegion)
    {
        DatabaseReference dbRefPlayersStats = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        var snapshot = await dbRefPlayersStats.GetValueAsync();
        if (!snapshot.Exists)
        {
            Debug.LogError("No players data found in the session.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        List<(string playerId, int support)> playersWithSupport = snapshot.Children
        .Select(playerSnapshot =>
        {
            string playerId = playerSnapshot.Key;
            var supportSnapshot = playerSnapshot
                .Child("stats")
                .Child("support")
                .Child(chosenRegion.ToString());

            return supportSnapshot.Exists && int.TryParse(supportSnapshot.Value.ToString(), out int support) && support != 0
                ? new ValueTuple<string, int>(playerId, support)
                : (string.Empty, 0);
        })
        .Where(player => player != (string.Empty, 0))
        .ToList();


        if (playersWithSupport.Count < 2)
        {
            Debug.Log("Not enough players with non-zero support in the chosen region.");
            errorPanelController.ShowError("no_player");
            return true;
        }

        var maxSupport = playersWithSupport.Max(p => p.support);
        var minSupport = playersWithSupport.Min(p => p.support);

        var maxSupportPlayers = playersWithSupport.Where(p => p.support == maxSupport).ToList();
        var minSupportPlayers = playersWithSupport.Where(p => p.support == minSupport).ToList();

        var maxPlayer = maxSupportPlayers[UnityEngine.Random.Range(0, maxSupportPlayers.Count)];
        var minPlayer = minSupportPlayers[UnityEngine.Random.Range(0, minSupportPlayers.Count)];

        if (await IsRegionOrPlayerProtected(maxPlayer, minPlayer, chosenRegion, maxPlayer.support, minPlayer.support))
        {
            Debug.Log("Player is protected, unable to play the card.");
            errorPanelController.ShowError("player_protected");
            return true;
        }

        await cardUtilities.CheckBonusBudget(minPlayer.playerId, minPlayer.support - maxPlayer.support);

        var maxSupportRef = dbRefPlayersStats
            .Child(maxPlayer.playerId)
            .Child("stats")
            .Child("support")
            .Child(chosenRegion.ToString());

        var minSupportRef = dbRefPlayersStats
            .Child(minPlayer.playerId)
            .Child("stats")
            .Child("support")
            .Child(chosenRegion.ToString());

        await maxSupportRef.SetValueAsync(minPlayer.support);
        await minSupportRef.SetValueAsync(maxPlayer.support);

        await cardUtilities.CheckAndAddCopySupport(maxPlayer.playerId, chosenRegion, maxPlayer.support - minPlayer.support, mapManager);

        return false;
    }

    private async Task<bool> IsRegionOrPlayerProtected((string playerId, int support) maxPlayer, (string playerId, int support) minPlayer, int chosenRegion, int maxSupport, int minSupport)
    {
        if (await cardUtilities.CheckIfRegionProtected(maxPlayer.playerId, chosenRegion, minSupport - maxSupport))
        {
            Debug.Log("Region is protected, unable to play the card.");
            return true;
        }

        bool isProtected = await cardUtilities.CheckIfProtected(maxPlayer.playerId, minSupport - maxSupport);
        bool isProtectedOneCard = await cardUtilities.CheckIfProtectedOneCard(maxPlayer.playerId, minSupport - maxSupport);

        if (isProtected || isProtectedOneCard)
        {
            Debug.Log("Player is protected, unable to play the card.");
            return true;
        }

        await cardUtilities.CheckIfBudgetPenalty(chosenRegion);
        return false;
    }

    private async Task<bool> ExchangeSupport(int chosenRegion, string enemyId, bool isBonus)
    {
        DatabaseReference dbRefPlayersStats = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(DataTransfer.LobbyId)
            .Child("players");

        var playerSupportRef = dbRefPlayersStats
            .Child(playerId)
            .Child("stats")
            .Child("support")
            .Child(chosenRegion.ToString());

        var enemySupportRef = dbRefPlayersStats
            .Child(enemyId)
            .Child("stats")
            .Child("support")
            .Child(chosenRegion.ToString());

        var playerSupportSnapshot = await playerSupportRef.GetValueAsync();
        var enemySupportSnapshot = await enemySupportRef.GetValueAsync();

        if (!playerSupportSnapshot.Exists || !enemySupportSnapshot.Exists)
        {
            Debug.LogError("Support data not found for player or enemy in the chosen region.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        if (!int.TryParse(playerSupportSnapshot.Value.ToString(), out int playerSupport) ||
            !int.TryParse(enemySupportSnapshot.Value.ToString(), out int enemySupport))
        {
            Debug.LogError("Failed to parse support values.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        if (isBonus)
        {
            enemySupport++;
            playerSupport--;
        }

        bool isRegionProtected = await cardUtilities.CheckIfRegionProtected(enemyId, chosenRegion, playerSupport - enemySupport);
        bool isPlayerProtected = await cardUtilities.CheckIfProtected(enemyId, playerSupport - enemySupport);
        bool isOneCardProtected = await cardUtilities.CheckIfProtectedOneCard(enemyId, playerSupport - enemySupport);

        if (isRegionProtected || isPlayerProtected || isOneCardProtected)
        {
            Debug.Log("The area or player is protected, unable to play the card.");
            errorPanelController.ShowError("player_protected");
            return true;
        }

        await cardUtilities.CheckIfBudgetPenalty(chosenRegion);

        await cardUtilities.CheckBonusBudget(playerId, enemySupport - playerSupport);

        await playerSupportRef.SetValueAsync(enemySupport);
        await enemySupportRef.SetValueAsync(playerSupport);

        await cardUtilities.CheckAndAddCopySupport(enemyId, chosenRegion, enemySupport - playerSupport, mapManager);

        return false;
    }

    private async Task<bool> ChangeIncomePerCard(int chosenRegion, bool isBonus, string instanceId)
    {
        var playersRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        var playersSnapshot = await playersRef.GetValueAsync();
        if (!playersSnapshot.Exists)
        {
            Debug.LogError("No players data found in session.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        foreach (var playerSnapshot in playersSnapshot.Children)
        {
            string currentPlayerId = playerSnapshot.Key;

            var supportRef = playerSnapshot
                .Child("stats")
                .Child("support")
                .Child(chosenRegion.ToString());

            if (!supportRef.Exists || !int.TryParse(supportRef.Value.ToString(), out int support) || support <= 0)
            {
                continue;
            }

            if (isBonus && currentPlayerId == playerId)
                continue;

            int cardsOnHand = await cardUtilities.CountCardsOnHand(currentPlayerId);

            if (currentPlayerId == playerId)
            {
                var cardRef = playersRef.Child(currentPlayerId).Child("deck").Child(instanceId);
                var cardSnapshot = await cardRef.GetValueAsync();

                if (cardSnapshot.Exists &&
                    cardSnapshot.Child("onHand").Exists &&
                    bool.TryParse(cardSnapshot.Child("onHand").Value.ToString(), out bool isOnHand) &&
                    isOnHand)
                {
                    cardsOnHand--;
                }
            }

            if (cardsOnHand > 0)
            {
                var incomeRef = playersRef.Child(currentPlayerId).Child("stats").Child("income");
                var incomeSnapshot = await incomeRef.GetValueAsync();

                if (incomeSnapshot.Exists && int.TryParse(incomeSnapshot.Value.ToString(), out int income))
                {
                    int newIncome = Mathf.Max(0, income - cardsOnHand);

                    bool isProtected = await cardUtilities.CheckIfProtected(currentPlayerId, -cardsOnHand) ||
                                       await cardUtilities.CheckIfProtectedOneCard(currentPlayerId, -cardsOnHand);

                    if (isProtected)
                    {
                        continue;
                    }

                    await incomeRef.SetValueAsync(newIncome);
                }
            }
        }

        return false;
    }

    private async Task<int?> GetTurnsTakenAsync(string playerId)
    {
        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefTurn = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("stats")
            .Child("turnsTaken");

        DataSnapshot turnSnapshot = await dbRefTurn.GetValueAsync();

        if (turnSnapshot.Exists)
        {
            return Convert.ToInt32(turnSnapshot.Value);
        }
        else
        {
            Debug.LogError($"Nie udało się pobrać liczby tur dla gracza {playerId}.");
            return null;
        }
    }

    private async Task<bool> ProtectRegion(int regionId)
    {
        string lobbyId = DataTransfer.LobbyId;

        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null)
        {
            Debug.LogError("Nie udało się pobrać liczby tur dla gracza.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        var dbRefProtectedRegion = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("protected")
            .Child("region")
            .Child(regionId.ToString());

        await dbRefProtectedRegion.SetValueAsync(turnsTaken);

        return false;
    }

    private async Task<bool> CopySupport(string enemyId)
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null)
        {
            Debug.LogError("Nie udało się pobrać liczby tur dla gracza.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefCopySupport = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("copySupport");

        var copySupportData = new Dictionary<string, object>
    {
        { "turnsTaken", turnsTaken },
        { "enemyId", enemyId }
    };

        await dbRefCopySupport.SetValueAsync(copySupportData);

        return false;
    }

    private async Task<bool> CopyBudget(string enemyId)
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null)
        {
            Debug.LogError("Nie udało się pobrać liczby tur dla gracza.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefCopyBudget = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("copyBudget");

        var copyBudgetData = new Dictionary<string, object>
    {
        { "turnsTaken", turnsTaken },
        { "enemyId", enemyId }
    };

        await dbRefCopyBudget.SetValueAsync(copyBudgetData);

        return false;
    }

    private async Task<bool> BlockCard(string enemyId)
    {
        if (await cardUtilities.CheckIfProtected(enemyId, -1))
        {
            Debug.Log("Gracz jest chroniony, nie można zagrać karty");
            errorPanelController.ShowError("player_protected");
            return true;
        }
        if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
        {
            Debug.Log("Gracz jest chroniony, nie można zagrać karty");
            errorPanelController.ShowError("player_protected");
            return true;
        }

        int? turnsTaken = await GetTurnsTakenAsync(enemyId);
        if (turnsTaken == null)
        {
            Debug.LogError("turnsTaken == null");
            errorPanelController.ShowError("general_error");
            return true;
        } 

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId);

        DatabaseReference dbRefCardBlocked = dbRefPlayer.Child("cardBlocked");

        var cardBlockedData = new Dictionary<string, object>
    {
        { "turnsTaken", turnsTaken + 1 },
        { "isBlocked", true }
    };

        await dbRefCardBlocked.SetValueAsync(cardBlockedData);

        return false;
    }

    private async Task<bool> ProtectPlayer()
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null)
        {
            Debug.LogError("Nie udało się pobrać liczby tur dla gracza.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefProtected = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("protected")
            .Child("all");

        await dbRefProtected.SetValueAsync(turnsTaken);

        return false;
    }

    private async Task<bool> IncreaseCost(string enemyId)
    {
        int? turnsTaken = await GetTurnsTakenAsync(enemyId);
        if (turnsTaken == null)
        {
            Debug.LogError("Nie udało się pobrać liczby tur dla gracza.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId);

        DatabaseReference dbRefIncreaseCost = dbRefPlayer.Child("increaseCost");

        var increaseCostSnapshot = await dbRefIncreaseCost.GetValueAsync();

        if (increaseCostSnapshot.Exists)
        {
            bool entryExists = false;
            int nextIndex = 0;

            foreach (var child in increaseCostSnapshot.Children)
            {
                int existingTurnsTaken = Convert.ToInt32(child.Child("turnsTaken").Value);
                if (existingTurnsTaken == turnsTaken + 1)
                {
                    entryExists = true;
                    break;
                }

                if (int.TryParse(child.Key, out int index) && index >= nextIndex)
                {
                    nextIndex = index + 1;
                }
            }

            if (!entryExists)
            {
                foreach (var child in increaseCostSnapshot.Children)
                {
                    await dbRefIncreaseCost.Child(child.Key).RemoveValueAsync();
                }

                var increaseCostData = new Dictionary<string, object>
            {
                { "turnsTaken", turnsTaken + 1 }
            };

                await dbRefIncreaseCost.Child(nextIndex.ToString()).SetValueAsync(increaseCostData);
            }
            else
            {
                var increaseCostData = new Dictionary<string, object>
            {
                { "turnsTaken", turnsTaken + 1 }
            };

                await dbRefIncreaseCost.Child(nextIndex.ToString()).SetValueAsync(increaseCostData);
            }
        }
        else
        {
            var increaseCostData = new Dictionary<string, object>
        {
            { "turnsTaken", turnsTaken + 1 }
        };

            await dbRefIncreaseCost.Child("0").SetValueAsync(increaseCostData);
        }

        return false;
    }



    private async Task<bool> BlockSupportAction(string enemyId)
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null)
        {
            Debug.LogError("Nie udało się pobrać liczby tur dla gracza.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefEnemy = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId);

        DatabaseReference dbRefBlockSupport = dbRefEnemy.Child("blockSupport");

        await dbRefBlockSupport.SetValueAsync(new Dictionary<string, object>
    {
        { "turnsTaken", turnsTaken },
        { "playerId", playerId }
    });

        return false;
    }

    private async Task<bool> IncreaseCostAllTurn(string enemyId)
    {
        int? turnsTaken = await GetTurnsTakenAsync(enemyId);
        if (turnsTaken == null)
        {
            Debug.LogError("Nie udało się pobrać liczby tur dla gracza.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId);

        DatabaseReference dbRefIncreaseCost = dbRefPlayer.Child("increaseCostAllTurn");

        var increaseCostSnapshot = await dbRefIncreaseCost.GetValueAsync();

        if (increaseCostSnapshot.Exists)
        {
            bool entryExists = false;

            foreach (var child in increaseCostSnapshot.Children)
            {
                int existingTurnsTaken = Convert.ToInt32(child.Child("turnsTaken").Value);

                if (existingTurnsTaken == turnsTaken + 1)
                {
                    entryExists = true;
                    break;
                }
            }

            if (!entryExists)
            {
                await dbRefIncreaseCost.RemoveValueAsync();

                var increaseCostData = new Dictionary<string, object>
            {
                { "turnsTaken", turnsTaken + 1 }
            };

                await dbRefIncreaseCost.Child("0").SetValueAsync(increaseCostData);
            }
            else
            {
                int index = (int)increaseCostSnapshot.ChildrenCount;
                var increaseCostData = new Dictionary<string, object>
            {
                { "turnsTaken", turnsTaken + 1 }
            };

                await dbRefIncreaseCost.Child(index.ToString()).SetValueAsync(increaseCostData);
            }
        }
        else
        {
            var increaseCostData = new Dictionary<string, object>
        {
            { "turnsTaken", turnsTaken + 1 }
        };

            await dbRefIncreaseCost.Child("0").SetValueAsync(increaseCostData);
        }

        return false;
    }


    private async Task<bool> DecreaseCost()
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null)
        {
            Debug.LogError("Nie udało się pobrać liczby tur dla gracza.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId);

        DatabaseReference dbRefDecreaseCost = dbRefPlayer.Child("decreaseCost");

        DataSnapshot snapshot = await dbRefDecreaseCost.GetValueAsync();
        bool hasSameTurnsTaken = false;
        int nextIndex = 0;

        if (snapshot.Exists)
        {
            foreach (var child in snapshot.Children)
            {
                if (int.TryParse(child.Key, out int index) && index >= nextIndex)
                {
                    nextIndex = index + 1;
                }

                if (child.Child("turnsTaken").Value?.ToString() == turnsTaken.ToString())
                {
                    hasSameTurnsTaken = true;
                }
            }

            if (!hasSameTurnsTaken)
            {
                foreach (var child in snapshot.Children)
                {
                    await dbRefDecreaseCost.Child(child.Key).RemoveValueAsync();
                }
            }
        }

        var decreaseCostData = new Dictionary<string, object>
    {
        { "turnsTaken", turnsTaken }
    };

        await dbRefDecreaseCost.Child(nextIndex.ToString()).SetValueAsync(decreaseCostData);

        return false;
    }

    private async Task<bool> BlockBudgetAction(string enemyId)
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null)
        {
            Debug.LogError("Nie udało się pobrać liczby tur dla gracza.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefEnemy = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId);

        DatabaseReference dbRefBlockBudget = dbRefEnemy.Child("blockBudget");

        await dbRefBlockBudget.SetValueAsync(new Dictionary<string, object>
    {
        { "turnsTaken", turnsTaken },
        { "playerId", playerId }
    });

        return false;
    }

    private async Task<bool> BlockIncomeAction(string enemyId)
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null)
        {
            Debug.LogError("Nie udało się pobrać liczby tur dla gracza.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefEnemy = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId);

        DatabaseReference dbRefBlockIncome = dbRefEnemy.Child("blockIncome");

        await dbRefBlockIncome.SetValueAsync(new Dictionary<string, object>
    {
        { "turnsTaken", turnsTaken },
        { "playerId", playerId }
    });

        return false;
    }

    private async Task<bool> ProtectPlayerOneCard()
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null)
        {
            Debug.LogError("Nie udało się pobrać liczby tur dla gracza.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefProtected = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("protected")
            .Child("allOneCard");

        await dbRefProtected.SetValueAsync(turnsTaken);

        return false;
    }

    private async Task<bool> LimitCards()
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null)
        {
            Debug.LogError("Nie udało się pobrać liczby tur dla gracza.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefPlayers = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        DataSnapshot playersSnapshot = await dbRefPlayers.GetValueAsync();

        if (!playersSnapshot.Exists)
        {
            Debug.LogError("Brak graczy w lobby.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        foreach (var playerSnapshot in playersSnapshot.Children)
        {
            string currentPlayerId = playerSnapshot.Key;

            var limitCardsData = new Dictionary<string, object>
        {
            { "playerId", playerId },
            { "playedCards", -1 },
            { "turnsTaken", turnsTaken }
        };

            var dbRefLimitCards = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(currentPlayerId)
                .Child("limitCards");

            await dbRefLimitCards.SetValueAsync(limitCardsData);
        }

        return false;
    }

    private async Task<bool> BonusSupport()
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null)
        {
            Debug.LogError("Nie udało się pobrać liczby tur dla gracza.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefBonusSupport = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("bonusSupport");

        await dbRefBonusSupport.SetValueAsync(new Dictionary<string, object>
    {
        { "turnsTaken", turnsTaken }
    });

        return false;
    }

    private async Task<bool> ChangeDrawCardLimit(string enemyId)
    {
        try
        {
            var enemyRef = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(DataTransfer.LobbyId)
                .Child("players")
                .Child(enemyId)
                .Child("drawCardsLimit");

            var snapshot = await enemyRef.GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError($"drawCardsLimit for enemyId {enemyId} does not exist.");
                errorPanelController.ShowError("general_error");
                return true;
            }

            int currentLimit = int.Parse(snapshot.Value.ToString());

            int newLimit = Math.Max(currentLimit - 1, 0);

            await enemyRef.SetValueAsync(newLimit);

            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in ChangeDrawCardLimit for enemyId {enemyId}: {ex.Message}");
            errorPanelController.ShowError("general_error");
            return true;
        }
    }

}
