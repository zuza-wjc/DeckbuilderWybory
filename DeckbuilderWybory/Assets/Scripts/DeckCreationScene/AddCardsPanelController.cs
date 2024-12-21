using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Linq;
using System;

public class AddCardsPanelController : MonoBehaviour
{
    public Text lobbySizeText;
    public Button plusButton;
    public Button minusButton;
    public Button acceptButton;
    public Button saveDeckButton;
    public Button okButton;

    private Image addButtonImage;
    private Image minusButtonImage;
    public Sprite addButtonActiveSprite;
    public Sprite minusButtonActiveSprite;
    public Sprite minusButtonInactiveSprite;
    public Sprite addButtonInactiveSprite;

    public CardButtonController cardButtonController;
    public GameObject addCardPanel;
    public GameObject tooMuchCardsPanel;
    public GameObject cardIconPrefab;

    public Transform panelParent;

    public Text deckNameText;
    public Text deckQuantityText;

    int cardsCount = 0;
    int maxDeckNumber = 0;
    string cardId;
    string type;
    string cardName;

    int listCardsCount = 0;

    [System.Serializable]
    public class CardData
    {
        public int cardsCount;
        public string cardId;
        public string type;
        public string cardName;

        public CardData(int cardsCount, string cardId, string type, string cardName)
        {
            this.cardsCount = cardsCount;
            this.cardId = cardId;
            this.type = type;
            this.cardName = cardName;
        }
    }

    public List<CardData> cardList = new List<CardData>();

    public Text Deck1Label;

    void Start()
    {
        StartCoroutine(InitializeDeckName());
    }

    private IEnumerator InitializeDeckName()
    {
        // Wait for the next frame to make sure everything is initialized
        yield return null;

        if (DataManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(DataManager.Instance.deckName))
            {
                deckNameText.text = DataManager.Instance.deckName;
                Debug.Log("Za�adowano deckName: " + DataManager.Instance.deckName);
            }
            else
            {
                Debug.LogWarning("DataManager.Instance.deckName is null or empty.");
            }
        }
        else
        {
            Debug.LogWarning("DataManager.Instance is null. Ensure the DataManager object exists and is initialized.");
        }

        LoadDeckList();
    }
    public void LoadDeckList()
    {
        // Sprawd�, czy DeckNameText jest ustawione i ma warto��
        if (deckNameText != null && !string.IsNullOrEmpty(deckNameText.text))
        {
            string deckName = deckNameText.text;
            string json = PlayerPrefs.GetString(deckName, ""); // Pobierz zapisany JSON z PlayerPrefs

            if (!string.IsNullOrEmpty(json))
            {
                // Je�li JSON istnieje, zdeserializuj go do listy kart
                CardListWrapper cardListWrapper = JsonUtility.FromJson<CardListWrapper>(json);
                if (cardListWrapper != null)
                {
                    cardList = cardListWrapper.cards; // Przypisz list� kart z JSON
                    Debug.Log("Deck loaded from PlayerPrefs.");
                    // Zresetuj listCardsCount przed przetwarzaniem kart
                    listCardsCount = 0;
                    // Utw�rz obiekty dla ka�dej karty w cardList
                    foreach (var card in cardList)
                    {
                        listCardsCount += card.cardsCount; // Zwi�ksz listCardsCount o ilo�� kart
                        CreateCardIcon(card);
                    }
                    // Zmie� tekst `deckNameText`, aby wy�wietla� np. nazw� talii i liczb� kart
                    deckQuantityText.text = $"{listCardsCount}/30";
                }
                else
                {
                    Debug.LogWarning("Failed to deserialize deck from PlayerPrefs.");
                }
            }
            else
            {
                Debug.LogWarning("No deck found in PlayerPrefs for the given name.");
            }
        }
        else
        {
            Debug.LogWarning("Deck name is not set or is empty.");
        }
    }
    private void CreateCardIcon(CardData card)
    {
        if (cardIconPrefab != null && panelParent != null)
        {
            // Tworzenie nowej ikony karty
            GameObject newCardIcon = Instantiate(cardIconPrefab, panelParent);

            // Ustawianie tekstu karty
            Text cardText = newCardIcon.GetComponentInChildren<Text>();
            Text cardQuantityText = newCardIcon.transform.Find("CardQuantityText").GetComponent<Text>();

            if (cardText != null)
            {
                cardText.text = card.cardName; // Ustaw nazw� karty
            }
            else
            {
                Debug.LogWarning("Prefab does not have a Text component!");
            }

            if (cardQuantityText != null)
            {
                cardQuantityText.text = card.cardsCount.ToString(); // Ustaw ilo�� karty
            }
            else
            {
                Debug.LogWarning("Prefab does not have a CardQuantityText component!");
            }

            Debug.Log($"Card {card.cardName} added to the deck with quantity {card.cardsCount}.");
        }
        else
        {
            Debug.LogError("CardIcon prefab or panelParent is not assigned!");
        }
    }

    public void SaveDeck()
    {
        foreach (var card in cardList)
        {
            Debug.Log($"Karta: {card.cardName}, ID: {card.cardId}, Typ: {card.type}, Ilo��: {card.cardsCount}");
        }
        string json = JsonUtility.ToJson(new CardListWrapper { cards = cardList });

        // Zapisanie do PlayerPrefs, u�ywaj�c nazwy z DeckNameText
        if (deckNameText != null && !string.IsNullOrEmpty(deckNameText.text))
        {
            PlayerPrefs.SetString(deckNameText.text, json); // Zapisz z dynamiczn� nazw�
            PlayerPrefs.Save(); // Upewnij si�, �e zmiany zostan� zapisane
            //PlayerPrefsKeysManager.AddKey(deckNameText.text);

            Debug.Log($"Deck saved to PlayerPrefs with name {deckNameText.text}.");
        }
        else
        {
            Debug.LogWarning("Deck name is not set or is empty.");
        }
    }
    [System.Serializable]
    public class CardListWrapper
    {
        public List<CardData> cards;
    }

    public void ShowPanel(string cardId, string type, int maxDeckNumber, string cardName)
    {
        this.maxDeckNumber = maxDeckNumber;
        this.cardId = cardId;
        this.type = type;
        this.cardName = cardName;
        addCardPanel.SetActive(true);

        addButtonImage = plusButton.GetComponentInChildren<Image>();
        minusButtonImage = minusButton.GetComponentInChildren<Image>();

        // Usu� istniej�ce listenery przed dodaniem nowych
        plusButton.onClick.RemoveAllListeners();
        minusButton.onClick.RemoveAllListeners();
        acceptButton.onClick.RemoveAllListeners();

        plusButton.onClick.AddListener(IncreaseLobbySize);
        minusButton.onClick.AddListener(DecreaseLobbySize);
        acceptButton.onClick.AddListener(AcceptCard);

        UpdateLobbySizeText();

        // Znajd� CardButtonController w scenie
        cardButtonController = FindObjectOfType<CardButtonController>();
        Debug.Log($"Dane karty: ID={cardId}, Typ={type}, MaxDeck={maxDeckNumber}");
    }

    public void IncreaseLobbySize()
    {
        if (cardsCount < maxDeckNumber)
        {
            cardsCount++;
            UpdateLobbySizeText();
        }
    }

    public void DecreaseLobbySize()
    {
        if (cardsCount > 0)
        {
            cardsCount--;
            UpdateLobbySizeText();
        }
    }

    void UpdateLobbySizeText()
    {
        lobbySizeText.text = cardsCount.ToString();

        plusButton.interactable = cardsCount < maxDeckNumber;
        minusButton.interactable = cardsCount > 0;

        UpdateButtonSprites();
    }

    void UpdateButtonSprites()
    {
        if (plusButton.interactable)
        {
            addButtonImage.sprite = addButtonActiveSprite;
        }
        else
        {
            addButtonImage.sprite = addButtonInactiveSprite;
        }

        if (minusButton.interactable)
        {
            minusButtonImage.sprite = minusButtonActiveSprite;
        }
        else
        {
            minusButtonImage.sprite = minusButtonInactiveSprite;
        }
    }
    private GameObject FindCardIconByName(string cardName)
    {
        // Przeszukaj wszystkie obiekty w panelu, aby znale�� te z odpowiednim tekstem
        foreach (Transform child in panelParent)
        {
            Text cardText = child.GetComponentInChildren<Text>();
            if (cardText != null && cardText.text == cardName)
            {
                return child.gameObject; // Zwr�� obiekt karty, je�li znaleziono dopasowanie
            }
        }
        return null; // Je�li nie znaleziono
    }
    public void AcceptCard()
    {
        
        

        // Sprawd�, czy karta o danym ID ju� istnieje
        CardData existingCard = cardList.Find(card => card.cardId == cardId);

        if (existingCard != null && cardsCount != 0)
        {
            if (listCardsCount == 30)
            {
                Debug.LogWarning("Nie mo�na doda� wi�cej kart. Osi�gni�to maksymalny limit 30 kart w talii.");
                addCardPanel.SetActive(false);
                tooMuchCardsPanel.SetActive(true);
                return; // Przerwij dzia�anie metody
            }

            // Je�li karta istnieje, zaktualizuj jej ilo��
            int previousCount = existingCard.cardsCount;


            existingCard.cardsCount = cardsCount;
            Debug.Log($"Karta o ID {cardId} ju� istnieje. Zaktualizowano ilo�� na {existingCard.cardsCount}.");

            // Aktualizuj `listCardsCount` r�nic� ilo�ci
            listCardsCount += (cardsCount - previousCount);
            deckQuantityText.text = $"{listCardsCount}/30";

            // Znajd� obiekt w UI, kt�ry zawiera odpowiedni tekst karty
            GameObject cardIcon = FindCardIconByName(cardName);
            if (cardIcon != null)
            {
                // Zaktualizuj ilo�� na tym obiekcie
                Text cardQuantityText = cardIcon.transform.Find("CardQuantityText").GetComponent<Text>();
                if (cardQuantityText != null)
                {
                    cardQuantityText.text = existingCard.cardsCount.ToString();
                    Debug.Log($"Zaktualizowano ilo�� karty {cardName} na {existingCard.cardsCount}.");
                }
                else
                {
                    Debug.LogWarning("Brak komponentu Text w CardQuantityText!");
                }
            }
            else
            {
                Debug.LogWarning($"Nie znaleziono ikony karty o nazwie {cardName}.");
            }
        }
        else if (cardsCount == 0)
        {
            DeleteCardFromList();
        }
        else
        {
            // Upewnij si�, �e dodanie nowej karty nie przekroczy limitu
            if (listCardsCount + cardsCount > 30)
            {
                Debug.LogWarning("Nie mo�na doda� tej ilo�ci kart. Przekroczy�oby to maksymalny limit 30 kart w talii.");
                addCardPanel.SetActive(false);
                tooMuchCardsPanel.SetActive(true);
                return;
            }

            // Je�li karta nie istnieje, dodaj now�
            CardData newCard = new CardData(cardsCount, cardId, type, cardName);
            cardList.Add(newCard);

            // Dodaj ilo�� nowej karty do `listCardsCount`
            listCardsCount += cardsCount;
            deckQuantityText.text = $"{listCardsCount}/30";

            Debug.Log($"Nowa karta zosta�a zapisana: {cardName} ({cardsCount} szt.), ID: {cardId}, Typ: {type}");
            ShowCardOnList();
        }

        addCardPanel.SetActive(false);
    }

    public void ShowCardOnList()
    {
        if (cardIconPrefab != null && panelParent != null)
        {
            GameObject newCardIcon = Instantiate(cardIconPrefab, panelParent);
            Text cardText = newCardIcon.GetComponentInChildren<Text>();
            Text cardQuantityText = newCardIcon.transform.Find("CardQuantityText").GetComponent<Text>();
            if (cardText != null)
            {
                cardText.text = cardName;
            }
            else
            {
                Debug.LogWarning("Prefab does not have a Text component!");
            }
            if (cardQuantityText != null)
            {
                cardQuantityText.text = cardsCount.ToString();
            }
            else
            {
                Debug.LogWarning("Prefab does not have a CardQuantityText component!");
            }

            //cardListController.SaveCard(cardId, type, cardsCount, cardName);
            Debug.Log($"Card {cardName} added to the deck with quantity {cardsCount}.");
        }
        else
        {
            Debug.LogError("CardIcon prefab or panelParent is not assigned!");
        }
        cardsCount = 0;

    }
    public void DeleteCardFromList()
    {
        // Usu� kart� z listy
        CardData existingCard = cardList.Find(card => card.cardId == cardId);
        if (existingCard != null)
        {
            // Odejmij liczb� usuni�tych kart od listCardsCount
            listCardsCount -= existingCard.cardsCount;
            // Zaktualizuj tekst wy�wietlaj�cy ilo�� kart w talii
            deckQuantityText.text = $"{listCardsCount}/30";

            cardList.Remove(existingCard);
            Debug.Log($"Karta o ID {cardId} zosta�a usuni�ta z listy.");
        }

        // Usu� obiekt karty z UI
        GameObject cardIcon = FindCardIconByName(cardName);
        if (cardIcon != null)
        {
            Destroy(cardIcon);
            Debug.Log($"Karta o nazwie {cardName} zosta�a usuni�ta z UI.");
        }
        else
        {
            Debug.LogWarning($"Nie znaleziono ikony karty o nazwie {cardName}.");
        }
    }
    public void ClosePanelTooMuch()
    {
        tooMuchCardsPanel.SetActive(false);
    }


    void OnDestroy()
    {

        if (plusButton != null)
        {
            plusButton.onClick.AddListener(IncreaseLobbySize);
        }

        if (minusButton != null)
        {
            minusButton.onClick.AddListener(DecreaseLobbySize);
        }
    }

}
