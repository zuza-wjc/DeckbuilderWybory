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
                string instanceId = cardSnapshot.Key;
                string cardId = (string)cardSnapshot.Child("cardId").Value;

                if (!cardObjects.ContainsKey(instanceId))
                {
                    AddCardToUI(instanceId, cardId);
                    ListenForCardOnHandChange(instanceId);
                    ListenForCardPlayed(instanceId);
                }
            }
        }
    }

    private void AddCardToUI(string instanceId, string cardId)
    {
        if (cardObjects.ContainsKey(instanceId)) return;

        GameObject newCard = Instantiate(cardPrefab, cardListContainer.transform);
        Image cardImage = newCard.GetComponent<Image>();

        cardImage.sprite = cardSpriteManager.GetCardSprite(cardId);

        DraggableItem draggableItem = newCard.GetComponent<DraggableItem>();
        if (draggableItem != null)
        {
            draggableItem.instanceId = instanceId;
            draggableItem.cardId = cardId;
        }

        cardObjects[instanceId] = newCard;
    }

    private void ListenForCardOnHandChange(string instanceId)
    {
        dbRef.Child(instanceId).Child("onHand").ValueChanged += (sender, args) =>
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError("Error while listening for card onHand status change: " + args.DatabaseError.Message);
                return;
            }

            bool onHand = (bool)args.Snapshot.Value;

            if (onHand)
            {
                if (!cardObjects.ContainsKey(instanceId))
                {
                    string cardId = (string)args.Snapshot.Child("cardId").Value;
                    AddCardToUI(instanceId, cardId);
                }
            }
            else
            {
                if (cardObjects.ContainsKey(instanceId))
                {
                    Destroy(cardObjects[instanceId]);
                    cardObjects.Remove(instanceId);
                }
            }

            ForceUpdateUI();
        };
    }

    private void ListenForCardPlayed(string instanceId)
    {
        dbRef.Child(instanceId).Child("played").ValueChanged += (sender, args) =>
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError("Error while listening for card played status change: " + args.DatabaseError.Message);
                return;
            }

            bool played = (bool)args.Snapshot.Value;

            if (played)
            {
                if (cardObjects.ContainsKey(instanceId))
                {
                    Destroy(cardObjects[instanceId]);
                    cardObjects.Remove(instanceId);
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
