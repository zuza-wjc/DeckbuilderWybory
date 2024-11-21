using System.Collections;
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
    int lobbySize;

    void Start()
    {
        lobbyId = DataTransfer.LobbyId;

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
                    List<(string, string, string)> playersData = new List<(string, string, string)>();

                    foreach (var childSnapshot in snapshot.Child("players").Children)
                    {
                        string playerName = childSnapshot.Child("playerName").Value?.ToString() ?? "Unknown";
                        string playerSupport = childSnapshot.Child("stats").Child("support").Value?.ToString() ?? "0";
                        string playerMoney = childSnapshot.Child("stats").Child("money").Value?.ToString() ?? "0";

                        playersData.Add((playerName, playerSupport, playerMoney));
                    }

                    if (int.TryParse(snapshot.Child("lobbySize").Value.ToString(), out lobbySize))
                    {
                        for (int i = 0; i < playersData.Count && i < lobbySize; i++)
                        {
                            var playerData = playersData[i];
                            AddStats(playerData.Item1, playerData.Item2, playerData.Item3);
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

    void AddStats(string playerName, string playerSupport, string playerMoney)
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
            statsCard.SetPlayerData(playerName, playerSupport, playerMoney);
        }
        else
        {
            Debug.LogError("StatsCard component is missing from the prefab!");
        }
    }
}