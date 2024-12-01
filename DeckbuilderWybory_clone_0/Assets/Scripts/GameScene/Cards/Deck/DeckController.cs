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

    public ErrorPanelController errorPanelController;

    public async Task InitializeDeck()
    {
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        try
        {
            // Pobranie deckType z Firebase
            var deckTypeSnapshot = await dbRef.Child(playerId).Child("stats").Child("deckType").GetValueAsync();
            if (deckTypeSnapshot.Exists)
            {
                string deckType = deckTypeSnapshot.Value.ToString();
                Debug.Log($"DeckType found: {deckType}");

                // Pobranie listy kart dla deckType
                await LoadCardsFromDeckType(deckType);

            }
            else
            {
                Debug.LogWarning("DeckType not found for the player.");
                errorPanelController.ShowError("Deck type not assigned to the player!");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error while loading deck type: {ex.Message}");
            errorPanelController.ShowError("Failed to load deck type.");
        }
    }

    private async Task LoadCardsFromDeckType(string deckType)
    {
        try
        {
            DatabaseReference readyDecksRef = FirebaseInitializer.DatabaseReference
                .Child("readyDecks")
                .Child(deckType);

            var deckSnapshot = await readyDecksRef.GetValueAsync();
            if (!deckSnapshot.Exists)
            {
                Debug.LogWarning($"No cards found for deckType: {deckType}");
                errorPanelController.ShowError($"No cards available for the deck type '{deckType}'.");
                return;
            }

            // Pobranie kart i dodanie ich do talii
            foreach (var cardSnapshot in deckSnapshot.Children)
            {
                string cardId = cardSnapshot.Value.ToString();

                // Dodanie karty do decka
                AddCardToDeck(cardId, false); // Ustawienie `onHand` na false dla wszystkich kart
            }

            Debug.Log("Deck loaded successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error while loading cards for deckType {deckType}: {ex.Message}");
            errorPanelController.ShowError("Failed to load cards for the selected deck type.");
        }
    }

    private async void AddCardToDeck(string cardId, bool isOnHand)
    {
        string instanceId = GenerateInstanceId(cardId);

        var cardDataDict = new Dictionary<string, object>
    {
        { "cardId", cardId },
        { "onHand", isOnHand },
        { "played", false }
    };

        try
        {
            await dbRef
                .Child(playerId)
                .Child("deck")
                .Child(instanceId)
                .SetValueAsync(cardDataDict);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to add card {instanceId} to the deck: {ex.Message}");
        }
    }

    private string GenerateInstanceId(string cardId)
    {
        return $"{cardId}_{Guid.NewGuid()}";
    }

    public async Task<bool> GetCardFromDeck(string source, string target, bool useUnityRandom = true)
{
    string lobbyId = DataTransfer.LobbyId;

    if (string.IsNullOrEmpty(lobbyId) || string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
    {
        Debug.LogError("Lobby ID, source, or target is null or empty. Cannot draw a card.");
            errorPanelController.ShowError("general_error");
            return true;
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
            errorPanelController.ShowError("general_error");
            return true;
        }

    List<string> availableCards = new();
    foreach (var cardSnapshot in snapshot.Children)
    {
        bool onHand = cardSnapshot.Child("onHand").Value as bool? ?? false;
        bool played = cardSnapshot.Child("played").Value as bool? ?? false;

        if (!onHand && !played)
        {
            availableCards.Add(cardSnapshot.Key);
        }
    }

    if (availableCards.Count == 0)
    {
        Debug.LogWarning($"No cards available to draw for player {source} in lobby {lobbyId}.");
        errorPanelController.ShowError("no_cards");
        return true;
    }

        System.Random random = new();
        int randomIndex;

    if (useUnityRandom)
    {
        randomIndex = UnityEngine.Random.Range(0, availableCards.Count);
    }
    else
    {
        randomIndex = random.Next(availableCards.Count);
    }

    string selectedInstanceId = availableCards[randomIndex];

    var selectedCardSnapshot = await sourceDeckRef.Child(selectedInstanceId).Child("cardId").GetValueAsync();
    if (!selectedCardSnapshot.Exists)
    {
        Debug.LogError($"CardId for instance {selectedInstanceId} not found in source deck.");
            errorPanelController.ShowError("general_error");
            return true;
        }

    string selectedCardId = selectedCardSnapshot.Value.ToString();

    if (source == target)
    {
        await UpdateCardOnHand(sourceDeckRef, selectedInstanceId, true);

            return false;
    }
    else
    {
        var cardData = new Dictionary<string, object>
        {
            { "onHand", true },
            { "played", false },
            { "cardId", selectedCardId }
        };

        await targetDeckRef.Child(selectedInstanceId).SetValueAsync(cardData);
        await sourceDeckRef.Child(selectedInstanceId).RemoveValueAsync();

            return false;
    }
}

    private async Task UpdateCardOnHand(DatabaseReference deckRef, string instanceId, bool onHandStatus)
    {
        try
        {
            await deckRef.Child(instanceId).Child("onHand").SetValueAsync(onHandStatus);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to mark card {instanceId} as 'onHand: {onHandStatus}': {ex.Message}");
        }
    }

    public async Task<bool> RejectCard(string source, string instanceId)
    {
        string lobbyId = DataTransfer.LobbyId;

        if (string.IsNullOrEmpty(lobbyId) || string.IsNullOrEmpty(source) || string.IsNullOrEmpty(instanceId))
        {
            Debug.LogError("Lobby ID, source, or instanceId is null or empty. Cannot reject card.");
            errorPanelController.ShowError("general_error");
            return true;
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
            errorPanelController.ShowError("general_error");
            return true;
        }

        try
        {
            await sourceDeckRef.RemoveValueAsync();
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to remove card {instanceId} for player {source}: {ex.Message}");
            errorPanelController.ShowError("general_error");
            return true;
        }
    }

    public async Task<bool> RejectRandomCard(string source, string excludedInstanceId)
    {
        string lobbyId = DataTransfer.LobbyId;

        if (string.IsNullOrEmpty(lobbyId) || string.IsNullOrEmpty(source))
        {
            Debug.LogError("Lobby ID or source is null or empty. Cannot reject a random card.");
            errorPanelController.ShowError("general_error");
            return true;
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
            errorPanelController.ShowError("general_error");
            return true;
        }

        List<string> availableCards = new List<string>();
        foreach (var child in deckSnapshot.Children)
        {
            string cardId = child.Key;

            if (cardId == excludedInstanceId)
                continue;

            bool? onHand = child.Child("onHand").Value as bool?;
            bool? played = child.Child("played").Value as bool?;

            if (onHand == true && played != true)
            {
                availableCards.Add(cardId);
            }
        }

        if (availableCards.Count == 0)
        {
            Debug.LogWarning($"No cards available to reject for player {source}, excluding {excludedInstanceId}.");
            errorPanelController.ShowError("no_cards");
            return true;
        }

        System.Random random = new System.Random();
        string randomCardId = availableCards[random.Next(availableCards.Count)];

        DatabaseReference randomCardRef = playerDeckRef.Child(randomCardId);

        try
        {
            await randomCardRef.RemoveValueAsync();
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to remove random card {randomCardId} for player {source}: {ex.Message}");
            errorPanelController.ShowError("general_error");
            return true;
        }
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

        List<string> playerIds = new List<string>();
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
        System.Random random = new System.Random();

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

            List<string> availableCards = new List<string>();

            foreach (var cardSnapshot in currentPlayerSnapshot.Children)
            {
                string cardId = cardSnapshot.Key;

                if (exchangedCards.Contains(cardId) || instanceId == cardId && playerId == currentPlayerId)
                    continue;

                var onHandSnapshot = cardSnapshot.Child("onHand");
                var playedSnapshot = cardSnapshot.Child("played");

                if (onHandSnapshot.Exists && bool.TryParse(onHandSnapshot.Value.ToString(), out bool onHand) && onHand &&
                    playedSnapshot.Exists && bool.TryParse(playedSnapshot.Value.ToString(), out bool played) && !played)
                {
                    availableCards.Add(cardId);
                }
            }

            if (availableCards.Count == 0)
            {
                Debug.LogWarning($"No available cards to exchange for player {currentPlayerId}.");
                continue;
            }

            string selectedInstanceId = availableCards[random.Next(availableCards.Count)];
            string selectedCardId = (string)currentPlayerSnapshot.Child(selectedInstanceId).Child("cardId").Value;

            var nextPlayerSnapshot = await nextPlayerDeckRef.GetValueAsync();
            if (!nextPlayerSnapshot.Exists)
            {
                Debug.LogError($"No cards found for player {nextPlayerId} in lobby {lobbyId}.");
                continue;
            }

            exchangedCards.Add(selectedInstanceId);

            try
            {
                await nextPlayerDeckRef.Child(selectedInstanceId).SetValueAsync(new Dictionary<string, object>
            {
                { "onHand", true },
                { "played", false },
                { "cardId", selectedCardId }
            });

                await currentPlayerDeckRef.Child(selectedInstanceId).RemoveValueAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during card exchange between {currentPlayerId} and {nextPlayerId}: {ex.Message}");
            }
        }
    }

    public async Task<bool> GetCardFromHand(string source, string target, List<KeyValuePair<string, string>> cards)
    {
        string lobbyId = DataTransfer.LobbyId;

        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("Lobby ID is null or empty.");
            errorPanelController.ShowError("general_error");
            return true;
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

        foreach (var card in cards)
        {
            string instanceId = card.Key;
            string cardId = card.Value;

            var sourceCardRef = sourceDeckRef.Child(instanceId);
            var targetCardRef = targetDeckRef.Child(instanceId);

            try
            {
                await sourceCardRef.RemoveValueAsync();

                var targetCardSnapshot = await targetCardRef.GetValueAsync();

                if (!targetCardSnapshot.Exists)
                {
                    await targetCardRef.SetValueAsync(new Dictionary<string, object>
                {
                    { "cardId", cardId },
                    { "onHand", true },
                    { "played", false }
                });
                }

            }
            catch (Exception ex)
            {
                Debug.LogError($"Error while processing card {instanceId} for target {target}: {ex.Message}");
                errorPanelController.ShowError("general_error");
                return true;
            }
        }

        return false;
    }

    public async Task<bool> ExchangeFromHandToDeck(string source, string instanceIdFromHand, string instanceIdFromDeck)
    {
        string lobbyId = DataTransfer.LobbyId;

        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("Lobby ID is null or empty.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        DatabaseReference sourceDeckRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(source)
            .Child("deck");

        var cardFromHandRef = sourceDeckRef.Child(instanceIdFromHand);
        var cardFromDeckRef = sourceDeckRef.Child(instanceIdFromDeck);

        try
        {
            var cardFromHandSnapshot = await cardFromHandRef.GetValueAsync();
            var cardFromDeckSnapshot = await cardFromDeckRef.GetValueAsync();

            if (!cardFromHandSnapshot.Exists)
            {
                Debug.LogError($"Card {instanceIdFromHand} not found in hand of player {source}.");
                errorPanelController.ShowError("general_error");
                return true;
            }

            if (!cardFromDeckSnapshot.Exists)
            {
                Debug.LogError($"Card {instanceIdFromDeck} not found in deck of player {source}.");
                errorPanelController.ShowError("general_error");
                return true;
            }

            await cardFromHandRef.Child("onHand").SetValueAsync(false);
            await cardFromDeckRef.Child("onHand").SetValueAsync(true);

            return false;

        }
        catch (Exception ex)
        {
            Debug.LogError($"Error occurred during card exchange for player {source}: {ex.Message}");
            errorPanelController.ShowError("general_error");
            return true;
        }
    }

    public async Task<bool> GetRandomCardsFromHand(string target, string source, int howMany, List<KeyValuePair<string, string>> cards)
    {
        if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(source) || howMany <= 0)
        {
            Debug.LogError("Invalid target, source, or howMany parameter.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        string lobbyId = DataTransfer.LobbyId;
        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("Lobby ID is null or empty.");
            errorPanelController.ShowError("general_error");
            return true;
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
            errorPanelController.ShowError("general_error");
            return true;
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
            errorPanelController.ShowError("no_cards");
            return true;
        }

        System.Random random = new();

        howMany = Math.Min(howMany, eligibleCards.Count);

        List<string> selectedCards = new();
        for (int i = 0; i < howMany; i++)
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

        return false;
    }

    public async Task<bool> ReturnCardToDeck(string source, string instanceId)
    {
        string lobbyId = DataTransfer.LobbyId;

        if (string.IsNullOrEmpty(lobbyId) || string.IsNullOrEmpty(source) || string.IsNullOrEmpty(instanceId))
        {
            Debug.LogError("Lobby ID, source, or instanceId is null or empty. Cannot return card to deck.");
            errorPanelController.ShowError("general_error");
            return true;
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
            errorPanelController.ShowError("general_error");
            return true;
        }

        try
        {
            var updates = new Dictionary<string, object>
        {
            { "onHand", false },
            { "played", false }
        };

            await sourceDeckRef.Child(instanceId).UpdateChildrenAsync(updates);

            return false;

        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to return card {instanceId} for player {source}: {ex.Message}");
            errorPanelController.ShowError("general_error");
            return true;
        }
    }

    public async Task<bool> GetRandomCardsFromDeck(string target, int howMany, List<KeyValuePair<string, string>> cards)
    {
        if (string.IsNullOrEmpty(target) || howMany <= 0)
        {
            Debug.LogError("Invalid target or howMany parameter.");
            errorPanelController.ShowError("general_error");
            return true;
        }

        string lobbyId = DataTransfer.LobbyId;
        if (string.IsNullOrEmpty(lobbyId))
        {
            Debug.LogError("Lobby ID is null or empty.");
            errorPanelController.ShowError("general_error");
            return true;
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
            errorPanelController.ShowError("general_error");
            return true;
        }

        HashSet<string> excludedInstanceIds = new(cards.Select(card => card.Key));

        List<string> eligibleCards = new();
        foreach (var cardSnapshot in targetDeckSnapshot.Children)
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
            errorPanelController.ShowError("no_card");
            return true;
        }

        int cardsToDraw = Math.Min(howMany, eligibleCards.Count);

        System.Random random = new();
        List<string> selectedCards = new();

        for (int i = 0; i < cardsToDraw; i++)
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

        return false;
    }

}
