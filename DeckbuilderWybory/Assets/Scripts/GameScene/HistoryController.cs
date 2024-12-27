using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class HistoryController : MonoBehaviour
{
    public GameObject cardPrefab;
    public Transform cardListContainer;
    public Button closeButton;
    public Button openHistoryButton;
    public GameObject cardViewerPanel;
    public Text infoText;
    public Text cardText;
    public CanvasGroup infoTextCanvasGroup;

    public CardSpriteManager cardSpriteManager;

    private DatabaseReference historyRef;
    private bool isHistoryListenerActive = false;

    private Queue<string> infoTextQueue = new Queue<string>();
    private bool isShowingInfoText = false;

    private void Start()
    {

        if (openHistoryButton != null)
        {
            openHistoryButton.onClick.AddListener(async () =>
            {
                string lobbyId = DataTransfer.LobbyId;
                await ShowHistory(lobbyId);
            });
        }

        string lobbyId = DataTransfer.LobbyId;
        historyRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("history");

        if (!isHistoryListenerActive)
        {
            StartHistoryListener();
        }
    }

    private void StartHistoryListener()
    {
        isHistoryListenerActive = true;
        historyRef.ChildAdded += HistoryRef_ChildAdded;
    }

    private async void HistoryRef_ChildAdded(object sender, ChildChangedEventArgs e)
    {
        if (e.Snapshot.Exists)
        {
            string description = e.Snapshot.Child("description")?.Value?.ToString();
            if (!string.IsNullOrEmpty(description))
            {
                infoTextQueue.Enqueue(description);
                if (!isShowingInfoText)
                {
                    await ProcessInfoTextQueue();
                }
            }
        }
    }

    private async Task ProcessInfoTextQueue()
    {
        if (cardText == null)
            return;

        isShowingInfoText = true;

        while (infoTextQueue.Count > 0)
        {
            string nextText = infoTextQueue.Dequeue();
            cardText.text = nextText;
            await ShowInfoText();
        }

        isShowingInfoText = false;
    }


    public async Task ShowHistory(string lobbyId)
    {
        ClearUI();
        cardViewerPanel.SetActive(true);
        cardViewerPanel.transform.SetAsLastSibling();
        infoText.text = "Historia kart";

        var snapshot = await historyRef.GetValueAsync();
        if (!snapshot.Exists || snapshot.ChildrenCount == 0)
        {
            infoText.text = "Brak kart w historii.";
        }
        else
        {
            foreach (var cardSnapshot in snapshot.Children)
            {
                string cardId = cardSnapshot.Child("cardId").Value as string;
                string playerName = cardSnapshot.Child("playerName").Value as string;
                string enemyName = cardSnapshot.Child("enemyName").Value as string;

                if (!string.IsNullOrEmpty(cardId) && !string.IsNullOrEmpty(playerName))
                {
                    AddCardToUI(cardId, playerName, enemyName);
                }
            }
        }

        closeButton.gameObject.SetActive(true);
    }

    private void AddCardToUI(string cardId, string playerName, string enemyName)
    {
        GameObject newCardUI = Instantiate(cardPrefab, cardListContainer);


        newCardUI.transform.SetSiblingIndex(0);

        Image cardImage = newCardUI.transform.Find("cardImage").GetComponent<Image>();
        Sprite cardSprite = cardSpriteManager?.GetCardSprite(cardId);
        if (cardSprite == null)
        {
            return;
        }
        cardImage.sprite = cardSprite;

        Text playerNameText = newCardUI.transform.Find("playerNameText").GetComponent<Text>();
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }

        string templateText = "zagrana na\n{0}";

        Text enemyNameText = newCardUI.transform.Find("enemyNameText").GetComponent<Text>();

        if (enemyName != null)
        {
            string finalText = string.Format(templateText, enemyName);

            if (enemyNameText != null)
            {
                enemyNameText.text = finalText;
                enemyNameText.enabled = true;
            }
        }
        else
        {
            if (enemyNameText != null)
            {
                enemyNameText.text = "";
                enemyNameText.enabled = false;
            }
        }
    }

    public void HideAndClearUI()
{
    cardViewerPanel.SetActive(false);
    ClearUI();
}


    private void ClearUI()
    {
        foreach (Transform child in cardListContainer)
        {
            if (child != null)
                Destroy(child.gameObject);
        }
    }


    public async Task AddCardToHistory(string cardId, string playerId, string desc, string enemyId)
    {

        if (string.IsNullOrEmpty(cardId) || string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(desc))
        {
            Debug.LogWarning("Invalid input parameters.");
            return;
        }

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference playerRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId);

        DatabaseReference enemyRef = string.IsNullOrEmpty(enemyId) ? null :
            FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId);

        DatabaseReference historyRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("history");

        try
        {
            var playerSnapshot = await playerRef.GetValueAsync();
            if (!playerSnapshot.Exists)
            {
                Debug.LogWarning("Player not found.");
                return;
            }

            string playerName = playerSnapshot.Child("playerName")?.Value?.ToString();
            if (string.IsNullOrEmpty(playerName))
            {
                Debug.LogWarning("Player name is null or empty.");
                return;
            }

            string enemyName = null;
            if (enemyRef != null)
            {
                var enemySnapshot = await enemyRef.GetValueAsync();
                if (enemySnapshot.Exists)
                {
                    enemyName = enemySnapshot.Child("playerName")?.Value?.ToString();
                }
            }

            if (!string.IsNullOrEmpty(enemyName) && desc.Contains("rywala"))
            {
                desc = desc.Replace("rywala", $"rywala {enemyName}");
            } else
            {
                enemyName = null;
            }

            desc = desc.Replace("Gracz", $"Gracz {playerName}");

            var historySnapshot = await historyRef.GetValueAsync();
            if (!historySnapshot.Exists)
            {
                await historyRef.SetValueAsync(new Dictionary<string, object>());
            }

            if (historySnapshot.Exists && historySnapshot.ChildrenCount >= 10)
            {
                string oldestKey = historySnapshot.Children.First().Key;
                await historyRef.Child(oldestKey).RemoveValueAsync();
            }

            string historyId = historyRef.Push().Key;
            var historyEntry = new Dictionary<string, object>
        {
            { "cardId", cardId },
            { "playerName", playerName },
            { "enemyName", enemyName },
            { "description", desc }
        };

            await historyRef.Child(historyId).SetValueAsync(historyEntry);
            Debug.Log("History entry added successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to add card to history: {ex.Message}");
        }
    }


    private async Task ShowInfoText()
    {
        if (infoTextCanvasGroup == null || cardText == null)
            return;

        infoTextCanvasGroup.alpha = 0;
        cardText.gameObject.SetActive(true);

        await FadeInText(2f);

        await Task.Delay(3000);

        await FadeOutText(2f);
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
            if (infoTextCanvasGroup == null)
                return;
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
            if (infoTextCanvasGroup == null)
                return;
            infoTextCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            await Task.Yield();
        }

        infoTextCanvasGroup.alpha = endAlpha;
        cardText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (historyRef != null)
        {
            historyRef.ChildAdded -= HistoryRef_ChildAdded;
        }

        if (openHistoryButton != null)
        {
            openHistoryButton.onClick.RemoveAllListeners();
        }
    }

}