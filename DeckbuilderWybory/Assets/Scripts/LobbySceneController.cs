using Firebase;
using Firebase.Database;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LobbySceneController : MonoBehaviour
{
    public Text lobbyNameText;
    public Text lobbyCodeText;

    public GameObject scrollViewContent;
    public GameObject textTemplate;

    DatabaseReference dbRef;
    string lobbyId;


    void Start()
    {

        // Pobierz nazwê lobby przekazan¹ z poprzedniej sceny
        string lobbyName = PlayerPrefs.GetString("LobbyName");
        // Pobierz lobbyId przekazane z poprzedniej sceny
        lobbyId = PlayerPrefs.GetString("LobbyId");

        // Ustaw nazwê lobby jako tekst do wyœwietlenia
        lobbyNameText.text = lobbyName;
        lobbyCodeText.text = "Kod do gry: " + lobbyId;

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
        dbRef.ChildRemoved += HandleChildRemoved;

        // Application.quitting += OnApplicationQuit; NA CZAS TESTOWANIA APLIKACJI ZAKOMENTOWUJE TE LINIJKE
    }

    void OnApplicationQuit()
    {
        // Wywo³aj funkcjê opuszczaj¹cej lobby
        // LeaveLobby();  NA CZAS TESTOWANIA APLIKACJI ZAKOMENTOWUJE TE LINIJKE
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

    void HandleChildRemoved(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        string playerName = args.Snapshot.GetValue(true).ToString();
        RemoveText(playerName);
    }

    void CreateText(string playerName)
    {
        GameObject text = Instantiate(textTemplate, scrollViewContent.transform);
        text.SetActive(true);
        text.GetComponentInChildren<Text>().text = playerName;
    }

    void RemoveText(string playerName)
    {
        foreach (Transform child in scrollViewContent.transform)
        {
            if (child.GetComponentInChildren<Text>().text == playerName)
            {
                Destroy(child.gameObject);
                return;
            }
        }
    }
    public void LeaveLobby()
    {
        string playerId = PlayerPrefs.GetString("PlayerId");

        // SprawdŸ aktualn¹ liczbê graczy w lobby
        dbRef.GetValueAsync().ContinueWith(countTask =>
        {
            if (countTask.IsCompleted && !countTask.IsFaulted)
            {
                DataSnapshot snapshot = countTask.Result;
                if (snapshot != null)
                {
                    // SprawdŸ iloœæ dzieci (graczy) w ga³êzi "players"
                    if (snapshot.ChildrenCount == 1)
                    {
                        // Jeœli pozosta³ tylko jeden gracz, usuñ ca³e lobby
                        FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId).RemoveValueAsync();
                    }
                    else
                    {
                        // Usuñ tylko gracza z bazy danych na podstawie playerId
                        dbRef.Child(playerId).RemoveValueAsync();
                    }
                }
                else
                {
                    Debug.LogError("Failed to get lobby player count: snapshot is null");
                }
            }
            else
            {
                Debug.LogError("Failed to get lobby player count: " + countTask.Exception);
            }
        });
    }


}
