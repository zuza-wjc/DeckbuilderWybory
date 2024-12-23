using Firebase.Database;
using Firebase;
using System.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public Image image;
    [HideInInspector] public Transform parentAfterDrag;
    private Vector2 startTouchPosition;
    private bool isDrag = false;
    public float tapThreshold = 10f;
    private bool isEnlarged = false;
    [HideInInspector] public string instanceId;
    [HideInInspector] public string cardId;
    public GameObject cardPanel;
    public Image panelImage;
    public Button closeButton;
    string currentCardId;

    string cardType;
    int sellCost;
    private int clickCount=0;

    public GameObject sellPanel;
    public Button trashButton;
    public Button yesSellButton;
    public Button noSellButton;
    public Text sellText;

    string lobbyId;
    string playerId;
    DatabaseReference dbRef;

    public void OnPointerDown(PointerEventData eventData)
    {
        startTouchPosition = Input.GetTouch(0).position;
        isDrag = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDrag)
        {
            OnTap();
        }
    }

    private void OnTap()
    {
        if (cardPanel == null || panelImage == null || closeButton == null)
        {
            Debug.LogError("Panel UI or its components are not assigned in DraggableItem!");
            return;
        }

        if (image == null)
        {
            Debug.LogError("Image reference is missing in DraggableItem!");
            return;
        }


        panelImage.sprite = image.sprite;
        cardPanel.SetActive(true);

        DraggableItem draggableItem = GetComponentInParent<DraggableItem>();
        if (draggableItem != null)
        {
            currentCardId = draggableItem.cardId;
        }
        //Debug.LogError(currentCardId);


        trashButton.onClick.RemoveAllListeners();
        trashButton.onClick.AddListener(SellPanelOpen);



        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(ClosePanel);
    }

    private async void SellPanelOpen()
    {
        clickCount=0;
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        string cardType = cardId.Substring(0, 2);

        switch (cardType)
        {
            case "AD":
                cardType = "addRemove";
                break;
            case "AS":
                cardType = "asMuchAs";
                break;
            case "CA":
                cardType = "cards";
                break;
            case "OP":
                cardType = "options";
                break;
            case "RA":
                cardType = "random";
                break;
            case "UN":
                cardType = "unique";
                break;
            default:
                cardType = "";
                break;

        }
        dbRef = FirebaseInitializer.DatabaseReference
            .Child("cards")
            .Child("id")
            .Child(cardType)
            .Child(currentCardId)
            .Child("cost");

        DataSnapshot sellSnapshot = await dbRef.GetValueAsync();

        sellCost = Convert.ToInt32(sellSnapshot.Value);
        //Debug.Log(currentCardId + " koszt: " + sellCost + " Type: " + cardType);

        if (0<=sellCost && sellCost<=4)
        {
            sellCost=2;
        }
        else if (sellCost%2==0)
        {
            sellCost=sellCost/2;
        }
        else
        {
            sellCost=(sellCost+1)/2;
        }

        sellText.text= $"Czy chcesz sprzedać kartę za {sellCost}k?";
        sellPanel.SetActive(true);

        yesSellButton.onClick.RemoveAllListeners();
        yesSellButton.onClick.AddListener(ExchangeForMoney);
        noSellButton.onClick.AddListener(SellPanelClose);

    }


    private async void ExchangeForMoney(){


        if(clickCount==0){
            clickCount++;
            
            lobbyId = DataTransfer.LobbyId;
            playerId = DataTransfer.PlayerId;

            dbRef = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(playerId);

            DataSnapshot budgetSellSnapshot = await dbRef.Child("stats").Child("money").GetValueAsync();
            int budgetSellCost = Convert.ToInt32(budgetSellSnapshot.Value);

            //Debug.Log("obecny fundusz:" + budgetSellCost);

            budgetSellCost=budgetSellCost+sellCost;
            //Debug.Log("po zmianie:" + budgetSellCost);
            await dbRef.Child("stats").Child("money").SetValueAsync(budgetSellCost);

            await dbRef.Child("deck").Child(instanceId).Child("onHand").SetValueAsync(false);
            await dbRef.Child("deck").Child(instanceId).Child("played").SetValueAsync(true);

        }
        SellPanelClose();
        ClosePanel();

    }

    private void SellPanelClose()
    {
        sellPanel.SetActive(false);
    }

    private void ClosePanel()
    {
        if (cardPanel != null)
        {
            cardPanel.SetActive(false);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isEnlarged)
        {
            float distance = Vector2.Distance(startTouchPosition, Input.GetTouch(0).position);
            if (distance > tapThreshold)
            {
                isDrag = true;
                parentAfterDrag = transform.parent;
                transform.SetParent(transform.root);
                transform.SetAsLastSibling();
                image.raycastTarget = false;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDrag)
        {
            transform.position = Input.GetTouch(0).position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDrag)
        {
            transform.SetParent(parentAfterDrag);
            image.raycastTarget = true;
        }
    }
}
