using Firebase.Database;
using Firebase;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CardOnHandController : MonoBehaviour
{
    public GameObject cardPrefab;
    public GameObject cardListContainer;
    public CardSpriteManager cardSpriteManager;

    DatabaseReference dbRef;
    string lobbyId;
    string playerId;

    // Zmienna do przechowywania referencji do kart w UI
    private Dictionary<string, GameObject> cardObjects = new Dictionary<string, GameObject>();

    private void Awake()
    {
    
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        // Sprawdzenie inicjalizacji Firebase
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck");


        StartCoroutine(LoadCardsOnHand());
    }

    private IEnumerator LoadCardsOnHand()
    {
    
        var task = dbRef.GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError("Failed to load cards from Firebase: " + task.Exception);
            yield break;
        }


        DataSnapshot snapshot = task.Result;
        foreach (DataSnapshot cardSnapshot in snapshot.Children)
        {
            bool onHand = (bool)cardSnapshot.Child("onHand").Value;
            bool played = (bool)cardSnapshot.Child("played").Value;

            if (onHand && !played)  // Tylko karty, kt�re s� na r�ce i nie zosta�y zagrane
            {
       
                string cardId = cardSnapshot.Key;
                AddCardToUI(cardId);
                ListenForCardPlayed(cardId); // Nas�uchiwanie na zmiany "played"
            }
        }
        
    }

    private void AddCardToUI(string cardId)
    {
  
        GameObject newCard = Instantiate(cardPrefab, cardListContainer.transform);
        Image cardImage = newCard.GetComponent<Image>();

        cardImage.sprite = cardSpriteManager.GetCardSprite(cardId);

        newCard.gameObject.tag = cardId;

        // Przechowuj kart� w s�owniku
        cardObjects[cardId] = newCard;

      

    }

    private void ListenForCardPlayed(string cardId)
    {
      
        // Nas�uchiwanie na zmiany w warto�ci "played" w Firebase
        dbRef.Child(cardId).Child("played").ValueChanged += (sender, args) =>
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError("Error while listening for card status change: " + args.DatabaseError.Message);
                return;
            }

            bool played = (bool)args.Snapshot.Value;
            if (played)
            {
                
                // Kiedy karta zostanie zagrana, usuwamy j� z UI
                if (cardObjects.ContainsKey(cardId))
                {
                    Destroy(cardObjects[cardId]);  // Usuwamy kart� z UI
                    cardObjects.Remove(cardId);  // Usuwamy kart� ze s�ownika
                    
                }
                else
                {
                    Debug.LogWarning("Card with ID: " + cardId + " not found in the container.");
                }
            }
        };
    }
}