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
    bool isFull;

    void Start()
    {
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions");

        joinButton.onClick.AddListener(JoinLobby);
        pasteButton.onClick.AddListener(PasteFromClipboard);
    }

    public void openDialogBox()
    {
        dialogBox.SetActive(true);
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
        isFull = false;
        lobbyId = lobbyCodeInputField.text;

        if (string.IsNullOrEmpty(lobbyId))
        {
            ShowErrorMessage("BRAK KODU");
            Debug.LogError("Lobby ID is empty!");
            return;
        }

        Debug.Log("Checking and joining lobby...");
        bool joinedSuccessfully = false;

        try
        {
            joinedSuccessfully = await AddPlayer(lobbyId);
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while joining the lobby: {ex.Message}");
            ShowErrorMessage("BŁĘDNY KOD");
        }

        if (joinedSuccessfully)
        {
            Debug.Log("Successfully joined lobby!");

            var random = new System.Random();
            await AllocateSupportAsync(lobbyId, random);

            ChangeScene();
        }
        else
        {
            if (isFull)
            {
                openDialogBox();
            }
            else
            {
                ShowErrorMessage("BŁĘDNY KOD");
            }
        }
    }

    public async Task<bool> AddPlayer(string lobbyId)
    {
        playerId = System.Guid.NewGuid().ToString();
        bool success = false;

        try
        {
            await dbRef.Child(lobbyId).RunTransaction((MutableData mutableData) =>
            {
                var lobbyData = mutableData.Value as Dictionary<string, object>;

                if (lobbyData == null)
                {
                    Debug.LogError("Lobby does not exist!");
                    success = false;
                    return TransactionResult.Abort();
                }

                var playersNode = mutableData.Child("players");
                long playerCount = playersNode.ChildrenCount;
                int maxPlayers = 0;

                if (lobbyData.ContainsKey("lobbySize"))
                {
                    maxPlayers = Convert.ToInt32(lobbyData["lobbySize"]);
                }

                if (playerCount >= maxPlayers)
                {
                    Debug.Log("Lobby is full!");
                    isFull = true;
                    success = false;
                    return TransactionResult.Abort();
                }

                Dictionary<string, object> playerData = new Dictionary<string, object>
                {
                    { "playerName", DataTransfer.PlayerName },
                    { "ready", false },
                    { "drawCardsLimit", 4 },
                    { "stats", new Dictionary<string, object>
                        {
                            { "inGame", false },
                            { "money", 50 },
                            { "income", 10 },
                            { "support", new int[6] },
                            { "playerTurn", 2 },
                            { "turnsTaken", 0 }
                        }
                    }
                };

                playersNode.Child(playerId).Value = playerData;

                success = true;
                return TransactionResult.Success(mutableData);
            });
        }
        catch (FirebaseException ex)
        {
            Debug.Log(ex);
            success = false;
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
            success = false;
        }

        return success;
    }


    public async Task AllocateSupportAsync(string lobbyId, System.Random random)
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

        try
        {
            await dbRef.Child(lobbyId).Child("players").Child(playerId).Child("stats").Child("support").SetValueAsync(support);
            Debug.Log("Support data has been successfully saved to Firebase!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save support data: {ex.Message}");
        }
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