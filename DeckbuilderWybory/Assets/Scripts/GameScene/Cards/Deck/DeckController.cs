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

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");

        // Dodaj karty do decka
        AddCardToDeck("AD081");

        Debug.Log("Deck loaded");
    }

    void AddCardToDeck(string cardId)
    {
   
        bool onHand = true;  
        bool played = false; 

        Dictionary<string, object> cardDataDict = new Dictionary<string, object>
    {
        { "onHand", onHand },
        { "played", played }
    };

        dbRef.Child(playerId).Child("deck").Child(cardId.ToString()).SetValueAsync(cardDataDict)
            .ContinueWith(task => {
                if (!task.IsCompleted)
                {
                    Debug.LogError($"Failed to add card {cardId} to the deck: {task.Exception}");
                }
              
            });
    }

}
