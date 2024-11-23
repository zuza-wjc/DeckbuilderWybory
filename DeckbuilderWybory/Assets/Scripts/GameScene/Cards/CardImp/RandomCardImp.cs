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

    public async void CardLibrary(string cardIdDropped, bool ignoreCost)
    {
        DatabaseReference dbRefCard;
        DatabaseReference dbRefPlayerStats;
        DatabaseReference dbRefPlayerDeck;

        bool budgetChange = false;
        bool supportChange = false;

        int cost;
        int playerBudget;
        string enemyId = string.Empty;
        string cardType;

        int chosenRegion = -1;
        bool isBonusRegion = false;

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

            DataSnapshot budgetSnapshot = snapshot.Child("budget");
            if (budgetSnapshot.Exists)
            {
                budgetChange = true;
                cardUtilities.ProcessBonusOptions(budgetSnapshot, budgetBonusOptionsDictionary);
                cardUtilities.ProcessOptions(budgetSnapshot, budgetOptionsDictionary);
            }

            DataSnapshot supportSnapshot = snapshot.Child("support");
            if (supportSnapshot.Exists)
            {
                supportChange = true;
                cardUtilities.ProcessBonusOptions(supportSnapshot, supportBonusOptionsDictionary);
                cardUtilities.ProcessOptions(supportSnapshot, supportOptionsDictionary);
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

        }
        else
        {
            Debug.LogError("No data for: " + cardIdDropped + ".");
            return;
        }

        if (supportChange)
        {
           isBonusRegion = await SupportAction(cardIdDropped, isBonusRegion, chosenRegion,cardType,supportOptionsDictionary,
    supportBonusOptionsDictionary);
        }

        if (budgetChange)
        {
           (dbRefPlayerStats, playerBudget) = await BudgetAction(dbRefPlayerStats,isBonusRegion, budgetOptionsDictionary, budgetBonusOptionsDictionary,enemyId, playerBudget);
        }

        if (!ignoreCost)
        {
            await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget - cost);
        }

        dbRefPlayerDeck = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck").Child(cardIdDropped);

        await dbRefPlayerDeck.Child("onHand").SetValueAsync(false);
        await dbRefPlayerDeck.Child("played").SetValueAsync(true);
    }

    private async Task<(DatabaseReference dbRefPlayerStats, int playerBudget)> BudgetAction(DatabaseReference dbRefPlayerStats,
        bool isBonusRegion,Dictionary<int, OptionData> budgetOptionsDictionary,Dictionary<int, OptionData> budgetBonusOptionsDictionary,
        string enemyId,int playerBudget)
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
                if (string.IsNullOrEmpty(enemyId))
                {
                    enemyId = await playerListManager.SelectEnemyPlayer();
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        Debug.LogError("Failed to select an enemy player.");
                        return (dbRefPlayerStats, -1);
                    }
                }
                await cardUtilities.ChangeEnemyStat(enemyId, data.Number, "money", playerBudget);

                playerBudget += 10 + data.Number;

                await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
            }
        }
        return (dbRefPlayerStats, playerBudget);
    }
    private async Task<bool> SupportAction(string cardId, bool isBonusRegion,int chosenRegion, string cardType,
        Dictionary<int, OptionData> supportOptionsDictionary,Dictionary<int, OptionData> supportBonusOptionsDictionary)
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
