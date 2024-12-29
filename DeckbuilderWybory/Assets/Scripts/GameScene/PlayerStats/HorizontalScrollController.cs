using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using Firebase;
using System.Linq;

public class HorizontalScrollController : MonoBehaviour
{
    public GameObject statsCardPrefab;
    public Transform content;

    DatabaseReference dbRef;
    string lobbyId;
    string playerId;
    int lobbySize;

    private Dictionary<string, StatsCard> statsCards = new Dictionary<string, StatsCard>();

    void Start()
    {
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);

        dbRef.Child("players").ChildChanged += HandleChildChanged;

        FetchDataFromDatabase();
    }

    void HandleChildChanged(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (args.Snapshot.Exists)
        {
            UpdatePlayerData(args.Snapshot);
        }
    }

    void FetchDataFromDatabase()
    {
        dbRef.GetValueAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted)
            {
                Debug.LogError("Error getting data from Firebase: " + task.Exception);
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    statsCards.Clear();
                    foreach (Transform child in content)
                    {
                        Destroy(child.gameObject);
                    }

                    foreach (var childSnapshot in snapshot.Child("players").Children)
                    {
                        UpdatePlayerData(childSnapshot);
                    }

                    SortStatsCards();
                }
                else
                {
                    Debug.Log("Data does not exist in the database.");
                }
            }
        });
    }

    void UpdatePlayerData(DataSnapshot playerSnapshot)
    {
        string currentPlayerId = playerSnapshot.Key;

        string playerName = playerSnapshot.Child("playerName").Value?.ToString() ?? "Unknown";
        if (currentPlayerId == playerId)
        {
            playerName = $"Ty - {playerName}";
        }

        string playerMoney = playerSnapshot.Child("stats").Child("money").Value?.ToString() ?? "0";
        string playerIncome = playerSnapshot.Child("stats").Child("income").Value?.ToString() ?? "0";
        int turnNumber = int.Parse(playerSnapshot.Child("myTurnNumber").Value?.ToString());
        string deckType = playerSnapshot.Child("stats").Child("deckType").Value?.ToString() ?? "";

        if (string.IsNullOrEmpty(deckType))
        {
            string deckName = playerSnapshot.Child("stats").Child("deckName").Value?.ToString() ?? "";

            deckType = deckName;

            Debug.LogWarning($"Brak deckType w bazie. Odczytany z nazwy: {deckType}");
        }

        deckType = deckType switch
        {
            "srodowisko" => "ŒRODOWISKO",
            "ambasada" => "AMBASADA",
            "przemysl" => "PRZEMYS£",
            "metropolia" => "METROPOLIA",
            _ => deckType
        };

        DataSnapshot supportSnapshot = playerSnapshot.Child("stats").Child("support");
        int supportSum = 0;
        if (supportSnapshot.Exists)
        {
            foreach (var supportChild in supportSnapshot.Children)
            {
                if (int.TryParse(supportChild.Value.ToString(), out int value))
                {
                    supportSum += value;
                }
            }
        }

        string playerSupport = supportSum.ToString();

        DataSnapshot deckSnapshot = playerSnapshot.Child("deck");
        int playerCardNumber = 0;
        if (deckSnapshot.Exists)
        {
            foreach (var cardSnapshot in deckSnapshot.Children)
            {
                bool onHand = bool.TryParse(cardSnapshot.Child("onHand").Value?.ToString(), out bool isOnHand) && isOnHand;
                bool played = bool.TryParse(cardSnapshot.Child("played").Value?.ToString(), out bool isPlayed) && isPlayed;

                if (!onHand && !played)
                {
                    playerCardNumber++;
                }
            }
        }

        DataSnapshot regionsSnapshot = playerSnapshot.Child("stats").Child("support");
        string regionSupportText = "";
        int regionsNumber = 0;

        if (regionsSnapshot.Exists)
        {
            List<string> regionSupports = new List<string>();
            for (int i = 0; i < regionsSnapshot.ChildrenCount; i++)
            {
                var regionSnapshot = regionsSnapshot.Child(i.ToString());
                int regionSupport = int.TryParse(regionSnapshot.Value?.ToString(), out int value) ? value : 0;

                if (regionSupport > 0)
                {
                    regionSupports.Add($"{i + 1}:{regionSupport}%");
                    regionsNumber += 1;
                }
            }

            regionSupportText = string.Join(" ", regionSupports);
        }

        if (statsCards.ContainsKey(currentPlayerId))
        {
            statsCards[currentPlayerId].SetPlayerData(playerName, playerSupport, playerMoney, playerIncome, playerCardNumber, regionSupportText, turnNumber, deckType, regionsNumber);
        }
        else
        {
            GameObject newCard = Instantiate(statsCardPrefab, content);
            StatsCard statsCard = newCard.GetComponent<StatsCard>();

            if (statsCard != null)
            {
                statsCard.SetPlayerData(playerName, playerSupport, playerMoney, playerIncome, playerCardNumber, regionSupportText, turnNumber, deckType, regionsNumber);
                statsCards[currentPlayerId] = statsCard;
            }
            else
            {
                Debug.LogError("StatsCard component is missing from the prefab!");
            }
        }
        SortStatsCards();
    }

    void SortStatsCards()
    {
        var sortedCards = statsCards.Values
            .OrderByDescending(card => card.PlayerSupportValue)
            .ThenByDescending(card => card.RegionsNumberValue)
            .ThenByDescending(card => card.PlayerMoneyValue)
            .ToList();

        for (int i = 0; i < sortedCards.Count; i++)
        {
            sortedCards[i].transform.SetSiblingIndex(i);
        }
    }

    void OnDestroy()
    {
        if (dbRef != null)
        {
            dbRef.Child("players").ChildChanged -= HandleChildChanged;
        }
    }
}