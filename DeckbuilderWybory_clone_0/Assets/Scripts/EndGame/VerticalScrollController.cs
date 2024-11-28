using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Linq;

public class VerticalScrollController : MonoBehaviour
{
    public GameObject playerRankPrefab;
    public Transform content;

    private DatabaseReference dbRef;
    private string lobbyId;
    private string playerId;

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
        dbRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
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
                    List<(string playerId, string playerName, int support, int regions, int money)> playersData = new List<(string, string, int, int, int)>();

                    foreach (var childSnapshot in snapshot.Child("players").Children)
                    {
                        string currentPlayerId = childSnapshot.Key;
                        string playerName = childSnapshot.Child("playerName").Value?.ToString() ?? "Unknown";
                        if (currentPlayerId == playerId) playerName = "Ty";

                        int money = int.TryParse(childSnapshot.Child("stats").Child("money").Value?.ToString(), out int m) ? m : 0;
                        int support = GetTotalSupport(childSnapshot.Child("stats").Child("support"));
                        int regions = GetSupportedRegionsCount(childSnapshot.Child("stats").Child("support"));

                        playersData.Add((currentPlayerId, playerName, support, regions, money));
                    }

                    // Sortowanie graczy wed³ug: poparcia -> regionów -> pieniêdzy
                    playersData = playersData.OrderByDescending(p => p.support)
                                             .ThenByDescending(p => p.regions)
                                             .ThenByDescending(p => p.money)
                                             .ToList();

                    // Dodawanie graczy do rankingu z przypisanym miejscem
                    int currentRank = 1;
                    for (int i = 0; i < playersData.Count; i++)
                    {
                        var playerData = playersData[i];

                        // Jeœli poprzedni gracz mia³ takie same wartoœci, przypisz ten sam rank
                        if (i > 0 && playersData[i - 1].support == playerData.support &&
                                     playersData[i - 1].regions == playerData.regions &&
                                     playersData[i - 1].money == playerData.money)
                        {
                            // Przypisz ten sam ranking co poprzedni gracz
                            AddStats(currentRank, playerData.playerName, playerData.support, playerData.regions, playerData.money);
                        }
                        else
                        {
                            // Jeœli nie, to przypisz nowy ranking
                            currentRank = i + 1;
                            AddStats(currentRank, playerData.playerName, playerData.support, playerData.regions, playerData.money);
                        }
                    }
                }
                else
                {
                    Debug.Log("No player data found in the database.");
                }
            }
        });
    }

    int GetTotalSupport(DataSnapshot supportSnapshot)
    {
        int totalSupport = 0;

        if (supportSnapshot.Exists)
        {
            foreach (var supportChild in supportSnapshot.Children)
            {
                if (int.TryParse(supportChild.Value.ToString(), out int value))
                {
                    totalSupport += value;
                }
            }
        }

        return totalSupport;
    }

    int GetSupportedRegionsCount(DataSnapshot supportSnapshot)
    {
        int regionCount = 0;

        if (supportSnapshot.Exists)
        {
            foreach (var supportChild in supportSnapshot.Children)
            {
                if (int.TryParse(supportChild.Value.ToString(), out int value) && value > 0)
                {
                    regionCount++;
                }
            }
        }

        return regionCount;
    }

    void AddStats(int rank, string playerName, int playerSupport, int regions, int playerMoney)
    {
        if (playerRankPrefab == null || content == null)
        {
            Debug.LogError("PlayerRank prefab or Content is not assigned!");
            return;
        }

        GameObject newCard = Instantiate(playerRankPrefab, content);
        PlayerRank playerRank = newCard.GetComponent<PlayerRank>();

        if (playerRank != null)
        {
            playerRank.SetPlayerData(rank, playerName, playerSupport.ToString(), regions, playerMoney.ToString());
        }
        else
        {
            Debug.LogError("PlayerRank component is missing from the prefab!");
        }
    }
}
