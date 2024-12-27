using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using UnityEngine.UI;
using System.Threading.Tasks;

public class DropSlot : MonoBehaviour, IDropHandler
{
    public GameObject playerListPanel;
    public GameObject mapPanel;
    public CardTypeManager cardTypeManager;

    private DatabaseReference playerStatsRef;

    public Text notYourTurnInfo;
    public CanvasGroup infoTextCanvasGroup;

    [SerializeField] private GameObject cardListContainer;
    [SerializeField] private Canvas mainCanvas;

    private void Awake()
    {
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        playerListPanel.SetActive(false);
        mapPanel.SetActive(false);
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        DraggableItem draggableItem = dropped.GetComponent<DraggableItem>();

        if (draggableItem != null)
        {
            draggableItem.MarkAsDroppedOnSlot();

            CheckPlayerTurnAndDrop(draggableItem);
        }
    }

    private async void CheckPlayerTurnAndDrop(DraggableItem draggableItem)
    {
        string playerId = DataTransfer.PlayerId;

        playerStatsRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(DataTransfer.LobbyId)
            .Child("players")
            .Child(playerId)
            .Child("stats");

        try
        {
            var snapshot = await playerStatsRef.GetValueAsync();

            if (snapshot.Exists)
            {
                int playerTurn = snapshot.Child("playerTurn").Value != null ? Convert.ToInt32(snapshot.Child("playerTurn").Value) : 0;
                if (playerTurn == 1)
                {
                    if (draggableItem != null)
                    {
                        cardTypeManager.OnCardDropped(draggableItem.instanceId, draggableItem.cardId, false);
                    }
                }
                else
                {
                    await ShowInfoText();
                    Debug.Log("Nie jest tura gracza.");
                }
            }
            else
            {
                Debug.LogError("Nie znaleziono danych dla gracza.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Wystąpił błąd podczas pobierania danych: {ex.Message}");
        }
    }

    private async Task ShowInfoText()
    {
        MoveCardsToCardListContainer();

        if (infoTextCanvasGroup == null || notYourTurnInfo == null)
            return;

        infoTextCanvasGroup.alpha = 0;
        notYourTurnInfo.gameObject.SetActive(true);

        await FadeInText(1f);

        await Task.Delay(2000);

        await FadeOutText(1f);
    }

    private void MoveCardsToCardListContainer()
    {
        Transform[] allTransforms = FindObjectsOfType<Transform>();

        foreach (Transform t in allTransforms)
        {
            DraggableItem draggableItem = t.GetComponent<DraggableItem>();
            if (draggableItem != null)
            {
                if (t.parent == mainCanvas.transform && t.parent != cardListContainer.transform)
                {
                    t.SetParent(cardListContainer.transform);
                    t.localPosition = Vector3.zero;

                    Image cardImage = t.GetComponent<Image>();
                    if (cardImage != null)
                    {
                        cardImage.raycastTarget = true;
                    }
                }
            }
        }
    }

    private async Task FadeInText(float duration)
    {
        if (infoTextCanvasGroup == null)
            return;

        float startAlpha = 0;
        float endAlpha = 1;
        float timeElapsed = 0;

        while (timeElapsed < duration)
        {
            infoTextCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            await Task.Yield();
        }

        infoTextCanvasGroup.alpha = endAlpha;
    }

    private async Task FadeOutText(float duration)
    {
        if (infoTextCanvasGroup == null)
            return;

        float startAlpha = infoTextCanvasGroup.alpha;
        float endAlpha = 0;
        float timeElapsed = 0;

        while (timeElapsed < duration)
        {
            infoTextCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            await Task.Yield();
        }

        infoTextCanvasGroup.alpha = endAlpha;
        notYourTurnInfo.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {

        if (infoTextCanvasGroup != null)
        {
            infoTextCanvasGroup.alpha = 0;
        }
    }

}