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
    public DeckController deckController;
    public TurnController turnController;
    void Start()
    {
        playerListManager.Initialize(lobbyId, playerId);
    }
    public async void CardLibrary(string instanceId, string cardIdDropped, bool ignoreCost)
    {
        try
        {
            bool cardLimitExceeded = await cardUtilities.CheckCardLimit(playerId);
            if (cardLimitExceeded)
            {
                Debug.Log("Limit kart w turze to 1");
                return;
            }

            if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
            {
                Debug.LogError("Firebase is not initialized properly!");
                return;
            }

            DatabaseReference dbRefCard = FirebaseInitializer.DatabaseReference
                .Child("cards")
                .Child("id")
                .Child("unique")
                .Child(cardIdDropped);

            DataSnapshot snapshot = await dbRefCard.GetValueAsync();
            if (!snapshot.Exists)
            {
                Debug.LogError("No data for: " + cardIdDropped + ".");
                return;
            }

            int cost = snapshot.Child("cost").Exists ? Convert.ToInt32(snapshot.Child("cost").Value) : throw new Exception("Branch cost does not exist.");
            string cardType = snapshot.Child("type").Exists ? snapshot.Child("type").Value.ToString() : throw new Exception("Branch type does not exist.");

            cost = await AdjustCardCost(cost);

            DatabaseReference dbRefPlayerStats = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(playerId)
                .Child("stats");

            DataSnapshot playerStatsSnapshot = await dbRefPlayerStats.GetValueAsync();
            if (!playerStatsSnapshot.Exists)
            {
                Debug.LogError("No data for player stats.");
                return;
            }

            int playerBudget = playerStatsSnapshot.Child("money").Exists ? Convert.ToInt32(playerStatsSnapshot.Child("money").Value) : throw new Exception("Branch money does not exist.");
            int playerIncome = playerStatsSnapshot.Child("income").Exists ? Convert.ToInt32(playerStatsSnapshot.Child("income").Value) : throw new Exception("Branch income does not exist.");

            if (!ignoreCost && playerBudget < cost)
            {
                Debug.LogError("Brak bud¿etu aby zagraæ kartê.");
                return;
            }

            if (!ignoreCost)
            {
                await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget - cost);
                playerBudget -= cost;
            }

            playerBudget = await SwitchCase(instanceId, dbRefPlayerStats, playerIncome, playerBudget, cardIdDropped, -1, false, cardType, string.Empty);

            if (ignoreCost)
            {
                await HandleIgnoreCost(dbRefPlayerStats, cost);
            }

            DatabaseReference dbRefPlayerDeck = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(playerId)
                .Child("deck")
                .Child(instanceId);

            await dbRefPlayerDeck.Child("onHand").SetValueAsync(false);
            await dbRefPlayerDeck.Child("played").SetValueAsync(true);

            DataTransfer.IsFirstCardInTurn = false;
            await cardUtilities.CheckIfPlayed2Cards(playerId);

            cardLimitExceeded = await cardUtilities.CheckCardLimit(playerId);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in CardLibrary method: {ex.Message}");
        }
    }

    private async Task<int> AdjustCardCost(int cost)
    {
        if (DataTransfer.IsFirstCardInTurn)
        {
            if (await cardUtilities.CheckIncreaseCost(playerId))
            {
                cost = (int)Math.Ceiling(1.5 * cost);
            }
        }

        if (await cardUtilities.CheckIncreaseCostAllTurn(playerId))
        {
            cost = (int)Math.Ceiling(1.5 * cost);
        }

        if (await cardUtilities.CheckDecreaseCost(playerId))
        {
            cost = (int)Math.Floor(0.5 * cost);
        }

        return cost;
    }

    private async Task HandleIgnoreCost(DatabaseReference dbRefPlayerStats, int cost)
    {
        DataSnapshot currentBudgetSnapshot = await dbRefPlayerStats.Child("money").GetValueAsync();
        if (currentBudgetSnapshot.Exists)
        {
            int currentBudget = Convert.ToInt32(currentBudgetSnapshot.Value);
            int updatedBudget = currentBudget + cost;
            await dbRefPlayerStats.Child("money").SetValueAsync(updatedBudget);
        }
        else
        {
            Debug.LogError("Failed to fetch current player budget.");
        }
    }

    private async Task<int> SwitchCase(string instanceId, DatabaseReference dbRefPlayerStats, int playerIncome, int playerBudget, string cardId, int chosenRegion, bool isBonusRegion, string cardType, string enemyId)
    {
        try
        {
            switch (cardId)
            {
                case "UN018":
                    if (await CheckBlockAndLog()) break;
                    chosenRegion = await mapManager.SelectArea();
                    isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
                    enemyId = await playerListManager.SelectEnemyPlayerInArea(chosenRegion);
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();

                    await ProcessEnemySupport(chosenRegion, enemyId, isBonusRegion);
                    break;

                case "UN089":
                    if (await CheckBlockAndLog()) break;

                    chosenRegion = await mapManager.SelectArea();
                    await ExchangeSupportMaxMin(playerId, chosenRegion);
                    break;

                case "UN025":
                    if (await CheckBlockAndLog()) break;

                    chosenRegion = await mapManager.SelectArea();
                    isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
                    enemyId = await playerListManager.SelectEnemyPlayerInArea(chosenRegion);
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();

                    await ExchangeSupport(chosenRegion, enemyId, isBonusRegion);
                    break;

                case "UN039":
                    List<KeyValuePair<string, string>> selectedCards = await cardSelectionUI.ShowCardSelection(playerId, 1, instanceId, true);
                    if (selectedCards.Count > 0)
                    {
                        string selectedInstanceId = selectedCards[0].Key;
                        string selectedCardId = selectedCards[0].Value;

                        cardTypeManager.OnCardDropped(selectedInstanceId, selectedCardId, true);
                    }
                    else
                    {
                        Debug.LogWarning("Nie wybrano ¿adnej karty.");
                    }
                    break;

                case "UN055":
                    if (!(await cardUtilities.CheckBudgetBlock(playerId)))
                    {
                        playerBudget += playerIncome;
                        await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
                        await cardUtilities.CheckAndAddCopyBudget(playerId, playerIncome);
                    }
                    break;

                case "UN086":
                    if (await cardUtilities.CheckSupportBlock(playerId))
                    {
                        Debug.Log("Support block");
                        break;
                    }

                    if (await cardUtilities.CheckIncomeBlock(playerId))
                    {
                        Debug.Log("Income block");
                        break;
                    }

                    chosenRegion = await mapManager.SelectArea();
                    isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
                    await ChangeIncomePerCard(chosenRegion, isBonusRegion, instanceId);
                    break;

                case "UN019":
                    chosenRegion = await mapManager.SelectArea();
                    isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
                    await ProtectRegion(chosenRegion);

                    if (isBonusRegion)
                    {
                        await deckController.GetCardFromDeck(playerId, playerId);
                    }
                    break;

                case "UN021":
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();
                    await CopySupport(enemyId);
                    break;

                case "UN022":
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();
                    await CopyBudget(enemyId);
                    break;

                case "UN024":
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();
                    await BlockCard(enemyId);
                    break;

                case "UN032":
                    if (DataTransfer.IsFirstCardInTurn)
                    {
                        await ProtectPlayer();
                        turnController.PassTurn();
                    }
                    else
                    {
                        Debug.Log("Karta mo¿e byæ zagrana tylko jako pierwsza w turze");
                    }
                    break;

                case "UN078":
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();

                    if (await cardUtilities.CheckIfProtected(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony, nie mo¿na zagraæ karty");
                        return -1;
                    }

                    if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony na jednej karcie, nie mo¿na zagraæ karty");
                        return -1;
                    }

                    await IncreaseCost(enemyId);
                    break;

                case "UN080":

                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();
                    await BlockSupportAction(enemyId);
                    playerBudget = await cardUtilities.ChangeEnemyStat(enemyId, 15, "money", playerBudget);
                    break;

                case "UN079":

                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();

                    if (await cardUtilities.CheckIfProtected(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony, nie mo¿na zagraæ karty");
                        return -1;
                    }

                    if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony na jednej karcie, nie mo¿na zagraæ karty");
                        return -1;
                    }

                    await IncreaseCostAllTurn(enemyId);
                    break;

                case "UN048":
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();

                    if (await cardUtilities.CheckIfProtected(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony, nie mo¿na zagraæ karty");
                        return -1;
                    }

                    if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony na jednej karcie, nie mo¿na zagraæ karty");
                        return -1;
                    }

                    await BlockTurn(enemyId);
                    break;

                case "UN050":
                    await DecreaseCost();
                    break;

                case "UN052":
                    await ProtectPlayer();
                    break;

                case "UN082":
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();

                    if (await cardUtilities.CheckIfProtected(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony, nie mo¿na zagraæ karty");
                        return -1;
                    }

                    if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony na jednej karcie, nie mo¿na zagraæ karty");
                        return -1;
                    }

                    await BlockBudgetAction(enemyId);
                    break;

                case "UN083":

                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId)) return HandleNoEnemyFound();

                    if (await cardUtilities.CheckIfProtected(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony, nie mo¿na zagraæ karty");
                        return -1;
                    }

                    if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony na jednej karcie, nie mo¿na zagraæ karty");
                        return -1;
                    }

                    await BlockIncomeAction(enemyId);
                    break;

                case "UN074":
                    await ProtectPlayerOneCard();
                    break;

                case "UN076":
                    await LimitCards();
                    break;

                case "UN084":
                    await BonusSupport();
                    break;

                default:
                    Debug.LogError("Unknown card ID: " + cardId + ".");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error processing card: {ex.Message}");
        }

        return playerBudget;
    }

    private async Task<bool> CheckBlockAndLog()
    {
        if (await cardUtilities.CheckSupportBlock(playerId))
        {
            Debug.Log("support block");
            return true;
        }
        return false;
    }

    private int HandleNoEnemyFound()
    {
        Debug.LogError("No enemy player found in the area.");
        return -1;
    }

    private async Task ProcessEnemySupport(int chosenRegion, string enemyId, bool isBonusRegion)
    {
        int supportValue = await GetEnemySupportFromRegion(enemyId, chosenRegion);
        if (isBonusRegion) supportValue--;

        if (supportValue != 0)
        {
            try
            {
                chosenRegion = await mapManager.SelectArea();
                await ChangeSupportNoLoss(enemyId, supportValue, chosenRegion);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing enemy support: {ex.Message}");
            }
        }
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
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error occurred while retrieving data for player {enemyId}: {ex.Message}");
        }

        return -1;
    }

    private async Task ChangeSupportNoLoss(string playerId, int value, int areaId)
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
            Debug.Log("Not enough available space for support in this region.");
            return;
        }

        if (await cardUtilities.CheckIfRegionProtected(playerId, areaId, value))
        {
            Debug.Log("Region is protected, unable to play the card.");
            return;
        }

        if (await cardUtilities.CheckIfProtected(playerId, value))
        {
            Debug.Log("Player is protected, unable to play the card.");
            return;
        }

        if (await cardUtilities.CheckIfProtectedOneCard(playerId, value))
        {
            Debug.Log("Player is protected by a one-card protection, unable to play the card.");
            return;
        }

        await cardUtilities.CheckBonusBudget(playerId, value);
        value = await cardUtilities.CheckBonusSupport(playerId, value);

        support += value;
        await dbRefSupport.SetValueAsync(support);

        await cardUtilities.CheckAndAddCopySupport(playerId, areaId, value, mapManager);
    }

    private async Task ExchangeSupportMaxMin(string cardHolderId, int chosenRegion)
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

        List<(string playerId, int support)> playersWithSupport = snapshot.Children
        .Select(playerSnapshot =>
        {
            string playerId = playerSnapshot.Key;
            var supportSnapshot = playerSnapshot
                .Child("stats")
                .Child("support")
                .Child(chosenRegion.ToString());

            return supportSnapshot.Exists && int.TryParse(supportSnapshot.Value.ToString(), out int support) && support != 0
                ? new ValueTuple<string, int>(playerId, support)
                : (string.Empty, 0);
        })
        .Where(player => player != (string.Empty, 0))
        .ToList();


        if (playersWithSupport.Count < 2)
        {
            Debug.Log("Not enough players with non-zero support in the chosen region.");
            return;
        }

        var maxSupport = playersWithSupport.Max(p => p.support);
        var minSupport = playersWithSupport.Min(p => p.support);

        var maxSupportPlayers = playersWithSupport.Where(p => p.support == maxSupport).ToList();
        var minSupportPlayers = playersWithSupport.Where(p => p.support == minSupport).ToList();

        var maxPlayer = maxSupportPlayers[UnityEngine.Random.Range(0, maxSupportPlayers.Count)];
        var minPlayer = minSupportPlayers[UnityEngine.Random.Range(0, minSupportPlayers.Count)];

        if (await IsRegionOrPlayerProtected(maxPlayer, minPlayer, chosenRegion, maxPlayer.support, minPlayer.support))
        {
            return;
        }

        await cardUtilities.CheckBonusBudget(minPlayer.playerId, minPlayer.support - maxPlayer.support);

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

        await cardUtilities.CheckAndAddCopySupport(maxPlayer.playerId, chosenRegion, maxPlayer.support - minPlayer.support, mapManager);
    }

    private async Task<bool> IsRegionOrPlayerProtected((string playerId, int support) maxPlayer, (string playerId, int support) minPlayer, int chosenRegion, int maxSupport, int minSupport)
    {
        if (await cardUtilities.CheckIfRegionProtected(maxPlayer.playerId, chosenRegion, minSupport - maxSupport))
        {
            Debug.Log("Region is protected, unable to play the card.");
            return true;
        }

        bool isProtected = await cardUtilities.CheckIfProtected(maxPlayer.playerId, minSupport - maxSupport);
        bool isProtectedOneCard = await cardUtilities.CheckIfProtectedOneCard(maxPlayer.playerId, minSupport - maxSupport);

        if (isProtected || isProtectedOneCard)
        {
            Debug.Log("Player is protected, unable to play the card.");
            return true;
        }


        return false;
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

        if (isBonus)
        {
            enemySupport--;
            playerSupport++;
        }

        bool isRegionProtected = await cardUtilities.CheckIfRegionProtected(enemyId, chosenRegion, playerSupport - enemySupport);
        bool isPlayerProtected = await cardUtilities.CheckIfProtected(enemyId, playerSupport - enemySupport);
        bool isOneCardProtected = await cardUtilities.CheckIfProtectedOneCard(enemyId, playerSupport - enemySupport);

        if (isRegionProtected || isPlayerProtected || isOneCardProtected)
        {
            Debug.Log("The area or player is protected, unable to play the card.");
            return;
        }

        await cardUtilities.CheckBonusBudget(playerId, enemySupport - playerSupport);

        await playerSupportRef.SetValueAsync(enemySupport);
        await enemySupportRef.SetValueAsync(playerSupport);

        await cardUtilities.CheckAndAddCopySupport(enemyId, chosenRegion, enemySupport - playerSupport, mapManager);
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

            int cardsOnHand = await cardUtilities.CountCardsOnHand(currentPlayerId);

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

                    bool isProtected = await cardUtilities.CheckIfProtected(currentPlayerId, -cardsOnHand) ||
                                       await cardUtilities.CheckIfProtectedOneCard(currentPlayerId, -cardsOnHand);

                    if (isProtected)
                    {
                        continue;
                    }

                    await incomeRef.SetValueAsync(newIncome);
                }
            }
        }
    }

    private async Task<int?> GetTurnsTakenAsync(string playerId)
    {
        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefTurn = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("stats")
            .Child("turnsTaken");

        DataSnapshot turnSnapshot = await dbRefTurn.GetValueAsync();

        if (turnSnapshot.Exists)
        {
            return Convert.ToInt32(turnSnapshot.Value);
        }
        else
        {
            Debug.LogError($"Nie uda³o siê pobraæ liczby tur dla gracza {playerId}.");
            return null;
        }
    }

    private async Task ProtectRegion(int regionId)
    {
        string lobbyId = DataTransfer.LobbyId;

        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null)
        {
            Debug.LogError("Nie uda³o siê pobraæ liczby tur dla gracza.");
            return;
        }

        var dbRefProtectedRegion = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("protected")
            .Child("region")
            .Child(regionId.ToString());

        await dbRefProtectedRegion.SetValueAsync(turnsTaken);
    }

    private async Task CopySupport(string enemyId)
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null) return;

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefCopySupport = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("copySupport");

        var copySupportData = new Dictionary<string, object>
    {
        { "turnsTaken", turnsTaken },
        { "enemyId", enemyId }
    };

        await dbRefCopySupport.SetValueAsync(copySupportData);
    }

    private async Task CopyBudget(string enemyId)
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null) return;

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefCopyBudget = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("copyBudget");

        var copyBudgetData = new Dictionary<string, object>
    {
        { "turnsTaken", turnsTaken },
        { "enemyId", enemyId }
    };

        await dbRefCopyBudget.SetValueAsync(copyBudgetData);
    }

    private async Task BlockCard(string enemyId)
    {
        if (await cardUtilities.CheckIfProtected(enemyId, -1))
        {
            Debug.Log("Gracz jest chroniony, nie mo¿na zagraæ karty");
            return;
        }
        if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
        {
            Debug.Log("Gracz jest chroniony, nie mo¿na zagraæ karty");
            return;
        }

        int? turnsTaken = await GetTurnsTakenAsync(enemyId);
        if (turnsTaken == null) return;

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId);

        DatabaseReference dbRefCardBlocked = dbRefPlayer.Child("cardBlocked");

        var cardBlockedData = new Dictionary<string, object>
    {
        { "turnsTaken", turnsTaken + 1 },
        { "isBlocked", true }
    };

        await dbRefCardBlocked.SetValueAsync(cardBlockedData);
    }

    private async Task ProtectPlayer()
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null) return;

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefProtected = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("protected")
            .Child("all");

        await dbRefProtected.SetValueAsync(turnsTaken);
    }

    private async Task IncreaseCost(string enemyId)
    {
        int? turnsTaken = await GetTurnsTakenAsync(enemyId);
        if (turnsTaken == null) return;

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId);

        DatabaseReference dbRefIncreaseCost = dbRefPlayer.Child("increaseCost");

        var increaseCostData = new Dictionary<string, object>
    {
        { "turnsTaken", turnsTaken + 1 }
    };

        await dbRefIncreaseCost.SetValueAsync(increaseCostData);
    }

    private async Task BlockSupportAction(string enemyId)
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null) return;

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefEnemy = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId);

        DatabaseReference dbRefBlockSupport = dbRefEnemy.Child("blockSupport");

        await dbRefBlockSupport.SetValueAsync(new Dictionary<string, object>
    {
        { "turnsTaken", turnsTaken },
        { "playerId", playerId }
    });
    }

    private async Task IncreaseCostAllTurn(string enemyId)
    {
        int? turnsTaken = await GetTurnsTakenAsync(enemyId);
        if (turnsTaken == null) return;

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId);

        DatabaseReference dbRefIncreaseCost = dbRefPlayer.Child("increaseCostAllTurn");

        var increaseCostData = new Dictionary<string, object>
    {
        { "turnsTaken", turnsTaken + 1 }
    };

        await dbRefIncreaseCost.SetValueAsync(increaseCostData);
    }

    private async Task BlockTurn(string enemyId)
    {
        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefEnemy = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId);

        var blockTurnData = new Dictionary<string, object>
    {
        { "blockTurn", true }
    };

        await dbRefEnemy.UpdateChildrenAsync(blockTurnData);
    }

    private async Task DecreaseCost()
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null) return;

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId);

        DatabaseReference dbRefDecreaseCost = dbRefPlayer.Child("decreaseCost");

        var decreaseCostData = new Dictionary<string, object>
    {
        { "turnsTaken", turnsTaken }
    };

        await dbRefDecreaseCost.SetValueAsync(decreaseCostData);
    }

    private async Task BlockBudgetAction(string enemyId)
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null) return;

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefEnemy = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId);

        DatabaseReference dbRefBlockBudget = dbRefEnemy.Child("blockBudget");

        await dbRefBlockBudget.SetValueAsync(new Dictionary<string, object>
    {
        { "turnsTaken", turnsTaken },
        { "playerId", playerId }
    });
    }

    private async Task BlockIncomeAction(string enemyId)
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null) return;

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefEnemy = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(enemyId);

        DatabaseReference dbRefBlockIncome = dbRefEnemy.Child("blockIncome");

        await dbRefBlockIncome.SetValueAsync(new Dictionary<string, object>
    {
        { "turnsTaken", turnsTaken },
        { "playerId", playerId }
    });
    }

    private async Task ProtectPlayerOneCard()
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null) return;

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefProtected = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("protected")
            .Child("allOneCard");

        await dbRefProtected.SetValueAsync(turnsTaken);
    }

    private async Task LimitCards()
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null) return;

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefPlayers = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        DataSnapshot playersSnapshot = await dbRefPlayers.GetValueAsync();

        if (!playersSnapshot.Exists)
        {
            Debug.LogError("Brak graczy w lobby.");
            return;
        }

        foreach (var playerSnapshot in playersSnapshot.Children)
        {
            string currentPlayerId = playerSnapshot.Key;

            var limitCardsData = new Dictionary<string, object>
        {
            { "playerId", playerId },
            { "playedCards", -1 },
            { "turnsTaken", turnsTaken }
        };

            var dbRefLimitCards = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(currentPlayerId)
                .Child("limitCards");

            await dbRefLimitCards.SetValueAsync(limitCardsData);
        }
    }

    private async Task BonusSupport()
    {
        int? turnsTaken = await GetTurnsTakenAsync(playerId);
        if (turnsTaken == null) return;

        string lobbyId = DataTransfer.LobbyId;

        DatabaseReference dbRefBonusSupport = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("bonusSupport");

        await dbRefBonusSupport.SetValueAsync(new Dictionary<string, object>
    {
        { "turnsTaken", turnsTaken }
    });
    }


}
