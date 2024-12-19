using UnityEngine;
using Firebase;
using Firebase.Database;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

public class LobbyListManager : MonoBehaviour
{
    public GameObject scrollViewContent;
    public GameObject buttonTemplate;
    public GameObject dialogBox;

    string playerId;
    DatabaseReference dbRef;

    void Start()
    {
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions");

        dbRef.ChildAdded += HandleChildAdded;
        dbRef.ChildRemoved += HandleChildRemoved;
        dbRef.ChildChanged += HandleChildChanged;
    }

    void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        var snapshot = args.Snapshot;
        if (snapshot == null)
        {
            Debug.LogError("Snapshot is null!");
            return;
        }

        bool isPublic = false;
        isPublic = bool.Parse(snapshot.Child("isPublic").GetValue(true).ToString());

        var lobbyNameValue = snapshot.Child("lobbyName").GetValue(true);
        string lobbyName = lobbyNameValue != null ? lobbyNameValue.ToString() : "Unknown";

        var lobbySizeValue = snapshot.Child("lobbySize").GetValue(true);
        int lobbySize = lobbySizeValue != null ? int.Parse(lobbySizeValue.ToString()) : 0;

        int playerCount = (int)snapshot.Child("players").ChildrenCount;

        if (isPublic)
        {
            if (playerCount < lobbySize)
            {
                CreateButton(lobbyName, args.Snapshot.Key, playerCount, lobbySize);
            }
        }
    }

    void HandleChildChanged(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        string lobbyId = args.Snapshot.Key;
        int lobbySize = int.Parse(args.Snapshot.Child("lobbySize").GetValue(true).ToString());
        int playerCount = (int)args.Snapshot.Child("players").ChildrenCount;

        bool isPublicValue = bool.Parse(args.Snapshot.Child("isPublic").GetValue(true).ToString());

        // Sprawdź, czy liczba graczy jest mniejsza od rozmiaru lobby
        if (playerCount >= lobbySize)
        {
            string lobbyName = args.Snapshot.Child("lobbyName").GetValue(true).ToString();
            DestroyButton(lobbyName);
        }
        else
        {
            string lobbyName = args.Snapshot.Child("lobbyName").GetValue(true).ToString();
            bool buttonExists = false;
            foreach (Transform child in scrollViewContent.transform)
            {
                Text[] texts = child.GetComponentsInChildren<Text>();
                Text text1 = texts[0];
                Text text2 = texts[1];

                if (text1.text == lobbyName)
                {
                    buttonExists = true;
                    text2.text = $"{playerCount}/{lobbySize}";
                    break;
                }
            }
            if (!buttonExists && isPublicValue)
            {
                CreateButton(lobbyName, lobbyId, playerCount, lobbySize);
            }
        }
    }

    void HandleChildRemoved(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        string lobbyName = args.Snapshot.Child("lobbyName").GetValue(true).ToString();
        DestroyButton(lobbyName);
    }

    void CreateButton(string lobbyName, string lobbyId, int playerCount, int lobbySize)
    {
        if (scrollViewContent == null || buttonTemplate == null)
        {
            Debug.LogWarning("scrollViewContent or buttonTemplate is null, unable to create button.");
            return;
        }

        GameObject button = Instantiate(buttonTemplate, scrollViewContent.transform);
        if (button != null)
        {
            button.SetActive(true);

            Text[] texts = button.GetComponentsInChildren<Text>();
            Text text1 = texts[0];
            Text text2 = texts[1];

            text1.text = lobbyName;
            text2.text = $"{playerCount}/{lobbySize}";

            button.GetComponent<Button>().onClick.AddListener(delegate { _ = TaskOnClick(lobbyName, lobbyId, lobbySize); });
        }
        else
        {
            Debug.LogWarning("Button was not created because prefab instantiation failed.");
        }
    }

    void DestroyButton(string lobbyName)
    {
        if (scrollViewContent == null)
        {
            Debug.LogWarning("scrollViewContent is null, unable to destroy button.");
            return;
        }

        foreach (Transform child in scrollViewContent.transform)
        {
            if (child != null)
            {
                Text textComponent = child.GetComponentInChildren<Text>();

                if (textComponent != null && textComponent.text.Contains(lobbyName))
                {
                    if (child.gameObject != null)
                    {
                        Destroy(child.gameObject);
                        return;
                    }
                }
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

    async Task TaskOnClick(string lobbyName, string lobbyId, int lobbySize)
    {
        SetButtonsInteractable(false);

        Debug.Log("Checking and joining lobby...");
        bool joinedSuccessfully = false;

        try
        {
            joinedSuccessfully = await AddPlayer(lobbyId);
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while joining the lobby: {ex.Message}");
        }

        if (joinedSuccessfully)
        {
            Debug.Log("Successfully joined lobby!");

            var random = new System.Random();
            await AllocateSupportAsync(lobbyId, random);

            DataTransfer.LobbyName = lobbyName;
            DataTransfer.LobbyId = lobbyId;
            DataTransfer.LobbySize = lobbySize;
            DataTransfer.PlayerId = playerId;

            SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        }
        else
        {
            openDialogBox();
        }

        SetButtonsInteractable(true);
    }

    void SetButtonsInteractable(bool interactable)
    {
        foreach (Transform child in scrollViewContent.transform)
        {
            Button button = child.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }

    public void openDialogBox()
    {
        dialogBox.SetActive(true);
    }

    public void closeDialogBox()
    {
        dialogBox.SetActive(false);
    }

    void OnDestroy()
    {
        dbRef.ChildAdded -= HandleChildAdded;
        dbRef.ChildRemoved -= HandleChildRemoved;
        dbRef.ChildChanged -= HandleChildChanged;
    }
}
