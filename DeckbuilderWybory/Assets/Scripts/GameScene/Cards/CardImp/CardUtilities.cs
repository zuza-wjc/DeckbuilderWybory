using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class CardUtilities : MonoBehaviour
{
    public void ProcessOptions(DataSnapshot optionSnapshot, Dictionary<int, OptionData> optionsDictionary)
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

    public void ProcessBonusOptions(DataSnapshot bonusSnapshot, Dictionary<int, OptionData> bonusOptionsDictionary)
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

        if (cardId == "AD020" || cardId == "AD054")
        {
            if (currentAreaSupport < value)
            {
                Debug.Log("Nie mo¿na zagraæ karty ze wzglêdu na niewystarczaj¹ce poparcie w tym regionie");
                return;
            }
        }

        support += supportToAdd;

        await dbRefSupport.SetValueAsync(support);
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
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while changing the enemy {statType}: {ex.Message}");
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