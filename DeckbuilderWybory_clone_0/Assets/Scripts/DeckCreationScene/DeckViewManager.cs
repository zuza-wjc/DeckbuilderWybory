using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Firebase.Database;
using System.Threading.Tasks;
using System.Linq;
using System;

public class DeckViewManager : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform cardListContainer;
    public Button submitButton;
    public GameObject cardSelectionPanel;
    public ScrollRect cardScrollView;

    public CardSpriteManager cardSpriteManager;

    private async Task<bool> LoadCardsFromDatabase(string deckType, Action<string> onCardFound)
    {
        DatabaseReference dbRef = FirebaseInitializer.DatabaseReference
            .Child("readyDecks")
            .Child(deckType);

        var snapshot = await dbRef.GetValueAsync();
        if (!snapshot.Exists)
        {
            Debug.LogWarning($"No cards found for deck type: {deckType}");
            return false;
        }

        bool anyCardAdded = false;

        foreach (var cardSnapshot in snapshot.Children)
        {
            string cardId = cardSnapshot.Value?.ToString();
            if (!string.IsNullOrEmpty(cardId))
            {
                onCardFound(cardId);
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

        Button button = newCardUI.GetComponent<Button>();
        button.interactable = false;
    }

    private void AddCardToUIForViewing(string instanceId, string cardId)
    {
        AddCardToUIBase(instanceId, cardId, true);
    }

    private void ClosePanel()
    {
        HideAndClearUI();
    }

    private void HideAndClearUI()
    {
        cardSelectionPanel.SetActive(false);
        submitButton.gameObject.SetActive(false);
        ClearUI();
    }

    private void HideCardSelectionPanel()
    {
        cardSelectionPanel.SetActive(false);
        submitButton.gameObject.SetActive(false);
    }

    private void ClearUI()
    {
        foreach (Transform child in cardListContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public async Task ShowDeckCardsForViewing(string deckType)
    {
        ClearUI();

        cardSelectionPanel.SetActive(true);
        submitButton.gameObject.SetActive(true);

        submitButton.onClick.RemoveAllListeners();
        submitButton.onClick.AddListener(ClosePanel);

        bool anyCardAdded = await LoadCardsFromDatabase(deckType, (cardId) =>
        {
            AddCardToUIForViewing(cardId, cardId);
        });

        if (!anyCardAdded)
        {
            Debug.LogWarning($"No cards available for deck type: {deckType}");
            HideCardSelectionPanel();
        }
    }
}
