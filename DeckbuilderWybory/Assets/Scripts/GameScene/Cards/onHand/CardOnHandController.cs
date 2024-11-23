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

    private Dictionary<string, GameObject> cardObjects = new();

    private void Awake()
    {
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

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

            if (onHand && !played)
            {
                string cardId = cardSnapshot.Key;
                if (!cardObjects.ContainsKey(cardId)) 
                {
                    AddCardToUI(cardId);
                    ListenForCardOnHandChange(cardId);
                    ListenForCardPlayed(cardId);
                }
            }
        }
    }

    private void AddCardToUI(string cardId)
    {
        if (cardObjects.ContainsKey(cardId)) return;

        GameObject newCard = Instantiate(cardPrefab, cardListContainer.transform);
        Image cardImage = newCard.GetComponent<Image>();

        cardImage.sprite = cardSpriteManager.GetCardSprite(cardId);
        newCard.tag = cardId;

        cardObjects[cardId] = newCard;

    }

    private void ListenForCardOnHandChange(string cardId)
    {
        dbRef.Child(cardId).Child("onHand").ValueChanged += (sender, args) =>
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError("Error while listening for card onHand status change: " + args.DatabaseError.Message);
                return;
            }

            bool onHand = (bool)args.Snapshot.Value;

            if (onHand)
            {
                if (!cardObjects.ContainsKey(cardId))
                {
                    AddCardToUI(cardId);
                }
            }
            else
            {
                if (cardObjects.ContainsKey(cardId))
                {
                    Destroy(cardObjects[cardId]);
                    cardObjects.Remove(cardId);
                }
            }

            ForceUpdateUI();
        };
    }

    private void ListenForCardPlayed(string cardId)
    {
        dbRef.Child(cardId).Child("played").ValueChanged += (sender, args) =>
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError("Error while listening for card played status change: " + args.DatabaseError.Message);
                return;
            }

            bool played = (bool)args.Snapshot.Value;

            if (played)
            {
                if (cardObjects.ContainsKey(cardId))
                {
                    Destroy(cardObjects[cardId]);
                    cardObjects.Remove(cardId);
                }
            }
        };
    }

    private void ForceUpdateUI()
    {
        if (this == null)
        {
            return;
        }

        StartCoroutine(LoadCardsOnHand());
    }



}