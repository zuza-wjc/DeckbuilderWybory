using Firebase;
using Firebase.Database;
using TMPro;
using UnityEngine;

public class TextController : MonoBehaviour
{
    DatabaseReference dbRef;
    string lobbyId;
    string playerId;

    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI supportText;

    void Start()
    {
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        if (string.IsNullOrEmpty(lobbyId) || string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("LobbyId or PlayerId is not assigned properly!");
            return;
        }

        if (moneyText == null || supportText == null)
        {
            Debug.LogError("TextMeshProUGUI components are not assigned in the Inspector!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId);

        dbRef.Child("stats").Child("money").ValueChanged += FetchFromDbMoney;
        dbRef.Child("stats").Child("support").ValueChanged += FetchFromDbSupport;
    }

    void OnDestroy()
    {
        if (dbRef != null)
        {
            dbRef.Child("stats").Child("money").ValueChanged -= FetchFromDbMoney;
            dbRef.Child("stats").Child("support").ValueChanged -= FetchFromDbSupport;
        }
    }

    void FetchFromDbMoney(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (args.Snapshot.Exists)
        {
            moneyText.text = args.Snapshot.Value.ToString();
        }
    }
    void FetchFromDbSupport(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (args.Snapshot.Exists)
        {
            supportText.text = args.Snapshot.Value.ToString();
        }
    }
}
