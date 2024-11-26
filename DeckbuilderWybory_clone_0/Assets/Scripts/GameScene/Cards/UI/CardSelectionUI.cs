using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Firebase.Database;
using System.Threading.Tasks;
using System.Linq;

public class CardSelectionUI : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform cardListContainer;
    public Button submitButton;
    public GameObject cardSelectionPanel;
    public ScrollRect cardScrollView;
    public Text infoText;

    public CardSpriteManager cardSpriteManager;

    private List<KeyValuePair<string, string>> selectedCards = new();
    private Dictionary<string, bool> cardSelectionStates = new();
    private int numberOfCardsToSelect = 0;
    private string playerId;
    private bool selectionConfirmed = false;

    public async Task<List<KeyValuePair<string, string>>> ShowCardSelection(string playerId, int numberOfCardsToSelect, string playedCardId,bool fromHand,
        List<KeyValuePair<string, string>> excludedCards = null)
    {
        this.playerId = playerId;
        this.numberOfCardsToSelect = numberOfCardsToSelect;
        selectedCards.Clear();
        cardSelectionStates.Clear();
        selectionConfirmed = false;
        ClearUI();

        cardSelectionPanel.SetActive(true);
        submitButton.gameObject.SetActive(false);

        infoText.text = "WYBIERZ KARTY";

        string lobbyId = DataTransfer.LobbyId;
        DatabaseReference playerDeckRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("deck");

        var snapshot = await playerDeckRef.GetValueAsync();
        if (!snapshot.Exists)
        {
            Debug.LogWarning("No deck data found for the player.");
            return null;
        }

        bool anyCardAdded = false;

        foreach (var cardSnapshot in snapshot.Children)
        {
            string instanceId = cardSnapshot.Key;

            if (excludedCards != null && excludedCards.Any(excluded => excluded.Key == instanceId)) continue;

            if (instanceId == playedCardId) continue;

            bool onHand = cardSnapshot.Child("onHand").Value as bool? ?? false;
            bool played = cardSnapshot.Child("played").Value as bool? ?? false;

            if ((fromHand && onHand && !played) || (!fromHand && !onHand && !played))
            {
                string cardId = (string)cardSnapshot.Child("cardId").Value;
                AddCardToUI(instanceId, cardId);
                anyCardAdded = true;
            }
        }

        if (!anyCardAdded)
        {
            Debug.LogWarning("No cards available to select.");
            HideCardSelectionPanel();
            return null;
        }

        submitButton.onClick.RemoveAllListeners();
        submitButton.onClick.AddListener(() => ConfirmSelection());

        while (!selectionConfirmed)
        {
            await Task.Yield();
        }

        HideCardSelectionPanel();
        return new List<KeyValuePair<string, string>>(selectedCards);
    }

    public async Task ShowCardsForViewing(string playerId)
    {
        this.playerId = playerId;
        selectedCards.Clear();
        cardSelectionStates.Clear();
        selectionConfirmed = false;
        ClearUI();

        cardSelectionPanel.SetActive(true);
        submitButton.gameObject.SetActive(true);

        infoText.text = "KARTY GRACZA";

        submitButton.onClick.RemoveAllListeners();
        submitButton.onClick.AddListener(ClosePanel);

        string lobbyId = DataTransfer.LobbyId;
        DatabaseReference playerDeckRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("deck");

        var snapshot = await playerDeckRef.GetValueAsync();
        if (!snapshot.Exists)
        {
            Debug.LogWarning("No deck data found for the player.");
            return;
        }

        bool anyCardAdded = false;

        foreach (var cardSnapshot in snapshot.Children)
        {
            string instanceId = cardSnapshot.Key;

            bool onHand = cardSnapshot.Child("onHand").Value as bool? ?? false;
            bool played = cardSnapshot.Child("played").Value as bool? ?? false;

            if (onHand && !played)
            {
                string cardId = (string)cardSnapshot.Child("cardId").Value;
                AddCardToUIForViewing(instanceId, cardId);
                anyCardAdded = true;
            }
        }

        if (!anyCardAdded)
        {
            Debug.LogWarning("No cards available to view.");
            HideCardSelectionPanel();
            return;
        }
    }

    public async Task ShowDeckCardsForViewing(string playerId)
    {
        this.playerId = playerId;
        selectedCards.Clear();
        cardSelectionStates.Clear();
        selectionConfirmed = false;
        ClearUI();

        cardSelectionPanel.SetActive(true);
        submitButton.gameObject.SetActive(true);

        infoText.text = "KARTY GRACZA";

        submitButton.onClick.RemoveAllListeners();
        submitButton.onClick.AddListener(ClosePanel);

        string lobbyId = DataTransfer.LobbyId;
        DatabaseReference playerDeckRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("deck");

        var snapshot = await playerDeckRef.GetValueAsync();
        if (!snapshot.Exists)
        {
            Debug.LogWarning("No deck data found for the player.");
            return;
        }

        bool anyCardAdded = false;

        foreach (var cardSnapshot in snapshot.Children)
        {
            string instanceId = cardSnapshot.Key;

            bool onHand = cardSnapshot.Child("onHand").Value as bool? ?? false;
            bool played = cardSnapshot.Child("played").Value as bool? ?? false;

            if (!onHand && !played)
            {
                string cardId = (string)cardSnapshot.Child("cardId").Value;
                AddCardToUIForViewing(instanceId, cardId);
                anyCardAdded = true;
            }
        }

        if (!anyCardAdded)
        {
            Debug.LogWarning("No cards available to view.");
            HideCardSelectionPanel();
            return;
        }
    }

    public async Task<(string keepCard, string destroyCard)> ShowCardSelectionForPlayerAndEnemy(string playerId, string playerCard, string enemyId, string enemyCard)
    {
        this.numberOfCardsToSelect = 1;

        selectedCards.Clear();
        cardSelectionStates.Clear();
        selectionConfirmed = false;
        ClearUI();

        cardSelectionPanel.SetActive(true);
        submitButton.gameObject.SetActive(false);

        infoText.text = "WYBIERZ KARTÊ DO ZACHOWANIA";

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference playerCardRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("deck")
            .Child(playerCard);

        var playerCardSnapshot = await playerCardRef.GetValueAsync();
        if (!playerCardSnapshot.Exists)
        {
            Debug.LogWarning("Player card not found.");
            return (null, null);
        }
        string playerCardId = playerCardSnapshot.Child("cardId").Value?.ToString();

        DatabaseReference enemyCardRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId)
            .Child("deck")
            .Child(enemyCard);

        var enemyCardSnapshot = await enemyCardRef.GetValueAsync();
        if (!enemyCardSnapshot.Exists)
        {
            Debug.LogWarning("Enemy card not found.");
            return (null, null);
        }
        string enemyCardId = enemyCardSnapshot.Child("cardId").Value?.ToString();

        AddCardToUI(playerCard, playerCardId);
        AddCardToUI(enemyCard, enemyCardId);

        submitButton.onClick.RemoveAllListeners();
        submitButton.onClick.AddListener(() => ConfirmSelection());

        while (!selectionConfirmed)
        {
            await Task.Yield();
        }

        HideCardSelectionPanel();

        string keepCard = selectedCards.Count > 0 ? selectedCards[0].Key : null;
        string destroyCard = keepCard == null ? null : keepCard == playerCard ? enemyCard : playerCard;

        return (keepCard, destroyCard);
    }

    private void AddCardToUI(string instanceId, string cardId)
    {
        GameObject newCardUI = Instantiate(cardPrefab, cardListContainer);

        Image cardImage = newCardUI.transform.Find("cardImage").GetComponent<Image>();
        Transform borderTransform = newCardUI.transform.Find("Border");

        if (borderTransform == null)
        {
            Debug.LogError("Border object not found in card prefab.");
            return;
        }

        GameObject border = borderTransform.gameObject;
        border.SetActive(false);

        Sprite cardSprite = cardSpriteManager?.GetCardSprite(cardId);
        if (cardSprite == null)
        {
            Debug.LogError($"No sprite found for cardId: {cardId}");
            return;
        }

        cardImage.sprite = cardSprite;
        cardSelectionStates[instanceId] = false;

        Button button = newCardUI.GetComponent<Button>();
        button.onClick.AddListener(() => ToggleCardState(instanceId, cardId, border));
    }

    private void AddCardToUIForViewing(string instanceId, string cardId)
    {
        GameObject newCardUI = Instantiate(cardPrefab, cardListContainer);

        Image cardImage = newCardUI.transform.Find("cardImage").GetComponent<Image>();
        Transform borderTransform = newCardUI.transform.Find("Border");

        if (borderTransform == null)
        {
            Debug.LogError("Border object not found in card prefab.");
            return;
        }

        GameObject border = borderTransform.gameObject;
        border.SetActive(false);

        Sprite cardSprite = cardSpriteManager?.GetCardSprite(cardId);
        if (cardSprite == null)
        {
            Debug.LogError($"No sprite found for cardId: {cardId}");
            return;
        }

        cardImage.sprite = cardSprite;
        cardSelectionStates[instanceId] = false;

        Button button = newCardUI.GetComponent<Button>();
        button.interactable = false;
    }

    private void ToggleCardState(string instanceId, string cardId, GameObject border)
    {
        bool isSelected = cardSelectionStates[instanceId];
        cardSelectionStates[instanceId] = !isSelected;

        if (cardSelectionStates[instanceId])
        {
            if (selectedCards.Count < numberOfCardsToSelect)
            {
                selectedCards.Add(new KeyValuePair<string, string>(instanceId, cardId));
                border.SetActive(true);
            }
            else
            {
                cardSelectionStates[instanceId] = false;
            }
        }
        else
        {
            selectedCards.Remove(new KeyValuePair<string, string>(instanceId, cardId));
            border.SetActive(false);
        }

        submitButton.gameObject.SetActive(selectedCards.Count == numberOfCardsToSelect);
    }

    private void ConfirmSelection()
    {
        if (selectedCards.Count == numberOfCardsToSelect)
        {
            selectionConfirmed = true;
        }
    }

    private void ClosePanel()
    {
        HideCardSelectionPanel();
    }

    private void HideCardSelectionPanel()
    {
        cardSelectionPanel.SetActive(false);
        submitButton.gameObject.SetActive(false);
        ClearUI();
    }

    private void ClearUI()
    {
        foreach (Transform child in cardListContainer)
        {
            Destroy(child.gameObject);
        }
    }

    void OnDestroy()
    {
        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
        }
    }
}
