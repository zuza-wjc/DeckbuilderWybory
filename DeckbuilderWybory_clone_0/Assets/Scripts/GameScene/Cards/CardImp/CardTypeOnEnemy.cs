using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CardTypeOnEnemy : MonoBehaviour
{
    DatabaseReference dbRef;
    string lobbyId;
    string playerId;
    string cardId;
    int supportAddValue;
    int support;

    public void OnCardDropped(string cardIdDropped, string enemyId)
    {
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;
        cardId = cardIdDropped;

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        // Inicjalizacja referencji do bazy danych Firebase
        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");

        HandlePlayedCard(enemyId);
    }

    async void HandlePlayedCard(string enemyId)
    {
        await GetCardValue();
        await GetSupport(enemyId);

        //zaktualizuj wartoœæ i wsadz do bazy danych
        support += supportAddValue;
        Debug.Log("FINAL to be added: " + supportAddValue);
        Debug.Log("FINAL player has: " + support);

        await dbRef.Child(enemyId).Child("stats").Child("support").SetValueAsync(support);
    }

    async Task GetCardValue()
    {
        //pobierz z bazy ile trzeba dodaæ(z karty)
        var task = await dbRef.Child(playerId).Child("deck").Child(cardId).Child("cardValue").GetValueAsync();
        if (task == null || task.Value == null)
        {
            Debug.Log("Error fetching value");
            return;
        }

        supportAddValue = int.Parse(task.Value.ToString());
        Debug.Log("To be added: " + supportAddValue);
    }

    async Task GetSupport(string enemyId)
    {
        //pobierz ile ma gracz
        var task = await dbRef.Child(enemyId).Child("stats").Child("support").GetValueAsync();
        if (task == null || task.Value == null)
        {
            Debug.Log("Error fetching value");
            return;
        }

        support = int.Parse(task.Value.ToString());
        Debug.Log("Player has: " + support);
    }
}