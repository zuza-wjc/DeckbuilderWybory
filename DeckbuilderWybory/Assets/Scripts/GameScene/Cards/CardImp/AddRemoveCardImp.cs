using System;
using System.Collections.Generic;
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

    private string lobbyId = DataTransfer.LobbyId;
    private string playerId = DataTransfer.PlayerId;

    private bool budgetChange;
    private bool incomeChange;
    private bool supportChange;

    private int cost;
    private int playerBudget;
    private int enemyBudget;
    private int playerIncome;
    private int support;
    private string enemyId;
    private int enemyIncome;

    private int chosenRegion;
    private int maxAreaSupport;
    private int currentAreaSupport;

    private Dictionary<int, BudgetOptionData> budgetOptionsDictionary = new Dictionary<int, BudgetOptionData>();
    private Dictionary<int, IncomeOptionData> incomeOptionsDictionary = new Dictionary<int, IncomeOptionData>();
    private Dictionary<int, SupportOptionData> supportOptionsDictionary = new Dictionary<int, SupportOptionData>();

    public PlayerListManager playerListManager;
    public MapManager mapManager;

    void Start()
    {
        playerListManager.Initialize(lobbyId, playerId);
    }


    public async void CardLibrary(string cardIdDropped)
    {
        // Zerowanie zmiennych przed ka¿dym wywo³aniem
        budgetChange = false;
        incomeChange = false;
        supportChange = false;

        cost = 0;
        playerBudget = 0;
        enemyBudget = 0;
        playerIncome = 0;
        support = 0;
        enemyId = string.Empty;
        enemyIncome = 0;

        chosenRegion = 0;
        maxAreaSupport = 0;
        currentAreaSupport = 0;

        budgetOptionsDictionary.Clear();
        incomeOptionsDictionary.Clear();
        supportOptionsDictionary.Clear();

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

            DataSnapshot budgetSnapshot = snapshot.Child("budget");
            if (budgetSnapshot.Exists)
            {
                budgetChange = true;

                int optionIndex = 0;
                foreach (var optionSnapshot in budgetSnapshot.Children)
                {
                    DataSnapshot numberSnapshot = optionSnapshot.Child("number");
                    DataSnapshot targetSnapshot = optionSnapshot.Child("target");

                    if (numberSnapshot.Exists && targetSnapshot.Exists)
                    {
                        int number = Convert.ToInt32(numberSnapshot.Value);
                        string target = targetSnapshot.Value.ToString();

                        budgetOptionsDictionary.Add(optionIndex, new BudgetOptionData(number, target));
                    }
                    else
                    {
                        Debug.LogError($"Option {optionIndex} is missing 'number' or 'target'.");
                        return;
                    }

                    optionIndex++;
                }
            }

            DataSnapshot incomeSnapshot = snapshot.Child("income");
            if (incomeSnapshot.Exists)
            {
                incomeChange = true;

                int optionIndex = 0;
                foreach (var optionSnapshot in incomeSnapshot.Children)
                {
                    DataSnapshot numberSnapshot = optionSnapshot.Child("number");
                    DataSnapshot targetSnapshot = optionSnapshot.Child("target");

                    if (numberSnapshot.Exists && targetSnapshot.Exists)
                    {
                        int number = Convert.ToInt32(numberSnapshot.Value);
                        string target = targetSnapshot.Value.ToString();

                        incomeOptionsDictionary.Add(optionIndex, new IncomeOptionData(number, target));
                    }
                    else
                    {
                        Debug.LogError($"Option {optionIndex} is missing 'number' or 'target'.");
                        return;
                    }

                    optionIndex++;
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

                    if (numberSnapshot.Exists && targetSnapshot.Exists)
                    {
                        int number = Convert.ToInt32(numberSnapshot.Value);
                        string target = targetSnapshot.Value.ToString();

                        supportOptionsDictionary.Add(optionIndex, new SupportOptionData(number, target));
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

        if (budgetChange)
        {
            await BudgetAction();
        }

        if (incomeChange)
        {
            await IncomeAction();
        }

        if (supportChange)
        {
            await SupportAction();
        }

        await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget - cost);

        dbRefPlayerDeck = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck").Child(cardIdDropped);

        await dbRefPlayerDeck.Child("onHand").SetValueAsync(false);
        await dbRefPlayerDeck.Child("played").SetValueAsync(true);
    }


    private async Task BudgetAction()
    {
        foreach (var data in budgetOptionsDictionary.Values)
        {
            if (data.Target == "player")
            {
                playerBudget += data.Number;
            }
            else if (data.Target == "enemy")
            {
                enemyId = await playerListManager.SelectEnemyPlayer();
                await ChangeEnemyBudget(enemyId, data.Number);
            }
        }
    }

    private async Task SupportAction()
    {
        foreach (var data in supportOptionsDictionary.Values)
        {
            if (data.Target == "enemy-region")
            {
                chosenRegion = await mapManager.SelectArea();
                enemyId = await playerListManager.SelectEnemyPlayerInArea(chosenRegion);
                await ChangeSupport(enemyId, data.Number, chosenRegion);

            } else if (data.Target == "player-region")
            {
                await ChangeSupport(playerId, data.Number, chosenRegion);
            }
        }
    }


    private async Task IncomeAction()
    {
        foreach (var data in incomeOptionsDictionary.Values)
        {
            if (data.Target == "player")
            {
                playerIncome += data.Number;
                await dbRefPlayerStats.Child("income").SetValueAsync(playerIncome);
            } else if (data.Target == "enemy")
            {
                await ChangeEnemyIncome(enemyId, data.Number);
            }
        }
    }


    private async Task ChangeEnemyBudget(string enemyId, int value)
    {
        dbRefEnemyStats = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(enemyId).Child("stats");
        var snapshot = await dbRefEnemyStats.GetValueAsync();

        if (snapshot.Exists)
        {
            DataSnapshot enemyMoneySnapshot = snapshot.Child("money");
            if (enemyMoneySnapshot.Exists)
            {
                enemyBudget = Convert.ToInt32(enemyMoneySnapshot.Value);
            }
            else
            {
                Debug.LogError("Branch money does not exist.");
                return;
            }

        } else {
            Debug.LogError("No enemy data found in the database.");
            return;
        }

        enemyBudget += value;

        if(enemyBudget < 0)
        {
            enemyBudget = 0;
        }

        await dbRefEnemyStats.Child("money").SetValueAsync(enemyBudget);

    }

    private async Task ChangeEnemyIncome(string enemyId, int value)
    {
        dbRefEnemyStats = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(enemyId).Child("stats");
        var snapshot = await dbRefEnemyStats.GetValueAsync();

        if (snapshot.Exists)
        {
            DataSnapshot enemyIncomeSnapshot = snapshot.Child("income");
            if (enemyIncomeSnapshot.Exists)
            {
                enemyIncome = Convert.ToInt32(enemyIncomeSnapshot.Value);
            }
            else
            {
                Debug.LogError("Branch income does not exist.");
                return;
            }

        }
        else
        {
            Debug.LogError("No enemy data found in the database.");
            return;
        }

        enemyIncome += value;

        if (enemyIncome < 0)
        {
            enemyIncome = 0;
        }

        await dbRefEnemyStats.Child("income").SetValueAsync(enemyIncome);

    }

    private async Task ChangeSupport(string playerId, int value, int areaId)
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

        if (snapshot.Exists)
        {
            support = Convert.ToInt32(snapshot.Value);
        }
        else
        {
            Debug.LogError("No support data found for the given region in the player's stats.");
            return;
        }

        maxAreaSupport = await mapManager.GetMaxSupportForRegion(areaId);
        currentAreaSupport = await mapManager.GetCurrentSupportForRegion(areaId, playerId);

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




}

public class BudgetOptionData
{
    public int Number { get; }
    public string Target { get; }

    public BudgetOptionData(int number, string target)
    {
        Number = number;
        Target = target;
    }
}

public class IncomeOptionData
{
    public int Number { get; }
    public string Target { get; }

    public IncomeOptionData(int number, string target)
    {
        Number = number;
        Target = target;
    }
}

public class SupportOptionData
{
    public int Number { get; }
    public string Target { get; }

    public SupportOptionData(int number, string target)
    {
        Number = number;
        Target = target;
    }
}