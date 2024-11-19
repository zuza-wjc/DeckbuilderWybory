using UnityEngine;
using UnityEngine.UI;

public class ExitButtonHandler : MonoBehaviour
{
    // Metoda wywo�ywana po klikni�ciu przycisku wyj�cia
    public void OnExitButtonClicked()
    {
        Debug.Log("Exit button clicked");

        // Znajd� obiekt z komponentem LobbySceneController w hierarchii
        LobbySceneController lobbyController = FindObjectOfType<LobbySceneController>();

        if (lobbyController != null)
        {
            // Wywo�aj funkcj� opuszczaj�cej lobby
            lobbyController.LeaveLobby();
        }
        else
        {
            Debug.LogError("LobbySceneController not found in the scene!");
        }
    }
}
