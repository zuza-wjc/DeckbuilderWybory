using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameExitDatabaseChange : MonoBehaviour
{
    DatabaseReference dbRef;
    DatabaseReference dbRefPlayers;
    string lobbyId;
    string playerId;
    string playerName;
    int lobbySize;

    public Button backButton;

    void Start()
    {
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");

        // Dodaj listener do przycisku backButton
        backButton.onClick.AddListener(ToggleInGame);
    }

    void ToggleInGame()
    {
        // Aktualizacja wartoœci "inGame" w bazie danych
        dbRef.Child(playerId).Child("stats").Child("inGame").SetValueAsync(false);
        SetAllPlayersInGameToFalse();
        // wyjscie do main menu dla gracza ktory kliknal guzik
        //SceneManager.LoadScene("Main Menu", LoadSceneMode.Single);
    }

    void SetAllPlayersInGameToFalse()
    {
        //zmiana statusu inGame WSZYSTKIM graczom w sesji na false
        dbRef.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DataSnapshot snapshot = task.Result;
                foreach (DataSnapshot playerSnapshot in snapshot.Children)
                {
                    string playerKey = playerSnapshot.Key;
                    dbRef.Child(playerKey).Child("stats").Child("inGame").SetValueAsync(false);
                }
            }
        });
    }

    void OnDestroy()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(ToggleInGame);
        }
    }
}
