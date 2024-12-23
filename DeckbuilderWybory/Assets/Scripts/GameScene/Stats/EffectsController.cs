using System.Threading.Tasks;
using UnityEngine;
using Firebase.Database;
using System;

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
    public Transform scrollViewContent;

    private GameObject copySupportInstance;
    private GameObject copyBudgetInstance;
    private GameObject increaseCostInstance;
    private GameObject decreaseCostInstance;
    private GameObject increaseAllCostInstance;
    private GameObject budgetPenaltyInstance;

    private DatabaseReference copySupportTurnsTakenRef;
    private DatabaseReference copyBudgetTurnsTakenRef;
    private DatabaseReference increaseCostTurnsTakenRef;
    private DatabaseReference decreaseCostTurnsTakenRef;
    private DatabaseReference increaseAllCostTurnsTakenRef;
    private DatabaseReference budgetPenaltyTurnsTakenRef;

    private int savedCopySupportTurnsTaken;
    private int savedCopyBudgetTurnsTaken;
    private int savedDecreaseCostTurnsTaken;
    private int savedIncreaseAllCostTurnsTaken;
    private int savedBudgetPenaltyTurnsTaken;

    public GameObject incomeBlockPrefab;
    public GameObject supportBlockPrefab;
    public GameObject budgetBlockPrefab;

    private GameObject incomeBlockInstance;
    private GameObject supportBlockInstance;
    private GameObject budgetBlockInstance;

    private DatabaseReference incomeBlockTurnsTakenRef;
    private DatabaseReference supportBlockTurnsTakenRef;
    private DatabaseReference budgetBlockTurnsTakenRef;

    private int savedIncomeBlockTurnsTaken;
    private int savedSupportBlockTurnsTaken;
    private int savedBudgetBlockTurnsTaken;


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
            CheckIsFirstCardChange();
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
                int turnsTakenInStats = await GetTurnsTakenFromStats(playerId);
                turnsTakenInStats++;

                if (turnsTakenInIncomeBlock == turnsTakenInStats)
                {
                    savedIncomeBlockTurnsTaken = turnsTakenInStats;
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

    private void CreateIncomeBlockPrefab()
    {
        if (incomeBlockInstance == null)
        {
            incomeBlockInstance = Instantiate(incomeBlockPrefab, scrollViewContent.transform);
            incomeBlockInstance.SetActive(true);
            ListenForIncomeBlockTurnsTakenChange();
        }
    }

    private void ListenForIncomeBlockTurnsTakenChange()
    {
        incomeBlockTurnsTakenRef = dbRef
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("stats")
            .Child("turnsTaken");

        incomeBlockTurnsTakenRef.ValueChanged += (sender, args) =>
        {
            int currentTurnsTaken = Convert.ToInt32(args.Snapshot.Value);

            if (currentTurnsTaken > savedIncomeBlockTurnsTaken)
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
                int turnsTakenInStats = await GetTurnsTakenFromStats(playerId);
                turnsTakenInStats++;

                if (turnsTakenInSupportBlock == turnsTakenInStats)
                {
                    savedSupportBlockTurnsTaken = turnsTakenInStats;
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

    private void CreateSupportBlockPrefab()
    {
        if (supportBlockInstance == null)
        {
            supportBlockInstance = Instantiate(supportBlockPrefab, scrollViewContent.transform);
            supportBlockInstance.SetActive(true);
            ListenForSupportBlockTurnsTakenChange();
        }
    }

    private void ListenForSupportBlockTurnsTakenChange()
    {
        supportBlockTurnsTakenRef = dbRef
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("stats")
            .Child("turnsTaken");

        supportBlockTurnsTakenRef.ValueChanged += (sender, args) =>
        {
            int currentTurnsTaken = Convert.ToInt32(args.Snapshot.Value);

            if (currentTurnsTaken > savedSupportBlockTurnsTaken)
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
                int turnsTakenInStats = await GetTurnsTakenFromStats(playerId);
                turnsTakenInStats++;

                if (turnsTakenInBudgetBlock == turnsTakenInStats)
                {
                    savedBudgetBlockTurnsTaken = turnsTakenInStats;
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

    private void CreateBudgetBlockPrefab()
    {
        if (budgetBlockInstance == null)
        {
            budgetBlockInstance = Instantiate(budgetBlockPrefab, scrollViewContent.transform);
            budgetBlockInstance.SetActive(true);
            ListenForBudgetBlockTurnsTakenChange();
        }
    }

    private void ListenForBudgetBlockTurnsTakenChange()
    {
        budgetBlockTurnsTakenRef = dbRef
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("stats")
            .Child("turnsTaken");

        budgetBlockTurnsTakenRef.ValueChanged += (sender, args) =>
        {
            int currentTurnsTaken = Convert.ToInt32(args.Snapshot.Value);

            if (currentTurnsTaken > savedBudgetBlockTurnsTaken)
            {
                if (budgetBlockInstance != null)
                {
                    Destroy(budgetBlockInstance);
                    budgetBlockInstance = null;
                }
            }
        };
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
                int turnsTakenInStats = await GetTurnsTakenFromStats(playerId);
                turnsTakenInStats++;

                if (turnsTakenInBudgetPenalty == turnsTakenInStats)
                {
                    savedBudgetPenaltyTurnsTaken = turnsTakenInStats;
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
            budgetPenaltyInstance = Instantiate(budgetPenaltyPrefab, scrollViewContent.transform);
            budgetPenaltyInstance.SetActive(true);
            ListenForBudgetPenaltyTurnsTakenChange();
        }
    }

    private void ListenForBudgetPenaltyTurnsTakenChange()
    {
        budgetPenaltyTurnsTakenRef = dbRef
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("stats")
            .Child("turnsTaken");

        budgetPenaltyTurnsTakenRef.ValueChanged += (sender, args) =>
        {
            int currentTurnsTaken = Convert.ToInt32(args.Snapshot.Value);

            if (currentTurnsTaken > savedBudgetPenaltyTurnsTaken)
            {
                if (budgetPenaltyInstance != null)
                {
                    Destroy(budgetPenaltyInstance);
                    budgetPenaltyInstance = null;
                }
            }
        };
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

            savedCopySupportTurnsTaken = Convert.ToInt32(turnsTakenSnapshot.Value);

            CreateCopySupportPrefab(otherPlayerId, copySupportTurnsTakenRef);
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

            savedCopyBudgetTurnsTaken = Convert.ToInt32(turnsTakenSnapshot.Value);

            CreateCopyBudgetPrefab(otherPlayerId, copyBudgetTurnsTakenRef);
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

            if (increaseCostSnapshot.Exists)
            {
                int turnsTakenInIncreaseCost = Convert.ToInt32(increaseCostSnapshot.Child("turnsTaken").Value);
                int turnsTakenInStats = await GetTurnsTakenFromStats(playerId);
                turnsTakenInStats++;

                if (turnsTakenInIncreaseCost == turnsTakenInStats)
                {
                    CreateIncreaseCostPrefab();
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

            if (decreaseCostSnapshot.Exists)
            {
                int turnsTakenInDecreaseCost = Convert.ToInt32(decreaseCostSnapshot.Child("turnsTaken").Value);
                int turnsTakenInStats = await GetTurnsTakenFromStats(playerId);

                if (turnsTakenInDecreaseCost == turnsTakenInStats)
                {
                    savedDecreaseCostTurnsTaken = turnsTakenInStats;

                    CreateDecreaseCostPrefab();
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

            if (increaseAllCostSnapshot.Exists)
            {
                int turnsTakenInIncreaseAllCost = Convert.ToInt32(increaseAllCostSnapshot.Child("turnsTaken").Value);
                int turnsTakenInStats = await GetTurnsTakenFromStats(playerId);
                turnsTakenInStats++;

                if (turnsTakenInIncreaseAllCost == turnsTakenInStats)
                {
                    savedIncreaseAllCostTurnsTaken = turnsTakenInStats;

                    CreateIncreaseAllCostPrefab();
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

    private void CreateCopySupportPrefab(string otherPlayerId, DatabaseReference turnsTakenRef)
    {
        if (copySupportInstance == null)
        {
            copySupportInstance = Instantiate(copySupportPrefab, scrollViewContent.transform);
            copySupportInstance.SetActive(true);
            ListenForCopySupportTurnsTakenChange(turnsTakenRef);
        }
    }

    private void CreateCopyBudgetPrefab(string otherPlayerId, DatabaseReference turnsTakenRef)
    {
        if (copyBudgetInstance == null)
        {
            copyBudgetInstance = Instantiate(copyBudgetPrefab, scrollViewContent.transform);
            copyBudgetInstance.SetActive(true);
            ListenForCopyBudgetTurnsTakenChange(turnsTakenRef);
        }
    }

    private void CreateIncreaseCostPrefab()
    {
        if (increaseCostInstance == null)
        {
            increaseCostInstance = Instantiate(increaseCostPrefab, scrollViewContent.transform);
            increaseCostInstance.SetActive(true);
        }
    }

    private void CreateIncreaseAllCostPrefab()
    {
        if (increaseAllCostInstance == null)
        {
            increaseAllCostInstance = Instantiate(increaseCostPrefab, scrollViewContent.transform);
            increaseAllCostInstance.SetActive(true);
            ListenForIncreaseAllTurnsTakenChange();
        }
    }

    private void CreateDecreaseCostPrefab()
    {
        if (decreaseCostInstance == null)
        {
            decreaseCostInstance = Instantiate(decreaseCostPrefab, scrollViewContent.transform);
            decreaseCostInstance.SetActive(true);
            ListenForDecreaseCostTurnsTakenChange();
        }
    }

    private void ListenForCopySupportTurnsTakenChange(DatabaseReference turnsTakenRef)
    {
        turnsTakenRef.ValueChanged += (sender, args) =>
        {
            int currentTurnsTaken = Convert.ToInt32(args.Snapshot.Value);

            if (currentTurnsTaken != savedCopySupportTurnsTaken)
            {
                if (copySupportInstance != null)
                {
                    Destroy(copySupportInstance);
                    copySupportInstance = null;
                }
            }
        };
    }

    private void ListenForCopyBudgetTurnsTakenChange(DatabaseReference turnsTakenRef)
    {
        turnsTakenRef.ValueChanged += (sender, args) =>
        {
            int currentTurnsTaken = Convert.ToInt32(args.Snapshot.Value);

            if (currentTurnsTaken != savedCopyBudgetTurnsTaken)
            {
                if (copyBudgetInstance != null)
                {
                    Destroy(copyBudgetInstance);
                    copyBudgetInstance = null;
                }
            }
        };
    }

    private void CheckIsFirstCardChange()
    {

        if (DataTransfer.IsFirstCardInTurn == false)
        {
            if (increaseCostInstance != null)
            {
                Destroy(increaseCostInstance);
                increaseCostInstance = null;
            }
        }
    }


    private void ListenForDecreaseCostTurnsTakenChange()
    {
        decreaseCostTurnsTakenRef = dbRef
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("stats")
            .Child("turnsTaken");

        decreaseCostTurnsTakenRef.ValueChanged += (sender, args) =>
        {
            int currentTurnsTaken = Convert.ToInt32(args.Snapshot.Value);

            if (currentTurnsTaken != savedDecreaseCostTurnsTaken)
            {
                if (decreaseCostInstance != null)
                {
                    Destroy(decreaseCostInstance);
                    decreaseCostInstance = null;
                }
            }
        };
    }

    private void ListenForIncreaseAllTurnsTakenChange()
    {
        increaseCostTurnsTakenRef = dbRef
            .Child("sessions")
            .Child(lobbyId)
            .Child("players")
            .Child(playerId)
            .Child("stats")
            .Child("turnsTaken");

        increaseCostTurnsTakenRef.ValueChanged += (sender, args) =>
        {
            int currentTurnsTaken = Convert.ToInt32(args.Snapshot.Value);

            if (currentTurnsTaken > savedIncreaseAllCostTurnsTaken)
            {
                if (increaseAllCostInstance != null)
                {
                    Destroy(increaseAllCostInstance);
                    increaseAllCostInstance = null;
                }
            }
        };
    }

    private void OnDestroy()
    {
        if (copySupportTurnsTakenRef != null)
        {
            copySupportTurnsTakenRef.ValueChanged -= null;
        }

        if (copyBudgetTurnsTakenRef != null)
        {
            copyBudgetTurnsTakenRef.ValueChanged -= null;
        }

        if (increaseCostTurnsTakenRef != null)
        {
            increaseCostTurnsTakenRef.ValueChanged -= null;
        }

        if (increaseAllCostTurnsTakenRef != null)
        {
            increaseAllCostTurnsTakenRef.ValueChanged -= null;
        }

        if (decreaseCostTurnsTakenRef != null)
        {
            decreaseCostTurnsTakenRef.ValueChanged -= null;
        }
        if (budgetPenaltyTurnsTakenRef != null)
        {
            budgetPenaltyTurnsTakenRef.ValueChanged -= null;
        }

        if (incomeBlockTurnsTakenRef != null)
        {
            incomeBlockTurnsTakenRef.ValueChanged -= null;
        }

        if (supportBlockTurnsTakenRef != null)
        {
            supportBlockTurnsTakenRef.ValueChanged -= null;
        }

        if (budgetBlockTurnsTakenRef != null)
        {
            budgetBlockTurnsTakenRef.ValueChanged -= null;
        }
    }
}
