using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class CardUtilities : MonoBehaviour
{
    public ErrorPanelController errorPanelController;

    public void ProcessOptions(DataSnapshot snapshot, Dictionary<int, OptionData> optionsDictionary)
    {
        foreach (var optionSnapshot in snapshot.Children)
        {
            if (optionSnapshot.Key == "bonus") continue;

            DataSnapshot numberSnapshot = optionSnapshot.Child("number");
            DataSnapshot targetSnapshot = optionSnapshot.Child("target");
            DataSnapshot targetNumberSnapshot = optionSnapshot.Child("targetNumber");

            if (numberSnapshot.Exists && targetSnapshot.Exists)
            {
                if (int.TryParse(numberSnapshot.Value.ToString(), out int number) &&
                    int.TryParse(targetNumberSnapshot.Value?.ToString() ?? "1", out int targetNumber))
                {
                    string target = targetSnapshot.Value.ToString();
                    int optionKey = int.TryParse(optionSnapshot.Key.Replace("option", ""), out int parsedKey) ? parsedKey : -1;

                    if (optionKey != -1)
                    {
                        optionsDictionary[optionKey] = new OptionData(number, target, targetNumber);
                    }
                    else
                    {
                        Debug.LogError($"Invalid option key: {optionSnapshot.Key}");
                    }
                }
                else
                {
                    Debug.LogError($"Invalid 'number' or 'targetNumber' value in option {optionSnapshot.Key}.");
                }
            }
            else
            {
                Debug.LogError($"Option {optionSnapshot.Key} is missing 'number' or 'target'.");
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
                    if (int.TryParse(numberSnapshot.Value.ToString(), out int number) &&
                        int.TryParse(targetNumberSnapshot.Value?.ToString() ?? "1", out int targetNumber))
                    {
                        string target = targetSnapshot.Value.ToString();
                        bonusOptionsDictionary[optionIndex] = new OptionData(number, target, targetNumber);
                    }
                    else
                    {
                        Debug.LogError($"Invalid 'number' or 'targetNumber' value in bonus option {optionIndex}.");
                    }
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
            if (optionSnapshot.Key == "bonus") continue;

            DataSnapshot cardNumberSnapshot = optionSnapshot.Child("cardNumber");
            DataSnapshot cardTypeSnapshot = optionSnapshot.Child("cardType");
            DataSnapshot sourceSnapshot = optionSnapshot.Child("source");
            DataSnapshot targetSnapshot = optionSnapshot.Child("target");
            DataSnapshot targetNumberSnapshot = optionSnapshot.Child("targetNumber");

            if (cardNumberSnapshot.Exists && cardTypeSnapshot.Exists && sourceSnapshot.Exists && targetSnapshot.Exists)
            {
                if (int.TryParse(cardNumberSnapshot.Value.ToString(), out int cardNumber) &&
                    int.TryParse(targetNumberSnapshot.Value?.ToString() ?? "1", out int targetNumber))
                {
                    string cardType = cardTypeSnapshot.Value.ToString();
                    string source = sourceSnapshot.Value.ToString();
                    string target = targetSnapshot.Value.ToString();

                    int optionKey = int.TryParse(optionSnapshot.Key.Replace("action", ""), out int parsedKey) ? parsedKey : -1;

                    if (optionKey != -1)
                    {
                        optionsDictionary[optionKey] = new OptionDataCard(cardNumber, cardType, source, target, targetNumber);
                    }
                    else
                    {
                        Debug.LogError($"Invalid option key: {optionSnapshot.Key}");
                    }
                }
                else
                {
                    Debug.LogError($"Invalid 'cardNumber' or 'targetNumber' value in option {optionSnapshot.Key}.");
                }
            }
            else
            {
                Debug.LogError($"Option {optionSnapshot.Key} is missing a required attribute.");
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
                    if (int.TryParse(cardNumberSnapshot.Value.ToString(), out int cardNumber) &&
                        int.TryParse(targetNumberSnapshot.Value?.ToString() ?? "1", out int targetNumber))
                    {
                        string cardType = cardTypeSnapshot.Value.ToString();
                        string source = sourceSnapshot.Value.ToString();
                        string target = targetSnapshot.Value.ToString();

                        bonusOptionsDictionary[optionIndex] = new OptionDataCard(cardNumber, cardType, source, target, targetNumber);
                    }
                    else
                    {
                        Debug.LogError($"Invalid 'cardNumber' or 'targetNumber' value in bonus option {optionIndex}.");
                    }
                }
                else
                {
                    Debug.LogError($"Bonus option {optionIndex} is missing a required attribute.");
                }

                optionIndex++;
            }
        }
    }

    public async Task<bool> ChangeSupport(string playerId, int value, int areaId, string cardId, MapManager mapManager)
    {
        string lobbyId = DataTransfer.LobbyId;
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
        int availableSupport = maxAreaSupport - currentAreaSupport - support;

        if (availableSupport <= 0 && value > 0)
        {
            if(cardId == "AD046")
            {
                return false;
            }

            Debug.Log("Brak dostêpnego miejsca na poparcie w tym regionie.");
            errorPanelController.ShowError("no_support_available");
            return true;
        }  

        int supportToAdd = Math.Min(value, availableSupport);

        if (cardId == "AD020" || cardId == "AD054" || cardId == "CA085")
        {
            if (currentAreaSupport < value)
            {
                Debug.Log("Nie mo¿na zagraæ karty ze wzglêdu na niewystarczaj¹ce poparcie w tym regionie");
                errorPanelController.ShowError("no_support");
                return true;
            }
        }

        await CheckIfRegionsProtected(playerId, support, supportToAdd);

        bool isRegionProtected = await CheckIfRegionProtected(playerId, areaId, supportToAdd);

        await CheckIfBudgetPenalty(areaId);
        if (isRegionProtected)
        {
            Debug.Log("Obszar jest chroniony, nie mo¿na zagraæ karty");
            errorPanelController.ShowError("region_protected");
            return true;
        }

        bool isProtected = await CheckIfProtected(playerId, supportToAdd);
        if (isProtected)
        {
            Debug.Log("Obszar jest chroniony, nie mo¿na zagraæ karty");
            errorPanelController.ShowError("region_protected");
            return true;
        }

        bool isProtectedOneCard = await CheckIfProtectedOneCard(playerId, supportToAdd);
        if (isProtectedOneCard)
        {
            Debug.Log("Obszar jest chroniony, nie mo¿na zagraæ karty");
            errorPanelController.ShowError("region_protected");
            return true;
        }


        await CheckBonusBudget(playerId, supportToAdd);

        if(cardId != "AD046")
        {
            supportToAdd = await CheckBonusSupport(playerId, supportToAdd);
        }

        support = Math.Max(0, support + supportToAdd);

        await dbRefSupport.SetValueAsync(support);

        await CheckAndAddCopySupport(playerId, areaId, supportToAdd, mapManager);

        return false;
    }

    public async Task<int> ChangeEnemyStat(string enemyId, int value, string statType, int playerBudget)
    {
        string lobbyId = DataTransfer.LobbyId;
        string playerId = DataTransfer.PlayerId;

        if (string.IsNullOrEmpty(enemyId))
        {
            Debug.LogError($"Enemy ID is null or empty. ID: {enemyId}");
            errorPanelController.ShowError("general_error");
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
                errorPanelController.ShowError("general_error");
                return -1;
            }

            if (!int.TryParse(statSnapshot.Value.ToString(), out int enemyStat))
            {
                Debug.LogError($"Failed to parse '{statType}' value for enemy ID: {enemyId}. Value: {statSnapshot.Value}");
                errorPanelController.ShowError("general_error");
                return -1;
            }

            int updatedStat = Math.Max(0, enemyStat + value);

            if (statType == "money")
            {
                bool isPlayerSameAsEnemy = playerId == enemyId;
                bool isBudgetNotBlocked = !(await CheckBudgetBlock(playerId));

                if (isPlayerSameAsEnemy && isBudgetNotBlocked)
                {
                    playerBudget = updatedStat;
                    if(playerBudget <0)
                    {
                        Debug.LogWarning("Brak wystarczaj¹cego bud¿etu aby zagraæ kartê.");
                        errorPanelController.ShowError("no_budget");
                        return -1;
                    }
                }


                await CheckAndAddCopyBudget(enemyId, value);
            }

            if (statType == "income" && playerId == enemyId)
            {
                bool isIncomeBlocked = await CheckIncomeBlock(playerId);
                if (isIncomeBlocked)
                {
                    Debug.Log("Nie mo¿na wp³ywaæ na przychód, zosta³o to zablokowane");
                    errorPanelController.ShowError("action_blocked");
                    return -1;
                }
            }

            bool isProtected = await CheckIfProtected(enemyId, value);
            if (isProtected)
            {
                Debug.Log("Gracz jest chroniony, nie mo¿na zagraæ karty");
                errorPanelController.ShowError("player_protected");
                return -1;
            }

            bool isProtectedOneCard = await CheckIfProtectedOneCard(enemyId, value);
            if (isProtectedOneCard)
            {
                Debug.Log("Gracz jest chroniony, nie mo¿na zagraæ karty");
                errorPanelController.ShowError("player_protected");
                return -1;
            }


            await dbRefEnemyStats.Child(statType).SetValueAsync(updatedStat);

            return playerBudget;
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while changing the enemy {statType}: {ex.Message}");
            errorPanelController.ShowError("general_error");
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

                    if (value < 0 && currentSupportValue <= 0)
                    {
                        continue;
                    }

                    if (updatedSupportValue < 0) updatedSupportValue = 0;

                    int regionId = Convert.ToInt32(supportChildSnapshot.Key);
                    var maxSupportTask = mapManager.GetMaxSupportForRegion(regionId);
                    var currentAreaSupportTask = mapManager.GetCurrentSupportForRegion(regionId, playerId);

                    int maxSupport = await maxSupportTask;
                    int currentAreaSupport = await currentAreaSupportTask;

                    if (currentAreaSupport + updatedSupportValue <= maxSupport)
                    {
                        validRegions.Add(regionId);
                    }
                }
                else
                {
                    Debug.LogError($"Invalid support value for region {supportChildSnapshot.Key}. Value: {supportChildSnapshot.Value}");
                    return -1;
                }
            }

            if (validRegions.Count > 0)
            {
                System.Random rand = new();
                return validRegions[rand.Next(validRegions.Count)];
            }
            else
            {
                Debug.LogError($"No valid regions found for player {playerId} with at least {value}% support.");
                errorPanelController.ShowError("no_player");
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

            if (!protectedSnapshot.Exists) return;

            int protectedTurn = Convert.ToInt32(protectedSnapshot.Value);

            DatabaseReference dbRefTurnsTaken = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(playerId)
                .Child("stats")
                .Child("turnsTaken");

            DataSnapshot turnsTakenSnapshot = await dbRefTurnsTaken.GetValueAsync();

            if (!turnsTakenSnapshot.Exists) return;

            int turnsTaken = Convert.ToInt32(turnsTakenSnapshot.Value);

            if (turnsTaken != protectedTurn) return;

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

    public async Task<bool> CheckIfRegionProtected(string playerId, int regionId, int value)
    {
        string lobbyId = DataTransfer.LobbyId;

        if (value >= 0) return false;

        DatabaseReference dbRefProtected = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("protected")
            .Child("region")
            .Child(regionId.ToString());

        DataSnapshot protectedSnapshot = await dbRefProtected.GetValueAsync();

        if (!protectedSnapshot.Exists) return false;

        int protectedTurn = Convert.ToInt32(protectedSnapshot.Value);

        DatabaseReference dbRefTurnsTaken = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("stats")
            .Child("turnsTaken");

        DataSnapshot turnsTakenSnapshot = await dbRefTurnsTaken.GetValueAsync();

        if (!turnsTakenSnapshot.Exists) return false;

        int turnsTaken = Convert.ToInt32(turnsTakenSnapshot.Value);

        return turnsTaken == protectedTurn;
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
            if (otherPlayerId == playerId) continue;

            var dbRefCopySupport = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(otherPlayerId)
                .Child("copySupport");

            DataSnapshot copySupportSnapshot = await dbRefCopySupport.GetValueAsync();

            if (!copySupportSnapshot.Exists || copySupportSnapshot.Child("enemyId").Value.ToString() != playerId)
            {
                continue;
            }

            int copySupportTurn = Convert.ToInt32(copySupportSnapshot.Child("turnsTaken").Value);

            var dbRefTurnsTaken = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(otherPlayerId)
                .Child("stats")
                .Child("turnsTaken");

            DataSnapshot turnsTakenSnapshot = await dbRefTurnsTaken.GetValueAsync();

            if (!turnsTakenSnapshot.Exists || Convert.ToInt32(turnsTakenSnapshot.Value) != copySupportTurn)
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
                continue;
            }

            int supportToAdd = Math.Min(availableSupport, value);
            if (supportToAdd > 0)
            {
                var dbRefSupport = FirebaseInitializer.DatabaseReference
                    .Child("sessions")
                    .Child(lobbyId)
                    .Child("players")
                    .Child(otherPlayerId)
                    .Child("stats")
                    .Child("support")
                    .Child(areaId.ToString());

                DataSnapshot currentSupportSnapshot = await dbRefSupport.GetValueAsync();
                int currentSupport = currentSupportSnapshot.Exists ? Convert.ToInt32(currentSupportSnapshot.Value) : 0;

                await dbRefSupport.SetValueAsync(currentSupport + supportToAdd);
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
        var dbRefPlayers = FirebaseInitializer.DatabaseReference
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
            string otherPlayerId = playerSnapshot.Key;
            if (otherPlayerId == playerId) continue;

            var dbRefCopyBudget = dbRefPlayers
                .Child(otherPlayerId)
                .Child("copyBudget");

            playerDataTasks.Add(dbRefCopyBudget.GetValueAsync());
        }

        var playerDataSnapshots = await Task.WhenAll(playerDataTasks);

        foreach (var playerSnapshot in playerDataSnapshots)
        {
            if (!playerSnapshot.Exists || playerSnapshot.Child("enemyId").Value.ToString() != playerId)
            {
                continue;
            }

            int copyBudgetTurn = Convert.ToInt32(playerSnapshot.Child("turnsTaken").Value);

            var dbRefTurnsTaken = dbRefPlayers
                .Child(playerSnapshot.Key)
                .Child("stats")
                .Child("turnsTaken");

            DataSnapshot turnsTakenSnapshot = await dbRefTurnsTaken.GetValueAsync();

            if (!turnsTakenSnapshot.Exists || Convert.ToInt32(turnsTakenSnapshot.Value) != copyBudgetTurn)
            {
                continue;
            }

            var dbRefBudget = dbRefPlayers
                .Child(playerSnapshot.Key)
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

        if (cardBlockedSnapshot.Exists &&
            cardBlockedSnapshot.HasChild("turnsTaken") &&
            cardBlockedSnapshot.HasChild("isBlocked"))
        {
            int cardBlockedTurn = Convert.ToInt32(cardBlockedSnapshot.Child("turnsTaken").Value);
            int currentTurn = Convert.ToInt32(playerSnapshot.Child("stats").Child("turnsTaken").Value);
            bool isBlocked = Convert.ToBoolean(cardBlockedSnapshot.Child("isBlocked").Value);

            if (cardBlockedTurn == currentTurn && isBlocked)
            {
                await dbRefPlayer.Child("cardBlocked").Child("isBlocked").SetValueAsync(false);
                Debug.Log("Card blocked");
                return true;
            }
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

        if (increaseCostSnapshot.Exists && increaseCostSnapshot.HasChild("turnsTaken"))
        {
            int increaseCostTurn = Convert.ToInt32(increaseCostSnapshot.Child("turnsTaken").Value);
            int currentTurn = Convert.ToInt32(playerSnapshot.Child("stats").Child("turnsTaken").Value);

            if (increaseCostTurn == currentTurn)
            {
                Debug.Log("Increase cost");
                return true;
            }
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
            errorPanelController.ShowError("general_error");
            return -1;
        }

        int cardsOnHand = 0;

        foreach (var cardSnapshot in snapshot.Children)
        {
            bool onHand = false; 

            if (cardSnapshot.Child("onHand").Exists &&
                bool.TryParse(cardSnapshot.Child("onHand").Value.ToString(), out onHand) &&
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

        var protectedSnapshot = protectedTask.Result;
        var turnsTakenSnapshot = turnsTakenTask.Result;

        if (protectedSnapshot.Exists && turnsTakenSnapshot.Exists)
        {
            bool protectedParsed = int.TryParse(protectedSnapshot.Value.ToString(), out int protectedTurn);
            bool turnsTakenParsed = int.TryParse(turnsTakenSnapshot.Value.ToString(), out int turnsTaken);

            if (protectedParsed && turnsTakenParsed)
            {
                return turnsTaken == protectedTurn;
            }
        }

        return false;
    }

    public async Task CheckIfBudgetPenalty(int areaId)
      {
          string lobbyId = DataTransfer.LobbyId;
          string playerId = DataTransfer.PlayerId; 

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

          DataSnapshot budgetPenaltySnapshot = playerSnapshot.Child("budgetPenalty");

          if (!budgetPenaltySnapshot.Exists)
          {
              return;
          }

          string budgetPenaltyPlayerId = Convert.ToString(budgetPenaltySnapshot.Child("playerId").Value);

          if (string.IsNullOrEmpty(budgetPenaltyPlayerId))
          {
              Debug.LogError("Nie znaleziono 'playerId' w ga³êzi 'budgetPenalty'.");
              return;
          }

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

          int budgetPenaltyTurnsTaken = Convert.ToInt32(budgetPenaltySnapshot.Child("turnsTaken").Value);

          int budgetPenaltyPlayerTurnsTaken = Convert.ToInt32(budgetPenaltyPlayerSnapshot.Child("stats").Child("turnsTaken").Value);

          if (budgetPenaltyTurnsTaken != budgetPenaltyPlayerTurnsTaken)
          {
              return;
          }

          DataSnapshot areaSupportSnapshot = budgetPenaltyPlayerSnapshot
              .Child("stats")
              .Child("support")
              .Child(areaId.ToString());

          if (areaSupportSnapshot.Exists)
          {
              int areaSupport = Convert.ToInt32(areaSupportSnapshot.Value);

              if (areaSupport > 0)
              {
                  int currentMoney = Convert.ToInt32(playerSnapshot.Child("stats").Child("money").Value);
                  int newMoney = Math.Max(currentMoney - 10, 0);

                  await dbRefPlayer.Child("stats").Child("money").SetValueAsync(newMoney);
                  await dbRefPlayer.Child("budgetPenalty").RemoveValueAsync();

              }

          }
          else
          {
              Debug.Log($"Brak danych o poparciu dla regionu {areaId} w statystykach gracza {budgetPenaltyPlayerId}.");
          }
      }

    public async Task<bool> CheckSupportBlock(string playerId)
    {
        string lobbyId = DataTransfer.LobbyId;

        var playerSnapshot = await FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .GetValueAsync();

        if (!playerSnapshot.Exists || !playerSnapshot.HasChild("blockSupport")) return false;

        var blockSupportSnapshot = playerSnapshot.Child("blockSupport");
        string blockSupportPlayerId = blockSupportSnapshot.Child("playerId").Value?.ToString();

        if (string.IsNullOrEmpty(blockSupportPlayerId)) return false;

        var blockSupportPlayerSnapshot = await FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(blockSupportPlayerId)
            .GetValueAsync();

        if (!blockSupportPlayerSnapshot.Exists) return false;

        return CheckTurnsTakenForBlock(blockSupportSnapshot, blockSupportPlayerSnapshot);
    }

    public async Task<bool> CheckBudgetBlock(string playerId)
    {
        string lobbyId = DataTransfer.LobbyId;

        var playerSnapshot = await FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .GetValueAsync();

        if (!playerSnapshot.Exists || !playerSnapshot.HasChild("blockBudget")) return false;

        var blockBudgetSnapshot = playerSnapshot.Child("blockBudget");
        string blockBudgetPlayerId = blockBudgetSnapshot.Child("playerId").Value?.ToString();

        if (string.IsNullOrEmpty(blockBudgetPlayerId)) return false;

        var blockBudgetPlayerSnapshot = await FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(blockBudgetPlayerId)
            .GetValueAsync();

        if (!blockBudgetPlayerSnapshot.Exists) return false;

        return CheckTurnsTakenForBlock(blockBudgetSnapshot, blockBudgetPlayerSnapshot);
    }

    public async Task<bool> CheckIncomeBlock(string playerId)
    {
        string lobbyId = DataTransfer.LobbyId;

        var playerSnapshot = await FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .GetValueAsync();

        if (!playerSnapshot.Exists || !playerSnapshot.HasChild("blockIncome")) return false;

        var blockIncomeSnapshot = playerSnapshot.Child("blockIncome");
        string blockIncomePlayerId = blockIncomeSnapshot.Child("playerId").Value?.ToString();

        if (string.IsNullOrEmpty(blockIncomePlayerId)) return false;

        var blockIncomePlayerSnapshot = await FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(blockIncomePlayerId)
            .GetValueAsync();

        if (!blockIncomePlayerSnapshot.Exists) return false;

        return CheckTurnsTakenForBlock(blockIncomeSnapshot, blockIncomePlayerSnapshot);
    }

    public async Task<bool> CheckIncreaseCostAllTurn(string playerId)
    {
        string lobbyId = DataTransfer.LobbyId;

        var playerSnapshot = await FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .GetValueAsync();

        if (!playerSnapshot.Exists || !playerSnapshot.HasChild("increaseCostAllTurn")) return false;

        var increaseCostSnapshot = playerSnapshot.Child("increaseCostAllTurn");
        int increaseCostTurn = Convert.ToInt32(increaseCostSnapshot.Child("turnsTaken").Value);
        int currentTurn = Convert.ToInt32(playerSnapshot.Child("stats").Child("turnsTaken").Value);

        return increaseCostTurn == currentTurn;
    }

    public async Task<bool> CheckDecreaseCost(string playerId)
    {
        string lobbyId = DataTransfer.LobbyId;

        var playerSnapshot = await FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .GetValueAsync();

        if (!playerSnapshot.Exists || !playerSnapshot.HasChild("decreaseCost")) return false;

        var decreaseCostSnapshot = playerSnapshot.Child("decreaseCost");
        int decreaseCostTurn = Convert.ToInt32(decreaseCostSnapshot.Child("turnsTaken").Value);
        int currentTurn = Convert.ToInt32(playerSnapshot.Child("stats").Child("turnsTaken").Value);

        return decreaseCostTurn == currentTurn;
    }

    public async Task CheckIfPlayed2Cards(string playerId)
    {
        if (string.IsNullOrEmpty(playerId)) return;

        var twoCardsSnapshot = await FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(DataTransfer.LobbyId)
            .Child("players")
            .Child(playerId)
            .Child("twoCards")
            .GetValueAsync();

        if (!twoCardsSnapshot.Exists) return;

        int turnsTakenInTwoCards = Convert.ToInt32(twoCardsSnapshot.Child("turnsTaken").Value);
        int played = Convert.ToInt32(twoCardsSnapshot.Child("played").Value);
        string relatedPlayerId = twoCardsSnapshot.Child("playerId").Value?.ToString();

        var relatedPlayerStatsSnapshot = await FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(DataTransfer.LobbyId)
            .Child("players")
            .Child(relatedPlayerId)
            .Child("stats")
            .GetValueAsync();

        if (!relatedPlayerStatsSnapshot.Exists) return;

        int turnsTakenForRelatedPlayer = Convert.ToInt32(relatedPlayerStatsSnapshot.Child("turnsTaken").Value);

        if (turnsTakenInTwoCards == turnsTakenForRelatedPlayer)
        {
            if (played == 0)
            {
                await FirebaseInitializer.DatabaseReference
                    .Child("sessions")
                    .Child(DataTransfer.LobbyId)
                    .Child("players")
                    .Child(playerId)
                    .Child("twoCards")
                    .Child("played")
                    .SetValueAsync(1);
            }
            else if (played == 1)
            {
                int currentMoney = Convert.ToInt32(relatedPlayerStatsSnapshot.Child("money").Value);
                int updatedMoney = currentMoney + 5;

                await FirebaseInitializer.DatabaseReference
                    .Child("sessions")
                    .Child(DataTransfer.LobbyId)
                    .Child("players")
                    .Child(relatedPlayerId)
                    .Child("stats")
                    .Child("money")
                    .SetValueAsync(updatedMoney);

                await FirebaseInitializer.DatabaseReference
                    .Child("sessions")
                    .Child(DataTransfer.LobbyId)
                    .Child("players")
                    .Child(playerId)
                    .Child("twoCards")
                    .RemoveValueAsync();
            }
        }
    }

    public async Task CheckBonusBudget(string playerId, int value)
    {
        if (string.IsNullOrEmpty(playerId) || value <= 0) return;

        var bonusBudgetSnapshot = await FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(DataTransfer.LobbyId)
            .Child("players")
            .Child(playerId)
            .Child("bonusBudget")
            .GetValueAsync();

        if (!bonusBudgetSnapshot.Exists) return;

        int bonusBudgetTurnsTaken = Convert.ToInt32(bonusBudgetSnapshot.Child("turnsTaken").Value);
        var playerStatsSnapshot = await FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(DataTransfer.LobbyId)
            .Child("players")
            .Child(playerId)
            .Child("stats")
            .GetValueAsync();

        if (!playerStatsSnapshot.Exists) return;

        int playerTurnsTaken = Convert.ToInt32(playerStatsSnapshot.Child("turnsTaken").Value);

        if (bonusBudgetTurnsTaken == playerTurnsTaken)
        {
            int currentMoney = Convert.ToInt32(playerStatsSnapshot.Child("money").Value);
            int updatedMoney = currentMoney + 5;
            await FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(DataTransfer.LobbyId)
                .Child("players")
                .Child(playerId)
                .Child("stats")
                .Child("money")
                .SetValueAsync(updatedMoney);
        }
    }

    public async Task<bool> CheckIfProtectedOneCard(string playerId, int value)
    {
        if (value >= 0)
        {
            return false;
        }

        string lobbyId = DataTransfer.LobbyId;
        var dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId);

        var playerSnapshot = await dbRefPlayer.GetValueAsync();

        if (playerSnapshot.Exists)
        {
            var protectedSnapshot = playerSnapshot.Child("protected").Child("allOneCard");
            var turnsTakenSnapshot = playerSnapshot.Child("stats").Child("turnsTaken");

            if (protectedSnapshot.Exists && turnsTakenSnapshot.Exists)
            {
                if (int.TryParse(protectedSnapshot.Value?.ToString(), out int protectedTurn) &&
                    int.TryParse(turnsTakenSnapshot.Value?.ToString(), out int turnsTaken))
                {
                    if (turnsTaken == protectedTurn)
                    {
                        await dbRefPlayer.Child("protected").Child("allOneCard").RemoveValueAsync();
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public async Task<bool> CheckIgnoreCost(string playerId)
    {
        string lobbyId = DataTransfer.LobbyId;

        var playerSnapshot = await FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .GetValueAsync();

        return playerSnapshot.Exists && playerSnapshot.HasChild("ignoreCost");
    }

    public async Task<bool> CheckCardLimit(string playerId)
{
    if (string.IsNullOrEmpty(playerId)) return false;

    string lobbyId = DataTransfer.LobbyId;
    var dbRefLimitCards = FirebaseInitializer.DatabaseReference
        .Child("sessions")
        .Child(lobbyId)
        .Child("players")
        .Child(playerId)
        .Child("limitCards");

    var limitCardsSnapshot = await dbRefLimitCards.GetValueAsync();
    if (!limitCardsSnapshot.Exists) return false;

    string limitCardsPlayerId = limitCardsSnapshot.Child("playerId").Value?.ToString();
    int limitCardsTurnsTaken = Convert.ToInt32(limitCardsSnapshot.Child("turnsTaken").Value);
    int playedCards = Convert.ToInt32(limitCardsSnapshot.Child("playedCards").Value);

    if (string.IsNullOrEmpty(limitCardsPlayerId)) return false;

    var dbRefPlayerStats = FirebaseInitializer.DatabaseReference
        .Child("sessions")
        .Child(lobbyId)
        .Child("players")
        .Child(limitCardsPlayerId)
        .Child("stats");

    var playerStatsSnapshot = await dbRefPlayerStats.GetValueAsync();
    if (!playerStatsSnapshot.Exists || !playerStatsSnapshot.Child("turnsTaken").Exists) return false;

    int playerTurnsTaken = Convert.ToInt32(playerStatsSnapshot.Child("turnsTaken").Value);

    if (limitCardsTurnsTaken != playerTurnsTaken) return false;

    if (playedCards == 1) return true;

    if (playedCards == 0 || playedCards == -1)
    {
        await dbRefLimitCards.Child("playedCards").SetValueAsync(playedCards == 0 ? 1 : 0);
        return false;
    }

    return false;
}

    public async Task<int> CheckBonusSupport(string playerId, int value)
    {
        if (string.IsNullOrEmpty(playerId) || value <= 0) return value;

        var dbRefBonusSupport = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(DataTransfer.LobbyId)
            .Child("players")
            .Child(playerId)
            .Child("bonusSupport");

        var bonusSupportSnapshot = await dbRefBonusSupport.GetValueAsync();
        if (!bonusSupportSnapshot.Exists) return value;

        int bonusSupportTurnsTaken = Convert.ToInt32(bonusSupportSnapshot.Child("turnsTaken").Value);

        var dbRefPlayerStats = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(DataTransfer.LobbyId)
            .Child("players")
            .Child(playerId)
            .Child("stats");

        var playerStatsSnapshot = await dbRefPlayerStats.GetValueAsync();
        if (!playerStatsSnapshot.Exists) return value;

        int playerTurnsTaken = Convert.ToInt32(playerStatsSnapshot.Child("turnsTaken").Value);

        if (bonusSupportTurnsTaken != playerTurnsTaken) return value;

        await dbRefBonusSupport.RemoveValueAsync();
        return 2 * value;
    }

    public bool CheckTurnsTakenForBlock(DataSnapshot blockSnapshot, DataSnapshot blockPlayerSnapshot)
    {
        if (blockSnapshot.Child("turnsTaken").Value?.ToString() == blockPlayerSnapshot.Child("stats").Child("turnsTaken").Value?.ToString())
        {
            return true;
        }

        return false;
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
