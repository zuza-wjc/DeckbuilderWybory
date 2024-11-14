using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Database;
using UnityEngine;

public class AddRemoveCardImp : MonoBehaviour
{
    private DatabaseReference dbRefCard;
    private DatabaseReference dbRefGame;
    private DatabaseReference dbRefDeck;

    private string lobbyId = DataTransfer.LobbyId;
    private string playerId = DataTransfer.PlayerId;

    private bool budgetChange = false;
    private bool incomeChange = false;
    private int cost;
    private int playerBudget;
    private int playerIncome;

    private Dictionary<int, BudgetOptionData> budgetOptionsDictionary = new Dictionary<int, BudgetOptionData>();
    private Dictionary<int, IncomeOptionData> incomeOptionsDictionary = new Dictionary<int, IncomeOptionData>();

    // Przekszta³æ metodê na asynchroniczn¹
    public async void cardLibrary(string cardIdDropped)
    {
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRefCard = FirebaseInitializer.DatabaseReference.Child("cards").Child("id").Child("addRemove").Child(cardIdDropped);

        // Czekaj na pobranie danych z Firebase
        DataSnapshot snapshot = await dbRefCard.GetValueAsync();

        if (snapshot.Exists)
        {
            // Przetwarzanie danych karty
            DataSnapshot costSnapshot = snapshot.Child("cost");
            if (costSnapshot.Exists)
            {
                cost = Convert.ToInt32(costSnapshot.Value);
            }
            else
            {
                Debug.Log("Branch cost does not exist.");
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
                        Debug.Log($"Option {optionIndex} is missing 'number' or 'target'.");
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
                        Debug.Log($"Option {optionIndex} is missing 'number' or 'target'.");
                        return;
                    }

                    optionIndex++;
                }
            }

        }
        else
        {
            Debug.Log("No data for: " + cardIdDropped + ".");
            return;
        }

        dbRefGame = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("stats");

        DataSnapshot playerStatsSnapshot = await dbRefGame.GetValueAsync();

        if (playerStatsSnapshot.Exists)
        {
            // Przetwarzanie danych gracza
            DataSnapshot moneySnapshot = playerStatsSnapshot.Child("money");
            if (moneySnapshot.Exists)
            {
                playerBudget = Convert.ToInt32(moneySnapshot.Value);
                if (playerBudget < cost)
                {
                    Debug.Log("Brak bud¿etu aby zagraæ kartê.");
                    return;
                }
            }
            else
            {
                Debug.Log("Branch money does not exist.");
                return;
            }

            DataSnapshot incomeSnapshot = playerStatsSnapshot.Child("income");
            if (incomeSnapshot.Exists)
            {
                playerIncome = Convert.ToInt32(incomeSnapshot.Value);
            }
            else
            {
                Debug.Log("Branch income does not exist.");
                return;
            }
        }
        else
        {
            Debug.Log("No data for: " + cardIdDropped + ".");
            return;
        }

        if (budgetChange)
        {
            Debug.Log("Bud¿et gracza przed: " + playerBudget);
            budgetChangeAction();
            Debug.Log("Bud¿et gracza po: " + playerBudget);
        }

        if (incomeChange)
        {
            Debug.Log("Przychód gracza przed: " + playerIncome);
            incomeChangeAction();
            Debug.Log("Przychód gracza po: " + playerIncome);
        }

        await dbRefGame.Child("money").SetValueAsync(playerBudget - cost);
        await dbRefGame.Child("income").SetValueAsync(playerIncome);

        dbRefDeck = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck").Child(cardIdDropped);

        await dbRefDeck.Child("onHand").SetValueAsync(false);
        await dbRefDeck.Child("played").SetValueAsync(true);
    }

    private void budgetChangeAction()
    {
        for (int i = 0; i < budgetOptionsDictionary.Count; i++)
        {
            BudgetOptionData data = budgetOptionsDictionary[i];

            if (data.Target == "player")
            {
                playerBudget += data.Number;
            }
        }
    }

    private void incomeChangeAction()
    {
        for (int i = 0; i < incomeOptionsDictionary.Count; i++)
        {
            IncomeOptionData data = incomeOptionsDictionary[i];

            if (data.Target == "player")
            {
                playerIncome += data.Number;
            }
        }
    }

    // Klasa przechowuj¹ca dane opcji (number i target)
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
}
