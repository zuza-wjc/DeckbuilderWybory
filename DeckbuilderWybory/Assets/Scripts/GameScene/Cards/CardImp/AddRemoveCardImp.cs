using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public class AddRemoveCardImp : MonoBehaviour
{
    private DatabaseReference dbRefCard;
    private DatabaseReference dbRefPlayerStats;
    private DatabaseReference dbRefPlayerDeck;
    private DatabaseReference dbRefPlayers;
    private DatabaseReference dbRefEnemyStats;

    private string lobbyId = DataTransfer.LobbyId;
    private string playerId = DataTransfer.PlayerId;

    private bool budgetChange = false;
    private bool incomeChange = false;
    private int cost;
    private int playerBudget;
    private int enemyBudget;
    private int playerIncome;

    private Dictionary<int, BudgetOptionData> budgetOptionsDictionary = new Dictionary<int, BudgetOptionData>();
    private Dictionary<int, IncomeOptionData> incomeOptionsDictionary = new Dictionary<int, IncomeOptionData>();

    public GameObject playerListPanel;

    string enemyId;

    public GameObject buttonTemplate;
    public GameObject scrollViewContent;

    private Dictionary<string, string> playerNameToIdMap = new Dictionary<string, string>();

    public async void CardLibrary(string cardIdDropped)
    {
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
                budgetOptionsDictionary.Clear();

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
                incomeOptionsDictionary.Clear();

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
                    Debug.LogError("Brak bud¿etu aby zagraæ kartê."); //trzeba zrobiæ okienko które o tym informuje
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
            await IncomeChangeAction();
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
                await FetchPlayersList();
                enemyId = await WaitForEnemySelection();
                await ChangeEnemyBudget(enemyId,data.Number);
               
            }
        }
    }


    private async Task IncomeChangeAction()
    {
        foreach (var data in incomeOptionsDictionary.Values)
        {
            if (data.Target == "player")
            {
                playerIncome += data.Number;
                await dbRefPlayerStats.Child("income").SetValueAsync(playerIncome); // Asynchroniczna operacja
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
        }

        enemyBudget += value;

        if(enemyBudget < 0)
        {
            enemyBudget = 0;
        }

        await dbRefEnemyStats.Child("money").SetValueAsync(enemyBudget);

    }

    private async Task FetchPlayersList()
    {
        dbRefPlayers = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);
        var snapshot = await dbRefPlayers.GetValueAsync(); // Czekaj na wynik operacji.

        if (snapshot.Exists)
        {
            foreach (var childSnapshot in snapshot.Child("players").Children)
            {
                string otherPlayerId = childSnapshot.Key;
                if (otherPlayerId != playerId)
                {
                    string otherPlayerName = childSnapshot.Child("playerName").Value.ToString();
                    playerNameToIdMap[otherPlayerName] = otherPlayerId;

                    CreateButton(otherPlayerName); // Twórz przyciski dla graczy.
                }
            }

            playerListPanel.SetActive(true); // Wyœwietl panel wyboru gracza.
            playerListPanel.transform.SetAsLastSibling();
        }
        else
        {
            Debug.LogError("No player data found in the database.");
        }
    }


    void CreateButton(string otherPlayerName)
    {
        GameObject button = Instantiate(buttonTemplate, scrollViewContent.transform);
        button.SetActive(true);
        button.GetComponentInChildren<UnityEngine.UI.Text>().text = otherPlayerName;

        // Dodanie funkcji obs³ugi zdarzenia dla klikniêcia w przycisk
        button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => TaskOnClick(otherPlayerName));
    }


    private Action<string> TaskOnClickCompleted;

    private Task<string> WaitForEnemySelection()
    {
        var tcs = new TaskCompletionSource<string>();
        TaskOnClickCompleted = (selectedEnemyId) =>
        {
            tcs.TrySetResult(selectedEnemyId);
        };

        return tcs.Task;
    }


    public void TaskOnClick(string otherPlayerName)
    {
        Debug.Log($"Clicked on {otherPlayerName}");

        // SprawdŸ, czy wybrany gracz istnieje w mapie
        if (playerNameToIdMap.TryGetValue(otherPlayerName, out string otherPlayerId))
        {
            enemyId = otherPlayerId;
            TaskOnClickCompleted?.Invoke(enemyId); // Rozwi¹zanie TaskCompletionSource.
        }
        else
        {
            Debug.LogError($"PlayerId not found for the given playerName: {otherPlayerName}");
            return;
        }

        playerListPanel.SetActive(false); // Zamknij panel wyboru gracza.
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
