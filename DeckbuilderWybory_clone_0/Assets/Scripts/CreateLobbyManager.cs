using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using System.Collections.Generic;
using TMPro;
using Firebase;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Linq;
using Unity.VisualScripting;


//public class AvailableNames
//{
//    private List<string> names;
//    public AvailableNames()
//    {
//        names = new List<string>() { "Gracz2", "Gracz3", "Gracz4", "Gracz5", "Gracz6", "Gracz7", "Gracz8" };
//    }

//public void AddName(string name)
//{
//    // Add validation or other logic here
//    names.Add(name);
//}

//public bool RemoveName(string name)
//{
//    return names.Remove(name);
//}

//public List<string> GetNames()
//{
//    return names;
//}
//}
//public class LobbyData
//{
//    public string LobbyName { get; set; }
//    public bool IsPublic { get; set; }
//    public int LobbySize { get; set; }
//    public Dictionary<string, string> Players { get; set; }
//    public List<string> AvailableNames { get; set; }
//}



public class CreateLobbyManager : MonoBehaviour
{
    public TMP_InputField lobbyNameInput;
    public Button publicButton;
    public Button privateButton;
    public TextMeshProUGUI lobbySizeText;
    public Button plusButton;
    public Button minusButton;

    DatabaseReference dbRef;
    bool isPublic = true; // Początkowo ustaw na publiczne
    int lobbySize = 2;
    private List<string> availableNames;

        void Start()
    {

        // Sprawdź, czy Firebase jest już zainicjalizowany
        if (FirebaseApp.DefaultInstance == null)
        {
            // Jeśli nie, inicjalizuj Firebase
            FirebaseInitializer firebaseInitializer = FindObjectOfType<FirebaseInitializer>();
            if (firebaseInitializer == null)
            {
                Debug.LogError("FirebaseInitializer not found in the scene!");
                return;
            }
        }

        // Inicjalizacja referencji do bazy danych Firebase
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions");

        // Dodaj nasłuchiwacze na kliknięcia przycisków
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
        if (lobbySize < 8)  // Upewnij się, że rozmiar lobby nie jest większy niż 8
        {
            lobbySize++;
            UpdateLobbySizeText();
        }
    }

    public void DecreaseLobbySize()
    {
        if (lobbySize > 2)  // Upewnij się, że rozmiar lobby nie jest mniejszy niż 2
        {
            lobbySize--;
            UpdateLobbySizeText();
        }
    }

    void UpdateLobbySizeText()
    {
        lobbySizeText.text = lobbySize.ToString();

        // Dezaktywuj przyciski, gdy rozmiar lobby osiągnie granice
        plusButton.interactable = lobbySize < 8;
        minusButton.interactable = lobbySize > 2;
    }

    public async void CreateLobby()
    {
        string lobbyId = await GenerateUniqueLobbyIdAsync();

        string playerId = SystemInfo.deviceUniqueIdentifier;
        string playerName = "Gracz1";
        string lobbyName = lobbyNameInput.text;

        Dictionary<string, object> lobbyData = new Dictionary<string, object>
        {
            { "lobbyName", lobbyName },
            { "isPublic", isPublic },
            { "lobbySize", lobbySize },
            { "players", new Dictionary<string, string> { { playerId, playerName } } },
            { "availableNames", availableNames = new List<string>(){"Gracz2", "Gracz3", "Gracz4", "Gracz5", "Gracz6", "Gracz7", "Gracz8"} }

        };

        //lobbyData = new lobbyData;


        // próbny odczyt
        //if (lobbyData.TryGetValue("availableNames", out object availableNamesObj))
        //{
        //    var availableNamesList = availableNamesObj as List<string>;
        //    foreach (var name in availableNamesList)
        //        Debug.Log(name + "  ");
        //}


        // Dodawanie danych do bazy Firebase
        await dbRef.Child(lobbyId).SetValueAsync(lobbyData);

        Debug.Log("Lobby created with ID: " + lobbyId);

        // odczyt sprawdzający
        var lobbyInfo = await dbRef.Child(lobbyId).GetValueAsync();
        //var lobbyInfo = await dbRef.GetValueAsync();


        if (lobbyInfo.Exists && lobbyInfo.Value != null)
        {
            lobbyData = lobbyInfo.Value as Dictionary<string, object>;
            //Debug.Log("lobbyData =" + lobbyData);

            ////    // lobbyData = lobbyInfo;

            ////    //Debug.Log(lobbyData.TryGetValue("availableNames", out object availableNamesObj));

            if (lobbyData.TryGetValue("availableNames", out object availableNamesObj))
            {
                Debug.Log("Są jakieś availableNames = True");
                var availableNamesList = availableNamesObj as List<string>;
                Debug.Log("Lista availableNames = " + availableNamesList);
                Debug.Log("coś jest ?");

                if (availableNamesList != null && availableNamesList.Count == 0)
                {
                    // The list is empty
                    Debug.Log("Empty !");
                }

                Debug.Log(" availableNamesList istnieje, jeżeli linia wyżej nie jest Empty !");
                //Debug.Log(availableNamesList);

                //Debug.Log("pojedynczy rekord loobyID =" + lobbyData[lobbyId]);

                foreach (var name in lobbyData)
                {
                    if (name.Key == "availableNames")
                    {
                        Debug.Log(name.Key);
                        Debug.Log(name.Value);

                        var availableNames1 = name.Value as List<object>;

                        foreach (var name2 in availableNames1)
                        {
                            Debug.Log(name2.ToString());
                            //Debug.Log(name2);
                        }

                    }


                }

            }
        }

        // Przejście do sceny Lobby i przekazanie nazwy lobby oraz lobbyId jako parametry
        SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        PlayerPrefs.SetString("LobbyName", lobbyName);
        PlayerPrefs.SetString("LobbyId", lobbyId);

    }

    async Task<string> GenerateUniqueLobbyIdAsync()
    {
        string lobbyId = "";
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        System.Random random = new System.Random();

        // Sprawdzanie czy wygenerowane ID już istnieje w bazie danych
        DataSnapshot snapshot = await dbRef.GetValueAsync();

        // Lista istniejących lobby ID
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