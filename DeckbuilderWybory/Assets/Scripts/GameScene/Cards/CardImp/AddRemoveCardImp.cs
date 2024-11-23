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


    public async void CardLibrary(string cardIdDropped, bool ignoreCost)
    {
        DatabaseReference dbRefCard;
        DatabaseReference dbRefPlayerStats;
        DatabaseReference dbRefPlayerDeck;

        bool budgetChange = false;
        bool incomeChange = false;
        bool supportChange = false;
        int roundChange = -1;
        bool cardsChange = false;

        int cost;
        int playerBudget;
        int playerIncome;
        string enemyId = string.Empty;
        string cardType;

        int chosenRegion =1;
        bool isBonusRegion = false;

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

        dbRefCard = FirebaseInitializer.DatabaseReference.Child("cards").Child("id").Child("addRemove").Child(cardIdDropped);

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

            DataSnapshot roundsSnapshot = snapshot.Child("rounds");
            if (roundsSnapshot.Exists)
            {
                roundChange = Convert.ToInt32(roundsSnapshot.Value);
            }

            DataSnapshot budgetSnapshot = snapshot.Child("budget");
            if (budgetSnapshot.Exists)
            {
                budgetChange = true;
                cardUtilities.ProcessBonusOptions(budgetSnapshot, budgetBonusOptionsDictionary);
                cardUtilities.ProcessOptions(budgetSnapshot, budgetOptionsDictionary);
            }

            DataSnapshot incomeSnapshot = snapshot.Child("income");
            if (incomeSnapshot.Exists)
            {
                incomeChange = true;
                cardUtilities.ProcessBonusOptions(incomeSnapshot, incomeBonusOptionsDictionary);
                cardUtilities.ProcessOptions(incomeSnapshot, incomeOptionsDictionary);
            }

            DataSnapshot supportSnapshot = snapshot.Child("support");
            if (supportSnapshot.Exists)
            {
                supportChange = true;
                cardUtilities.ProcessBonusOptions(supportSnapshot, supportBonusOptionsDictionary);
                cardUtilities.ProcessOptions(supportSnapshot, supportOptionsDictionary);
            }

            DataSnapshot cardsSnapshot = snapshot.Child("cardsOnHand");
            if (cardsSnapshot.Exists)
            {
                cardsChange = true;

                cardUtilities.ProcessBonusOptionsCard(cardsSnapshot, cardsBonusOptionsDictionary);
                cardUtilities.ProcessOptionsCard(cardsSnapshot, cardsOptionsDictionary);
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

        if (supportChange)
        {
            (chosenRegion, isBonusRegion, enemyId)  = await SupportAction(cardIdDropped,chosenRegion,isBonusRegion,cardType,
                enemyId,cardsChange,supportOptionsDictionary,supportBonusOptionsDictionary,cardsBonusOptionsDictionary);
        }

        if (budgetChange)
        {
            (dbRefPlayerStats, chosenRegion, isBonusRegion, playerBudget, enemyId) = await BudgetAction(dbRefPlayerStats,
                cardIdDropped, chosenRegion,isBonusRegion,cardType,budgetOptionsDictionary,budgetBonusOptionsDictionary,
                playerBudget,enemyId);
        }

        if (incomeChange)
        {
            (dbRefPlayerStats, playerBudget) = await IncomeAction(isBonusRegion,incomeOptionsDictionary,enemyId,
                incomeBonusOptionsDictionary,playerIncome,dbRefPlayerStats,chosenRegion,playerBudget);
        }

        if(roundChange != 0)
        {
            await RoundAction(roundChange);
        }

        if(!ignoreCost)
        {
            await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget - cost);
        }

        dbRefPlayerDeck = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck").Child(cardIdDropped);

        await dbRefPlayerDeck.Child("onHand").SetValueAsync(false);
        await dbRefPlayerDeck.Child("played").SetValueAsync(true);
    }

    private async Task<(DatabaseReference dbRefPlayerStats, int chosenRegion, bool isBonusRegion, int playerBudget, string enemyId)>
        BudgetAction(DatabaseReference dbRefPlayerStats,string cardId, int chosenRegion, bool isBonusRegion, string cardType,
        Dictionary<int, OptionData> budgetOptionsDictionary, Dictionary<int, OptionData> budgetBonusOptionsDictionary,
        int playerBudget, string enemyId)
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
                if (cardId == "AD042")
                {
                    int areas = await CountMinSupport(playerId, data.Number);
                    int budgetMulti = data.Number * areas;
                    playerBudget += budgetMulti;
                    await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);

                } else
                {
                    playerBudget += data.Number;
                    await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
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
                    if(cardId == "AD090")
                    {
                        enemyId = await HighestSupportInArea(chosenRegion);
                        await cardUtilities.ChangeEnemyStat(enemyId, data.Number, "money",playerBudget);
                        await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
                    }
                    else {
                        if (string.IsNullOrEmpty(enemyId))
                        {
                            enemyId = await playerListManager.SelectEnemyPlayer();
                            if (string.IsNullOrEmpty(enemyId))
                            {
                                Debug.LogError("Failed to select an enemy player.");
                                return (dbRefPlayerStats, -1, false, -1, null); ;
                            }
                        }
                        await cardUtilities.ChangeEnemyStat(enemyId, data.Number, "money", playerBudget);
                        await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
                    }
                }
            }
        }
        return (dbRefPlayerStats,chosenRegion,isBonusRegion,playerBudget,enemyId);
    }

    private async Task<(int chosenRegion,bool isBonusRegion, string enemyId)> SupportAction(string cardId, int chosenRegion,
        bool isBonusRegion, string cardType, string enemyId, bool cardsChange,Dictionary<int, OptionData> supportOptionsDictionary,
        Dictionary<int, OptionData> supportBonusOptionsDictionary,Dictionary<int, OptionDataCard> cardsBonusOptionsDictionary)
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
                playerIncome += data.Number;
                if(playerIncome < 0)
                {
                    Debug.Log("Nie wystaraczaj¹cy przychód aby zagraæ kartê");
                } else
                {
                    await dbRefPlayerStats.Child("income").SetValueAsync(playerIncome);
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

                    await cardUtilities.ChangeEnemyStat(enemyId, data.Number, "income", playerBudget);
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
                        int updatedSupportValue = currentSupportValue + value;

                        updatedSupportValue = Math.Max(updatedSupportValue, 0);

                        var maxSupport = await mapManager.GetMaxSupportForRegion(areaId);
                        var currentAreaSupport = await mapManager.GetCurrentSupportForRegion(areaId, playerId);
                        updatedSupportValue = Math.Min(updatedSupportValue, maxSupport - currentAreaSupport);

                        await dbRefAllPlayersStats
                            .Child(playerId)
                            .Child("stats")
                            .Child("support")
                            .Child(supportChildSnapshot.Key)
                            .SetValueAsync(updatedSupportValue);
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
                if (int.TryParse(supportChildSnapshot.Value.ToString(), out int currentSupportValue))
                {
                    int updatedSupportValue = currentSupportValue + value;

                    updatedSupportValue = Math.Max(updatedSupportValue, 0);

                    var maxSupport = await mapManager.GetMaxSupportForRegion(Convert.ToInt32(supportChildSnapshot.Key));
                    var currentAreaSupport = await mapManager.GetCurrentSupportForRegion(Convert.ToInt32(supportChildSnapshot.Key), playerId);
                    updatedSupportValue = Math.Min(updatedSupportValue, maxSupport - currentAreaSupport);

                    await dbRefAllPlayersStats
                        .Child(playerId)
                        .Child("stats")
                        .Child("support")
                        .Child(supportChildSnapshot.Key)
                        .SetValueAsync(updatedSupportValue);
                }
                else
                {
                    Debug.LogError($"Invalid support value for player {playerId} in area {supportChildSnapshot.Key}. Value: {supportChildSnapshot.Value}");
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

            Debug.Log($"{cardholderId} rzucaj¹cy, a to inny gracz ${playerID}");

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

            await dbRefAllPlayersStats
                .Child(playerID)
                .Child("stats")
                .Child(statType)
                .SetValueAsync(updatedStat);
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

}
