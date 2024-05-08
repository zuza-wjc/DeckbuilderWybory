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
        // Pobierz nazw� lobby przekazan� z poprzedniej sceny
        string lobbyName = PlayerPrefs.GetString("LobbyName");
        // Pobierz lobbyId przekazane z poprzedniej sceny
        string lobbyId = PlayerPrefs.GetString("LobbyId");

        // Ustaw nazw� lobby jako tekst do wy�wietlenia
        lobbyNameText.text = lobbyName;

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
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId).Child("players");

        // Ustaw nas�uchiwanie zmian w strukturze bazy danych (dodanie/usuni�cie ga��zi)
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
