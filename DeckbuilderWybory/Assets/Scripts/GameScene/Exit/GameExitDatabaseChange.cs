using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameExitDatabaseChange : MonoBehaviour
{
    DatabaseReference dbRef;

    string lobbyId;
    string playerId;

    public Button backButton;
    public GameObject exitPanel;

    void Start()
    {
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");

        backButton.onClick.AddListener(ShowExitPanel);
    }

    void ShowExitPanel()
    {
        exitPanel.SetActive(true);
        exitPanel.transform.SetAsLastSibling();
    }

    public void ToggleInGame()
    {
        dbRef.Child(playerId).Child("stats").Child("inGame").SetValueAsync(false);
        SceneManager.LoadScene("Main Menu", LoadSceneMode.Single);
        exitPanel.SetActive(false);
    }

    public void CloseExitPanel()
    {
        exitPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(ShowExitPanel);
        }
    }
}
