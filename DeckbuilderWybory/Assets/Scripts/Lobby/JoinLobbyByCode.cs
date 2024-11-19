using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Database;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.SocialPlatforms;

public class JoinLobbyByCode : MonoBehaviour
{
    public InputField lobbyCodeInputField;
    public Button joinButton;

    public GameObject dialogBox;
    public GameObject background;

    DatabaseReference dbRef;

    string playerId;
    string lobbyName;
    string lobbyId;
    private List<string> availableNames = new List<string>() { "Katarzyna", "Wojciech", "Jakub", "Przemysław", "Gabriela", "Barbara", "Mateusz", "Aleksandra" };
    private List<string> gracze = new List<string>();

    bool isPlayerAdded;

    void OnEnable()
    {
        isPlayerAdded = false;
    }

    void Start()
    {
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions");

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
                await AddPlayerAsync(lobbyId);
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


    public async Task AssignName(string playerId, string lobbyId)
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

            DataTransfer.PlayerName= playerName;
        }
        else
        {
            Debug.LogError("Brak dostępnych imion.");
        }
    }

    public async Task<string> AddPlayerAsync(string lobbyId)
    {
        string playerId = System.Guid.NewGuid().ToString();

        await AssignName(playerId, lobbyId);
        isPlayerAdded = true;

        return playerId;
    }

    //async Task AddPlayerAsync()
    //{
    //    playerId = System.Guid.NewGuid().ToString();

    //    Dictionary<string, object> playerData = new Dictionary<string, object>
      //  {
        //    { "playerName", playerName },
          //  { "ready", false }
//        };

  //      var task = dbRef.Child(lobbyId).Child("players").Child(playerId).SetValueAsync(playerData);
    //    await task;

      //  if (task.IsCompletedSuccessfully)
        //{
          //  Debug.Log("Player successfully added to the lobby.");
            //isPlayerAdded = true;
//        }
  //      else
    //    {
      //      Debug.LogError("Error adding player to the lobby.");
        //}
    //}

    public void ChangeScene()
    {
        Debug.Log("Changing scene...");

        // Przej�cie do sceny Lobby i przekazanie nazwy lobby oraz lobbyId jako parametry
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        DataTransfer.LobbyName = lobbyName;
        DataTransfer.LobbyId = lobbyId;
        DataTransfer.PlayerId = playerId;
        DataTransfer.PlayerName = DataTransfer.PlayerName;
    }
}
