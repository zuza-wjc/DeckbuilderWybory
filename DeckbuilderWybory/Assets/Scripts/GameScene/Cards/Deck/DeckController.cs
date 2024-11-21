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
        AddCardToDeck("RA012", true);
        AddCardToDeck("AD023", false);
        AddCardToDeck("AD058", false);

        Debug.Log("Deck loaded");
    }

    private void AddCardToDeck(string cardId, bool isOnHand)
    {
   
        bool onHand = isOnHand;  
        bool played = false; 

        Dictionary<string, object> cardDataDict = new()
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

    public async Task GetCardFromDeck(string source, string target)
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

        List<string> availableCards = new();
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

        System.Random random = new();
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

    public async Task RejectCard(string source, string cardId)
    {
        string lobbyId = DataTransfer.LobbyId;

        if (string.IsNullOrEmpty(lobbyId) || string.IsNullOrEmpty(source) || string.IsNullOrEmpty(cardId))
        {
            Debug.LogError("Lobby ID, source, or cardId is null or empty. Cannot reject card.");
            return;
        }

        DatabaseReference sourceDeckRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(source)
            .Child("deck");

        var cardSnapshot = await sourceDeckRef.Child(cardId).GetValueAsync();
        if (!cardSnapshot.Exists)
        {
            Debug.LogError($"Card {cardId} not found for player {source} in lobby {lobbyId}.");
            return;
        }

        await sourceDeckRef.Child(cardId).UpdateChildrenAsync(new Dictionary<string, object>
    {
        { "onHand", false },
        { "played", true }
    })
        .ContinueWith(task =>
        {
            if (!task.IsCompleted)
            {
                Debug.LogError($"Failed to reject card {cardId} for player {source}: {task.Exception}");
            }
        });
    }

    public async Task ExchangeCards(string playerId, string cardId)
    {
        string lobbyId = DataTransfer.LobbyId;
        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("Lobby ID is null or empty.");
            return;
        }

        DatabaseReference playersRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        var snapshot = await playersRef.GetValueAsync();
        if (!snapshot.Exists)
        {
            Debug.LogError($"No players found in lobby {lobbyId}.");
            return;
        }

        List<string> playerIds = new();
        foreach (var playerSnapshot in snapshot.Children)
        {
            playerIds.Add(playerSnapshot.Key);
        }

        if (playerIds.Count < 2)
        {
            Debug.LogError("There must be at least two players to exchange cards.");
            return;
        }

        System.Random random = new();
        for (int i = 0; i < playerIds.Count; i++)
        {
            string currentPlayerId = playerIds[i];
            string nextPlayerId = playerIds[(i + 1) % playerIds.Count];

            DatabaseReference currentPlayerDeckRef = playersRef.Child(currentPlayerId).Child("deck");
            DatabaseReference nextPlayerDeckRef = playersRef.Child(nextPlayerId).Child("deck");

            var currentPlayerSnapshot = await currentPlayerDeckRef.GetValueAsync();
            if (!currentPlayerSnapshot.Exists)
            {
                Debug.LogError($"No cards found for player {currentPlayerId} in lobby {lobbyId}.");
                continue;
            }

            List<string> availableCards = new();
            foreach (var cardSnapshot in currentPlayerSnapshot.Children)
            {
                if(cardId ==  cardSnapshot.Key && playerId == currentPlayerId) { continue;  }

                var onHandSnapshot = cardSnapshot.Child("onHand");
                var playedSnapshot = cardSnapshot.Child("played");

                if (onHandSnapshot.Exists && bool.TryParse(onHandSnapshot.Value.ToString(), out bool onHand) && onHand &&
                    playedSnapshot.Exists && bool.TryParse(playedSnapshot.Value.ToString(), out bool played) && !played)
                {
                    availableCards.Add(cardSnapshot.Key);
                }
            }

            if (availableCards.Count == 0)
            {
                Debug.LogWarning($"No available cards to exchange for player {currentPlayerId}.");
                continue;
            }

            int randomIndex = random.Next(availableCards.Count);
            string selectedCardId = availableCards[randomIndex];

            var nextPlayerSnapshot = await nextPlayerDeckRef.GetValueAsync();
            if (!nextPlayerSnapshot.Exists)
            {
                Debug.LogError($"No cards found for player {nextPlayerId} in lobby {lobbyId}.");
                continue;
            }

            await nextPlayerDeckRef.Child(selectedCardId).SetValueAsync(new Dictionary<string, object>
        {
            { "onHand", true },
            { "played", false }
        })
            .ContinueWith(task =>
            {
                if (!task.IsCompleted)
                {
                    Debug.LogError($"Failed to assign card {selectedCardId} to next player {nextPlayerId}: {task.Exception}");
                }
            });

            await currentPlayerDeckRef.Child(selectedCardId).RemoveValueAsync()
            .ContinueWith(task =>
            {
                if (!task.IsCompleted)
                {
                    Debug.LogError($"Failed to remove card {selectedCardId} from player {currentPlayerId}: {task.Exception}");
                }
            });

            Debug.Log($"Player {currentPlayerId} exchanged card {selectedCardId} with player {nextPlayerId}.");
        }
    }

    public async Task GetCardFromHand(string source, string target, List<string> cards)
    {
        string lobbyId = DataTransfer.LobbyId;

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

        foreach (string cardId in cards)
        {
            var sourceCardRef = sourceDeckRef.Child(cardId);
            await sourceCardRef.RemoveValueAsync();
            var targetCardRef = targetDeckRef.Child(cardId);
            var targetCardSnapshot = await targetCardRef.GetValueAsync();
            if (!targetCardSnapshot.Exists)
            {
                await targetCardRef.Child("onHand").SetValueAsync(true);
                await targetCardRef.Child("played").SetValueAsync(false);
            }
        }
    }

    public async Task ExchangeFromHandToDeck(string source, string cardIdFromHand, string cardIdFromDeck)
    {
        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference sourceDeckRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(source)
            .Child("deck");

        var cardFromHandRef = sourceDeckRef.Child(cardIdFromHand);
        await cardFromHandRef.Child("onHand").SetValueAsync(false);

        var cardFromDeckRef = sourceDeckRef.Child(cardIdFromDeck);
        await cardFromDeckRef.Child("onHand").SetValueAsync(true);
    }

}
