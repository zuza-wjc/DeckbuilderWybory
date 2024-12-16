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
    public Button firstPassButton;
    public Button passButton;
    public GameObject passTurnPanel;

    public Button yesSellButton;

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
    private string lastInTurnPlayerId;

    public CardOnHandController cardsOnHandController;

    void Start()
    {
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        yesSellButton.interactable = false;

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");
        dbRefLobby = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);

        firstPassButton.interactable = false;
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
        yield return DistributeInitialCards();

        yield return StartCoroutine(InitializeTurnOrderCoroutine());

        if (playerId != turnOrderList[0])
        {
            yield return StartCoroutine(SetTurnPlayerName(turnOrderList[0]));

            //Debug.Log("Tura gracza: " + turnOrderList[0]);
        }
        else
        {
            turnPlayerName.text = "Twoja tura!";
            yesSellButton.interactable = true;
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
                                Debug.LogWarning($"Nie uda�o si� sparsowa� kolejno�ci tury gracza {id}.");
                            }
                        }
                        else
                        {
                            Debug.Log(id + "jest NULL");
                        }
                    }

                    turnOrderList = players.OrderBy(p => p.Value).Select(p => p.Key).ToList();

                    lastInTurnPlayerId = turnOrderList.Last();

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

            isCompleted = true; // Oznacz jako uko�czone
        });

        // Czekaj, a� `isCompleted` b�dzie true
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
            if (previousPlayerTurn == 0 && !isMyTurn) // Je�li poprzedni gracz zako�czy� tur� i to moja kolej
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
            if (player != playerId && player != "None")
            {
                StartCoroutine(SetTurnPlayerName(player));
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
                int displayRounds = 11 - newRounds;
                roundText.text = "Runda: " + displayRounds;
                Debug.Log($"Zaktualizowano wy�wietlan� rund�: {displayRounds}");
            }
        }
    }

    IEnumerator SetTurnPlayerName(string currentPlayerId)
    {
        if (currentPlayerId == playerId)
        {
            turnPlayerName.text = "Twoja tura!";
            yield break; // Przerywamy dalsze wykonywanie
        }

        bool isCompleted = false;

        getPlayerName(() =>
        {
            turnPlayerName.text = "Tura: " + currentPlayerName;
            isCompleted = true; // Oznacz jako zako�czone
        }, currentPlayerId);

        while (!isCompleted)
        {
            yield return null;
        }
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
                    currentPlayerName = "Unknown"; // Ustawienie domy�lnej warto�ci
                    Debug.LogWarning("PlayerName not found.");
                }
            }
            else
            {
                Debug.LogError("Failed to retrieve player name: " + task.Exception);
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

        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to add income to player budget: {ex.Message}");
        }
    }
    
    public async Task DistributeInitialCards()
    {
       // Debug.Log("Rozpoczynam rozdawanie pocz�tkowych kart.");

        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("Lobby ID is null or empty.");
            return;
        }

        await DrawCardsUntilLimit(playerId, 4); // Przydziel graczowi do 4 kart

       // Debug.Log("Zako�czono rozdawanie pocz�tkowych kart.");
        cardsOnHandController.ForceUpdateUI();
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

        // Pobierz obecne karty na r�ce
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

        if (cardsToDraw <= 0)
        {
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
            availableCards.RemoveAt(randomIndex); // Usu� wybran� kart� z dost�pnych
        }
    }

    public async Task<int> GetCardLimit()
    {
        try
        {
            var cardLimitRef = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(playerId)
                .Child("drawCardsLimit");

            var snapshot = await cardLimitRef.GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError($"drawCardsLimit for player {DataTransfer.PlayerId} does not exist.");
                return 4;
            }

            int currentLimit = int.Parse(snapshot.Value.ToString());

            if (currentLimit != 4)
            {
                await cardLimitRef.SetValueAsync(4);
            }

            return currentLimit;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in GetCardLimit: {ex.Message}");
            return 4;
        }
    }

    async void StartTurn()
    {
        timer = 60f;
        turnPlayerName.text = "Twoja tura!";
        if (turnsTaken > 0)
        {
            _ = AddIncomeToBudget();
        }
        firstPassButton.interactable = true;
        await dbRef.Child(playerId).Child("stats").Child("playerTurn").SetValueAsync(1);
        turnsTaken++;
        await dbRef.Child(playerId).Child("stats").Child("turnsTaken").SetValueAsync(turnsTaken);
        await dbRefLobby.Child("playerTurnId").SetValueAsync(playerId);
        isMyTurn = true;
        DataTransfer.IsFirstCardInTurn = true;
        yesSellButton.interactable = true;
    }

    public async void EndTurn()
    {
        SetPassTurnPanelInactive();
        timer = 60f;
        int cardLimit = await GetCardLimit();
        await DrawCardsUntilLimit(playerId, cardLimit);
        cardsOnHandController.ForceUpdateUI();
        firstPassButton.interactable = false;
        yesSellButton.interactable = false;
        isMyTurn = false;
        await dbRef.Child(playerId).Child("stats").Child("playerTurn").SetValueAsync(0);

        if (playerId == lastInTurnPlayerId)
        {
            var roundsSnapshot = await dbRefLobby.Child("rounds").GetValueAsync();
            if (roundsSnapshot.Exists && int.TryParse(roundsSnapshot.Value.ToString(), out int currentRounds))
            {
                int updatedRounds = currentRounds - 1;
                await dbRefLobby.Child("rounds").SetValueAsync(updatedRounds);
               // Debug.Log("Zaktualizowano liczb� rund: " + updatedRounds);
            }
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

    public void SetPassTurnPanelInactive()
    {
        if (passTurnPanel != null)
        {
            passTurnPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("PassTurnPanel nie jest przypisany!");
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
