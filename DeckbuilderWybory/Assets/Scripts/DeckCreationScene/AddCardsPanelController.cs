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

    private Image addButtonImage;
    private Image minusButtonImage;
    public Sprite addButtonActiveSprite;
    public Sprite minusButtonActiveSprite;
    public Sprite minusButtonInactiveSprite;
    public Sprite addButtonInactiveSprite;

    public CardButtonController cardButtonController;
    public GameObject addCardPanel;
    public GameObject cardIconPrefab;

    public Transform panelParent;

    int cardsCount = 0;
    int maxDeckNumber = 0;
    string cardId;
    string type;
    string cardName;

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

    void Start()
    {
        addCardPanel.SetActive(false);
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

        if (existingCard != null && cardsCount!= 0)
        {
            // Je�li karta istnieje, zaktualizuj jej ilo��
            existingCard.cardsCount = cardsCount; // Przyk�ad: aktualizacja ilosci
            Debug.Log($"Karta o ID {cardId} ju� istnieje. Zaktualizowano ilo�� na {existingCard.cardsCount}.");

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
            // Je�li karta nie istnieje, dodaj now�
            CardData newCard = new CardData(cardsCount, cardId, type, cardName);
            cardList.Add(newCard);
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
