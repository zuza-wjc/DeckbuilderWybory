using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using Firebase;

public class HorizontalScrollController : MonoBehaviour
{
    public GameObject statsCardPrefab;
    public Transform content;

    DatabaseReference dbRef;
    string lobbyId;
    string playerId;
    int lobbySize;

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

        FetchDataFromDatabase();
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
                    List<(string, string, string, string, int, string, int, string)> playersData = new List<(string, string, string, string, int, string, int, string)>();
                    (string, string, string, string, int, string, int, string)? currentPlayerData = null;

                    foreach (var childSnapshot in snapshot.Child("players").Children)
                    {
                        string currentPlayerId = childSnapshot.Key;

                        string playerName = childSnapshot.Child("playerName").Value?.ToString() ?? "Unknown";

                        if (currentPlayerId == playerId)
                        {
                            playerName = "Ty";
                        }

                        string playerMoney = childSnapshot.Child("stats").Child("money").Value?.ToString() ?? "0";
                        string playerIncome = childSnapshot.Child("stats").Child("income").Value?.ToString() ?? "0";
                        int turnNumber = int.Parse(childSnapshot.Child("myTurnNumber").Value?.ToString());
                        string deckType = childSnapshot.Child("stats").Child("deckType").Value?.ToString() ?? "";

                        if(deckType == "srodowisko")
                        {
                            deckType = "ŒRODOWISKO";
                        }
                        if (deckType == "ambasada")
                        {
                            deckType = "AMBASADA";
                        }
                        if (deckType == "przemysl")
                        {
                            deckType = "PRZEMYS£";
                        }
                        if (deckType == "metropolia")
                        {
                            deckType = "METROPOLIA";
                        }

                        DataSnapshot supportSnapshot = childSnapshot.Child("stats").Child("support");
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

                        DataSnapshot deckSnapshot = childSnapshot.Child("deck");
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

                        DataSnapshot regionsSnapshot = childSnapshot.Child("stats").Child("support");
                        string regionSupportText = "";

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
                                }
                            }

                            regionSupportText = string.Join(" ", regionSupports);
                        }

                        if (currentPlayerId == playerId)
                        {
                            currentPlayerData = (playerName, playerSupport, playerMoney, playerIncome, playerCardNumber, regionSupportText, turnNumber, deckType);
                        }
                        else
                        {
                            playersData.Add((playerName, playerSupport, playerMoney, playerIncome, playerCardNumber, regionSupportText, turnNumber, deckType));
                        }
                    }

                    if (currentPlayerData.HasValue)
                    {
                        playersData.Insert(0, currentPlayerData.Value);
                    }

                    if (int.TryParse(snapshot.Child("lobbySize").Value.ToString(), out lobbySize))
                    {
                        for (int i = 0; i < playersData.Count && i < lobbySize; i++)
                        {
                            var playerData = playersData[i];
                            AddStats(playerData.Item1, playerData.Item2, playerData.Item3, playerData.Item4, playerData.Item5, playerData.Item6, playerData.Item7, playerData.Item8);
                        }
                    }
                    else
                    {
                        Debug.LogError("Failed to parse lobbySize.");
                    }
                }
                else
                {
                    Debug.Log("Data does not exist in the database.");
                }
            }
        });
    }


    void AddStats(string playerName, string playerSupport, string playerMoney, string playerIncome, int playerCardNumber, string regionSupportText, int turnNumber, string deckType)
    {
        if (statsCardPrefab == null || content == null)
        {
            Debug.LogError("StatsCard prefab or Content is not assigned!");
            return;
        }

        GameObject newCard = Instantiate(statsCardPrefab, content);
        StatsCard statsCard = newCard.GetComponent<StatsCard>();

        if (statsCard != null)
        {
            statsCard.SetPlayerData(playerName, playerSupport, playerMoney, playerIncome, playerCardNumber, regionSupportText, turnNumber, deckType);
        }
        else
        {
            Debug.LogError("StatsCard component is missing from the prefab!");
        }
    }
}
