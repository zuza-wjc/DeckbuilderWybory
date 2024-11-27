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

        var cardData = await GetCardData(cardIdDropped);
        if (cardData == null) return;

        int cost = cardData.Cost;
        string cardType = cardData.CardType;

        cost = await AdjustCardCost(cardIdDropped, cost);

        var playerStats = await GetPlayerStats(playerId);
        if (playerStats == null) return;

        int playerBudget = playerStats.Money;
        if (!ignoreCost && playerBudget < cost)
        {
            Debug.LogError("Brak bud¿etu aby zagraæ kartê.");
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
                bool isBonusRegion = await SupportAction(cardIdDropped, false, -1, cardType, cardData.SupportOptions, cardData.SupportBonusOptions);
            }

            if (cardData.BudgetChange)
            {
                await BudgetAction(playerBudget, cardData.BudgetOptions, cardData.BudgetBonusOptions);
            }
        }

        await UpdatePlayerDeck(instanceId);

        DataTransfer.IsFirstCardInTurn = false;

        await cardUtilities.CheckIfPlayed2Cards(playerId);
        tmp = await cardUtilities.CheckCardLimit(playerId);
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

        int cost = snapshot.Child("cost").Exists ? Convert.ToInt32(snapshot.Child("cost").Value) : 0;
        string cardType = snapshot.Child("type").Exists ? snapshot.Child("type").Value.ToString() : string.Empty;

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

        return new CardData(cost, cardType, budgetChange, supportChange, budgetOptions, budgetBonusOptions, supportOptions, supportBonusOptions);
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

        int playerMoney = playerStatsSnapshot.Child("money").Exists ? Convert.ToInt32(playerStatsSnapshot.Child("money").Value) : 0;
        return new PlayerStats(playerMoney);
    }

    private async Task DeductPlayerMoney(int cost, int playerBudget)
    {
        DatabaseReference dbRefPlayerStats = FirebaseInitializer.DatabaseReference
            .Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("stats");
        await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget - cost);
    }

    private async Task UpdatePlayerDeck(string instanceId)
    {
        DatabaseReference dbRefPlayerDeck = FirebaseInitializer.DatabaseReference
            .Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck").Child(instanceId);
        await dbRefPlayerDeck.Child("onHand").SetValueAsync(false);
        await dbRefPlayerDeck.Child("played").SetValueAsync(true);
    }

    private async Task BudgetAction(int playerBudget, Dictionary<int, OptionData> budgetOptions, Dictionary<int, OptionData> budgetBonusOptions)
    {
        var optionsToApply = budgetBonusOptions.Any() ? budgetBonusOptions : budgetOptions;
        optionsToApply = RandomizeOption(optionsToApply);

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No options to apply.");
            return;
        }

        foreach (var data in optionsToApply.Values)
        {
            if (data.Target == "enemy")
            {
                playerBudget = await cardUtilities.ChangeEnemyStat(playerId, data.Number, "money", playerBudget);
                await FirebaseInitializer.DatabaseReference.Child("sessions")
                    .Child(lobbyId).Child("players").Child(playerId).Child("stats").Child("money")
                    .SetValueAsync(playerBudget);
            }
        }
    }

    private async Task<bool> SupportAction(string cardId, bool isBonusRegion, int chosenRegion, string cardType, Dictionary<int, OptionData> supportOptions, Dictionary<int, OptionData> supportBonusOptions)
    {
        chosenRegion = await mapManager.SelectArea();
        isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);

        var optionsToApply = isBonusRegion ? supportBonusOptions : supportOptions;
        optionsToApply = RandomizeOption(optionsToApply);

        foreach (var data in optionsToApply.Values)
        {
            if (data.Target == "player-region")
            {
                await cardUtilities.ChangeSupport(playerId, data.Number, chosenRegion, cardId, mapManager);
            }
        }

        return isBonusRegion;
    }

    public static Dictionary<int, OptionData> RandomizeOption(Dictionary<int, OptionData> optionsDictionary)
    {
        if (optionsDictionary == null || optionsDictionary.Count == 0)
            throw new ArgumentException("Dictionary cannot be null or empty");

        int minNumber = optionsDictionary.Values.Min(option => option.Number);
        int maxNumber = optionsDictionary.Values.Max(option => option.Number);

        System.Random random = new();
        int randomNumber = random.Next(minNumber, maxNumber + 1);

        Debug.Log($"Wylosowana liczba to: {randomNumber}");

        string existingTarget = optionsDictionary.Values.First().Target;

        var newOption = new OptionData(randomNumber, existingTarget, 1);

        return new Dictionary<int, OptionData>
        {
            { 1, newOption }
        };
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

    public CardData(int cost, string cardType, bool budgetChange, bool supportChange,
                    Dictionary<int, OptionData> budgetOptions, Dictionary<int, OptionData> budgetBonusOptions,
                    Dictionary<int, OptionData> supportOptions, Dictionary<int, OptionData> supportBonusOptions)
    {
        Cost = cost;
        CardType = cardType;
        BudgetChange = budgetChange;
        SupportChange = supportChange;
        BudgetOptions = budgetOptions;
        BudgetBonusOptions = budgetBonusOptions;
        SupportOptions = supportOptions;
        SupportBonusOptions = supportBonusOptions;
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
