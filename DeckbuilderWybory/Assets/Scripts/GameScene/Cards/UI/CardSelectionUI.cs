using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Firebase.Database;
using System.Threading.Tasks;
using System.Linq;

public class CardSelectionUI : MonoBehaviour
{
    public GameObject cardPrefab;  // Prefab karty
    public Transform cardListContainer;  // Kontener na karty w UI
    public Button submitButton;  // Przycisk zatwierdzaj¹cy wybór kart
    public GameObject cardSelectionPanel;  // Panel wyboru kart
    public ScrollRect cardScrollView;  // Scroll do przewijania kart

    public CardSpriteManager cardSpriteManager;  // Odwo³anie do CardSpriteManager

    private List<KeyValuePair<string, string>> selectedCards = new();
    private Dictionary<string, bool> cardSelectionStates = new();
    private int numberOfCardsToSelect = 0;
    private string playerId;
    private bool selectionConfirmed = false;

    // Metoda do pokazania ekranu wyboru kart
    public async Task<List<KeyValuePair<string, string>>> ShowCardSelection(
        string playerId, int numberOfCardsToSelect, string playedCardId,
        bool fromHand, List<KeyValuePair<string, string>> excludedCards = null)
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
                    AddCardToUI(instanceId, cardId);
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

    // Funkcja do rozpoznania typu karty na podstawie ID
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

    // Dodawanie karty do UI
    private void AddCardToUI(string instanceId, string cardId)
    {
        GameObject newCardUI = Instantiate(cardPrefab, cardListContainer);

        // Pobierz komponenty
        Image cardImage = newCardUI.transform.Find("cardImage").GetComponent<Image>();
        Transform borderTransform = newCardUI.transform.Find("Border");

        if (borderTransform == null)
        {
            Debug.LogError("Border object not found in card prefab.");
            return;
        }

        GameObject border = borderTransform.gameObject;
        border.SetActive(false); // Ukryj border na pocz¹tku

        // Pobierz sprite karty
        Sprite cardSprite = cardSpriteManager?.GetCardSprite(cardId);
        if (cardSprite == null)
        {
            Debug.LogError($"No sprite found for cardId: {cardId}");
            return;
        }

        cardImage.sprite = cardSprite;

        // Zapisz stan karty
        cardSelectionStates[instanceId] = false;

        // Dodaj listener na klikniêcie
        Button button = newCardUI.GetComponent<Button>();
        button.onClick.AddListener(() => ToggleCardState(instanceId, cardId, border));
    }

    // Zmiana stanu zaznaczenia karty
    private void ToggleCardState(string instanceId, string cardId, GameObject border)
    {
        bool isSelected = cardSelectionStates[instanceId];
        cardSelectionStates[instanceId] = !isSelected;

        if (cardSelectionStates[instanceId])
        {
            if (selectedCards.Count < numberOfCardsToSelect)
            {
                selectedCards.Add(new KeyValuePair<string, string>(instanceId, cardId));
                border.SetActive(true); // Poka¿ obramowanie, gdy karta jest zaznaczona
            }
            else
            {
                cardSelectionStates[instanceId] = false;
            }
        }
        else
        {
            selectedCards.Remove(new KeyValuePair<string, string>(instanceId, instanceId));
            border.SetActive(false); // Ukryj obramowanie, gdy karta jest odznaczona
        }

        submitButton.gameObject.SetActive(selectedCards.Count == numberOfCardsToSelect);
    }

    // Potwierdzenie wyboru kart
    private void ConfirmSelection()
    {
        if (selectedCards.Count == numberOfCardsToSelect)
        {
            selectionConfirmed = true;
        }
    }

    // Ukrywanie panelu wyboru kart
    private void HideCardSelectionPanel()
    {
        cardSelectionPanel.SetActive(false);
        submitButton.gameObject.SetActive(false);
        ClearUI();
    }

    // Czyszczenie UI z wczeœniej za³adowanych kart
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
