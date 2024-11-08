using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
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

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId);

        // Ustawienie nas³uchiwania zmian w stats
        dbRef.Child("stats").Child("money").ValueChanged += FetchFromDbMoney;
        dbRef.Child("stats").Child("support").ValueChanged += FetchFromDbSupport;
    }

    void OnDestroy()
    {
        // Unregister listeners to prevent interference
        dbRef.Child("stats").Child("money").ValueChanged -= FetchFromDbMoney;
        dbRef.Child("stats").Child("support").ValueChanged -= FetchFromDbSupport;
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
