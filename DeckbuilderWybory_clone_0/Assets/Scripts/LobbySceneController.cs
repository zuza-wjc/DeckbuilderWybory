using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbySceneController : MonoBehaviour
{
    public Text lobbyNameText;
    public Text lobbyCodeText;

    public GameObject scrollViewContent;
    public GameObject textTemplate;
    public Button readyButton;  // Przycisk gotowoœci

    DatabaseReference dbRef;
    DatabaseReference dbRefLobby;
    string lobbyId;
    int isStarted;
    string playerId;
    string playerName;
    int lobbySize;

    bool readyState = false;

    private int readyPlayersCount = 0;
    private int totalPlayersCount = 0;
    public Text playerCountsText; // Tekst do wyœwietlania liczby graczy

    void Start()
    {
        // Pobierz nazwê lobby przekazan¹ z poprzedniej sceny
        string lobbyName = PlayerPrefs.GetString("LobbyName");
        // Pobierz lobbyId przekazane z poprzedniej sceny
        lobbyId = PlayerPrefs.GetString("LobbyId");
        isStarted = PlayerPrefs.GetInt("IsStarted");
        playerId = PlayerPrefs.GetString("PlayerId");
        playerName = PlayerPrefs.GetString("PlayerName");
        lobbySize = PlayerPrefs.GetInt("LobbySize");

        // Ustaw nazwê lobby jako tekst do wyœwietlenia
        lobbyNameText.text = lobbyName;
        lobbyCodeText.text = "Kod do gry: " + lobbyId;

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
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId).Child("players");
        dbRefLobby = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId);
        dbRefLobby.Child("isStarted").ValueChanged += HandleIsStartedChanged;


        // Ustaw nas³uchiwanie zmian w strukturze bazy danych (dodanie/usuniêcie ga³êzi)
        dbRef.ChildAdded += HandleChildAdded;
        dbRef.ChildRemoved += HandleChildRemoved;
        dbRef.ChildChanged += HandleChildChanged;

        // Dodaj listener do przycisku gotowoœci
        readyButton.onClick.AddListener(ToggleReady);
    }

    void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        // Pobierz nazwê gracza z danych snapshot
        string playerName = args.Snapshot.Child("playerName").Value.ToString();
        bool readyStatus = (bool)args.Snapshot.Child("ready").Value;
        CreateText(playerName, readyStatus);
        totalPlayersCount++;

        // SprawdŸ, czy gracz jest gotowy i zwiêksz odpowiednio licznik
        if (readyStatus)
        {
            readyPlayersCount++;
        }

        // Aktualizuj tekst
        UpdatePlayerCountsText();
        StartingGame(lobbyId);
    }


    void HandleChildRemoved(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        // Pobierz nazwê gracza z danych snapshot
        string playerName = args.Snapshot.Child("playerName").Value.ToString();
        RemoveText(playerName);
        totalPlayersCount--;
        // Aktualizuj tekst
        UpdatePlayerCountsText();
    }

    void HandleChildChanged(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        // Pobierz ID gracza, który zmieni³ stan
        string playerChanged = args.Snapshot.Key;


        // Jeœli zmiana pochodzi od innego gracza ni¿ my, zaktualizuj licznik gotowych graczy
        if (playerId != playerChanged)
        {

            // SprawdŸ, czy zmieni³ siê stan gotowoœci gracza
            if (args.Snapshot.Child("ready").Exists)
            {
                bool isReady = (bool)args.Snapshot.Child("ready").Value;
                if (isReady)
                {
                    readyPlayersCount++;
                }
                else
                {
                    readyPlayersCount--;
                }

                string playerNameChange = args.Snapshot.Child("playerName").Value.ToString();

                // Aktualizuj tekst wyœwietlaj¹cy liczbê graczy
                UpdatePlayerCountsText();
                UpdateText(playerNameChange, isReady);

            }
        }
        StartingGame(lobbyId);
    }

    void CreateText(string playerName, bool readyStatus)
    {
        string playerInfo;

        if (readyStatus)
        {
            playerInfo = playerName + "    GOTOWY";
        }
        else
        {
            playerInfo = playerName + "    NIEGOTOWY";
        }

        GameObject text = Instantiate(textTemplate, scrollViewContent.transform);
        if (text != null)
        {
            text.SetActive(true);
            text.GetComponentInChildren<Text>().text = playerInfo;
        }
    }

    void UpdateText(string playerName, bool readyStatus)
    {
        // Przeszukaj wszystkie teksty w scrollViewContent
        Text[] texts = scrollViewContent.GetComponentsInChildren<Text>();

        foreach (Text text in texts)
        {
            // SprawdŸ czy tekst zawiera imiê gracza
            if (text.text.Contains(playerName))
            {
                // Zaktualizuj zawartoœæ tekstu na podstawie nowego statusu gotowoœci
                if (readyStatus)
                {
                    text.text = playerName + "    GOTOWY";
                }
                else
                {
                    text.text = playerName + "    NIEGOTOWY";
                }
                return; // Zakoñcz pêtlê po znalezieniu odpowiedniego tekstu
            }
        }

        // Jeœli nie znaleziono tekstu dla danego gracza, utwórz nowy
        CreateText(playerName, readyStatus);
    }

    void RemoveText(string playerName)
    {
        foreach (Transform child in scrollViewContent.transform)
        {
            Text textComponent = child.GetComponentInChildren<Text>();
            if (textComponent != null && textComponent.text.Contains(playerName))
            {
                Destroy(child.gameObject);
                return;
            }
        }
    }

    void ToggleReady()
    {
        readyState = !readyState;
        UpdateImageColor();
        // Aktualizacja wartoœci "ready" w bazie danych
        dbRef.Child(playerId).Child("ready").SetValueAsync(readyState);

        readyPlayersCount += readyState ? 1 : -1;
        UpdatePlayerCountsText();
        UpdateText(playerName,readyState);
    }

    void UpdateImageColor()
    {
        Image buttonImage = readyButton.GetComponent<Image>();
        buttonImage.color = readyState ? Color.green : Color.red;
    }

    void UpdatePlayerCountsText()
    {
        // Aktualizuj tekst wyœwietlaj¹cy liczbê graczy
        playerCountsText.text = "Gotowi gracze: " + readyPlayersCount + " / " + lobbySize;
    }

    public void LeaveLobby()
    {
        // SprawdŸ aktualn¹ liczbê graczy w lobby
        dbRef.GetValueAsync().ContinueWith(countTask =>
        {
            if (countTask.IsCompleted && !countTask.IsFaulted)
            {
                DataSnapshot snapshot = countTask.Result;
                if (snapshot != null)
                {
                    // SprawdŸ iloœæ dzieci (graczy) w ga³êzi "players"
                    if (snapshot.ChildrenCount == 1)
                    {
                        // Jeœli pozosta³ tylko jeden gracz, usuñ ca³e lobby
                        FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId).RemoveValueAsync();
                    }
                    else
                    {
                        // Usuñ tylko gracza z bazy danych na podstawie playerId
                        dbRef.Child(playerId).RemoveValueAsync();
                    }
                }
                else
                {
                    Debug.LogError("Failed to get lobby player count: snapshot is null");
                }
            }
            else
            {
                Debug.LogError("Failed to get lobby player count: " + countTask.Exception);
            }
        });
    }

    void HandleIsStartedChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        // SprawdŸ, czy zmieni³ siê stan isStarted
        if (args.Snapshot.Exists)
        {
            isStarted = int.Parse(args.Snapshot.Value.ToString());
            if (isStarted == 1)
            {
                SceneManager.LoadScene("Game", LoadSceneMode.Single);
            }
        }
    }

    void StartingGame (string lobbyId)
    {
        //sprawdz czy liczba graczy i liczba gotowych graczy jest rowna rozmiarowi lobby
        if(readyPlayersCount==lobbySize && totalPlayersCount==lobbySize)
        {
            //zmiana statusu lobby na started(zaczêcie gry) i update bazy danych
            isStarted = 1;
            dbRefLobby.Child("isStarted").SetValueAsync(isStarted);
        }

    } 
}
