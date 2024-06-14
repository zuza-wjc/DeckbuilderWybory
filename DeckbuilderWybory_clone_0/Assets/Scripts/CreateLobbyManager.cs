using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using System.Collections.Generic;
using TMPro;
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

    private Image publicButtonImage; // Dodaj referencje do Image przycisku public
    private Image privateButtonImage; // Dodaj referencje do Image przycisku private
    public Sprite publicActiveSprite; // Sprite do wy�wietlenia, gdy przycisk public jest aktywny
    public Sprite publicInactiveSprite; // Sprite do wy�wietlenia, gdy przycisk public jest nieaktywny
    public Sprite privateActiveSprite; // Sprite do wy�wietlenia, gdy przycisk private jest aktywny
    public Sprite privateInactiveSprite; // Sprite do wy�wietlenia, gdy przycisk private jest nieaktywny

    private Image addButtonImage;
    private Image minusButtonImage;
    public Sprite addButtonActiveSprite;
    public Sprite minusButtonActiveSprite;
    public Sprite minusButtonInactiveSprite;
    public Sprite addButtonInactiveSprite;

    DatabaseReference dbRef;
    bool isPublic = true; // Pocz�tkowo ustaw na publiczne
    int lobbySize = 2;

    private List<string> availableNames = new List<string>() { "Katarzyna", "Wojciech", "Jakub", "Przemysław", "Gabriela", "Barbara", "Mateusz", "Aleksandra" };


    void Start()
    {
        // Sprawd�, czy Firebase jest ju� zainicjalizowany
        if (FirebaseApp.DefaultInstance == null)
        {
            // Je�li nie, inicjalizuj Firebase
            FirebaseInitializer firebaseInitializer = FindObjectOfType<FirebaseInitializer>();
            if (firebaseInitializer == null)
            {
                Debug.LogError("FirebaseInitializer not found in the scene!");
                return;
            }
        }

        // Inicjalizacja referencji do bazy danych Firebase
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions");

        publicButtonImage = publicButton.GetComponentInChildren<Image>();
        privateButtonImage = privateButton.GetComponentInChildren<Image>();
        addButtonImage = plusButton.GetComponentInChildren<Image>();
        minusButtonImage = minusButton. GetComponentInChildren<Image>();

        // Dodaj nas�uchiwacze na klikni�cia przycisk�w
        publicButton.onClick.AddListener(() => TogglePublic(true));
        privateButton.onClick.AddListener(() => TogglePublic(false));
        plusButton.onClick.AddListener(IncreaseLobbySize);
        minusButton.onClick.AddListener(DecreaseLobbySize);

        UpdateLobbySizeText();
    }

    public void TogglePublic(bool isPublicLobby)
    {
        isPublic = isPublicLobby;
        if (isPublic)
        {
            publicButtonImage.sprite = publicActiveSprite;
            privateButtonImage.sprite = privateInactiveSprite;
        }
        else
        {
            publicButtonImage.sprite = publicInactiveSprite;
            privateButtonImage.sprite = privateActiveSprite;
        }
    }

    public void IncreaseLobbySize()
    {
        if (lobbySize < 8)  // Upewnij si�, �e rozmiar lobby nie jest wi�kszy ni� 8
        {
            lobbySize++;
            UpdateLobbySizeText();
        }
    }

    public void DecreaseLobbySize()
    {
        if (lobbySize > 2)  // Upewnij si�, �e rozmiar lobby nie jest mniejszy ni� 2
        {
            lobbySize--;
            UpdateLobbySizeText();
        }
    }

    void UpdateLobbySizeText()
    {
        lobbySizeText.text = lobbySize.ToString();

        // Dezaktywuj przyciski, gdy rozmiar lobby osi�gnie granice
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

    public async void CreateLobby()
    {
        string lobbyId = await GenerateUniqueLobbyIdAsync();

        string playerId = System.Guid.NewGuid().ToString();

        var random = new System.Random();
        int index = random.Next(8);
        string playerName = availableNames[index];

        string lobbyName = lobbyNameInput.text;
        int isStarted = 0;
        int money = 0;
        int support = 0;

        // Tworzenie danych lobby
        Dictionary<string, object> lobbyData = new Dictionary<string, object>
        {
            { "lobbyName", lobbyName },
            { "isStarted", isStarted },
            { "isPublic", isPublic },
            { "lobbySize", lobbySize },
            { "players", new Dictionary<string, object> { { playerId, new Dictionary<string, object> { { "playerName", playerName }, { "ready", false }, { "stats", new Dictionary<string, object> { { "inGame", false }, { "money", money }, { "support", support }, { "playerTurn", false } }  } } } } }
        };

        // Dodawanie danych do bazy Firebase
        await dbRef.Child(lobbyId).SetValueAsync(lobbyData);

        Debug.Log("Lobby created with ID: " + lobbyId);

        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        
        //wsadzanie danych do data transfer
        DataTransfer.LobbyName = lobbyName;
        DataTransfer.LobbyId = lobbyId;
        DataTransfer.LobbySize = lobbySize;
        DataTransfer.IsStarted = isStarted;
        DataTransfer.PlayerId = playerId;
        DataTransfer.PlayerName = playerName;
    }

    async Task<string> GenerateUniqueLobbyIdAsync()
    {
        string lobbyId = "";
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();

        // Sprawdzanie czy wygenerowane ID ju� istnieje w bazie danych
        DataSnapshot snapshot = await dbRef.GetValueAsync();

        // Lista istniej�cych lobby ID
        List<string> existingIds = new List<string>();

        foreach (DataSnapshot childSnapshot in snapshot.Children)
        {
            existingIds.Add(childSnapshot.Key);
        }

        // Generowanie unikalnego ID
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
}
