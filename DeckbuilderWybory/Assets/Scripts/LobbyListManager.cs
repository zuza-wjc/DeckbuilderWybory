using UnityEngine;
using Firebase;
using Firebase.Database;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

using System;
using TMPro;
using System.Threading.Tasks;
using Unity.VisualScripting;



public class LobbyListManager : MonoBehaviour
{
    public GameObject scrollViewContent;
    public GameObject buttonTemplate;

    DatabaseReference dbRef;

    string playerName = "list_player";


    private List<string> availableNames = new List<string>() { "Gracz1", "Gracz2", "Gracz3", "Gracz4", "Gracz5", "Gracz6", "Gracz7", "Gracz8" };
    private List<string> gracze = new List<string>();
    private Dictionary<string, object> players = new Dictionary<string, object>() { { "playerName", null }, { "ready", false } };



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
        button.GetComponent<Button>().onClick.AddListener(delegate { TaskOnClick(lobbyName, lobbyId, playerName ); });
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


    public async void AssignName(string playerId, string lobbyId)
    {
        Dictionary<string, object> lobbyData;
        gracze.Clear();

        // odczyt z lobbyInfo z bazy
        var lobbyInfo = await dbRef.Child(lobbyId).GetValueAsync();
        lobbyData = lobbyInfo.Value as Dictionary<string, object>;

        foreach (var name in lobbyData)
        {
            if (name.Key == "players")
            {
                Debug.Log(" gdy Key = players to name.Key = " + name.Key + " a name.Value = " + name.Value);

                players = name.Value as Dictionary<string, object>;

                foreach (var player in players)
                {
                    Debug.Log(" player Key = " + player.Key + "a player.Value = " + player.Value);
                    var gracz = player.Value as Dictionary<string, object>;


                    foreach (var gra in gracz )
                    {
                        var a = gra.Key;
                        if (a == "playerName")
                        {
                            var b = gra.Value;
                            Debug.Log("playerKey = " + a + "playerName = " + b);
                            gracze.Add((string)b);

                        }

                    };


                };
            };
        };
        // koniec odczytu z bazydanych

        var random = new System.Random();
        if (players.ContainsKey(playerId))
        {
            string te = "gracza już ma przypisane imię";
            return;
        }

        List<string> namesToAssign = availableNames;
        foreach (var name in gracze)
        {
            namesToAssign.Remove(name); // Usuń imiona, które są już używane.
        }

        if (namesToAssign.Count > 0)
        {
            int index = random.Next(namesToAssign.Count);
            playerName = namesToAssign[index];
            availableNames.Remove(namesToAssign[index]); // Usuń z puli dostępnych, aby nie było duplikatów.



            Dictionary<string, object> playerData = new Dictionary<string, object>
        {
            { "playerName", playerName },
            { "ready", false }
        };

            await dbRef.Child(lobbyId).Child("players").Child(playerId).SetValueAsync(playerData);
        }
        else
        {
            Debug.LogError("Brak dostępnych imion.");
        }

        PlayerPrefs.SetString("PlayerName", playerName);
    }


    public async Task<string> AddPlayerAsync(string lobbyId, string playerName)
    {
        // Wygeneruj unikalny identyfikator gracza
        string playerId = System.Guid.NewGuid().ToString();
        Debug.Log("wygenerowany playerID = " + playerId);

        // wylosuj unikalną nazwę gracza
        AssignName(playerId, lobbyId);


        // Przygotuj dane gracza jako słownik
        //Dictionary<string, object> playerData = new Dictionary<string, object>
        //{
        //    { "playerName", playerName },
        //    { "ready", false }
        //};



        // Dodaj nowego gracza do bazy danych Firebase - przeniesione do Assing Name
        //dbRef.Child(lobbyId).Child("players").Child(playerId).SetValueAsync(playerData);

        return playerId;
    }

    async Task TaskOnClick(string lobbyName, string lobbyId, string playerName)
    {
        string playerId = await AddPlayerAsync(lobbyId, playerName);
        Debug.Log("powrót z AddPlayer z sukcesem :), lobbyId = " + lobbyId + " playerName = " + playerName + " playerId = " + playerId);

        // Przejście do sceny Lobby i przekazanie nazwy lobby oraz lobbyId jako parametry
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        PlayerPrefs.SetString("LobbyName", lobbyName);
        PlayerPrefs.SetString("LobbyId", lobbyId);
        PlayerPrefs.SetString("PlayerId", playerId);
        //PlayerPrefs.SetString("PlayerName", playerName);
    }
}
