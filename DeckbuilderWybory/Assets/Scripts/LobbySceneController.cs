using System.Collections;
using System.Collections.Generic; // Added for Dictionary
using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbySceneController : MonoBehaviour
{
    public Text lobbyNameText;
    public Text lobbyCodeText;
    public GameObject scrollViewContent;
    public GameObject textTemplate;
    public Button readyButton;  // Przycisk gotowosci
    private Image readyButtonImage;
    public Button copyButton; //przycisk do kopiowania kodu
    public Sprite readySprite;
    public Sprite notReadySprite;
    public Text playerCountsText; // Tekst do wyswietlania liczby graczy

    DatabaseReference dbRef;
    DatabaseReference dbRefLobby;
    string lobbyId;
    int isStarted;
    string playerId;
    string playerName;
    int lobbySize;

    bool readyState = false;
    private int readyPlayersCount = 0;

    void Start()
    {
        string lobbyName = DataTransfer.LobbyName;
        isStarted = DataTransfer.IsStarted;
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;
        playerName = DataTransfer.PlayerName;
        lobbySize = DataTransfer.LobbySize;

        lobbyNameText.text = lobbyName;
        lobbyCodeText.text = "KOD: " + lobbyId;

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");
        dbRefLobby = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);

        dbRefLobby.Child("isStarted").ValueChanged += HandleIsStartedChanged;
        dbRef.ChildAdded += HandleChildAdded;
        dbRef.ChildRemoved += HandleChildRemoved;
        dbRef.ChildChanged += HandleChildChanged;

        readyButtonImage = readyButton.GetComponentInChildren<Image>();
        readyButton.onClick.AddListener(ToggleReady);
        copyButton.onClick.AddListener(CopyFromClipboard);
    }

    void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        string playerName = args.Snapshot.Child("playerName").Value.ToString();
        CreateText(playerName, false);

        bool isReady = (bool)args.Snapshot.Child("ready").Value;
        if (isReady)
        {
            readyPlayersCount += 1;
        }

        UpdatePlayerCountsText();
        UpdateText(playerName, isReady);

        StartingGame(lobbyId);
    }

    void HandleChildRemoved(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        string playerName = args.Snapshot.Child("playerName").Value.ToString();
        RemoveText(playerName);

        bool isReady = (bool)args.Snapshot.Child("ready").Value;
        if (isReady)
        {
            readyPlayersCount -= 1;
        }

        UpdatePlayerCountsText();
        UpdateText(playerName, isReady);
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
        UpdateImage();
        dbRef.Child(playerId).Child("ready").SetValueAsync(readyState);

        readyPlayersCount += readyState ? 1 : -1;
        UpdatePlayerCountsText();
        UpdateText(playerName, readyState);
    }

    void UpdateImage()
    {
        readyButtonImage.sprite = readyState ? readySprite : notReadySprite;
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
                        FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId).RemoveValueAsync();
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
                    Dictionary<string, Dictionary<string, object>> mapData = new Dictionary<string, Dictionary<string, object>>
                {
                    { "region1", new Dictionary<string, object> { {"currentSupport",15}, { "maxSupport", 15 }, { "type", "Ambasada" } } },
                    { "region2", new Dictionary<string, object> { {"currentSupport",19}, { "maxSupport", 19 }, { "type", "Metropolia" } } },
                    { "region3", new Dictionary<string, object> { {"currentSupport",16}, { "maxSupport", 16 }, { "type", "�rodowisko" } } },
                    { "region4", new Dictionary<string, object> { {"currentSupport",18}, { "maxSupport", 18 }, { "type", "Przemys�" } } },
                    { "region5", new Dictionary<string, object> { {"currentSupport",16}, { "maxSupport", 16 }, { "type", "Metropolia" } } },
                    { "region6", new Dictionary<string, object> { {"currentSupport",16}, { "maxSupport", 16 }, { "type", "Ambasada" } } }
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
