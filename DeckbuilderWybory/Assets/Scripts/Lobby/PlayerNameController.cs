using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Database;

public class PlayerNameController : MonoBehaviour
{
    public InputField inputField;
    public Text errorMessage;
    public GameObject sectionToChange;
    public GameObject sectionFromChange;

    private DatabaseReference dbRef;

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
            return;
        }

        bool nameExists = await CheckIfPlayerNameExists(playerName);

        if (nameExists)
        {
            bool isNameUpdated = await SetPlayerNameInDatabase(playerName);

            if (isNameUpdated)
            {
                DataTransfer.PlayerName = playerName;
                SwitchSections();
            }
            else
            {
                ShowErrorMessage("B³¹d z baz¹ danych.");
            }
        }
        else
        {
            ShowErrorMessage("To imiê jest ju¿ zajête!");
        }
    }

    private async System.Threading.Tasks.Task<bool> CheckIfPlayerNameExists(string playerName)
    {
        try
        {
            DataSnapshot snapshot = await dbRef
                .Child(DataTransfer.LobbyId)
                .Child("players")
                .OrderByChild("playerName")
                .EqualTo(playerName)
                .GetValueAsync();

            return snapshot.ChildrenCount == 0;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"B³¹d podczas sprawdzania nazwy gracza: {ex.Message}");
            ShowErrorMessage("B³¹d z baz¹ danych.");
            return false;
        }
    }

    private async System.Threading.Tasks.Task<bool> SetPlayerNameInDatabase(string playerName)
    {
        try
        {
            var playerRef = dbRef.Child(DataTransfer.LobbyId).Child("players").Child(DataTransfer.PlayerId);
            await playerRef.Child("playerName").SetValueAsync(playerName);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"B³¹d podczas zapisywania imienia gracza: {ex.Message}");
            ShowErrorMessage("B³¹d z baz¹ danych.");
            return false;
        }
    }

    private void SwitchSections()
    {
        if (sectionFromChange != null && sectionToChange != null)
        {
            sectionFromChange.SetActive(false);
            sectionToChange.SetActive(true);
        }

        if (errorMessage != null)
        {
            errorMessage.text = "";
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
