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

    public void InitializeDeck()
    {
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");

        AddCardToDeck("CA070", true);
        AddCardToDeck("UN039", true);
        AddCardToDeck("UN086", true);
        AddCardToDeck("CA077", true);
        AddCardToDeck("CA031", true);
        AddCardToDeck("CA070", false);
        AddCardToDeck("UN039", false);
        AddCardToDeck("UN086", false);
        AddCardToDeck("CA077", false);
        AddCardToDeck("CA031", false);

        Debug.Log("Deck loaded");
    }

    private void AddCardToDeck(string cardId, bool isOnHand)
    {
        string instanceId = GenerateInstanceId(cardId);

        bool onHand = isOnHand;
        bool played = false;

        Dictionary<string, object> cardDataDict = new()
    {
        { "cardId", cardId },
        { "onHand", onHand },
        { "played", played }
    };

        dbRef.Child(playerId).Child("deck").Child(instanceId).SetValueAsync(cardDataDict)
            .ContinueWith(task => {
                if (!task.IsCompleted)
                {
                    Debug.LogError($"Failed to add card {instanceId} to the deck: {task.Exception}");
                }
            });
    }

    private string GenerateInstanceId(string cardId)
    {
        return cardId + "_" + System.Guid.NewGuid().ToString();
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

            if (onHandSnapshot.Exists && bool.TryParse(onHandSnapshot.Value.ToString(), out bool onHand) && !onHand &&
                playedSnapshot.Exists && bool.TryParse(playedSnapshot.Value.ToString(), out bool played) && !played)
            {
                availableCards.Add(cardSnapshot.Key);
            }
        }

        if (availableCards.Count == 0)
        {
            Debug.LogWarning($"No cards available to draw for player {source} in lobby {lobbyId}.");
            return;
        }

        System.Random random = new();
        int randomIndex = random.Next(availableCards.Count);
        string selectedInstanceId = availableCards[randomIndex];

        var selectedCardSnapshot = await sourceDeckRef.Child(selectedInstanceId).Child("cardId").GetValueAsync();
        if (!selectedCardSnapshot.Exists)
        {
            Debug.LogError($"CardId for instance {selectedInstanceId} not found in source deck.");
            return;
        }

        string selectedCardId = selectedCardSnapshot.Value.ToString();

        if (source == target)
        {
            await sourceDeckRef.Child(selectedInstanceId).Child("onHand").SetValueAsync(true)
                .ContinueWith(task =>
                {
                    if (!task.IsCompleted)
                    {
                        Debug.LogError($"Failed to mark card {selectedInstanceId} as 'onHand: true' for player {target}: {task.Exception}");
                    }
                });
        }
        else
        {
            var cardData = new Dictionary<string, object>
        {
            { "onHand", true },
            { "played", false },
            { "cardId", selectedCardId }
        };

            await targetDeckRef.Child(selectedInstanceId).SetValueAsync(cardData)
                .ContinueWith(task =>
                {
                    if (!task.IsCompleted)
                    {
                        Debug.LogError($"Failed to assign card {selectedInstanceId} to target player {target}: {task.Exception}");
                    }
                });

            await sourceDeckRef.Child(selectedInstanceId).RemoveValueAsync()
                .ContinueWith(task =>
                {
                    if (!task.IsCompleted)
                    {
                        Debug.LogError($"Failed to remove card {selectedInstanceId} from source player {source}: {task.Exception}");
                    }
                });
        }
    }


    public async Task RejectCard(string source, string instanceId)
    {
        string lobbyId = DataTransfer.LobbyId;

        if (string.IsNullOrEmpty(lobbyId) || string.IsNullOrEmpty(source) || string.IsNullOrEmpty(instanceId))
        {
            Debug.LogError("Lobby ID, source, or instanceId is null or empty. Cannot reject card.");
            return;
        }

        DatabaseReference sourceDeckRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(source)
            .Child("deck");

        var cardSnapshot = await sourceDeckRef.Child(instanceId).GetValueAsync();
        if (!cardSnapshot.Exists)
        {
            Debug.LogError($"Card {instanceId} not found for player {source} in lobby {lobbyId}.");
            return;
        }

        await sourceDeckRef.Child(instanceId).UpdateChildrenAsync(new Dictionary<string, object>
    {
        { "onHand", false },
        { "played", true }
    })
        .ContinueWith(task =>
        {
            if (!task.IsCompleted)
            {
                Debug.LogError($"Failed to reject card {instanceId} for player {source}: {task.Exception}");
            }
        });
    }

    public async Task ExchangeCards(string playerId, string instanceId)
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
                if (instanceId == cardSnapshot.Key && playerId == currentPlayerId) { continue; }

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
            string selectedInstanceId = availableCards[randomIndex];

            var nextPlayerSnapshot = await nextPlayerDeckRef.GetValueAsync();
            if (!nextPlayerSnapshot.Exists)
            {
                Debug.LogError($"No cards found for player {nextPlayerId} in lobby {lobbyId}.");
                continue;
            }

            await nextPlayerDeckRef.Child(selectedInstanceId).SetValueAsync(new Dictionary<string, object>
        {
            { "onHand", true },
            { "played", false }
        })
            .ContinueWith(task =>
            {
                if (!task.IsCompleted)
                {
                    Debug.LogError($"Failed to assign card {selectedInstanceId} to next player {nextPlayerId}: {task.Exception}");
                }
            });

            await currentPlayerDeckRef.Child(selectedInstanceId).RemoveValueAsync()
            .ContinueWith(task =>
            {
                if (!task.IsCompleted)
                {
                    Debug.LogError($"Failed to remove card {selectedInstanceId} from player {currentPlayerId}: {task.Exception}");
                }
            });

            Debug.Log($"Player {currentPlayerId} exchanged card {selectedInstanceId} with player {nextPlayerId}.");
        }
    }

    public async Task GetCardFromHand(string source, string target, List<KeyValuePair<string, string>> cards)
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

        foreach (var card in cards)
        {
            string instanceId = card.Key;
            string cardId = card.Value;

            var sourceCardRef = sourceDeckRef.Child(instanceId);
            await sourceCardRef.RemoveValueAsync();

            var targetCardRef = targetDeckRef.Child(instanceId);
            var targetCardSnapshot = await targetCardRef.GetValueAsync();

            if (!targetCardSnapshot.Exists)
            {
                await targetCardRef.Child("cardId").SetValueAsync(cardId);
                await targetCardRef.Child("onHand").SetValueAsync(true);
                await targetCardRef.Child("played").SetValueAsync(false);
            }
        }
    }

    public async Task ExchangeFromHandToDeck(string source, string instanceIdFromHand, string instanceIdFromDeck)
    {
        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference sourceDeckRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(source)
            .Child("deck");

        var cardFromHandRef = sourceDeckRef.Child(instanceIdFromHand);
        await cardFromHandRef.Child("onHand").SetValueAsync(false);

        var cardFromDeckRef = sourceDeckRef.Child(instanceIdFromDeck);
        await cardFromDeckRef.Child("onHand").SetValueAsync(true);
    }


}
