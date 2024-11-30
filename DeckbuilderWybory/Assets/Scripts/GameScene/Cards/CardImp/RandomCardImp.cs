using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using UnityEngine;

public class RandomCardImp : MonoBehaviour
{
    private readonly string lobbyId = DataTransfer.LobbyId;
    private readonly string playerId = DataTransfer.PlayerId;

    public PlayerListManager playerListManager;
    public MapManager mapManager;
    public CardUtilities cardUtilities;
    public ErrorPanelController errorPanelController;
    public HistoryController historyController;
    void Start()
    {
        playerListManager.Initialize(lobbyId, playerId);
    }

    public async void CardLibrary(string instanceId, string cardIdDropped, bool ignoreCost)
    {
        bool errorCheck = false, isBonusRegion = false;
        string enemyId = string.Empty;
        bool tmp = await cardUtilities.CheckCardLimit(playerId);
        if (tmp)
        {
            Debug.Log("Limit kart w turze to 1");
            errorPanelController.ShowError("card_limit");
            return;
        }

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            errorPanelController.ShowError("general_error");
            return;
        }

        var cardData = await GetCardData(cardIdDropped);
        if (cardData == null) 
        {
            Debug.LogError("Card data jest null");
            errorPanelController.ShowError("general_error");
            return;
        }

        int cost = cardData.Cost;
        string cardType = cardData.CardType;

        cost = await AdjustCardCost(cardIdDropped, cost);

        var playerStats = await GetPlayerStats(playerId);
        if (playerStats == null)
        {
            Debug.LogError("Player data jest null");
            errorPanelController.ShowError("general_error");
            return;
        }

        int playerBudget = playerStats.Money;
        if (!ignoreCost && playerBudget < cost)
        {
            Debug.LogError("Brak bud¿etu aby zagraæ kartê.");
            errorPanelController.ShowError("no_budget");
            return;
        }

        ignoreCost = await cardUtilities.CheckIgnoreCost(playerId);
        if (!(await cardUtilities.CheckBlockedCard(playerId)))
        {
            if (!ignoreCost)
            {
                await DeductPlayerMoney(cost, playerBudget);
                playerBudget -= cost;
            }
            else
            {
                ignoreCost = false;
            }

            if (cardData.SupportChange)
            {
                (errorCheck, cardData.Desc) = await SupportAction(cardIdDropped, false, -1, cardType, cardData.SupportOptions, cardData.SupportBonusOptions, cardData.Desc);
                if (errorCheck)
                {
                    await DeductPlayerMoney(-cost, playerBudget);
                    return;
                }
            }

            if (cardData.BudgetChange)
            {
                (cardData.Desc, playerBudget) = await BudgetAction(playerBudget, cardData.BudgetOptions, cardData.BudgetBonusOptions, enemyId, cardData.Desc);

                if (playerBudget == -1)
                {
                    await DeductPlayerMoney(-cost, playerBudget);
                    return;
                }
            }
        } else
        {
            Debug.Log("Karta zosta³a zablokowana");
            errorPanelController.ShowError("action_blocked");
            return;
        }

        await UpdatePlayerDeck(instanceId);

        DataTransfer.IsFirstCardInTurn = false;

        await cardUtilities.CheckIfPlayed2Cards(playerId);
        tmp = await cardUtilities.CheckCardLimit(playerId);
        await historyController.AddCardToHistory(cardIdDropped, playerId, cardData.Desc[0]);

    }

    private async Task<CardData> GetCardData(string cardIdDropped)
    {
        DatabaseReference dbRefCard = FirebaseInitializer.DatabaseReference
            .Child("cards").Child("id").Child("random").Child(cardIdDropped);
        DataSnapshot snapshot = await dbRefCard.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.LogError("No data for: " + cardIdDropped + ".");
            return null;
        }

        int cost = snapshot.Child("cost").Exists ? Convert.ToInt32(snapshot.Child("cost").Value) : -1;
        string cardType = snapshot.Child("type").Exists ? snapshot.Child("type").Value.ToString() : string.Empty;

        List<string> descriptions = new List<string>
        {
            snapshot.Child("playDescriptionPositive").Exists ? snapshot.Child("playDescriptionPositive").Value.ToString() : string.Empty,
            snapshot.Child("playDescriptionNegative").Exists ? snapshot.Child("playDescriptionNegative").Value.ToString() : string.Empty
        };


        if (cost < 0) return null;
        if(cardType == string.Empty) return null;
        if (descriptions[0] == string.Empty) return null;

        bool budgetChange = snapshot.Child("budget").Exists;
        bool supportChange = snapshot.Child("support").Exists;

        var budgetOptions = new Dictionary<int, OptionData>();
        var budgetBonusOptions = new Dictionary<int, OptionData>();
        var supportOptions = new Dictionary<int, OptionData>();
        var supportBonusOptions = new Dictionary<int, OptionData>();

        if (budgetChange)
        {
            cardUtilities.ProcessBonusOptions(snapshot.Child("budget"), budgetBonusOptions);
            cardUtilities.ProcessOptions(snapshot.Child("budget"), budgetOptions);
        }

        if (supportChange)
        {
            cardUtilities.ProcessBonusOptions(snapshot.Child("support"), supportBonusOptions);
            cardUtilities.ProcessOptions(snapshot.Child("support"), supportOptions);
        }

        return new CardData(cost, cardType, budgetChange, supportChange, budgetOptions, budgetBonusOptions, supportOptions, supportBonusOptions, descriptions);
    }

    private async Task<int> AdjustCardCost(string cardIdDropped, int cost)
    {
        bool isFirstCardInTurn = DataTransfer.IsFirstCardInTurn;
        bool canIncreaseCost = await cardUtilities.CheckIncreaseCost(playerId);

        if (isFirstCardInTurn && canIncreaseCost)
        {
            cost = (int)Math.Ceiling(1.5 * cost);
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

    private async Task<PlayerStats> GetPlayerStats(string playerId)
    {
        DatabaseReference dbRefPlayerStats = FirebaseInitializer.DatabaseReference
            .Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("stats");
        DataSnapshot playerStatsSnapshot = await dbRefPlayerStats.GetValueAsync();

        if (!playerStatsSnapshot.Exists)
        {
            Debug.LogError("No data for player: " + playerId);
            return null;
        }

        int playerMoney = playerStatsSnapshot.Child("money").Exists ? Convert.ToInt32(playerStatsSnapshot.Child("money").Value) : -1;
        if(playerMoney<0)
        {
            return null;
        }
        return new PlayerStats(playerMoney);
    }

    private async Task DeductPlayerMoney(int cost, int playerBudget)
    {
        DatabaseReference dbRefPlayerStats = FirebaseInitializer.DatabaseReference
            .Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("stats");

        if (playerBudget == -1)
        {
            var snapshot = await dbRefPlayerStats.Child("money").GetValueAsync();
            if (snapshot.Exists && int.TryParse(snapshot.Value.ToString(), out int budget))
            {
                playerBudget = budget;
            }
        }

        await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget - cost);
    }


    private async Task UpdatePlayerDeck(string instanceId)
    {
        DatabaseReference dbRefPlayerDeck = FirebaseInitializer.DatabaseReference
            .Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck").Child(instanceId);
        await dbRefPlayerDeck.Child("onHand").SetValueAsync(false);
        await dbRefPlayerDeck.Child("played").SetValueAsync(true);
    }

    private async Task<(List<string>, int)> BudgetAction(int playerBudget, Dictionary<int, OptionData> budgetOptions, Dictionary<int, OptionData> budgetBonusOptions, string enemyId, List<string> descriptions) {
        
        var optionsToApply = budgetBonusOptions.Any() ? budgetBonusOptions : budgetOptions;

        (optionsToApply, descriptions) = RandomizeOption(optionsToApply, descriptions);

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No options to apply.");
            errorPanelController.ShowError("general_error");
            return (descriptions, -1);
        }

        foreach (var data in optionsToApply.Values)
        {
            if (data.Target == "enemy")
            {
                if (!(await cardUtilities.CheckBudgetBlock(playerId)))
                {

                    if (string.IsNullOrEmpty(enemyId))
                    {
                        enemyId = await playerListManager.SelectEnemyPlayer();
                        if (string.IsNullOrEmpty(enemyId))
                        {
                            Debug.LogError("Failed to select an enemy player.");
                            errorPanelController.ShowError("general_error");
                            return (descriptions, -1);
                        }
                    }
                    playerBudget = await cardUtilities.ChangeEnemyStat(enemyId, data.Number, "money", playerBudget);

                    if(playerBudget == -1)
                    {
                        return (descriptions, -1);
                    }

                    playerBudget += 10 + data.Number;

                    if(playerBudget < 0)
                    {
                        Debug.LogWarning("Brak wystarczaj¹cego bud¿etu aby zagraæ kartê.");
                        errorPanelController.ShowError("no_budget");
                        return (descriptions, -1);
                    }

                    DatabaseReference dbRefPlayerStats = FirebaseInitializer.DatabaseReference
                    .Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("stats");
                    await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);

                    await cardUtilities.CheckAndAddCopyBudget(playerId, 10 + data.Number);
                } else
                {
                    Debug.Log("Budget blocked");
                    errorPanelController.ShowError("action_blocked");
                    return (descriptions, -1);
                }
            }
        }

        return (descriptions, playerBudget);
    }

    private async Task<(bool errorCheck, List<string>)> SupportAction(string cardId, bool isBonusRegion, int chosenRegion, string cardType, Dictionary<int, OptionData> supportOptions, Dictionary<int, OptionData> supportBonusOptions, List<string> descriptions)
    {
        chosenRegion = await mapManager.SelectArea();
        isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);

        var optionsToApply = isBonusRegion ? supportBonusOptions : supportOptions;
        (optionsToApply, descriptions) = RandomizeOption(optionsToApply, descriptions);

        foreach (var data in optionsToApply.Values)
        {
            if (data.Target == "player-region")
            {
                bool errorCheck = await cardUtilities.ChangeSupport(playerId, data.Number, chosenRegion, cardId, mapManager);
                return (errorCheck, descriptions);
            }
        }
        return (false, descriptions);
    }

    public static (Dictionary<int, OptionData>, List<string>) RandomizeOption(Dictionary<int, OptionData> optionsDictionary, List<string> descriptions)
    {
        if (optionsDictionary == null || optionsDictionary.Count == 0)
            throw new ArgumentException("Dictionary cannot be null or empty");

        int minNumber = optionsDictionary.Values.Min(option => option.Number);
        int maxNumber = optionsDictionary.Values.Max(option => option.Number);

        System.Random random = new();
        int randomNumber = random.Next(minNumber, maxNumber + 1);

        if (randomNumber < 0)
        {
            if (descriptions[1] != string.Empty)
            {
                descriptions[0] = descriptions[1];
                descriptions.RemoveAt(1);
            }
        }

        Debug.Log($"Wylosowana liczba to: {randomNumber}");

        string existingTarget = optionsDictionary.Values.First().Target;

        var newOption = new OptionData(randomNumber, existingTarget, 1);

        return (new Dictionary<int, OptionData>
        {
            { 1, newOption }
        }, descriptions);
    }
}

public class CardData
{
    public int Cost { get; }
    public string CardType { get; }
    public bool BudgetChange { get; }
    public bool SupportChange { get; }
    public Dictionary<int, OptionData> BudgetOptions { get; }
    public Dictionary<int, OptionData> BudgetBonusOptions { get; }
    public Dictionary<int, OptionData> SupportOptions { get; }
    public Dictionary<int, OptionData> SupportBonusOptions { get; }

    public List<string> Desc { get; set; }

    public CardData(int cost, string cardType, bool budgetChange, bool supportChange,
                       Dictionary<int, OptionData> budgetOptions, Dictionary<int, OptionData> budgetBonusOptions,
                       Dictionary<int, OptionData> supportOptions, Dictionary<int, OptionData> supportBonusOptions, List<string> desc)
    {
        Cost = cost;
        CardType = cardType;
        BudgetChange = budgetChange;
        SupportChange = supportChange;
        BudgetOptions = budgetOptions;
        BudgetBonusOptions = budgetBonusOptions;
        SupportOptions = supportOptions;
        SupportBonusOptions = supportBonusOptions;
        Desc = desc;
    }
}

public class PlayerStats
{
    public int Money { get; }

    public PlayerStats(int money)
    {
        Money = money;
    }
}
