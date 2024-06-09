using UnityEngine;
using Firebase;
using Firebase.Database;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class LobbyListManager : MonoBehaviour
{
    public GameObject scrollViewContent;
    public GameObject buttonTemplate;

    DatabaseReference dbRef;

    string playerName = "list_player";
    int money = 0;
    int support = 0;

    void Start()
    {
        // Sprawdź, czy Firebase jest już zainicjalizowany
        if (FirebaseApp.DefaultInstance == null)
        {
            // Jeśli nie, inicjalizuj Firebase
            FirebaseInitializer firebaseInitializer = FindObjectOfType<FirebaseInitializer>();
            if (firebaseInitializer == null)
            {
                Debug.LogError("FirebaseInitializer not found in the scene!");
                return;
            }
        }

        // Inicjalizacja referencji do bazy danych Firebase
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions");

        // Nasłuchiwanie zmian w strukturze bazy danych (dodanie/usunięcie gałęzi)
        dbRef.ChildAdded += HandleChildAdded;
        dbRef.ChildRemoved += HandleChildRemoved;
        dbRef.ChildChanged += HandleChildChanged;
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
            int lobbySize = int.Parse(args.Snapshot.Child("lobbySize").GetValue(true).ToString());
            int playerCount = (int)args.Snapshot.Child("players").ChildrenCount;

            // Sprawdź, czy liczba graczy jest mniejsza od rozmiaru lobby
            if (playerCount < lobbySize)
            {
                CreateButton(lobbyName, lobbyId, playerCount, lobbySize);
            }
        }
    }

    void HandleChildChanged(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        string lobbyId = args.Snapshot.Key;
        int lobbySize = int.Parse(args.Snapshot.Child("lobbySize").GetValue(true).ToString());
        int playerCount = (int)args.Snapshot.Child("players").ChildrenCount;

        // Sprawdź, czy liczba graczy jest mniejsza od rozmiaru lobby
        if (playerCount >= lobbySize)
        {
            string lobbyName = args.Snapshot.Child("lobbyName").GetValue(true).ToString();
            DestroyButton(lobbyName);
        }
        else
        {
            // Optional: handle case where players leave and lobby is no longer full
            string lobbyName = args.Snapshot.Child("lobbyName").GetValue(true).ToString();
            bool buttonExists = false;
            foreach (Transform child in scrollViewContent.transform)
            {
                if (child.GetComponentInChildren<UnityEngine.UI.Text>().text.Contains(lobbyName))
                {
                    buttonExists = true;
                    child.GetComponentInChildren<UnityEngine.UI.Text>().text = $"{lobbyName} {playerCount}/{lobbySize}";
                    break;
                }
            }
            if (!buttonExists)
            {
                CreateButton(lobbyName, lobbyId, playerCount, lobbySize);
            }
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

    void CreateButton(string lobbyName, string lobbyId, int playerCount, int lobbySize)
    {
        GameObject button = Instantiate(buttonTemplate, scrollViewContent.transform);
        button.SetActive(true);
        button.GetComponentInChildren<UnityEngine.UI.Text>().text = $"{lobbyName} {playerCount}/{lobbySize}";

        // Dodanie funkcji obsługi zdarzenia dla kliknięcia w przycisk
        button.GetComponent<Button>().onClick.AddListener(delegate { TaskOnClick(lobbyName, lobbyId, lobbySize); });
    }

    void DestroyButton(string lobbyName)
    {
        foreach (Transform child in scrollViewContent.transform)
        {
            if (child.GetComponentInChildren<UnityEngine.UI.Text>().text.Contains(lobbyName))
            {
                Destroy(child.gameObject);
                return;
            }
        }
    }

    public string AddPlayer(string lobbyId)
    {
        // Wygeneruj unikalny identyfikator gracza
        string playerId = System.Guid.NewGuid().ToString();

        // Przygotuj dane gracza jako słownik
        Dictionary<string, object> playerData = new Dictionary<string, object>
        {
            { "playerName", playerName },
            { "ready", false },
            { "stats", new Dictionary<string, object> { { "inGame", false }, { "money", money }, { "support", support }, { "playerTurn", false } }  }
        };

        // Dodaj nowego gracza do bazy danych Firebase
        dbRef.Child(lobbyId).Child("players").Child(playerId).SetValueAsync(playerData);

        return playerId;
    }

    void TaskOnClick(string lobbyName, string lobbyId, int lobbySize)
    {
        string playerId = AddPlayer(lobbyId);

        // Przejście do sceny Lobby i przekazanie nazwy lobby oraz lobbyId jako parametry
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        PlayerPrefs.SetString("LobbyName", lobbyName);
        PlayerPrefs.SetString("LobbyId", lobbyId);
        PlayerPrefs.SetInt("LobbySize", lobbySize);
        DataTransfer.PlayerId = playerId;
        PlayerPrefs.SetString("PlayerName", playerName);
    }
}
