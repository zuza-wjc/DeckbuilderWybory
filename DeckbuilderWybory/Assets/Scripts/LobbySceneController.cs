using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;

public class LobbySceneController : MonoBehaviour
{
    public Text lobbyNameText;
    public Text lobbyCodeText;

    public GameObject scrollViewContent;
    public GameObject textTemplate;
    public Button readyButton;  // Przycisk gotowo�ci

    DatabaseReference dbRef;
    string lobbyId;
    string playerId;
    string playerName;

    bool readyState = false;

    private int readyPlayersCount = 0;
    private int totalPlayersCount = 0;
    public Text playerCountsText; // Tekst do wy�wietlania liczby graczy

    void Start()
    {
        // Pobierz nazw� lobby przekazan� z poprzedniej sceny
        string lobbyName = PlayerPrefs.GetString("LobbyName");
        // Pobierz lobbyId przekazane z poprzedniej sceny
        lobbyId = PlayerPrefs.GetString("LobbyId");
        playerId = PlayerPrefs.GetString("PlayerId");
        playerName = PlayerPrefs.GetString("PlayerName");

        // Ustaw nazw� lobby jako tekst do wy�wietlenia
        lobbyNameText.text = lobbyName;
        lobbyCodeText.text = "Kod do gry: " + lobbyId;

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
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId).Child("players");


        // Ustaw nas�uchiwanie zmian w strukturze bazy danych (dodanie/usuni�cie ga��zi)
        dbRef.ChildAdded += HandleChildAdded;
        dbRef.ChildRemoved += HandleChildRemoved;
        dbRef.ChildChanged += HandleChildChanged;

        // Dodaj listener do przycisku gotowo�ci
        readyButton.onClick.AddListener(ToggleReady);
    }

    void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        // Pobierz nazw� gracza z danych snapshot
        string playerName = args.Snapshot.Child("playerName").Value.ToString();
        bool readyStatus = (bool)args.Snapshot.Child("ready").Value;
        CreateText(playerName, readyStatus);
        totalPlayersCount++;

        // Sprawd�, czy gracz jest gotowy i zwi�ksz odpowiednio licznik
        if (readyStatus)
        {
            readyPlayersCount++;
        }

        // Aktualizuj tekst
        UpdatePlayerCountsText();
    }


    void HandleChildRemoved(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        // Pobierz nazw� gracza z danych snapshot
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

        // Pobierz ID gracza, kt�ry zmieni� stan
        string playerChanged = args.Snapshot.Key;


        // Je�li zmiana pochodzi od innego gracza ni� my, zaktualizuj licznik gotowych graczy
        if (playerId != playerChanged)
        {

            // Sprawd�, czy zmieni� si� stan gotowo�ci gracza
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

                // Aktualizuj tekst wy�wietlaj�cy liczb� graczy
                UpdatePlayerCountsText();
                UpdateText(playerNameChange, isReady);

            }
        }
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
        text.SetActive(true);
        text.GetComponentInChildren<Text>().text = playerInfo;
    }

    void UpdateText(string playerName, bool readyStatus)
    {
        // Przeszukaj wszystkie teksty w scrollViewContent
        Text[] texts = scrollViewContent.GetComponentsInChildren<Text>();

        foreach (Text text in texts)
        {
            // Sprawd� czy tekst zawiera imi� gracza
            if (text.text.Contains(playerName))
            {
                // Zaktualizuj zawarto�� tekstu na podstawie nowego statusu gotowo�ci
                if (readyStatus)
                {
                    text.text = playerName + "    GOTOWY";
                }
                else
                {
                    text.text = playerName + "    NIEGOTOWY";
                }
                return; // Zako�cz p�tl� po znalezieniu odpowiedniego tekstu
            }
        }

        // Je�li nie znaleziono tekstu dla danego gracza, utw�rz nowy
        CreateText(playerName, readyStatus);
    }

    void RemoveText(string playerName)
       {
        foreach (Transform child in scrollViewContent.transform)
        {
            if (child.GetComponentInChildren<Text>().text == playerName)
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
        // Aktualizacja warto�ci "ready" w bazie danych
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
        // Aktualizuj tekst wy�wietlaj�cy liczb� graczy
        playerCountsText.text = "Gotowi gracze: " + readyPlayersCount + " / " + totalPlayersCount;
    }

    public void LeaveLobby()
    {
        // Sprawd� aktualn� liczb� graczy w lobby
        dbRef.GetValueAsync().ContinueWith(countTask =>
        {
            if (countTask.IsCompleted && !countTask.IsFaulted)
            {
                DataSnapshot snapshot = countTask.Result;
                if (snapshot != null)
                {
                    // Sprawd� ilo�� dzieci (graczy) w ga��zi "players"
                    if (snapshot.ChildrenCount == 1)
                    {
                        // Je�li pozosta� tylko jeden gracz, usu� ca�e lobby
                        FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId).RemoveValueAsync();
                    }
                    else
                    {
                        // Usu� tylko gracza z bazy danych na podstawie playerId
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
}