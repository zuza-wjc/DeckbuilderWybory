using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Database;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;

public class JoinLobbyByCode : MonoBehaviour
{
    public TMP_InputField lobbyCodeInputField;
    public Button joinButton;

    public GameObject dialogBox;
    public GameObject background;

    DatabaseReference dbRef;

    string playerId;
    string lobbyName;
    string lobbyId;
    string playerName = "code_player";

    bool isPlayerAdded;

    void OnEnable()
    {
        isPlayerAdded = false;
    }

    void Start()
    {
        // Sprawdü, czy Firebase jest juø zainicjalizowany
        if (FirebaseApp.DefaultInstance == null)
        {
            // Jeúli nie, inicjalizuj Firebase
            FirebaseInitializer firebaseInitializer = FindObjectOfType<FirebaseInitializer>();
            if (firebaseInitializer == null)
            {
                Debug.LogError("FirebaseInitializer not found in the scene!");
                return;
            }
        }

        // Inicjalizacja referencji do bazy danych Firebase
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions");

        // Dodanie listenera do przycisku
        joinButton.onClick.AddListener(JoinLobby);
    }

    public void openDialogBox()
    {
        background.SetActive(true);
        dialogBox.SetActive(true);
        Debug.Log("here");
    }

    public void closeDialogBox()
    {
        background.SetActive(false);
        dialogBox.SetActive(false);
    }

    async void JoinLobby()
    {
        lobbyId = lobbyCodeInputField.text;

        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("Lobby ID is empty!");
            return;
        }

        Debug.Log("Checking if lobby exists and is full...");
        var lobbyStatus = await CheckLobbyAsync();
        if (lobbyStatus.exists)
        {
            if (!lobbyStatus.isFull)
            {
                Debug.Log("Lobby exists and is not full. Adding player...");
                await AddPlayerAsync();
            }
            else
            {
                Debug.LogError("Lobby is full! Showing dialog box.");
                openDialogBox();
            }
        }
        else
        {
            Debug.LogError("Lobby not found!");
        }

        if (isPlayerAdded)
        {
            ChangeScene();
        }
    }

    async Task<(bool exists, bool isFull)> CheckLobbyAsync()
    {
        var snapshot = await dbRef.Child(lobbyId).GetValueAsync();
        if (snapshot.Exists)
        {
            Debug.Log("Lobby exists.");
            int playerCount = (int)snapshot.Child("players").ChildrenCount;
            int lobbySize = int.Parse(snapshot.Child("lobbySize").GetValue(true).ToString());
            lobbyName = snapshot.Child("lobbyName").GetValue(true).ToString();
            bool isFull = playerCount >= lobbySize;
            return (true, isFull);
        }
        else
        {
            Debug.Log("Lobby not found.");
            return (false, false);
        }
    }

    async Task AddPlayerAsync()
    {
        playerId = System.Guid.NewGuid().ToString();

        Dictionary<string, object> playerData = new Dictionary<string, object>
        {
            { "playerName", playerName },
            { "ready", false }
        };

        var task = dbRef.Child(lobbyId).Child("players").Child(playerId).SetValueAsync(playerData);
        await task;

        if (task.IsCompletedSuccessfully)
        {
            Debug.Log("Player successfully added to the lobby.");
            isPlayerAdded = true;
        }
        else
        {
            Debug.LogError("Error adding player to the lobby.");
        }
    }

    public void ChangeScene()
    {
        Debug.Log("Changing scene...");

        // Przejúcie do sceny Lobby i przekazanie nazwy lobby oraz lobbyId jako parametry
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        DataTransfer.LobbyName = lobbyName;
        DataTransfer.LobbyId = lobbyId;
        DataTransfer.PlayerId = playerId;
        DataTransfer.PlayerName = playerName;
    }
}
