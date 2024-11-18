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

    public async Task<int> SelectArea()
    {
        await FetchDataFromDatabase();
        return await WaitForAreaSelection();
    }

    public async Task FetchDataFromDatabase()
    {
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

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
        var playersSnapshot = await dbRef.Child("players").GetValueAsync();

        if (!playersSnapshot.Exists)
        {
            Debug.LogError("No player data found in the database.");
            return;
        }

        var mapSnapshot = await dbRef.Child("map").GetValueAsync();
        if (!mapSnapshot.Exists)
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

    public async Task<int> GetCurrentSupportForRegion(int areaId, string excludedPlayerId)
    {
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
        lobbyId = DataTransfer.LobbyId;
        int maxSupport = 0;

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("map");

        var snapshot = await dbRef.GetValueAsync();

        if (snapshot.Exists)
        {
            switch (areaId)
            {
                case 0:
                    maxSupport = Convert.ToInt32(snapshot.Child("region1").Child("maxSupport").Value);
                    break;
                case 1:
                    maxSupport = Convert.ToInt32(snapshot.Child("region2").Child("maxSupport").Value);
                    break;
                case 2:
                    maxSupport = Convert.ToInt32(snapshot.Child("region3").Child("maxSupport").Value);
                    break;
                case 3:
                    maxSupport = Convert.ToInt32(snapshot.Child("region4").Child("maxSupport").Value);
                    break;
                case 4:
                    maxSupport = Convert.ToInt32(snapshot.Child("region5").Child("maxSupport").Value);
                    break;
                case 5:
                    maxSupport = Convert.ToInt32(snapshot.Child("region6").Child("maxSupport").Value);
                    break;
                default:
                    Debug.LogError("Invalid region ID.");
                    break;
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
        lobbyId = DataTransfer.LobbyId;
        bool sameRegion = false;

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("map");

        var snapshot = await dbRef.GetValueAsync();

        if (snapshot.Exists)
        {
            switch (areaId)
            {
                case 0:
                    sameRegion = snapshot.Child("region1").Child("type").Value.ToString() == cardType;
                    break;
                case 1:
                    sameRegion = snapshot.Child("region2").Child("type").Value.ToString() == cardType;
                    break;
                case 2:
                    sameRegion = snapshot.Child("region3").Child("type").Value.ToString() == cardType;
                    break;
                case 3:
                    sameRegion = snapshot.Child("region4").Child("type").Value.ToString() == cardType;
                    break;
                case 4:
                    sameRegion = snapshot.Child("region5").Child("type").Value.ToString() == cardType;
                    break;
                case 5:
                    sameRegion = snapshot.Child("region6").Child("type").Value.ToString() == cardType;
                    break;
                default:
                    Debug.LogError("Invalid region ID.");
                    break;
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
        region1Button.onClick.AddListener(() => RegionClicked(0));
        region2Button.onClick.AddListener(() => RegionClicked(1));
        region3Button.onClick.AddListener(() => RegionClicked(2));
        region4Button.onClick.AddListener(() => RegionClicked(3));
        region5Button.onClick.AddListener(() => RegionClicked(4));
        region6Button.onClick.AddListener(() => RegionClicked(5));

        mapPanel.SetActive(true);
    }

    public void RegionClicked(int regionId)
    {
        TaskOnClickCompleted?.Invoke(regionId);
        mapPanel.SetActive(false);
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
}
