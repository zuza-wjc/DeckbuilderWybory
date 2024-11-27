using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using UnityEngine;
public class PlayerListManager : MonoBehaviour
{
    private DatabaseReference dbRefPlayers;
    private string lobbyId;
    private string playerId;
    private Dictionary<string, string> playerNameToIdMap = new Dictionary<string, string>();
    public GameObject playerListPanel;
    public GameObject buttonTemplate;
    public GameObject scrollViewContent;

    private Action<string> TaskOnClickCompleted;

    public void Initialize(string lobbyId, string playerId)
    {
        this.lobbyId = lobbyId;
        this.playerId = playerId;
    }

    public async Task<string> SelectEnemyPlayer()
    {
        ClearOldButtons();
        await FetchPlayersList();
        return await WaitForEnemySelection();
    }

    public async Task<string> SelectEnemyPlayerInArea(int areaId)
    {
        ClearOldButtons();
        await FetchPlayersListInArea(areaId);
        return await WaitForEnemySelection();
    }

    private void ClearOldButtons()
    {

        foreach (Transform child in scrollViewContent.transform)
        {
            if (child != buttonTemplate.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private async Task FetchPlayersList()
    {
        dbRefPlayers = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);
        var snapshot = await dbRefPlayers.GetValueAsync();

        if (snapshot.Exists)
        {
            foreach (var childSnapshot in snapshot.Child("players").Children)
            {
                string otherPlayerId = childSnapshot.Key;
                if (otherPlayerId != playerId)
                {
                    string otherPlayerName = childSnapshot.Child("playerName").Value.ToString();
                    playerNameToIdMap[otherPlayerName] = otherPlayerId;

                    CreateButton(otherPlayerName);
                }
            }

            playerListPanel.SetActive(true);
            playerListPanel.transform.SetAsLastSibling();
        }
        else
        {
            Debug.LogError("No player data found in the database.");
        }
    }

    private async Task FetchPlayersListInArea(int areaId)
    {
        dbRefPlayers = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);
        var snapshot = await dbRefPlayers.GetValueAsync();

        if (snapshot.Exists)
        {
            foreach (var childSnapshot in snapshot.Child("players").Children)
            {
                string otherPlayerId = childSnapshot.Key;
                if (otherPlayerId != playerId)
                {
                    string otherPlayerName = childSnapshot.Child("playerName").Value.ToString();

                    var supportSnapshot = childSnapshot.Child("stats").Child("support");

                    if (supportSnapshot.Exists)
                    {
                        int supportValue = 0;

                        var areaSupport = supportSnapshot.Child(areaId.ToString());
                        if (areaSupport.Exists && int.TryParse(areaSupport.Value.ToString(), out supportValue) && supportValue > 0)
                        {
                            playerNameToIdMap[otherPlayerName] = otherPlayerId;
                            CreateButton(otherPlayerName);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Player {otherPlayerId} has no support data.");
                    }
                }
            }

            playerListPanel.SetActive(true);
            playerListPanel.transform.SetAsLastSibling();
        }
        else
        {
            Debug.LogError("No player data found in the database.");
        }
    }


    private void CreateButton(string otherPlayerName)
    {
        GameObject button = Instantiate(buttonTemplate, scrollViewContent.transform);
        button.SetActive(true);
        button.GetComponentInChildren<UnityEngine.UI.Text>().text = otherPlayerName;

        button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => TaskOnClick(otherPlayerName));
    }

    private Task<string> WaitForEnemySelection()
    {
        var tcs = new TaskCompletionSource<string>();
        TaskOnClickCompleted = (selectedEnemyId) => tcs.TrySetResult(selectedEnemyId);
        return tcs.Task;
    }

    public void TaskOnClick(string otherPlayerName)
    {
        if (playerNameToIdMap.TryGetValue(otherPlayerName, out string otherPlayerId))
        {
            TaskOnClickCompleted?.Invoke(otherPlayerId);
        }
        else
        {
            Debug.LogError($"PlayerId not found for the given playerName: {otherPlayerName}");
        }

        playerListPanel.SetActive(false);
    }
}
