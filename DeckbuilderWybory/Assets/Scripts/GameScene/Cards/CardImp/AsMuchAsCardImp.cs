using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using UnityEngine;

public class AsMuchAsCardImp : MonoBehaviour
{
    private readonly string lobbyId = DataTransfer.LobbyId;
    private readonly string playerId = DataTransfer.PlayerId;

    public PlayerListManager playerListManager;
    public CardUtilities cardUtilities;
    public MapManager mapManager;
    public ErrorPanelController errorPanelController;
    public HistoryController historyController;
    void Start()
    {
        playerListManager.Initialize(lobbyId, playerId);
    }


    public async void CardLibrary(string instanceId,string cardIdDropped, bool ignoreCost)
    {
        bool tmp = await cardUtilities.CheckCardLimit(playerId);

        if (tmp)
        {
            Debug.Log("Limit kart w turze to 1");
            errorPanelController.ShowError("card_limit");
            return;
        }

            DatabaseReference dbRefCard, dbRefPlayerStats, dbRefPlayerDeck;
            int cost, playerBudget, playerIncome, chosenRegion = -1;
            string cardType, enemyId = string.Empty;
        bool budgetChange = false, supportChange = false, isBonusRegion = false, incomeChange = false, errorCheck = false;

            Dictionary<int, OptionData> budgetOptionsDictionary = new();
            Dictionary<int, OptionData> budgetBonusOptionsDictionary = new();
            Dictionary<int, OptionDataPerCard> incomeOptionsDictionary = new();
            Dictionary<int, OptionDataPerCard> incomeBonusOptionsDictionary = new();
            Dictionary<int, OptionDataPerCard> supportOptionsDictionary = new();
            Dictionary<int, OptionDataPerCard> supportBonusOptionsDictionary = new();
            Dictionary<int, OptionDataPerCard> budgetOptionsPerCardDictionary = new();
            Dictionary<int, OptionDataPerCard> budgetBonusOptionsPerCardDictionary = new();

            budgetOptionsDictionary.Clear();
            budgetBonusOptionsDictionary.Clear();
            incomeOptionsDictionary.Clear();
            incomeBonusOptionsDictionary.Clear();
            supportOptionsDictionary.Clear();
            supportBonusOptionsDictionary.Clear();
            budgetOptionsPerCardDictionary.Clear();
            budgetBonusOptionsPerCardDictionary.Clear();

            if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
            {
                Debug.LogError("Firebase is not initialized properly!");
            errorPanelController.ShowError("general_error");
            return;
            }

            dbRefCard = FirebaseInitializer.DatabaseReference.Child("cards").Child("id").Child("asMuchAs").Child(cardIdDropped);
            DataSnapshot snapshot = await dbRefCard.GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError("No data for: " + cardIdDropped + ".");
            errorPanelController.ShowError("general_error");
            return;
            }

            cost = snapshot.Child("cost").Exists ? Convert.ToInt32(snapshot.Child("cost").Value) : -1;

        if(cost <-1)
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

        if(cardType == string.Empty)
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

        budgetChange = snapshot.Child("budget").Exists;
            if (budgetChange)
            {
                if (cardIdDropped == "AS072")
                {
                    ProcessBonusOptionsPerCard(snapshot.Child("budget"), budgetBonusOptionsPerCardDictionary, "Budget");
                    ProcessOptionsPerCard(snapshot.Child("budget"), budgetOptionsPerCardDictionary, "Budget");
                }
                else
                {
                    cardUtilities.ProcessBonusOptions(snapshot.Child("budget"), budgetBonusOptionsDictionary);
                    cardUtilities.ProcessOptions(snapshot.Child("budget"), budgetOptionsDictionary);
                }
            }

            incomeChange = snapshot.Child("income").Exists;
            if (incomeChange)
            {
                ProcessBonusOptionsPerCard(snapshot.Child("income"), incomeBonusOptionsDictionary, "Income");
                ProcessOptionsPerCard(snapshot.Child("income"), incomeOptionsDictionary, "Income");
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

            ProcessBonusOptionsPerCard(snapshot.Child("support"), supportBonusOptionsDictionary, "Support");
                ProcessOptionsPerCard(snapshot.Child("support"), supportOptionsDictionary, "Support");
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

        if(playerBudget < 0)
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

            playerIncome = playerStatsSnapshot.Child("income").Exists ? Convert.ToInt32(playerStatsSnapshot.Child("income").Value) : -1;

        if (playerIncome < 0)
        {
            Debug.LogError("Błąd w pobieraniu wartości playerIncome");
            errorPanelController.ShowError("general_error");
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
                (isBonusRegion,errorCheck) = await SupportAction(cardIdDropped, isBonusRegion, supportOptionsDictionary, supportBonusOptionsDictionary, chosenRegion, cardType);

                if(errorCheck)
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

            if (budgetChange)
            {
                (dbRefPlayerStats, isBonusRegion, playerBudget, enemyId) = await BudgetAction(dbRefPlayerStats, cardIdDropped, isBonusRegion, budgetOptionsDictionary, budgetBonusOptionsDictionary, budgetOptionsPerCardDictionary,
                    budgetBonusOptionsPerCardDictionary, playerBudget, enemyId, cardType);

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

            if (incomeChange)
            {
                (dbRefPlayerStats,errorCheck) = await IncomeAction(isBonusRegion, incomeOptionsDictionary, incomeBonusOptionsDictionary, playerIncome, cardType, dbRefPlayerStats);

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
            await dbRefPlayerDeck.Child("onHand").SetValueAsync(false);
            await dbRefPlayerDeck.Child("played").SetValueAsync(true);

        DataTransfer.IsFirstCardInTurn = false;

        await cardUtilities.CheckIfPlayed2Cards(playerId);
         tmp = await cardUtilities.CheckCardLimit(playerId);

        await historyController.AddCardToHistory(cardIdDropped, playerId, desc, enemyId);
    }

    private async Task<(DatabaseReference,bool)> IncomeAction(
    bool isBonusRegion,
    Dictionary<int, OptionDataPerCard> incomeOptionsDictionary,
    Dictionary<int, OptionDataPerCard> incomeBonusOptionsDictionary,
    int playerIncome,
    string cardType,
    DatabaseReference dbRefPlayerStats)
    {
        var optionsToApply = isBonusRegion ? incomeBonusOptionsDictionary : incomeOptionsDictionary;

        if (optionsToApply == null || !optionsToApply.Any())
        {
            Debug.LogError("No options to apply.");
            errorPanelController.ShowError("general_error");
            return (dbRefPlayerStats,true);
        }

        if (isBonusRegion)
        {
            Debug.Log("Bonus region detected.");
        }

        foreach (var data in optionsToApply.Values)
        {
            if (data == null)
            {
                Debug.LogWarning("Encountered null data, skipping.");
                continue;
            }

            if (data.Target == "player")
            {
                if (!DataTransfer.IsPlayerTurn)
                {
                    errorPanelController.ShowError("turn_over");
                    return (dbRefPlayerStats, true);
                }
                if (!(await cardUtilities.CheckIncomeBlock(playerId))) {
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, true);
                    }
                    int howMany = await CalculateValueFromHand(playerId, cardType);
                    if(howMany == -1)
                    {
                        return (dbRefPlayerStats, true);
                    }
                    playerIncome += howMany * data.NumberPerCard;
                    await dbRefPlayerStats.Child("income").SetValueAsync(playerIncome);
                } else
                {
                    errorPanelController.ShowError("action_blocked");
                    return (dbRefPlayerStats, true);
                }
            }
        }

        return (dbRefPlayerStats, false);
    }

    private async Task<(DatabaseReference dbRefPlayerStats,bool isBonusRegion,int playerBudget, string)>BudgetAction(DatabaseReference dbRefPlayerStats,string cardId, bool isBonusRegion,
        Dictionary<int, OptionData> budgetOptionsDictionary,Dictionary<int, OptionData> budgetBonusOptionsDictionary,Dictionary<int, OptionDataPerCard> budgetOptionsPerCardDictionary,
        Dictionary<int, OptionDataPerCard> budgetBonusOptionsPerCardDictionary,int playerBudget, string enemyId, string cardType)
    {
        var isBonus = isBonusRegion;
        object optionsToApply;

        if (cardId == "AS072")
        {
            optionsToApply = isBonus ? (object)budgetBonusOptionsPerCardDictionary : (object)budgetOptionsPerCardDictionary;
        }
        else
        {
            optionsToApply = isBonus ? (object)budgetBonusOptionsDictionary : (object)budgetOptionsDictionary;
        }

        if (optionsToApply is Dictionary<int, OptionData> optionsData)
        {
            if (optionsData?.Values == null || !optionsData.Values.Any())
            {
                Debug.LogError("No options to apply.");
                errorPanelController.ShowError("general_error");
                return (dbRefPlayerStats,false,-1,enemyId);
            }

            if (isBonus)
            {
                Debug.Log("Bonus region detected.");
            }

            foreach (var data in optionsData.Values)
            {
                if (data == null)
                {
                    Debug.LogWarning("Encountered null data, skipping.");
                    continue;
                }

                if (data.Target == "player")
                {
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, false, -1, enemyId);
                    }
                    if (!(await cardUtilities.CheckBudgetBlock(playerId)))
                    {
                        playerBudget += data.Number;
                        if (playerBudget < 0)
                        {
                            Debug.LogWarning("Brak wystarczającego budżetu aby zagrać kartę.");
                            errorPanelController.ShowError("no_budget");
                            return (dbRefPlayerStats, false, -1, enemyId);
                        }
                        await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
                        await cardUtilities.CheckAndAddCopyBudget(playerId, data.Number);
                    } else
                    {
                        Debug.Log("Budget blocked");
                        errorPanelController.ShowError("action_blocked");
                        return(dbRefPlayerStats, false, -1, enemyId);
                    }
                }
            }
        }

        else if (optionsToApply is Dictionary<int, OptionDataPerCard> optionsPerCard)
        {
            if (optionsPerCard?.Values == null || !optionsPerCard.Values.Any())
            {
                Debug.LogError("No options to apply.");
                errorPanelController.ShowError("general_error");
                return (dbRefPlayerStats, false, -1, enemyId);
            }

            if (isBonus)
            {
                Debug.Log("Bonus region detected.");
            }

            foreach (var data in optionsPerCard.Values)
            {
                if (data == null)
                {
                    Debug.LogWarning("Encountered null data, skipping.");
                    continue;
                }

                if (data.Target == "enemy")
                {
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (dbRefPlayerStats, false, -1, enemyId);
                        }
                        enemyId = await playerListManager.SelectEnemyPlayer();
                        if (string.IsNullOrEmpty(enemyId))
                        {
                            Debug.LogError("Failed to select an enemy player.");
                            errorPanelController.ShowError("general_error");
                            return (dbRefPlayerStats, false, -1, enemyId);
                        }
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, false, -1, enemyId);
                    }
                    int howMany = await CalculateValueFromHand(playerId, cardType);
                    if(howMany == -1)
                    {
                        return (dbRefPlayerStats, false, -1, enemyId);
                    }
                    int changeBudget = howMany * data.NumberPerCard;
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, false, -1, enemyId);
                    }
                    playerBudget = await cardUtilities.ChangeEnemyStat(enemyId, changeBudget, "money", playerBudget);
                    if(playerBudget == -1)
                    {
                        return (dbRefPlayerStats, false, -1, enemyId);
                    }
                    await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
                }
            }
        }
        else
        {
            Debug.LogError("Unexpected optionsToApply type.");
            errorPanelController.ShowError("general_error");
            return (dbRefPlayerStats, false, -1, enemyId);
        }
        return (dbRefPlayerStats, isBonusRegion, playerBudget, enemyId);
    }
    private async Task<(bool,bool)> SupportAction(
    string cardId,
    bool isBonusRegion,
    Dictionary<int, OptionDataPerCard> supportOptionsDictionary,
    Dictionary<int, OptionDataPerCard> supportBonusOptionsDictionary,
    int chosenRegion,
    string cardType)
    {
        var optionsToApply = isBonusRegion ? supportBonusOptionsDictionary : supportOptionsDictionary;

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No support options available.");
            errorPanelController.ShowError("general_error");
            return (false,false);
        }

        if (isBonusRegion)
        {
            Debug.Log("Bonus region detected.");
        }

        foreach (var data in optionsToApply.Values)
        {
            if (data == null)
            {
                Debug.LogWarning("Encountered null data in support options, skipping.");
                continue;
            }

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

                isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
                if (!DataTransfer.IsPlayerTurn)
                {
                    errorPanelController.ShowError("turn_over");
                    return (false, false);
                }
                int supportValue = await CalculateValueFromHand(playerId, cardType);
                if (!DataTransfer.IsPlayerTurn)
                {
                    errorPanelController.ShowError("turn_over");
                    return (false, false);
                }
                await cardUtilities.ChangeSupport(playerId, supportValue, chosenRegion, cardId, mapManager);
            }
        }

        return (isBonusRegion,false);
    }

    private void ProcessOptionsPerCard(DataSnapshot snapshot, Dictionary<int, OptionDataPerCard> optionsDictionary, string optionType)
    {
        foreach (var optionSnapshot in snapshot.Children)
        {
            if (optionSnapshot.Key != "bonus")
            {
                DataSnapshot numberPerCardSnapshot = optionSnapshot.Child("numberPerCard");
                DataSnapshot targetSnapshot = optionSnapshot.Child("target");

                if (numberPerCardSnapshot.Exists && targetSnapshot.Exists)
                {
                    if (int.TryParse(numberPerCardSnapshot.Value.ToString(), out int numberPerCard) &&
                        targetSnapshot.Value is string target)
                    {
                        if (int.TryParse(optionSnapshot.Key.Replace("option", ""), out int optionKey))
                        {
                            if (!optionsDictionary.ContainsKey(optionKey))
                            {
                                optionsDictionary.Add(optionKey, new OptionDataPerCard(numberPerCard, target));
                            }
                            else
                            {
                                Debug.LogWarning($"{optionType} option with key {optionKey} already exists in the dictionary.");
                            }
                        }
                        else
                        {
                            Debug.LogError($"Invalid option key format for {optionType}: {optionSnapshot.Key}");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Failed to parse 'numberPerCard' or 'target' for {optionType} option: {optionSnapshot.Key}");
                    }
                }
                else
                {
                    Debug.LogError($"{optionType} option is missing 'numberPerCard' or 'target' for {optionSnapshot.Key}.");
                }
            }
        }
    }


    private void ProcessBonusOptionsPerCard(DataSnapshot snapshot, Dictionary<int, OptionDataPerCard> bonusOptionsDictionary, string optionType)
    {
        DataSnapshot bonusSnapshot = snapshot.Child("bonus");
        if (bonusSnapshot.Exists)
        {
            int optionIndex = 0;

            foreach (var optionSnapshot in bonusSnapshot.Children)
            {
                DataSnapshot numberPerCardSnapshot = optionSnapshot.Child("numberPerCard");
                DataSnapshot targetSnapshot = optionSnapshot.Child("target");

                if (numberPerCardSnapshot.Exists && targetSnapshot.Exists)
                {
                    if (int.TryParse(numberPerCardSnapshot.Value.ToString(), out int numberPerCard) &&
                        targetSnapshot.Value is string target)
                    {
                        if (!bonusOptionsDictionary.ContainsKey(optionIndex))
                        {
                            bonusOptionsDictionary.Add(optionIndex, new OptionDataPerCard(numberPerCard, target));
                        }
                        else
                        {
                            Debug.LogWarning($"{optionType} bonus option {optionIndex} already exists in the dictionary.");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Failed to parse 'numberPerCard' or 'target' for {optionType} bonus option {optionIndex}.");
                    }
                }
                else
                {
                    Debug.LogError($"{optionType} bonus option {optionIndex} is missing 'numberPerCard' or 'target'.");
                }

                optionIndex++;
            }
        }
    }


    private async Task<int> CalculateValueFromHand(string playerId, string cardType)
    {
        int cardCount = 0;

        var dbRefPlayerDeck = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("deck");

        var snapshot = await dbRefPlayerDeck.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.LogError("No deck data found for the player.");
            errorPanelController.ShowError("general_error");
            return cardCount;
        }

        foreach (var cardSnapshot in snapshot.Children)
        {
            var onHandSnapshot = cardSnapshot.Child("onHand");
            var playedSnapshot = cardSnapshot.Child("played");
            var cardIdSnapshot = cardSnapshot.Child("cardId");

            if (onHandSnapshot.Exists && playedSnapshot.Exists && cardIdSnapshot.Exists)
            {
                if (bool.TryParse(onHandSnapshot.Value.ToString(), out bool onHand) && onHand &&
                    bool.TryParse(playedSnapshot.Value.ToString(), out bool played) && !played)
                {
                    string cardId = cardIdSnapshot.Value.ToString();
                    string type = await GetCardType(cardId);

                    if (type == cardType)
                    {
                        cardCount++;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Missing data in card snapshot for player {playerId}, card: {cardSnapshot.Key}");
            }
        }

        return cardCount;
    }

    private async Task<string> GetCardType(string cardId)
    {
        string cardType = cardId.Substring(0, 2);

        switch (cardType)
        {
            case "AD":
                cardType = "addRemove";
                break;
            case "AS":
                cardType = "asMuchAs";
                break;
            case "CA":
                cardType = "cards";
                break;
            case "OP":
                cardType = "options";
                break;
            case "RA":
                cardType = "random";
                break;
            case "UN":
                cardType = "unique";
                break;
            default:
                cardType = "";
                break;

        }

        var dbRefCard = FirebaseInitializer.DatabaseReference
            .Child("cards")
            .Child("id")
            .Child(cardType)
            .Child(cardId);

        var snapshot = await dbRefCard.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.LogError($"Card with cardId: {cardId} not found.");
            return null;
        }

        var typeSnapshot = snapshot.Child("type");
        if (typeSnapshot.Exists)
        {
            return typeSnapshot.Value.ToString();
        }
        else
        {
            Debug.LogWarning($"Type not found for cardId: {cardId}");
            return null;
        }
    }


}

public class OptionDataPerCard
{
    public int NumberPerCard { get; }
    public string Target { get; }

    public OptionDataPerCard(int numberPerCard, string target)
    {
        NumberPerCard = numberPerCard;
        Target = target;
    }


}