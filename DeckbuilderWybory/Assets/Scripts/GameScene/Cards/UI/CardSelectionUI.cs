using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Firebase.Database;
using System.Threading.Tasks;
using System.Linq;

public class CardSelectionUI : MonoBehaviour
{
    public GameObject cardCheckboxPrefab;
    public Transform cardListContainer;
    public Button submitButton;
    public GameObject cardSelectionPanel;
    public Sprite unselectedSprite;
    public Sprite selectedSprite;
    public ScrollRect cardScrollView;

    private List<KeyValuePair<string, string>> selectedCards = new();
    private Dictionary<string, bool> cardSelectionStates = new();
    private int numberOfCardsToSelect = 0;
    private string playerId;
    private bool selectionConfirmed = false;

    public async Task<List<KeyValuePair<string, string>>> ShowCardSelection(string playerId,int numberOfCardsToSelect,string playedCardId, bool fromHand,List<KeyValuePair<string, string>> excludedCards = null)
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
                    AddCardToUI(instanceId, cardName, cardId);
                    anyCardAdded = true;
                }
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



    private string GetCardType(string cardId)
    {
        string cardLetters = cardId[..2];
        return cardLetters switch
        {
            "AD" => "addRemove",
            "AS" => "asMuchAs",
            "CA" => "cards",
            "OP" => "options",
            "RA" => "random",
            "UN" => "unique",
            _ => "unknown",
        };
    }

    private void AddCardToUI(string instanceId, string cardName, string cardId)
    {
        GameObject newButton = Instantiate(cardCheckboxPrefab, cardListContainer);

        Button button = newButton.GetComponent<Button>();
        Image cardImage = newButton.GetComponent<Image>();
        Text nameText = newButton.GetComponentInChildren<Text>();

        nameText.text = cardName;
        cardImage.sprite = unselectedSprite;
        cardSelectionStates[instanceId] = false;

        button.onClick.AddListener(() => ToggleCardState(instanceId, cardImage, cardId));
    }

    private void ToggleCardState(string instanceId, Image cardImage, string cardId)
    {
        bool isSelected = cardSelectionStates[instanceId];
        cardSelectionStates[instanceId] = !isSelected;

        if (cardSelectionStates[instanceId])
        {
            if (selectedCards.Count < numberOfCardsToSelect)
            {
                selectedCards.Add(new KeyValuePair<string, string>(instanceId, cardId));
                cardImage.sprite = selectedSprite;
            }
            else
            {
                cardSelectionStates[instanceId] = false;
            }
        }
        else
        {
            selectedCards.Remove(new KeyValuePair<string, string>(instanceId, cardId));
            cardImage.sprite = unselectedSprite;
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
