using System.Collections.Generic;
using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Extensions;
using System;
using System.Linq;
using System.Collections;

public class LobbySceneController : MonoBehaviour
{
    public Text lobbyNameText;
    public Text lobbyCodeText;
    public GameObject scrollViewContent;
    public GameObject textTemplate;

    public Button readyButton;
    public Sprite selected;
    public Sprite notSelected;
    private Image image;

    public Button copyButton;
    public Text playerCountsText;

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
            getReadyPlayersFromDatabase();
        });

        dbRefLobby.Child("isStarted").ValueChanged += HandleIsStartedChanged;
        dbRef.ChildAdded += HandleChildAdded;
        dbRef.ChildRemoved += HandleChildRemoved;
        dbRef.ChildChanged += HandleChildChanged;

        image = readyButton.GetComponent<Image>();
        readyButton.onClick.AddListener(ToggleReady);
        copyButton.onClick.AddListener(CopyFromClipboard);
    }

    void Update()
    {
        playerCountsText.text = "GOTOWI GRACZE: " + readyPlayers + " / " + lobbySize;
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
                    readyPlayers = 0;
                    Debug.LogWarning("Ready players count not found. Defaulting to 0.");
                }
            }
            onComplete?.Invoke();
        });
    }

    void getReadyPlayersFromDatabase(Action onComplete = null)
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
                    readyPlayers = 0;
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

        getReadyPlayersFromDatabase();

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
                   // Debug.Log("Player " + playerChanged + " ready status changed to: " + isReady);

                    string playerNameChange = args.Snapshot.Child("playerName").Value.ToString();

                    getReadyPlayersFromDatabase(() =>
                    {
                        UpdateText(playerNameChange, isReady);
                    });                    
                }
            }
        }
        StartingGame(lobbyId);
    }

    void CreateText(string playerName, bool readyStatus)
    {
        foreach (Transform child in scrollViewContent.transform)
        {
            Text childText = child.GetComponentInChildren<Text>();
            if (childText != null && childText.text.Contains(playerName))
            {
                Debug.Log("Obiekt z t¹ nazw¹ gracza ju¿ istnieje: " + playerName);
                return;
            }
        }

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

        // Disable the ready button immediately after clicking
        readyButton.interactable = false;

        dbRefLobby.Child("readyPlayers").RunTransaction(mutableData =>
        {
            int currentReadyPlayers = mutableData.Value == null ? 0 : Convert.ToInt32(mutableData.Value);

            // Increase or decrease the count of ready players
            if (readyState)
            {
                currentReadyPlayers++;
            }
            else
            {
                currentReadyPlayers--;
            }

            mutableData.Value = currentReadyPlayers;
            return TransactionResult.Success(mutableData);
        }).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                // Update the player's ready state in the database
                dbRef.Child(playerId).Child("ready").SetValueAsync(readyState);

                // Fetch the updated ready players count and update UI
                getReadyPlayersFromDatabase(() =>
                {
                    UpdateText(playerName, readyState);
                    UpdateReadyButton();
                    LoadPlayerCards();

                    // Re-enable the button after a delay
                    StartCoroutine(EnableButtonAfterDelay(1));
                });
            }
            else
            {
                Debug.LogError("Transaction failed: " + task.Exception);

                // Re-enable the button if the transaction fails
                StartCoroutine(EnableButtonAfterDelay(2));
            }
        });
    }

    IEnumerator EnableButtonAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        readyButton.interactable = true;
    }

    void UpdateReadyButton()
    {
        image.sprite = readyState ? selected : notSelected;
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
                        dbRef.Child(playerId).Child("ready").GetValueAsync().ContinueWith(readyTask =>
                        {
                            if (readyTask.IsCompleted && !readyTask.IsFaulted)
                            {
                               bool wasReady = readyTask.Result.Value != null && (bool)readyTask.Result.Value;

                               if (wasReady)
                               {
                                    getReadyPlayersFromDatabase(() =>
                                    {
                                        readyPlayers -= 1;
                                        dbRefLobby.Child("readyPlayers").SetValueAsync(readyPlayers);
                                    });
                                }

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

                getReadyPlayersFromDatabase(() =>
                {
                    int budget = 50;

                    if (readyPlayers >= 2 && readyPlayers <= 3)
                    {
                        budget = 50;
                    }
                    else if (readyPlayers >= 4 && readyPlayers <= 6)
                    {
                        budget = 70;
                    }
                    else if (readyPlayers >= 7 && readyPlayers <= 8)
                    {
                        budget = 90;
                    }

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

                getTurnOrder(() =>
                {
                    StartCoroutine(TransitionToGameScene());
                });
            }
        }
    }

    IEnumerator TransitionToGameScene()
    {
        // Czekaj na zapisanie wartości `inGame`
        var inGameTask = dbRef.Child(playerId).Child("stats").Child("inGame").SetValueAsync(true);

        // Poczekaj, aż zadanie zakończy się
        while (!inGameTask.IsCompleted)
        {
            yield return null; // Odczekaj jedną klatkę
        }

        if (inGameTask.IsFaulted || inGameTask.Exception != null)
        {
            Debug.LogError("Nie udało się zapisać wartości 'inGame' w bazie danych: " + inGameTask.Exception);
            yield break; // Przerwij, jeśli zapis nie powiódł się
        }

        // Dodaj 1-sekundowe opóźnienie
        yield return new WaitForSeconds(1f);

        // Przejdź do sceny "Game"
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }


    void getTurnOrder(Action onComplete)
    {
        dbRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    List<DataSnapshot> playersList = snapshot.Children.ToList();
                    playersList.Sort((a, b) => string.Compare(
                        a.Child("playerName").Value.ToString(),
                        b.Child("playerName").Value.ToString(),
                        StringComparison.Ordinal));

                    for (int i = 0; i < playersList.Count; i++)
                    {
                        string playerKey = playersList[i].Key;
                        dbRef.Child(playerKey).Child("myTurnNumber").SetValueAsync(i + 1);
                    }

                    Debug.Log("Turn order assigned successfully.");
                }
                else
                {
                    Debug.LogWarning("No players found in the lobby.");
                }
            }
            else
            {
                Debug.LogError("Failed to fetch players for turn order: " + task.Exception);
            }

            // Wywołanie callbacka po zakończeniu
            onComplete?.Invoke();
        });
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

    void OnDestroy()
    {
        if (dbRef != null)
        {
            dbRef.ChildAdded -= HandleChildAdded;
            dbRef.ChildRemoved -= HandleChildRemoved;
            dbRef.ChildChanged -= HandleChildChanged;
        }

        if (dbRefLobby != null)
        {
            dbRefLobby.Child("isStarted").ValueChanged -= HandleIsStartedChanged;
        }

        if (readyButton != null)
        {
            readyButton.onClick.RemoveListener(ToggleReady);
        }

        if (copyButton != null)
        {
            copyButton.onClick.RemoveListener(CopyFromClipboard);
        }
    }
}
