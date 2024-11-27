using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class CardUtilities : MonoBehaviour
{
    public void ProcessOptions(DataSnapshot snapshot, Dictionary<int, OptionData> optionsDictionary)
    {
        foreach (var optionSnapshot in snapshot.Children)
        {
            if (optionSnapshot.Key != "bonus")
            {
                DataSnapshot numberSnapshot = optionSnapshot.Child("number");
                DataSnapshot targetSnapshot = optionSnapshot.Child("target");
                DataSnapshot targetNumberSnapshot = optionSnapshot.Child("targetNumber");

                if (numberSnapshot.Exists && targetSnapshot.Exists)
                {
                    int number = Convert.ToInt32(numberSnapshot.Value);
                    string target = targetSnapshot.Value.ToString();
                    int targetNumber = targetNumberSnapshot.Exists ? Convert.ToInt32(targetNumberSnapshot.Value) : 1;

                    int optionKey = Convert.ToInt32(optionSnapshot.Key.Replace("option", ""));

                    optionsDictionary.Add(optionKey, new OptionData(number, target, targetNumber));
                }
                else
                {
                    Debug.LogError($"Option is missing 'number' or 'target'.");
                }
            }
        }
    }

    public void ProcessBonusOptions(DataSnapshot snapshot, Dictionary<int, OptionData> bonusOptionsDictionary)
    {
        DataSnapshot bonusSnapshot = snapshot.Child("bonus");
        if (bonusSnapshot.Exists)
        {
            int optionIndex = 0;

            foreach (var optionSnapshot in bonusSnapshot.Children)
            {
                DataSnapshot numberSnapshot = optionSnapshot.Child("number");
                DataSnapshot targetSnapshot = optionSnapshot.Child("target");
                DataSnapshot targetNumberSnapshot = optionSnapshot.Child("targetNumber");

                if (numberSnapshot.Exists && targetSnapshot.Exists)
                {
                    int number = Convert.ToInt32(numberSnapshot.Value);
                    string target = targetSnapshot.Value.ToString();
                    int targetNumber = targetNumberSnapshot.Exists ? Convert.ToInt32(targetNumberSnapshot.Value) : 1;

                    bonusOptionsDictionary.Add(optionIndex, new OptionData(number, target, targetNumber));
                }
                else
                {
                    Debug.LogError($"Bonus option {optionIndex} is missing 'number' or 'target'.");
                }

                optionIndex++;
            }
        }
    }

    public void ProcessOptionsCard(DataSnapshot cardsSnapshot, Dictionary<int, OptionDataCard> optionsDictionary)
    {
        foreach (var optionSnapshot in cardsSnapshot.Children)
        {
            if (optionSnapshot.Key != "bonus")
            {
                DataSnapshot cardNumberSnapshot = optionSnapshot.Child("cardNumber");
                DataSnapshot cardTypeSnapshot = optionSnapshot.Child("cardType");
                DataSnapshot sourceSnapshot = optionSnapshot.Child("source");
                DataSnapshot targetSnapshot = optionSnapshot.Child("target");
                DataSnapshot targetNumberSnapshot = optionSnapshot.Child("targetNumber");

                // Sprawdzamy, czy wszystkie wymagane dane istniej¹
                if (cardNumberSnapshot.Exists && cardTypeSnapshot.Exists && sourceSnapshot.Exists && targetSnapshot.Exists)
                {
                    int cardNumber = Convert.ToInt32(cardNumberSnapshot.Value);
                    string cardType = cardTypeSnapshot.Value.ToString();
                    string source = sourceSnapshot.Value.ToString();
                    string target = targetSnapshot.Value.ToString();

                    int targetNumber = targetNumberSnapshot.Exists ? Convert.ToInt32(targetNumberSnapshot.Value) : 1;

                    int optionKey = Convert.ToInt32(optionSnapshot.Key.Replace("action", ""));

                    optionsDictionary.Add(optionKey, new OptionDataCard(cardNumber, cardType, source, target, targetNumber));
                }
                else
                {
                    Debug.LogError($"Option is missing an attribute.");
                }
            }
        }
    }

    public void ProcessBonusOptionsCard(DataSnapshot cardsSnapshot, Dictionary<int, OptionDataCard> bonusOptionsDictionary)
    {
        DataSnapshot bonusSnapshot = cardsSnapshot.Child("bonus");
        if (bonusSnapshot.Exists)
        {
            int optionIndex = 0;

            foreach (var optionSnapshot in bonusSnapshot.Children)
            {
                DataSnapshot cardNumberSnapshot = optionSnapshot.Child("cardNumber");
                DataSnapshot cardTypeSnapshot = optionSnapshot.Child("cardType");
                DataSnapshot sourceSnapshot = optionSnapshot.Child("source");
                DataSnapshot targetSnapshot = optionSnapshot.Child("target");
                DataSnapshot targetNumberSnapshot = optionSnapshot.Child("targetNumber");

                if (cardNumberSnapshot.Exists && cardTypeSnapshot.Exists && sourceSnapshot.Exists && targetSnapshot.Exists)
                {
                    int cardNumber = Convert.ToInt32(cardNumberSnapshot.Value);
                    string cardType = cardTypeSnapshot.Value.ToString();
                    string source = sourceSnapshot.Value.ToString();
                    string target = targetSnapshot.Value.ToString();

                    int targetNumber = targetNumberSnapshot.Exists ? Convert.ToInt32(targetNumberSnapshot.Value) : 1;

                    bonusOptionsDictionary.Add(optionIndex, new OptionDataCard(cardNumber, cardType, source, target, targetNumber));
                }
                else
                {
                    Debug.LogError($"Bonus option {optionIndex} is missing an attribute.");
                }

                optionIndex++;
            }
        }
    }

    public async Task ChangeSupport(string playerId, int value, int areaId, string cardId, MapManager mapManager)
    {
        DatabaseReference dbRefSupport;
        string lobbyId = DataTransfer.LobbyId;
        int maxAreaSupport, currentAreaSupport;

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

        if (!int.TryParse(snapshot.Value.ToString(), out int support))
        {
            Debug.LogError("Failed to parse support value from the database.");
            return;
        }

        var maxSupportTask = mapManager.GetMaxSupportForRegion(areaId);
        var currentSupportTask = mapManager.GetCurrentSupportForRegion(areaId, playerId);

        await Task.WhenAll(maxSupportTask, currentSupportTask);

        maxAreaSupport = await maxSupportTask;
        currentAreaSupport = await currentSupportTask;

        int availableSupport = maxAreaSupport - currentAreaSupport - support;

        if (availableSupport <= 0)
        {
            Debug.Log("Brak dostêpnego miejsca na poparcie w tym regionie.");
            return;
        }

        int supportToAdd = (availableSupport >= value) ? value : availableSupport;

        if (cardId == "AD020" || cardId == "AD054" || cardId == "CA085")
        {
            if (currentAreaSupport < value)
            {
                Debug.Log("Nie mo¿na zagraæ karty ze wzglêdu na niewystarczaj¹ce poparcie w tym regionie");
                return;
            }
        }

        await CheckIfRegionsProtected(playerId, support, supportToAdd);

        if (await CheckIfRegionProtected(playerId, areaId, supportToAdd))
        {
            Debug.Log("Obszar jest chroniony, nie mo¿na zagraæ karty");
            return;
        }

        if (await CheckIfProtected(playerId, supportToAdd))
        {
            Debug.Log("Obszar jest chroniony, nie mo¿na zagraæ karty");
            return;
        }

        if (await CheckIfProtectedOneCard(playerId, supportToAdd))
        {
            Debug.Log("Obszar jest chroniony, nie mo¿na zagraæ karty");
            return;
        }

        await CheckBonusBudget(playerId, supportToAdd);

        supportToAdd = await CheckBonusSupport(playerId, supportToAdd);

        //await CheckIfBudgetPenalty(playerId, areaId);

        support = Math.Max(0, support + supportToAdd);

        await dbRefSupport.SetValueAsync(support);

        await CheckAndAddCopySupport(playerId, areaId, supportToAdd, mapManager);

    }

    public async Task<int> ChangeEnemyStat(string enemyId, int value, string statType, int playerBudget)
{
    string lobbyId = DataTransfer.LobbyId;
    string playerId = DataTransfer.PlayerId;

    if (string.IsNullOrEmpty(enemyId))
    {
        Debug.LogError($"Enemy ID is null or empty. ID: {enemyId}");
        return -1;
    }

    DatabaseReference dbRefEnemyStats = FirebaseInitializer.DatabaseReference
        .Child("sessions")
        .Child(lobbyId)
        .Child("players")
        .Child(enemyId)
        .Child("stats");

    try
    {
        var statSnapshot = await dbRefEnemyStats.Child(statType).GetValueAsync();

        if (!statSnapshot.Exists)
        {
            Debug.LogError($"Branch '{statType}' does not exist for enemy ID: {enemyId}");
            return -1;
        }

        if (!int.TryParse(statSnapshot.Value.ToString(), out int enemyStat))
        {
            Debug.LogError($"Failed to parse '{statType}' value for enemy ID: {enemyId}. Value: {statSnapshot.Value}");
            return -1;
        }

        int updatedStat = Math.Max(0, enemyStat + value);

        if (statType == "money" && playerId == enemyId)
        {
            if(!(await CheckBudgetBlock(playerId)))
            {
               playerBudget = updatedStat;
            }
        }
            if (statType == "income" && playerId == enemyId)
            {
                if (await CheckIncomeBlock(playerId))
                {
                    return -1;
                }
            }


            if (await CheckIfProtected(enemyId, value))
        {
            Debug.Log("Gracz jest chroniony, nie mo¿na zagraæ karty");
            return -1;
        }

            if (await CheckIfProtectedOneCard(enemyId, value))
            {
                Debug.Log("Gracz jest chroniony, nie mo¿na zagraæ karty");
                return -1;
            }

            await dbRefEnemyStats.Child(statType).SetValueAsync(updatedStat);

        if (statType == "money")
        {
            await CheckAndAddCopyBudget(enemyId, value);
        }

            return playerBudget;
    }
    catch (Exception ex)
    {
        Debug.LogError($"An error occurred while changing the enemy {statType}: {ex.Message}");
            return -1;
        }
}

    public async Task<int> RandomizeRegion(string playerId, int value, MapManager mapManager)
    {
        string lobbyId = DataTransfer.LobbyId;

        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError($"Player ID is null or empty. ID: {playerId}");
            return -1;
        }

        DatabaseReference dbRefSupport = FirebaseInitializer.DatabaseReference
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

    public async Task CheckIfRegionsProtected(string playerId, int currentSupport, int value)
    {
        string lobbyId = DataTransfer.LobbyId;

        if (currentSupport + value < currentSupport)
        {
            DatabaseReference dbRefProtected = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(playerId)
                .Child("protected")
                .Child("allRegions");

            DataSnapshot protectedSnapshot = await dbRefProtected.GetValueAsync();

            if (protectedSnapshot.Exists)
            {
                int protectedTurn = Convert.ToInt32(protectedSnapshot.Value);

                DatabaseReference dbRefTurnsTaken = FirebaseInitializer.DatabaseReference
                    .Child("sessions")
                    .Child(lobbyId)
                    .Child("players")
                    .Child(playerId)
                    .Child("stats")
                    .Child("turnsTaken");

                DataSnapshot turnsTakenSnapshot = await dbRefTurnsTaken.GetValueAsync();

                if (turnsTakenSnapshot.Exists)
                {
                    int turnsTaken = Convert.ToInt32(turnsTakenSnapshot.Value);

                    if (turnsTaken == protectedTurn)
                    {
                        DatabaseReference dbRefBudget = FirebaseInitializer.DatabaseReference
                            .Child("sessions")
                            .Child(lobbyId)
                            .Child("players")
                            .Child(playerId)
                            .Child("stats")
                            .Child("money");

                        DataSnapshot budgetSnapshot = await dbRefBudget.GetValueAsync();
                        int currentBudget = budgetSnapshot.Exists ? Convert.ToInt32(budgetSnapshot.Value) : 0;

                        await dbRefBudget.SetValueAsync(currentBudget + 3);

                    }
                }
            }
        }
    }

    public async Task<bool> CheckIfRegionProtected(string playerId, int regionId, int value)
    {
        string lobbyId = DataTransfer.LobbyId;

        if (value < 0)
        {
            DatabaseReference dbRefProtected = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(playerId)
                .Child("protected")
                .Child("region")
                .Child(regionId.ToString());

            DataSnapshot protectedSnapshot = await dbRefProtected.GetValueAsync();

            if (protectedSnapshot.Exists)
            {
                int protectedTurn = Convert.ToInt32(protectedSnapshot.Value);

                DatabaseReference dbRefTurnsTaken = FirebaseInitializer.DatabaseReference
                    .Child("sessions")
                    .Child(lobbyId)
                    .Child("players")
                    .Child(playerId)
                    .Child("stats")
                    .Child("turnsTaken");

                DataSnapshot turnsTakenSnapshot = await dbRefTurnsTaken.GetValueAsync();

                if (turnsTakenSnapshot.Exists)
                {
                    int turnsTaken = Convert.ToInt32(turnsTakenSnapshot.Value);

                    if (turnsTaken == protectedTurn)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public async Task CheckAndAddCopySupport(string playerId, int areaId, int value, MapManager mapManager)
    {
        if (value <= 0)
        {
            return;
        }

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefPlayers = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        DataSnapshot playersSnapshot = await dbRefPlayers.GetValueAsync();

        if (!playersSnapshot.Exists)
        {
            Debug.LogError($"No players found in lobby {lobbyId}.");
            return;
        }

        foreach (var playerSnapshot in playersSnapshot.Children)
        {
            string otherPlayerId = playerSnapshot.Key;

            if (otherPlayerId == playerId)
            {
                continue;
            }

            DatabaseReference dbRefCopySupport = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(otherPlayerId)
                .Child("copySupport");

            DataSnapshot copySupportSnapshot = await dbRefCopySupport.GetValueAsync();

            if (!copySupportSnapshot.Exists || !copySupportSnapshot.HasChild("enemyId"))
            {
                continue;
            }

            string enemyId = copySupportSnapshot.Child("enemyId").Value.ToString();

            if (enemyId != playerId)
            {
                continue;
            }

            int copySupportTurn = Convert.ToInt32(copySupportSnapshot.Child("turnsTaken").Value);

            DatabaseReference dbRefTurnsTaken = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(otherPlayerId)
                .Child("stats")
                .Child("turnsTaken");

            DataSnapshot turnsTakenSnapshot = await dbRefTurnsTaken.GetValueAsync();

            if (!turnsTakenSnapshot.Exists)
            {
                Debug.LogError($"TurnsTaken not found for player {otherPlayerId}. Cannot verify copySupport.");
                continue;
            }

            int turnsTaken = Convert.ToInt32(turnsTakenSnapshot.Value);
            if (turnsTaken != copySupportTurn)
            {
                continue;
            }

            var maxSupportTask = mapManager.GetMaxSupportForRegion(areaId);
            var currentSupportTask = mapManager.GetCurrentSupportForRegion(areaId, otherPlayerId);

            await Task.WhenAll(maxSupportTask, currentSupportTask);

            int maxAreaSupport = await maxSupportTask;
            int currentAreaSupport = await currentSupportTask;

            int availableSupport = maxAreaSupport - currentAreaSupport;

            if (availableSupport <= 0)
            {
                Debug.Log($"No available support space in region {areaId} for player {otherPlayerId}.");
                continue;
            }

            int supportToAdd = Math.Min(availableSupport, value);

            if (supportToAdd > 0)
            {
                DatabaseReference dbRefSupport = FirebaseInitializer.DatabaseReference
                    .Child("sessions")
                    .Child(lobbyId)
                    .Child("players")
                    .Child(otherPlayerId)
                    .Child("stats")
                    .Child("support")
                    .Child(areaId.ToString());

                DataSnapshot currentSupportSnapshot = await dbRefSupport.GetValueAsync();

                int currentSupport = currentSupportSnapshot.Exists ? Convert.ToInt32(currentSupportSnapshot.Value) : 0;
                int updatedSupportValue = currentSupport + supportToAdd;

                await dbRefSupport.SetValueAsync(updatedSupportValue);
            }
        }
    }

    public async Task CheckAndAddCopyBudget(string playerId, int value)
    {
        if (value <= 0)
        {
            return;
        }

        string lobbyId = DataTransfer.LobbyId;
        string otherPlayerId = "";

        DatabaseReference dbRefPlayers = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        DataSnapshot playersSnapshot = await dbRefPlayers.GetValueAsync();

        if (!playersSnapshot.Exists)
        {
            Debug.LogError($"No players found in lobby {lobbyId}.");
            return;
        }

        List<Task<DataSnapshot>> playerDataTasks = new List<Task<DataSnapshot>>();

        foreach (var playerSnapshot in playersSnapshot.Children)
        {
            otherPlayerId = playerSnapshot.Key;
            if (otherPlayerId == playerId)
            {
                continue;
            }

            var dbRefCopyBudget = dbRefPlayers
                .Child(otherPlayerId)
                .Child("copyBudget");

            playerDataTasks.Add(dbRefCopyBudget.GetValueAsync());
        }

        DataSnapshot[] playerDataSnapshots = await Task.WhenAll(playerDataTasks);

        foreach (var playerSnapshot in playerDataSnapshots)
        {
            if (!playerSnapshot.Exists || !playerSnapshot.HasChild("enemyId"))
            {
                continue;
            }

            string enemyId = playerSnapshot.Child("enemyId").Value.ToString();
            if (enemyId != playerId)
            {
                continue;
            }

            int copyBudgetTurn = Convert.ToInt32(playerSnapshot.Child("turnsTaken").Value);

            var dbRefTurnsTaken = dbRefPlayers
                .Child(otherPlayerId)
                .Child("stats")
                .Child("turnsTaken");

            DataSnapshot turnsTakenSnapshot = await dbRefTurnsTaken.GetValueAsync();

            if (!turnsTakenSnapshot.Exists)
            {
                Debug.LogError($"TurnsTaken not found for player {otherPlayerId}. Cannot verify copyBudget.");
                continue;
            }

            int turnsTaken = Convert.ToInt32(turnsTakenSnapshot.Value);
            if (turnsTaken != copyBudgetTurn)
            {
                continue;
            }

            var dbRefBudget = dbRefPlayers
                .Child(otherPlayerId)
                .Child("stats")
                .Child("money");

            DataSnapshot currentBudgetSnapshot = await dbRefBudget.GetValueAsync();

            int currentBudget = currentBudgetSnapshot.Exists ? Convert.ToInt32(currentBudgetSnapshot.Value) : 0;
            int updatedBudgetValue = currentBudget + value;

            await dbRefBudget.SetValueAsync(updatedBudgetValue);
        }
    }

    public async Task<bool> CheckBlockedCard(string playerId)
    {
        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId);

        DataSnapshot playerSnapshot = await dbRefPlayer.GetValueAsync();

        if (!playerSnapshot.Exists) return false;

        var cardBlockedSnapshot = playerSnapshot.Child("cardBlocked");
        if (!cardBlockedSnapshot.Exists ||
            !cardBlockedSnapshot.HasChild("turnsTaken") ||
            !cardBlockedSnapshot.HasChild("isBlocked"))
            return false;

        int cardBlockedTurn = Convert.ToInt32(cardBlockedSnapshot.Child("turnsTaken").Value);
        int currentTurn = Convert.ToInt32(playerSnapshot.Child("stats").Child("turnsTaken").Value);
        bool isBlocked = Convert.ToBoolean(cardBlockedSnapshot.Child("isBlocked").Value);

        if (cardBlockedTurn == currentTurn && isBlocked)
        {
            await dbRefPlayer.Child("cardBlocked").Child("isBlocked").SetValueAsync(false);
            Debug.Log("Card blocked");
            return true;
        }

        return false;
    }

    public async Task<bool> CheckIncreaseCost(string playerId)
    {
        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId);

        DataSnapshot playerSnapshot = await dbRefPlayer.GetValueAsync();

        if (!playerSnapshot.Exists) return false;

        var increaseCostSnapshot = playerSnapshot.Child("increaseCost");
        if (!increaseCostSnapshot.Exists ||
            !increaseCostSnapshot.HasChild("turnsTaken"))
            return false;

        int increaseCostTurn = Convert.ToInt32(increaseCostSnapshot.Child("turnsTaken").Value);
        int currentTurn = Convert.ToInt32(playerSnapshot.Child("stats").Child("turnsTaken").Value);

        if (increaseCostTurn == currentTurn)
        { 
            Debug.Log("Increase cost");
            return true;
        }

        return false;
    }

    public async Task<int> CountCardsOnHand(string playerId)
    {
        string lobbyId = DataTransfer.LobbyId;

        var deckRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("deck");

        var snapshot = await deckRef.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.LogWarning($"Deck for player {playerId} does not exist.");
            return 0;
        }

        int cardsOnHand = 0;

        foreach (var cardSnapshot in snapshot.Children)
        {
            if (cardSnapshot.Child("onHand").Exists &&
                bool.TryParse(cardSnapshot.Child("onHand").Value.ToString(), out bool onHand) &&
                onHand)
            {
                cardsOnHand++;
            }
        }

        return cardsOnHand;
    }

    public async Task<bool> CheckIfProtected(string playerId, int value)
    {
        if (value >= 0)
        {
            return false;
        }

        string lobbyId = DataTransfer.LobbyId;

        var dbRefProtectedAll = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId);

        var protectedTask = dbRefProtectedAll
            .Child("protected")
            .Child("all")
            .GetValueAsync();

        var turnsTakenTask = dbRefProtectedAll
            .Child("stats")
            .Child("turnsTaken")
            .GetValueAsync();

        await Task.WhenAll(protectedTask, turnsTakenTask);

        if (protectedTask.Result.Exists && turnsTakenTask.Result.Exists)
        {
            if (int.TryParse(protectedTask.Result.Value.ToString(), out int protectedTurn) &&
                int.TryParse(turnsTakenTask.Result.Value.ToString(), out int turnsTaken))
            {
                return turnsTaken == protectedTurn;
            }
        }

        return false;
    }

  /*  public async Task CheckIfBudgetPenalty(string playerId, int areaId)
    {
        string lobbyId = DataTransfer.LobbyId;

        Debug.Log($"Rozpoczêto sprawdzanie BudgetPenalty dla gracza o ID: {playerId} w lobby: {lobbyId}.");

        DatabaseReference dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId);

        DataSnapshot playerSnapshot = await dbRefPlayer.GetValueAsync();

        if (!playerSnapshot.Exists)
        {
            Debug.LogError($"Brak danych gracza o ID: {playerId} w bazie.");
            return;
        }
        Debug.Log($"Pobrano dane gracza o ID: {playerId}.");

        DataSnapshot budgetPenaltySnapshot = playerSnapshot.Child("budgetPenalty");

        if (!budgetPenaltySnapshot.Exists)
        {
            Debug.Log($"Brak ga³êzi 'budgetPenalty' dla gracza o ID: {playerId}.");
            return;
        }
        Debug.Log($"Znaleziono 'budgetPenalty' dla gracza o ID: {playerId}.");

        string budgetPenaltyPlayerId = Convert.ToString(budgetPenaltySnapshot.Child("playerId").Value);

        if (string.IsNullOrEmpty(budgetPenaltyPlayerId))
        {
            Debug.LogError("Nie znaleziono 'playerId' w ga³êzi 'budgetPenalty'.");
            return;
        }
        Debug.Log($"'playerId' w 'budgetPenalty' wskazuje na gracza o ID: {budgetPenaltyPlayerId}.");

        DatabaseReference dbRefBudgetPenaltyPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(budgetPenaltyPlayerId);

        DataSnapshot budgetPenaltyPlayerSnapshot = await dbRefBudgetPenaltyPlayer.GetValueAsync();

        if (!budgetPenaltyPlayerSnapshot.Exists)
        {
            Debug.LogError($"Brak danych gracza o ID: {budgetPenaltyPlayerId} wskazanego w 'budgetPenalty'.");
            return;
        }
        Debug.Log($"Pobrano dane gracza o ID: {budgetPenaltyPlayerId} wskazanego w 'budgetPenalty'.");

        int budgetPenaltyTurnsTaken = Convert.ToInt32(budgetPenaltySnapshot.Child("turnsTaken").Value);
        Debug.Log($"Liczba tur z 'budgetPenalty': {budgetPenaltyTurnsTaken}.");

        int budgetPenaltyPlayerTurnsTaken = Convert.ToInt32(budgetPenaltyPlayerSnapshot.Child("stats").Child("turnsTaken").Value);
        Debug.Log($"Liczba tur gracza wskazanego w 'budgetPenalty': {budgetPenaltyPlayerTurnsTaken}.");

        if (budgetPenaltyTurnsTaken != budgetPenaltyPlayerTurnsTaken)
        {
            Debug.Log($"Liczba tur siê nie zgadza. 'BudgetPenalty' (tury): {budgetPenaltyTurnsTaken}, gracz (tury): {budgetPenaltyPlayerTurnsTaken}. Przerywam.");
            return;
        }

        DataSnapshot areaSupportSnapshot = budgetPenaltyPlayerSnapshot
            .Child("stats")
            .Child("support")
            .Child(areaId.ToString());

        if (areaSupportSnapshot.Exists)
        {
            int areaSupport = Convert.ToInt32(areaSupportSnapshot.Value);
            Debug.Log($"Poparcie dla regionu {areaId} wynosi: {areaSupport}.");

            if (areaSupport > 0)
            {
                int currentMoney = Convert.ToInt32(playerSnapshot.Child("stats").Child("money").Value);
                int newMoney = Math.Max(currentMoney - 10, 0);

                await dbRefPlayer.Child("stats").Child("money").SetValueAsync(newMoney);
                await dbRefPlayer.Child("budgetPenalty").RemoveValueAsync();

                Debug.Log($"Zmniejszono pieni¹dze gracza o ID: {playerId} z {currentMoney} do {newMoney}. Usuniêto ga³¹Ÿ 'budgetPenalty'.");
            }
            else
            {
                Debug.Log($"Poparcie dla regionu {areaId} jest równe 0. Kara pieniê¿na nie zosta³a na³o¿ona.");
            }
        }
        else
        {
            Debug.Log($"Brak danych o poparciu dla regionu {areaId} w statystykach gracza {budgetPenaltyPlayerId}.");
        }
    }*/

    public async Task<bool> CheckSupportBlock(string playerId)
    {
        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId);

        DataSnapshot playerSnapshot = await dbRefPlayer.GetValueAsync();

        if (!playerSnapshot.Exists)
        {
            Debug.LogError($"Brak danych dla gracza o ID: {playerId}");
            return false;
        }

        DataSnapshot blockSupportSnapshot = playerSnapshot.Child("blockSupport");

        if (!blockSupportSnapshot.Exists)
        {
            return false;
        }

        string blockSupportPlayerId = Convert.ToString(blockSupportSnapshot.Child("playerId").Value);

        if (string.IsNullOrEmpty(blockSupportPlayerId))
        {
            Debug.LogError($"Nie znaleziono 'playerId' w ga³êzi 'blockSupport' dla gracza o ID: {playerId}");
            return false;
        }

        DatabaseReference dbRefBlockSupportPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(blockSupportPlayerId);

        DataSnapshot blockSupportPlayerSnapshot = await dbRefBlockSupportPlayer.GetValueAsync();

        if (!blockSupportPlayerSnapshot.Exists)
        {
            Debug.LogError($"Brak danych dla gracza z 'playerId' w 'blockSupport': {blockSupportPlayerId}");
            return false;
        }

        DataSnapshot blockSupportTurnsTakenSnapshot = blockSupportSnapshot.Child("turnsTaken");

        if (!blockSupportTurnsTakenSnapshot.Exists)
        {
            Debug.LogError($"Brak wartoœci 'turnsTaken' w ga³êzi 'blockSupport' dla gracza o ID: {playerId}");
            return false;
        }

        int blockSupportTurnsTaken = Convert.ToInt32(blockSupportTurnsTakenSnapshot.Value);

        DataSnapshot blockSupportPlayerTurnsTakenSnapshot = blockSupportPlayerSnapshot.Child("stats").Child("turnsTaken");

        if (!blockSupportPlayerTurnsTakenSnapshot.Exists)
        {
            Debug.LogError($"Brak wartoœci 'turnsTaken' w statystykach dla gracza o ID: {blockSupportPlayerId}");
            return false;
        }

        int blockSupportPlayerTurnsTaken = Convert.ToInt32(blockSupportPlayerTurnsTakenSnapshot.Value);

        if (blockSupportTurnsTaken == blockSupportPlayerTurnsTaken)
        {
            return true;
        }

        return false;
    }

    public async Task<bool> CheckBudgetBlock(string playerId)
    {
        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId);

        DataSnapshot playerSnapshot = await dbRefPlayer.GetValueAsync();

        if (!playerSnapshot.Exists)
        {
            Debug.LogError($"Brak danych dla gracza o ID: {playerId}");
            return false;
        }

        DataSnapshot blockBudgetSnapshot = playerSnapshot.Child("blockBudget");

        if (!blockBudgetSnapshot.Exists)
        {
            return false;
        }

        string blockBudgetPlayerId = Convert.ToString(blockBudgetSnapshot.Child("playerId").Value);

        if (string.IsNullOrEmpty(blockBudgetPlayerId))
        {
            Debug.LogError($"Nie znaleziono 'playerId' w ga³êzi 'blockBudget' dla gracza o ID: {playerId}");
            return false;
        }

        DatabaseReference dbRefBlockBudgetPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(blockBudgetPlayerId);

        DataSnapshot blockBudgetPlayerSnapshot = await dbRefBlockBudgetPlayer.GetValueAsync();

        if (!blockBudgetPlayerSnapshot.Exists)
        {
            Debug.LogError($"Brak danych dla gracza z 'playerId' w 'blockBudget': {blockBudgetPlayerId}");
            return false;
        }

        DataSnapshot blockBudgetTurnsTakenSnapshot = blockBudgetSnapshot.Child("turnsTaken");

        if (!blockBudgetTurnsTakenSnapshot.Exists)
        {
            Debug.LogError($"Brak wartoœci 'turnsTaken' w ga³êzi 'blockBudget' dla gracza o ID: {playerId}");
            return false;
        }

        int blockBudgetTurnsTaken = Convert.ToInt32(blockBudgetTurnsTakenSnapshot.Value);

        DataSnapshot blockBudgetPlayerTurnsTakenSnapshot = blockBudgetPlayerSnapshot.Child("stats").Child("turnsTaken");

        if (!blockBudgetPlayerTurnsTakenSnapshot.Exists)
        {
            Debug.LogError($"Brak wartoœci 'turnsTaken' w statystykach dla gracza o ID: {blockBudgetPlayerId}");
            return false;
        }

        int blockBudgetPlayerTurnsTaken = Convert.ToInt32(blockBudgetPlayerTurnsTakenSnapshot.Value);

        if (blockBudgetTurnsTaken == blockBudgetPlayerTurnsTaken)
        {
            Debug.Log("budget blocked cant do this");
            return true;
        }

        return false;
    }

    public async Task<bool> CheckIncomeBlock(string playerId)
    {
        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId);

        DataSnapshot playerSnapshot = await dbRefPlayer.GetValueAsync();

        if (!playerSnapshot.Exists)
        {
            Debug.LogError($"Brak danych dla gracza o ID: {playerId}");
            return false;
        }

        DataSnapshot blockIncomeSnapshot = playerSnapshot.Child("blockIncome");

        if (!blockIncomeSnapshot.Exists)
        {
            return false;
        }

        string blockIncomePlayerId = Convert.ToString(blockIncomeSnapshot.Child("playerId").Value);

        if (string.IsNullOrEmpty(blockIncomePlayerId))
        {
            Debug.LogError($"Nie znaleziono 'playerId' w ga³êzi 'blockIncome' dla gracza o ID: {playerId}");
            return false;
        }

        DatabaseReference dbRefBlockIncomePlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(blockIncomePlayerId);

        DataSnapshot blockIncomePlayerSnapshot = await dbRefBlockIncomePlayer.GetValueAsync();

        if (!blockIncomePlayerSnapshot.Exists)
        {
            Debug.LogError($"Brak danych dla gracza z 'playerId' w 'blockIncome': {blockIncomePlayerId}");
            return false;
        }

        DataSnapshot blockIncomeTurnsTakenSnapshot = blockIncomeSnapshot.Child("turnsTaken");

        if (!blockIncomeTurnsTakenSnapshot.Exists)
        {
            Debug.LogError($"Brak wartoœci 'turnsTaken' w ga³êzi 'blockIncome' dla gracza o ID: {playerId}");
            return false;
        }

        int blockIncomeTurnsTaken = Convert.ToInt32(blockIncomeTurnsTakenSnapshot.Value);

        DataSnapshot blockIncomePlayerTurnsTakenSnapshot = blockIncomePlayerSnapshot.Child("stats").Child("turnsTaken");

        if (!blockIncomePlayerTurnsTakenSnapshot.Exists)
        {
            Debug.LogError($"Brak wartoœci 'turnsTaken' w statystykach dla gracza o ID: {blockIncomePlayerId}");
            return false;
        }

        int blockIncomePlayerTurnsTaken = Convert.ToInt32(blockIncomePlayerTurnsTakenSnapshot.Value);

        if (blockIncomeTurnsTaken == blockIncomePlayerTurnsTaken)
        {
            Debug.Log("Income is blocked, action cannot proceed");
            return true;
        }

        return false;
    }

    public async Task<bool> CheckIncreaseCostAllTurn(string playerId)
    {
        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId);

        DataSnapshot playerSnapshot = await dbRefPlayer.GetValueAsync();

        if (!playerSnapshot.Exists) return false;

        var increaseCostSnapshot = playerSnapshot.Child("increaseCostAllTurn");
        if (!increaseCostSnapshot.Exists ||
            !increaseCostSnapshot.HasChild("turnsTaken"))
            return false;

        int increaseCostTurn = Convert.ToInt32(increaseCostSnapshot.Child("turnsTaken").Value);
        int currentTurn = Convert.ToInt32(playerSnapshot.Child("stats").Child("turnsTaken").Value);

        if (increaseCostTurn == currentTurn)
        {
            Debug.Log("Increase cost");
            return true;
        }

        return false;
    }

    public async Task<bool> CheckDecreaseCost(string playerId)
    {
        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId);

        DataSnapshot playerSnapshot = await dbRefPlayer.GetValueAsync();

        if (!playerSnapshot.Exists) return false;

        var decreaseCostSnapshot = playerSnapshot.Child("decreaseCost");
        if (!decreaseCostSnapshot.Exists ||
            !decreaseCostSnapshot.HasChild("turnsTaken"))
            return false;

        int decreaseCostTurn = Convert.ToInt32(decreaseCostSnapshot.Child("turnsTaken").Value);
        int currentTurn = Convert.ToInt32(playerSnapshot.Child("stats").Child("turnsTaken").Value);

        if (decreaseCostTurn == currentTurn)
        {
            Debug.Log("Decrease cost");
            return true;
        }

        return false;
    }

    public async Task CheckIfPlayed2Cards(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("Player ID is null or empty.");
            return;
        }

        DatabaseReference dbRefPlayerTwoCards = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(DataTransfer.LobbyId)
            .Child("players")
            .Child(playerId)
            .Child("twoCards");

        DataSnapshot twoCardsSnapshot = await dbRefPlayerTwoCards.GetValueAsync();
        if (!twoCardsSnapshot.Exists)
        {
            return;
        }

        int turnsTakenInTwoCards = Convert.ToInt32(twoCardsSnapshot.Child("turnsTaken").Value);
        int played = Convert.ToInt32(twoCardsSnapshot.Child("played").Value);
        string relatedPlayerId = twoCardsSnapshot.Child("playerId").Value.ToString();

        DatabaseReference dbRefRelatedPlayerStats = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(DataTransfer.LobbyId)
            .Child("players")
            .Child(relatedPlayerId)
            .Child("stats");

        DataSnapshot relatedPlayerStatsSnapshot = await dbRefRelatedPlayerStats.GetValueAsync();
        if (!relatedPlayerStatsSnapshot.Exists)
        {
            Debug.LogError($"Related player {relatedPlayerId} stats not found.");
            return;
        }

        int turnsTakenForRelatedPlayer = Convert.ToInt32(relatedPlayerStatsSnapshot.Child("turnsTaken").Value);

        if (turnsTakenInTwoCards != turnsTakenForRelatedPlayer)
        {
            return;
        }

        if (played == 0)
        {
            await dbRefPlayerTwoCards.Child("played").SetValueAsync(1);
        }
        else if (played == 1)
        {
            int currentMoney = Convert.ToInt32(relatedPlayerStatsSnapshot.Child("money").Value);
            int updatedMoney = currentMoney + 5;

            await dbRefRelatedPlayerStats.Child("money").SetValueAsync(updatedMoney);

            await dbRefPlayerTwoCards.RemoveValueAsync();
        }
    }

    public async Task CheckBonusBudget(string playerId, int value)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("Player ID is null or empty.");
            return;
        }

        if (value <= 0)
        {
            return;
        }

        DatabaseReference dbRefBonusBudget = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(DataTransfer.LobbyId)
            .Child("players")
            .Child(playerId)
            .Child("bonusBudget");

        DataSnapshot bonusBudgetSnapshot = await dbRefBonusBudget.GetValueAsync();
        if (!bonusBudgetSnapshot.Exists)
        {
            return;
        }

        int bonusBudgetTurnsTaken = Convert.ToInt32(bonusBudgetSnapshot.Child("turnsTaken").Value);

        DatabaseReference dbRefPlayerStats = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(DataTransfer.LobbyId)
            .Child("players")
            .Child(playerId)
            .Child("stats");

        DataSnapshot playerStatsSnapshot = await dbRefPlayerStats.GetValueAsync();
        if (!playerStatsSnapshot.Exists)
        {
            Debug.LogError($"Stats for player {playerId} not found.");
            return;
        }

        int playerTurnsTaken = Convert.ToInt32(playerStatsSnapshot.Child("turnsTaken").Value);

        if (bonusBudgetTurnsTaken != playerTurnsTaken)
        {
            return;
        }

        int currentMoney = Convert.ToInt32(playerStatsSnapshot.Child("money").Value);
        int updatedMoney = currentMoney + 5;

        await dbRefPlayerStats.Child("money").SetValueAsync(updatedMoney);
    }

    public async Task<bool> CheckIfProtectedOneCard(string playerId, int value)
    {
        if (value >= 0)
        {
            return false;
        }

        string lobbyId = DataTransfer.LobbyId;

        var dbRefProtectedOneCard = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId);

        var protectedTask = dbRefProtectedOneCard
            .Child("protected")
            .Child("allOneCard")
            .GetValueAsync();

        var turnsTakenTask = dbRefProtectedOneCard
            .Child("stats")
            .Child("turnsTaken")
            .GetValueAsync();

        await Task.WhenAll(protectedTask, turnsTakenTask);

        if (protectedTask.Result.Exists && turnsTakenTask.Result.Exists)
        {
            if (int.TryParse(protectedTask.Result.Value.ToString(), out int protectedTurn) &&
                int.TryParse(turnsTakenTask.Result.Value.ToString(), out int turnsTaken))
            {
                if (turnsTaken == protectedTurn)
                {
                    await dbRefProtectedOneCard.Child("protected").Child("allOneCard").RemoveValueAsync();
                    return true;
                }
            }
        }

        return false;
    }

    public async Task<bool> CheckIgnoreCost(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("Player ID is null or empty.");
            return false;
        }

        string lobbyId = DataTransfer.LobbyId;

        var dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId);

        var ignoreCostTask = dbRefPlayer.Child("ignoreCost").GetValueAsync();
        var statsTask = dbRefPlayer.Child("stats").GetValueAsync();

        await Task.WhenAll(ignoreCostTask, statsTask);

        var ignoreCostSnapshot = ignoreCostTask.Result;
        var statsSnapshot = statsTask.Result;

        if (!ignoreCostSnapshot.Exists || !statsSnapshot.Exists || !statsSnapshot.Child("turnsTaken").Exists)
        {
            return false;
        }

        int ignoreCostValue = Convert.ToInt32(ignoreCostSnapshot.Value);
        int turnsTaken = Convert.ToInt32(statsSnapshot.Child("turnsTaken").Value);

        if (ignoreCostValue == turnsTaken)
        {
            await dbRefPlayer.Child("ignoreCost").RemoveValueAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> CheckCardLimit(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("Player ID is null or empty.");
            return false;
        }

        string lobbyId = DataTransfer.LobbyId;

        var dbRefLimitCards = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("limitCards");

        var limitCardsSnapshot = await dbRefLimitCards.GetValueAsync();
        if (!limitCardsSnapshot.Exists)
        {
            return false;
        }

        string limitCardsPlayerId = limitCardsSnapshot.Child("playerId").Value?.ToString();
        int limitCardsTurnsTaken = Convert.ToInt32(limitCardsSnapshot.Child("turnsTaken").Value);
        int playedCards = Convert.ToInt32(limitCardsSnapshot.Child("playedCards").Value);

        if (string.IsNullOrEmpty(limitCardsPlayerId))
        {
            Debug.LogError("playerId is missing in limitCards.");
            return false;
        }

        var dbRefPlayerStats = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(limitCardsPlayerId)
            .Child("stats");

        var playerStatsSnapshot = await dbRefPlayerStats.GetValueAsync();
        if (!playerStatsSnapshot.Exists || !playerStatsSnapshot.Child("turnsTaken").Exists)
        {
            Debug.LogError($"Stats or turnsTaken for player {limitCardsPlayerId} not found.");
            return false;
        }

        int playerTurnsTaken = Convert.ToInt32(playerStatsSnapshot.Child("turnsTaken").Value);

        if (limitCardsTurnsTaken == playerTurnsTaken)
        {
            if (playedCards == 1)
            {
                return true;
            }

            if (playedCards == 0)
            {
                await dbRefLimitCards.Child("playedCards").SetValueAsync(1); 
                return false;
            }

            if (playedCards == -1)
            {
                await dbRefLimitCards.Child("playedCards").SetValueAsync(0);
                return false;
            }
        }

        return false;
    }

    public async Task<int> CheckBonusSupport(string playerId, int value)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("Player ID is null or empty.");
            return value;
        }

        if (value <= 0)
        {
            return value;
        }

        DatabaseReference dbRefBonusSupport = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(DataTransfer.LobbyId)
            .Child("players")
            .Child(playerId)
            .Child("bonusSupport");

        DataSnapshot bonusSupportSnapshot = await dbRefBonusSupport.GetValueAsync();
        if (!bonusSupportSnapshot.Exists)
        {
            return value;
        }

        int bonusSupportTurnsTaken = Convert.ToInt32(bonusSupportSnapshot.Child("turnsTaken").Value);

        DatabaseReference dbRefPlayerStats = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(DataTransfer.LobbyId)
            .Child("players")
            .Child(playerId)
            .Child("stats");

        DataSnapshot playerStatsSnapshot = await dbRefPlayerStats.GetValueAsync();
        if (!playerStatsSnapshot.Exists)
        {
            Debug.LogError($"Stats for player {playerId} not found.");
            return value;
        }

        int playerTurnsTaken = Convert.ToInt32(playerStatsSnapshot.Child("turnsTaken").Value);

        if (bonusSupportTurnsTaken != playerTurnsTaken)
        {
            return value;
        }

        await dbRefBonusSupport.RemoveValueAsync();

        return 2 * value;
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

public class OptionDataCard
{
    public int CardNumber { get; set; }
    public string CardType { get; set; }
    public string Source { get; set; }
    public string Target { get; set; }
    public int TargetNumber { get; set; }

    public OptionDataCard(int cardNumber, string cardType, string source, string target, int targetNumber)
    {
        CardNumber = cardNumber;
        CardType = cardType;
        Source = source;
        Target = target;
        TargetNumber = targetNumber;
    }
}
