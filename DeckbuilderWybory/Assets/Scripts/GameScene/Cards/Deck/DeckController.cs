using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;

public class DeckController : MonoBehaviour
{
    DatabaseReference dbRef;
    string lobbyId;
    string playerId;

    public void InitializeDeck ()
    {
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        // SprawdŸ, czy Firebase jest ju¿ zainicjalizowany
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        // Inicjalizacja referencji do bazy danych Firebase
        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");

        // Dodaj karty do decka
        AddCardToDeck("AD003");
        AddCardToDeck("AD007");
        AddCardToDeck("AD016");

        Debug.Log("Deck loaded");
    }
    // Funkcja dodaj¹ca kartê do decka gracza
    void AddCardToDeck(string cardId)
    {
   
        bool onHand = true;  // Karta jest na rêce
        bool played = false; // Karta nie zosta³a jeszcze zagrana

        // Przygotowanie danych do zapisania
        Dictionary<string, object> cardDataDict = new Dictionary<string, object>
    {
        { "onHand", onHand },
        { "played", played }
    };

        // Dodajemy kartê do decku w Firebase
        dbRef.Child(playerId).Child("deck").Child(cardId.ToString()).SetValueAsync(cardDataDict)
            .ContinueWith(task => {
                if (!task.IsCompleted)
                {
                    Debug.LogError($"Failed to add card {cardId} to the deck: {task.Exception}");
                }
              
            });
    }

}
