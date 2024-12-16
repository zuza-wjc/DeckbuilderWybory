using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Firebase.Database;
using System.Threading.Tasks;
using System.Linq;
using System;

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

    private async Task<bool> LoadCardsFromDatabase(string playerId, bool onHandFilter, bool playedFilter, Action<string, string> onCardFound)
    {
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
            return false;
        }

        bool anyCardAdded = false;

        foreach (var cardSnapshot in snapshot.Children)
        {
            string instanceId = cardSnapshot.Key;
            bool onHand = cardSnapshot.Child("onHand").Value as bool? ?? false;
            bool played = cardSnapshot.Child("played").Value as bool? ?? false;

            if ((onHand == onHandFilter) && (played == playedFilter))
            {
                string cardId = (string)cardSnapshot.Child("cardId").Value;
                onCardFound(instanceId, cardId);
                anyCardAdded = true;
            }
        }

        return anyCardAdded;
    }

    private void AddCardToUIBase(string instanceId, string cardId, bool isForViewing)
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
        if (isForViewing)
        {
            button.interactable = false;
        }
        else
        {
            button.onClick.AddListener(() => ToggleCardState(instanceId, cardId, border));
        }
    }

    private void AddCardToUI(string instanceId, string cardId)
    {
        AddCardToUIBase(instanceId, cardId, false);
    }

    private void AddCardToUIForViewing(string instanceId, string cardId)
    {
        AddCardToUIBase(instanceId, cardId, true);
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

    public void ClosePanel()
    {
        HideAndClearUI();
    }

    private void HideAndClearUI()
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

    public async Task<List<KeyValuePair<string, string>>> ShowCardSelection(string playerId, int numberOfCardsToSelect, string playedCardId, bool fromHand, List<KeyValuePair<string, string>> excludedCards = null)
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

        bool anyCardAdded = await LoadCardsFromDatabase(playerId, fromHand, false, (instanceId, cardId) =>
        {
            if (excludedCards != null && excludedCards.Any(excluded => excluded.Key == instanceId)) return;
            if (instanceId == playedCardId) return;

            AddCardToUI(instanceId, cardId);
        });

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

        bool anyCardAdded = await LoadCardsFromDatabase(playerId, true, false, (instanceId, cardId) =>
        {
            AddCardToUIForViewing(instanceId, cardId);
        });

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

        submitButton.onClick.RemoveAllListeners();

        bool anyCardAdded = await LoadCardsFromDatabase(playerId, false, false, (instanceId, cardId) =>
        {
            AddCardToUIForViewing(instanceId, cardId);
        });

        if (!anyCardAdded)
        {
            infoText.text = "BRAK KART W TALII";
            return;
        }

        infoText.text = "KARTY GRACZA";
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
        if (playerCardSnapshot.Exists)
        {
            string cardId = (string)playerCardSnapshot.Child("cardId").Value;
            AddCardToUI(playerCard, cardId);
        }

        DatabaseReference enemyCardRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId)
            .Child("deck")
            .Child(enemyCard);

        var enemyCardSnapshot = await enemyCardRef.GetValueAsync();
        if (enemyCardSnapshot.Exists)
        {
            string cardId = (string)enemyCardSnapshot.Child("cardId").Value;
            AddCardToUI(enemyCard, cardId);
        }

        submitButton.onClick.RemoveAllListeners();
        submitButton.onClick.AddListener(() => ConfirmSelection());

        while (!selectionConfirmed)
        {
            await Task.Yield();
        }

        HideCardSelectionPanel();
        return new(selectedCards[0].Key, selectedCards[1].Key);
    }

    private void HideCardSelectionPanel()
    {
        cardSelectionPanel.SetActive(false);
        submitButton.gameObject.SetActive(false);
        infoText.text = "";
        cardSelectionStates.Clear();
    }

}
