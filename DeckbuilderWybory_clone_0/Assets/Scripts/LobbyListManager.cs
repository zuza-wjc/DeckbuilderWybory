using UnityEngine;
using Firebase;
using Firebase.Database;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
// linia dodana
using System.Collections.Generic;

using System.Linq;
using System;

using TMPro;
using System.Threading.Tasks;
using Unity.VisualScripting;

public class LobbyListManager : MonoBehaviour
{
    public GameObject scrollViewContent;
    public GameObject buttonTemplate;

    DatabaseReference dbRef;

    private List<string> availableNames = new List<string>() { "Gracz1", "Gracz2", "Gracz3", "Gracz4", "Gracz5", "Gracz6", "Gracz7", "Gracz8" };

    private List<string> gracze = new List<string>();

    private Dictionary<string, string> players = new Dictionary<string, string>();

    public async void AssignName(string playerId, string lobbyId)
    {
        Dictionary<string, object> lobbyData;

        Debug.Log("jestem w AssignName" + "lobbyId = " + lobbyId + "playerId = " + playerId);

        gracze.Clear();

        // odczyt availableNames z bazydanych

        var lobbyInfo = await dbRef.Child(lobbyId).GetValueAsync();
        lobbyData = lobbyInfo.Value as Dictionary<string, object>;

        Debug.Log("dotychczasowi gracze");
        foreach (var name in lobbyData)
        {
            if (name.Key == "players")
            {
                var players = name.Value as Dictionary<string, object>;
                foreach (var player in players)
                {
                    Debug.Log(player.Value);
                    gracze.Add((string)player.Value);
                }
            }
        }
        // koniec odczytu z bazydanych

        var random = new System.Random();
        if (players.ContainsKey(playerId))
        {
            string te = "gracza już ma przypisane imię";
            return; // Gracz już ma przypisane imię.

        }

        List<string> namesToAssign = availableNames;
        //foreach (var i in namesToAssign) { Debug.Log(i); }
        foreach (var name in gracze)
        {
          namesToAssign.Remove(name); // Usuń imiona, które są już używane.
        }

        Debug.Log("nazwy pozostałe do przydzielenia:");
        foreach (var i in namesToAssign) { Debug.Log(i); }

        Debug.Log("pozostałe tyle imion: " + namesToAssign.Count);

        if (namesToAssign.Count > 0)
        {
                int index = random.Next(namesToAssign.Count);
                var playerName = namesToAssign[index];
                //players[playerId] = namesToAssign[index];
                Debug.Log("nazwa dodanego wylosowanego Gracza = " + playerName);
                availableNames.Remove(namesToAssign[index]); // Usuń z puli dostępnych, aby nie było duplikatów.
                Debug.Log("wielkość puli zasobów" + availableNames.Count);
                await dbRef.Child(lobbyId).Child("players").Child(playerId).SetValueAsync(playerName);

        }
        else
        {
             Debug.LogError("Brak dostępnych imion.");
        }
        Debug.Log("wychodzę :)");

        //return playerName;

    }

    public void ReleaseName(string playerId)
    {
        if (players.TryGetValue(playerId, out string name))
        {
            availableNames.Add(name); // Dodaj imię z powrotem do puli dostępnych.
            players.Remove(playerId);
        }
    }


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


        // Assume dbRef is already initialized and points to the Firebase database

    }

    void HandleChildChanged(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        string lobbyId = args.Snapshot.Key;
        Debug.Log("lobbyId z HCChanged = " + lobbyId);
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
            // dodane

            //List<string> availableNames = (List<string>)args.Snapshot.Child("availableNames").GetValue(true);
            //private Dictionary<string, string> players = (string)args.Snapshot.Child("players").GetValue(true);

            // Sprawdź, czy liczba graczy jest mniejsza od rozmiaru lobby
            if (playerCount < lobbySize)
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
        button.GetComponent<Button>().onClick.AddListener(delegate { TaskOnClickAsync(lobbyName, lobbyId); });
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

    public async Task<string> AddPlayerAsync(string lobbyId)
    {

        Debug.Log(" jestem w AddPlayers");
        // Wygeneruj unikalny identyfikator gracza
        string playerId = System.Guid.NewGuid().ToString();
        Debug.Log("dotarłam do AddPlayer i nadałam unikalne playerId = " + playerId);

        AssignName(playerId, lobbyId);

        Debug.Log("wyszłam z AssigneName i playerId jest to samo ?= " + playerId );

        // Dodaj nowego gracza do bazy danych Firebase
        return playerId;
    }

    async Task TaskOnClickAsync(string lobbyName, string lobbyId )
    {
        //AssignName(playerId);


        string playerId = await AddPlayerAsync(lobbyId);
        //string playerId = await AddPlayerAsync(playerName, lobbyId);

        Debug.Log("wróciłem z AddPlayer :)");

        // Przejście do sceny Lobby i przekazanie nazwy lobby oraz lobbyId jako parametry
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        PlayerPrefs.SetString("LobbyName", lobbyName);
        PlayerPrefs.SetString("LobbyId", lobbyId);
        PlayerPrefs.SetString("PlayerId", playerId);

    }
}
