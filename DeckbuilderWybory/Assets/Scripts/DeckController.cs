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

    public TextAsset cardDataJsonFile; // Referencja do pliku JSON zawieraj¹cego dane kart

    [System.Serializable]
    public class CardData
    {
        public string cardName;
        public string cardType;
        public int cardValue;
        public int cardCost;
        public bool onHand;
        public bool played;
    }

    [System.Serializable]
    public class CardsList
    {
        public CardData[] cardData;
    }

    public CardsList myCardsList = new CardsList();

    void Start()
    {
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        // SprawdŸ, czy Firebase jest ju¿ zainicjalizowany
        if (FirebaseApp.DefaultInstance == null)
        {
            // Jeœli nie, inicjalizuj Firebase
            FirebaseInitializer firebaseInitializer = FindObjectOfType<FirebaseInitializer>();
            if (firebaseInitializer == null)
            {
                Debug.LogError("FirebaseInitializer not found in the scene!");
                return;
            }
        }

        // Inicjalizacja referencji do bazy danych Firebase
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId).Child("players");

        //CardsList cardsList = JsonUtility.FromJson<CardsList>(cardDataJsonFile.text);
        myCardsList = JsonUtility.FromJson<CardsList>(cardDataJsonFile.text);

        // Dodanie kart do bazy Firebase
        for (int i = 0; i < myCardsList.cardData.Length; i++)
        {
            // Konwertuj obiekt CardData na s³ownik
            Dictionary<string, object> cardDataDict = new Dictionary<string, object>
            {
                { "cardName", myCardsList.cardData[i].cardName },
                { "cardType", myCardsList.cardData[i].cardType },
                { "cardValue", myCardsList.cardData[i].cardValue },
                { "cardCost", myCardsList.cardData[i].cardCost },
                { "onHand", myCardsList.cardData[i].onHand },
                { "played", myCardsList.cardData[i].played }
            };

            // Dodaj kartê do bazy Firebase
            dbRef.Child(playerId).Child("deck").Child(i.ToString()).SetValueAsync(cardDataDict);
        }
    }
}