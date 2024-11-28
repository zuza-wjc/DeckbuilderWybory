using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Database;
using System;
using Firebase.Extensions;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class TurnController : MonoBehaviour
{
    public Text timeText;
    public Text turnPlayerName;
    public Text roundText;
    public Button passButton;

    DatabaseReference dbRef;
    DatabaseReference dbRefLobby;
    string lobbyId;
    string playerId;
    int rounds;

    private List<string> turnOrderList;
    private bool isMyTurn = false;
    private float timer = 60f;
    private string currentPlayerName;
    private string previousPlayerId;
    private int turnsTaken = 0;

    public CardOnHandController cardsOnHandController;

    void Start()
    {
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");
        dbRefLobby = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);

        passButton.interactable = false;
        passButton.onClick.AddListener(PassTurn);

        roundText.text = "Runda: 1";

        StartCoroutine(InitializeGameFlow());
    }

    void Update()
    {
        if (isMyTurn)
        {
            timer -= Time.deltaTime;
            timer = Mathf.Max(timer, 0);
            timeText.text = $"{Mathf.CeilToInt(timer)}";

            if (timer <= 0)
            {
                EndTurn();
            }
        }
        else
        {
            timeText.text = "";
        }
    }

    IEnumerator InitializeGameFlow()
    {
        yield return StartCoroutine(InitializeTurnOrderCoroutine());

        if (playerId != turnOrderList[0])
        {
            getPlayerName(() =>
            {
                turnPlayerName.text = "Tura: " + currentPlayerName;
            }, turnOrderList[0]);

            Debug.Log("Tura gracza: " + turnOrderList[0]);
        }
        else
        {
            StartTurn();
        }
    }

    IEnumerator InitializeTurnOrderCoroutine()
    {
        bool isCompleted = false;

        dbRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    Dictionary<string, int> players = new Dictionary<string, int>();

                    foreach (DataSnapshot player in snapshot.Children)
                    {
                        string id = player.Key;
                        var turnOrderValue = player.Child("myTurnNumber").Value;

                        if (turnOrderValue != null)
                        {
                            if (int.TryParse(turnOrderValue.ToString(), out int turnOrder))
                            {
                                players.Add(id, turnOrder);
                            }
                            else
                            {
                                Debug.LogWarning($"Nie uda³o siê sparsowaæ kolejnoœci tury gracza {id}.");
                            }
                        }
                        else
                        {
                            Debug.Log(id + "jest NULL");
                        }
                    }

                    turnOrderList = players.OrderBy(p => p.Value).Select(p => p.Key).ToList();

                    int myIndex = turnOrderList.IndexOf(playerId);
                    int previousIndex = (myIndex - 1 + turnOrderList.Count) % turnOrderList.Count;
                    previousPlayerId = turnOrderList[previousIndex];
                }
                else
                {
                    Debug.LogWarning("Snapshot does not exist at the specified path.");
                }
            }
            else
            {
                Debug.LogError("Failed to get data from Firebase: " + task.Exception);
            }

            isCompleted = true; // Oznacz jako ukoñczone
        });

        // Czekaj, a¿ `isCompleted` bêdzie true
        while (!isCompleted)
        {
            yield return null;
        }

        // Dodaj listenery
        StartListening();
    }


    void StartListening()
    {
        if (string.IsNullOrEmpty(previousPlayerId))
        {
            Debug.LogError("Previous player ID is not set.");
            return;
        }

        dbRefLobby.Child("playerTurnId").ValueChanged += HandlePlayerTurnIdChanged;
        dbRefLobby.Child("rounds").ValueChanged += HandleRoundsChanged;
        dbRef.Child(previousPlayerId).Child("stats").Child("playerTurn").ValueChanged += HandlePreviousPlayerTurnChanged;
    }

    void HandlePreviousPlayerTurnChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.Snapshot.Exists && args.Snapshot.Value != null)
        {
            int previousPlayerTurn = int.Parse(args.Snapshot.Value.ToString());
            if (previousPlayerTurn == 0 && !isMyTurn) // Jeœli poprzedni gracz zakoñczy³ turê i to moja kolej
            {
                StartTurn();
            }
        }
    }

    void HandlePlayerTurnIdChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.Snapshot.Exists && args.Snapshot.Value != null)
        {
            string player = args.Snapshot.Value.ToString();
            if (player != playerId)
            {
                getPlayerName(() =>
                {
                    turnPlayerName.text = "Tura: " + currentPlayerName;
                }, player);
                turnPlayerName.text = "Tura: " + currentPlayerName;
            }
        }
    }

    void HandleRoundsChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.Snapshot.Exists && args.Snapshot.Value != null)
        {
            int newRounds = int.Parse(args.Snapshot.Value.ToString());

            if (newRounds <= 0)
            {
                SceneManager.LoadScene("End Game", LoadSceneMode.Single);
            }
            else
            {
                newRounds = 11 - newRounds;
                roundText.text = "Runda: " + newRounds;
            }
        }
    }

    void FetchRoundsFromDatabase(Action<int> onRoundsFetched)
    {
        dbRefLobby.Child("rounds").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists && int.TryParse(snapshot.Value.ToString(), out int fetchedRounds))
                {
                    Debug.Log("Pobrano liczbê rund: " + fetchedRounds);
                    onRoundsFetched?.Invoke(fetchedRounds); // Przeka¿ pobran¹ wartoœæ przez callback
                }
                else
                {
                    Debug.LogWarning("Nie znaleziono liczby rund w bazie danych lub nie jest liczb¹.");
                    onRoundsFetched?.Invoke(0); // Domyœlnie 0, jeœli brak danych
                }
            }
            else
            {
                Debug.LogError("B³¹d podczas pobierania liczby rund: " + task.Exception);
                onRoundsFetched?.Invoke(0); // Domyœlnie 0 w razie b³êdu
            }
        });
    }

    void getPlayerName(Action onComplete, string currentPlayerId)
    {
        dbRef.Child(currentPlayerId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.Exists && snapshot.Child("playerName").Exists)
                {
                    currentPlayerName = snapshot.Child("playerName").Value.ToString();
                }
                else
                {
                    currentPlayerName = "Unknown"; // Ustawienie domyœlnej wartoœci
                    Debug.LogWarning("PlayerName not found.");
                }
            }
            onComplete?.Invoke();
        });
    }

    public async Task AddIncomeToBudget()
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogWarning("Player ID is not set.");
            return;
        }

        DatabaseReference playerStatsRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(DataTransfer.LobbyId)
            .Child("players")
            .Child(DataTransfer.PlayerId)
            .Child("stats");

        try
        {
            var playerStatsSnapshot = await playerStatsRef.GetValueAsync();
            if (!playerStatsSnapshot.Exists)
            {
                Debug.LogWarning("Player not found.");
                return;
            }

            int incomeAmount = playerStatsSnapshot.Child("income")?.Value != null ? Convert.ToInt32(playerStatsSnapshot.Child("income").Value) : -1;
            int currentMoney = playerStatsSnapshot.Child("money")?.Value != null ? Convert.ToInt32(playerStatsSnapshot.Child("money").Value) : -1;

            if (incomeAmount == -1 || currentMoney == -1)
            {
                Debug.LogWarning("Income or budget is null or failed to convert");
                return;
            }

            int newMoney = currentMoney + incomeAmount;

            await playerStatsRef.Child("money").SetValueAsync(newMoney);

            Debug.Log("added income to budget");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to add income to player budget: {ex.Message}");
        }
    }

    public async Task DrawCardsUntilLimit(string playerId, int targetCardCount)
    {
        string lobbyId = DataTransfer.LobbyId;

        if (string.IsNullOrEmpty(lobbyId) || string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("Lobby ID or player ID is null or empty.");
            return;
        }

        DatabaseReference playerDeckRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("deck");

        var snapshot = await playerDeckRef.GetValueAsync();
        if (!snapshot.Exists)
        {
            Debug.LogError($"Deck not found for player {playerId} in lobby {lobbyId}.");
            return;
        }

        // Pobierz obecne karty na rêce
        List<string> currentHandCards = new();
        List<string> availableCards = new();

        foreach (var cardSnapshot in snapshot.Children)
        {
            bool onHand = cardSnapshot.Child("onHand").Value as bool? ?? false;
            bool played = cardSnapshot.Child("played").Value as bool? ?? false;

            if (onHand) currentHandCards.Add(cardSnapshot.Key);
            if (!onHand && !played) availableCards.Add(cardSnapshot.Key);
        }

        int cardsToDraw = Math.Min(targetCardCount - currentHandCards.Count, availableCards.Count);

        Debug.Log($"Player {playerId} has {currentHandCards.Count} cards. Drawing {cardsToDraw} more cards.");

        if (cardsToDraw <= 0)
        {
            Debug.Log("No need to draw more cards.");
            return;
        }

        System.Random random = new();
        for (int i = 0; i < cardsToDraw; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableCards.Count);
            string selectedInstanceId = availableCards[randomIndex];

            // Pobierz ID karty i oznacz jako "onHand"
            var selectedCardSnapshot = await playerDeckRef.Child(selectedInstanceId).Child("cardId").GetValueAsync();
            if (!selectedCardSnapshot.Exists)
            {
                Debug.LogError($"CardId for instance {selectedInstanceId} not found in deck.");
                continue;
            }

            await playerDeckRef.Child(selectedInstanceId).Child("onHand").SetValueAsync(true);
            Debug.Log($"Card {selectedInstanceId} added to hand.");
            availableCards.RemoveAt(randomIndex); // Usuñ wybran¹ kartê z dostêpnych
        }
    }


    async void StartTurn()
    {
      /*  var playerRef = dbRef.Child(playerId);
        var blockTurnSnapshot = playerRef.Child("blockTurn");

        blockTurnSnapshot.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted)
            {
                var snapshot = task.Result;

                if (snapshot.Exists && Convert.ToBoolean(snapshot.Value))
                {
                    playerRef.Child("blockTurn").RemoveValueAsync();
                    PassTurn();

                }
                else
                {
                    timer = 10f;
                    turnPlayerName.text = "Twoja tura!";
                    passButton.interactable = true;
                    dbRef.Child(playerId).Child("stats").Child("playerTurn").SetValueAsync(1);
                    dbRefLobby.Child("playerTurnId").SetValueAsync(playerId);
                    isMyTurn = true;
                    DataTransfer.IsFirstCardInTurn = true;

                    turnsTaken++;
                    dbRef.Child(playerId).Child("stats").Child("turnsTaken").SetValueAsync(turnsTaken);
                }
            }
            else
            {
                Debug.LogError($"Nie uda³o siê pobraæ danych dla gracza {playerId}.");
            }
        }); 
*/
        timer = 60f;
        turnPlayerName.text = "Twoja tura!";
        await DrawCardsUntilLimit(playerId, 4);
        cardsOnHandController.ForceUpdateUI();
        if (turnsTaken > 0)
        {
            _ = AddIncomeToBudget();
        }
        passButton.interactable = true;
        await dbRef.Child(playerId).Child("stats").Child("playerTurn").SetValueAsync(1);
        turnsTaken++;
        await dbRef.Child(playerId).Child("stats").Child("turnsTaken").SetValueAsync(turnsTaken);
        await dbRefLobby.Child("playerTurnId").SetValueAsync(playerId);
        isMyTurn = true;
        DataTransfer.IsFirstCardInTurn = true;
        

    }

    void EndTurn()
    {
        timer = 60f; // Zresetuj timer
        passButton.interactable = false;
        isMyTurn = false; // Nie wyswietlaj timera
        dbRef.Child(playerId).Child("stats").Child("playerTurn").SetValueAsync(0);

        if (playerId == turnOrderList.Last())
        {
            FetchRoundsFromDatabase(fetchedRounds =>
            {
                rounds = fetchedRounds - 1; // Zmniejsz liczbê rund
                dbRefLobby.Child("rounds").SetValueAsync(rounds);
                Debug.Log("Zaktualizowano liczbê rund: " + rounds);
            });
        }
    }

    public void PassTurn()
    {
        if (timer > 0)
        {
            Debug.Log("Skipped turn");
            EndTurn();
        }
    }

    void OnDestroy()
    {
        if (!string.IsNullOrEmpty(previousPlayerId))
        {
            dbRef.Child(previousPlayerId).Child("stats").Child("playerTurn").ValueChanged -= HandlePreviousPlayerTurnChanged;
        }

        dbRefLobby.Child("playerTurnId").ValueChanged -= HandlePlayerTurnIdChanged;
        dbRefLobby.Child("rounds").ValueChanged -= HandleRoundsChanged;
    }
}
