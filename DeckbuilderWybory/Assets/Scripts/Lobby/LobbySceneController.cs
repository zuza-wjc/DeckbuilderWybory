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

    public Button readyButton;
    public Sprite selected;
    public Sprite notSelected;
    private Image image;

    public Button copyButton;
    public Text playerCountsText;

    public DeckController deckController;

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
                    readyPlayers = 0;
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
                    // Debug.Log("Player " + playerChanged + " ready status changed to: " + isReady);

                    string playerNameChange = args.Snapshot.Child("playerName").Value.ToString();

                    getReadyPlayersFromDatabase(() =>
                    {
                        UpdatePlayerCountsText();
                        UpdateText(playerNameChange, isReady);
                    });
                }
            }
        }
        StartingGame();
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

    public void ToggleReady()
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
        int isStartedValue = Convert.ToInt32(args.Snapshot.Value);
        if (isStartedValue == 1)
        {
            SceneManager.LoadScene("LoadingScreen", LoadSceneMode.Single);
        }

    }

    void StartingGame()
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

        if (copyButton != null)
        {
            copyButton.onClick.RemoveListener(CopyFromClipboard);
        }
    }


}