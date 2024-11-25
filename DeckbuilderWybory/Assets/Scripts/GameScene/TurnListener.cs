using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.SceneManagement;

public class TurnListener : MonoBehaviour
{
    DatabaseReference dbRef;
    string lobbyId;

    private float timer = 30f;
    private float maxIdleTime = 30f; // maksymalny czas oczekiwania na zmianê tury

    void Start()
    {
        lobbyId = DataTransfer.LobbyId;

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");

        StartListening();
    }

    void StartListening()
    {
        dbRef.ValueChanged += HandlePlayerTurnChanged;
    }

    void HandlePlayerTurnChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.Snapshot.Exists)
        {
            bool turnChanged = false;

            foreach (DataSnapshot playerSnapshot in args.Snapshot.Children)
            {
                // Sprawdzaj istnienie pola "playerTurn"
                if (playerSnapshot.Child("stats").Child("playerTurn").Exists)
                {
                    int playerTurn = int.Parse(playerSnapshot.Child("stats").Child("playerTurn").Value.ToString());

                    // Jeœli jakikolwiek gracz ma aktywn¹ turê (playerTurn == 1), uznaj to za zmianê
                    if (playerTurn == 1)
                    {
                        turnChanged = true;
                        break;
                    }
                }
            }

            // Jeœli wykryto zmianê, zresetuj timer
            if (turnChanged)
            {
                Debug.Log("Turn detected. Resetting timer.");
                timer = maxIdleTime;
            }
        }
    }

    void Update()
    {
        // Odliczaj czas
        timer -= Time.deltaTime;

        // SprawdŸ, czy czas siê skoñczy³
        if (timer <= 0)
        {
            Debug.LogWarning("No turn change detected for 30 seconds. Ending game.");
            SceneManager.LoadScene("End Game", LoadSceneMode.Single);
        }
    }

    void OnDestroy()
    {
        dbRef.ValueChanged -= HandlePlayerTurnChanged;
    }
}