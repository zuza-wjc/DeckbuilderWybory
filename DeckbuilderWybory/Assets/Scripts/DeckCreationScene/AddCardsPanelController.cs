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

    public Sprite podstawaListCardSprite;
    public Sprite ambasadaListCardSprite;
    public Sprite przemyslListCardSprite;
    public Sprite metropoliaListCardSprite;
    public Sprite srodowiskoListCardSprite;

    public CardButtonController cardButtonController;
    public EditListCardsPanel editListCardsPanel;
    public CardSpriteManager cardSpriteManager;

    public GameObject addCardPanel;
    public GameObject tooMuchCardsPanel;
    public GameObject cardIconPrefab;
    public GameObject cardPrefab;

    public Transform panelParent;
    public Transform shadePanel;

    public Text deckNameText;
    public Text deckQuantityText;
    public Text infoText;

    int cardsCount = 0;
    int maxDeckNumber = 0;
    string cardId;
    string type;
    string cardName;

    int listCardsCount = 0;
    int podstawaCardsCount = 0;
    int specjalneCardsCount = 0;
    string deckCardsType;

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
                    Debug.Log($"TYP DECKU: {deckCardsType}");
                    Debug.Log($"podstawaCardsCount: {podstawaCardsCount}");
                    Debug.Log($"specjalneCardsCount: {specjalneCardsCount}");
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

            newCardIcon.name = card.cardId;
            // Pobierz przycisk z instancji
            Button button = newCardIcon.GetComponent<Button>();

            if (button != null && editListCardsPanel != null)
            {
                // Dodaj listener do przycisku
                button.onClick.AddListener(() => editListCardsPanel.OnCardButtonClick(button));
            }
            else
            {
                Debug.LogWarning("Nie uda�o si� przypisa� funkcji do przycisku!");
            }

            // Ustawianie tekstu karty
            Text cardText = newCardIcon.GetComponentInChildren<Text>();
            Text cardQuantityText = newCardIcon.transform.Find("CardQuantityText").GetComponent<Text>();
            Image cardImage = newCardIcon.GetComponentInChildren<Image>(); // Dodajemy Image, aby zmieni� sprite

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

            // Zmieniamy sprite w zale�no�ci od typu karty
            if (cardImage != null)
            {
                switch (card.type)
                {
                    case "Podstawa":
                        cardImage.sprite = podstawaListCardSprite;
                        //zwieksz licznik kart podstawa o cards count karty
                        podstawaCardsCount += card.cardsCount;
                        break;
                    case "Ambasada":
                        cardImage.sprite = ambasadaListCardSprite;
                        specjalneCardsCount += card.cardsCount;
                        deckCardsType = "Ambasada";
                        break;
                    case "Przemys�":
                        cardImage.sprite = przemyslListCardSprite;
                        specjalneCardsCount += card.cardsCount;
                        deckCardsType = "Przemys�";
                        break;
                    case "Metropolia":
                        cardImage.sprite = metropoliaListCardSprite;
                        specjalneCardsCount += card.cardsCount;
                        deckCardsType = "Metropolia";
                        break;
                    case "�rodowisko":
                        cardImage.sprite = srodowiskoListCardSprite;
                        specjalneCardsCount += card.cardsCount;
                        deckCardsType = "�rodowisko";
                        break;
                    default:
                        Debug.LogWarning($"Unknown card type: {card.type}. No sprite change.");
                        break;
                }
                
            }
            else
            {
                Debug.LogWarning("Prefab does not have an Image component to change the sprite!");
            }

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

        CardData existingCard = cardList.Find(card => card.cardId == cardId);
        if (existingCard == null)
        {
            cardsCount = 0; // Mo�esz przypisa� domy�ln� warto��, je�li karta nie istnieje
        }
        else
        {
            cardsCount = existingCard.cardsCount;
        }
        //cardsCount = 0;  

        addButtonImage = plusButton.GetComponentInChildren<Image>();
        minusButtonImage = minusButton.GetComponentInChildren<Image>();

        // Usu� istniej�ce listenery przed dodaniem nowych
        plusButton.onClick.RemoveAllListeners();
        minusButton.onClick.RemoveAllListeners();
        acceptButton.onClick.RemoveAllListeners();

        plusButton.onClick.AddListener(IncreaseLobbySize);
        minusButton.onClick.AddListener(DecreaseLobbySize);
        acceptButton.onClick.AddListener(AcceptCard);
        
        ShowInfoCard();
        Canvas.ForceUpdateCanvases();
        UpdateLobbySizeText();
        

        // Znajd� CardButtonController w scenie
        cardButtonController = FindObjectOfType<CardButtonController>();
    }
    public void ShowInfoCard()
    {
        if (cardSpriteManager != null)
        {
            // Get the sprite using the cardId
            Sprite cardSprite = cardSpriteManager.GetCardSprite(cardId);
            if (cardSprite != null)
            {
                // Assuming you have a reference to the 'cardPrefab' GameObject
                if (cardPrefab != null)
                {
                    GameObject instantiatedCard = Instantiate(cardPrefab);
                    instantiatedCard.transform.SetParent(shadePanel.transform);
                    // Get the Image component from the cardPrefab (assuming it's a child of the prefab)
                    Image cardImage = instantiatedCard.GetComponentInChildren<Image>();
                    if (cardImage != null)
                    {
                        // Set the sprite on the Image component of the prefab
                        cardImage.sprite = cardSprite;
                        Debug.Log($"Card sprite for '{cardId}' has been set successfully.");
                    }
                    else
                    {
                        Debug.LogError("No Image component found in cardPrefab.");
                    }

                    if (shadePanel != null)
                    {
                        instantiatedCard.transform.SetParent(shadePanel.transform);

                        // Adjust the position to the right by changing the localPosition
                        RectTransform rectTransform = instantiatedCard.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            // Set the localPosition to move it to the right (adjust the X value)
                            rectTransform.localPosition = new Vector3(650f, 0f, 0f); // Modify '200f' as per your need
                            rectTransform.sizeDelta = new Vector2(800f, 1200f);
                        }
                        else
                        {
                            Debug.LogError("The instantiated card does not have a RectTransform.");
                        }
                    }
                    else
                    {
                        Debug.LogError("shadePanel is not assigned.");
                    }
                }
                else
                {
                    Debug.LogError("cardPrefab is not assigned.");
                }
            }
        }
        else
        {
            Debug.LogError("CardSpriteManager is not assigned.");
        }
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
        
        // Je�li karta istnieje, zaktualizuj jej ilo��
        if (existingCard != null && cardsCount != 0)
        {
            int previousCount = existingCard.cardsCount; //ile bylo na liscie

            if (type == "Podstawa" && podstawaCardsCount + (cardsCount - previousCount) >= 21)
            {
                Debug.LogWarning($"Nie mo�na doda� wi�cej kart typu 'podstawa'. Osi�gni�to maksymalny limit 20 kart.");
                infoText.text = "Nie mo�na doda� wi�cej kart typu 'podstawa'. Osi�gni�to maksymalny limit 20 kart.";
                addCardPanel.SetActive(false);
                tooMuchCardsPanel.SetActive(true);
                return; // Przerwij dzia�anie metody
            }
            if (type!= "Podstawa" && type != deckCardsType) 
            {
                Debug.LogWarning($"Nie mo�na doda� innej karty typu specjalne. TYP TALII: {deckCardsType}.");
                infoText.text = string.Format("Nie mo�na doda� innej karty typu specjalne. TYP TALII: {0}.", deckCardsType);
                addCardPanel.SetActive(false);
                tooMuchCardsPanel.SetActive(true);
                return; // Przerwij dzia�anie metody
            }
            if (specjalneCardsCount + (cardsCount - previousCount) >= 11)
            {
                Debug.LogWarning($"Nie mo�na doda� wi�cej kart typu 'specjalne'. Osi�gni�to maksymalny limit 10 kart.");
                infoText.text = "Nie mo�na doda� wi�cej kart typu 'specjalne'. Osi�gni�to maksymalny limit 10 kart.";
                addCardPanel.SetActive(false);
                tooMuchCardsPanel.SetActive(true);
                return; // Przerwij dzia�anie metody
            }

            existingCard.cardsCount = cardsCount;

            Debug.Log($"Karta o ID {cardId} ju� istnieje. Zaktualizowano ilo�� na {existingCard.cardsCount}.");

            // Aktualizuj `listCardsCount` r�nic� ilo�ci
            listCardsCount += (cardsCount - previousCount);
            if (type == "Podstawa")
            {
                podstawaCardsCount += (cardsCount - previousCount);
                Debug.Log($"CardsCount: {cardsCount}");
                Debug.Log($"podstawaCardsCount: {podstawaCardsCount}");
            }
            else
            {
                specjalneCardsCount += (cardsCount - previousCount);
                Debug.Log($"CardsCount: {cardsCount}");
                Debug.Log($"specjalneCardsCount: {specjalneCardsCount}");
                if (specjalneCardsCount == 0)
                {
                    deckCardsType = null;
                    Debug.Log($"deckCardsType usuniety: {deckCardsType}");
                }
            }
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
            if (type == "Podstawa" && podstawaCardsCount + cardsCount >= 21)
            {
                Debug.LogWarning($"Nie mo�na doda� wi�cej kart typu 'podstawa'. Osi�gni�to maksymalny limit 20 kart.");
                infoText.text = "Nie mo�na doda� wi�cej kart typu 'podstawa'. Osi�gni�to maksymalny limit 20 kart podstawy.";
                addCardPanel.SetActive(false);
                tooMuchCardsPanel.SetActive(true);
                return; // Przerwij dzia�anie metody
            }
            if (type != "Podstawa" && deckCardsType != null && type != deckCardsType) 
            {
                Debug.LogWarning($"Nie mo�na doda� innej karty typu specjalne. TYP TALII: {deckCardsType}.");
                infoText.text = string.Format("Nie mo�na doda� innej karty typu specjalne. TYP TALII: {0}.", deckCardsType);
                addCardPanel.SetActive(false);
                tooMuchCardsPanel.SetActive(true);
                return; // Przerwij dzia�anie metody
            }
            if (specjalneCardsCount + cardsCount >= 11)
            {
                Debug.LogWarning($"Nie mo�na doda� wi�cej kart typu 'specjalne'. Osi�gni�to maksymalny limit 10 kart.");
                infoText.text = "Nie mo�na doda� wi�cej kart typu 'specjalne'. Osi�gni�to maksymalny limit 10 kart.";
                addCardPanel.SetActive(false);
                tooMuchCardsPanel.SetActive(true);
                return; // Przerwij dzia�anie metody

            }

            // Je�li karta nie istnieje, dodaj now�
            CardData newCard = new CardData(cardsCount, cardId, type, cardName);
            if(type == "Podstawa")
            {
                podstawaCardsCount += cardsCount;
                Debug.Log($"podstawaCardsCount: {podstawaCardsCount}");
            }
            else
            {
                specjalneCardsCount += cardsCount;
                Debug.Log($"specjalneCardsCount: {specjalneCardsCount}");
            }
            cardList.Add(newCard);
            if(deckCardsType == null && type!= "Podstawa" )
            {
                deckCardsType = type;
                Debug.Log($"zmiana typu przy pierwszej specjalnej karcie: {type}");
            }

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
            // Tworzenie nowej ikony karty
            GameObject newCardIcon = Instantiate(cardIconPrefab, panelParent);
            newCardIcon.name = cardId;
            // Pobierz przycisk z instancji
            Button button = newCardIcon.GetComponent<Button>();

            if (button != null && editListCardsPanel != null)
            {
                // Dodaj listener do przycisku
                button.onClick.AddListener(() => editListCardsPanel.OnCardButtonClick(button));
            }
            else
            {
                Debug.LogWarning("Nie uda�o si� przypisa� funkcji do przycisku!");
            }

            Text cardText = newCardIcon.GetComponentInChildren<Text>();
            Text cardQuantityText = newCardIcon.transform.Find("CardQuantityText").GetComponent<Text>();
            Image cardImage = newCardIcon.GetComponentInChildren<Image>(); // Dodajemy Image, aby zmieni� sprite

            if (cardText != null)
            {
                cardText.text = cardName; // Ustaw nazw� karty
            }
            else
            {
                Debug.LogWarning("Prefab does not have a Text component!");
            }

            if (cardQuantityText != null)
            {
                cardQuantityText.text = cardsCount.ToString(); // Ustaw ilo�� karty
            }
            else
            {
                Debug.LogWarning("Prefab does not have a CardQuantityText component!");
            }

            // Zmieniamy sprite w zale�no�ci od typu karty
            if (cardImage != null)
            {
                switch (type)
                {
                    case "Podstawa":
                        cardImage.sprite = podstawaListCardSprite;
                        Debug.Log($"Sprite for card {cardName} changed to 'Podstawa'.");
                        break;
                    case "Ambasada":
                        cardImage.sprite = ambasadaListCardSprite;
                        Debug.Log($"Sprite for card {cardName} changed to 'Ambasada'.");
                        break;
                    case "Przemys�":
                        cardImage.sprite = przemyslListCardSprite;
                        Debug.Log($"Sprite for card {cardName} changed to 'Przemys�'.");
                        break;
                    case "Metropolia":
                        cardImage.sprite = metropoliaListCardSprite;
                        Debug.Log($"Sprite for card {cardName} changed to 'Metropolia'.");
                        break;
                    case "�rodowisko":
                        cardImage.sprite = srodowiskoListCardSprite;
                        Debug.Log($"Sprite for card {cardName} changed to '�rodowisko'.");
                        break;
                    default:
                        Debug.LogWarning($"Unknown card type: {type}. No sprite change.");
                        break;
                }
            }
            else
            {
                Debug.LogWarning("Prefab does not have an Image component to change the sprite!");
            }

            // Logowanie dodania karty do listy
            Debug.Log($"Card {cardName} added to the deck with quantity {cardsCount}.");
        }
        else
        {
            Debug.LogError("CardIcon prefab or panelParent is not assigned!");
        }
        cardsCount = 0; // Resetowanie liczby kart
    }
    public void DeleteCardFromList()
    {
        // Usu� kart� z listy
        CardData existingCard = cardList.Find(card => card.cardId == cardId);
        if (existingCard != null)
        {
            // Odejmij liczb� usuni�tych kart od listCardsCount
            listCardsCount -= (existingCard.cardsCount - cardsCount);
            if (type == "Podstawa")
            {
                podstawaCardsCount -= (existingCard.cardsCount - cardsCount);
                Debug.Log($"podstawaCardsCount: {podstawaCardsCount}");
            }
            else
            {
                specjalneCardsCount -= (existingCard.cardsCount - cardsCount);
                Debug.Log($"specjalneCardsCount: {specjalneCardsCount}");
                if(specjalneCardsCount == 0)
                {
                    deckCardsType = null;
                    Debug.Log($"deckCardsType usuniety: {deckCardsType}");
                }
            }
            // Zaktualizuj tekst wy�wietlaj�cy ilo�� kart w talii
            deckQuantityText.text = $"{listCardsCount}/30";

            cardList.Remove(existingCard);
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
