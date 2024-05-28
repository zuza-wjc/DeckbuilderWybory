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

        // Pobierz nazw� lobby przekazan� z poprzedniej sceny
        string lobbyName = PlayerPrefs.GetString("LobbyName");
        // Pobierz lobbyId przekazane z poprzedniej sceny
        lobbyId = PlayerPrefs.GetString("LobbyId");

        // Ustaw nazw� lobby jako tekst do wy�wietlenia
        lobbyNameText.text = lobbyName;
        lobbyCodeText.text = "Kod do gry: " + lobbyId;

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
        dbRef.ChildRemoved += HandleChildRemoved;

        // Application.quitting += OnApplicationQuit; NA CZAS TESTOWANIA APLIKACJI ZAKOMENTOWUJE TE LINIJKE
    }

    void OnApplicationQuit()
    {
        // Wywo�aj funkcj� opuszczaj�cej lobby
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

        // Sprawd� aktualn� liczb� graczy w lobby
        dbRef.GetValueAsync().ContinueWith(countTask =>
        {
            if (countTask.IsCompleted && !countTask.IsFaulted)
            {
                DataSnapshot snapshot = countTask.Result;
                if (snapshot != null)
                {
                    // Sprawd� ilo�� dzieci (graczy) w ga��zi "players"
                    if (snapshot.ChildrenCount == 1)
                    {
                        // Je�li pozosta� tylko jeden gracz, usu� ca�e lobby
                        FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId).RemoveValueAsync();
                    }
                    else
                    {
                        // Usu� tylko gracza z bazy danych na podstawie playerId
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
