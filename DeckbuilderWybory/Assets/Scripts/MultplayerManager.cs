using UnityEngine;
using Firebase;
using Firebase.Database;

public class MultiplayerManager : MonoBehaviour
{
    DatabaseReference dbRef;

    void Start()
    {
        // Sprawd�, czy Firebase jest ju� zainicjalizowany
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

        // Inicjalizacja referencji do bazy danych Firebase
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions");
    }

    public void JoinSession()
    {
        // Po klikni�ciu przycisku do��czania, pr�bujemy do��czy� do sesji
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
                int playerCount = (int)snapshot.ChildrenCount; // Zlicz istniej�cych graczy w danej sesji

                string playerName = $"Gracz {playerCount + 1}"; // Utw�rz nazw� gracza z numerem kolejno�ci

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
