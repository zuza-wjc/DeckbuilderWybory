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
        
            Dictionary<int, OptionData> budgetOptionsDictionary = new();
            Dictionary<int, OptionData> budgetBonusOptionsDictionary = new();
            Dictionary<int, OptionData> incomeOptionsDictionary = new();
            Dictionary<int, OptionData> incomeBonusOptionsDictionary = new();
            Dictionary<int, OptionData> supportOptionsDictionary = new();
            Dictionary<int, OptionData> supportBonusOptionsDictionary = new();
            Dictionary<int, OptionDataCard> cardsOptionsDictionary = new();
            Dictionary<int, OptionDataCard> cardsBonusOptionsDictionary = new();

            budgetOptionsDictionary.Clear();
            incomeOptionsDictionary.Clear();
            supportOptionsDictionary.Clear();
            budgetBonusOptionsDictionary.Clear();
            supportBonusOptionsDictionary.Clear();
            incomeBonusOptionsDictionary.Clear();
            cardsOptionsDictionary.Clear();
            cardsBonusOptionsDictionary.Clear();

            if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
            {
                Debug.LogError("Firebase is not initialized properly!");
                return;
            }

            string lobbyId = DataTransfer.LobbyId;
            int cost = -1, playerBudget = -1, playerIncome = -1;
            int roundChange = -1, chosenRegion = -1;
            bool budgetChange = false, incomeChange = false, supportChange = false, cardsChange = false, isBonusRegion = false;
            string enemyId = string.Empty, cardType = string.Empty;

            DatabaseReference dbRefCard = FirebaseInitializer.DatabaseReference.Child("cards").Child("id").Child("addRemove").Child(cardIdDropped);
            DataSnapshot snapshot = await dbRefCard.GetValueAsync();

            if (snapshot.Exists)
            {
                DataSnapshot costSnapshot = snapshot.Child("cost");
                cost = costSnapshot.Exists ? Convert.ToInt32(costSnapshot.Value) : 0;

                if (DataTransfer.IsFirstCardInTurn)
                {
                    if (await cardUtilities.CheckIncreaseCost(playerId))
                    {
                        double increasedCost = 1.5 * cost;

                        if (cost >= 0)
                        {
                            if (cost % 2 != 0)
                            {
                                cost = (int)Math.Ceiling(increasedCost);
                            }
                            else
                            {
                                cost = (int)increasedCost;
                            }
                        }
                        else
                        {
                            Debug.LogWarning("Cost is negative, not increasing cost.");
                        }
                    }
                }

                if (await cardUtilities.CheckIncreaseCostAllTurn(playerId))
                {
                    double increasedCost = 1.5 * cost;

                    if (cost >= 0)
                    {
                        if (cost % 2 != 0)
                        {
                            cost = (int)Math.Ceiling(increasedCost);
                        }
                        else
                        {
                            cost = (int)increasedCost;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Cost is negative, not increasing cost.");
                    }
                }

                if (await cardUtilities.CheckDecreaseCost(playerId))
                {
                    double decreasedCost = 0.5 * cost;

                    if (cost >= 0)
                    {
                        if (cost % 2 != 0)
                        {
                            cost = (int)Math.Floor(decreasedCost);
                        }
                        else
                        {
                            cost = (int)decreasedCost;
                        }
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

                    if(await cardUtilities.CheckSupportBlock(playerId))
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
            }
            else
            {
                Debug.LogError("No data for: " + cardIdDropped + ".");
                return;
            }

            DatabaseReference dbRefPlayerStats = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("stats");
            DataSnapshot playerStatsSnapshot = await dbRefPlayerStats.GetValueAsync();

            if (playerStatsSnapshot.Exists)
            {
                DataSnapshot moneySnapshot = playerStatsSnapshot.Child("money");
                playerBudget = moneySnapshot.Exists ? Convert.ToInt32(moneySnapshot.Value) : 0;

                if (!ignoreCost && playerBudget < cost)
                {
                    Debug.LogError("Not enough budget to play the card.");
                    return;
                }

                DataSnapshot incomeSnapshot = playerStatsSnapshot.Child("income");
                playerIncome = incomeSnapshot.Exists ? Convert.ToInt32(incomeSnapshot.Value) : 0;
            }
            else
            {
                Debug.LogError("No data for player: " + playerId + ".");
                return;
            }

        ignoreCost = await cardUtilities.CheckIgnoreCost(playerId);

            if (!(await cardUtilities.CheckBlockedCard(playerId)))
            {

            if(!ignoreCost)
            {
                await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget - cost);
                playerBudget -= cost;
            } else
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
                        await BudgetPenalty(data.Number);
                        
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

            if (!snapshot.Exists)
            {
                Debug.LogError("Round data does not exist.");
                return;
            }

            if (snapshot.Value == null)
            {
                Debug.LogError("Round data is null.");
                return;
            }

            if (!int.TryParse(snapshot.Value.ToString(), out int currentRound))
            {
                Debug.LogError("Failed to parse round data as integer.");
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

                var supportChildSnapshot = playerSupportSnapshot.Children
                    .FirstOrDefault(s => Convert.ToInt32(s.Key) == areaId);

                if (supportChildSnapshot == null || !int.TryParse(supportChildSnapshot.Value.ToString(), out int currentSupportValue) || currentSupportValue <= 0)
                {
                    continue;
                }

                if (await cardUtilities.CheckIfProtected(playerID, value))
                {
                    continue;
                }

                if (await cardUtilities.CheckIfProtectedOneCard(playerID, value))
                {
                    continue;
                }

                if (int.TryParse(playerIncomeSnapshot.Value.ToString(), out int currentIncome))
                {
                    await cardUtilities.CheckIfBudgetPenalty(playerID, areaId);

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
        DatabaseReference dbRefAllPlayersStats = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");

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

            foreach (var supportChildSnapshot in playerSupportSnapshot.Children)
            {
                if (Convert.ToInt32(supportChildSnapshot.Key) == areaId)
                {
                    if (int.TryParse(supportChildSnapshot.Value.ToString(), out int currentSupportValue))
                    {
                        if (await cardUtilities.CheckIfRegionProtected(playerId, areaId, value))
                        {
                            Debug.Log("Obszar jest chroniony, nie mo¿na zagraæ karty");
                            return;
                        }

                        if (await cardUtilities.CheckIfProtected(playerId, value))
                        {
                            Debug.Log("Gracz jest chroniony, nie mo¿na zagraæ karty");
                            return;
                        }

                        if (await cardUtilities.CheckIfProtectedOneCard(playerId, value))
                        {
                            Debug.Log("Gracz jest chroniony, nie mo¿na zagraæ karty");
                            return;
                        }

                        await cardUtilities.CheckBonusBudget(playerId, value);

                        value = await cardUtilities.CheckBonusSupport(playerId, value);

                        int updatedSupportValue = currentSupportValue + value;

                        updatedSupportValue = Math.Max(updatedSupportValue, 0);

                        var maxSupport = await mapManager.GetMaxSupportForRegion(areaId);
                        var currentAreaSupport = await mapManager.GetCurrentSupportForRegion(areaId, playerId);
                        updatedSupportValue = Math.Min(updatedSupportValue, maxSupport - currentAreaSupport);

                        await cardUtilities.CheckIfRegionsProtected(playerId, currentSupportValue, value);

                        await cardUtilities.CheckIfBudgetPenalty(playerId, areaId);

                        await dbRefAllPlayersStats
                            .Child(playerId)
                            .Child("stats")
                            .Child("support")
                            .Child(supportChildSnapshot.Key)
                            .SetValueAsync(updatedSupportValue);

                        await cardUtilities.CheckAndAddCopySupport(playerId, areaId, value, mapManager);
                    }
                    else
                    {
                        Debug.LogError($"Invalid support value for player {playerId} in area {areaId}. Value: {supportChildSnapshot.Value}");
                    }
                }
            }
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
                if (int.TryParse(supportChildSnapshot.Key, out int areaId))
                {
                    if (int.TryParse(supportChildSnapshot.Value.ToString(), out int currentSupportValue))
                    {
                        if (await cardUtilities.CheckIfRegionProtected(playerId, areaId, value))
                        {
                            continue;
                        }

                        if (await cardUtilities.CheckIfProtected(playerId, value))
                        {
                            continue;
                        }

                        if (await cardUtilities.CheckIfProtectedOneCard(playerId, value))
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

                        await cardUtilities.CheckIfBudgetPenalty(playerId, areaId);

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
                        Debug.LogError($"Invalid support value for player {playerId} in area {areaId}. Value: {supportChildSnapshot.Value}");
                    }
                }
                else
                {
                    Debug.LogError($"Invalid area ID: {supportChildSnapshot.Key}. It must be an integer.");
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

            if (playerID == cardholderId) continue;

            DataSnapshot playerStatSnapshot = playerSnapshot.Child("stats").Child(statType);

            if (!playerStatSnapshot.Exists)
            {
                Debug.LogError($"{statType} not found for player {playerID}.");
                return;
            }

            if (!int.TryParse(playerStatSnapshot.Value.ToString(), out int currentStat))
            {
                Debug.LogError($"Invalid {statType} value for player {playerID}. Value: {playerStatSnapshot.Value}");
                return;
            }

            int updatedStat = currentStat + value;

            updatedStat = Mathf.Max(updatedStat, 0);

            if (await cardUtilities.CheckIfProtected(playerID, value))
            {
                continue;
            }

            if (await cardUtilities.CheckIfProtectedOneCard(playerID, value))
            {
                continue;
            }

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

    private async Task<int> CountMinSupport(string playerId, int value)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError($"Player ID is null or empty. ID: {playerId}");
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

            if (!snapshot.Exists)
            {
                Debug.LogError($"No player data found in the database for player ID: {playerId}");
                return -1;
            }

            int validRegions = snapshot.Children
                .Where(supportChildSnapshot =>
                    int.TryParse(supportChildSnapshot.Value.ToString(), out int currentSupportValue) &&
                    currentSupportValue >= value)
                .Count();

            return validRegions;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error occurred while retrieving data for player {playerId}: {ex.Message}");
            return -1;
        }
    }

    private async Task<List<int>> CheckHighestSupport(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError($"Player ID is null or empty. ID: {playerId}");
            return null;
        }

        DatabaseReference dbRefSupport = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        try
        {
            var snapshot = await dbRefSupport.GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError($"No player data found in the database for player ID: {playerId}");
                return null;
            }

            List<int> regionsWithHighestSupport = new();

            Dictionary<int, int> highestSupportInRegion = new();

            Dictionary<int, string> regionWithMaxSupport = new();

            foreach (var playerSnapshot in snapshot.Children)
            {
                string currentPlayerId = playerSnapshot.Key;

                var supportSnapshot = playerSnapshot.Child("stats").Child("support");

                if (!supportSnapshot.Exists)
                {
                    Debug.LogError($"No support data found for player {currentPlayerId}");
                    return null;
                }

                foreach (var supportChildSnapshot in supportSnapshot.Children)
                {
                    int areaId = Convert.ToInt32(supportChildSnapshot.Key);
                    if (int.TryParse(supportChildSnapshot.Value.ToString(), out int supportValue))
                    {
                        if (!highestSupportInRegion.ContainsKey(areaId) || supportValue > highestSupportInRegion[areaId])
                        {
                            highestSupportInRegion[areaId] = supportValue;
                            regionWithMaxSupport[areaId] = currentPlayerId;
                        }
                    }
                    else
                    {
                        Debug.LogError($"Invalid support value for player {currentPlayerId} in region {areaId}. Value: {supportChildSnapshot.Value}");
                        return null;
                    }
                }
            }

            foreach (var region in regionWithMaxSupport)
            {
                if (region.Value == playerId)
                {
                    regionsWithHighestSupport.Add(region.Key);
                }
            }

            return regionsWithHighestSupport;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error occurred while retrieving data for player {playerId}: {ex.Message}");
            return null;
        }
    }

    private async Task<string> HighestSupportInArea(int chosenRegion)
    {
        DatabaseReference dbRefSupport = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        var snapshot = await dbRefSupport.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.LogError("No data found for players in the session.");
            return null;
        }

        Dictionary<string, int> playerSupport = new();
        int maxSupportValue = int.MinValue;

        foreach (var playerSnapshot in snapshot.Children)
        {
            string playerId = playerSnapshot.Key;
            var playerSupportSnapshot = playerSnapshot.Child("stats").Child("support").Child(chosenRegion.ToString());

            if (playerSupportSnapshot.Exists)
            {
                if (int.TryParse(playerSupportSnapshot.Value.ToString(), out int supportValue))
                {
                    if (supportValue > maxSupportValue)
                    {
                        maxSupportValue = supportValue;
                        playerSupport.Clear();
                        playerSupport.Add(playerId, supportValue);
                    }
                    else if (supportValue == maxSupportValue)
                    {
                        playerSupport.Add(playerId, supportValue);
                    }
                }
                else
                {
                    Debug.LogError($"Invalid support value for player {playerId} in region {chosenRegion}. Value: {playerSupportSnapshot.Value}");
                    return null;
                }
            }
        }

        if (playerSupport.Count > 1)
        {
            System.Random rand = new();
            int randomIndex = rand.Next(playerSupport.Count);
            return playerSupport.Keys.ToArray()[randomIndex];
        }

        return playerSupport.Keys.FirstOrDefault();
    }

    private async Task<string> LowestSupportInArea(int chosenRegion)
    {
        DatabaseReference dbRefSupport = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        var snapshot = await dbRefSupport.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.LogError("No data found for players in the session.");
            return null;
        }

        Dictionary<string, int> playerSupport = new();
        int minSupportValue = int.MaxValue;

        foreach (var playerSnapshot in snapshot.Children)
        {
            string playerId = playerSnapshot.Key;
            var playerSupportSnapshot = playerSnapshot.Child("stats").Child("support").Child(chosenRegion.ToString());

            if (playerSupportSnapshot.Exists)
            {
                if (int.TryParse(playerSupportSnapshot.Value.ToString(), out int supportValue))
                {
                    if (supportValue > 0)
                    {
                        if (supportValue < minSupportValue)
                        {
                            minSupportValue = supportValue;
                            playerSupport.Clear();
                            playerSupport.Add(playerId, supportValue);
                        }
                        else if (supportValue == minSupportValue)
                        {
                            playerSupport.Add(playerId, supportValue);
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Invalid support value for player {playerId} in region {chosenRegion}. Value: {playerSupportSnapshot.Value}");
                    return null;
                }
            }
        }

        if (playerSupport.Count == 0)
        {
            Debug.LogWarning($"No players with non-zero support found in region {chosenRegion}.");
            return null;
        }

        if (playerSupport.Count > 1)
        {
            System.Random rand = new();
            int randomIndex = rand.Next(playerSupport.Count);
            return playerSupport.Keys.ToArray()[randomIndex];
        }

        return playerSupport.Keys.FirstOrDefault();
    }

    private async Task ProtectRegions()
    {
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
            int turnsTaken = Convert.ToInt32(turnSnapshot.Value);

            DatabaseReference dbRefProtected = FirebaseInitializer.DatabaseReference
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(playerId)
                .Child("protected")
                .Child("allRegions");

            await dbRefProtected.SetValueAsync(turnsTaken);

        }
        else
        {
            Debug.LogError("Nie uda³o siê pobraæ liczby tur dla gracza.");
        }
    }

    private async Task BudgetPenalty(int value)
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
        int maxTurnNumber = -1;
        string nextPlayerId = null;

        // ZnajdŸ aktualnego gracza i maksymalny numer tury
        foreach (var playerSnapshot in playersSnapshot.Children)
        {
            string id = playerSnapshot.Key;

            DataSnapshot turnSnapshot = playerSnapshot.Child("myTurnNumber");

            if (turnSnapshot.Exists)
            {
                int turnNumber = Convert.ToInt32(turnSnapshot.Value);

                if (id == playerId)
                {
                    currentTurnNumber = turnNumber;
                }

                // ŒledŸ najwiêkszy numer tury
                maxTurnNumber = Math.Max(maxTurnNumber, turnNumber);
            }
        }

        if (currentTurnNumber == -1)
        {
            Debug.LogError("Nie znaleziono bie¿¹cego gracza.");
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

                // ZnajdŸ nastêpnego gracza (cyklicznoœæ)
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
        { "value", value },
        { "currentPlayerTurnsTaken", currentTurnNumber }, // Obecny numer tury
        { "playerId", playerId }
    };

        if (budgetPenaltySnapshot.Exists)
        {
            await dbRefNextPlayer.Child("value").SetValueAsync(value);
            await dbRefNextPlayer.Child("turnsTaken").SetValueAsync(currentTurnNumber);
            await dbRefNextPlayer.Child("playerId").SetValueAsync(playerId);
        }
        else
        {
            await dbRefNextPlayer.SetValueAsync(budgetPenaltyData);
        }
    }


    private async Task MoreThan2Cards(string enemyId)
    {
        if (string.IsNullOrEmpty(enemyId))
        {
            Debug.LogError("Enemy ID is null or empty.");
            return;
        }

        DatabaseReference dbRefPlayerStats = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("stats");

        DataSnapshot playerStatsSnapshot = await dbRefPlayerStats.GetValueAsync();
        if (!playerStatsSnapshot.Exists)
        {
            Debug.LogError($"Player stats for {playerId} not found.");
            return;
        }

        DataSnapshot turnsTakenSnapshot = playerStatsSnapshot.Child("turnsTaken");
        if (!turnsTakenSnapshot.Exists)
        {
            Debug.LogError("TurnsTaken stat not found for playerId.");
            return;
        }

        int turnsTaken = Convert.ToInt32(turnsTakenSnapshot.Value);

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

    private async Task BonusBudget()
    {

        DatabaseReference dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId);

        DatabaseReference dbRefBudgetBonus = dbRefPlayer.Child("bonusBudget");

        DatabaseReference dbRefTurnsTaken = dbRefPlayer
            .Child("stats")
            .Child("turnsTaken");

        DataSnapshot turnsTakenSnapshot = await dbRefTurnsTaken.GetValueAsync();

        if (!turnsTakenSnapshot.Exists)
        {
            Debug.LogError($"Field 'turnsTaken' does not exist for player {playerId}. Cannot block card.");
            return;
        }

        int turnsTaken = Convert.ToInt32(turnsTakenSnapshot.Value);

        var bonusBudgetData = new Dictionary<string, object>
            {
                { "turnsTaken", turnsTaken }
            };

        await dbRefBudgetBonus.SetValueAsync(bonusBudgetData);
    }

    private async Task IgnoreCost()
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogError("Player ID is null or empty.");
            return;
        }

        string lobbyId = DataTransfer.LobbyId;

        var dbRefPlayer = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId);

        var statsSnapshot = await dbRefPlayer.Child("stats").GetValueAsync();
        if (!statsSnapshot.Exists || !statsSnapshot.Child("turnsTaken").Exists)
        {
            Debug.LogError($"Stats or turnsTaken for player {playerId} not found.");
            return;
        }

        int turnsTaken = Convert.ToInt32(statsSnapshot.Child("turnsTaken").Value);

        await dbRefPlayer.Child("ignoreCost").SetValueAsync(turnsTaken);

    }


}
