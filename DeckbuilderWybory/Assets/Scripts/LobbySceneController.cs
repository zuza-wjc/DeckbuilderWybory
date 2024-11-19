using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Extensions;

public class LobbySceneController : MonoBehaviour
{
    public Text lobbyNameText;
    public Text lobbyCodeText;
    public GameObject scrollViewContent;
    public GameObject textTemplate;

    public Button readyButton;  // Przycisk gotowosci
    public Sprite selected;
    public Sprite notSelected;
    private Image image;

    public Button copyButton; //przycisk do kopiowania kodu
    public Text playerCountsText; // Tekst do wyswietlania liczby graczy

    DatabaseReference dbRef;
    DatabaseReference dbRefLobby;
    string lobbyId;
    string playerId;
    string playerName;
    int lobbySize;

    bool readyState = false;
    private int readyPlayersCount = 0;
    int isStarted = 0;

    void Start()
    {
        string lobbyName = DataTransfer.LobbyName;
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;
        playerName = DataTransfer.PlayerName;

        lobbyNameText.text = lobbyName;
        lobbyCodeText.text = "KOD: " + lobbyId;

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");
        dbRefLobby = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);

        CountReadyPlayers();
        getLobbySizeFromDatabase();

        dbRefLobby.Child("isStarted").ValueChanged += HandleIsStartedChanged;
        dbRef.ChildAdded += HandleChildAdded;
        dbRef.ChildRemoved += HandleChildRemoved;
        dbRef.ChildChanged += HandleChildChanged;

        image = readyButton.GetComponent<Image>();
        readyButton.onClick.AddListener(ToggleReady);
        copyButton.onClick.AddListener(CopyFromClipboard);
    }

    void CountReadyPlayers()
    {
        dbRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DataSnapshot snapshot = task.Result;
                readyPlayersCount = 0;

                foreach (var child in snapshot.Children)
                {
                    if (child.Child("ready").Exists && (bool)child.Child("ready").Value)
                    {
                        readyPlayersCount += 1;

                        if (child.Child("playerName").Exists && child.Child("playerName").Value != null)
                        {
                            string readyPlayerName = child.Child("playerName").Value.ToString();
                            UpdateText(readyPlayerName, true);
                        }
                        else
                        {
                            Debug.LogWarning("playerName key not found or null in child data.");
                        }
                    }
                }

                Debug.Log("Liczba gotowych graczy: " + readyPlayersCount);
            }
            else
            {
                Debug.LogError("Nie uda³o siê pobraæ danych graczy: " + task.Exception);
            }
        });
    }

    void getLobbySizeFromDatabase()
    {
        dbRefLobby.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    if (snapshot.Child("lobbySize").Exists)
                    {
                        lobbySize = int.Parse(snapshot.Child("lobbySize").Value.ToString());
                        UpdatePlayerCountsText();
                    }
                    else
                    {
                        Debug.LogWarning("lobbySize key not found in database.");
                    }
                }
                else
                {
                    Debug.LogWarning("Snapshot for lobby not found.");
                }
            }
            else
            {
                Debug.LogError("Failed to fetch lobby data: " + task.Exception);
            }
        });
    }


    void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        Debug.Log("ADDED");
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        string playerName = args.Snapshot.Child("playerName").Value.ToString();
        CreateText(playerName, false);
    }

    void HandleChildRemoved(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if(readyState)
        {
            readyPlayersCount -= 1;
            UpdatePlayerCountsText();
        }

        string playerName = args.Snapshot.Child("playerName").Value.ToString();
        RemoveText(playerName);
    }

    void HandleChildChanged(object sender, ChildChangedEventArgs args)
    {
        Debug.Log("CHANGED");
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        foreach (var child in args.Snapshot.Children)
        {
            if (child.Key == "ready")
            {
                string playerChanged = args.Snapshot.Key;

                if (playerId != playerChanged)
                {
                    bool isReady = (bool)child.Value;
                    Debug.Log("Player " + playerChanged + " ready status changed to: " + isReady);

                    if (isReady)
                    {
                        readyPlayersCount += 1;
                    }
                    else
                    {
                        readyPlayersCount -= 1;
                    }

                    string playerNameChange = args.Snapshot.Child("playerName").Value.ToString();

                    UpdatePlayerCountsText();
                    UpdateText(playerNameChange, isReady);
                }
            }
        }
        StartingGame(lobbyId);
    }

    void CreateText(string playerName, bool readyStatus)
    {
        // Sprawdzanie, czy obiekt z t¹ sam¹ nazw¹ gracza ju¿ istnieje
        foreach (Transform child in scrollViewContent.transform)
        {
            // Sprawdzamy, czy tekst obiektu zawiera nazwê gracza
            Text childText = child.GetComponentInChildren<Text>();
            if (childText != null && childText.text.Contains(playerName))
            {
                // Jeœli obiekt z t¹ nazw¹ gracza ju¿ istnieje, nie tworzymy nowego
                Debug.Log("Obiekt z t¹ nazw¹ gracza ju¿ istnieje: " + playerName);
                return;
            }
        }

        // Jeœli obiekt z t¹ nazw¹ nie istnieje, tworzymy nowy
        string playerInfo = readyStatus ? playerName + "    GOTOWY" : playerName + "    NIEGOTOWY";
        GameObject text = Instantiate(textTemplate, scrollViewContent.transform);
        if (text != null)
        {
            text.SetActive(true);
            text.GetComponentInChildren<Text>().text = playerInfo;
        }
    }

    void UpdateText(string playerName, bool readyStatus)
    {
        Text[] texts = scrollViewContent.GetComponentsInChildren<Text>();

        foreach (Text text in texts)
        {
            if (text != null && text.text.Contains(playerName))
            {
                text.text = readyStatus ? playerName + "    GOTOWY" : playerName + "    NIEGOTOWY";
                return;
            }
        }

        CreateText(playerName, readyStatus);
    }

    void RemoveText(string playerName)
    {
        foreach (Transform child in scrollViewContent.transform)
        {
            if (child != null)
            {
                Text textComponent = child.GetComponentInChildren<Text>();
                if (textComponent != null && textComponent.text.Contains(playerName))
                {
                    Destroy(child.gameObject);
                    return;
                }
            }
        }
    }

    void CopyFromClipboard (){
        GUIUtility.systemCopyBuffer=lobbyId;
    }

    void ToggleReady()
    {
        readyState = !readyState;

        if (readyState)
        {
            readyPlayersCount += 1;
        }
        else
        {
            readyPlayersCount -= 1;
        }
        UpdatePlayerCountsText();
        UpdateText(playerName, readyState);
        UpdateReadyButton();

        dbRef.Child(playerId).Child("ready").SetValueAsync(readyState);
    }

    void UpdateReadyButton()
    {
        if(image.sprite == selected)
        {
            image.sprite = notSelected;
        }
        else
        {
            image.sprite = selected;
        }
    }

    void UpdatePlayerCountsText()
    {
        playerCountsText.text = "GOTOWI GRACZE: " + readyPlayersCount + " / " + lobbySize;
    }

    public void LeaveLobby()
    {
        dbRef.GetValueAsync().ContinueWith(countTask =>
        {
            if (countTask.IsCompleted && !countTask.IsFaulted)
            {
                DataSnapshot snapshot = countTask.Result;
                if (snapshot != null)
                {
                    if (snapshot.ChildrenCount == 1)
                    {
                        dbRefLobby.RemoveValueAsync();
                    }
                    else
                    {
                        dbRef.Child(playerId).RemoveValueAsync();
                    }
                }
                else
                {
                    Debug.Log("Failed to get lobby player count: snapshot is null");
                }
            }
            else
            {
                Debug.Log("Failed to get lobby player count: " + countTask.Exception);
            }
        });
    }

    void OnApplicationQuit()
    {
       LeaveLobby();
    }

    void HandleIsStartedChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.Log(args.DatabaseError.Message);
            return;
        }

        if (args.Snapshot.Exists)
        {
            isStarted = int.Parse(args.Snapshot.Value.ToString());
            if (isStarted == 1)
            {
                dbRef.Child(playerId).Child("stats").Child("inGame").SetValueAsync(true);

                dbRefLobby.Child("map").GetValueAsync().ContinueWith(mapTask =>
                {
                    if (mapTask.IsCompleted && !mapTask.IsFaulted)
                    {
                        DataSnapshot mapSnapshot = mapTask.Result;
                        if (mapSnapshot.Exists)
                        {
                            Debug.Log("Map data already exists in the database.");
                        }
                        else
                        {
                            CheckAndSetMapData();
                        }
                    }
                    else
                    {
                        Debug.Log("Failed to fetch map data: " + mapTask.Exception);
                    }
                });
                SceneManager.LoadScene("Game", LoadSceneMode.Single);
            }
        }
    }

    void CheckAndSetMapData()
    {
        dbRefLobby.GetValueAsync().ContinueWith(sessionTask =>
        {
            if (sessionTask.IsCompleted && !sessionTask.IsFaulted)
            {
                DataSnapshot sessionSnapshot = sessionTask.Result;
                if (sessionSnapshot.Exists)
                {
                    Dictionary<string, object> mapData = new Dictionary<string, object>
                    {
                        { "region1", 15 },
                        { "region2", 19 },
                        { "region3", 16 },
                        { "region4", 18 },
                        { "region5", 16 },
                        { "region6", 16 }
                    };

                    dbRefLobby.Child("map").SetValueAsync(mapData);
                }
                else
                {
                    Debug.Log("Session has been removed. Not setting map data.");
                }
            }
            else
            {
                Debug.Log("Failed to fetch session data: " + sessionTask.Exception);
            }
        });
    }

    void StartingGame(string lobbyId)
    {
        if (readyPlayersCount == lobbySize)
        {
            isStarted = 1;
            dbRefLobby.Child("isStarted").SetValueAsync(isStarted);
        }
    }
}
