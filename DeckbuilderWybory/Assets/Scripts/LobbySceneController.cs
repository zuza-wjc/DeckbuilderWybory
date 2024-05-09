using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;

public class LobbySceneController : MonoBehaviour
{
    public Text lobbyNameText;

    public GameObject scrollViewContent;
    public GameObject textTemplate;

    DatabaseReference dbRef;

    void Start()
    {
        // Pobierz nazwê lobby przekazan¹ z poprzedniej sceny
        string lobbyName = PlayerPrefs.GetString("LobbyName");
        // Pobierz lobbyId przekazane z poprzedniej sceny
        string lobbyId = PlayerPrefs.GetString("LobbyId");

        // Ustaw nazwê lobby jako tekst do wyœwietlenia
        lobbyNameText.text = lobbyName;

        // SprawdŸ, czy Firebase jest ju¿ zainicjalizowany
        if (FirebaseApp.DefaultInstance == null)
        {
            // Jeœli nie, inicjalizuj Firebase
            FirebaseInitializer firebaseInitializer = FindObjectOfType<FirebaseInitializer>();
            if (firebaseInitializer == null)
            {
                Debug.LogError("FirebaseInitializer not found in the scene!");
                return;
            }
        }

        // Inicjalizacja referencji do bazy danych Firebase
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId).Child("players");

        // Ustaw nas³uchiwanie zmian w strukturze bazy danych (dodanie/usuniêcie ga³êzi)
        dbRef.ChildAdded += HandleChildAdded;
    }

    void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        string playerName = args.Snapshot.GetValue(true).ToString();
        CreateText(playerName);
    }

    void CreateText(string playerName)
    {
        GameObject text = Instantiate(textTemplate, scrollViewContent.transform);
        text.SetActive(true);
        text.GetComponentInChildren<Text>().text = playerName;
    }
}
