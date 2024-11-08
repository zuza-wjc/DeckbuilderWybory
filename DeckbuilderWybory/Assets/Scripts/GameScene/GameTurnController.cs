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
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;
        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");

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

            // Tutaj mo¿esz zaktualizowaæ Firebase, aby poinformowaæ, ¿e tura zosta³a zmieniona

            if (isPlayerTurn)
            {
                Debug.Log("Tura gracza 1");
                // Tutaj mo¿esz w³¹czyæ jakieœ akcje dla gracza 1
            }
            else
            {
                Debug.Log("Tura gracza 2");
                // Tutaj mo¿esz w³¹czyæ jakieœ akcje dla gracza 2
            }
        }
    }
}
