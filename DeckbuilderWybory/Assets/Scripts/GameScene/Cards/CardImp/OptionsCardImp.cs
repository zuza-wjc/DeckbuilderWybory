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

    void Start()
    {
        playerListManager.Initialize(lobbyId, playerId);
    }


    public async void CardLibrary(string cardIdDropped, bool ignoreCost)
    {
        DatabaseReference dbRefCard;
        DatabaseReference dbRefPlayerStats;
        DatabaseReference dbRefPlayerDeck;

        bool budgetChange = false;
        bool supportChange = false;

        int cost;
        int playerBudget;
        string enemyId = string.Empty;
        string cardType;

        int chosenRegion = -1;
        bool isBonusRegion = false;

        Dictionary<int, OptionDataRandom> budgetOptionsDictionary = new();
        Dictionary<int, OptionDataRandom> budgetBonusOptionsDictionary = new();
        Dictionary<int, OptionDataRandom> incomeOptionsDictionary = new();
        Dictionary<int, OptionDataRandom> incomeBonusOptionsDictionary = new();
        Dictionary<int, OptionDataRandom> supportOptionsDictionary = new();
        Dictionary<int, OptionDataRandom> supportBonusOptionsDictionary = new();

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

        dbRefCard = FirebaseInitializer.DatabaseReference.Child("cards").Child("id").Child("options").Child(cardIdDropped);

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

            DataSnapshot budgetSnapshot = snapshot.Child("budget");
            if (budgetSnapshot.Exists)
            {
                budgetChange = true;
                ProcessBonusOptions(budgetSnapshot, budgetBonusOptionsDictionary);
                ProcessOptions(budgetSnapshot, budgetOptionsDictionary);
            }

            DataSnapshot supportSnapshot = snapshot.Child("support");
            if (supportSnapshot.Exists)
            {
                supportChange = true;
                ProcessBonusOptions(supportSnapshot, supportBonusOptionsDictionary);
                ProcessOptions(supportSnapshot, supportOptionsDictionary);
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
                if (!ignoreCost && playerBudget < cost)
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

        }
        else
        {
            Debug.LogError("No data for: " + cardIdDropped + ".");
            return;
        }

        if (supportChange)
        {
            (playerBudget, ignoreCost, isBonusRegion, enemyId) = await SupportAction(budgetChange, playerBudget, cardIdDropped,
                ignoreCost, chosenRegion, isBonusRegion, cardType, supportOptionsDictionary, supportBonusOptionsDictionary,
                enemyId, budgetOptionsDictionary);
        }

        if (budgetChange)
        {
            (dbRefPlayerStats, playerBudget) = await BudgetAction(dbRefPlayerStats, isBonusRegion, budgetOptionsDictionary, budgetBonusOptionsDictionary, playerBudget, enemyId);
        }

        if (!ignoreCost)
        {
            await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget - cost);
        }

        dbRefPlayerDeck = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck").Child(cardIdDropped);

        await dbRefPlayerDeck.Child("onHand").SetValueAsync(false);
        await dbRefPlayerDeck.Child("played").SetValueAsync(true);
    }

    private async Task<(DatabaseReference dbRefPlayerStats, int playerBudget)> BudgetAction(DatabaseReference dbRefPlayerStats,
        bool isBonusRegion,Dictionary<int, OptionDataRandom> budgetOptionsDictionary,Dictionary<int, OptionDataRandom> budgetBonusOptionsDictionary,
        int playerBudget, string enemyId)
    {
        var isBonus = isBonusRegion;
        var optionsToApply = isBonus ? budgetBonusOptionsDictionary : budgetOptionsDictionary;

        optionsToApply = RandomizeOption(optionsToApply);

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No options to apply.");
            return (dbRefPlayerStats,-1);
        }

        if (isBonus)
        {
            Debug.Log("Bonus region detected.");
        }

        foreach (var data in optionsToApply.Values)
        {
            if(data.Target == "player")
            {
                playerBudget += data.Number;
                await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);

            } else if(data.Target == "enemy")
            {
                if (string.IsNullOrEmpty(enemyId))
                {
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("Failed to select an enemy player.");
                        return (dbRefPlayerStats, -1);
                    }
                }
                await cardUtilities.ChangeEnemyStat(enemyId, data.Number, "money", playerBudget);
                await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
            }
        }
        return (dbRefPlayerStats, playerBudget);
    }

    private async Task<(int playerBudget, bool ignoreCost, bool isBonusRegion, string enemyId)> 
        SupportAction(bool budgetChange,int playerBudget, string cardId, bool ignoreCost, int chosenRegion, bool isBonusRegion,
        string cardType,Dictionary<int, OptionDataRandom> supportOptionsDictionary,Dictionary<int, OptionDataRandom> supportBonusOptionsDictionary,
        string enemyId, Dictionary<int, OptionDataRandom> budgetOptionsDictionary)
    {
        if (cardId == "OP011")
        {
            chosenRegion = await mapManager.SelectArea();
            isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
        }

        var isBonus = isBonusRegion;
        var optionsToApply = isBonus ? supportBonusOptionsDictionary : supportOptionsDictionary;

        if (cardId == "OP013")
        {
            chosenRegion = await mapManager.SelectArea();
            isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
        }

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
            return (-1,false,false,null);
        }

        if (isBonus)
        {
            Debug.Log("Bonus region detected.");
        }

        foreach (var data in optionsToApply.Values)
        {
            if (data.Target == "enemy-random")
            {
                enemyId = await playerListManager.SelectEnemyPlayer();
                chosenRegion = await cardUtilities.RandomizeRegion(enemyId, data.Number, mapManager);
                Debug.Log($"Wylosowany region to {chosenRegion}");
                await cardUtilities.ChangeSupport(enemyId, data.Number, chosenRegion, cardId, mapManager);
                RemoveTargetOption(ref budgetOptionsDictionary, "player");

            } else if(data.Target == "player-random")
            {
                chosenRegion = await cardUtilities.RandomizeRegion(playerId, data.Number, mapManager);
                Debug.Log($"Wylosowany region to {chosenRegion}");
                await cardUtilities.ChangeSupport(playerId, data.Number, chosenRegion, cardId, mapManager);
                RemoveTargetOption(ref budgetOptionsDictionary, "enemy");


            } else if (data.Target == "enemy-region")
            {
                if (chosenRegion < 0)
                {
                    chosenRegion = await mapManager.SelectArea();
                }
                    enemyId = await playerListManager.SelectEnemyPlayerInArea(chosenRegion);
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("No enemy player found in the area.");
                        return (-1, false, false, null);
                }

                    await cardUtilities.ChangeSupport(enemyId, data.Number, chosenRegion, cardId, mapManager);
                

            } else if (data.Target == "player-region")
            {
                if (chosenRegion < 0)
                {
                    chosenRegion = await mapManager.SelectArea();
                }

                await cardUtilities.ChangeSupport(playerId, data.Number, chosenRegion, cardId, mapManager);

                if (cardId == "OP011") { ignoreCost = UnityEngine.Random.Range(0, 2) == 0; }
                if (cardId == "OP013") { budgetChange = false; }
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
                DataSnapshot numberSnapshot = optionSnapshot.Child("number");
                DataSnapshot targetSnapshot = optionSnapshot.Child("target");
                DataSnapshot percentSnapshot = optionSnapshot.Child("percent");

                if (numberSnapshot.Exists && targetSnapshot.Exists)
                {
                    int number = Convert.ToInt32(numberSnapshot.Value);
                    string target = targetSnapshot.Value.ToString();
                    int percent = percentSnapshot.Exists ? Convert.ToInt32(percentSnapshot.Value) : 0;

                    int optionKey = Convert.ToInt32(optionSnapshot.Key.Replace("option", ""));

                    optionsDictionary.Add(optionKey, new OptionDataRandom(number, percent, target));
                }
                else
                {
                    Debug.LogError($"Option is missing 'number' or 'target'.");
                }
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
                DataSnapshot numberSnapshot = optionSnapshot.Child("number");
                DataSnapshot targetSnapshot = optionSnapshot.Child("target");
                DataSnapshot percentSnapshot = optionSnapshot.Child("percent");

                if (numberSnapshot.Exists && targetSnapshot.Exists)
                {
                    int number = Convert.ToInt32(numberSnapshot.Value);
                    string target = targetSnapshot.Value.ToString();
                    int percent = percentSnapshot.Exists ? Convert.ToInt32(percentSnapshot.Value) : 0;

                    bonusOptionsDictionary.Add(optionIndex, new OptionDataRandom(number, percent, target));
                }
                else
                {
                    Debug.LogError($"Bonus option {optionIndex} is missing 'number' or 'target'.");
                }

                optionIndex++;
            }
        }
    }

    public Dictionary<int, OptionDataRandom> RandomizeOption(Dictionary<int, OptionDataRandom> optionsDictionary)
    {
        if (optionsDictionary.Count == 1)
        {
            var newOptionsDictionary = new Dictionary<int, OptionDataRandom>
        {
            { 0, optionsDictionary.First().Value }
        };
            return newOptionsDictionary;
        }

        if (optionsDictionary.Count == 2)
        {
            int randomValue = UnityEngine.Random.Range(0, 2);

            var chosenOption = randomValue == 0 ? optionsDictionary.First().Value : optionsDictionary.Last().Value;

            var newOptionsDictionary = new Dictionary<int, OptionDataRandom>
        {
            { 0, chosenOption }
        };

            return newOptionsDictionary;
        }

        Debug.LogError("Options dictionary must contain either 1 or 2 options.");
        return new Dictionary<int, OptionDataRandom>();
    }

    public void RemoveTargetOption(ref Dictionary<int, OptionDataRandom> budgetOptionsDictionary, string targetToRemove)
    {
        if (budgetOptionsDictionary != null && budgetOptionsDictionary.Count > 0)
        {
            var optionToRemove = budgetOptionsDictionary.FirstOrDefault(kvp => kvp.Value.Target == targetToRemove);

            if (optionToRemove.Key != 0)
            { 
                budgetOptionsDictionary.Remove(optionToRemove.Key);
                Debug.Log($"Opcja z targetem '{targetToRemove}' zosta³a usuniêta.");
            }
            else
            {
                Debug.Log($"Nie znaleziono opcji z targetem '{targetToRemove}'.");
            }
        }
        else
        {
            Debug.Log("S³ownik 'budgetOptionsDictionary' jest pusty.");
        }
    }

    public void CheckBudget(ref Dictionary<int, OptionDataRandom> supportOptionsDictionary, int playerBudget)
    {
        if (playerBudget > 30)
        {
            var optionToRemove = supportOptionsDictionary.FirstOrDefault(kvp => kvp.Value.Number == -2);

            if (optionToRemove.Key != 0)
            {
                supportOptionsDictionary.Remove(optionToRemove.Key);
                Debug.Log("Opcja z 'number' == -2 zosta³a usuniêta.");
            }
            else
            {
                Debug.Log("Nie znaleziono opcji z 'number' == -2.");
            }
        }
        else
        {
            var optionToRemove = supportOptionsDictionary.FirstOrDefault(kvp => kvp.Value.Number == -4);

            if (optionToRemove.Key != 0)
            {
                supportOptionsDictionary.Remove(optionToRemove.Key);
                Debug.Log("Opcja z 'number' == -4 zosta³a usuniêta.");
            }
            else
            {
                Debug.Log("Nie znaleziono opcji z 'number' == -4.");
            }
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