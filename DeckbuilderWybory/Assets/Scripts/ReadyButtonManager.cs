using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Database;

public class ReadyButtonManager : MonoBehaviour
{
    public Button readyButton;
    public Image buttonImage; // Obrazek przycisku

    DatabaseReference dbRef;
    string lobbyId;
    string playerId;

    public void Initialize(string lobbyId, string playerId)
    {
        this.lobbyId = lobbyId;
        this.playerId = playerId;

        readyButton.onClick.AddListener(ToggleReady);

        // Inicjalizacja referencji do bazy danych Firebase
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId);
    }

    void ToggleReady()
    {
        // Inwersja koloru obrazka
        buttonImage.color = buttonImage.color == Color.green ? Color.red : Color.green;

        // Aktualizacja wartoœci "ready" w bazie danych
        bool newReadyState = buttonImage.color == Color.green;
        dbRef.Child("ready").SetValueAsync(newReadyState);
    }
}
