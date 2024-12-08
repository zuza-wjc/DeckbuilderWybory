using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Extensions;
using System;

public class ExitToGameScene : MonoBehaviour
{
    public Button backButton;
    public string sceneName;
    string lobbyId;

    DatabaseReference dbRef;

    void Start()
    {
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        lobbyId = DataTransfer.LobbyId;
        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);

        backButton.onClick.AddListener(OnButtonClicked);
    }

    void OnButtonClicked()
    {
        dbRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error checking lobby existence: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    // jesli lobby istnieje, wroc do sceny "Game"
                    SceneManager.UnloadSceneAsync(sceneName);
                }
                else
                {
                    // jesli lobby nie istnieje przejdz do sceny "Main Menu"
                    SceneManager.LoadScene("Main Menu", LoadSceneMode.Single);
                }
            }
        });
    }

    void OnDestroy()
    {
        if(backButton != null)
        {
            backButton.onClick.RemoveListener(OnButtonClicked);
        }
    }
}
