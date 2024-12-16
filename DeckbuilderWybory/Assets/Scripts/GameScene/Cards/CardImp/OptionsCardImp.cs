using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using UnityEngine;

public class OptionsCardImp : MonoBehaviour
{
    private readonly string lobbyId = DataTransfer.LobbyId;
    private readonly string playerId = DataTransfer.PlayerId;

    public PlayerListManager playerListManager;
    public MapManager mapManager;
    public CardUtilities cardUtilities;
    public ErrorPanelController errorPanelController;
    public HistoryController historyController;
    void Start()
    {
        playerListManager.Initialize(lobbyId, playerId);
    }


    public async void CardLibrary(string instanceId, string cardIdDropped, bool ignoreCost)
    {
        bool cardLimitReached = await cardUtilities.CheckCardLimit(playerId);

        if (cardLimitReached)
        {
            Debug.Log("Limit kart w turze to 1");
            errorPanelController.ShowError("card_limit");
            return;
        }

        DatabaseReference dbRefCard, dbRefPlayerStats, dbRefPlayerDeck;
        int cost, playerBudget, chosenRegion = -1;
        string cardType, enemyId = string.Empty;
        bool budgetChange = false, supportChange = false, isBonusRegion = false;

        Dictionary<int, OptionDataRandom> budgetOptions = new();
        Dictionary<int, OptionDataRandom> budgetBonusOptions = new();
        Dictionary<int, OptionDataRandom> incomeOptions = new();
        Dictionary<int, OptionDataRandom> incomeBonusOptions = new();
        Dictionary<int, OptionDataRandom> supportOptions = new();
        Dictionary<int, OptionDataRandom> supportBonusOptions = new();

        budgetOptions.Clear();
        incomeOptions.Clear();
        supportOptions.Clear();
        budgetBonusOptions.Clear();
        supportBonusOptions.Clear();
        incomeBonusOptions.Clear();

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            errorPanelController.ShowError("general_error");
            return;
        }

        dbRefCard = FirebaseInitializer.DatabaseReference.Child("cards").Child("id").Child("options").Child(cardIdDropped);
        DataSnapshot snapshot = await dbRefCard.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.LogError($"No data for: {cardIdDropped}.");
            errorPanelController.ShowError("general_error");
            return;
        }

        if (snapshot.Child("cost").Exists)
        {
            cost = Convert.ToInt32(snapshot.Child("cost").Value);
        }
        else
        {
            Debug.LogError("Branch cost does not exist");
            errorPanelController.ShowError("general_error");
            return;

        }

        if (snapshot.Child("type").Exists)
        {
            cardType = snapshot.Child("type").Value.ToString();
        }
        else
        {
            Debug.LogError("Branch type does not exist");
            errorPanelController.ShowError("general_error");
            return;
        }

        List<string> descriptions = new List<string>
{
    snapshot.Child("playDescriptionPositive").Exists ? snapshot.Child("playDescriptionPositive").Value.ToString() : string.Empty,
    snapshot.Child("playDescriptionNegative").Exists ? snapshot.Child("playDescriptionNegative").Value.ToString() : string.Empty
};

        if (descriptions[0] == string.Empty)
        {
            Debug.LogError("B��d w pobieraniu warto�ci playDescriptionPositive");
            errorPanelController.ShowError("general_error");
            return;
        }


        if (DataTransfer.IsFirstCardInTurn)
        {
            if (await cardUtilities.CheckIncreaseCost(playerId))
            {
                cost = AdjustCost(cost, 1.5);
            }
        }

        if (await cardUtilities.CheckIncreaseCostAllTurn(playerId))
        {
            cost = AdjustCost(cost, 1.5);
        }

        if (await cardUtilities.CheckDecreaseCost(playerId))
        {
            cost = AdjustCost(cost, 0.5, true);
        }

        if (snapshot.Child("budget").Exists)
        {
            budgetChange = true;
            ProcessBonusOptions(snapshot.Child("budget"), budgetBonusOptions);
            ProcessOptions(snapshot.Child("budget"), budgetOptions);
        }

        if (snapshot.Child("support").Exists)
        {
            supportChange = true;

            if (await cardUtilities.CheckSupportBlock(playerId))
            {
                Debug.Log("support block");
                errorPanelController.ShowError("action_blocked");
                return;
            }

            ProcessBonusOptions(snapshot.Child("support"), supportBonusOptions);
            ProcessOptions(snapshot.Child("support"), supportOptions);
        }

        dbRefPlayerStats = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("stats");
        DataSnapshot playerStatsSnapshot = await dbRefPlayerStats.GetValueAsync();

        if (!playerStatsSnapshot.Exists)
        {
            Debug.LogError($"No data for player {playerId}.");
            errorPanelController.ShowError("general_error");
            return;
        }

        DataSnapshot moneySnapshot = playerStatsSnapshot.Child("money");
        playerBudget = moneySnapshot.Exists ? Convert.ToInt32(moneySnapshot.Value) : -1;

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

        ignoreCost = await cardUtilities.CheckIgnoreCost(playerId);

        if (!(await cardUtilities.CheckBlockedCard(playerId)))
        {
            if (!ignoreCost)
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
                (budgetChange,playerBudget, ignoreCost, isBonusRegion, enemyId, descriptions) = await SupportAction(budgetChange, descriptions, playerBudget, cardIdDropped, ignoreCost, chosenRegion, isBonusRegion, cardType, supportOptions, supportBonusOptions, enemyId, budgetOptions);
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

            if (budgetChange)
            {
                (dbRefPlayerStats, playerBudget, descriptions, enemyId) = await BudgetAction(dbRefPlayerStats, descriptions, isBonusRegion, budgetOptions, budgetBonusOptions, playerBudget, enemyId);
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
        cardLimitReached = await cardUtilities.CheckCardLimit(playerId);

        await historyController.AddCardToHistory(cardIdDropped, playerId, descriptions[0], enemyId);
    }

    private int AdjustCost(int cost, double multiplier, bool decrease = false)
    {
        if (cost < 0)
        {
            Debug.LogWarning("Cost is negative, not adjusting cost.");
            return cost;
        }

        double adjustedCost = multiplier * cost;

        if (decrease)
        {
            return cost % 2 != 0 ? (int)Math.Floor(adjustedCost) : (int)adjustedCost;
        }

        return cost % 2 != 0 ? (int)Math.Ceiling(adjustedCost) : (int)adjustedCost;
    }


    private async Task<(DatabaseReference dbRefPlayerStats, int playerBudget, List<string>, string)> BudgetAction(
   DatabaseReference dbRefPlayerStats, List<string> descriptions,
   bool isBonusRegion,
   Dictionary<int, OptionDataRandom> budgetOptionsDictionary,
   Dictionary<int, OptionDataRandom> budgetBonusOptionsDictionary,
   int playerBudget,
   string enemyId)
    {
        var optionsToApply = isBonusRegion ? budgetBonusOptionsDictionary : budgetOptionsDictionary;
        (optionsToApply, descriptions) = RandomizeOption(optionsToApply, descriptions);

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No options to apply.");
            errorPanelController.ShowError("general_error");
            return (dbRefPlayerStats, -1, descriptions, enemyId);
        }

        if (isBonusRegion)
        {
            Debug.Log("Bonus region detected.");
        }

        foreach (var data in optionsToApply.Values)
        {
            if (data.Target == "player")
            {
                if (!DataTransfer.IsPlayerTurn)
                {
                    errorPanelController.ShowError("turn_over");
                    return (dbRefPlayerStats, -1, descriptions, enemyId);
                }
                if (playerBudget + data.Number < 0)
                {
                    Debug.LogWarning("Brak wystarczającego budżetu aby zagrać kartę.");
                    errorPanelController.ShowError("no_budget");
                    return (dbRefPlayerStats, -1, descriptions, enemyId);
                }
                playerBudget += data.Number;
                await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
                await cardUtilities.CheckAndAddCopyBudget(playerId, data.Number);
            }
            else if (data.Target == "enemy")
            {
                if (string.IsNullOrEmpty(enemyId))
                {
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (dbRefPlayerStats, -1, descriptions, enemyId);
                    }
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("Failed to select an enemy player.");
                        errorPanelController.ShowError("general_error");
                        return (dbRefPlayerStats, -1, descriptions, enemyId);
                    }
                }
                if (!DataTransfer.IsPlayerTurn)
                {
                    errorPanelController.ShowError("turn_over");
                    return (dbRefPlayerStats, -1, descriptions, enemyId);
                }
                playerBudget = await cardUtilities.ChangeEnemyStat(enemyId, data.Number, "money", playerBudget);
                if(playerBudget == -1)
                {
                    return (dbRefPlayerStats, -1, descriptions, enemyId);
                }
                await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
            }
        }

        return (dbRefPlayerStats, playerBudget, descriptions, enemyId);
    }

    private async Task<(bool,int playerBudget, bool ignoreCost, bool isBonusRegion, string enemyId, List<string>)> SupportAction(
          bool budgetChange, List<string> descriptions,
          int playerBudget,
          string cardId,
          bool ignoreCost,
          int chosenRegion,
          bool isBonusRegion,
          string cardType,
          Dictionary<int, OptionDataRandom> supportOptionsDictionary,
          Dictionary<int, OptionDataRandom> supportBonusOptionsDictionary,
          string enemyId,
          Dictionary<int, OptionDataRandom> budgetOptionsDictionary)
    {
        if (cardId == "OP011")
        {
            if (!DataTransfer.IsPlayerTurn)
            {
                errorPanelController.ShowError("turn_over");
                return  (budgetChange, -1, false, false, null, descriptions);
            }
            chosenRegion = await mapManager.SelectArea();
            isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
        }

        var optionsToApply = isBonusRegion ? supportBonusOptionsDictionary : supportOptionsDictionary;

        if (cardId == "OP013")
        {
            if (!DataTransfer.IsPlayerTurn)
            {
                errorPanelController.ShowError("turn_over");
                return  (budgetChange, -1, false, false, null, descriptions);
            }
            chosenRegion = await mapManager.SelectArea();
            isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
        }
        if (!DataTransfer.IsPlayerTurn)
        {
            errorPanelController.ShowError("turn_over");
            return (budgetChange, -1, false, false, null, descriptions);
        }
        if (cardId == "OP006")
        {

            descriptions = CheckBudget(ref optionsToApply, playerBudget, descriptions);
        }
        else
        {
            (optionsToApply, descriptions) = RandomizeOption(optionsToApply, descriptions);
        }

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No options to apply.");
            errorPanelController.ShowError("general_error");
            return (budgetChange,-1, false, false, null, descriptions);

        }

        if (isBonusRegion)
        {
            Debug.Log("Bonus region detected.");
        }

        foreach (var data in optionsToApply.Values)
        {
            switch (data.Target)
            {
                case "enemy-random":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (budgetChange, -1, false, false, null, descriptions);
                    }
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("Failed to select an enemy player.");
                        errorPanelController.ShowError("general_error");
                        return (budgetChange, playerBudget, ignoreCost, isBonusRegion, enemyId, descriptions);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (budgetChange, -1, false, false, null, descriptions);
                    }
                    chosenRegion = await cardUtilities.RandomizeRegion(enemyId, data.Number, mapManager);
                    if(chosenRegion == -1)
                    {
                        Debug.LogError("Failed to randomize region.");
                        return (budgetChange, playerBudget, ignoreCost, isBonusRegion, enemyId, descriptions);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (budgetChange, -1, false, false, null, descriptions);
                    }
                    bool errorCheck = await cardUtilities.ChangeSupport(enemyId, data.Number, chosenRegion, cardId, mapManager);
                    if(errorCheck)
                    {
                        return (budgetChange, playerBudget, ignoreCost, isBonusRegion, enemyId, descriptions);
                    }
                    RemoveTargetOption(ref budgetOptionsDictionary, "player");
                    break;

                case "player-random":
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (budgetChange, -1, false, false, null, descriptions);
                    }
                    chosenRegion = await cardUtilities.RandomizeRegion(playerId, data.Number, mapManager);
                    if (chosenRegion == -1)
                    {
                        Debug.LogError("Failed to randomize region.");
                        return (budgetChange, playerBudget, ignoreCost, isBonusRegion, enemyId, descriptions);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (budgetChange, -1, false, false, null, descriptions);
                    }
                    errorCheck = await cardUtilities.ChangeSupport(playerId, data.Number, chosenRegion, cardId, mapManager);
                    if (errorCheck)
                    {
                        return (budgetChange, playerBudget, ignoreCost, isBonusRegion, enemyId, descriptions);
                    }
                    RemoveTargetOption(ref budgetOptionsDictionary, "enemy");
                    break;

                case "enemy-region":
                    if (chosenRegion < 0)
                    {
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (budgetChange, -1, false, false, null, descriptions);
                        }
                        chosenRegion = await mapManager.SelectArea();
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (budgetChange, -1, false, false, null, descriptions);
                    }
                    enemyId = await playerListManager.SelectEnemyPlayerInArea(chosenRegion);
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("No enemy player found in the area.");
                        errorPanelController.ShowError("general_error");
                        return (budgetChange, -1, false, false, null, descriptions);
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (budgetChange, -1, false, false, null, descriptions);
                    }
                    errorCheck = await cardUtilities.ChangeSupport(enemyId, data.Number, chosenRegion, cardId, mapManager);
                    if(errorCheck)
                    {
                        return (budgetChange, playerBudget, ignoreCost, isBonusRegion, enemyId, descriptions);
                    }
                    break;

                case "player-region":
                    if (chosenRegion < 0)
                    {
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (budgetChange, -1, false, false, null, descriptions);
                        }
                        chosenRegion = await mapManager.SelectArea();
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (budgetChange, -1, false, false, null, descriptions);
                    }
                    errorCheck = await cardUtilities.ChangeSupport(playerId, data.Number, chosenRegion, cardId, mapManager);

                    if(errorCheck)
                    {
                        return (budgetChange, playerBudget, ignoreCost, isBonusRegion, enemyId, descriptions);
                    }

                    if (cardId == "OP011")
                    {
                        if (!DataTransfer.IsPlayerTurn)
                        {
                            errorPanelController.ShowError("turn_over");
                            return (budgetChange, -1, false, false, null, descriptions);
                        }
                        ignoreCost = UnityEngine.Random.Range(0, 2) == 0;
                        if (ignoreCost)
                        {
                            if (descriptions[1] != string.Empty)
                            {
                                descriptions[0] = descriptions[1];
                                descriptions.RemoveAt(1);
                            }
                        }
                    }
                    if (!DataTransfer.IsPlayerTurn)
                    {
                        errorPanelController.ShowError("turn_over");
                        return (budgetChange, -1, false, false, null, descriptions);
                    }
                    if (cardId == "OP013") { budgetChange = false; }
                    break;

                default:
                    Debug.LogError($"Unsupported target type: {data.Target}");
                    errorPanelController.ShowError("general_error");
                    break;
            }
        }

        return (budgetChange, playerBudget, ignoreCost, isBonusRegion, enemyId, descriptions);
    }


    public void ProcessOptions(DataSnapshot snapshot, Dictionary<int, OptionDataRandom> optionsDictionary)
    {
        foreach (var optionSnapshot in snapshot.Children)
        {
            if (optionSnapshot.Key != "bonus")
            {
                ProcessOption(optionSnapshot, optionsDictionary);
            }
        }
    }

    public void ProcessBonusOptions(DataSnapshot snapshot, Dictionary<int, OptionDataRandom> bonusOptionsDictionary)
    {
        DataSnapshot bonusSnapshot = snapshot.Child("bonus");
        if (bonusSnapshot.Exists)
        {
            int optionIndex = 0;
            foreach (var optionSnapshot in bonusSnapshot.Children)
            {
                ProcessOption(optionSnapshot, bonusOptionsDictionary, optionIndex);
                optionIndex++;
            }
        }
    }

    private void ProcessOption(DataSnapshot optionSnapshot, Dictionary<int, OptionDataRandom> optionsDictionary, int? optionIndex = null)
    {
        DataSnapshot numberSnapshot = optionSnapshot.Child("number");
        DataSnapshot targetSnapshot = optionSnapshot.Child("target");
        DataSnapshot percentSnapshot = optionSnapshot.Child("percent");

        if (numberSnapshot.Exists && targetSnapshot.Exists)
        {
            int number = Convert.ToInt32(numberSnapshot.Value);
            string target = targetSnapshot.Value.ToString();
            int percent = percentSnapshot.Exists ? Convert.ToInt32(percentSnapshot.Value) : 0;

            int optionKey = optionIndex ?? Convert.ToInt32(optionSnapshot.Key.Replace("option", ""));
            optionsDictionary.Add(optionKey, new OptionDataRandom(number, percent, target));
        }
        else
        {
            string optionId = optionIndex?.ToString() ?? optionSnapshot.Key;
            Debug.LogError($"Option {optionId} is missing 'number' or 'target'.");
        }
    }

    public (Dictionary<int, OptionDataRandom>, List<string>) RandomizeOption(Dictionary<int, OptionDataRandom> optionsDictionary, List<string> descriptions)
    {

        if (optionsDictionary.Count == 1)
        {
            return (new Dictionary<int, OptionDataRandom> { { 0, optionsDictionary.First().Value } }, descriptions);
        }

        if (optionsDictionary.Count == 2)
        {
            int randomIndex = UnityEngine.Random.Range(0, 2);
            var chosenOption = randomIndex == 0 ? optionsDictionary.First().Value : optionsDictionary.Last().Value;
            if (chosenOption.Number < 0 || chosenOption.Target == "player" || chosenOption.Target == "player-random" || chosenOption.Target == "player-region")
            {
                if (descriptions[1] != string.Empty)
                {
                    descriptions[0] = descriptions[1];
                    descriptions.RemoveAt(1);
                }

                return (new Dictionary<int, OptionDataRandom> { { 0, chosenOption } }, descriptions);
            }

            // Je�li 'number' w wybranej opcji nie jest ujemne, zwracamy j�
            return (new Dictionary<int, OptionDataRandom> { { 0, chosenOption } }, descriptions);
        }

        Debug.LogError("Options dictionary must contain either 1 or 2 options.");
        return (new Dictionary<int, OptionDataRandom> { { 0, optionsDictionary.First().Value } }, descriptions);
    }

    public void RemoveTargetOption(ref Dictionary<int, OptionDataRandom> optionsDictionary, string targetToRemove)
    {
        if (optionsDictionary != null && optionsDictionary.Count > 0)
        {
            var optionToRemove = optionsDictionary.FirstOrDefault(kvp => kvp.Value.Target == targetToRemove);

            if (optionToRemove.Key != 0)
            {
                optionsDictionary.Remove(optionToRemove.Key);
            }
            else
            {
                Debug.Log($"No option found with target '{targetToRemove}'.");
            }
        }
        else
        {
            Debug.Log("Options dictionary is empty.");
        }
    }

    public List<string> CheckBudget(ref Dictionary<int, OptionDataRandom> supportOptionsDictionary, int playerBudget, List<string> descriptions)
    {
        int targetNumber;
        if (playerBudget > 30)
        {
            targetNumber = -2;
        }
        else
        {
            targetNumber = -4;
            if (descriptions[1] != string.Empty)
            {
                descriptions[0] = descriptions[1];
                descriptions.RemoveAt(1);
            }
        }
        RemoveOptionByNumber(ref supportOptionsDictionary, targetNumber);

        return descriptions;
    }

    private void RemoveOptionByNumber(ref Dictionary<int, OptionDataRandom> optionsDictionary, int number)
    {
        var optionToRemove = optionsDictionary.FirstOrDefault(kvp => kvp.Value.Number == number);

        if (optionToRemove.Key != 0)
        {
            optionsDictionary.Remove(optionToRemove.Key);
        }
        else
        {
            Debug.Log($"No option found with 'number' == {number}.");
        }
    }

}

public class OptionDataRandom
{
    public int Number { get; }
    public int Percent { get; }
    public string Target { get; }

    public OptionDataRandom(int number, int percent, string target)
    {
        Number = number;
        Percent = percent;
        Target = target;
    }
}