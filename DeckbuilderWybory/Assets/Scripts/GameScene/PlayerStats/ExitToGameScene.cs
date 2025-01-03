using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Extensions;

public class ExitToGameScene : MonoBehaviour
{
    public Button backButton;
    public string sceneName;
    string lobbyId;

    DatabaseReference dbRef;

    void Start()
    {
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        lobbyId = DataTransfer.LobbyId;
        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);

        backButton.onClick.AddListener(OnButtonClicked);
    }

    void OnButtonClicked()
    {
        AudioManager audioManager = FindObjectOfType<AudioManager>();
        if (audioManager != null)
        {
            audioManager.PlayButtonClickSound();
        }

        dbRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error checking lobby existence: " + task.Exception);
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    SceneManager.UnloadSceneAsync(sceneName);
                }
                else
                {
                    SceneManager.LoadScene("Main Menu", LoadSceneMode.Single);
                }
            }
        });
    }

    void OnDestroy()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnButtonClicked);
        }
    }
}
