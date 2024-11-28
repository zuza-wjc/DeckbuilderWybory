using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Linq;

public class InformationManager : MonoBehaviour
{
    public GameObject gameFinished;
    public GameObject gameNotFinished;

    private DatabaseReference dbRef;
    private string lobbyId;
    private string playerId;

    void Start()
    {
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);

        // Asynchronicznie pobieramy dane o rundach
        GetRounds();
    }

    // Funkcja do pobrania liczby rund z bazy danych Firebase
    void GetRounds()
    {
        dbRef.Child("rounds").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error getting rounds from Firebase: " + task.Exception);
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    int rounds = int.TryParse(snapshot.Value.ToString(), out rounds) ? rounds : 0;

                    // Jeœli liczba rund jest wiêksza ni¿ 0, gra nie jest zakoñczona
                    if (rounds <= 0)
                    {
                        gameFinished.SetActive(true);
                    }
                    else
                    {
                        gameNotFinished.SetActive(true);
                    }
                }
                else
                {
                    Debug.LogWarning("Rounds data not found in Firebase.");
                    gameNotFinished.SetActive(true);
                }
            }
        });
    }
}
