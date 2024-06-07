using UnityEngine;
using UnityEngine.UI;

public class ExitButtonHandler : MonoBehaviour
{
    // Metoda wywo³ywana po klikniêciu przycisku wyjœcia
    public void OnExitButtonClicked()
    {
        Debug.Log("Exit button clicked");

        // ZnajdŸ obiekt z komponentem LobbySceneController w hierarchii
        LobbySceneController lobbyController = FindObjectOfType<LobbySceneController>();

        if (lobbyController != null)
        {
            // Wywo³aj funkcjê opuszczaj¹cej lobby
            lobbyController.LeaveLobby();
        }
        else
        {
            Debug.LogError("LobbySceneController not found in the scene!");
        }
    }
}
