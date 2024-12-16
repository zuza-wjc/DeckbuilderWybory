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
using System.Linq;
using System;

public class JoinLobbyByCode : MonoBehaviour
{
    public InputField lobbyCodeInputField;
    public Button joinButton;
    public Button pasteButton;
    public Text feedbackText;

    public GameObject dialogBox;

    DatabaseReference dbRef;

    string playerId;
    string lobbyName;
    string lobbyId;

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

        pasteButton.onClick.AddListener(PasteFromClipboard);
    }

    public void openDialogBox()
    {
        dialogBox.SetActive(true);
        Debug.Log("here");
    }

    public void closeDialogBox()
    {
        dialogBox.SetActive(false);
    }

    void PasteFromClipboard()
    {
        lobbyCodeInputField.text = GUIUtility.systemCopyBuffer;
    }

    async void JoinLobby()
    {
        lobbyId = lobbyCodeInputField.text;

        if (string.IsNullOrEmpty(lobbyId))
        {
            ShowErrorMessage("BRAK KODU");
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
                ChangeScene();
            }
            else
            {
                Debug.LogError("Lobby is full! Showing dialog box.");
                openDialogBox();
            }
        }
        else
        {
            ShowErrorMessage("KOD NIEPOPRAWNY");
            Debug.LogError("Lobby not found!");
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


    public async Task AssignPlayer(string playerId, string lobbyId)
    {
        string playerName = DataTransfer.PlayerName;
        var lobbyInfo = await dbRef.Child(lobbyId).GetValueAsync();

        var random = new System.Random();

        int[] support = new int[6];

        support = await AllocateSupportAsync(lobbyId, random);

        Dictionary<string, object> playerData = new Dictionary<string, object>
            {
                { "playerName", playerName },
                { "ready", false },
                {"drawCardsLimit", 4 },
                { "stats", new Dictionary<string, object>
                    {
                        { "inGame", false },
                        { "money", 50 },
                        { "income", 10 },
                        { "support", support },
                        { "playerTurn", 2 },
                        { "turnsTaken", 0 }
                    }
                }
            };

        await dbRef.Child(lobbyId).Child("players").Child(playerId).SetValueAsync(playerData);
    }

    public async Task<int[]> AllocateSupportAsync(string lobbyId, System.Random random)
    {
        int[] support = new int[6];
        int totalSupport = 8;

        var sessionDataSnapshot = await dbRef.Child(lobbyId).Child("map").GetValueAsync();
        var sessionData = sessionDataSnapshot.Value as Dictionary<string, object>;
        Dictionary<int, int> maxSupport = new Dictionary<int, int>();

        foreach (var regionData in sessionData)
        {
            int regionId = int.Parse(regionData.Key.Replace("region", "")) - 1;
            var regionDetails = regionData.Value as Dictionary<string, object>;

            int regionMaxSupport = Convert.ToInt32(regionDetails["maxSupport"]);
            maxSupport[regionId] = regionMaxSupport;
        }

        var supportDataSnapshot = await dbRef.Child(lobbyId).Child("players").Child("support").GetValueAsync();
        var supportData = supportDataSnapshot.Value as Dictionary<string, object>;
        int[] currentSupport = new int[6];

        if (supportData != null)
        {
            foreach (var areaData in supportData)
            {
                int regionId = int.Parse(areaData.Key);
                currentSupport[regionId] = Convert.ToInt32(areaData.Value);
            }
        }

        int regionsCount = random.Next(2, 4);
        List<int> chosenRegions = new List<int>();
        while (chosenRegions.Count < regionsCount)
        {
            int region = random.Next(0, 6);
            if (!chosenRegions.Contains(region))
            {
                chosenRegions.Add(region);
            }
        }

        for (int i = 0; i < regionsCount - 1; i++)
        {
            int maxPoints = totalSupport - (regionsCount - i - 1) * 2;
            int points;

            do
            {
                points = random.Next(2, maxPoints + 1);
            } while (points > maxSupport[chosenRegions[i]] - currentSupport[chosenRegions[i]]);

            support[chosenRegions[i]] = points;
            totalSupport -= points;
        }

        int lastRegion = chosenRegions.Last();
        if (totalSupport <= maxSupport[lastRegion] - currentSupport[lastRegion])
        {
            support[lastRegion] = totalSupport;
        }
        else
        {
            throw new InvalidOperationException("Nie można przydzielić wsparcia bez przekroczenia limitu.");
        }

        return support;
    }


    public async Task<string> AddPlayerAsync(string lobbyId)
    {
        playerId = System.Guid.NewGuid().ToString();

        await AssignPlayer(playerId, lobbyId);

        return playerId;
    }

    public void ChangeScene()
    {
        Debug.Log("Changing scene...");

        DataTransfer.LobbyName = lobbyName;
        DataTransfer.LobbyId = lobbyId;
        DataTransfer.PlayerId = playerId;

        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }

    async void ShowErrorMessage(string message)
    {
        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);
        await Task.Delay(3000);
        feedbackText.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (joinButton != null)
        {
            joinButton.onClick.RemoveListener(JoinLobby);
        }

        if (pasteButton != null)
        {
            pasteButton.onClick.RemoveListener(PasteFromClipboard);
        }
    }
}