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
    public Button readyButton;  // Przycisk gotowoœci

    DatabaseReference dbRef;
    string lobbyId;
    string playerId;

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
        playerId = PlayerPrefs.GetString("PlayerId");

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
        CreateText(playerName, readyStatus.ToString());
        totalPlayersCount++;

        // SprawdŸ, czy gracz jest gotowy i zwiêksz odpowiednio licznik
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

                string playerName = args.Snapshot.Child("playerName").Value.ToString();

                // Aktualizuj tekst wyœwietlaj¹cy liczbê graczy
                UpdatePlayerCountsText();

            }
        }
    }

    void CreateText(string playerName, string readyStatus)
    {
        if (readyStatus == "true")
        {
            readyStatus = "GOTOWY";
        }
        else
        {
            readyStatus = "NIEGOTOWY";
        }

        GameObject text = Instantiate(textTemplate, scrollViewContent.transform);
        text.SetActive(true);
        text.GetComponentInChildren<Text>().text = playerName+"   "+readyStatus;
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
        // Aktualizacja wartoœci "ready" w bazie danych
        dbRef.Child(playerId).Child("ready").SetValueAsync(readyState);

        readyPlayersCount += readyState ? 1 : -1;
        UpdatePlayerCountsText();
 
    }

    void UpdateImageColor()
    {
        Image buttonImage = readyButton.GetComponent<Image>();
        buttonImage.color = readyState ? Color.green : Color.red;
    }

    void UpdatePlayerCountsText()
    {
        // Aktualizuj tekst wyœwietlaj¹cy liczbê graczy
        playerCountsText.text = "Gotowi gracze: " + readyPlayersCount + " / " + totalPlayersCount;
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
}
