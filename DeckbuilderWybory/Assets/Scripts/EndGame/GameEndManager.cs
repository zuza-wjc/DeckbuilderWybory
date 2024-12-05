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
    private DatabaseReference dbRefLobby;
    private DatabaseReference dbRef;
    private string lobbyId;
    private string playerId;

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
    }

    public void EndGame()
    {
        Debug.Log("Klikniety przycisk main menu");
        StartCoroutine(CheckLobby());
    }

    IEnumerator CheckLobby()
    {
        // SprawdŸ, czy lobby istnieje
        var lobbyTask = dbRefLobby.GetValueAsync();
        yield return new WaitUntil(() => lobbyTask.IsCompleted);

        if (lobbyTask.IsFaulted || lobbyTask.Result == null || !lobbyTask.Result.Exists)
        {
            Debug.LogWarning("Lobby does not exist. Returning to Main Menu.");
            ChangeToScene(); // Jeœli lobby nie istnieje, przejdŸ do g³ównego menu
            yield break;
        }

        // Jeœli lobby istnieje, kontynuuj normaln¹ logikê
        yield return StartCoroutine(CheckAndHandleEndGame());
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
            yield return StartCoroutine(RemoveSession());
        }

        ChangeToScene();
    }

    void ChangeToScene()
    {
        SceneManager.LoadScene("Main Menu", LoadSceneMode.Single);
    }

    IEnumerator RemoveSession()
    {
        yield return new WaitForSeconds(0.1f);

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
}