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
        
            DatabaseReference dbRefCard, dbRefPlayerStats, dbRefPlayerDeck;
            int cost, playerBudget, chosenRegion = -1;
            string cardType, enemyId = string.Empty;
            bool budgetChange = false, supportChange = false, isBonusRegion = false;

            Dictionary<int, OptionData> budgetOptionsDictionary = new();
            Dictionary<int, OptionData> budgetBonusOptionsDictionary = new();
            Dictionary<int, OptionData> supportOptionsDictionary = new();
            Dictionary<int, OptionData> supportBonusOptionsDictionary = new();

            budgetOptionsDictionary.Clear();
            supportOptionsDictionary.Clear();
            budgetBonusOptionsDictionary.Clear();
            supportBonusOptionsDictionary.Clear();

            if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
            {
                Debug.LogError("Firebase is not initialized properly!");
                return;
            }

            dbRefCard = FirebaseInitializer.DatabaseReference.Child("cards").Child("id").Child("random").Child(cardIdDropped);
            DataSnapshot snapshot = await dbRefCard.GetValueAsync();

            if (!snapshot.Exists)
            {
                Debug.LogError("No data for: " + cardIdDropped + ".");
                return;
            }

            cost = snapshot.Child("cost").Exists ? Convert.ToInt32(snapshot.Child("cost").Value) : throw new Exception("Branch cost does not exist.");

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
        cardType = snapshot.Child("type").Exists ? snapshot.Child("type").Value.ToString() : throw new Exception("Branch type does not exist.");

            if (snapshot.Child("budget").Exists)
            {
                budgetChange = true;
                cardUtilities.ProcessBonusOptions(snapshot.Child("budget"), budgetBonusOptionsDictionary);
                cardUtilities.ProcessOptions(snapshot.Child("budget"), budgetOptionsDictionary);
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

            dbRefPlayerStats = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("stats");
            DataSnapshot playerStatsSnapshot = await dbRefPlayerStats.GetValueAsync();

            if (!playerStatsSnapshot.Exists)
            {
                Debug.LogError("No data for: " + cardIdDropped + ".");
                return;
            }

            playerBudget = playerStatsSnapshot.Child("money").Exists ? Convert.ToInt32(playerStatsSnapshot.Child("money").Value) : throw new Exception("Branch money does not exist.");

            if (!ignoreCost && playerBudget < cost)
            {
                Debug.LogError("Brak bud¿etu aby zagraæ kartê.");
                return;
            }
        if (!(await cardUtilities.CheckBlockedCard(playerId)))
        {

            if (supportChange)
            {
                isBonusRegion = await SupportAction(cardIdDropped, isBonusRegion, chosenRegion, cardType, supportOptionsDictionary, supportBonusOptionsDictionary);
            }

            if (budgetChange)
            {
                (dbRefPlayerStats, playerBudget) = await BudgetAction(dbRefPlayerStats, isBonusRegion, budgetOptionsDictionary, budgetBonusOptionsDictionary, enemyId, playerBudget);
            }
        }

            if (!ignoreCost)
            {
                await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget - cost);
            }

            dbRefPlayerDeck = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck").Child(instanceId);

            await dbRefPlayerDeck.Child("onHand").SetValueAsync(false);
            await dbRefPlayerDeck.Child("played").SetValueAsync(true);

        DataTransfer.IsFirstCardInTurn = false;
    }

    private async Task<(DatabaseReference dbRefPlayerStats, int playerBudget)> BudgetAction(DatabaseReference dbRefPlayerStats,bool isBonusRegion,Dictionary<int, OptionData> budgetOptionsDictionary,
        Dictionary<int, OptionData> budgetBonusOptionsDictionary,string enemyId,int playerBudget)
    {
        var isBonus = isBonusRegion;
        var optionsToApply = isBonus ? budgetBonusOptionsDictionary : budgetOptionsDictionary;

        optionsToApply = RandomizeOption(optionsToApply);

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No options to apply.");
            return (dbRefPlayerStats, -1);
        }

        if (isBonus)
        {
            Debug.Log("Bonus region detected.");
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
                            return (dbRefPlayerStats, -1);
                        }
                    }
                    playerBudget = await cardUtilities.ChangeEnemyStat(enemyId, data.Number, "money", playerBudget);

                    playerBudget += 10 + data.Number;

                    await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);

                    await cardUtilities.CheckAndAddCopyBudget(playerId, 10 + data.Number);
                }
            }
        }
        return (dbRefPlayerStats, playerBudget);
    }
    private async Task<bool> SupportAction(string cardId, bool isBonusRegion,int chosenRegion, string cardType,Dictionary<int, OptionData> supportOptionsDictionary,
        Dictionary<int, OptionData> supportBonusOptionsDictionary)
    {
        chosenRegion = await mapManager.SelectArea();
        isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);

        var isBonus = isBonusRegion;
        var optionsToApply = isBonus ? supportBonusOptionsDictionary : supportOptionsDictionary;

        optionsToApply = RandomizeOption(optionsToApply);

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No options to apply.");
            return false;
        }

        if (isBonus)
        {
            Debug.Log("Bonus region detected.");
        }

        foreach (var data in optionsToApply.Values)
        {
            if(data.Target == "player-region")
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
