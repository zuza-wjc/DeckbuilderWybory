using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Database;

public class PlayerNameController : MonoBehaviour
{
    public InputField inputField;
    public Text errorMessage;
    public GameObject namePanel;

    private DatabaseReference dbRef;

    public System.Action<bool> OnSubmitCallback;

    void Start()
    {
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions");
    }

    public async void OnSubmit()
    {
        string playerName = inputField.text;

        if (string.IsNullOrEmpty(playerName))
        {
            ShowErrorMessage("Pole nie mo¿e byæ puste!");
            OnSubmitCallback?.Invoke(false);
            return;
        }

        bool nameExists = await CheckIfPlayerNameExists(playerName);

        if (nameExists)
        {
            ShowErrorMessage("To imiê jest ju¿ zajête!");
            OnSubmitCallback?.Invoke(false);
        }
        else
        {
            DataTransfer.PlayerName = playerName;
            OnSubmitCallback?.Invoke(true);
            CloseNamePanel();
        }
    }

    private async System.Threading.Tasks.Task<bool> CheckIfPlayerNameExists(string playerName)
    {
        try
        {
            if (string.IsNullOrEmpty(DataTransfer.LobbyId))
            {
                return false;
            }

            DataSnapshot snapshot = await dbRef
                .Child(DataTransfer.LobbyId)
                .Child("players")
                .OrderByChild("playerName")
                .EqualTo(playerName)
                .GetValueAsync();

            return snapshot.ChildrenCount > 0;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"B³¹d podczas sprawdzania nazwy gracza: {ex.Message}");
            ShowErrorMessage("B³¹d z baz¹ danych.");
            return true;
        }
    }

    private void CloseNamePanel()
    {
        if (namePanel != null)
        {
            namePanel.SetActive(false);
        }
    }

    private void ShowErrorMessage(string message)
    {
        if (errorMessage != null)
        {
            errorMessage.text = message;
        }
    }
}
