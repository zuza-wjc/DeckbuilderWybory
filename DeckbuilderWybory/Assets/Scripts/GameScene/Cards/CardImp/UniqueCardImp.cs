using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using UnityEngine;

public class UniqueCardImp : MonoBehaviour
{
    private readonly string lobbyId = DataTransfer.LobbyId;
    private readonly string playerId = DataTransfer.PlayerId;

    public PlayerListManager playerListManager;
    public MapManager mapManager;
    public CardUtilities cardUtilities;
    public CardSelectionUI cardSelectionUI;
    public CardTypeManager cardTypeManager;

    void Start()
    {
        playerListManager.Initialize(lobbyId, playerId);
    }


    public async void CardLibrary(string instanceId,string cardIdDropped, bool ignoreCost)
    {
        DatabaseReference dbRefCard;
        DatabaseReference dbRefPlayerStats;
        DatabaseReference dbRefPlayerDeck;

        int cost;
        int playerBudget;
        string enemyId = string.Empty;
        string cardType;
        int playerIncome;

        int chosenRegion = -1;
        bool isBonusRegion = false;

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRefCard = FirebaseInitializer.DatabaseReference.Child("cards").Child("id").Child("unique").Child(cardIdDropped);

        DataSnapshot snapshot = await dbRefCard.GetValueAsync();

        if (snapshot.Exists)
        {
            DataSnapshot costSnapshot = snapshot.Child("cost");
            if (costSnapshot.Exists)
            {
                cost = Convert.ToInt32(costSnapshot.Value);
            }
            else
            {
                Debug.LogError("Branch cost does not exist.");
                return;
            }

            DataSnapshot typeSnapshot = snapshot.Child("type");
            if (typeSnapshot.Exists)
            {
                cardType = typeSnapshot.Value.ToString();
            }
            else
            {
                Debug.LogError("Branch type does not exist.");
                return;
            }

        }
        else
        {
            Debug.LogError("No data for: " + cardIdDropped + ".");
            return;
        }

        dbRefPlayerStats = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("stats");

        DataSnapshot playerStatsSnapshot = await dbRefPlayerStats.GetValueAsync();

        if (playerStatsSnapshot.Exists)
        {
            DataSnapshot moneySnapshot = playerStatsSnapshot.Child("money");
            if (moneySnapshot.Exists)
            {
                playerBudget = Convert.ToInt32(moneySnapshot.Value);

                if (!ignoreCost && playerBudget < cost)
                {
                    Debug.LogError("Brak bud¿etu aby zagraæ kartê.");
                    return;
                }
            }
            else
            {
                Debug.LogError("Branch money does not exist.");
                return;
            }

            DataSnapshot incomeSnapshot = playerStatsSnapshot.Child("income");
            if (incomeSnapshot.Exists)
            {
                playerIncome = Convert.ToInt32(incomeSnapshot.Value);

            }
            else
            {
                Debug.LogError("Branch income does not exist.");
                return;
            }

        }
        else
        {
            Debug.LogError("No data for: " + cardIdDropped + ".");
            return;
        }

        playerBudget = await SwitchCase(instanceId,dbRefPlayerStats,playerIncome, playerBudget, cardIdDropped, chosenRegion,isBonusRegion, cardType,enemyId);

        if (!ignoreCost)
        {
            await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget - cost);
        }

        dbRefPlayerDeck = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck").Child(instanceId);

        await dbRefPlayerDeck.Child("onHand").SetValueAsync(false);
        await dbRefPlayerDeck.Child("played").SetValueAsync(true);
    }

    private async Task<int> SwitchCase(string instanceId,DatabaseReference dbRefPlayerStats,int playerIncome, int playerBudget, string cardId, int chosenRegion, bool isBonusRegion, string cardType, string enemyId)
    {
        switch (cardId)
        {
            case "UN018":
                chosenRegion = await mapManager.SelectArea();
                isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
                enemyId = await playerListManager.SelectEnemyPlayerInArea(chosenRegion);
                if (string.IsNullOrEmpty(enemyId))
                {
                    Debug.LogError("No enemy player found in the area.");
                    return -1;
                }
                int supportValue = await GetEnemySupportFromRegion(enemyId, chosenRegion);
                if(isBonusRegion) { supportValue--; }
                if(supportValue != 0)
                {
                    chosenRegion = await mapManager.SelectArea();
                    await ChangeSupportNoLoss(enemyId, supportValue, chosenRegion);
                }
                break;
            case "UN089":
                chosenRegion = await mapManager.SelectArea();
                await ExchangeSupportMaxMin(chosenRegion);
                break;
            case "UN025":
                chosenRegion = await mapManager.SelectArea();
                isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
                enemyId = await playerListManager.SelectEnemyPlayerInArea(chosenRegion);
                if (string.IsNullOrEmpty(enemyId))
                {
                    Debug.LogError("No enemy player found in the area.");
                    return -1;
                }
                await ExchangeSupport(chosenRegion, enemyId, isBonusRegion);
                break;
            case "UN039":
                List<KeyValuePair<string, string>> selectedCards = await cardSelectionUI.ShowCardSelection(playerId, 1, instanceId, true);

                if (selectedCards.Count > 0)
                {
                    string selectedInstanceId = selectedCards[0].Key;
                    string selectedCardId = selectedCards[0].Value;

                    Debug.Log($"Wybrana karta: {selectedInstanceId} ({selectedCardId})");

                    cardTypeManager.OnCardDropped(selectedInstanceId,selectedCardId, true);
                }
                else
                {
                    Debug.LogWarning("Nie wybrano ¿adnej karty.");
                }

                break;
            case "UN055":
                playerBudget += playerIncome;
                await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
                break;
            case "UN086":
                chosenRegion = await mapManager.SelectArea();
                isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
                await ChangeIncomePerCard(chosenRegion, isBonusRegion, instanceId);
                break;

            default:
                Debug.LogError("Unknown card ID: " + cardId + ".");
                break;
        }

        return playerBudget;
    }

    private async Task<int> GetEnemySupportFromRegion(string enemyId, int areaId)
    {
        if (string.IsNullOrEmpty(enemyId))
        {
            Debug.LogError($"Player ID is null or empty. ID: {enemyId}");
            return -1;
        }

        var dbRefSupport = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId)
            .Child("stats")
            .Child("support")
            .Child(areaId.ToString());

        try
        {
            var snapshot = await dbRefSupport.GetValueAsync();
            if (snapshot.Exists && int.TryParse(snapshot.Value.ToString(), out int support))
            {
                await dbRefSupport.SetValueAsync(0);
                return support;
            }
            else
            {
                Debug.LogWarning($"Support data not found or invalid for player {enemyId} in area {areaId}.");
                return -1;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error occurred while retrieving data for player {enemyId}: {ex.Message}");
            return -1;
        }
    }

    private async Task ChangeSupportNoLoss(string playerId,int value,int areaId)
        {
            DatabaseReference dbRefSupport = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(playerId)
                .Child("stats")
                .Child("support")
                .Child(areaId.ToString());

            var snapshot = await dbRefSupport.GetValueAsync();
            if (!snapshot.Exists)
            {
                Debug.LogError("No support data found for the given region in the player's stats.");
                return;
            }

            if (!int.TryParse(snapshot.Value.ToString(), out int support))
            {
                Debug.LogError("Failed to parse support value from the database.");
                return;
            }

            var maxSupportTask = mapManager.GetMaxSupportForRegion(areaId);
            var currentSupportTask = mapManager.GetCurrentSupportForRegion(areaId, playerId);

            await Task.WhenAll(maxSupportTask, currentSupportTask);

            int maxAreaSupport = await maxSupportTask;
            int currentAreaSupport = await currentSupportTask;

            int availableSupport = maxAreaSupport - currentAreaSupport;

            if (availableSupport < value)
            {
                Debug.Log("Brak dostêpnego miejsca na poparcie w tym regionie.");
                return;
            }

            support += value;
            await dbRefSupport.SetValueAsync(support);

    }

    private async Task ExchangeSupportMaxMin(int chosenRegion)
    {
        DatabaseReference dbRefPlayersStats = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        var snapshot = await dbRefPlayersStats.GetValueAsync();
        if (!snapshot.Exists)
        {
            Debug.LogError("No players data found in the session.");
            return;
        }

        List<(string playerId, int support)> playersWithSupport = new();

        foreach (var playerSnapshot in snapshot.Children)
        {
            string playerId = playerSnapshot.Key;
            var supportSnapshot = playerSnapshot
                .Child("stats")
                .Child("support")
                .Child(chosenRegion.ToString());

            if (!supportSnapshot.Exists || !int.TryParse(supportSnapshot.Value.ToString(), out int support) || support == 0)
            {
                continue;
            }

            playersWithSupport.Add((playerId, support));
        }

        if (playersWithSupport.Count < 2)
        {
            Debug.Log("Not enough players with non-zero support in the chosen region.");
            return;
        }

        var maxSupportPlayers = playersWithSupport
            .Where(p => p.support == playersWithSupport.Max(p => p.support))
            .ToList();

        var minSupportPlayers = playersWithSupport
            .Where(p => p.support == playersWithSupport.Min(p => p.support))
            .ToList();

        var maxPlayer = maxSupportPlayers[UnityEngine.Random.Range(0, maxSupportPlayers.Count)];
        var minPlayer = minSupportPlayers[UnityEngine.Random.Range(0, minSupportPlayers.Count)];

        var maxSupportRef = dbRefPlayersStats
            .Child(maxPlayer.playerId)
            .Child("stats")
            .Child("support")
            .Child(chosenRegion.ToString());

        var minSupportRef = dbRefPlayersStats
            .Child(minPlayer.playerId)
            .Child("stats")
            .Child("support")
            .Child(chosenRegion.ToString());

        await maxSupportRef.SetValueAsync(minPlayer.support);
        await minSupportRef.SetValueAsync(maxPlayer.support);

    }

    private async Task ExchangeSupport(int chosenRegion, string enemyId, bool isBonus)
    {
        DatabaseReference dbRefPlayersStats = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(DataTransfer.LobbyId)
            .Child("players");

        var playerSupportRef = dbRefPlayersStats
            .Child(playerId)
            .Child("stats")
            .Child("support")
            .Child(chosenRegion.ToString());

        var enemySupportRef = dbRefPlayersStats
            .Child(enemyId)
            .Child("stats")
            .Child("support")
            .Child(chosenRegion.ToString());

        var playerSupportSnapshot = await playerSupportRef.GetValueAsync();
        var enemySupportSnapshot = await enemySupportRef.GetValueAsync();

        if (!playerSupportSnapshot.Exists || !enemySupportSnapshot.Exists)
        {
            Debug.LogError("Support data not found for player or enemy in the chosen region.");
            return;
        }

        if (!int.TryParse(playerSupportSnapshot.Value.ToString(), out int playerSupport) ||
            !int.TryParse(enemySupportSnapshot.Value.ToString(), out int enemySupport))
        {
            Debug.LogError("Failed to parse support values.");
            return;
        }

        if(isBonus)
        {
            enemySupport--;
            playerSupport++;
        }

        await playerSupportRef.SetValueAsync(enemySupport);
        await enemySupportRef.SetValueAsync(playerSupport);

    }

    private async Task<int> CountCardsOnHand(string playerId)
    {
        string lobbyId = DataTransfer.LobbyId;

        var deckRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("deck");

        var snapshot = await deckRef.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.LogWarning($"Deck for player {playerId} does not exist.");
            return 0;
        }

        int cardsOnHand = 0;

        foreach (var cardSnapshot in snapshot.Children)
        {
            if (cardSnapshot.Child("onHand").Exists &&
                bool.TryParse(cardSnapshot.Child("onHand").Value.ToString(), out bool onHand) &&
                onHand)
            {
                cardsOnHand++;
            }
        }

        return cardsOnHand;
    }

    private async Task ChangeIncomePerCard(int chosenRegion, bool isBonus, string instanceId)
    {
        var playersRef = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        var playersSnapshot = await playersRef.GetValueAsync();
        if (!playersSnapshot.Exists)
        {
            Debug.LogError("No players data found in session.");
            return;
        }

        foreach (var playerSnapshot in playersSnapshot.Children)
        {
            string currentPlayerId = playerSnapshot.Key;

            var supportRef = playerSnapshot
                .Child("stats")
                .Child("support")
                .Child(chosenRegion.ToString());

            if (!supportRef.Exists || !int.TryParse(supportRef.Value.ToString(), out int support) || support <= 0)
            {
                continue;
            }

            if (isBonus && currentPlayerId == playerId)
                continue;

            int cardsOnHand = await CountCardsOnHand(currentPlayerId);

            if (currentPlayerId == playerId)
            {
                var cardRef = playersRef.Child(currentPlayerId).Child("deck").Child(instanceId);
                var cardSnapshot = await cardRef.GetValueAsync();

                if (cardSnapshot.Exists &&
                    cardSnapshot.Child("onHand").Exists &&
                    bool.TryParse(cardSnapshot.Child("onHand").Value.ToString(), out bool isOnHand) &&
                    isOnHand)
                {
                    cardsOnHand--;
                }
            }

            if (cardsOnHand > 0)
            {
                var incomeRef = playersRef.Child(currentPlayerId).Child("stats").Child("income");
                var incomeSnapshot = await incomeRef.GetValueAsync();

                if (incomeSnapshot.Exists && int.TryParse(incomeSnapshot.Value.ToString(), out int income))
                {
                    int newIncome = Mathf.Max(0, income - cardsOnHand);
                    await incomeRef.SetValueAsync(newIncome);
                }
            }
        }
    }

}
