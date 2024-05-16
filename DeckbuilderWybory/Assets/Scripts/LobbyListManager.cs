using UnityEngine;
using Firebase;
using Firebase.Database;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyListManager : MonoBehaviour
{
    public GameObject scrollViewContent;
    public GameObject buttonTemplate;

    DatabaseReference dbRef;


    void Start()
    {

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
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions");

        // Nas³uchiwanie zmian w strukturze bazy danych (dodanie/usuniêcie ga³êzi)
        dbRef.ChildAdded += HandleChildAdded;
        dbRef.ChildRemoved += HandleChildRemoved;
    }

    void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        bool isPublic = bool.Parse(args.Snapshot.Child("isPublic").GetValue(true).ToString());
        if (isPublic)
        {
            string lobbyName = args.Snapshot.Child("lobbyName").GetValue(true).ToString();
            string lobbyId = args.Snapshot.Key;
            CreateButton(lobbyName, lobbyId);
        }
    }


    void HandleChildRemoved(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        string lobbyName = args.Snapshot.Child("lobbyName").GetValue(true).ToString();
        DestroyButton(lobbyName);
    }

    void CreateButton(string lobbyName, string lobbyId)
    {
        GameObject button = Instantiate(buttonTemplate, scrollViewContent.transform);
        button.SetActive(true);
        button.GetComponentInChildren<UnityEngine.UI.Text>().text = lobbyName;

        // Dodanie funkcji obs³ugi zdarzenia dla klikniêcia w przycisk
        button.GetComponent<Button>().onClick.AddListener(delegate { TaskOnClick(lobbyName, lobbyId); });
    }

    void DestroyButton(string lobbyName)
    {
        foreach (Transform child in scrollViewContent.transform)
        {
            if (child.GetComponentInChildren<UnityEngine.UI.Text>().text == lobbyName)
            {
                Destroy(child.gameObject);
                return;
            }
        }
    }

    public string AddPlayer(string playerName, string lobbyId)
    {
        
            // Wygeneruj unikalny identyfikator gracza
            string playerId = System.Guid.NewGuid().ToString();

            // Dodaj nowego gracza do bazy danych Firebase
            dbRef.Child(lobbyId).Child("players").Child(playerId).SetValueAsync(playerName);

            return playerId;

    }

    void TaskOnClick(string lobbyName, string lobbyId)
    {
        string playerId = AddPlayer("some_Gracz", lobbyId);

        // Przejœcie do sceny Lobby i przekazanie nazwy lobby oraz lobbyId jako parametry
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        PlayerPrefs.SetString("LobbyName", lobbyName);
        PlayerPrefs.SetString("LobbyId", lobbyId);
        PlayerPrefs.SetString("PlayerId", playerId);
    }

}