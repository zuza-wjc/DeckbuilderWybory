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

        await CheckIfProtected(playerId, support, supportToAdd);
        supportToAdd = await CheckIfRegionProtected(playerId, areaId, support, supportToAdd);

        support += supportToAdd;

        await dbRefSupport.SetValueAsync(support);

        await CheckAndAddCopySupport(playerId, areaId, supportToAdd,mapManager);
    }

    public async Task ChangeEnemyStat(string enemyId, int value, string statType, int playerBudget)
    {
        DatabaseReference dbRefEnemyStats;
        string lobbyId = DataTransfer.LobbyId;
        string playerId = DataTransfer.PlayerId;

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

            if (playerId == enemyId) { playerBudget = updatedStat; }

            await dbRefEnemyStats.Child(statType).SetValueAsync(updatedStat);

            if(statType == "money")
            {
                await CheckAndAddCopyBudget(enemyId, value);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while changing the enemy {statType}: {ex.Message}");
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

    public async Task CheckIfProtected(string playerId, int currentSupport, int value)
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
                .Child("all");

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

    public async Task<int> CheckIfRegionProtected(string playerId, int regionId, int currentSupport, int value)
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
                        return 0;
                    }
                }
            }
        }

        return value;
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

        if (!playerSnapshot.Exists ||
            !playerSnapshot.HasChild("cardBlocked") ||
            !playerSnapshot.HasChild("stats") ||
            !playerSnapshot.Child("stats").HasChild("turnsTaken"))
            return false;

        DataSnapshot cardBlockedSnapshot = playerSnapshot.Child("cardBlocked");
        DataSnapshot turnsTakenSnapshot = playerSnapshot.Child("stats").Child("turnsTaken");

        if (!cardBlockedSnapshot.HasChild("turnsTaken") || !cardBlockedSnapshot.HasChild("isBlocked"))
            return false;

        int cardBlockedTurn = Convert.ToInt32(cardBlockedSnapshot.Child("turnsTaken").Value);
        int currentTurn = Convert.ToInt32(turnsTakenSnapshot.Value);
        bool isBlocked = Convert.ToBoolean(cardBlockedSnapshot.Child("isBlocked").Value);

        if (cardBlockedTurn == currentTurn && isBlocked)
        {
            await dbRefPlayer.Child("cardBlocked").Child("isBlocked").SetValueAsync(false);
            Debug.Log("Card blocked");
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
