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


    void Start()
    {
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions");

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
        if (scrollViewContent == null || buttonTemplate == null)
        {
            Debug.LogWarning("scrollViewContent or buttonTemplate is null, unable to create button.");
            return;
        }

        GameObject button = Instantiate(buttonTemplate, scrollViewContent.transform);
        if (button != null)
        {
            button.SetActive(true);

            // Znajdź komponenty tekstowe w instancji prefabrykatu
            Text[] texts = button.GetComponentsInChildren<Text>();
            Text text1 = texts[0]; // Pierwszy tekst
            Text text2 = texts[1]; // Drugi tekst

            // Ustaw wartości tekstów
            text1.text = lobbyName;
            text2.text = $"{playerCount}/{lobbySize}";

            // Dodanie funkcji obsługi zdarzenia dla kliknięcia w przycisk
            button.GetComponent<Button>().onClick.AddListener(delegate { _ = TaskOnClick(lobbyName, lobbyId, lobbySize); });
        }
        else
        {
            Debug.LogWarning("Button was not created because prefab instantiation failed.");
        }
    }


    void DestroyButton(string lobbyName)
    {
        if (scrollViewContent == null)
        {
            Debug.LogWarning("scrollViewContent is null, unable to destroy button.");
            return;
        }

        foreach (Transform child in scrollViewContent.transform)
        {
            if (child != null)
            {
                Text textComponent = child.GetComponentInChildren<Text>();

                if (textComponent != null && textComponent.text.Contains(lobbyName))
                {
                    if (child.gameObject != null)
                    {
                        Destroy(child.gameObject);
                        return;
                    }
                }
            }
        }
    }


    public async Task AssignName(string playerId, string lobbyId)
    {
        try
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
                    { "stats", new Dictionary<string, object>
                        {
                            { "inGame", false },
                            { "money", 0 },
                            { "income", 10 },
                            { "support", new int[6] { 5, 5, 5, 5, 5, 5 } }, 
                            { "playerTurn", false }
                        }
                    }
                };

                await dbRef.Child(lobbyId).Child("players").Child(playerId).SetValueAsync(playerData);

                DataTransfer.PlayerName = playerName;
            }
            else
            {
                Debug.LogError("Brak dostępnych imion.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error assigning player name: {e.Message}");
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
    }

    void OnDestroy()
    {
        dbRef.ChildAdded -= HandleChildAdded;
        dbRef.ChildRemoved -= HandleChildRemoved;
        dbRef.ChildChanged -= HandleChildChanged;
    }


}
