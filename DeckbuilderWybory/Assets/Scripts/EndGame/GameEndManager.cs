using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Threading.Tasks;

public class GameEndManager : MonoBehaviour
{
    private DatabaseReference dbRefLobby;
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

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");
        dbRefLobby = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);
    }

    public void EndGame()
    {
        _ = CheckLobby();
    }

    private async Task CheckLobby()
    {
        try
        {
            var lobbySnapshot = await dbRefLobby.GetValueAsync();
            if (lobbySnapshot == null || !lobbySnapshot.Exists)
            {
                Debug.LogWarning("Lobby does not exist. Returning to Main Menu.");
                ChangeToScene();
                return;
            }

            await CheckAndHandleEndGame();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error checking lobby: {ex.Message}");
            ChangeToScene();
        }
    }

    private async Task CheckAndHandleEndGame()
    {
        try
        {
            // Ustaw swojego gracza na "nie w grze"
            await dbRef.Child(playerId).Child("stats").Child("inGame").SetValueAsync(false);

            // Wykonaj transakcjê na wêŸle ca³ego lobby
            await dbRefLobby.RunTransaction(mutableData =>
            {
                var players = mutableData.Child("players").Children;

                bool otherPlayersInGame = false;

                foreach (var child in players)
                {
                    string currentPlayerId = child.Key;
                    if (currentPlayerId != playerId)
                    {
                        bool inGame = child.Child("stats").Child("inGame").Value as bool? ?? false;
                        if (inGame)
                        {
                            otherPlayersInGame = true;
                            break;
                        }
                    }
                }

                // Jeœli nikt nie jest w grze, usuñ ca³e lobby
                if (!otherPlayersInGame)
                {
                    mutableData.Value = null; // Usuwa ca³y wêze³ lobby
                }

                return TransactionResult.Success(mutableData); // Zwróæ wynik transakcji
            });

            ChangeToScene();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error handling end game: {ex.Message}");
            ChangeToScene();
        }
    }

    private void ChangeToScene()
    {
        SceneManager.LoadScene("Main Menu", LoadSceneMode.Single);
    }

    private async Task RemoveSession()
    {
        try
        {
            if (dbRefLobby != null)
            {
                await dbRefLobby.RemoveValueAsync();
                Debug.Log("Session removed successfully.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to remove session: {ex.Message}");
        }
    }
}