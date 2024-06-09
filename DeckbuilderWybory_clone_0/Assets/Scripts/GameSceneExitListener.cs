using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameSceneExitListener : MonoBehaviour
{
    DatabaseReference dbRef;
    DatabaseReference dbRefPlayers;
    string lobbyId;
    string playerId;

    public GameObject quitGamePanel;
    public Button closeButton; // Przycisk zamykaj¹cy overlay

    void Start()
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            // Jeœli nie, inicjalizuj Firebase
            FirebaseInitializer firebaseInitializer = FindObjectOfType<FirebaseInitializer>();
            if (firebaseInitializer == null)
            {
                Debug.LogError("FirebaseInitializer not found in the scene!");
                return;
            }
        }
        lobbyId = PlayerPrefs.GetString("LobbyId");

        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId);
        dbRefPlayers = dbRef.Child("players");

        quitGamePanel.SetActive(false);
        closeButton.onClick.AddListener(QuitGame);
        // Ustaw nas³uchiwanie zmian w strukturze bazy danych (zmiana wartoœci "inGame")
        dbRefPlayers.ChildChanged += HandleInGameChanged;
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
                //odpali sie warunek gdy inGame == false
                //otwieramy overlay
                quitGamePanel.SetActive(true);
                //usuwanie sesji
                if (args.Snapshot.Reference.Parent.Parent.Key == lobbyId)
                {
                    // Usuñ ca³¹ ga³¹Ÿ lobbyId z sessions
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
        }
    }

    void QuitGame()
    {
        quitGamePanel.SetActive(false);
        SceneManager.LoadScene("Main Menu", LoadSceneMode.Single);
    }
}
