using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Database;

public class GameTurnController : MonoBehaviour
{
    DatabaseReference dbRef;

    string playerId;
    string lobbyId;
    bool isPlayerTurn = false;

    void Start()
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            // Je�li nie, inicjalizuj Firebase
            FirebaseInitializer firebaseInitializer = FindObjectOfType<FirebaseInitializer>();
            if (firebaseInitializer == null)
            {
                Debug.LogError("FirebaseInitializer not found in the scene!");
                return;
            }
        }

        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId).Child("players");

        // Rozpocznij timer tury gracza
        StartCoroutine(PlayerTurnTimer());
    }

    IEnumerator PlayerTurnTimer()
    {
        while (true)
        {
            // Odczekaj 60 sekund
            yield return new WaitForSeconds(60f);

            // Zmiana tury gracza
            isPlayerTurn = !isPlayerTurn;

            // Tutaj mo�esz zaktualizowa� Firebase, aby poinformowa�, �e tura zosta�a zmieniona

            if (isPlayerTurn)
            {
                Debug.Log("Tura gracza 1");
                // Tutaj mo�esz w��czy� jakie� akcje dla gracza 1
            }
            else
            {
                Debug.Log("Tura gracza 2");
                // Tutaj mo�esz w��czy� jakie� akcje dla gracza 2
            }
        }
    }
}
