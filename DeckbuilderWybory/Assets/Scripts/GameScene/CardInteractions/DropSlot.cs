using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class DropSlot : MonoBehaviour, IDropHandler
{
    public GameObject playerListPanel;
    public GameObject mapPanel;
    public CardTypeManager cardTypeManager;

    private DatabaseReference playerStatsRef;

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
        CheckPlayerTurnAndDrop(draggableItem);
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
            Debug.LogError($"Wyst¹pi³ b³¹d podczas pobierania danych: {ex.Message}");
        }
    }
}
