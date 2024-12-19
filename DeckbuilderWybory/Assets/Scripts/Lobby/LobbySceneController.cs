using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Extensions;
using System;
using System.Threading.Tasks;

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

    bool readyState = false;
    int readyPlayers = 0;
    int isStarted = 0;
    int lobbySize = 0;

    public GameObject nameInputPanel;

    async void Start()
    {
        string lobbyName = DataTransfer.LobbyName;
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        lobbyNameText.text = lobbyName;
        lobbyCodeText.text = "KOD: " + lobbyId;

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");
        dbRefLobby = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);

        try
        {
            await getLobbySizeFromDatabaseAsync();
            await getReadyPlayersFromDatabaseAsync();
            UpdatePlayerCountsText();
        }
        catch (Exception ex)
        {
            Debug.LogError("Error during initialization: " + ex.Message);
        }

        dbRefLobby.Child("isStarted").ValueChanged += HandleIsStartedChanged;
        dbRefLobby.Child("readyPlayers").ValueChanged += HandleReadyPlayersChanged;
        dbRef.ChildAdded += HandleChildAdded;
        dbRef.ChildRemoved += HandleChildRemoved;
        dbRef.ChildChanged += HandleChildChanged;

        image = readyButton.GetComponent<Image>();
        copyButton.onClick.AddListener(CopyFromClipboard);
    }

    async Task getLobbySizeFromDatabaseAsync()
    {
        var snapshot = await dbRefLobby.GetValueAsync();
        if (snapshot.Exists && snapshot.Child("lobbySize").Exists)
        {
            lobbySize = int.Parse(snapshot.Child("lobbySize").Value.ToString());
        }
        else
        {
            Debug.LogWarning("Lobby size not found. Defaulting to 0.");
            lobbySize = 0;
        }
    }

    async Task getReadyPlayersFromDatabaseAsync()
    {
        var snapshot = await dbRefLobby.GetValueAsync();
        if (snapshot.Exists && snapshot.Child("readyPlayers").Exists)
        {
            readyPlayers = int.Parse(snapshot.Child("readyPlayers").Value.ToString());
        }
        else
        {
            Debug.LogWarning("Ready players count not found. Defaulting to 0.");
            readyPlayers = 0;
        }
    }

    async void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {

        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        string playerName = args.Snapshot.Child("playerName").Value?.ToString();

        if (string.IsNullOrEmpty(playerName))
        {
            if(playerId == args.Snapshot.Key)
            {
                nameInputPanel.SetActive(true);
            }
            await WaitForPlayerNameAsync(args.Snapshot.Key);
        }
        else
        {
            bool isReady = args.Snapshot.Child("ready").Value != null && (bool)args.Snapshot.Child("ready").Value;
            CreateText(playerName, isReady);
        }

     }

    async Task WaitForPlayerNameAsync(string playerId)
    {
        bool nameIsSet = false;

        while (!nameIsSet)
        {
            var snapshot = await dbRef.Child(playerId).GetValueAsync();

            if (snapshot.Exists && snapshot.Child("playerName").Value != null)
            {
                string playerName = snapshot.Child("playerName").Value.ToString();

                if (!string.IsNullOrEmpty(playerName))
                {
                    nameIsSet = true;

                    bool isReady = snapshot.Child("ready").Value != null && (bool)snapshot.Child("ready").Value;
                    CreateText(playerName, isReady);

                    nameInputPanel.SetActive(false);
                }
            }

            await Task.Delay(500);
        }
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
    }

    async void HandleChildChanged(object sender, ChildChangedEventArgs args)
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

                if (playerId == playerChanged)
                {
                    string playerNameChange = args.Snapshot.Child("playerName").Value?.ToString();
                    bool isReady = (bool)child.Value;
                    UpdateText(playerNameChange, isReady);
                }
                else
                {
                    string playerNameChange = args.Snapshot.Child("playerName").Value?.ToString();

                    if (string.IsNullOrEmpty(playerNameChange))
                    {
                        await WaitForPlayerNameAsync(playerChanged);
                        playerNameChange = args.Snapshot.Child("playerName").Value.ToString();
                    }
                    bool isReady = (bool)child.Value;

                    UpdateText(playerNameChange, isReady);
                }
            }
        }

        await StartingGameAsync();
    }



    async Task StartingGameAsync()
    {
        if (readyPlayers == lobbySize)
        {
            isStarted = 1;
            await dbRefLobby.Child("isStarted").SetValueAsync(isStarted);
        }
    }

    void CreateText(string playerName, bool readyStatus)
    {
        foreach (Transform child in scrollViewContent.transform)
        {
            Text childText = child.GetComponentInChildren<Text>();
            if (childText != null && childText.text.Contains(playerName))
            {
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
            if (text != null && (text.text == playerName + "    GOTOWY" || text.text == playerName + "    NIEGOTOWY"))
            {
                text.text = readyStatus ? playerName + "    GOTOWY" : playerName + "    NIEGOTOWY";
                return;
            }
        }

        if(playerName != "")
        {
            CreateText(playerName, readyStatus);
        }
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

    public async void ToggleReady()
    {
        readyButton.interactable = false;

        readyState = !readyState;

        await dbRefLobby.Child("readyPlayers").RunTransaction(mutableData =>
        {
            int currentReadyPlayers = mutableData.Value == null ? 0 : Convert.ToInt32(mutableData.Value);

            currentReadyPlayers += readyState ? 1 : -1;

            if (currentReadyPlayers < 0) currentReadyPlayers = 0;

            mutableData.Value = currentReadyPlayers;
            return TransactionResult.Success(mutableData);
        });

        await dbRef.Child(playerId).Child("ready").SetValueAsync(readyState);

        UpdateText(DataTransfer.PlayerName, readyState);
        UpdateReadyButton();

        await Task.Delay(500); // Poczekaj pol sekundy
        readyButton.interactable = true;
    }

    void UpdateReadyButton()
    {
        image.sprite = readyState ? selected : notSelected;
    }

    void UpdatePlayerCountsText()
    {
        playerCountsText.text = "GOTOWI GRACZE: " + readyPlayers + " / " + lobbySize;
    }

    public async void LeaveLobby()
    {
        var snapshot = await dbRef.GetValueAsync();

        if (snapshot.ChildrenCount == 1)
        {
            await dbRefLobby.RemoveValueAsync();
        }
        else
        {
            var readySnapshot = await dbRef.Child(playerId).Child("ready").GetValueAsync();

            if (readySnapshot.Exists && (bool)readySnapshot.Value)
            {
                readyPlayers -= 1;
                await dbRefLobby.Child("readyPlayers").SetValueAsync(readyPlayers);
            }

            await dbRef.Child(playerId).RemoveValueAsync();
        }
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

    void HandleReadyPlayersChanged(object sender, ValueChangedEventArgs args)
    {
        readyPlayers = Convert.ToInt32(args.Snapshot.Value);

        UpdatePlayerCountsText();
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
            dbRefLobby.Child("readyPlayers").ValueChanged -= HandleReadyPlayersChanged;
        }

        if (copyButton != null)
        {
            copyButton.onClick.RemoveListener(CopyFromClipboard);
        }
    }
}