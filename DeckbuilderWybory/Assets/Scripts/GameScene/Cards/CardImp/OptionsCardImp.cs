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
            Debug.LogError("B³¹d w pobieraniu wartoœci playerBudget");
            errorPanelController.ShowError("general_error");
            return;
        }

        if (!ignoreCost && playerBudget < cost)
        {
            Debug.LogError("Brak bud¿etu aby zagraæ kartê.");
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
                (playerBudget, ignoreCost, isBonusRegion, enemyId) = await SupportAction(budgetChange, playerBudget, cardIdDropped, ignoreCost, chosenRegion, isBonusRegion, cardType, supportOptions, supportBonusOptions, enemyId, budgetOptions);
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
                (dbRefPlayerStats, playerBudget) = await BudgetAction(dbRefPlayerStats, isBonusRegion, budgetOptions, budgetBonusOptions, playerBudget, enemyId);
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
            }
        } else
        {
            Debug.Log("Karta zosta³a zablokowana");
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


    private async Task<(DatabaseReference dbRefPlayerStats, int playerBudget)> BudgetAction(
    DatabaseReference dbRefPlayerStats,
    bool isBonusRegion,
    Dictionary<int, OptionDataRandom> budgetOptionsDictionary,
    Dictionary<int, OptionDataRandom> budgetBonusOptionsDictionary,
    int playerBudget,
    string enemyId)
    {
        var optionsToApply = isBonusRegion ? budgetBonusOptionsDictionary : budgetOptionsDictionary;
        optionsToApply = RandomizeOption(optionsToApply);

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No options to apply.");
            errorPanelController.ShowError("general_error");
            return (dbRefPlayerStats, -1);
        }

        if (isBonusRegion)
        {
            Debug.Log("Bonus region detected.");
        }

        foreach (var data in optionsToApply.Values)
        {
            if (data.Target == "player")
            {
                if(playerBudget < data.Number)
                {
                    Debug.LogWarning("Brak wystarczaj¹cego bud¿etu aby zagraæ kartê.");
                    errorPanelController.ShowError("no_budget");
                    return (dbRefPlayerStats, -1); 
                }
                playerBudget += data.Number;
                await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
                await cardUtilities.CheckAndAddCopyBudget(playerId, data.Number);
            }
            else if (data.Target == "enemy")
            {
                if (string.IsNullOrEmpty(enemyId))
                {
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("Failed to select an enemy player.");
                        errorPanelController.ShowError("general_error");
                        return (dbRefPlayerStats, -1);
                    }
                }
                playerBudget = await cardUtilities.ChangeEnemyStat(enemyId, data.Number, "money", playerBudget);
                await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
            }
        }

        return (dbRefPlayerStats, playerBudget);
    }

    private async Task<(int playerBudget, bool ignoreCost, bool isBonusRegion, string enemyId)> SupportAction(
        bool budgetChange,
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
        if (cardId == "OP011" || cardId == "OP013")
        {
            chosenRegion = await mapManager.SelectArea();
            isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
        }

        var optionsToApply = isBonusRegion ? supportBonusOptionsDictionary : supportOptionsDictionary;

        if (cardId == "OP006")
        {
            CheckBudget(ref optionsToApply, playerBudget);
        }
        else
        {
            optionsToApply = RandomizeOption(optionsToApply);
        }

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No options to apply.");
            errorPanelController.ShowError("general_error");
            return (-1, false, false, null);
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
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("Failed to select an enemy player.");
                        errorPanelController.ShowError("general_error");
                        return (playerBudget, ignoreCost, isBonusRegion, enemyId);
                    }
                    chosenRegion = await cardUtilities.RandomizeRegion(enemyId, data.Number, mapManager);
                    if(chosenRegion == -1)
                    {
                        Debug.LogError("Failed to randomize region.");
                        return (playerBudget, ignoreCost, isBonusRegion, enemyId);
                    }
                    bool errorCheck = await cardUtilities.ChangeSupport(enemyId, data.Number, chosenRegion, cardId, mapManager);
                    if(errorCheck)
                    {
                        return (playerBudget, ignoreCost, isBonusRegion, enemyId);
                    }
                    RemoveTargetOption(ref budgetOptionsDictionary, "player");
                    break;

                case "player-random":
                    chosenRegion = await cardUtilities.RandomizeRegion(playerId, data.Number, mapManager);
                    if (chosenRegion == -1)
                    {
                        Debug.LogError("Failed to randomize region.");
                        return (playerBudget, ignoreCost, isBonusRegion, enemyId);
                    }
                    errorCheck = await cardUtilities.ChangeSupport(playerId, data.Number, chosenRegion, cardId, mapManager);
                    if (errorCheck)
                    {
                        return (playerBudget, ignoreCost, isBonusRegion, enemyId);
                    }
                    RemoveTargetOption(ref budgetOptionsDictionary, "enemy");
                    break;

                case "enemy-region":
                    if (chosenRegion < 0)
                    {
                        chosenRegion = await mapManager.SelectArea();
                    }
                    enemyId = await playerListManager.SelectEnemyPlayerInArea(chosenRegion);
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("No enemy player found in the area.");
                        errorPanelController.ShowError("general_error");
                        return (-1, false, false, null);
                    }

                    errorCheck = await cardUtilities.ChangeSupport(enemyId, data.Number, chosenRegion, cardId, mapManager);
                    if(errorCheck)
                    {
                       return (playerBudget, ignoreCost, isBonusRegion, enemyId);
                    }
                    break;

                case "player-region":
                    if (chosenRegion < 0)
                    {
                        chosenRegion = await mapManager.SelectArea();
                    }

                     errorCheck = await cardUtilities.ChangeSupport(playerId, data.Number, chosenRegion, cardId, mapManager);

                    if(errorCheck)
                    {
                        return (playerBudget, ignoreCost, isBonusRegion, enemyId);
                    }

                    if (cardId == "OP011") { ignoreCost = UnityEngine.Random.Range(0, 2) == 0; }
                    if (cardId == "OP013") { budgetChange = false; }
                    break;

                default:
                    Debug.LogError($"Unsupported target type: {data.Target}");
                    errorPanelController.ShowError("general_error");
                    break;
            }
        }

        return (playerBudget, ignoreCost, isBonusRegion, enemyId);
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

    public Dictionary<int, OptionDataRandom> RandomizeOption(Dictionary<int, OptionDataRandom> optionsDictionary)
    {

        if (optionsDictionary.Count == 1)
        {
            return new Dictionary<int, OptionDataRandom> { { 0, optionsDictionary.First().Value } };
        }

        if (optionsDictionary.Count == 2)
        {
            int randomIndex = UnityEngine.Random.Range(0, 2);
            var chosenOption = randomIndex == 0 ? optionsDictionary.First().Value : optionsDictionary.Last().Value;
            return new Dictionary<int, OptionDataRandom> { { 0, chosenOption } };
        }

        Debug.LogError("Options dictionary must contain either 1 or 2 options.");
        return new Dictionary<int, OptionDataRandom>();
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

    public void CheckBudget(ref Dictionary<int, OptionDataRandom> supportOptionsDictionary, int playerBudget)
    {
        int targetNumber = playerBudget > 30 ? -2 : -4;
        RemoveOptionByNumber(ref supportOptionsDictionary, targetNumber);
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