using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Extensions;
using System;

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

    bool readyState = false;
    int readyPlayers = 0;
    int isStarted = 0;
    int lobbySize = 0;

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

        getLobbySizeFromDatabase(() =>
        {
            getReadyPlayersFromDatabase(() =>
            {
                UpdatePlayerCountsText();
            });
        });

        dbRefLobby.Child("isStarted").ValueChanged += HandleIsStartedChanged;
        dbRef.ChildAdded += HandleChildAdded;
        dbRef.ChildRemoved += HandleChildRemoved;
        dbRef.ChildChanged += HandleChildChanged;

        image = readyButton.GetComponent<Image>();
        readyButton.onClick.AddListener(ToggleReady);
        copyButton.onClick.AddListener(CopyFromClipboard);
    }

    void getLobbySizeFromDatabase(Action onComplete)
    {
        dbRefLobby.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists && snapshot.Child("lobbySize").Exists)
                {
                    lobbySize = int.Parse(snapshot.Child("lobbySize").Value.ToString());
                }
                else
                {
                    readyPlayers = 0; // Ustawienie domyślnej wartości
                    Debug.LogWarning("Ready players count not found. Defaulting to 0.");
                }
            }
            onComplete?.Invoke();
        });
    }

    void getReadyPlayersFromDatabase(Action onComplete)
    {
        dbRefLobby.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists && snapshot.Child("readyPlayers").Exists)
                {
                    readyPlayers = int.Parse(snapshot.Child("readyPlayers").Value.ToString());
                }
                else
                {
                    readyPlayers = 0; // Ustawienie domyślnej wartości
                    Debug.LogWarning("Ready players count not found. Defaulting to 0.");
                }
            }
            onComplete?.Invoke();
        });
    }

    void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        string playerName = args.Snapshot.Child("playerName").Value.ToString();
        bool isReady = (bool)args.Snapshot.Child("ready").Value;
        CreateText(playerName, isReady);
    }

    void HandleChildRemoved(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        getReadyPlayersFromDatabase(() =>
        {
            UpdatePlayerCountsText();
        });

        string playerName = args.Snapshot.Child("playerName").Value.ToString();
        RemoveText(playerName);
    }

    void HandleChildChanged(object sender, ChildChangedEventArgs args)
    {
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

                    string playerNameChange = args.Snapshot.Child("playerName").Value.ToString();

                    getReadyPlayersFromDatabase(() =>
                    {
                        UpdatePlayerCountsText();
                        UpdateText(playerNameChange, isReady);
                    });                    
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

    void CopyFromClipboard()
    {
        GUIUtility.systemCopyBuffer = lobbyId;
    }

    void ToggleReady()
    {
        readyState = !readyState;

        getReadyPlayersFromDatabase(() =>
        {
            readyPlayers += readyState ? 1 : -1;

            dbRef.Child(playerId).Child("ready").SetValueAsync(readyState);
            dbRefLobby.Child("readyPlayers").SetValueAsync(readyPlayers);

            UpdatePlayerCountsText();
            UpdateText(playerName, readyState);
            UpdateReadyButton();

            LoadPlayerCards();
        });
    }

    void UpdateReadyButton()
    {
        image.sprite = readyState ? selected : notSelected;
    }

    void UpdatePlayerCountsText()
    {
        playerCountsText.text = "GOTOWI GRACZE: " + readyPlayers + " / " + lobbySize;
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
                        // Usuń cały węzeł lobby, jeśli jesteś ostatnim graczem
                        dbRefLobby.RemoveValueAsync();
                    }
                    else
                    {
                        // Sprawdź, czy miałeś ustawione "ready: true"
                        dbRef.Child(playerId).Child("ready").GetValueAsync().ContinueWith(readyTask =>
                        {
                            if (readyTask.IsCompleted && !readyTask.IsFaulted)
                            {
                                bool wasReady = readyTask.Result.Value != null && (bool)readyTask.Result.Value;

                                if (wasReady)
                                {
                                    // Jeśli gracz był gotowy, zmniejsz licznik "readyPlayers"
                                    getReadyPlayersFromDatabase(() =>
                                    {
                                        readyPlayers -= 1;
                                        dbRefLobby.Child("readyPlayers").SetValueAsync(readyPlayers);
                                    });
                                }

                                // Usuń gracza z lobby niezależnie od stanu "ready"
                                dbRef.Child(playerId).RemoveValueAsync();
                            }
                            else
                            {
                                Debug.Log("Failed to get player 'ready' status: " + readyTask.Exception);
                            }
                        });
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

                int budget = 50; // NA CZAS IMPLEMENTACJI KART USTAWIONE NA SZTYWNO

                // getReadyPlayersFromDatabase();
                // if (readyPlayers >= 2 && readyPlayers <= 3)
                // {
                //     budget = 50;
                // } else if (readyPlayers >= 4 && readyPlayers <= 6 )
                // {
                //     budget = 70;
                //  } else if (readyPlayers >= 7 && readyPlayers <= 8 )
                //  {
                //     budget = 90;
                //  }

                dbRef.Child(playerId).Child("stats").Child("money").SetValueAsync(budget);

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

    // Funkcja do �adowania kart gracza z Firebase przed rozpocz�ciem gry
    void LoadPlayerCards()
    {
        DeckController deckController = FindObjectOfType<DeckController>();

        if (deckController != null)
        {
            // Wywo�aj metod� InitializeDeck()
            deckController.InitializeDeck();
          
        }
        else
        {
            Debug.LogError("DeckController not found!");
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
                    Dictionary<string, Dictionary<string, object>> mapData = new Dictionary<string, Dictionary<string, object>>
                {
                    { "region1", new Dictionary<string, object> { { "maxSupport", 15 }, { "type", "Ambasada" } } },
                    { "region2", new Dictionary<string, object> { { "maxSupport", 19 }, { "type", "Metropolia" } } },
                    { "region3", new Dictionary<string, object> { { "maxSupport", 16 }, { "type", "Środowisko" } } },
                    { "region4", new Dictionary<string, object> { { "maxSupport", 18 }, { "type", "Przemysł" } } },
                    { "region5", new Dictionary<string, object> { { "maxSupport", 16 }, { "type", "Metropolia" } } },
                    { "region6", new Dictionary<string, object> { { "maxSupport", 16 }, { "type", "Ambasada" } } }
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
        getReadyPlayersFromDatabase(() =>
        {
            if (readyPlayers == lobbySize)
            {
                isStarted = 1;
                dbRefLobby.Child("isStarted").SetValueAsync(isStarted);
            }
        });
    }
}
