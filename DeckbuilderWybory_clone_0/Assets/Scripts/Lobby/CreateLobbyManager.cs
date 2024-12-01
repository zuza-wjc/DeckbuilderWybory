using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Linq;
using System;

public class CreateLobbyManager : MonoBehaviour
{
    public InputField lobbyNameInput;
    public Button publicButton;
    public Button privateButton;
    public Text lobbySizeText;
    public Button plusButton;
    public Button minusButton;
    public Text feedbackText;

    private Image addButtonImage;
    private Image minusButtonImage;
    public Sprite addButtonActiveSprite;
    public Sprite minusButtonActiveSprite;
    public Sprite minusButtonInactiveSprite;
    public Sprite addButtonInactiveSprite;

    DatabaseReference dbRef;
    bool isPublic = true; 
    int lobbySize = 2;

    private List<string> availableNames = new List<string>() { "Katarzyna", "Wojciech", "Jakub", "Przemysław", "Gabriela", "Barbara", "Mateusz", "Aleksandra" };


    void Start()
    {
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions");

        addButtonImage = plusButton.GetComponentInChildren<Image>();
        minusButtonImage = minusButton.GetComponentInChildren<Image>();

        publicButton.onClick.AddListener(() => TogglePublic(true));
        privateButton.onClick.AddListener(() => TogglePublic(false));
        plusButton.onClick.AddListener(IncreaseLobbySize);
        minusButton.onClick.AddListener(DecreaseLobbySize);

        UpdateLobbySizeText();
    }

    public void TogglePublic(bool isPublicLobby)
    {
        isPublic = isPublicLobby;
    }

    public void IncreaseLobbySize()
    {
        if (lobbySize < 8) 
        {
            lobbySize++;
            UpdateLobbySizeText();
        }
    }

    public void DecreaseLobbySize()
    {
        if (lobbySize > 2)
        {
            lobbySize--;
            UpdateLobbySizeText();
        }
    }

    void UpdateLobbySizeText()
    {
        lobbySizeText.text = lobbySize.ToString();

        plusButton.interactable = lobbySize < 8;
        minusButton.interactable = lobbySize > 2;

        UpdateButtonSprites();
    }

    void UpdateButtonSprites()
    {
        if (plusButton.interactable)
        {
            addButtonImage.sprite = addButtonActiveSprite;
        }
        else
        {
            addButtonImage.sprite = addButtonInactiveSprite;
        }

        if (minusButton.interactable)
        {
            minusButtonImage.sprite = minusButtonActiveSprite;
        }
        else
        {
            minusButtonImage.sprite = minusButtonInactiveSprite;
        }
    }

    async Task<bool> IsLobbyNameUnique(string lobbyName)
    {
        DataSnapshot snapshot = await dbRef.GetValueAsync();
        foreach (DataSnapshot childSnapshot in snapshot.Children)
        {
            if (childSnapshot.HasChild("lobbyName"))
            {
                var lobbyNameValue = childSnapshot.Child("lobbyName").Value;
                if (lobbyNameValue != null && lobbyNameValue.ToString() == lobbyName)
                {
                    return false;
                }
            }
        }
        return true;
    }

    IEnumerator ShowErrorMessage(string message)
    {
        feedbackText.text = message;
        feedbackText.gameObject.SetActive(true);
        yield return new WaitForSeconds(3);
        feedbackText.gameObject.SetActive(false);
    }

    public async void CreateLobby()
    {
        string lobbyId = await GenerateUniqueLobbyIdAsync();

        string playerId = System.Guid.NewGuid().ToString();

        var random = new System.Random();
        int index = random.Next(8);
        string playerName = availableNames[index];

        string lobbyName = lobbyNameInput.text;
        bool isUnique = await IsLobbyNameUnique(lobbyName);

        if (lobbyName == "")
        {
            StartCoroutine(ShowErrorMessage("NAZWA NIE MOŻE BYĆ PUSTA"));
            return;
        }

        if (!isUnique)
        {
            StartCoroutine(ShowErrorMessage("NAZWA JEST JUŻ ZAJĘTA"));
            return;
        }
         
        await CheckAndSetMapData(lobbyId);

        int isStarted = 0;
        int money = 0;
        int readyPlayers = 0;

        int[] support = new int[6];

        support = await AllocateSupportAsync(lobbyId, random);

        Dictionary<string, object> lobbyData = new Dictionary<string, object>
        {
            { "lobbyName", lobbyName },
            { "isStarted", isStarted },
            { "isPublic", isPublic },
            { "lobbySize", lobbySize },
            { "readyPlayers", readyPlayers },
            { "playerTurnId", "None" },
            { "rounds", 10 },
            { "players", new Dictionary<string, object> { { playerId, new Dictionary<string, object> { { "playerName", playerName }, { "ready", false }, { "stats", new Dictionary<string, object> { { "inGame", false }, { "money", money }, { "income", 10 }, { "support", support }, { "playerTurn", 2 }, { "turnsTaken",0 } }  } } } } }
        };

        await dbRef.Child(lobbyId).UpdateChildrenAsync(lobbyData);

        DataTransfer.LobbyName = lobbyName;
        DataTransfer.LobbyId = lobbyId;
        DataTransfer.LobbySize = lobbySize;
        DataTransfer.IsStarted = isStarted;
        DataTransfer.PlayerId = playerId;
        DataTransfer.PlayerName = playerName;

        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
    }

    public async Task<int[]> AllocateSupportAsync(string lobbyId, System.Random random)
    {
        int[] support = new int[6];
        int totalSupport = 8;

        // Pobranie danych sesji (mapy)
        var sessionDataSnapshot = await dbRef.Child(lobbyId).Child("map").GetValueAsync();
        var sessionData = sessionDataSnapshot.Value as Dictionary<string, object>;
        Dictionary<int, int> maxSupport = new Dictionary<int, int>();

        // Pobranie danych maxSupport z mapy
        foreach (var regionData in sessionData)
        {
            int regionId = int.Parse(regionData.Key.Replace("region", "")) - 1;
            var regionDetails = regionData.Value as Dictionary<string, object>;

            int regionMaxSupport = Convert.ToInt32(regionDetails["maxSupport"]);
            maxSupport[regionId] = regionMaxSupport;
        }

        // Pobranie danych o wsparciu graczy (jeśli istnieją)
        var supportDataSnapshot = await dbRef.Child(lobbyId).Child("players").Child("support").GetValueAsync();
        var supportData = supportDataSnapshot.Value as Dictionary<string, object>;
        int[] currentSupport = new int[6];

        // Sprawdzamy, czy są dane o wsparciu graczy, jeśli nie, to ustawiamy wszystkie wartości na 0
        if (supportData != null)
        {
            foreach (var areaData in supportData)
            {
                int regionId = int.Parse(areaData.Key);
                currentSupport[regionId] = Convert.ToInt32(areaData.Value);
            }
        }

        // Wybór losowej liczby regionów (od 2 do 3)
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

        // Przydzielanie wsparcia do wybranych regionów
        for (int i = 0; i < regionsCount - 1; i++)
        {
            int maxPoints = totalSupport - (regionsCount - i - 1) * 2;
            int points;

            // Sprawdzamy, czy można przydzielić punkty bez przekroczenia limitu
            do
            {
                points = random.Next(2, maxPoints + 1);
            } while (points > maxSupport[chosenRegions[i]] - currentSupport[chosenRegions[i]]);

            support[chosenRegions[i]] = points;
            totalSupport -= points;
        }

        // Przydzielanie pozostałego wsparcia do ostatniego regionu
        int lastRegion = chosenRegions.Last();
        if (totalSupport <= maxSupport[lastRegion] - currentSupport[lastRegion])
        {
            support[lastRegion] = totalSupport;
        }
        else
        {
            throw new InvalidOperationException("Nie można przydzielić wsparcia bez przekroczenia limitu.");
        }

        // Zwracamy przydzielone wsparcie
        return support;
    }


    async Task CheckAndSetMapData(string lobbyId)
    {
        // Przygotowanie listy typów regionów
        List<string> regionTypes = new List<string> { "Ambasada", "Metropolia", "Środowisko", "Przemysł" };
        List<string> typesCopy = new List<string>(regionTypes);
        System.Random rng = new System.Random();

        // Losowanie i przetasowanie typów regionów
        regionTypes = regionTypes.Concat(typesCopy.OrderBy(x => rng.Next())).ToList();
        regionTypes.RemoveRange(regionTypes.Count - 2, 2); // Usuń dwa ostatnie elementy, jeśli jest to wymagane

        // Losowe przetasowanie regionów
        regionTypes = regionTypes.OrderBy(x => rng.Next()).ToList();

        // Stworzenie mapy z sześcioma regionami
        Dictionary<string, Dictionary<string, object>> mapData = new Dictionary<string, Dictionary<string, object>>
    {
        { "region1", new Dictionary<string, object> { { "maxSupport", rng.Next(15, 20) }, { "type", regionTypes[0] } } },
        { "region2", new Dictionary<string, object> { { "maxSupport", rng.Next(15, 20) }, { "type", regionTypes[1] } } },
        { "region3", new Dictionary<string, object> { { "maxSupport", rng.Next(15, 20) }, { "type", regionTypes[2] } } },
        { "region4", new Dictionary<string, object> { { "maxSupport", rng.Next(15, 20) }, { "type", regionTypes[3] } } },
        { "region5", new Dictionary<string, object> { { "maxSupport", rng.Next(15, 20) }, { "type", regionTypes[4] } } },
        { "region6", new Dictionary<string, object> { { "maxSupport", rng.Next(15, 20) }, { "type", regionTypes[5] } } }
    };

        // Zapisanie mapy do bazy danych Firebase, tworząc nową sesję
        await dbRef.Child(lobbyId).Child("map").SetValueAsync(mapData).ContinueWith(mapTask =>
        {
            if (mapTask.IsCompleted && !mapTask.IsFaulted)
            {
                Debug.Log("Map data has been successfully set for lobby " + lobbyId);
            }
            else
            {
                Debug.Log("Failed to set map data: " + mapTask.Exception);
            }
        });
    }


    async Task<string> GenerateUniqueLobbyIdAsync()
    {
        string lobbyId = "";
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();

        DataSnapshot snapshot = await dbRef.GetValueAsync();

        List<string> existingIds = new List<string>();

        foreach (DataSnapshot childSnapshot in snapshot.Children)
        {
            existingIds.Add(childSnapshot.Key);
        }

        do
        {
            char[] idChars = new char[8];
            for (int i = 0; i < idChars.Length; i++)
            {
                idChars[i] = chars[random.Next(chars.Length)];
            }
            lobbyId = new string(idChars);
        } while (existingIds.Contains(lobbyId));

        return lobbyId;
    }

    void OnDestroy()
    {
        if (publicButton != null)
        {
            publicButton.onClick.RemoveListener(() => TogglePublic(true));
        }

        if (privateButton != null)
        {
            privateButton.onClick.AddListener(() => TogglePublic(false));
        }

        if (plusButton != null)
        {
            plusButton.onClick.AddListener(IncreaseLobbySize);
        }

        if (minusButton != null)
        {
            minusButton.onClick.AddListener(DecreaseLobbySize);
        }
    }
}