using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using System;
using System.Collections.Generic;
using TMPro;
using Firebase;
using System.Threading.Tasks;

public class CreateLobbyManager : MonoBehaviour
{
    public TMP_InputField lobbyNameInput;
    public Button publicButton;
    public Button privateButton;
    public TextMeshProUGUI lobbySize;

    DatabaseReference dbRef;
    bool isPublic = true; // Poczπtkowo ustaw na publiczne

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

        // Dodaj nas≥uchiwacze na klikniÍcia przyciskÛw
        publicButton.onClick.AddListener(() => TogglePublic(true));
        privateButton.onClick.AddListener(() => TogglePublic(false));
    }

    public void TogglePublic(bool isPublicLobby)
    {
        isPublic = isPublicLobby;
    }

    public async void CreateLobby()
    {
        string lobbyId = await GenerateUniqueLobbyIdAsync();

        // Pobieranie nazwy gracza z PlayerPrefs
        string playerId = SystemInfo.deviceUniqueIdentifier;
        string playerName = "Gracz 1";

        // Tworzenie danych lobby
        Dictionary<string, object> lobbyData = new Dictionary<string, object>
        {
            { "lobbyName", lobbyNameInput.text },
            { "isPublic", isPublic },
            { "lobbySize", Convert.ToInt32(lobbySize.text) },
            { "players", new Dictionary<string, string> { { playerId, playerName } } }
        };

        // Dodawanie danych do bazy Firebase
        await dbRef.Child(lobbyId).SetValueAsync(lobbyData);

        Debug.Log("Lobby created with ID: " + lobbyId);
    }

    async Task<string> GenerateUniqueLobbyIdAsync()
    {
        string lobbyId = "";
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();

        // Sprawdzanie czy wygenerowane ID juø istnieje w bazie danych
        DataSnapshot snapshot = await dbRef.GetValueAsync();

        // Lista istniejπcych lobby ID
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