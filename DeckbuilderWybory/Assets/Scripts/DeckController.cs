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

    // Start is called before the first frame update
    void Start()
    {
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        // Sprawdü, czy Firebase jest juø zainicjalizowany
        if (FirebaseApp.DefaultInstance == null)
        {
            // Jeúli nie, inicjalizuj Firebase
            FirebaseInitializer firebaseInitializer = FindObjectOfType<FirebaseInitializer>();
            if (firebaseInitializer == null)
            {
                Debug.LogError("FirebaseInitializer not found in the scene!");
                return;
            }
        }

        // Inicjalizacja referencji do bazy danych Firebase
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId).Child("players");

        // Dodanie deckow graczowi
        dbRef.Child(playerId).Child("deck");
        string cardName = "nazwa";
        string cardType = "typ";
        int cardValue = 0;
        int cardCost = 0;
        bool onHand = true;
        bool played = false;

        Dictionary<string, object> cardData = new Dictionary<string, object>
        {
            { "cardName", cardName },
            { "cardType", cardType },
            { "cardValue", cardValue },
            { "cardCost", cardCost },
            { "onHand", onHand },
            { "played", played }
        };

        for (int i = 1; i < 5; i++)
        {
            // Dodawanie 4 kart do bazy Firebase
            dbRef.Child(playerId).Child("deck").Child(i.ToString()).SetValueAsync(cardData);
        };
    }
}
