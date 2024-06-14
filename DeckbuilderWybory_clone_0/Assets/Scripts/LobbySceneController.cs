using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LobbySceneController : MonoBehaviour
{
    public Text lobbyNameText;
    public Text lobbyCodeText;

    public GameObject scrollViewContent;
    public GameObject textTemplate;
    public Button readyButton;  // Przycisk gotowosci
    private Image readyButtonImage;

    public Sprite readySprite;
    public Sprite notReadySprite;

    DatabaseReference dbRef;
    DatabaseReference dbRefLobby;
    string lobbyId;
    int isStarted;
    string playerId;
    string playerName;
    int lobbySize;

    bool readyState = false;

    private int readyPlayersCount = 0;
    public Text playerCountsText; // Tekst do wyswietlania liczby graczy

    void Start()
    {
        // Pobierz nazwe lobby przekazana z poprzedniej sceny
        string lobbyName = DataTransfer.LobbyName;
        // Pobierz lobbyId przekazane z poprzedniej sceny
        isStarted = DataTransfer.IsStarted;
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;
        playerName = DataTransfer.PlayerName;
        lobbySize = DataTransfer.LobbySize;

        // Ustaw nazwe lobby jako tekst do wyswietlenia
        lobbyNameText.text = lobbyName;
        lobbyCodeText.text = "KOD: " + lobbyId;

        // Sprawdz, czy Firebase jest juz zainicjalizowany
        if (FirebaseApp.DefaultInstance == null)
        {
            // Jesli nie, inicjalizuj Firebase
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

        // Ustaw nasluchiwanie zaminy zmiennej isStarted
        dbRefLobby.Child("isStarted").ValueChanged += HandleIsStartedChanged;

        // Ustaw nasluchiwanie zmian w strukturze bazy danych (dodanie/usuniecie galezi)
        dbRef.ChildAdded += HandleChildAdded;
        dbRef.ChildRemoved += HandleChildRemoved;
        dbRef.ChildChanged += HandleChildChanged;

        readyButtonImage = readyButton.GetComponentInChildren<Image>();

        // Dodaj listener do przycisku gotowosci
        readyButton.onClick.AddListener(ToggleReady);
    }

    void HandleChildAdded(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        // Pobierz nazwe gracza z danych snapshot
        string playerName = args.Snapshot.Child("playerName").Value.ToString();
        CreateText(playerName, false);

        // Pobierz jego status ready i uaktualnij licznik gotowych graczy
        bool isReady = (bool)args.Snapshot.Child("ready").Value;
        if (isReady)
        {
            readyPlayersCount += 1;
        }

        // Aktualizuj tekst
        UpdatePlayerCountsText();
        UpdateText(playerName, isReady);

        StartingGame(lobbyId);
    }


    void HandleChildRemoved(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        // Pobierz nazwe gracza z danych snapshot
        string playerName = args.Snapshot.Child("playerName").Value.ToString();
        RemoveText(playerName);

        // Zmien licznik gotowych graczy po wyjsciu
        bool isReady = (bool)args.Snapshot.Child("ready").Value;
        if (isReady)
        {
            readyPlayersCount -= 1;
        }

        // Aktualizuj tekst
        UpdatePlayerCountsText();
        UpdateText(playerName, isReady);
    }

    void HandleChildChanged(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        // Jezeli zmienila sie wartosc ready dla jakiegokolwiek gracza
        foreach (var child in args.Snapshot.Children)
        {
            if (child.Key == "ready")
            {
                // Pobierz ID gracza, ktory zmienil stan
                string playerChanged = args.Snapshot.Key;


                // Jesli zmiana pochodzi od innego gracza niz my, zaktualizuj licznik gotowych graczy
                if (playerId != playerChanged)
                {
                    bool isReady = (bool)child.Value;
                    Debug.Log("Player " + playerChanged + " ready status changed to: " + isReady);

                    if (isReady)
                    {
                        readyPlayersCount += 1;
                    }
                    else
                    {
                        readyPlayersCount -= 1;
                    }

                    string playerNameChange = args.Snapshot.Child("playerName").Value.ToString();

                    // Aktualizuj tekst wyswietlajacy liczbe graczy
                    UpdatePlayerCountsText();
                    UpdateText(playerNameChange, isReady);
                }
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
            // Sprawdz czy tekst zawiera imie gracza
            if (text.text.Contains(playerName))
            {
                // Zaktualizuj zawartosc tekstu na podstawie nowego statusu gotowosci
                if (readyStatus)
                {
                    text.text = playerName + "    GOTOWY";
                }
                else
                {
                    text.text = playerName + "    NIEGOTOWY";
                }
                return; // Zakoncz petle po znalezieniu odpowiedniego tekstu
            }
        }

        // Jesli nie znaleziono tekstu dla danego gracza, utworz nowy
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
        UpdateImage();
        // Aktualizacja wartosci "ready" w bazie danych
        dbRef.Child(playerId).Child("ready").SetValueAsync(readyState);

        if (readyState)
        {
            readyPlayersCount += 1;
        }
        else
        {
            readyPlayersCount -= 1;
        }
        UpdatePlayerCountsText();
        UpdateText(playerName, readyState);
    }

    void UpdateImage()
    {
        readyButtonImage.sprite = readyState ? readySprite : notReadySprite;
    }

    void UpdatePlayerCountsText()
    {
        // Aktualizuj tekst wyswietlajacy liczbe graczy
        playerCountsText.text = "GOTOWI GRACZE: " + readyPlayersCount + " / " + lobbySize;
    }

    public void LeaveLobby()
    {
        // Sprawdz aktualna liczbe graczy w lobby
        dbRef.GetValueAsync().ContinueWith(countTask =>
        {
            if (countTask.IsCompleted && !countTask.IsFaulted)
            {
                DataSnapshot snapshot = countTask.Result;
                if (snapshot != null)
                {
                    // Sprawdz ilosc dzieci (graczy) w galezi "players"
                    if (snapshot.ChildrenCount == 1)
                    {
                        // Jesli pozostal tylko jeden gracz, usun cale lobby
                        FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId).RemoveValueAsync();
                    }
                    else
                    {
                        // Usun tylko gracza z bazy danych na podstawie playerId
                        dbRef.Child(playerId).RemoveValueAsync();
                    }
                }
                else
                {
                    Debug.Log("Failed to get lobby player count: snapshot is null");
                }
            }
            else
            {
                Debug.Log("Failed to get lobby player count: " + countTask.Exception);
            }
        });
    }

    void HandleIsStartedChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.Log(args.DatabaseError.Message);
            return;
        }

        // Sprawdz, czy zmienil sie stan isStarted
        if (args.Snapshot.Exists)
        {
            isStarted = int.Parse(args.Snapshot.Value.ToString());
            if (isStarted == 1)
            {
                dbRef.Child(playerId).Child("stats").Child("inGame").SetValueAsync(true);
                SceneManager.LoadScene("Game", LoadSceneMode.Single);
            }
        }
    }

    void StartingGame(string lobbyId)
    {
        //sprawdz czy liczba graczy i liczba gotowych graczy jest rowna rozmiarowi lobby
        if (readyPlayersCount == lobbySize)
        {
            //zmiana statusu lobby na started(zaczecie gry) i update bazy danych
            isStarted = 1;
            dbRefLobby.Child("isStarted").SetValueAsync(isStarted);
        }

    }
}