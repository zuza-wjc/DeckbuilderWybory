using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Extensions;
using System;

public class PassTurnPanelController : MonoBehaviour
{
    [SerializeField] private GameObject passTurnPanel;
    [SerializeField] private GameObject passTurnPanelExpired;

    DatabaseReference dbRef;
    string lobbyId;
    string playerId;

    void Start()
    {
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");

        dbRef.Child(playerId).Child("stats").Child("playerTurn").ValueChanged += HandleMyTurnChanged;
    }

    public void SetPassTurnPanelActive()
    {
        if (passTurnPanel != null)
        {
            passTurnPanel.SetActive(true);
            passTurnPanel.transform.SetAsLastSibling();
        }
        else
        {
            Debug.LogWarning("PassTurnPanel nie jest przypisany!");
        }
    }

    public void SetPassTurnPanelInactive()
    {
        if (passTurnPanel != null)
        {
            passTurnPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("PassTurnPanel nie jest przypisany!");
        }
    }

    public void SetPassTurnPanelExpiredActive()
    {
        if (passTurnPanelExpired != null)
        {
            passTurnPanelExpired.SetActive(true);
            passTurnPanelExpired.transform.SetAsLastSibling();
        }
        else
        {
            Debug.LogWarning("passTurnPanelExpired nie jest przypisany!");
        }
    }

    public void SetPassTurnPanelExpiredInactive()
    {
        if (passTurnPanelExpired != null)
        {
            passTurnPanelExpired.SetActive(false);
        }
        else
        {
            Debug.LogWarning("passTurnPanelExpired nie jest przypisany!");
        }
    }

    void HandleMyTurnChanged(object sender, ValueChangedEventArgs args)
    {
        int isMyTurn = Convert.ToInt32(args.Snapshot.Value);
        if (isMyTurn != 1)
        {
            if (passTurnPanel != null && passTurnPanel.activeSelf)
            {
                SetPassTurnPanelInactive();
                SetPassTurnPanelExpiredActive();
            }
        }
        else
        {
            if (passTurnPanelExpired != null && passTurnPanelExpired.activeSelf)
            {
                SetPassTurnPanelExpiredInactive();
            }
        }
    }

    void OnDestroy()
    {
        if (dbRef != null)
        {
            dbRef.Child(playerId).Child("stats").Child("playerTurn").ValueChanged -= HandleMyTurnChanged;
        }
    }
}