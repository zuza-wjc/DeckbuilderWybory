using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System;

public class MapManager : MonoBehaviour
{
    DatabaseReference dbRef;

    string lobbyId;
    public GameObject mapPanel;

    public TextMeshProUGUI region1Text;
    public TextMeshProUGUI region2Text;
    public TextMeshProUGUI region3Text;
    public TextMeshProUGUI region4Text;
    public TextMeshProUGUI region5Text;
    public TextMeshProUGUI region6Text;

    public Button region1Button;
    public Button region2Button;
    public Button region3Button;
    public Button region4Button;
    public Button region5Button;
    public Button region6Button;

    private Action<int> TaskOnClickCompleted;

    private bool IsFirebaseInitialized()
    {
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return false;
        }
        return true;
    }

    public async Task<int> SelectArea()
    {
        await FetchDataFromDatabase();
        return await WaitForAreaSelection();
    }

    public async Task FetchDataFromDatabase()
    {
        if (!IsFirebaseInitialized()) return;

        lobbyId = DataTransfer.LobbyId;
        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);

        bool sessionExists = await SessionExists();
        if (!sessionExists)
        {
            Debug.Log("Session does not exist in the database.");
            return;
        }

        await CalculateRegionValues();
        InitializeRegionButtons();
    }

    async Task CalculateRegionValues()
    {
        try
        {
            var playersSnapshot = await GetSnapshot("players");

            if (playersSnapshot == null || !playersSnapshot.Exists)
            {
                Debug.LogError("No player data found in the database.");
                return;
            }

            var mapSnapshot = await GetSnapshot("map");
            if (mapSnapshot == null || !mapSnapshot.Exists)
            {
                Debug.LogError("No map data found in the database.");
                return;
            }

            int maxRegion1 = int.Parse(mapSnapshot.Child("region1").Child("maxSupport").Value.ToString());
            int maxRegion2 = int.Parse(mapSnapshot.Child("region2").Child("maxSupport").Value.ToString());
            int maxRegion3 = int.Parse(mapSnapshot.Child("region3").Child("maxSupport").Value.ToString());
            int maxRegion4 = int.Parse(mapSnapshot.Child("region4").Child("maxSupport").Value.ToString());
            int maxRegion5 = int.Parse(mapSnapshot.Child("region5").Child("maxSupport").Value.ToString());
            int maxRegion6 = int.Parse(mapSnapshot.Child("region6").Child("maxSupport").Value.ToString());

            int[] regionValues = new int[6];

            foreach (var playerSnapshot in playersSnapshot.Children)
            {
                var supportSnapshot = playerSnapshot.Child("stats").Child("support");

                if (supportSnapshot.Exists)
                {
                    int index = 0;
                    foreach (var regionSupport in supportSnapshot.Children)
                    {
                        if (int.TryParse(regionSupport.Value.ToString(), out int supportValue))
                        {
                            regionValues[index] += supportValue;
                        }
                        index++;
                    }
                }
                else
                {
                    Debug.LogWarning($"Player {playerSnapshot.Key} has no support data.");
                }
            }

            UpdateMap(regionValues, maxRegion1, maxRegion2, maxRegion3, maxRegion4, maxRegion5, maxRegion6);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error calculating region values: {ex.Message}");
        }
    }

    public async Task<int> GetCurrentSupportForRegion(int areaId, string excludedPlayerId)
    {
        if (!IsFirebaseInitialized()) return 0;

        lobbyId = DataTransfer.LobbyId;
        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");

        int totalSupport = 0;

        var playersSnapshot = await dbRef.GetValueAsync();

        if (!playersSnapshot.Exists)
        {
            Debug.LogError("No player data found in the database.");
            return 0;
        }

        foreach (var playerSnapshot in playersSnapshot.Children)
        {
            if (playerSnapshot.Key == excludedPlayerId)
            {
                continue;
            }

            var supportSnapshot = playerSnapshot.Child("stats").Child("support");

            if (supportSnapshot.Exists)
            {
                string regionKey = areaId.ToString();

                if (supportSnapshot.HasChild(regionKey))
                {
                    int supportValue = Convert.ToInt32(supportSnapshot.Child(regionKey).Value);
                    totalSupport += supportValue;
                }
            }
            else
            {
                Debug.LogWarning($"Player {playerSnapshot.Key} has no support data.");
            }
        }

        return totalSupport;
    }

    public async Task<int> GetMaxSupportForRegion(int areaId)
    {
        if (!IsFirebaseInitialized()) return 0;

        lobbyId = DataTransfer.LobbyId;
        int maxSupport = 0;

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("map");

        var snapshot = await dbRef.GetValueAsync();

        if (snapshot.Exists)
        {
            var region = snapshot.Child($"region{areaId + 1}");
            if (region.Exists)
            {
                maxSupport = Convert.ToInt32(region.Child("maxSupport").Value);
            }
            else
            {
                Debug.LogError($"Region {areaId + 1} does not exist in the map data.");
            }
        }
        else
        {
            Debug.LogError("Map data does not exist in the database.");
        }

        return maxSupport;
    }

    public async Task<bool> CheckIfBonusRegion(int areaId, string cardType)
    {
        if (!IsFirebaseInitialized()) return false;

        lobbyId = DataTransfer.LobbyId;
        bool sameRegion = false;

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("map");

        var snapshot = await dbRef.GetValueAsync();

        if (snapshot.Exists)
        {
            var region = snapshot.Child($"region{areaId + 1}");
            if (region.Exists)
            {
                sameRegion = region.Child("type").Value.ToString() == cardType;
            }
            else
            {
                Debug.LogError($"Region {areaId + 1} does not exist in the map data.");
            }
        }
        else
        {
            Debug.LogError("Map data does not exist in the database.");
        }

        return sameRegion;
    }

    void UpdateMap(int[] regionValues, int maxRegion1, int maxRegion2, int maxRegion3, int maxRegion4, int maxRegion5, int maxRegion6)
    {
        if (regionValues.Length != 6)
        {
            Debug.LogError("Region values array has an incorrect size.");
            return;
        }

        region1Text.text = $"{regionValues[0]}/{maxRegion1}";
        region2Text.text = $"{regionValues[1]}/{maxRegion2}";
        region3Text.text = $"{regionValues[2]}/{maxRegion3}";
        region4Text.text = $"{regionValues[3]}/{maxRegion4}";
        region5Text.text = $"{regionValues[4]}/{maxRegion5}";
        region6Text.text = $"{regionValues[5]}/{maxRegion6}";
    }

    void InitializeRegionButtons()
    {
        if (region1Button != null) region1Button.onClick.AddListener(() => RegionClicked(0));
        if (region2Button != null) region2Button.onClick.AddListener(() => RegionClicked(1));
        if (region3Button != null) region3Button.onClick.AddListener(() => RegionClicked(2));
        if (region4Button != null) region4Button.onClick.AddListener(() => RegionClicked(3));
        if (region5Button != null) region5Button.onClick.AddListener(() => RegionClicked(4));
        if (region6Button != null) region6Button.onClick.AddListener(() => RegionClicked(5));

        if (mapPanel != null)
        {
            mapPanel.SetActive(true);
        }
    }

    public void RegionClicked(int regionId)
    {
        TaskOnClickCompleted?.Invoke(regionId);
        if (mapPanel != null && mapPanel.activeSelf)
        {
            mapPanel.SetActive(false);
        }
    }

    private Task<int> WaitForAreaSelection()
    {
        var tcs = new TaskCompletionSource<int>();
        TaskOnClickCompleted = (selectedAreaId) => tcs.TrySetResult(selectedAreaId);
        return tcs.Task;
    }

    async Task<bool> SessionExists()
    {
        var sessionCheck = await dbRef.Parent.Parent.GetValueAsync();
        return sessionCheck.Exists;
    }

    private async Task<DataSnapshot> GetSnapshot(string path)
    {
        var snapshot = await dbRef.Child(path).GetValueAsync();
        if (!snapshot.Exists)
        {
            Debug.LogError($"Data at {path} does not exist.");
            return null;
        }
        return snapshot;
    }
}
