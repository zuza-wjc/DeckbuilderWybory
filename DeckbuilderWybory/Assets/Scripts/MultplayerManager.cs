using UnityEngine;
using Firebase;
using Firebase.Database;
using UnityEngine.SceneManagement;

public class MultiplayerManager : MonoBehaviour
{
    DatabaseReference sessionRef;

    void Start()
    {
        // Inicjalizacja Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                Debug.Log("Firebase is ready to use!");
                InitializeFirebase();
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }

    void InitializeFirebase()
    {
        // Inicjalizacja referencji do bazy danych Firebase
        sessionRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions");
    }

    public void JoinSession()
    {
        // Po klikniêciu przycisku do³¹czania, próbujemy do³¹czyæ do sesji
        string sessionId = "lobby_1"; // Zdefiniuj unikalny identyfikator sesji
        string playerId = System.Guid.NewGuid().ToString(); // Generujemy unikalny identyfikator gracza
        Color playerColor = GetRandomColor(); // Generujemy losowy kolor dla gracza

        sessionRef.Child(sessionId).Child(playerId).SetValueAsync(playerColor.ToString()).ContinueWith(task => {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Failed to join session.");
                return;
            }
            Debug.Log("Joined session successfully!");
            changeScene();
        });
    }

    Color GetRandomColor()
    {
        return new Color(Random.value, Random.value, Random.value);
    }

    private void changeScene()
    {
        SceneManager.LoadSceneAsync("Lobby");
    }
}
