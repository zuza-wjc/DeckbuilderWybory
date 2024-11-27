using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using UnityEngine;

public class AddRemoveCardImp : MonoBehaviour
{
    private readonly string lobbyId = DataTransfer.LobbyId;
    private readonly string playerId = DataTransfer.PlayerId;

    public PlayerListManager playerListManager;
    public MapManager mapManager;
    public DeckController deckController;
    public CardUtilities cardUtilities;

    void Start()
    {
        playerListManager.Initialize(lobbyId, playerId);
    }


    public async void CardLibrary(string instanceId, string cardIdDropped, bool ignoreCost)
    {
        bool tmp = await cardUtilities.CheckCardLimit(playerId);
        if (tmp)
        {
            Debug.Log("Limit kart w turze to 1");
            return;
        }

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        Dictionary<int, OptionData> budgetOptionsDictionary = new();
        Dictionary<int, OptionData> budgetBonusOptionsDictionary = new();
        Dictionary<int, OptionData> incomeOptionsDictionary = new();
        Dictionary<int, OptionData> incomeBonusOptionsDictionary = new();
        Dictionary<int, OptionData> supportOptionsDictionary = new();
        Dictionary<int, OptionData> supportBonusOptionsDictionary = new();
        Dictionary<int, OptionDataCard> cardsOptionsDictionary = new();
        Dictionary<int, OptionDataCard> cardsBonusOptionsDictionary = new();

        string lobbyId = DataTransfer.LobbyId;
        int cost = -1, playerBudget = -1, playerIncome = -1;
        int roundChange = -1, chosenRegion = -1;
        bool budgetChange = false, incomeChange = false, supportChange = false, cardsChange = false, isBonusRegion = false;
        string enemyId = string.Empty, cardType = string.Empty;

        DatabaseReference dbRefCard = FirebaseInitializer.DatabaseReference.Child("cards").Child("id").Child("addRemove").Child(cardIdDropped);
        DataSnapshot snapshot = await dbRefCard.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.LogError($"No data for: {cardIdDropped}.");
            return;
        }

        DataSnapshot costSnapshot = snapshot.Child("cost");
        cost = costSnapshot.Exists ? Convert.ToInt32(costSnapshot.Value) : 0;

        if (DataTransfer.IsFirstCardInTurn)
        {
            if (await cardUtilities.CheckIncreaseCost(playerId))
            {
                if (cost >= 0)
                {
                    double increasedCost = 1.5 * cost;
                    cost = (cost % 2 != 0) ? (int)Math.Ceiling(increasedCost) : (int)increasedCost;
                }
                else
                {
                    Debug.LogWarning("Cost is negative, not increasing cost.");
                }
            }
        }

        if (await cardUtilities.CheckIncreaseCostAllTurn(playerId))
        {
            if (cost >= 0)
            {
                double increasedCost = 1.5 * cost;
                cost = (cost % 2 != 0) ? (int)Math.Ceiling(increasedCost) : (int)increasedCost;
            }
            else
            {
                Debug.LogWarning("Cost is negative, not increasing cost.");
            }
        }

        if (await cardUtilities.CheckDecreaseCost(playerId))
        {
            if (cost >= 0)
            {
                double decreasedCost = 0.5 * cost;
                cost = (cost % 2 != 0) ? (int)Math.Floor(decreasedCost) : (int)decreasedCost;
            }
            else
            {
                Debug.LogWarning("Cost is negative, not decreasing cost.");
            }
        }

        DataSnapshot typeSnapshot = snapshot.Child("type");
        cardType = typeSnapshot.Exists ? typeSnapshot.Value.ToString() : string.Empty;

        DataSnapshot roundsSnapshot = snapshot.Child("rounds");
        roundChange = roundsSnapshot.Exists ? Convert.ToInt32(roundsSnapshot.Value) : -1;

        if (snapshot.Child("budget").Exists)
        {
            budgetChange = true;
            cardUtilities.ProcessBonusOptions(snapshot.Child("budget"), budgetBonusOptionsDictionary);
            cardUtilities.ProcessOptions(snapshot.Child("budget"), budgetOptionsDictionary);
        }
        if (snapshot.Child("income").Exists)
        {
            incomeChange = true;
            cardUtilities.ProcessBonusOptions(snapshot.Child("income"), incomeBonusOptionsDictionary);
            cardUtilities.ProcessOptions(snapshot.Child("income"), incomeOptionsDictionary);
        }
        if (snapshot.Child("support").Exists)
        {
            supportChange = true;
            if (await cardUtilities.CheckSupportBlock(playerId))
            {
                Debug.Log("support block");
                return;
            }

            cardUtilities.ProcessBonusOptions(snapshot.Child("support"), supportBonusOptionsDictionary);
            cardUtilities.ProcessOptions(snapshot.Child("support"), supportOptionsDictionary);
        }
        if (snapshot.Child("cardsOnHand").Exists)
        {
            cardsChange = true;
            cardUtilities.ProcessBonusOptionsCard(snapshot.Child("cardsOnHand"), cardsBonusOptionsDictionary);
            cardUtilities.ProcessOptionsCard(snapshot.Child("cardsOnHand"), cardsOptionsDictionary);
        }

        DatabaseReference dbRefPlayerStats = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("stats");
        DataSnapshot playerStatsSnapshot = await dbRefPlayerStats.GetValueAsync();

        if (!playerStatsSnapshot.Exists)
        {
            Debug.LogError($"No data for player: {playerId}.");
            return;
        }

        DataSnapshot moneySnapshot = playerStatsSnapshot.Child("money");
        playerBudget = moneySnapshot.Exists ? Convert.ToInt32(moneySnapshot.Value) : 0;

        if (!ignoreCost && playerBudget < cost)
        {
            Debug.LogError("Not enough budget to play the card.");
            return;
        }

        DataSnapshot incomeSnapshot = playerStatsSnapshot.Child("income");
        playerIncome = incomeSnapshot.Exists ? Convert.ToInt32(incomeSnapshot.Value) : 0;

        ignoreCost = await cardUtilities.CheckIgnoreCost(playerId);

        if (await cardUtilities.CheckBlockedCard(playerId)) return;

        if (!ignoreCost)
        {
            await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget - cost);
            playerBudget -= cost;
        }
        else
        {
            ignoreCost = false;
        }

        if (supportChange)
        {
            (chosenRegion, isBonusRegion, enemyId) = await SupportAction(cardIdDropped, chosenRegion, isBonusRegion, cardType, enemyId, cardsChange, supportOptionsDictionary, supportBonusOptionsDictionary, cardsBonusOptionsDictionary);
        }

        if (budgetChange)
        {
            (dbRefPlayerStats, chosenRegion, isBonusRegion, playerBudget, enemyId) = await BudgetAction(dbRefPlayerStats, cardIdDropped, chosenRegion, isBonusRegion, cardType, budgetOptionsDictionary, budgetBonusOptionsDictionary, playerBudget, enemyId);
        }

        if (incomeChange)
        {
            (dbRefPlayerStats, playerBudget) = await IncomeAction(isBonusRegion, incomeOptionsDictionary, enemyId, incomeBonusOptionsDictionary, playerIncome, dbRefPlayerStats, chosenRegion, playerBudget);
        }

        if (roundChange != 0)
        {
            await RoundAction(roundChange);
        }

        if (ignoreCost)
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
                return;
            }
        }

        DatabaseReference dbRefPlayerDeck = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck").Child(instanceId);
        await dbRefPlayerDeck.Child("onHand").SetValueAsync(false);
        await dbRefPlayerDeck.Child("played").SetValueAsync(true);

        DataTransfer.IsFirstCardInTurn = false;

        await cardUtilities.CheckIfPlayed2Cards(playerId);

        tmp = await cardUtilities.CheckCardLimit(playerId);
    }



    private async Task<(DatabaseReference dbRefPlayerStats, int chosenRegion, bool isBonusRegion, int playerBudget, string enemyId)>BudgetAction(DatabaseReference dbRefPlayerStats,string cardId, int chosenRegion,
        bool isBonusRegion, string cardType,Dictionary<int, OptionData> budgetOptionsDictionary, Dictionary<int, OptionData> budgetBonusOptionsDictionary,int playerBudget, string enemyId)
    {
        if(cardId == "AD090")
        {
            chosenRegion = await mapManager.SelectArea();
            isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
        }

        var isBonus = isBonusRegion;
        var optionsToApply = isBonus ? budgetBonusOptionsDictionary : budgetOptionsDictionary;

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No options to apply.");
            return (dbRefPlayerStats,-1,false,-1,null);
        }

        if (isBonus)
        {
            Debug.Log("Bonus region detected.");
        }

        foreach (var data in optionsToApply.Values)
        {
            if (data == null)
            {
                Debug.LogWarning("Encountered null data, skipping.");
                continue;
            }

            if (data.Target == "player")
            {
                if(cardId == "AD059")
                {
                    await BonusBudget();
                } else if(cardId == "AD057")
                {
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        enemyId = await playerListManager.SelectEnemyPlayer();
                        if (string.IsNullOrEmpty(enemyId))
                        {
                            Debug.LogError("Failed to select an enemy player.");
                            return (dbRefPlayerStats, -1, false, -1, null); 
                        }
                    }
                    if (await cardUtilities.CheckIfProtected(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony nie mo¿na zagraæ karty");
                        return (dbRefPlayerStats, -1, false, -1, null); 
                    }
                    else if (await cardUtilities.CheckIfProtectedOneCard(enemyId, -1))
                    {
                        Debug.Log("Gracz jest chroniony nie mo¿na zagraæ karty");
                        return (dbRefPlayerStats, -1, false, -1, null);
                    }
                    else
                    {
                        await MoreThan2Cards(enemyId);
                    }

                }
                else if(cardId == "AD007")
                {
                    await ProtectRegions();

                } else if (cardId == "AD042")
                {
                   if (!(await cardUtilities.CheckBudgetBlock(playerId)))
                    {

                    int areas = await CountMinSupport(playerId, data.Number);
                    int budgetMulti = data.Number * areas;
                    playerBudget += budgetMulti;
                    await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
                    await cardUtilities.CheckAndAddCopyBudget(playerId, budgetMulti);
                }

            } else
                {
                    if (!(await cardUtilities.CheckBudgetBlock(playerId)))
                    {
                        playerBudget += data.Number;
                        await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
                        await cardUtilities.CheckAndAddCopyBudget(playerId, data.Number);
                    }
                }

            }
            else if (data.Target == "enemy")
            {

                if (data.TargetNumber == 7)
                {

                    await ChangeAllStats(data.Number, playerId, "money");
                }
                else
                {
                    if(cardId == "AD047")
                    {
                       // await BudgetPenalty();
                        
                    } else if(cardId == "AD090")
                    {
                        enemyId = await HighestSupportInArea(chosenRegion);
                        playerBudget = await cardUtilities.ChangeEnemyStat(enemyId, data.Number, "money",playerBudget);
                        await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
                    }
                    else {
                        if (string.IsNullOrEmpty(enemyId))
                        {
                            enemyId = await playerListManager.SelectEnemyPlayer();
                            if (string.IsNullOrEmpty(enemyId))
                            {
                                Debug.LogError("Failed to select an enemy player.");
                                return (dbRefPlayerStats, -1, false, -1, null); 
                            }
                        }
                        playerBudget = await cardUtilities.ChangeEnemyStat(enemyId, data.Number, "money", playerBudget);
                        await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
                    }
                }
            }
        }
        return (dbRefPlayerStats,chosenRegion,isBonusRegion,playerBudget,enemyId);
    }

    private async Task<(int chosenRegion,bool isBonusRegion, string enemyId)> SupportAction(string cardId, int chosenRegion,bool isBonusRegion, string cardType, string enemyId, bool cardsChange,
        Dictionary<int, OptionData> supportOptionsDictionary,Dictionary<int, OptionData> supportBonusOptionsDictionary,Dictionary<int, OptionDataCard> cardsBonusOptionsDictionary)
    {
        if (cardId == "AD091")
        {
            chosenRegion = await mapManager.SelectArea();
            isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
        }

        var isBonus = isBonusRegion;
        var optionsToApply = isBonus ? supportBonusOptionsDictionary : supportOptionsDictionary;

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No support options available.");
            return (-1,false,null);
        }

        if (cardId == "AD069" || cardId == "AD071")
        {
            chosenRegion = await mapManager.SelectArea();
            isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
        }

        if (isBonus)
        {
            Debug.Log("Bonus region detected.");
        }

        foreach (var data in optionsToApply.Values)
        {
            if (data == null)
            {
                Debug.LogWarning("Encountered null data in support options, skipping.");
                continue;
            }

            if (data.Target == "enemy-region")
            {
                if (data.TargetNumber == 8)
                {
                    await ChangeAllSupport(data.Number);
                }
                else
                {
                    if(chosenRegion < 0)
                    {
                        chosenRegion = await mapManager.SelectArea();
                    }

                    if (data.TargetNumber == 7)
                    {
                        await ChangeAreaSupport(chosenRegion, data.Number, playerId);
                    }
                    else
                    {
                        if(cardId == "AD091")
                        {
                            enemyId = await LowestSupportInArea(chosenRegion);
                            await cardUtilities.ChangeSupport(playerId, data.Number, chosenRegion, cardId, mapManager);
                            isBonusRegion = false;
                        }
                        else
                        {
                            enemyId = await playerListManager.SelectEnemyPlayerInArea(chosenRegion);
                            if (string.IsNullOrEmpty(enemyId))
                            {
                                Debug.LogError("No enemy player found in the area.");
                                return (-1, false, null);
                            }
                            await cardUtilities.ChangeSupport(enemyId, data.Number, chosenRegion, cardId, mapManager);

                            if (isBonusRegion && cardsChange && (cardId == "AD069" || cardId == "AD071"))
                            {
                                if (cardsBonusOptionsDictionary?.Values == null || !cardsBonusOptionsDictionary.Values.Any())
                                {
                                    Debug.LogError("No card options available.");
                                    return (-1, false, null);
                                }

                                foreach (var cardData in cardsBonusOptionsDictionary.Values)
                                {
                                    Debug.Log($"Processing card: {cardData.Source} -> {cardData.Target} with {cardData.CardNumber} cards.");

                                    string source = "";
                                    string target = "";

                                    if (cardData.Source == "player-deck") { source = playerId; }
                                    if (cardData.Target == "player") { target = playerId; }

                                    if (cardData.CardNumber > 0)
                                    {
                                        for (int i = 0; i < cardData.CardNumber; i++)
                                        {
                                            try
                                            {
                                                await deckController.GetCardFromDeck(source, target);
                                            }
                                            catch (Exception ex)
                                            {
                                                Debug.LogError($"Error while calling GetCard: {ex.Message}");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogWarning("Card number is not greater than 0.");
                                    }
                                }

                            }
                        }
                    }
                }
            }
            else if (data.Target == "player-region")
            {
                if (chosenRegion < 0)
                {
                    chosenRegion = await mapManager.SelectArea();
                }
                await cardUtilities.ChangeSupport(playerId, data.Number, chosenRegion, cardId, mapManager);

                if(cardId== "AD075")
                {
                    isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
                    if(isBonusRegion)
                    {
                        await IgnoreCost();
                    }
                }
            }
            else if (data.Target == "player-random")
            {
                if (cardId == "AD046")
                {
                    List<int> areas = await CheckHighestSupport(playerId);
                    foreach (var regionId in areas)
                    {
                        await cardUtilities.ChangeSupport(playerId, data.Number, regionId, cardId, mapManager);
                    }
                }
                else
                {
                    chosenRegion = await cardUtilities.RandomizeRegion(playerId, data.Number,mapManager);
                    Debug.Log($"Wylosowany region to {chosenRegion}");
                    await cardUtilities.ChangeSupport(playerId, data.Number, chosenRegion, cardId, mapManager);
                }
            }
            else if (data.Target == "enemy-random")
            {
                enemyId = await playerListManager.SelectEnemyPlayer();
                chosenRegion = await cardUtilities.RandomizeRegion(enemyId, data.Number, mapManager);
                Debug.Log($"Wylosowany region to {chosenRegion}");
                await cardUtilities.ChangeSupport(enemyId, data.Number, chosenRegion, cardId, mapManager);
            }
        }

        return (chosenRegion, isBonusRegion, enemyId);
    }

    private async Task<(DatabaseReference dbRefPlayerStats,int playerBudget)> IncomeAction(bool isBonusRegion,
        Dictionary<int, OptionData> incomeOptionsDictionary,string enemyId,Dictionary<int, OptionData> incomeBonusOptionsDictionary,
        int playerIncome, DatabaseReference dbRefPlayerStats,int chosenRegion, int playerBudget)
    {
        var optionsToApply = isBonusRegion ? incomeBonusOptionsDictionary : incomeOptionsDictionary;

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogWarning("No income options available.");
            return (dbRefPlayerStats,-1);
        }

        if (isBonusRegion)
        {
            Debug.Log("Bonus jest zagrany");
        }

        foreach (var data in optionsToApply.Values)
        {
            if (data == null)
            {
                Debug.LogWarning("Encountered null data in income options, skipping.");
                continue;
            }

            if (data.Target == "player")
            {
                if (!(await cardUtilities.CheckIncomeBlock(playerId)))
                {
                    playerIncome += data.Number;
                    if (playerIncome < 0)
                    {
                        Debug.Log("Nie wystaraczaj¹cy przychód aby zagraæ kartê");
                    }
                    else
                    {
                        await dbRefPlayerStats.Child("income").SetValueAsync(playerIncome);
                    }
                }
            }
            else if (data.Target == "enemy")
            {

                if (data.TargetNumber == 7)
                {
                    await ChangeAllStats(data.Number, playerId, "income");
                }
                else
                {

                    if (string.IsNullOrEmpty(enemyId))
                    {
                        enemyId = await playerListManager.SelectEnemyPlayer();
                        if (string.IsNullOrEmpty(enemyId))
                        {
                            Debug.LogError("Failed to select an enemy player.");
                            return (dbRefPlayerStats, -1);
                        }
                    }

                    playerBudget = await cardUtilities.ChangeEnemyStat(enemyId, data.Number, "income", playerBudget);
                    await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
                }
            }
            else if (data.Target == "enemy-region")
            {
                if (data.TargetNumber == 7)
                {
                    await ChangeAreaIncome(chosenRegion, data.Number, playerId);
                }
            }
        }
        return (dbRefPlayerStats, playerBudget);
    }

    private async Task RoundAction(int roundChange)
    {
        DatabaseReference dbRefRounds = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("rounds");

        try
        {
            DataSnapshot snapshot = await dbRefRounds.GetValueAsync();

            if (!snapshot.Exists || snapshot.Value == null || !int.TryParse(snapshot.Value.ToString(), out int currentRound))
            {
                Debug.LogError("Failed to retrieve or parse round data.");
                return;
            }

            int updatedRound = currentRound + roundChange;
            await dbRefRounds.SetValueAsync(updatedRound);
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while updating the round: {ex.Message}");
        }
    }

    private async Task ChangeAreaIncome(int areaId, int value, string cardholderId)
    {
        DatabaseReference dbRefAllPlayersStats = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");

        try
        {
            var snapshot = await dbRefAllPlayersStats.GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError("No data found for players in the session.");
                return;
            }

            foreach (var playerSnapshot in snapshot.Children)
            {
                string playerID = playerSnapshot.Key;

                if (playerID == cardholderId) continue;

                var playerStatsSnapshot = playerSnapshot.Child("stats");
                var playerSupportSnapshot = playerStatsSnapshot.Child("support");
                var playerIncomeSnapshot = playerStatsSnapshot.Child("income");

                if (!playerSupportSnapshot.Exists || !playerIncomeSnapshot.Exists)
                {
                    Debug.LogError($"Missing support or income data for player {playerID}.");
                    continue;
                }

                var supportChildSnapshot = playerSupportSnapshot
                    .Children
                    .FirstOrDefault(s => Convert.ToInt32(s.Key) == areaId);

                if (supportChildSnapshot == null || !int.TryParse(supportChildSnapshot.Value.ToString(), out int currentSupportValue) || currentSupportValue <= 0)
                {
                    continue;
                }

                bool isProtected = await cardUtilities.CheckIfProtected(playerID, value);

                bool isProtectedOneCard = await cardUtilities.CheckIfProtectedOneCard(playerID, value);

                if (isProtected || isProtectedOneCard)
                {
                    continue;
                }


                if (int.TryParse(playerIncomeSnapshot.Value.ToString(), out int currentIncome))
                {
                    int updatedIncome = Math.Max(0, currentIncome + value);
                    await dbRefAllPlayersStats
                        .Child(playerID)
                        .Child("stats")
                        .Child("income")
                        .SetValueAsync(updatedIncome);
                }
                else
                {
                    Debug.LogError($"Invalid income data for player {playerID}.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while updating area income: {ex.Message}");
        }
    }

    private async Task ChangeAreaSupport(int areaId, int value, string cardholderId)
    {
        var dbRefAllPlayersStats = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");

        var snapshot = await dbRefAllPlayersStats.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.LogError("No data found for players in the session.");
            return;
        }

        foreach (var playerSnapshot in snapshot.Children)
        {
            string playerId = playerSnapshot.Key;

            if (playerId == cardholderId) continue;

            var playerSupportSnapshot = playerSnapshot.Child("stats").Child("support");

            if (!playerSupportSnapshot.Exists)
            {
                Debug.LogError($"Support not found for player {playerId}.");
                continue;
            }

            var supportChildSnapshot = playerSupportSnapshot.Children
                .FirstOrDefault(s => Convert.ToInt32(s.Key) == areaId);

            if (supportChildSnapshot == null)
                continue;

            if (!int.TryParse(supportChildSnapshot.Value.ToString(), out int currentSupportValue))
            {
                Debug.LogError($"Invalid support value for player {playerId} in area {areaId}. Value: {supportChildSnapshot.Value}");
                continue;
            }

            bool isRegionProtected = await cardUtilities.CheckIfRegionProtected(playerId, areaId, value);
            bool isPlayerProtected = await cardUtilities.CheckIfProtected(playerId, value);
            bool isOneCardProtected = await cardUtilities.CheckIfProtectedOneCard(playerId, value);

            if (isRegionProtected)
            {
                Debug.Log("Obszar jest chroniony, nie mo¿na zagraæ karty");
                continue;
            }

            if (isPlayerProtected)
            {
                Debug.Log("Gracz jest chroniony, nie mo¿na zagraæ karty");
                continue;
            }

            if (isOneCardProtected)
            {
                Debug.Log("Gracz jest chroniony przez jedn¹ kartê, nie mo¿na zagraæ karty");
                continue;
            }

            await cardUtilities.CheckBonusBudget(playerId, value);
            value = await cardUtilities.CheckBonusSupport(playerId, value);

            int updatedSupportValue = Math.Max(currentSupportValue + value, 0);

            var maxSupport = await mapManager.GetMaxSupportForRegion(areaId);
            var currentAreaSupport = await mapManager.GetCurrentSupportForRegion(areaId, playerId);
            updatedSupportValue = Math.Min(updatedSupportValue, maxSupport - currentAreaSupport);

            await dbRefAllPlayersStats
                .Child(playerId)
                .Child("stats")
                .Child("support")
                .Child(supportChildSnapshot.Key)
                .SetValueAsync(updatedSupportValue);

            await cardUtilities.CheckAndAddCopySupport(playerId, areaId, value, mapManager);
        }
    }

    private async Task ChangeAllSupport(int value)
    {
        DatabaseReference dbRefAllPlayersStats = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        var snapshot = await dbRefAllPlayersStats.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.LogError("No data found for players in the session.");
            return;
        }

        foreach (var playerSnapshot in snapshot.Children)
        {
            string playerId = playerSnapshot.Key;
            DataSnapshot playerSupportSnapshot = playerSnapshot.Child("stats").Child("support");

            if (!playerSupportSnapshot.Exists)
            {
                Debug.LogError($"Support not found for player {playerId}.");
                continue;
            }

            foreach (var supportChildSnapshot in playerSupportSnapshot.Children)
            {
                if (int.TryParse(supportChildSnapshot.Key, out int areaId) && int.TryParse(supportChildSnapshot.Value.ToString(), out int currentSupportValue))
                {
                    bool isRegionProtected = await cardUtilities.CheckIfRegionProtected(playerId, areaId, value);
                    bool isPlayerProtected = await cardUtilities.CheckIfProtected(playerId, value);
                    bool isOneCardProtected = await cardUtilities.CheckIfProtectedOneCard(playerId, value);

                    if (isRegionProtected || isPlayerProtected || isOneCardProtected)
                    {
                        continue;
                    }

                    await cardUtilities.CheckBonusBudget(playerId, value);
                    value = await cardUtilities.CheckBonusSupport(playerId, value);

                    int updatedSupportValue = currentSupportValue + value;
                    updatedSupportValue = Math.Max(updatedSupportValue, 0);

                    var maxSupport = await mapManager.GetMaxSupportForRegion(areaId);
                    var currentAreaSupport = await mapManager.GetCurrentSupportForRegion(areaId, playerId);

                    updatedSupportValue = Math.Min(updatedSupportValue, maxSupport - currentAreaSupport);

                    await cardUtilities.CheckIfRegionsProtected(playerId, currentSupportValue, value);
                    await dbRefAllPlayersStats
                        .Child(playerId)
                        .Child("stats")
                        .Child("support")
                        .Child(areaId.ToString())
                        .SetValueAsync(updatedSupportValue);

                    await cardUtilities.CheckAndAddCopySupport(playerId, areaId, value, mapManager);
                }
                else
                {
                    Debug.LogError($"Invalid data for player {playerId} in area {supportChildSnapshot.Key}. Ensure area ID and support value are valid integers.");
                }
            }
        }
    }

    private async Task ChangeAllStats(int value, string cardholderId, string statType)
    {
        DatabaseReference dbRefAllPlayersStats = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        var snapshot = await dbRefAllPlayersStats.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.LogError("No data found for players in the session.");
            return;
        }

        foreach (var playerSnapshot in snapshot.Children)
        {
            string playerID = playerSnapshot.Key;

            if (playerID == cardholderId)
                continue;

            DataSnapshot playerStatSnapshot = playerSnapshot.Child("stats").Child(statType);

            if (!playerStatSnapshot.Exists)
            {
                Debug.LogError($"{statType} not found for player {playerID}.");
                continue;
            }

            if (!int.TryParse(playerStatSnapshot.Value.ToString(), out int currentStat))
            {
                Debug.LogError($"Invalid {statType} value for player {playerID}. Value: {playerStatSnapshot.Value}");
                continue;
            }

            if (await IsPlayerProtected(playerID, value))
                continue;

            int updatedStat = Mathf.Max(currentStat + value, 0);

            await dbRefAllPlayersStats
                .Child(playerID)
                .Child("stats")
                .Child(statType)
                .SetValueAsync(updatedStat);

            if (statType == "money")
            {
                await cardUtilities.CheckAndAddCopyBudget(playerID, value);
            }
        }
    }

    private async Task<bool> IsPlayerProtected(string playerID, int value)
    {
        bool isProtected = await cardUtilities.CheckIfProtected(playerID, value);
        bool isOneCardProtected = await cardUtilities.CheckIfProtectedOneCard(playerID, value);

        return isProtected || isOneCardProtected;
    }

    private async Task<int> CountMinSupport(string playerId, int value)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("Player ID is null or empty.");
            return -1;
        }

        DatabaseReference dbRefSupport = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("stats")
            .Child("support");

        try
        {
            var snapshot = await dbRefSupport.GetValueAsync();

            if (!snapshot.Exists || snapshot.Value == null)
            {
                Debug.LogError($"No support data found for player ID: {playerId}");
                return 0;
            }

            int validRegions = snapshot.Children
                .Count(supportChildSnapshot =>
                    int.TryParse(supportChildSnapshot.Value?.ToString(), out int currentSupportValue) &&
                    currentSupportValue >= value);

            return validRegions;
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while counting support for player {playerId}: {ex.Message}");
            return -1;
        }
    }

    private async Task<List<int>> CheckHighestSupport(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("Player ID is null or empty.");
            return null;
        }

        DatabaseReference dbRefSupport = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        try
        {
            var snapshot = await dbRefSupport.GetValueAsync();

            if (!snapshot.Exists || snapshot.Value == null)
            {
                Debug.LogError($"No player data found for lobby: {lobbyId}");
                return null;
            }

            var highestSupportInRegion = new Dictionary<int, int>();
            var regionWithMaxSupport = new Dictionary<int, string>();

            foreach (var playerSnapshot in snapshot.Children)
            {
                var supportSnapshot = playerSnapshot.Child("stats").Child("support");

                if (!supportSnapshot.Exists)
                {
                    Debug.LogError($"No support data found for player {playerSnapshot.Key}");
                    continue;
                }

                foreach (var supportChildSnapshot in supportSnapshot.Children)
                {
                    if (int.TryParse(supportChildSnapshot.Key, out int areaId) &&
                        int.TryParse(supportChildSnapshot.Value?.ToString(), out int supportValue))
                    {
                        if (!highestSupportInRegion.ContainsKey(areaId) || supportValue > highestSupportInRegion[areaId])
                        {
                            highestSupportInRegion[areaId] = supportValue;
                            regionWithMaxSupport[areaId] = playerSnapshot.Key;
                        }
                    }
                    else
                    {
                        Debug.LogError($"Invalid data for player {playerSnapshot.Key} in region {supportChildSnapshot.Key}.");
                    }
                }
            }

            var regionsWithHighestSupport = regionWithMaxSupport
                .Where(region => region.Value == playerId)
                .Select(region => region.Key)
                .ToList();

            return regionsWithHighestSupport;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error occurred while processing data for player {playerId}: {ex.Message}");
            return null;
        }
    }

    private async Task<string> HighestSupportInArea(int chosenRegion)
    {
        if (chosenRegion < 0)
        {
            Debug.LogError("Chosen region ID must be a positive integer.");
            return null;
        }

        DatabaseReference dbRefSupport = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        try
        {
            var snapshot = await dbRefSupport.GetValueAsync();

            if (!snapshot.Exists || snapshot.Value == null)
            {
                Debug.LogError("No data found for players in the session.");
                return null;
            }

            var playersWithMaxSupport = new List<string>();
            int maxSupportValue = int.MinValue;

            foreach (var playerSnapshot in snapshot.Children)
            {
                string playerId = playerSnapshot.Key;
                var supportSnapshot = playerSnapshot.Child("stats").Child("support").Child(chosenRegion.ToString());

                if (supportSnapshot.Exists && int.TryParse(supportSnapshot.Value?.ToString(), out int supportValue))
                {
                    if (supportValue > maxSupportValue)
                    {
                        maxSupportValue = supportValue;
                        playersWithMaxSupport.Clear();
                        playersWithMaxSupport.Add(playerId);
                    }
                    else if (supportValue == maxSupportValue)
                    {
                        playersWithMaxSupport.Add(playerId);
                    }
                }
                else if (supportSnapshot.Exists)
                {
                    Debug.LogError($"Invalid support value for player {playerId} in region {chosenRegion}. Value: {supportSnapshot.Value}");
                }
            }

            if (playersWithMaxSupport.Count == 0)
            {
                Debug.Log($"No players have support in region {chosenRegion}.");
                return null;
            }

            if (playersWithMaxSupport.Count == 1)
            {
                return playersWithMaxSupport.First();
            }

            var random = new System.Random();
            int randomIndex = random.Next(playersWithMaxSupport.Count);
            return playersWithMaxSupport[randomIndex];
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while retrieving the highest support in region {chosenRegion}: {ex.Message}");
            return null;
        }
    }

    private async Task<string> LowestSupportInArea(int chosenRegion)
    {
        if (chosenRegion < 0)
        {
            Debug.LogError("Chosen region ID must be a positive integer.");
            return null;
        }

        DatabaseReference dbRefSupport = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        try
        {
            var snapshot = await dbRefSupport.GetValueAsync();

            if (!snapshot.Exists || snapshot.Value == null)
            {
                Debug.LogError("No data found for players in the session.");
                return null;
            }

            var playersWithMinSupport = new List<string>();
            int minSupportValue = int.MaxValue;

            foreach (var playerSnapshot in snapshot.Children)
            {
                string playerId = playerSnapshot.Key;
                var supportSnapshot = playerSnapshot.Child("stats").Child("support").Child(chosenRegion.ToString());

                if (supportSnapshot.Exists && int.TryParse(supportSnapshot.Value?.ToString(), out int supportValue))
                {
                    if (supportValue > 0)
                    {
                        if (supportValue < minSupportValue)
                        {
                            minSupportValue = supportValue;
                            playersWithMinSupport.Clear();
                            playersWithMinSupport.Add(playerId);
                        }
                        else if (supportValue == minSupportValue)
                        {
                            playersWithMinSupport.Add(playerId);
                        }
                    }
                }
                else if (supportSnapshot.Exists)
                {
                    Debug.LogError($"Invalid support value for player {playerId} in region {chosenRegion}. Value: {supportSnapshot.Value}");
                }
            }

            if (playersWithMinSupport.Count == 0)
            {
                Debug.LogWarning($"No players with non-zero support found in region {chosenRegion}.");
                return null;
            }

            if (playersWithMinSupport.Count == 1)
            {
                return playersWithMinSupport.First();
            }

            var random = new System.Random();
            int randomIndex = random.Next(playersWithMinSupport.Count);
            return playersWithMinSupport[randomIndex];
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred while retrieving the lowest support in region {chosenRegion}: {ex.Message}");
            return null;
        }
    }

    private async Task ProtectRegions()
    {
        try
        {
            DatabaseReference dbRefTurn = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(playerId)
                .Child("stats")
                .Child("turnsTaken");

            var turnSnapshot = await dbRefTurn.GetValueAsync();

            if (!turnSnapshot.Exists || turnSnapshot.Value == null)
            {
                Debug.LogError("Failed to retrieve the number of turns taken for the player.");
                return;
            }

            if (!int.TryParse(turnSnapshot.Value.ToString(), out int turnsTaken))
            {
                Debug.LogError($"Invalid turn value for player {playerId}. Value: {turnSnapshot.Value}");
                return;
            }

            DatabaseReference dbRefProtected = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(playerId)
                .Child("protected")
                .Child("allRegions");

            await dbRefProtected.SetValueAsync(turnsTaken);
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred in ProtectRegions: {ex.Message}");
        }
    }

    /*private async Task BudgetPenalty()
    {
        DatabaseReference dbRefPlayers = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        DataSnapshot playersSnapshot = await dbRefPlayers.GetValueAsync();

        if (!playersSnapshot.Exists)
        {
            Debug.LogError("Brak danych graczy w bazie.");
            return;
        }

        int currentTurnNumber = -1;
        int currentPlayerTurnsTaken = -1; // Liczba tur wykonanych przez zagrywaj¹cego
        int maxTurnNumber = -1;
        string nextPlayerId = null;

        // ZnajdŸ aktualnego gracza i maksymalny numer tury
        foreach (var playerSnapshot in playersSnapshot.Children)
        {
            string id = playerSnapshot.Key;

            DataSnapshot turnSnapshot = playerSnapshot.Child("myTurnNumber");
            DataSnapshot turnsTakenSnapshot = playerSnapshot.Child("stats").Child("turnsTaken");

            if (turnSnapshot.Exists && turnsTakenSnapshot.Exists)
            {
                int turnNumber = Convert.ToInt32(turnSnapshot.Value);
                int turnsTaken = Convert.ToInt32(turnsTakenSnapshot.Value);

                if (id == playerId)
                {
                    currentTurnNumber = turnNumber;
                    currentPlayerTurnsTaken = turnsTaken; // Zapamiêtaj liczbê tur wykonanych
                }

                maxTurnNumber = Math.Max(maxTurnNumber, turnNumber);
            }
        }

        if (currentTurnNumber == -1 || currentPlayerTurnsTaken == -1)
        {
            Debug.LogError("Bie¿¹cy gracz lub jego statystyki nie zosta³y znalezione.");
            return;
        }

        // ZnajdŸ nastêpnego gracza (cyklicznie)
        foreach (var playerSnapshot in playersSnapshot.Children)
        {
            string id = playerSnapshot.Key;

            DataSnapshot turnSnapshot = playerSnapshot.Child("myTurnNumber");

            if (turnSnapshot.Exists)
            {
                int turnNumber = Convert.ToInt32(turnSnapshot.Value);

                if (turnNumber == currentTurnNumber + 1 ||
                    (currentTurnNumber == maxTurnNumber && turnNumber == 1))
                {
                    nextPlayerId = id;
                    break;
                }
            }
        }

        if (nextPlayerId == null)
        {
            Debug.LogError("Nie znaleziono nastêpnego gracza.");
            return;
        }

        // Dodaj karê bud¿etow¹ do nastêpnego gracza
        DatabaseReference dbRefNextPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(nextPlayerId)
            .Child("budgetPenalty");

        DataSnapshot budgetPenaltySnapshot = await dbRefNextPlayer.GetValueAsync();

        var budgetPenaltyData = new Dictionary<string, object>
    {
        { "currentPlayerTurnsTaken", currentPlayerTurnsTaken }, 
        { "playerId", playerId }
    };

        if (budgetPenaltySnapshot.Exists)
        {
            await dbRefNextPlayer.Child("turnsTaken").SetValueAsync(currentPlayerTurnsTaken);
            await dbRefNextPlayer.Child("playerId").SetValueAsync(playerId);
        }
        else
        {
            await dbRefNextPlayer.SetValueAsync(budgetPenaltyData);
        }
    }*/

    private async Task MoreThan2Cards(string enemyId)
    {
        if (string.IsNullOrEmpty(enemyId))
        {
            Debug.LogError("Enemy ID is null or empty.");
            return;
        }

        try
        {
            DatabaseReference dbRefPlayerStats = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(playerId)
                .Child("stats");

            var playerStatsSnapshot = await dbRefPlayerStats.GetValueAsync();

            if (!playerStatsSnapshot.Exists || playerStatsSnapshot.Value == null)
            {
                Debug.LogError($"Player stats for {playerId} not found or empty.");
                return;
            }

            var turnsTakenSnapshot = playerStatsSnapshot.Child("turnsTaken");

            if (!turnsTakenSnapshot.Exists || turnsTakenSnapshot.Value == null)
            {
                Debug.LogError("TurnsTaken stat not found for the player.");
                return;
            }

            if (!int.TryParse(turnsTakenSnapshot.Value.ToString(), out int turnsTaken))
            {
                Debug.LogError($"Invalid TurnsTaken value for player {playerId}. Value: {turnsTakenSnapshot.Value}");
                return;
            }

            DatabaseReference dbRefEnemy = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(enemyId)
                .Child("twoCards");

            var twoCardsData = new Dictionary<string, object>
        {
            { "turnsTaken", turnsTaken },
            { "played", 0 },
            { "playerId", playerId }
        };

            await dbRefEnemy.SetValueAsync(twoCardsData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred in MoreThan2Cards: {ex.Message}");
        }
    }

    private async Task BonusBudget()
    {
        try
        {
            DatabaseReference dbRefPlayer = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(playerId);

            DatabaseReference dbRefTurnsTaken = dbRefPlayer
                .Child("stats")
                .Child("turnsTaken");

            var turnsTakenSnapshot = await dbRefTurnsTaken.GetValueAsync();

            if (!turnsTakenSnapshot.Exists || turnsTakenSnapshot.Value == null)
            {
                Debug.LogError($"Field 'turnsTaken' does not exist or is null for player {playerId}. Cannot set bonus budget.");
                return;
            }

            if (!int.TryParse(turnsTakenSnapshot.Value.ToString(), out int turnsTaken))
            {
                Debug.LogError($"Invalid 'turnsTaken' value for player {playerId}: {turnsTakenSnapshot.Value}");
                return;
            }

            var bonusBudgetData = new Dictionary<string, object>
        {
            { "turnsTaken", turnsTaken }
        };

            await dbRefPlayer.Child("bonusBudget").SetValueAsync(bonusBudgetData);
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred in BonusBudget: {ex.Message}");
        }
    }

    private async Task IgnoreCost()
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("Player ID is null or empty.");
            return;
        }

        try
        {
            DatabaseReference dbRefPlayer = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(DataTransfer.LobbyId)
                .Child("players")
                .Child(playerId);

            var turnsTakenSnapshot = await dbRefPlayer
                .Child("stats")
                .Child("turnsTaken")
                .GetValueAsync();

            if (!turnsTakenSnapshot.Exists || turnsTakenSnapshot.Value == null)
            {
                Debug.LogError($"Field 'turnsTaken' for player {playerId} not found or is null.");
                return;
            }

            if (!int.TryParse(turnsTakenSnapshot.Value.ToString(), out int turnsTaken))
            {
                Debug.LogError($"Invalid 'turnsTaken' value for player {playerId}: {turnsTakenSnapshot.Value}");
                return;
            }

            await dbRefPlayer.Child("ignoreCost").SetValueAsync(turnsTaken);
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred in IgnoreCost: {ex.Message}");
        }
    }


}
