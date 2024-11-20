using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using System.Threading.Tasks;

public class DeckController : MonoBehaviour
{
    DatabaseReference dbRef;
    string lobbyId;
    string playerId;

    public void InitializeDeck ()
    {
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");

        // Dodaj karty do decka
        AddCardToDeck("AS062", true);

        Debug.Log("Deck loaded");
    }

    private void AddCardToDeck(string cardId, bool isOnHand)
    {
   
        bool onHand = isOnHand;  
        bool played = false; 

        Dictionary<string, object> cardDataDict = new Dictionary<string, object>
    {
        { "onHand", onHand },
        { "played", played }
    };

        dbRef.Child(playerId).Child("deck").Child(cardId.ToString()).SetValueAsync(cardDataDict)
            .ContinueWith(task => {
                if (!task.IsCompleted)
                {
                    Debug.LogError($"Failed to add card {cardId} to the deck: {task.Exception}");
                }
              
            });
    }

    public async Task GetCard(string source, string target)
    {
        string lobbyId = DataTransfer.LobbyId;

        if (string.IsNullOrEmpty(lobbyId) || string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
        {
            Debug.LogError("Lobby ID, source, or target is null or empty. Cannot draw a card.");
            return;
        }

        DatabaseReference sourceDeckRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(source)
            .Child("deck");

        DatabaseReference targetDeckRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(target)
            .Child("deck");

        var snapshot = await sourceDeckRef.GetValueAsync();
        if (!snapshot.Exists)
        {
            Debug.LogError($"Source deck not found for player {source} in lobby {lobbyId}.");
            return;
        }

        List<string> availableCards = new List<string>();
        foreach (var cardSnapshot in snapshot.Children)
        {
            var onHandSnapshot = cardSnapshot.Child("onHand");
            var playedSnapshot = cardSnapshot.Child("played");

            if (onHandSnapshot.Exists && bool.TryParse(onHandSnapshot.Value.ToString(), out bool onHand) && !onHand)
            {
                if (playedSnapshot.Exists && bool.TryParse(playedSnapshot.Value.ToString(), out bool played) && !played)
                {
                    availableCards.Add(cardSnapshot.Key);
                }
            }
        }

        if (availableCards.Count == 0)
        {
            Debug.LogWarning($"No cards available to draw for player {source} in lobby {lobbyId}.");
            return;
        }

        System.Random random = new System.Random();
        int randomIndex = random.Next(availableCards.Count);
        string selectedCardId = availableCards[randomIndex];

        if (source == target)
        {
            await sourceDeckRef.Child(selectedCardId).Child("onHand").SetValueAsync(true)
                .ContinueWith(task =>
                {
                    if (!task.IsCompleted)
                    {
                        Debug.LogError($"Failed to mark card {selectedCardId} as 'onHand: true' for player {target}: {task.Exception}");
                    }
                });
        }
        else
        {
            await targetDeckRef.Child(selectedCardId).SetValueAsync(new Dictionary<string, object>
        {
            { "onHand", true },
            { "played", false }
        })
            .ContinueWith(task =>
            {
                if (!task.IsCompleted)
                {
                    Debug.LogError($"Failed to assign card {selectedCardId} to target player {target}: {task.Exception}");
                }
            });
        }
    }



}
