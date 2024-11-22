using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Firebase.Database;
using System.Threading.Tasks;

public class CardSelectionUI : MonoBehaviour
{
    public GameObject cardCheckboxPrefab;
    public Transform cardListContainer;
    public Button submitButton;
    public GameObject cardSelectionPanel;
    public Sprite unselectedSprite;
    public Sprite selectedSprite;
    public ScrollRect cardScrollView;

    private List<string> selectedCards = new List<string>();
    private Dictionary<string, bool> cardSelectionStates = new Dictionary<string, bool>();
    private int numberOfCardsToSelect = 0;
    private string playerId;
    private bool selectionConfirmed = false;

    public async Task<List<string>> ShowCardSelection(string playerId, int numberOfCardsToSelect, string playedCardId, bool fromHand)
    {
        this.playerId = playerId;
        this.numberOfCardsToSelect = numberOfCardsToSelect;
        selectedCards.Clear();
        cardSelectionStates.Clear();
        selectionConfirmed = false;
        ClearUI();

        cardSelectionPanel.SetActive(true);
        submitButton.gameObject.SetActive(false);

        string lobbyId = DataTransfer.LobbyId;
        DatabaseReference playerDeckRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("deck");

        var snapshot = await playerDeckRef.GetValueAsync();
        if (!snapshot.Exists) return null;

        foreach (var cardSnapshot in snapshot.Children)
        {
            if (playedCardId == cardSnapshot.Key) continue;

            bool onHand = cardSnapshot.Child("onHand").Value as bool? ?? false;
            bool played = cardSnapshot.Child("played").Value as bool? ?? false;

            if ((fromHand && onHand && !played) || (!fromHand && !onHand && !played))
            {
                string cardId = cardSnapshot.Key;
                string cardType = GetCardType(cardId);

                DatabaseReference cardRef = FirebaseInitializer.DatabaseReference
                    .Child("cards")
                    .Child("id")
                    .Child(cardType)
                    .Child(cardId)
                    .Child("name");

                var cardNameSnapshot = await cardRef.GetValueAsync();
                if (cardNameSnapshot.Exists)
                {
                    string cardName = cardNameSnapshot.Value.ToString();
                    AddCardToUI(cardId, cardName);
                }
            }
        }

        submitButton.onClick.RemoveAllListeners();
        submitButton.onClick.AddListener(() => ConfirmSelection());

        while (!selectionConfirmed)
        {
            await Task.Yield();
        }

        HideCardSelectionPanel();
        return new List<string>(selectedCards);
    }

    private string GetCardType(string cardId)
    {
        string cardLetters = cardId.Substring(0, 2);
        switch (cardLetters)
        {
            case "AD": return "addRemove";
            case "AS": return "asMuchAs";
            case "CA": return "cards";
            case "OP": return "options";
            case "RA": return "random";
            case "UN": return "unique";
            default: return "unknown";
        }
    }

    private void AddCardToUI(string cardId, string cardName)
    {
        GameObject newButton = Instantiate(cardCheckboxPrefab, cardListContainer);

        Button button = newButton.GetComponent<Button>();
        Image cardImage = newButton.GetComponent<Image>();
        Text nameText = newButton.GetComponentInChildren<Text>();

        nameText.text = cardName;
        cardImage.sprite = unselectedSprite;
        cardSelectionStates[cardId] = false;

        button.onClick.AddListener(() => ToggleCardState(cardId, cardImage));
    }

    private void ToggleCardState(string cardId, Image cardImage)
    {
        bool isSelected = cardSelectionStates[cardId];
        cardSelectionStates[cardId] = !isSelected;

        if (cardSelectionStates[cardId])
        {
            if (selectedCards.Count < numberOfCardsToSelect)
            {
                selectedCards.Add(cardId);
                cardImage.sprite = selectedSprite;
            }
            else
            {
                cardSelectionStates[cardId] = false;
            }
        }
        else
        {
            selectedCards.Remove(cardId);
            cardImage.sprite = unselectedSprite;
        }

        // Poka� przycisk Submit tylko, gdy wybrano wystarczaj�c� liczb� kart
        submitButton.gameObject.SetActive(selectedCards.Count == numberOfCardsToSelect);
    }

    private void ConfirmSelection()
    {
        if (selectedCards.Count == numberOfCardsToSelect)
        {
            selectionConfirmed = true;
        }
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
