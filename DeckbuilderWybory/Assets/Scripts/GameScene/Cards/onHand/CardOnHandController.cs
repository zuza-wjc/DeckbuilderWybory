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

            if (onHand && !played)  // Tylko karty, które s¹ na rêce i nie zosta³y zagrane
            {
       
                string cardId = cardSnapshot.Key;
                AddCardToUI(cardId);
                ListenForCardPlayed(cardId); // Nas³uchiwanie na zmiany "played"
            }
        }
        Debug.Log("Finished processing cards.");
    }

    private void AddCardToUI(string cardId)
    {
  
        GameObject newCard = Instantiate(cardPrefab, cardListContainer.transform);
        Image cardImage = newCard.GetComponent<Image>();

        cardImage.sprite = cardSpriteManager.GetCardSprite(cardId);

        // Przechowuj kartê w s³owniku
        cardObjects[cardId] = newCard;

        Debug.Log("Added card with ID: " + cardId);

    }

    private void ListenForCardPlayed(string cardId)
    {
        Debug.Log("ListenForCardPlayed called for cardId: " + cardId);
        // Nas³uchiwanie na zmiany w wartoœci "played" w Firebase
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
                Debug.Log("Card with ID: " + cardId + " has been played, removing it from UI.");
                // Kiedy karta zostanie zagrana, usuwamy j¹ z UI
                if (cardObjects.ContainsKey(cardId))
                {
                    Destroy(cardObjects[cardId]);  // Usuwamy kartê z UI
                    cardObjects.Remove(cardId);  // Usuwamy kartê ze s³ownika
                    Debug.Log("Card with ID: " + cardId + " successfully removed.");
                }
                else
                {
                    Debug.LogWarning("Card with ID: " + cardId + " not found in the container.");
                }
            }
        };
    }
}