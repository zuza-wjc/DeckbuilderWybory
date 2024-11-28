using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Database;
using UnityEngine.UI;
using System.Globalization;
using Firebase.Extensions;
using System;

public class GameEndManager : MonoBehaviour
{
    public Button backToMainButton;
    public Text informationText;

    private DatabaseReference dbRefLobby;
    private DatabaseReference dbRef;
    private string lobbyId;
    private string playerId;
    private int rounds;

    IEnumerator Start()
    {
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            yield break;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");
        dbRefLobby = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);

        backToMainButton.onClick.AddListener(EndGame);

        yield return StartCoroutine(GetRounds());

        if (rounds <= 0)
        {
            informationText.text = "Rozgrywka zakoñczona!";
        }
        else
        {
            informationText.text = "Rozgrywka przerwana! Gracz opuœci³ grê.";
        }
    }

    void EndGame()
    {
        StartCoroutine(CheckAndHandleEndGame());
    }

    IEnumerator CheckAndHandleEndGame()
    {
        bool otherPlayersInGame = false;

        var task = dbRef.GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted || task.Result == null)
        {
            Debug.LogError("Error retrieving player data or no players found.");
            ChangeToScene();
            yield break;
        }

        DataSnapshot snapshot = task.Result;
        foreach (var child in snapshot.Children)
        {
            string currentPlayerId = child.Key;
            if (currentPlayerId != playerId)
            {
                bool inGame = child.Child("stats").Child("inGame").Value as bool? ?? false;
                if (inGame)
                {
                    otherPlayersInGame = true;
                    break;
                }
            }
        }

        dbRef.Child(playerId).Child("stats").Child("inGame").SetValueAsync(false);

        if (!otherPlayersInGame)
        {
            yield return StartCoroutine(RemoveSessionAfterDelay());
        }

        ChangeToScene();
    }

    void ChangeToScene()
    {
        SceneManager.LoadScene("Main Menu", LoadSceneMode.Single);
    }

    IEnumerator RemoveSessionAfterDelay()
    {
        yield return new WaitForSeconds(1.0f);

        if (dbRefLobby != null)
        {
            dbRefLobby.RemoveValueAsync().ContinueWith(task => {
                if (task.IsCompleted)
                {
                    Debug.Log("Session removed successfully.");
                }
                else
                {
                    Debug.LogError("Failed to remove session: " + task.Exception);
                }
            });
        }
    }

    IEnumerator GetRounds()
    {
        bool isCompleted = false;

        dbRefLobby.Child("rounds").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists && snapshot.Value != null)
                {
                    rounds = int.Parse(snapshot.Value.ToString());
                }
                else
                {
                    Debug.LogWarning("Rounds not found.");
                    rounds = 0; // Domyœlna wartoœæ
                }
            }
            else
            {
                Debug.LogError("Failed to get rounds: " + task.Exception);
            }

            isCompleted = true; // Oznacz jako ukoñczone
        });

        // Czekaj, a¿ `isCompleted` bêdzie true
        while (!isCompleted)
        {
            yield return null;
        }
    }

    void OnDestroy()
    {
        if (backToMainButton != null)
        {
            backToMainButton.onClick.RemoveListener(EndGame);
        }
    }
}
