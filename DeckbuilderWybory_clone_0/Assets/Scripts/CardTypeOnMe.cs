using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CardTypeOnMe : MonoBehaviour
{
    DatabaseReference dbRef;
    string lobbyId;
    string playerId;
    string cardId;
    int moneyAddValue;
    int money;

    public void OnCardDropped(string cardIdDropped)
    {
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;
        cardId = cardIdDropped;

        if (FirebaseApp.DefaultInstance == null)
        {
            // Jesli nie, inicjalizuj Firebase
            FirebaseInitializer firebaseInitializer = FindObjectOfType<FirebaseInitializer>();
            if (firebaseInitializer == null)
            {
                Debug.LogError("FirebaseInitializer not found in the scene!");
                return;
            }
        }

        // Inicjalizacja referencji do bazy danych Firebase
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId);

        dbRef.Child("deck").Child(cardId).Child("played").ValueChanged += HandlePlayedCard;
    }

    async void HandlePlayedCard(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.Log(args.DatabaseError.Message);
            return;
        }

        await GetCardValue();
        await GetMoney();

        //zaktualizuj wartoœæ i wsadz do bazy danych
        money += moneyAddValue;
        Debug.Log("FINAL Money to be added: " + moneyAddValue);
        Debug.Log("FINAL Money player has: " + money);

        await dbRef.Child("stats").Child("money").SetValueAsync(money);
    }

    async Task GetCardValue()
    {
        //pobierz z bazy ile money trzeba dodaæ(z karty)
        var task = await dbRef.Child("deck").Child(cardId).Child("cardValue").GetValueAsync();
        if (task == null || task.Value == null)
        {
            Debug.Log("Error fetching money value");
            return;
        }

        moneyAddValue = int.Parse(task.Value.ToString());
        Debug.Log("Money to be added: " + moneyAddValue);
    }

    async Task GetMoney()
    {
        //pobierz ile money ma gracz
        var task = await dbRef.Child("stats").Child("money").GetValueAsync();
        if (task == null || task.Value == null)
        {
            Debug.Log("Error fetching money value");
            return;
        }

        money = int.Parse(task.Value.ToString());
        Debug.Log("Money player has: " + money);
    }
}