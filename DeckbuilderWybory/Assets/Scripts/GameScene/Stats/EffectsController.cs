using System.Threading.Tasks;
using UnityEngine;
using Firebase.Database;
using System;
using System.Linq;

public class EffectsController : MonoBehaviour
{
    private DatabaseReference dbRef;
    private string lobbyId;
    private string playerId;

    public GameObject copySupportPrefab;
    public GameObject copyBudgetPrefab;

    public GameObject increaseCostPrefab;
    public GameObject decreaseCostPrefab;

    public GameObject budgetPenaltyPrefab;

    public GameObject incomeBlockPrefab;
    public GameObject supportBlockPrefab;
    public GameObject budgetBlockPrefab;

    public Transform scrollViewContent;

    private GameObject copySupportInstance;
    private GameObject copyBudgetInstance;

    private GameObject increaseCostInstance;
    private GameObject decreaseCostInstance;
    private GameObject increaseAllCostInstance;

    private GameObject budgetPenaltyInstance;

    private GameObject incomeBlockInstance;
    private GameObject supportBlockInstance;
    private GameObject budgetBlockInstance;

    private DatabaseReference copySupportTurnsTakenRef;
    private DatabaseReference copyBudgetTurnsTakenRef;
    private DatabaseReference budgetPenaltyRef;

    private EventHandler<ValueChangedEventArgs> budgetPenaltyListener;

    void Start()
    {
        dbRef = FirebaseInitializer.DatabaseReference;
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        ListenForIncomeBlockChanges();
        ListenForSupportBlockChanges();
        ListenForBudgetBlockChanges();

        ListenForCopySupportChanges();
        ListenForCopyBudgetChanges();
        ListenForIncreaseCostChanges();
        ListenForDecreaseCostChanges();
        ListenForIncreaseAllCostChanges();
        ListenForBudgetPenaltyChanges();
    }

    void Update()
    {
        if (increaseCostInstance != null)
        {
            if (DataTransfer.IsFirstCardInTurn == false || (DataTransfer.TurnEnded && !DataTransfer.EffectActive))
            {
                if (increaseCostInstance != null)
                {
                    Destroy(increaseCostInstance);
                    increaseCostInstance = null;
                }
            }
        }

        if (DataTransfer.TurnEnded && !DataTransfer.EffectActive)
        {
            if (supportBlockInstance != null)
            {
                Destroy(supportBlockInstance);
                supportBlockInstance = null;
            }

            if (budgetBlockInstance != null)
            {
                Destroy(budgetBlockInstance);
                budgetBlockInstance = null;
            }

            if (incomeBlockInstance != null)
            {
                Destroy(incomeBlockInstance);
                incomeBlockInstance = null;
            }

            if (decreaseCostInstance != null)
            {
                Destroy(decreaseCostInstance);
                decreaseCostInstance = null;
            }

            if (increaseAllCostInstance != null)
            {
                Destroy(increaseAllCostInstance);
                increaseAllCostInstance = null;
            }

            if (copySupportInstance != null)
            {
                Destroy(copySupportInstance);
                copySupportInstance = null;
            }

            if (copyBudgetInstance != null)
            {
                Destroy(copyBudgetInstance);
                copyBudgetInstance = null;
            }

            if(budgetPenaltyInstance  != null)
            {
                Destroy(budgetPenaltyInstance);
                budgetPenaltyInstance = null;
            }

            DataTransfer.TurnEnded = false;
        }
    }
    private void ListenForIncomeBlockChanges()
    {
        DatabaseReference incomeBlockRef = dbRef
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("blockIncome");

        incomeBlockRef.ValueChanged += async (sender, args) =>
        {
            var incomeBlockSnapshot = args.Snapshot;

            if (incomeBlockSnapshot.Exists)
            {
                int turnsTakenInIncomeBlock = Convert.ToInt32(incomeBlockSnapshot.Child("turnsTaken").Value);
                string playerIdFromIncomeBlock = incomeBlockSnapshot.Child("playerId").Value.ToString();

                int turnsTakenInStats = await GetTurnsTakenFromStats(playerIdFromIncomeBlock);

                if (turnsTakenInIncomeBlock == turnsTakenInStats)
                {
                    CreateIncomeBlockPrefab();
                }
            }
            else
            {
                if (incomeBlockInstance != null)
                {
                    Destroy(incomeBlockInstance);
                    incomeBlockInstance = null;
                }
            }
        };
    }

    private void ListenForSupportBlockChanges()
    {
        DatabaseReference supportBlockRef = dbRef
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("blockSupport");

        supportBlockRef.ValueChanged += async (sender, args) =>
        {
            var supportBlockSnapshot = args.Snapshot;

            if (supportBlockSnapshot.Exists)
            {
                int turnsTakenInSupportBlock = Convert.ToInt32(supportBlockSnapshot.Child("turnsTaken").Value);
                string playerIdFromSupportBlock = supportBlockSnapshot.Child("playerId").Value.ToString();

                // Pobieranie turnsTaken z Firebase
                int turnsTakenInStats = await GetTurnsTakenFromStats(playerIdFromSupportBlock);

                // Sprawdzanie, czy turnsTaken z supportBlock pasuje do wartoœci w stats
                if (turnsTakenInSupportBlock == turnsTakenInStats)
                {
                    CreateSupportBlockPrefab();
                }
            }
            else
            {
                if (supportBlockInstance != null)
                {
                    Destroy(supportBlockInstance);
                    supportBlockInstance = null;
                }
            }
        };
    }

    private void ListenForBudgetBlockChanges()
    {
        DatabaseReference budgetBlockRef = dbRef
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("blockBudget");

        budgetBlockRef.ValueChanged += async (sender, args) =>
        {
            var budgetBlockSnapshot = args.Snapshot;

            if (budgetBlockSnapshot.Exists)
            {
                int turnsTakenInBudgetBlock = Convert.ToInt32(budgetBlockSnapshot.Child("turnsTaken").Value);
                string playerIdFromBudgetBlock = budgetBlockSnapshot.Child("playerId").Value.ToString();

                // Pobieranie turnsTaken z Firebase
                int turnsTakenInStats = await GetTurnsTakenFromStats(playerIdFromBudgetBlock);

                // Sprawdzanie, czy turnsTaken z budgetBlock pasuje do wartoœci w stats
                if (turnsTakenInBudgetBlock == turnsTakenInStats)
                {
                    CreateBudgetBlockPrefab();
                }
            }
            else
            {
                if (budgetBlockInstance != null)
                {
                    Destroy(budgetBlockInstance);
                    budgetBlockInstance = null;
                }
            }
        };
    }

    private void CreateSupportBlockPrefab()
    {
        if (supportBlockInstance == null)
        {
            DataTransfer.EffectActive = true;
            supportBlockInstance = Instantiate(supportBlockPrefab, scrollViewContent.transform);
            supportBlockInstance.SetActive(true);
        }
    }

    private void CreateBudgetBlockPrefab()
    {
        if (budgetBlockInstance == null)
        {
            DataTransfer.EffectActive = true;
            budgetBlockInstance = Instantiate(budgetBlockPrefab, scrollViewContent.transform);
            budgetBlockInstance.SetActive(true);
        }
    }

    private void CreateIncomeBlockPrefab()
    {
        if (incomeBlockInstance == null)
        {
            DataTransfer.EffectActive = true;
            incomeBlockInstance = Instantiate(incomeBlockPrefab, scrollViewContent.transform);
            incomeBlockInstance.SetActive(true);
        }
    }

    private void ListenForBudgetPenaltyChanges()
    {
        DatabaseReference budgetPenaltyRef = dbRef
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("budgetPenalty");

        budgetPenaltyRef.ValueChanged += async (sender, args) =>
        {
            var budgetPenaltySnapshot = args.Snapshot;

            if (budgetPenaltySnapshot.Exists)
            {
                int turnsTakenInBudgetPenalty = Convert.ToInt32(budgetPenaltySnapshot.Child("turnsTaken").Value);
                string playerIdFromBudgetPenalty = budgetPenaltySnapshot.Child("playerId").Value.ToString();

                int turnsTakenInStats = await GetTurnsTakenFromStats(playerIdFromBudgetPenalty);

                if (turnsTakenInBudgetPenalty == turnsTakenInStats)
                {
                    CreateBudgetPenaltyPrefab();
                }
            }
            else
            {
                if (budgetPenaltyInstance != null)
                {
                    Destroy(budgetPenaltyInstance);
                    budgetPenaltyInstance = null;
                }
            }
        };
    }

    private void CreateBudgetPenaltyPrefab()
    {
        if (budgetPenaltyInstance == null)
        {
            DataTransfer.EffectActive = true;
            budgetPenaltyInstance = Instantiate(budgetPenaltyPrefab, scrollViewContent.transform);
            budgetPenaltyInstance.SetActive(true);
            ListenForBudgetPenaltyDelete();
        }
    }

    private void ListenForCopySupportChanges()
    {
        DatabaseReference copySupportRef = dbRef
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        copySupportRef.ChildChanged += async (sender, args) =>
        {
            var playerSnapshot = args.Snapshot;
            string otherPlayerId = playerSnapshot.Key;

            if (otherPlayerId == playerId) return;

            var copySupportSnapshot = playerSnapshot.Child("copySupport");
            if (!copySupportSnapshot.Exists || copySupportSnapshot.Child("enemyId").Value.ToString() != playerId)
            {
                return;
            }

            int copySupportTurn = Convert.ToInt32(copySupportSnapshot.Child("turnsTaken").Value);

            copySupportTurnsTakenRef = dbRef
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(otherPlayerId)
                .Child("stats")
                .Child("turnsTaken");

            DataSnapshot turnsTakenSnapshot = await copySupportTurnsTakenRef.GetValueAsync();

            if (!turnsTakenSnapshot.Exists || Convert.ToInt32(turnsTakenSnapshot.Value) != copySupportTurn)
            {
                return;
            }

            CreateCopySupportPrefab();
        };
    }

    private void ListenForCopyBudgetChanges()
    {
        DatabaseReference copyBudgetRef = dbRef
            .Child("sessions")
            .Child(lobbyId)
            .Child("players");

        copyBudgetRef.ChildChanged += async (sender, args) =>
        {
            var playerSnapshot = args.Snapshot;
            string otherPlayerId = playerSnapshot.Key;

            if (otherPlayerId == playerId) return;

            var copyBudgetSnapshot = playerSnapshot.Child("copyBudget");
            if (!copyBudgetSnapshot.Exists || copyBudgetSnapshot.Child("enemyId").Value.ToString() != playerId)
            {
                return;
            }

            int copyBudgetTurn = Convert.ToInt32(copyBudgetSnapshot.Child("turnsTaken").Value);

            copyBudgetTurnsTakenRef = dbRef
                .Child("sessions")
                .Child(lobbyId)
                .Child("players")
                .Child(otherPlayerId)
                .Child("stats")
                .Child("turnsTaken");

            DataSnapshot turnsTakenSnapshot = await copyBudgetTurnsTakenRef.GetValueAsync();

            if (!turnsTakenSnapshot.Exists || Convert.ToInt32(turnsTakenSnapshot.Value) != copyBudgetTurn)
            {
                return;
            }

            CreateCopyBudgetPrefab();
        };
    }

    private void ListenForIncreaseCostChanges()
    {
        DatabaseReference increaseCostRef = dbRef
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("increaseCost");

        increaseCostRef.ValueChanged += async (sender, args) =>
        {
            var increaseCostSnapshot = args.Snapshot;

            if (increaseCostSnapshot.Exists && increaseCostSnapshot.ChildrenCount > 0)
            {
                var firstChild = increaseCostSnapshot.Children.FirstOrDefault();
                if (firstChild != null)
                {
                    int turnsTakenInIncreaseCost = Convert.ToInt32(firstChild.Child("turnsTaken").Value);
                    int turnsTakenInStats = await GetTurnsTakenFromStats(playerId);
                    turnsTakenInStats++;

                    if (turnsTakenInIncreaseCost == turnsTakenInStats)
                    {
                        CreateIncreaseCostPrefab();
                    }
                }
            }
            else
            {
                if (increaseCostInstance != null)
                {
                    Destroy(increaseCostInstance);
                    increaseCostInstance = null;
                }
            }
        };
    }


    private void ListenForDecreaseCostChanges()
    {
        DatabaseReference decreaseCostRef = dbRef
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("decreaseCost");

        decreaseCostRef.ValueChanged += async (sender, args) =>
        {
            var decreaseCostSnapshot = args.Snapshot;

            if (decreaseCostSnapshot.Exists && decreaseCostSnapshot.Children.Count() > 0)
            {
                var firstChild = decreaseCostSnapshot.Children.FirstOrDefault();
                if (firstChild != null && firstChild.HasChild("turnsTaken"))
                {
                    int turnsTakenInDecreaseCost = Convert.ToInt32(firstChild.Child("turnsTaken").Value);
                    int turnsTakenInStats = await GetTurnsTakenFromStats(playerId);

                    if (turnsTakenInDecreaseCost == turnsTakenInStats)
                    {
                        CreateDecreaseCostPrefab();
                    }
                }
            }
        };
    }


    private void ListenForIncreaseAllCostChanges()
    {
        DatabaseReference increaseAllCostRef = dbRef
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("increaseCostAllTurn");

        increaseAllCostRef.ValueChanged += async (sender, args) =>
        {
            var increaseAllCostSnapshot = args.Snapshot;

            if (increaseAllCostSnapshot.Exists && increaseAllCostSnapshot.ChildrenCount > 0)
            {
                var firstChild = increaseAllCostSnapshot.Children.FirstOrDefault();
                if (firstChild != null)
                {
                    int turnsTakenInIncreaseAllCost = Convert.ToInt32(firstChild.Child("turnsTaken").Value);
                    int turnsTakenInStats = await GetTurnsTakenFromStats(playerId);
                    turnsTakenInStats++;

                    if (turnsTakenInIncreaseAllCost == turnsTakenInStats)
                    {
                        CreateIncreaseAllCostPrefab();
                    }
                }
            }
        };
    }


    private async Task<int> GetTurnsTakenFromStats(string playerId)
    {
        var dbRefStatsTurnsTaken = dbRef
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("stats")
            .Child("turnsTaken");

        DataSnapshot turnsTakenSnapshot = await dbRefStatsTurnsTaken.GetValueAsync();
        return turnsTakenSnapshot.Exists ? Convert.ToInt32(turnsTakenSnapshot.Value) : 0;
    }

    private void CreateCopySupportPrefab()
    {
        if (copySupportInstance == null)
        {
            DataTransfer.EffectActive = true;
            copySupportInstance = Instantiate(copySupportPrefab, scrollViewContent.transform);
            copySupportInstance.SetActive(true);
        }
    }

    private void CreateCopyBudgetPrefab()
    {
        if (copyBudgetInstance == null)
        {
            DataTransfer.EffectActive = true;
            copyBudgetInstance = Instantiate(copyBudgetPrefab, scrollViewContent.transform);
            copyBudgetInstance.SetActive(true);
        }
    }

    private void CreateIncreaseCostPrefab()
    {
        if (increaseCostInstance == null)
        {
            DataTransfer.EffectActive = true;
            increaseCostInstance = Instantiate(increaseCostPrefab, scrollViewContent.transform);
            increaseCostInstance.SetActive(true);
        }
    }

    private void CreateIncreaseAllCostPrefab()
    {
        if (increaseAllCostInstance == null)
        {
            DataTransfer.EffectActive = true;
            increaseAllCostInstance = Instantiate(increaseCostPrefab, scrollViewContent.transform);
            increaseAllCostInstance.SetActive(true);
        }
    }

    private void CreateDecreaseCostPrefab()
    {
        if (decreaseCostInstance == null)
        {
            DataTransfer.EffectActive = true;
            decreaseCostInstance = Instantiate(decreaseCostPrefab, scrollViewContent.transform);
            decreaseCostInstance.SetActive(true);
        }
    }

    private void ListenForBudgetPenaltyDelete()
    {
        budgetPenaltyRef = dbRef
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("budgetPenalty");

        budgetPenaltyListener = (sender, args) =>
        {
            if (!args.Snapshot.Exists)
            {
                if (budgetPenaltyInstance != null)
                {
                    Destroy(budgetPenaltyInstance);
                    budgetPenaltyInstance = null;
                }

                budgetPenaltyRef.ValueChanged -= budgetPenaltyListener;
            }
        };

        budgetPenaltyRef.ValueChanged += budgetPenaltyListener;
    }

    private void OnDestroy()
    {
        if (budgetPenaltyRef != null)
        {
            budgetPenaltyRef.ValueChanged -= budgetPenaltyListener;
        }
    }

}
