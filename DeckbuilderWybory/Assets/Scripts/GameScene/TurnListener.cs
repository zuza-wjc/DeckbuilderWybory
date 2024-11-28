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
    DatabaseReference dbRefLobby;
    string lobbyId;

    private float timer = 30f;
    private float maxIdleTime = 30f; // Maksymalny czas oczekiwania na zmian� tury
    private bool sessionRemoved = false; // Flaga, aby unikn�� wielokrotnego usuwania

    void Start()
    {
        lobbyId = DataTransfer.LobbyId;

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");
        dbRefLobby = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);

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

                    // Je�li jakikolwiek gracz ma aktywn� tur� (playerTurn == 1), uznaj to za zmian�
                    if (playerTurn == 1)
                    {
                        turnChanged = true;
                        break;
                    }
                }
            }

            // Je�li wykryto zmian�, zresetuj timer
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

        // Sprawd�, czy czas si� sko�czy�
        if (timer <= 0 && !sessionRemoved)
        {
            sessionRemoved = true; // Ustaw flag�, aby unikn�� wielokrotnego usuwania
            Debug.LogWarning("No turn change detected for 30 seconds. Ending game and deleting lobby.");

            StartCoroutine(RemoveSessionAndEndGame());
        }
    }

    IEnumerator RemoveSessionAndEndGame()
    {
        // Usu� sesj� z op�nieniem
        yield return new WaitForSeconds(1.0f);

        if (dbRefLobby != null)
        {
            var task = dbRefLobby.RemoveValueAsync();
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.IsFaulted)
            {
                Debug.LogError("Failed to remove session: " + task.Exception);
            }
            else
            {
                Debug.Log("Session removed successfully.");
            }
        }

        // Przejd� do sceny ko�cowej
        SceneManager.LoadScene("End Game", LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        dbRef.ValueChanged -= HandlePlayerTurnChanged;
    }
}