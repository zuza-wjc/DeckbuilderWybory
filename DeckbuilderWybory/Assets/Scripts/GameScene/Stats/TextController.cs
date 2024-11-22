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
    public Text incomeText;

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
        dbRef.Child("stats").Child("income").ValueChanged += FetchFromDbIncome;
    }

    void OnDestroy()
    {
        // Usun nasluchiwanie
        dbRef.Child("stats").Child("money").ValueChanged -= FetchFromDbMoney;
        dbRef.Child("stats").Child("support").ValueChanged -= FetchFromDbSupport;
        dbRef.Child("stats").Child("income").ValueChanged -= FetchFromDbIncome;
    }

    void FetchFromDbIncome(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (args.Snapshot.Exists)
        {
            incomeText.text = "+" + args.Snapshot.Value.ToString() + "k";
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
            int sum = 0;

            // Iteruj przez wszystkie dzieci wêz³a "support" i sumuj wartoœci
            foreach (var child in args.Snapshot.Children)
            {
                if (int.TryParse(child.Value.ToString(), out int value))
                {
                    sum += value;
                }
            }

            // Aktualizuj tekst sumy wsparcia
            supportText.text = sum + "%";
        }
        else
        {
            supportText.text = "0%";
        }
    }
}