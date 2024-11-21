using System; 
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using UnityEngine;

public class RandomCardImp : MonoBehaviour
{
    private DatabaseReference dbRefCard;
    private DatabaseReference dbRefPlayerStats;
    private DatabaseReference dbRefPlayerDeck;

    private readonly string lobbyId = DataTransfer.LobbyId;
    private readonly string playerId = DataTransfer.PlayerId;

    private bool budgetChange;
    private bool supportChange;

    private int cost;
    private int playerBudget;
    private string enemyId;
    private string cardType;

    private int chosenRegion;
    private bool isBonusRegion;

    private Dictionary<int, OptionData> budgetOptionsDictionary = new();
    private Dictionary<int, OptionData> budgetBonusOptionsDictionary = new();
    private Dictionary<int, OptionData> supportOptionsDictionary = new();
    private Dictionary<int, OptionData> supportBonusOptionsDictionary = new();

    public PlayerListManager playerListManager;
    public MapManager mapManager;
    public CardUtilities cardUtilities;

    void Start()
    {
        playerListManager.Initialize(lobbyId, playerId);
    }


    public async void CardLibrary(string cardIdDropped, bool ignoreCost)
    {

        budgetChange = false;
        supportChange = false;
        isBonusRegion = false;

        cost = -1;
        playerBudget = -1;
        enemyId = string.Empty;
        cardType = string.Empty;

        chosenRegion = -1;

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
                if (playerBudget < cost)
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
            await SupportAction(cardIdDropped);
        }

        if (budgetChange)
        {
            await BudgetAction(cardIdDropped);
        }

        if (!ignoreCost)
        {
            await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget - cost);
        }

        dbRefPlayerDeck = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck").Child(cardIdDropped);

        await dbRefPlayerDeck.Child("onHand").SetValueAsync(false);
        await dbRefPlayerDeck.Child("played").SetValueAsync(true);
    }

    private async Task BudgetAction(string cardId)
    {
        var isBonus = isBonusRegion;
        var optionsToApply = isBonus ? budgetBonusOptionsDictionary : budgetOptionsDictionary;

        optionsToApply = RandomizeOption(optionsToApply);

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No options to apply.");
            return;
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
                        return;
                    }
                }
                await cardUtilities.ChangeEnemyStat(enemyId, data.Number, "money", playerBudget);

                playerBudget += 10 + data.Number;
            }
        }
    }

    private async Task SupportAction(string cardId)
    {

        chosenRegion = await mapManager.SelectArea();
        isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);

        var isBonus = isBonusRegion;
        var optionsToApply = isBonus ? supportBonusOptionsDictionary : supportOptionsDictionary;

        optionsToApply = RandomizeOption(optionsToApply);

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No options to apply.");
            return;
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
    }

    public static Dictionary<int, OptionData> RandomizeOption(Dictionary<int, OptionData> optionsDictionary)
    {
        if (optionsDictionary == null || optionsDictionary.Count == 0)
            throw new ArgumentException("Dictionary cannot be null or empty");

        int minNumber = optionsDictionary.Values.Min(option => option.Number);
        int maxNumber = optionsDictionary.Values.Max(option => option.Number);

        System.Random random = new System.Random();
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
