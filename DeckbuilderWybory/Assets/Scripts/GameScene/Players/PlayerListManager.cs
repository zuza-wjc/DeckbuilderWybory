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
        try
        {
            ClearOldButtons();
            await FetchPlayersList();
            return await WaitForEnemySelection();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error selecting enemy player: {ex.Message}");
            return null;
        }
    }

    public async Task<string> SelectEnemyPlayerInArea(int areaId)
    {
        try
        {
            ClearOldButtons();
            await FetchPlayersListInArea(areaId);
            return await WaitForEnemySelection();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error selecting enemy player in area {areaId}: {ex.Message}");
            return null;
        }
    }

    private void ClearOldButtons()
    {
        try
        {
            foreach (Transform child in scrollViewContent.transform)
            {
                if (child != buttonTemplate.transform)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error clearing old buttons: {ex.Message}");
        }
    }

    private async Task FetchPlayersList()
    {
        try
        {
            dbRefPlayers = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);
            var snapshot = await dbRefPlayers.GetValueAsync();

            if (snapshot.Exists)
            {
                var playersSnapshot = snapshot.Child("players");
                if (!playersSnapshot.Exists) return;

                foreach (var childSnapshot in playersSnapshot.Children)
                {
                    string otherPlayerId = childSnapshot.Key;

                    if (otherPlayerId == playerId) continue;

                    string otherPlayerName = childSnapshot.Child("playerName")?.Value?.ToString();
                    if (string.IsNullOrEmpty(otherPlayerName)) continue;

                    playerNameToIdMap[otherPlayerName] = otherPlayerId;
                    CreateButton(otherPlayerName);
                }

                UpdatePlayerListPanel();
            }
            else
            {
                Debug.LogError("No player data found in the database.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error fetching players list: {ex.Message}");
        }
    }

    private void UpdatePlayerListPanel()
    {
        if (playerListPanel != null)
        {
            playerListPanel.SetActive(true);
            playerListPanel.transform.SetAsLastSibling();
        }
    }

    private async Task FetchPlayersListInArea(int areaId)
    {
        try
        {
            dbRefPlayers = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);
            var snapshot = await dbRefPlayers.GetValueAsync();

            if (snapshot.Exists)
            {
                var playersSnapshot = snapshot.Child("players");
                if (!playersSnapshot.Exists) return;

                foreach (var childSnapshot in playersSnapshot.Children)
                {
                    string otherPlayerId = childSnapshot.Key;
                    if (otherPlayerId == playerId) continue;

                    string otherPlayerName = childSnapshot.Child("playerName")?.Value?.ToString();
                    if (string.IsNullOrEmpty(otherPlayerName)) continue;

                    var supportSnapshot = childSnapshot.Child("stats").Child("support");
                    if (!supportSnapshot.Exists) continue;

                    var areaSupport = supportSnapshot.Child(areaId.ToString());
                    if (areaSupport.Exists && int.TryParse(areaSupport.Value?.ToString(), out int supportValue) && supportValue > 0)
                    {
                        playerNameToIdMap[otherPlayerName] = otherPlayerId;
                        CreateButton(otherPlayerName);
                    }
                }

                UpdatePlayerListPanel();
            }
            else
            {
                Debug.LogError("No player data found in the database.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error fetching players list for area {areaId}: {ex.Message}");
        }
    }

    private void CreateButton(string otherPlayerName)
    {
        if (string.IsNullOrEmpty(otherPlayerName)) return;

        try
        {
            GameObject button = Instantiate(buttonTemplate, scrollViewContent.transform);
            button.SetActive(true);

            var textComponent = button.GetComponentInChildren<UnityEngine.UI.Text>();
            if (textComponent != null)
            {
                textComponent.text = otherPlayerName;
            }

            var buttonComponent = button.GetComponent<UnityEngine.UI.Button>();
            if (buttonComponent != null)
            {
                buttonComponent.onClick.RemoveAllListeners();
                buttonComponent.onClick.AddListener(() => TaskOnClick(otherPlayerName));
            }
            else
            {
                Debug.LogWarning("Button component not found on the instantiated button.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating button for {otherPlayerName}: {ex.Message}");
        }
    }

    private Task<string> WaitForEnemySelection()
    {
        var tcs = new TaskCompletionSource<string>();

        try
        {
            TaskOnClickCompleted = (selectedEnemyId) =>
            {
                if (!string.IsNullOrEmpty(selectedEnemyId))
                {
                    tcs.TrySetResult(selectedEnemyId);
                }
                else
                {
                    tcs.TrySetException(new ArgumentException("Selected enemy ID is null or empty."));
                }
            };

            return tcs.Task;
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
            return tcs.Task;
        }
    }

    public void TaskOnClick(string otherPlayerName)
    {
        try
        {
            if (playerNameToIdMap.TryGetValue(otherPlayerName, out string otherPlayerId))
            {
                TaskOnClickCompleted?.Invoke(otherPlayerId);
            }
            else
            {
                Debug.LogError($"PlayerId not found for the given playerName: {otherPlayerName}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in TaskOnClick for {otherPlayerName}: {ex.Message}");
        }
        finally
        {
            playerListPanel.SetActive(false);
        }
    }

}
