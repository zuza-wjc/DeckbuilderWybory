using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

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

        int isStarted = 0;
        int money = 0;
        int readyPlayers = 0;

        Dictionary<string, object> lobbyData = new Dictionary<string, object>
        {
            { "lobbyName", lobbyName },
            { "isStarted", isStarted },
            { "isPublic", isPublic },
            { "lobbySize", lobbySize },
            { "readyPlayers", readyPlayers },
            { "playerTurnId", "None" },
            { "rounds", 10 },
            { "players", new Dictionary<string, object> { { playerId, new Dictionary<string, object> { { "playerName", playerName }, { "ready", false }, { "stats", new Dictionary<string, object> { { "inGame", false }, { "money", money }, { "income", 10 }, { "support", new int[6] { 0, 0, 0, 0, 0, 0 } }, { "playerTurn", 2 }, { "turnsTaken",0 } }  } } } } }
        };

        await dbRef.Child(lobbyId).SetValueAsync(lobbyData);

        DataTransfer.LobbyName = lobbyName;
        DataTransfer.LobbyId = lobbyId;
        DataTransfer.LobbySize = lobbySize;
        DataTransfer.IsStarted = isStarted;
        DataTransfer.PlayerId = playerId;
        DataTransfer.PlayerName = playerName;

        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
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