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
    public TMP_InputField lobbyNameInput;
    public Button publicButton;
    public Button privateButton;
    public TextMeshProUGUI lobbySizeText;
    public Button plusButton;
    public Button minusButton;

    DatabaseReference dbRef;
    bool isPublic = true; // Pocz¹tkowo ustaw na publiczne
    int lobbySize = 2;

    void Start()
    {
        // SprawdŸ, czy Firebase jest ju¿ zainicjalizowany
        if (FirebaseApp.DefaultInstance == null)
        {
            // Jeœli nie, inicjalizuj Firebase
            FirebaseInitializer firebaseInitializer = FindObjectOfType<FirebaseInitializer>();
            if (firebaseInitializer == null)
            {
                Debug.LogError("FirebaseInitializer not found in the scene!");
                return;
            }
        }

        // Inicjalizacja referencji do bazy danych Firebase
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions");

        // Dodaj nas³uchiwacze na klikniêcia przycisków
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
        if (lobbySize < 8)  // Upewnij siê, ¿e rozmiar lobby nie jest wiêkszy ni¿ 8
        {
            lobbySize++;
            UpdateLobbySizeText();
        }
    }

    public void DecreaseLobbySize()
    {
        if (lobbySize > 2)  // Upewnij siê, ¿e rozmiar lobby nie jest mniejszy ni¿ 2
        {
            lobbySize--;
            UpdateLobbySizeText();
        }
    }

    void UpdateLobbySizeText()
    {
        lobbySizeText.text = lobbySize.ToString();

        // Dezaktywuj przyciski, gdy rozmiar lobby osi¹gnie granice
        plusButton.interactable = lobbySize < 8;
        minusButton.interactable = lobbySize > 2;
    }

    public async void CreateLobby()
    {
        string lobbyId = await GenerateUniqueLobbyIdAsync();

        string playerId = System.Guid.NewGuid().ToString();
        string playerName = "Some Gracz";
        string lobbyName = lobbyNameInput.text;

        // Tworzenie danych lobby
        Dictionary<string, object> lobbyData = new Dictionary<string, object>
        {
            { "lobbyName", lobbyName },
            { "isPublic", isPublic },
            { "lobbySize", lobbySize },
            { "players", new Dictionary<string, object> { { playerId, new Dictionary<string, object> { { "playerName", playerName }, { "ready", false } } } } }
        };

        // Dodawanie danych do bazy Firebase
        await dbRef.Child(lobbyId).SetValueAsync(lobbyData);

        Debug.Log("Lobby created with ID: " + lobbyId);

        // Przejœcie do sceny Lobby i przekazanie nazwy lobby oraz lobbyId jako parametry
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        PlayerPrefs.SetString("LobbyName", lobbyName);
        PlayerPrefs.SetString("LobbyId", lobbyId);
        PlayerPrefs.SetString("PlayerId", playerId);
    }

    async Task<string> GenerateUniqueLobbyIdAsync()
    {
        string lobbyId = "";
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();

        // Sprawdzanie czy wygenerowane ID ju¿ istnieje w bazie danych
        DataSnapshot snapshot = await dbRef.GetValueAsync();

        // Lista istniej¹cych lobby ID
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
