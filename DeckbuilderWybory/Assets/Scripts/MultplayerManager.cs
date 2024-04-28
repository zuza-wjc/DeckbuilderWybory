using UnityEngine;
using Firebase;
using Firebase.Database;

public class MultiplayerManager : MonoBehaviour
{
    DatabaseReference dbRef;

    void Start()
    {
        // Sprawdü, czy Firebase jest juø zainicjalizowany
        if (FirebaseApp.DefaultInstance == null)
        {
            // Jeúli nie, inicjalizuj Firebase
            FirebaseInitializer firebaseInitializer = FindObjectOfType<FirebaseInitializer>();
            if (firebaseInitializer == null)
            {
                Debug.LogError("FirebaseInitializer not found in the scene!");
                return;
            }
        }

        // Inicjalizacja referencji do bazy danych Firebase
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions");
    }

    public void JoinSession()
    {
        // Po klikniÍciu przycisku do≥πczania, prÛbujemy do≥πczyÊ do sesji
        string sessionId = "lobby_1"; // Zdefiniuj unikalny identyfikator sesji
        string playerId = System.Guid.NewGuid().ToString(); // Generujemy unikalny identyfikator gracza

        dbRef.Child(sessionId).Child(playerId).SetValueAsync(null).ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Failed to join session.");
                return;
            }

            dbRef.Child(sessionId).GetValueAsync().ContinueWith(snapshotTask =>
            {
                if (snapshotTask.IsFaulted || snapshotTask.IsCanceled)
                {
                    Debug.LogError("Failed to retrieve session data.");
                    return;
                }

                DataSnapshot snapshot = snapshotTask.Result;
                int playerCount = (int)snapshot.ChildrenCount; // Zlicz istniejπcych graczy w danej sesji

                string playerName = $"Gracz {playerCount + 1}"; // UtwÛrz nazwÍ gracza z numerem kolejnoúci

                dbRef.Child(sessionId).Child(playerId).Child("name").SetValueAsync(playerName).ContinueWith(nameTask =>
                {
                    if (nameTask.IsFaulted || nameTask.IsCanceled)
                    {
                        Debug.LogError("Failed to set player name.");
                        return;
                    }

                    Debug.Log("Joined session successfully! Player name: " + playerName);
                });
            });
        });
    }
}
