using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameExitListener : MonoBehaviour
{
    DatabaseReference dbRef;
    DatabaseReference dbRefPlayers;
    string lobbyId;

    public GameObject quitGamePanel;
    public Button closeButton;

    void Start()
    {
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        lobbyId = DataTransfer.LobbyId;

        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("LobbyId is not assigned properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);
        dbRefPlayers = dbRef.Child("players");

        if (quitGamePanel == null || closeButton == null)
        {
            Debug.LogError("UI components are not assigned properly!");
            return;
        }

        quitGamePanel.SetActive(false);
        closeButton.onClick.AddListener(QuitGame);

        if (dbRefPlayers != null)
        {
            dbRefPlayers.ChildChanged += HandleInGameChanged;
        }
    }

    void OnDestroy()
    {
        if (dbRefPlayers != null)
        {
            dbRefPlayers.ChildChanged -= HandleInGameChanged;
        }

        if (quitGamePanel != null && closeButton != null)
        {
            closeButton.onClick.RemoveListener(QuitGame);
        }
    }

    void HandleInGameChanged(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (args.Snapshot.Child("stats").Child("inGame").Exists)
        {
            bool inGameState = (bool)args.Snapshot.Child("stats").Child("inGame").Value;
            if (!inGameState)
            {
          
                if (quitGamePanel != null)
                {
                    quitGamePanel.SetActive(true);
                }
             
                if (dbRefPlayers != null)
                {
                    dbRefPlayers.ChildChanged -= HandleInGameChanged;
                }
                StartCoroutine(RemoveSessionAfterDelay());
            }
        }
    }

    IEnumerator RemoveSessionAfterDelay()
    {
        yield return new WaitForSeconds(1.0f);

        if (dbRef != null)
        {
            dbRef.RemoveValueAsync().ContinueWith(task => {
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

    void QuitGame()
    {
        if (quitGamePanel != null)
        {
            quitGamePanel.SetActive(false);
        }
        SceneManager.LoadScene("Main Menu", LoadSceneMode.Single);
    }
}
