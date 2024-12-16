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
    public GameObject cardPanel;
    public Image panelImage;
    public Button closeButton;
    public CardSpriteManager cardSpriteManager;

    DatabaseReference dbRef;
    string lobbyId;
    string playerId;

    public GameObject sellPanel;
    public Button trashButton;
    public Button yesSellButton;
    public Button noSellButton;
    public Text sellText;

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

        dbRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("deck");

        ListenForNewCards();
        ListenForCardRemoved();
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
            bool onHand = cardSnapshot.Child("onHand").Value as bool? ?? false;
            bool played = cardSnapshot.Child("played").Value as bool? ?? false;

            if (onHand && !played)
            {
                string instanceId = cardSnapshot.Key;
                string cardId = cardSnapshot.Child("cardId").Value as string;

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
        if (cardObjects.ContainsKey(instanceId))
        {
            if (cardObjects[instanceId] == null)
            {
                cardObjects.Remove(instanceId);
            }
            else
            {
                return;
            }
        }

        if (cardPrefab == null || cardListContainer == null)
        {
            return;
        }

        GameObject newCard = Instantiate(cardPrefab, cardListContainer.transform);

        if (newCard == null)
        {
            return;
        }

        Image cardImage = newCard.GetComponent<Image>();
        if (cardImage != null)
        {
            cardImage.sprite = cardSpriteManager.GetCardSprite(cardId);
        }

        DraggableItem draggableItem = newCard.GetComponent<DraggableItem>();
        if (draggableItem != null)
        {
            draggableItem.cardPanel = cardPanel;
            draggableItem.panelImage = panelImage;
            draggableItem.closeButton = closeButton;
            draggableItem.image = newCard.GetComponent<Image>();
            draggableItem.instanceId = instanceId;
            draggableItem.cardId = cardId;

            draggableItem.sellPanel = sellPanel;
            draggableItem.trashButton = trashButton;
            draggableItem.yesSellButton = yesSellButton;
            draggableItem.noSellButton = noSellButton;
            draggableItem.sellText = sellText;
        }

        cardObjects[instanceId] = newCard;
    }

    private void ListenForCardOnHandChange(string instanceId)
    {
        dbRef.Child(instanceId).Child("onHand").ValueChanged += (sender, args) =>
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError($"Error in 'onHand' listener: {args.DatabaseError.Message}");
                return;
            }

            if (!cardObjects.ContainsKey(instanceId) || cardObjects[instanceId] == null)
            {
                return;
            }

            bool onHand = args.Snapshot.Value != null && (bool)args.Snapshot.Value;

            if (onHand)
            {
                if (!cardObjects.ContainsKey(instanceId))
                {
                    string cardId = args.Snapshot.Child("cardId").Value.ToString();
                    AddCardToUI(instanceId, cardId);
                }
            }
            else
            {
                if (cardObjects.ContainsKey(instanceId))
                {
                    GameObject cardToRemove = cardObjects[instanceId];
                    if (cardToRemove != null)
                    {
                        Destroy(cardToRemove);
                    }
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
                Debug.LogError($"Error in 'played' listener: {args.DatabaseError.Message}");
                return;
            }

            if (!cardObjects.ContainsKey(instanceId) || cardObjects[instanceId] == null)
            {
                return;
            }

            bool played = args.Snapshot.Value != null && (bool)args.Snapshot.Value;

            if (played)
            {
                if (cardObjects.ContainsKey(instanceId))
                {
                    GameObject cardToRemove = cardObjects[instanceId];
                    if (cardToRemove != null)
                    {
                        Destroy(cardToRemove);
                    }
                    cardObjects.Remove(instanceId);
                }
            }

            ForceUpdateUI();
        };
    }

    private void ListenForNewCards()
    {
        dbRef.ChildAdded += (sender, args) =>
        {
            if (args.Snapshot == null)
            {
                Debug.LogError("Snapshot is null.");
                return;
            }

            string instanceId = args.Snapshot.Key;
            string cardId = args.Snapshot.Child("cardId").Value as string;
            bool onHand = args.Snapshot.Child("onHand").Value as bool? ?? false;
            bool played = args.Snapshot.Child("played").Value as bool? ?? false;

            if (onHand && !played)
            {
                if (!cardObjects.ContainsKey(instanceId))
                {
                    AddCardToUI(instanceId, cardId);
                    ListenForCardOnHandChange(instanceId);
                    ListenForCardPlayed(instanceId);
                }
            }
        };
    }

    private void ListenForCardRemoved()
    {
        dbRef.ChildRemoved += (sender, args) =>
        {
            string instanceId = args.Snapshot.Key;
            if (cardObjects.ContainsKey(instanceId))
            {
                GameObject cardToRemove = cardObjects[instanceId];
                if (cardToRemove != null)
                {
                    Destroy(cardToRemove);
                }
                cardObjects.Remove(instanceId);
            }
        };

    }

    public void ForceUpdateUI()
    {
        if (this == null)
        {
            return;
        }

        StartCoroutine(LoadCardsOnHand());
    }
}
