using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.UI;

public class TextController : MonoBehaviour
{
    DatabaseReference dbRef;
    string lobbyId;
    string playerId;

    public Text moneyText;
    public Text supportText;

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

        // Ustawienie nasluchiwania zmian w stats
        dbRef.Child("stats").Child("money").ValueChanged += FetchFromDbMoney;
        dbRef.Child("stats").Child("support").ValueChanged += FetchFromDbSupport;
    }

    void OnDestroy()
    {
        // Usun nasluchiwanie
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
            moneyText.text = args.Snapshot.Value.ToString() + "k";
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
            supportText.text = args.Snapshot.Value.ToString() + "%";
        }
    }
}