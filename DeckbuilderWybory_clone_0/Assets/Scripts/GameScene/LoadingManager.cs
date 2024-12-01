using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Database;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    public string nextSceneName = "Game";
    public DeckController deckController;
    public Image loadingSpinner;

    private string lobbyId = DataTransfer.LobbyId;
    private string playerId = DataTransfer.PlayerId;

    DatabaseReference dbRef;
    DatabaseReference dbRefLobby;

    private int readyPlayers;

    private bool isLoading = false;

    private void Update()
    {
        if (isLoading && loadingSpinner != null)
        {
            loadingSpinner.transform.Rotate(500 * Time.deltaTime * Vector3.forward);
        }
    }

    private async void Start()
    {
        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");
        dbRefLobby = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);

        ShowLoadingAnimation(true);

        float startTime = Time.time;

        await GetReadyPlayersFromDatabaseAsync();
        await SetPlayerBudgetAsync();
        await LoadPlayerCardsAsync();
        await SetPlayerInGameAsync();
        await AssignTurnOrderAsync();

        float elapsedTime = Time.time - startTime;
        float waitTime = Mathf.Max(3f - elapsedTime, 0f);

        await Task.Delay((int)(waitTime * 1000));

        await LoadSceneAsync();

    }

    private void ShowLoadingAnimation(bool isLoading)
    {
        this.isLoading = isLoading;
        if (loadingSpinner != null)
        {
            loadingSpinner.gameObject.SetActive(isLoading);
        }
    }

    private async Task GetReadyPlayersFromDatabaseAsync()
    {
        try
        {
            DataSnapshot snapshot = await dbRefLobby.GetValueAsync();
            if (snapshot.Exists && snapshot.Child("readyPlayers").Exists)
            {
                readyPlayers = int.Parse(snapshot.Child("readyPlayers").Value.ToString());
            }
            else
            {
                readyPlayers = 0;
                Debug.LogWarning("Ready players count not found. Defaulting to 0.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error fetching ready players: " + ex.Message);
            readyPlayers = 0;
        }
    }

    private async Task SetPlayerBudgetAsync()
    {
        int budget;

        if (readyPlayers >= 2 && readyPlayers <= 3)
        {
            budget = 50;
        }
        else if (readyPlayers >= 4 && readyPlayers <= 6)
        {
            budget = 70;
        }
        else if (readyPlayers >= 7 && readyPlayers <= 8)
        {
            budget = 90;
        }
        else
        {
            Debug.LogWarning("Unexpected number of ready players. Setting default budget to 50.");
            budget = 50;
        }

        try
        {
            await dbRef.Child(playerId).Child("stats").Child("money").SetValueAsync(budget);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error setting player budget: " + ex.Message);
        }
    }

    private async Task LoadPlayerCardsAsync()
    {
       
            if (deckController != null)
            {
                await deckController.InitializeDeck();
            }
            else
            {
                Debug.LogError("DeckController not found!");
            }
     
    }

    private async Task AssignTurnOrderAsync()
    {
        try
        {
            DataSnapshot snapshot = await dbRef.GetValueAsync();
            if (snapshot.Exists)
            {
                List<DataSnapshot> playersList = snapshot.Children.ToList();
                playersList.Sort((a, b) => string.Compare(
                    a.Child("playerName").Value.ToString(),
                    b.Child("playerName").Value.ToString(),
                    StringComparison.Ordinal));

                for (int i = 0; i < playersList.Count; i++)
                {
                    string playerKey = playersList[i].Key;
                    await dbRef.Child(playerKey).Child("myTurnNumber").SetValueAsync(i + 1);
                }

            }
            else
            {
                Debug.LogWarning("No players found in the lobby.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error assigning turn order: " + ex.Message);
        }
    }

    private async Task SetPlayerInGameAsync()
    {
        try
        {
            await dbRef.Child(playerId).Child("stats").Child("inGame").SetValueAsync(true);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error setting player inGame status: " + ex.Message);
        }
    }

    private async Task LoadSceneAsync()
    {
        ShowLoadingAnimation(false);

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);

        while (!asyncLoad.isDone)
        {
            await Task.Yield();
        }

    }
}
