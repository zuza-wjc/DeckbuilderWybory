using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameExitListener : MonoBehaviour
{
    DatabaseReference dbRefPlayers;

    string lobbyId;
    string playerId;

    void Start()
    {
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("LobbyId is not assigned properly!");
            return;
        }

        dbRefPlayers = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");

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
                if (args.Snapshot.Key != playerId && dbRefPlayers != null)
                {
                    SceneManager.LoadScene("End Game", LoadSceneMode.Single);
                }
            }
        }
    }
}
