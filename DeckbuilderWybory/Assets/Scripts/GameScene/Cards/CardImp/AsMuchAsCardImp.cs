using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using UnityEngine;

public class AsMuchAsCardImp : MonoBehaviour
{
    private readonly string lobbyId = DataTransfer.LobbyId;
    private readonly string playerId = DataTransfer.PlayerId;

    public PlayerListManager playerListManager;
    public CardUtilities cardUtilities;
    public MapManager mapManager;

    void Start()
    {
        playerListManager.Initialize(lobbyId, playerId);
    }


    public async void CardLibrary(string cardIdDropped, bool ignoreCost)
    {
        DatabaseReference dbRefCard;
        DatabaseReference dbRefPlayerStats;
        DatabaseReference dbRefPlayerDeck;

        int cost;
        string cardType;
        int playerBudget;
        int playerIncome;
        int chosenRegion = -1;
        string enemyId = string.Empty;

        bool budgetChange = false;
        bool supportChange = false;
        bool isBonusRegion = false;
        bool incomeChange = false;

        Dictionary<int, OptionData> budgetOptionsDictionary = new();
        Dictionary<int, OptionData> budgetBonusOptionsDictionary = new();
        Dictionary<int, OptionDataPerCard> incomeOptionsDictionary = new();
        Dictionary<int, OptionDataPerCard> incomeBonusOptionsDictionary = new();
        Dictionary<int, OptionDataPerCard> supportOptionsDictionary = new();
        Dictionary<int, OptionDataPerCard> supportBonusOptionsDictionary = new();
        Dictionary<int, OptionDataPerCard> budgetOptionsPerCardDictionary = new();
        Dictionary<int, OptionDataPerCard> budgetBonusOptionsPerCardDictionary = new();

        budgetOptionsDictionary.Clear();
        budgetBonusOptionsDictionary.Clear();
        incomeOptionsDictionary.Clear();
        incomeBonusOptionsDictionary.Clear();
        supportOptionsDictionary.Clear();
        supportBonusOptionsDictionary.Clear();
        budgetOptionsPerCardDictionary.Clear();
        budgetBonusOptionsPerCardDictionary.Clear();

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRefCard = FirebaseInitializer.DatabaseReference.Child("cards").Child("id").Child("asMuchAs").Child(cardIdDropped);

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

                if (cardIdDropped == "AS072")
                {
                    ProcessBonusOptionsPerCard(budgetSnapshot, budgetBonusOptionsPerCardDictionary, "Budget");
                    ProcessOptionsPerCard(budgetSnapshot, budgetOptionsPerCardDictionary, "Budget");
                }
                else
                {
                    cardUtilities.ProcessBonusOptions(budgetSnapshot, budgetBonusOptionsDictionary);
                    cardUtilities.ProcessOptions(budgetSnapshot, budgetOptionsDictionary);
                }
            }


            DataSnapshot incomeSnapshot = snapshot.Child("income");
            if (incomeSnapshot.Exists)
            {
                incomeChange = true;

                ProcessBonusOptionsPerCard(incomeSnapshot, incomeBonusOptionsDictionary, "Income");
                ProcessOptionsPerCard(incomeSnapshot, incomeOptionsDictionary, "Income");
            }

            DataSnapshot supportSnapshot = snapshot.Child("support");
            if (supportSnapshot.Exists)
            {
                supportChange = true;

                ProcessBonusOptionsPerCard(supportSnapshot, supportBonusOptionsDictionary, "Support");
                ProcessOptionsPerCard(supportSnapshot, supportOptionsDictionary, "Support");
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

        if(supportChange)
        {
            isBonusRegion = await SupportAction(cardIdDropped,isBonusRegion,supportOptionsDictionary,supportBonusOptionsDictionary,
                chosenRegion,cardType);
        }

        if (budgetChange)
        {
            (dbRefPlayerStats, isBonusRegion, playerBudget) = await BudgetAction(dbRefPlayerStats,cardIdDropped,isBonusRegion,
                budgetOptionsDictionary,budgetBonusOptionsDictionary,budgetOptionsPerCardDictionary,
                budgetBonusOptionsPerCardDictionary,playerBudget,enemyId,cardType);
        }

        if (incomeChange)
        {
            dbRefPlayerStats = await IncomeAction(isBonusRegion,incomeOptionsDictionary,incomeBonusOptionsDictionary,
                playerIncome,cardType,dbRefPlayerStats);
        }

        if (!ignoreCost)
        {
            await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget - cost);
        }

        dbRefPlayerDeck = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players").Child(playerId).Child("deck").Child(cardIdDropped);

        await dbRefPlayerDeck.Child("onHand").SetValueAsync(false);
        await dbRefPlayerDeck.Child("played").SetValueAsync(true);
    }

    private async Task<DatabaseReference> IncomeAction(bool isBonusRegion, Dictionary<int, OptionDataPerCard> incomeOptionsDictionary,
        Dictionary<int, OptionDataPerCard> incomeBonusOptionsDictionary, int playerIncome, string cardType,
        DatabaseReference dbRefPlayerStats)
    {
        var isBonus = isBonusRegion;
        var optionsToApply = isBonus ? incomeBonusOptionsDictionary : incomeOptionsDictionary;

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No options to apply.");
            return dbRefPlayerStats;
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
                int howMany = await CalculateValueFromHand(playerId, cardType);
                playerIncome += howMany * data.NumberPerCard;
                await dbRefPlayerStats.Child("income").SetValueAsync(playerIncome);
            }
        }

        return dbRefPlayerStats;
    }
    private async Task<(DatabaseReference dbRefPlayerStats,bool isBonusRegion,int playerBudget)>
        BudgetAction(DatabaseReference dbRefPlayerStats,string cardId, bool isBonusRegion,
        Dictionary<int, OptionData> budgetOptionsDictionary,Dictionary<int, OptionData> budgetBonusOptionsDictionary,
        Dictionary<int, OptionDataPerCard> budgetOptionsPerCardDictionary,Dictionary<int, OptionDataPerCard> budgetBonusOptionsPerCardDictionary,
        int playerBudget, string enemyId, string cardType)
    {
        var isBonus = isBonusRegion;
        object optionsToApply;

        if (cardId == "AS072")
        {
            optionsToApply = isBonus ? (object)budgetBonusOptionsPerCardDictionary : (object)budgetOptionsPerCardDictionary;
        }
        else
        {
            optionsToApply = isBonus ? (object)budgetBonusOptionsDictionary : (object)budgetOptionsDictionary;
        }

        if (optionsToApply is Dictionary<int, OptionData> optionsData)
        {
            if (optionsData?.Values == null || !optionsData.Values.Any())
            {
                Debug.LogError("No options to apply.");
                return(dbRefPlayerStats,false,-1);
            }

            if (isBonus)
            {
                Debug.Log("Bonus region detected.");
            }

            foreach (var data in optionsData.Values)
            {
                if (data == null)
                {
                    Debug.LogWarning("Encountered null data, skipping.");
                    continue;
                }

                if (data.Target == "player")
                {
                    playerBudget += data.Number;
                    await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
                }
            }
        }

        else if (optionsToApply is Dictionary<int, OptionDataPerCard> optionsPerCard)
        {
            if (optionsPerCard?.Values == null || !optionsPerCard.Values.Any())
            {
                Debug.LogError("No options to apply.");
                return (dbRefPlayerStats, false, -1);
            }

            if (isBonus)
            {
                Debug.Log("Bonus region detected.");
            }

            foreach (var data in optionsPerCard.Values)
            {
                if (data == null)
                {
                    Debug.LogWarning("Encountered null data, skipping.");
                    continue;
                }

                if (data.Target == "enemy")
                {
                    if (string.IsNullOrEmpty(enemyId))
                    {
                        enemyId = await playerListManager.SelectEnemyPlayer();
                        if (string.IsNullOrEmpty(enemyId))
                        {
                            Debug.LogError("Failed to select an enemy player.");
                            return (dbRefPlayerStats, false, -1);
                        }
                    }

                    int howMany = await CalculateValueFromHand(playerId, cardType);
                    int changeBudget = howMany * data.NumberPerCard;
                    await cardUtilities.ChangeEnemyStat(enemyId, changeBudget, "money", playerBudget);
                    await dbRefPlayerStats.Child("money").SetValueAsync(playerBudget);
                }
            }
        }
        else
        {
            Debug.LogError("Unexpected optionsToApply type.");
            return (dbRefPlayerStats, false, -1);
        }
        return (dbRefPlayerStats, isBonusRegion, playerBudget);
    }
    private async Task<bool> SupportAction(string cardId, bool isBonusRegion, Dictionary<int, OptionDataPerCard> supportOptionsDictionary,
    Dictionary<int, OptionDataPerCard> supportBonusOptionsDictionary, int chosenRegion, string cardType)
    {
        var isBonus = isBonusRegion;
        var optionsToApply = isBonus ? supportBonusOptionsDictionary : supportOptionsDictionary;

        if (optionsToApply?.Values == null || !optionsToApply.Values.Any())
        {
            Debug.LogError("No support options available.");
            return false;
        }

        if (isBonus)
        {
            Debug.Log("Bonus region detected.");
        }

        foreach(var data in optionsToApply.Values )
        {
            if (data == null)
            {
                Debug.LogWarning("Encountered null data in support options, skipping.");
                continue;
            }

            if(data.Target == "player-region")
            {
                if (chosenRegion < 0)
                {
                    chosenRegion = await mapManager.SelectArea();
                }
                isBonusRegion = await mapManager.CheckIfBonusRegion(chosenRegion, cardType);
                int supportValue = await CalculateValueFromHand(playerId, cardType);
                Debug.Log($"Poparcie zwiêksza siê o {supportValue}");
                await cardUtilities.ChangeSupport(playerId, supportValue, chosenRegion, cardId, mapManager);
            }
        }
        return isBonusRegion;
    }

    private void ProcessOptionsPerCard(DataSnapshot snapshot, Dictionary<int, OptionDataPerCard> optionsDictionary, string optionType)
    {
        foreach (var optionSnapshot in snapshot.Children)
        {
            if (optionSnapshot.Key != "bonus")
            {
                DataSnapshot numberPerCardSnapshot = optionSnapshot.Child("numberPerCard");
                DataSnapshot targetSnapshot = optionSnapshot.Child("target");

                if (numberPerCardSnapshot.Exists && targetSnapshot.Exists)
                {
                    int numberPerCard = Convert.ToInt32(numberPerCardSnapshot.Value);
                    string target = targetSnapshot.Value.ToString();

                    int optionKey = Convert.ToInt32(optionSnapshot.Key.Replace("option", ""));

                    // Dodajemy dane do s³ownika opcji
                    optionsDictionary.Add(optionKey, new OptionDataPerCard(numberPerCard, target));
                }
                else
                {
                    Debug.LogError($"{optionType} option is missing 'numberPerCard' or 'target'.");
                }
            }
        }
    }

    private void ProcessBonusOptionsPerCard(DataSnapshot snapshot, Dictionary<int, OptionDataPerCard> bonusOptionsDictionary, string optionType)
    {
        DataSnapshot bonusSnapshot = snapshot.Child("bonus");
        if (bonusSnapshot.Exists)
        {
            int optionIndex = 0;

            foreach (var optionSnapshot in bonusSnapshot.Children)
            {
                DataSnapshot numberPerCardSnapshot = optionSnapshot.Child("numberPerCard");
                DataSnapshot targetSnapshot = optionSnapshot.Child("target");

                if (numberPerCardSnapshot.Exists && targetSnapshot.Exists)
                {
                    int numberPerCard = Convert.ToInt32(numberPerCardSnapshot.Value);
                    string target = targetSnapshot.Value.ToString();

                    // Dodajemy dane do s³ownika bonusów
                    bonusOptionsDictionary.Add(optionIndex, new OptionDataPerCard(numberPerCard, target));
                }
                else
                {
                    Debug.LogError($"{optionType} bonus option {optionIndex} is missing 'numberPerCard' or 'target'.");
                }

                optionIndex++;
            }
        }
    }


    private async Task<int> CalculateValueFromHand(string playerId, string cardType)
    {
        int cardCount = 0;

        var dbRefPlayerDeck = FirebaseInitializer.DatabaseReference
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("deck");

        var snapshot = await dbRefPlayerDeck.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.LogError("No deck data found for the player.");
            return cardCount;
        }

        foreach (var cardSnapshot in snapshot.Children)
        {
            string onHandValue = cardSnapshot.Child("onHand").Value.ToString();
            string playedValue = cardSnapshot.Child("played").Value.ToString();

            if (onHandValue == "True" && playedValue == "False")
            {
                string cardId = cardSnapshot.Key;
                string type = await GetCardType(cardId);

                if (type == cardType)
                {
                    cardCount++;
                }
            }
        }

        return cardCount;
    }

    private async Task<string> GetCardType(string cardId)
    {
        var dbRefCard = FirebaseInitializer.DatabaseReference
            .Child("cards")
            .Child("id")
            .Child("asMuchAs")
            .Child(cardId);

        var snapshot = await dbRefCard.GetValueAsync();

        if (!snapshot.Exists)
        {
            Debug.LogError($"Nie znaleziono karty o cardId: {cardId}");
            return null;
        }

        string cardType = snapshot.Child("type").Value.ToString();
        return cardType;
    }

}

public class OptionDataPerCard
{
    public int NumberPerCard { get; }
    public string Target { get; }

    public OptionDataPerCard(int numberPerCard, string target)
    {
        NumberPerCard = numberPerCard;
        Target = target;
    }


}