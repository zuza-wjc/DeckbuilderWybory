using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using UnityEngine;

public class AddRemoveCardImp : MonoBehaviour
{
    private DatabaseReference dbRefCard;
    private DatabaseReference dbRefPlayerStats;
    private DatabaseReference dbRefPlayerDeck;
    private DatabaseReference dbRefEnemyStats;
    private DatabaseReference dbRefSupport;
    private DatabaseReference dbRefAllPlayersStats;
    private DatabaseReference dbRefRounds;

    private readonly string lobbyId = DataTransfer.LobbyId;
    private readonly string playerId = DataTransfer.PlayerId;

    private bool budgetChange;
    private bool incomeChange;
    private bool supportChange;
    private int roundChange;

    private int cost;
    private int playerBudget;
    private int playerIncome;
    private int support;
    private string enemyId;
    private string cardType;

    private int chosenRegion;
    private int maxAreaSupport;
    private int currentAreaSupport;
    private bool isBonusRegion;

    private Dictionary<int, OptionData> budgetOptionsDictionary = new();
    private Dictionary<int, OptionData> budgetBonusOptionsDictionary = new();
    private Dictionary<int, OptionData> incomeOptionsDictionary = new();
    private Dictionary<int, OptionData> incomeBonusOptionsDictionary = new();
    private Dictionary<int, OptionData> supportOptionsDictionary = new();
    private Dictionary<int, OptionData> supportBonusOptionsDictionary = new();

    public PlayerListManager playerListManager;
    public MapManager mapManager;

    void Start()
    {
        playerListManager.Initialize(lobbyId, playerId);
    }


    public async void CardLibrary(string cardIdDropped)
    {

        budgetChange = false;
        incomeChange = false;
        supportChange = false;
        isBonusRegion = false;
        roundChange = 0;

        cost = 0;
        playerBudget = 0;
        playerIncome = 0;
        support = 0;
        enemyId = string.Empty;
        cardType = string.Empty;

        chosenRegion = -1;
        maxAreaSupport = 0;
        currentAreaSupport = 0;

        budgetOptionsDictionary.Clear();
        incomeOptionsDictionary.Clear();
        supportOptionsDictionary.Clear();
        budgetBonusOptionsDictionary.Clear();
        supportBonusOptionsDictionary.Clear();
        incomeBonusOptionsDictionary.Clear();

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRefCard = FirebaseInitializer.DatabaseReference.Child("cards").Child("id").Child("addRemove").Child(cardIdDropped);

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

            DataSnapshot roundsSnapshot = snapshot.Child("rounds");
            if (roundsSnapshot.Exists)
            {
                roundChange = Convert.ToInt32(roundsSnapshot.Value);
            }

            DataSnapshot budgetSnapshot = snapshot.Child("budget");
            if (budgetSnapshot.Exists)
            {
                budgetChange = true;

                DataSnapshot bonusSnapshot = budgetSnapshot.Child("bonus");
                if (bonusSnapshot.Exists)
                {
                    ProcessBonusOptions(bonusSnapshot, optionType: "money");
                }

                foreach (var optionSnapshot in budgetSnapshot.Children)
                {
                    if (optionSnapshot.Key != "bonus")
                    {
                        ProcessOptions(optionSnapshot, optionType: "money");
                    }
                }
            }

            DataSnapshot incomeSnapshot = snapshot.Child("income");
            if (incomeSnapshot.Exists)
            {
                incomeChange = true;

                DataSnapshot bonusSnapshot = incomeSnapshot.Child("bonus");
                if (bonusSnapshot.Exists)
                {
                    ProcessBonusOptions(bonusSnapshot, optionType: "income");
                }

                foreach (var optionSnapshot in incomeSnapshot.Children)
                {
                    if (optionSnapshot.Key != "bonus") 
                    {
                        ProcessOptions(optionSnapshot, optionType: "income");
                    }
                }
            }

            DataSnapshot supportSnapshot = snapshot.Child("support");
            if (supportSnapshot.Exists)
            {
                supportChange = true;

                int optionIndex = 0;
                foreach (var optionSnapshot in supportSnapshot.Children)
                {
                    DataSnapshot numberSnapshot = optionSnapshot.Child("number");
                    DataSnapshot targetSnapshot = optionSnapshot.Child("target");
                    DataSnapshot targetNumberSnapshot = optionSnapshot.Child("targetNumber");

                    if (numberSnapshot.Exists && targetSnapshot.Exists)
                    {
                        int number = Convert.ToInt32(numberSnapshot.Value);
                        string target = targetSnapshot.Value.ToString();

                        int targetNumber;

                        if (targetNumberSnapshot.Exists)
                        {
                            targetNumber = Convert.ToInt32(targetNumberSnapshot.Value);
                        }
                        else
                        {
                            targetNumber = 1;
                        }

                        supportOptionsDictionary.Add(optionIndex, new OptionData(number, target, targetNumber));
                    }
                    else
                    {
                        Debug.LogError($"Option {optionIndex} is missing 'number' or 'target'.");
                        return;
                    }

                    optionIndex++;
                }
            }
        }
        else
        {
            Debug.LogError("No data for: " + cardIdDropped + ".");
            return;
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

            DataSnapshot incomeSnapshot = playerStatsSnapshot.Child("income");
            if (incomeSnapshot.Exists)
            {
                playerIncome = Convert.ToInt32(incomeSnapshot.Value);
            }
            else
            {
                Debug.LogError("Branch income does not exist.");
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

        if (budgetChange)
        {
            await BudgetAction();
        }

        if (incomeChange)
        {
            await IncomeAction();
        }

        if(roundChange != 0)
        {
            await RoundAction();
        }

        await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget - cost);

        dbRefPlayerDeck = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck").Child(cardIdDropped);

        await dbRefPlayerDeck.Child("onHand").SetValueAsync(false);
        await dbRefPlayerDeck.Child("played").SetValueAsync(true);
    }

    private void ProcessOptions(DataSnapshot optionSnapshot, string optionType)
    {
        var optionsDictionary = optionType == "income" ? incomeOptionsDictionary : budgetOptionsDictionary;

        DataSnapshot numberSnapshot = optionSnapshot.Child("number");
        DataSnapshot targetSnapshot = optionSnapshot.Child("target");
        DataSnapshot targetNumberSnapshot = optionSnapshot.Child("targetNumber");

        if (numberSnapshot.Exists && targetSnapshot.Exists)
        {
            int number = Convert.ToInt32(numberSnapshot.Value);
            string target = targetSnapshot.Value.ToString();
            int targetNumber = targetNumberSnapshot.Exists ? Convert.ToInt32(targetNumberSnapshot.Value) : 1;

            int optionKey = Convert.ToInt32(optionSnapshot.Key.Replace("option", ""));

            // Dodajemy do odpowiedniego s³ownika
            optionsDictionary.Add(optionKey, new OptionData(number, target, targetNumber));
        }
        else
        {
            Debug.LogError($"Option is missing 'number' or 'target'.");
        }
    }


    private void ProcessBonusOptions(DataSnapshot bonusSnapshot, string optionType)
    {
        int optionIndex = 0;

        // Bonusy s¹ przechowywane w "bonus" z opcjami jako pary (np. "option1", "option2")
        foreach (var optionSnapshot in bonusSnapshot.Children)
        {
            // Pobieramy wartoœci 'number' i 'target' dla bonusu
            DataSnapshot numberSnapshot = optionSnapshot.Child("number");
            DataSnapshot targetSnapshot = optionSnapshot.Child("target");
            DataSnapshot targetNumberSnapshot = optionSnapshot.Child("targetNumber");  // Sprawdzamy targetNumber

            if (numberSnapshot.Exists && targetSnapshot.Exists)
            {
                int number = Convert.ToInt32(numberSnapshot.Value);
                string target = targetSnapshot.Value.ToString();
                int targetNumber = targetNumberSnapshot.Exists ? Convert.ToInt32(targetNumberSnapshot.Value) : 1;  // Domyœlnie 1, jeœli nie ma

                // Dodajemy bonusow¹ opcjê do odpowiedniego s³ownika
                if (optionType == "money")
                {
                    budgetBonusOptionsDictionary.Add(optionIndex, new OptionData(number, target, targetNumber));
                }
                else if (optionType == "income")
                {
                    incomeBonusOptionsDictionary.Add(optionIndex, new OptionData(number, target, targetNumber));
                }
            }
            else
            {
                Debug.LogError($"Bonus option {optionIndex} is missing 'number' or 'target'.");
            }

            optionIndex++;
        }
    }


    private async Task BudgetAction()
    {
        var isBonus = isBonusRegion;
        var optionsToApply = isBonus ? budgetBonusOptionsDictionary : budgetOptionsDictionary;

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No options to apply.");
            return;
        }

        if (isBonus)
        {
            Debug.Log("bonus jest zagrany");
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
                playerBudget += data.Number;
            }
            else if (data.Target == "enemy")
            {

                if (data.TargetNumber == 7)
                {

                    await ChangeAllStats(data.Number, playerId, "money");
                }
                else
                {
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        enemyId = await playerListManager.SelectEnemyPlayer();
                        if (string.IsNullOrEmpty(enemyId))
                        {
                            Debug.LogError("Failed to select an enemy player.");
                            return;
                        }
                    }
                    await ChangeEnemyStat(enemyId, data.Number, "money");
                }
            }
        }
    }

    private async Task SupportAction(string cardId)
    {
        if (supportOptionsDictionary?.Values == null || !supportOptionsDictionary.Values.Any())
        {
            Debug.LogError("No support options available.");
            return;
        }

        foreach (var data in supportOptionsDictionary.Values)
        {
            if (data == null)
            {
                Debug.LogWarning("Encountered null data in support options, skipping.");
                continue;
            }

            if (data.Target == "enemy-region")
            {
                if (data.TargetNumber == 8)
                {
                    await ChangeAllSupport(data.Number);
                }
                else
                {
                    chosenRegion = await mapManager.SelectArea();

                    isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);

                    if (data.TargetNumber == 7)
                    {
                        await ChangeAreaSupport(chosenRegion, data.Number, playerId);
                    }
                    else
                    {
                        enemyId = await playerListManager.SelectEnemyPlayerInArea(chosenRegion);
                        if (string.IsNullOrEmpty(enemyId))
                        {
                            Debug.LogError("No enemy player found in the area.");
                            return;
                        }
                        await ChangeSupport(enemyId, data.Number, chosenRegion, cardId);
                    }
                }
            }
            else if (data.Target == "player-region")
            {
                if (chosenRegion < 0)
                {
                    chosenRegion = await mapManager.SelectArea();
                }

                isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);

                await ChangeSupport(playerId, data.Number, chosenRegion, cardId);
            }
            else if (data.Target == "player-random")
            {
                chosenRegion = await RandomizeRegion(playerId, data.Number);
                Debug.Log($"Wylosowany region to ${chosenRegion}");
                await ChangeSupport(playerId, data.Number, chosenRegion, cardId);
            }
            else if (data.Target == "enemy-random")
            {
                enemyId = await playerListManager.SelectEnemyPlayer();
                chosenRegion = await RandomizeRegion(enemyId, data.Number);
                Debug.Log($"Wylosowany region to ${chosenRegion}");
                await ChangeSupport(enemyId, data.Number, chosenRegion, cardId);
            }
        }
    }


    private async Task IncomeAction()
    {
        var optionsToApply = isBonusRegion ? incomeBonusOptionsDictionary : incomeOptionsDictionary;

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogWarning("No income options available.");
            return;
        }

        if (isBonusRegion)
        {
            Debug.Log("Bonus jest zagrany");
        }

        foreach (var data in optionsToApply.Values)
        {
            if (data == null)
            {
                Debug.LogWarning("Encountered null data in income options, skipping.");
                continue;
            }

            if (data.Target == "player")
            {
                playerIncome += data.Number;
                if(playerIncome < 0)
                {
                    Debug.Log("Nie wystaraczaj¹cy przychód aby zagraæ kartê");
                } else
                {
                    await dbRefPlayerStats.Child("income").SetValueAsync(playerIncome);
                } 
            }
            else if (data.Target == "enemy")
            {

                if (data.TargetNumber == 7)
                {
                    await ChangeAllStats(data.Number, playerId, "income");
                }
                else
                {

                    if (string.IsNullOrEmpty(enemyId))
                    {
                        enemyId = await playerListManager.SelectEnemyPlayer();
                        if (string.IsNullOrEmpty(enemyId))
                        {
                            Debug.LogError("Failed to select an enemy player.");
                            return;
                        }
                    }

                    await ChangeEnemyStat(enemyId, data.Number, "income");
                }
            }
            else if (data.Target == "enemy-region")
            {
                if (data.TargetNumber == 7)
                {
                    await ChangeAreaIncome(chosenRegion, data.Number, playerId);
                }
            }
        }
    }


    private async Task RoundAction()
    {
        dbRefRounds = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("rounds");

        try
        {
            DataSnapshot snapshot = await dbRefRounds.GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError("Round data does not exist.");
                return;
            }

            if (snapshot.Value == null)
            {
                Debug.LogError("Round data is null.");
                return;
            }

            if (!int.TryParse(snapshot.Value.ToString(), out int currentRound))
            {
                Debug.LogError("Failed to parse round data as integer.");
                return;
            }

            int updatedRound = currentRound + roundChange;

            await dbRefRounds.SetValueAsync(updatedRound);
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while updating the round: {ex.Message}");
        }
    }

    private async Task ChangeEnemyStat(string enemyId, int value, string statType)
    {
        if (string.IsNullOrEmpty(enemyId))
        {
            Debug.LogError($"Enemy ID is null or empty. ID: {enemyId}");
            return;
        }

        dbRefEnemyStats = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId)
            .Child("stats");

        try
        {
            var snapshot = await dbRefEnemyStats.GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError($"No enemy data found in the database for enemy ID: {enemyId}");
                return;
            }

            var enemyStatSnapshot = snapshot.Child(statType);
            if (!enemyStatSnapshot.Exists)
            {
                Debug.LogError($"Branch '{statType}' does not exist for enemy ID: {enemyId}");
                return;
            }

            if (!int.TryParse(enemyStatSnapshot.Value.ToString(), out int enemyStat))
            {
                Debug.LogError($"Failed to parse '{statType}' value for enemy ID: {enemyId}. Value: {enemyStatSnapshot.Value}");
                return;
            }

            int updatedStat = Math.Max(0, enemyStat + value);

            await dbRefEnemyStats.Child(statType).SetValueAsync(updatedStat);
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while changing the enemy {statType}: {ex.Message}");
        }
    }

    private async Task ChangeAreaIncome(int areaId, int value, string cardholderId)
    {
        dbRefAllPlayersStats = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");

        try
        {
            var snapshot = await dbRefAllPlayersStats.GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError("No data found for players in the session.");
                return;
            }

            foreach (var playerSnapshot in snapshot.Children)
            {
                string playerID = playerSnapshot.Key;

                if (playerID == cardholderId) continue;

                var playerStatsSnapshot = playerSnapshot.Child("stats");
                var playerSupportSnapshot = playerStatsSnapshot.Child("support");
                var playerIncomeSnapshot = playerStatsSnapshot.Child("income");

                if (!playerSupportSnapshot.Exists || !playerIncomeSnapshot.Exists)
                {
                    Debug.LogError($"Missing support or income data for player {playerID}.");
                    continue;
                }

                var supportChildSnapshot = playerSupportSnapshot.Children
                    .FirstOrDefault(s => Convert.ToInt32(s.Key) == areaId);

                if (supportChildSnapshot == null || !int.TryParse(supportChildSnapshot.Value.ToString(), out int currentSupportValue) || currentSupportValue <= 0)
                {
                    continue;
                }

                if (int.TryParse(playerIncomeSnapshot.Value.ToString(), out int currentIncome))
                {
                    int updatedIncome = Math.Max(0, currentIncome + value);

                    await dbRefAllPlayersStats
                        .Child(playerID)
                        .Child("stats")
                        .Child("income")
                        .SetValueAsync(updatedIncome);
                }
                else
                {
                    Debug.LogError($"Invalid income data for player {playerID}.");
                }
            }

        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while updating area income: {ex.Message}");
        }
    }

    private async Task ChangeSupport(string playerId, int value, int areaId, string cardId)
    {
        dbRefSupport = FirebaseInitializer.DatabaseReference
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
            return;
        }

        if (!int.TryParse(snapshot.Value.ToString(), out support))
        {
            Debug.LogError("Failed to parse support value from the database.");
            return;
        }

        var maxSupportTask = mapManager.GetMaxSupportForRegion(areaId);
        var currentSupportTask = mapManager.GetCurrentSupportForRegion(areaId, playerId);

        await Task.WhenAll(maxSupportTask, currentSupportTask);

        maxAreaSupport = await maxSupportTask;
        currentAreaSupport = await currentSupportTask;

        if (cardId == "AD020" || cardId == "AD054")
        {
            if (currentAreaSupport < value)
            {
                Debug.Log("Nie mo¿na zagraæ karty ze wzglêdu na niewystarczaj¹ce poparcie w tym regionie");
                return;
            }
        }

        support += value;

        if (support < 0)
        {
            support = 0;
        }
        else if (currentAreaSupport + support > maxAreaSupport)
        {
            support = maxAreaSupport - currentAreaSupport;
        }

        await dbRefSupport.SetValueAsync(support);
    }

    private async Task ChangeAreaSupport(int areaId, int value, string cardholderId)
    {
        dbRefAllPlayersStats = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");

        var snapshot = await dbRefAllPlayersStats.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.LogError("No data found for players in the session.");
            return;
        }

        foreach (var playerSnapshot in snapshot.Children)
        {
            string playerId = playerSnapshot.Key;

            if (playerId == cardholderId) continue;

            var playerSupportSnapshot = playerSnapshot.Child("stats").Child("support");

            if (!playerSupportSnapshot.Exists)
            {
                Debug.LogError($"Support not found for player {playerId}.");
                continue;
            }

            foreach (var supportChildSnapshot in playerSupportSnapshot.Children)
            {
                if (Convert.ToInt32(supportChildSnapshot.Key) == areaId)
                {
                    if (int.TryParse(supportChildSnapshot.Value.ToString(), out int currentSupportValue))
                    {
                        int updatedSupportValue = currentSupportValue + value;

                        updatedSupportValue = Math.Max(updatedSupportValue, 0);

                        var maxSupport = await mapManager.GetMaxSupportForRegion(areaId);
                        var currentAreaSupport = await mapManager.GetCurrentSupportForRegion(areaId, playerId);
                        updatedSupportValue = Math.Min(updatedSupportValue, maxSupport - currentAreaSupport);

                        await dbRefAllPlayersStats
                            .Child(playerId)
                            .Child("stats")
                            .Child("support")
                            .Child(supportChildSnapshot.Key)
                            .SetValueAsync(updatedSupportValue);
                    }
                    else
                    {
                        Debug.LogError($"Invalid support value for player {playerId} in area {areaId}. Value: {supportChildSnapshot.Value}");
                    }
                }
            }
        }
    }

    private async Task ChangeAllSupport(int value)
    {
        dbRefAllPlayersStats = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        var snapshot = await dbRefAllPlayersStats.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.LogError("No data found for players in the session.");
            return;
        }

        foreach (var playerSnapshot in snapshot.Children)
        {
            string playerId = playerSnapshot.Key;

            DataSnapshot playerSupportSnapshot = playerSnapshot.Child("stats").Child("support");

            if (!playerSupportSnapshot.Exists)
            {
                Debug.LogError($"Support not found for player {playerId}.");
                continue;
            }

            foreach (var supportChildSnapshot in playerSupportSnapshot.Children)
            {
                if (int.TryParse(supportChildSnapshot.Value.ToString(), out int currentSupportValue))
                {
                    int updatedSupportValue = currentSupportValue + value;

                    updatedSupportValue = Math.Max(updatedSupportValue, 0);

                    var maxSupport = await mapManager.GetMaxSupportForRegion(Convert.ToInt32(supportChildSnapshot.Key));
                    var currentAreaSupport = await mapManager.GetCurrentSupportForRegion(Convert.ToInt32(supportChildSnapshot.Key), playerId);
                    updatedSupportValue = Math.Min(updatedSupportValue, maxSupport - currentAreaSupport);

                    await dbRefAllPlayersStats
                        .Child(playerId)
                        .Child("stats")
                        .Child("support")
                        .Child(supportChildSnapshot.Key)
                        .SetValueAsync(updatedSupportValue);
                }
                else
                {
                    Debug.LogError($"Invalid support value for player {playerId} in area {supportChildSnapshot.Key}. Value: {supportChildSnapshot.Value}");
                }
            }
        }
    }

    private async Task ChangeAllStats(int value, string cardholderId, string statType)
    {
        dbRefAllPlayersStats = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        var snapshot = await dbRefAllPlayersStats.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.LogError("No data found for players in the session.");
            return;
        }

        foreach (var playerSnapshot in snapshot.Children)
        {
            string playerID = playerSnapshot.Key;

            Debug.Log($"{cardholderId} rzucaj¹cy, a to inny gracz ${playerID}");

            if (playerID == cardholderId) continue;

            DataSnapshot playerStatSnapshot = playerSnapshot.Child("stats").Child(statType);

            if (!playerStatSnapshot.Exists)
            {
                Debug.LogError($"{statType} not found for player {playerID}.");
                return;
            }

            if (!int.TryParse(playerStatSnapshot.Value.ToString(), out int currentStat))
            {
                Debug.LogError($"Invalid {statType} value for player {playerID}. Value: {playerStatSnapshot.Value}");
                return;
            }

            int updatedStat = currentStat + value;

            updatedStat = Mathf.Max(updatedStat, 0);

            await dbRefAllPlayersStats
                .Child(playerID)
                .Child("stats")
                .Child(statType)
                .SetValueAsync(updatedStat);
        }
    }

    private async Task<int> RandomizeRegion(string playerId, int value)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError($"Player ID is null or empty. ID: {playerId}");
            return -1;
        }

        dbRefSupport = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("stats")
            .Child("support");

        try
        {
            var snapshot = await dbRefSupport.GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError($"No player data found in the database for player ID: {playerId}");
                return -1;
            }

            List<int> validRegions = new();

            foreach (var supportChildSnapshot in snapshot.Children)
            {
                if (int.TryParse(supportChildSnapshot.Value.ToString(), out int currentSupportValue))
                {
                    int updatedSupportValue = currentSupportValue + value;

                    if (updatedSupportValue < 0)
                    {
                        continue;
                    }

                    var maxSupportTask = mapManager.GetMaxSupportForRegion(Convert.ToInt32(supportChildSnapshot.Key));
                    var currentAreaSupportTask = mapManager.GetCurrentSupportForRegion(Convert.ToInt32(supportChildSnapshot.Key), playerId);

                    await Task.WhenAll(maxSupportTask, currentAreaSupportTask);

                    int maxSupport = await maxSupportTask;
                    int currentAreaSupport = await currentAreaSupportTask;

                    if (currentAreaSupport + updatedSupportValue > maxSupport)
                    {
                        continue;
                    }

                    validRegions.Add(Convert.ToInt32(supportChildSnapshot.Key));
                }
                else
                {
                    Debug.LogError($"Invalid support value for region {supportChildSnapshot.Key}. Value: {supportChildSnapshot.Value}");
                }
            }

            if (validRegions.Count > 0)
            {
                System.Random rand = new();
                int randomIndex = rand.Next(validRegions.Count);
                return validRegions[randomIndex];
            }
            else
            {
                Debug.LogError($"No valid regions found for player {playerId} with at least {value}% support.");
                return -1;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error occurred while retrieving data for player {playerId}: {ex.Message}");
            return -1;
        }
    }

}

public class OptionData
{
    public int Number { get; }
    public string Target { get; }
    public int TargetNumber { get; }

    public OptionData(int number, string target, int targetNumber)
    {
        Number = number;
        Target = target;
        TargetNumber = targetNumber;
    }
}

