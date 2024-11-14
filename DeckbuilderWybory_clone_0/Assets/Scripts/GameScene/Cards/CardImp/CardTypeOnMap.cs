using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class CardTypeOnMap : MonoBehaviour
{

    DatabaseReference dbRef;
    string lobbyId;
    string playerId;
    string cardId;
    string moneyAddValue;
    string money;

    public void OnCardDropped(string cardIdDropped)
    {
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;
        cardId = cardIdDropped;

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        // Inicjalizacja referencji do bazy danych Firebase
        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId);

        dbRef.Child("deck").Child(cardId).Child("played").ValueChanged += HandlePlayedCard;
    }

    void HandlePlayedCard(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.Log(args.DatabaseError.Message);
            return;
        }


        //pobierz z bazy ile money trzeba dodaæ(z karty)
        dbRef.Child("deck").Child(cardId).Child("cardValue").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                // Handle the error...
                Debug.Log("Error fetching money value: " + task.Exception);
                return;
            }

            DataSnapshot snapshot = task.Result;
            moneyAddValue = snapshot.Value.ToString();
            Debug.Log("Money to be added: " + moneyAddValue);
        });

        Debug.Log("money to be added" + moneyAddValue);
        //pobierz ile money ma gracz

        dbRef.Child("stats").Child("money").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                // Handle the error...
                Debug.Log("Error fetching money value: " + task.Exception);
                return;
            }

            DataSnapshot snapshot = task.Result;
            money = snapshot.Value.ToString();
            Debug.Log("Money player has: " + money);
        });

        //zaktualizuj wartoœæ i wsadz do bazy danych
        money += moneyAddValue;
        dbRef.Child("stats").Child("money").SetValueAsync(money);

    }
}
