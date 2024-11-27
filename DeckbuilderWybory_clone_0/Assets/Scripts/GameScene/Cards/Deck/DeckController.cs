using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using System.Threading.Tasks;
using System.Linq;
using System;

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

        AddCardToDeck("AD057", true);
        AddCardToDeck("AD037", true);
        AddCardToDeck("AD037", true);
        AddCardToDeck("AD037", true);
        AddCardToDeck("AD037", true);
        AddCardToDeck("AD047", true);

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
            .Child("deck")
            .Child(instanceId);

        var cardSnapshot = await sourceDeckRef.GetValueAsync();
        if (!cardSnapshot.Exists)
        {
            Debug.LogError($"Card {instanceId} not found for player {source} in lobby {lobbyId}.");
            return;
        }

        // Usuñ kartê
        await sourceDeckRef.RemoveValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError($"Failed to remove card {instanceId} for player {source}: {task.Exception}");
            }
        });
    }

    public async Task RejectRandomCard(string source, string excludedInstanceId)
    {
        string lobbyId = DataTransfer.LobbyId;

        if (string.IsNullOrEmpty(lobbyId) || string.IsNullOrEmpty(source))
        {
            Debug.LogError("Lobby ID or source is null or empty. Cannot reject a random card.");
            return;
        }

        DatabaseReference playerDeckRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(source)
            .Child("deck");

        var deckSnapshot = await playerDeckRef.GetValueAsync();
        if (!deckSnapshot.Exists)
        {
            Debug.LogError($"Deck not found for player {source} in lobby {lobbyId}.");
            return;
        }

        List<string> availableCards = new List<string>();
        foreach (var child in deckSnapshot.Children)
        {
            string cardId = child.Key;
            if (cardId == excludedInstanceId)
                continue;

            bool onHand = child.Child("onHand").Exists && Convert.ToBoolean(child.Child("onHand").Value);
            bool played = child.Child("played").Exists && Convert.ToBoolean(child.Child("played").Value);

            if (onHand && !played)
            {
                availableCards.Add(cardId);
            }
        }

        if (availableCards.Count == 0)
        {
            Debug.LogWarning($"No cards available to reject for player {source}, excluding {excludedInstanceId}.");
            return;
        }

        System.Random random = new System.Random();
        int randomIndex = random.Next(availableCards.Count);
        string randomCardId = availableCards[randomIndex];

        DatabaseReference randomCardRef = playerDeckRef.Child(randomCardId);
        await randomCardRef.RemoveValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError($"Failed to remove random card {randomCardId} for player {source}: {task.Exception}");
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

        HashSet<string> exchangedCards = new HashSet<string>();

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
                if (exchangedCards.Contains(cardSnapshot.Key))
                {
                    continue;
                }

                if (instanceId == cardSnapshot.Key && playerId == currentPlayerId)
                {
                    continue;
                }

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

            string selectedCardId = (string)currentPlayerSnapshot.Child(selectedInstanceId).Child("cardId").Value;

            var nextPlayerSnapshot = await nextPlayerDeckRef.GetValueAsync();
            if (!nextPlayerSnapshot.Exists)
            {
                Debug.LogError($"No cards found for player {nextPlayerId} in lobby {lobbyId}.");
                continue;
            }

            exchangedCards.Add(selectedInstanceId);

            await nextPlayerDeckRef.Child(selectedInstanceId).SetValueAsync(new Dictionary<string, object>
        {
            { "onHand", true },
            { "played", false },
            { "cardId", selectedCardId }
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

            Debug.Log("Now getting card");
            try
            {
                // Sprawdzamy czy operacja GetValueAsync zakoñczy siê b³êdem
                var targetCardSnapshot = await targetCardRef.GetValueAsync();

                Debug.Log("Now adding card");

                // Jeœli dane istniej¹, wykonaj operacje
                if (!targetCardSnapshot.Exists)
                {
                    await targetCardRef.Child("cardId").SetValueAsync(cardId);
                    await targetCardRef.Child("onHand").SetValueAsync(true);
                    await targetCardRef.Child("played").SetValueAsync(false);
                }
            }
            catch (Exception ex)
            {
                // Logujemy szczegó³y b³êdu
                Debug.LogError($"Error while fetching card {instanceId} for target {target}: {ex.Message}");
                // Dodatkowo: mo¿esz chcieæ przerwaæ dalsze przetwarzanie kart lub podj¹æ inne dzia³ania
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

    public async Task GetRandomCardsFromHand(string target, string source, int howMany, List<KeyValuePair<string, string>> cards)
    {

        if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(source) || howMany <= 0)
        {
            Debug.LogError("Invalid target, source, or howMany parameter.");
            return;
        }

        string lobbyId = DataTransfer.LobbyId;
        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("Lobby ID is null or empty.");
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

        var sourceSnapshot = await sourceDeckRef.GetValueAsync();
        if (!sourceSnapshot.Exists)
        {
            Debug.LogError($"Source deck not found for player {source} in lobby {lobbyId}.");
            return;
        }

        HashSet<string> excludedInstanceIds = new(cards.Select(card => card.Key));

        List<string> eligibleCards = new();
        foreach (var cardSnapshot in sourceSnapshot.Children)
        {
            string instanceId = cardSnapshot.Key;
            bool onHand = bool.TryParse(cardSnapshot.Child("onHand").Value?.ToString(), out bool isOnHand) && isOnHand;
            bool played = bool.TryParse(cardSnapshot.Child("played").Value?.ToString(), out bool isPlayed) && !isPlayed;

            if (onHand && played && !excludedInstanceIds.Contains(instanceId))
            {
                eligibleCards.Add(instanceId);
            }
        }

        if (eligibleCards.Count == 0)
        {
            Debug.LogWarning("No eligible cards available to transfer.");
            return;
        }

        System.Random random = new();
        List<string> selectedCards = new();

        for (int i = 0; i < howMany && eligibleCards.Count > 0; i++)
        {
            int randomIndex = random.Next(eligibleCards.Count);
            selectedCards.Add(eligibleCards[randomIndex]);
            eligibleCards.RemoveAt(randomIndex);
        }

        foreach (string instanceId in selectedCards)
        {
            var cardDataSnapshot = await sourceDeckRef.Child(instanceId).GetValueAsync();
            if (!cardDataSnapshot.Exists)
            {
                Debug.LogError($"Card {instanceId} does not exist in source deck.");
                continue;
            }

            Dictionary<string, object> cardData = new();
            foreach (var child in cardDataSnapshot.Children)
            {
                cardData[child.Key] = child.Value;
            }

            cardData["onHand"] = true;
            cardData["played"] = false;

            await targetDeckRef.Child(instanceId).SetValueAsync(cardData);

            await sourceDeckRef.Child(instanceId).RemoveValueAsync();

        }
    }

    public async Task ReturnCardToDeck(string source, string instanceId)
    {
        string lobbyId = DataTransfer.LobbyId;

        if (string.IsNullOrEmpty(lobbyId) || string.IsNullOrEmpty(source) || string.IsNullOrEmpty(instanceId))
        {
            Debug.LogError("Lobby ID, source, or instanceId is null or empty. Cannot return card to deck.");
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

        try
        {
            await sourceDeckRef.Child(instanceId).UpdateChildrenAsync(new Dictionary<string, object>
        {
            { "onHand", false },
            { "played", false }
        });

        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to return card {instanceId} for player {source}: {ex.Message}");
        }
    }

    public async Task GetRandomCardsFromDeck(string target, int howMany, List<KeyValuePair<string, string>> cards)
    {
        if (string.IsNullOrEmpty(target) || howMany <= 0)
        {
            Debug.LogError("Invalid target or howMany parameter.");
            return;
        }

        string lobbyId = DataTransfer.LobbyId;
        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("Lobby ID is null or empty.");
            return;
        }

        DatabaseReference targetDeckRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(target)
            .Child("deck");

        var targetDeckSnapshot = await targetDeckRef.GetValueAsync();
        if (!targetDeckSnapshot.Exists)
        {
            Debug.LogError($"Target deck not found for player {target} in lobby {lobbyId}.");
            return;
        }

        HashSet<string> excludedInstanceIds = new(cards.Select(card => card.Key));

        List<string> eligibleCards = new();
        foreach (var cardSnapshot in targetDeckSnapshot.Children)
        {
            string instanceId = cardSnapshot.Key;
            bool onHand = bool.TryParse(cardSnapshot.Child("onHand").Value?.ToString(), out bool isOnHand) && !isOnHand;
            bool played = bool.TryParse(cardSnapshot.Child("played").Value?.ToString(), out bool isPlayed) && !isPlayed;

            if (onHand && played && !excludedInstanceIds.Contains(instanceId))
            {
                eligibleCards.Add(instanceId);
            }
        }

        if (eligibleCards.Count == 0)
        {
            Debug.LogWarning("No eligible cards available to transfer.");
            return;
        }

        System.Random random = new();
        List<string> selectedCards = new();

        for (int i = 0; i < howMany && eligibleCards.Count > 0; i++)
        {
            int randomIndex = random.Next(eligibleCards.Count);
            selectedCards.Add(eligibleCards[randomIndex]);
            eligibleCards.RemoveAt(randomIndex);
        }

        foreach (string instanceId in selectedCards)
        {
            var cardDataSnapshot = await targetDeckRef.Child(instanceId).GetValueAsync();
            if (!cardDataSnapshot.Exists)
            {
                Debug.LogError($"Card {instanceId} does not exist in target deck.");
                continue;
            }

            Dictionary<string, object> cardData = new();
            foreach (var child in cardDataSnapshot.Children)
            {
                cardData[child.Key] = child.Value;
            }

            cardData["onHand"] = true;
            cardData["played"] = false;

            await targetDeckRef.Child(instanceId).SetValueAsync(cardData);
        }
    }

}
