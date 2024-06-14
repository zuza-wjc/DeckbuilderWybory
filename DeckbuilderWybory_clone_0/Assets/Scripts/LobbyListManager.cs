using UnityEngine;
using Firebase;
using Firebase.Database;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;

public class LobbyListManager : MonoBehaviour
{
    public GameObject scrollViewContent;
    public GameObject buttonTemplate;

    DatabaseReference dbRef;

    private List<string> availableNames = new List<string>() { "Katarzyna", "Wojciech", "Jakub", "Przemysław", "Gabriela", "Barbara", "Mateusz", "Aleksandra" };
    private List<string> gracze = new List<string>();

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
                // Znajdź komponenty tekstowe w prefabrykacie przycisku
                Text[] texts = child.GetComponentsInChildren<Text>();
                Text text1 = texts[0]; // Pierwszy tekst
                Text text2 = texts[1]; // Drugi tekst

                if (text1.text == lobbyName)
                {
                    buttonExists = true;
                    text2.text = $"{playerCount}/{lobbySize}";
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

        // Znajdź komponenty tekstowe w instancji prefabrykatu
        Text[] texts = button.GetComponentsInChildren<Text>();
        Text text1 = texts[0]; // Pierwszy tekst
        Text text2 = texts[1]; // Drugi tekst

        // Ustaw wartości tekstów
        text1.text = lobbyName;
        text2.text = $"{playerCount}/{lobbySize}";

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

    public async Task AssignName(string playerId, string lobbyId)
    {
        gracze.Clear();

        var lobbyInfo = await dbRef.Child(lobbyId).GetValueAsync();
        var lobbyData = lobbyInfo.Value as Dictionary<string, object>;

        foreach (var name in lobbyData)
        {
            if (name.Key == "players")
            {
                var players = name.Value as Dictionary<string, object>;

                foreach (var player in players)
                {
                    var gracz = player.Value as Dictionary<string, object>;
                    if (gracz.ContainsKey("playerName"))
                    {
                        gracze.Add(gracz["playerName"].ToString());
                    }
                }
            }
        }

        var random = new System.Random();

        List<string> namesToAssign = new List<string>(availableNames);
        foreach (var name in gracze)
        {
            namesToAssign.Remove(name);
        }

        if (namesToAssign.Count > 0)
        {
            int index = random.Next(namesToAssign.Count);
            string playerName = namesToAssign[index];
            availableNames.Remove(playerName);

            Dictionary<string, object> playerData = new Dictionary<string, object>
            {
                { "playerName", playerName },
                { "ready", false },
                { "stats", new Dictionary<string, object> { { "inGame", false }, { "money", money }, { "support", support }, { "playerTurn", false } } }
            };

            await dbRef.Child(lobbyId).Child("players").Child(playerId).SetValueAsync(playerData);

            DataTransfer.PlayerName= playerName;
        }
        else
        {
            Debug.LogError("Brak dostępnych imion.");
        }
    }

    public async Task<string> AddPlayerAsync(string lobbyId)
    {
        string playerId = System.Guid.NewGuid().ToString();

        await AssignName(playerId, lobbyId);

        return playerId;
    }

    async Task TaskOnClick(string lobbyName, string lobbyId, int lobbySize)
    {
        string playerId = await AddPlayerAsync(lobbyId);

        // Przejście do sceny Lobby i przekazanie nazwy lobby oraz lobbyId jako parametry
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);

        //wsadzanie danych do dataTransfer
        DataTransfer.LobbyName = lobbyName;
        DataTransfer.LobbyId = lobbyId;
        DataTransfer.LobbySize = lobbySize;
        DataTransfer.PlayerId = playerId;
        DataTransfer.PlayerName = DataTransfer.PlayerName;  // PlayerPrefs.GetString("PlayerName");
    }


}
