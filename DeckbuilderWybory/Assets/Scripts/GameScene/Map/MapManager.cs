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
    private readonly int lobbySize = DataTransfer.LobbySize;
    string playerId;
    string playerName;
    string playerSupport;
    public GameObject mapPanel;

    string[] mapType = new string[] {"0","0","0","0","0","0"};
    public Image[] regionImage;
    public Color[] regionTypeColor;

    public Text region1Text;
    public Text region2Text;
    public Text region3Text;
    public Text region4Text;
    public Text region5Text;
    public Text region6Text;

    public Button region1Button;
    public Button region2Button;
    public Button region3Button;
    public Button region4Button;
    public Button region5Button;
    public Button region6Button;

    public GameObject[] segmentPrefab; // Prefab dla każdego segmentu
    public Transform[] chartContainer; // Kontener na segmenty (np. Panel)
    public Color[] segmentColors;
    public float[] valuesChart = new float[] {0,0,0,0,0,0,0,0 };

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

            mapType[0] = mapSnapshot.Child("region1").Child("type").Value.ToString();
            mapType[1] = mapSnapshot.Child("region2").Child("type").Value.ToString();
            mapType[2] = mapSnapshot.Child("region3").Child("type").Value.ToString();
            mapType[3] = mapSnapshot.Child("region4").Child("type").Value.ToString();
            mapType[4] = mapSnapshot.Child("region5").Child("type").Value.ToString();
            mapType[5] = mapSnapshot.Child("region6").Child("type").Value.ToString();

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

            DataForChart(0,maxRegion1);
            DataForChart(1,maxRegion2);
            DataForChart(2,maxRegion3);
            DataForChart(3,maxRegion4);
            DataForChart(4,maxRegion5);
            DataForChart(5,maxRegion6);
            //CreateChart(valuesChart,maxRegion1,0);



        }
        catch (Exception ex)
        {
            Debug.LogError($"Error calculating region values: {ex.Message}");
        }
    }
//wartosci wszystkich graczy w regionie
    async Task DataForChart(int regionNumber,int maxRegion)
    {

        string regionNumberDb=regionNumber.ToString();

        valuesChart = new float[valuesChart.Length];

        var snapshot = await GetSnapshot("players");

            //DataSnapshot snapshot = snapshotTask.Result;
            int playerNumber =0;
            // Pobierz playerName i playerId dla ka dego innego gracza ni  ty
            foreach (var childSnapshot in snapshot.Children)
            {
                playerId = childSnapshot.Key;
                playerName = childSnapshot.Child("playerName").Value.ToString();

                playerSupport = snapshot.Child(playerId).Child("stats").Child("support").Child(regionNumberDb).Value.ToString();
                float.TryParse(playerSupport, out valuesChart[playerNumber]);
                //values chart to jest wszystkich graczy w regionie

                playerNumber++;

            }

            //float total = 18f;
            CreateChart(valuesChart,maxRegion,regionNumber);//, total);
//stworzenie chart na region


    }


     public async Task CreateChart(float[] values, int total, int regionNumber)
     {

         foreach (Transform child in chartContainer[regionNumber])
         {
             Destroy(child.gameObject);
         }

         // Tworzenie segmentów
         float currentFill = 0f;
         //float[] total = regionMaxFloat[];
         int lobbySize = DataTransfer.LobbySize;

         for (int i = 0; i < lobbySize; i++)
         {

             // Oblicz proporcję wartości
             float fillAmount = values[i] / total;
             Debug.Log("Region "+ regionNumber+": "+fillAmount);

             // Stwórz nowy segment
             GameObject newSegment = Instantiate(segmentPrefab[regionNumber], chartContainer[regionNumber]);
             Image segmentImage = newSegment.GetComponent<Image>();

             // Ustaw kolor segmentu
             if (i < segmentColors.Length)
             {
                 segmentImage.color = segmentColors[i];
             }

             // Ustaw zakres wypełnienia
             segmentImage.fillAmount = fillAmount;

             // Obróć segment, aby zaczynał się tam, gdzie poprzedni się skończył
             newSegment.transform.rotation = Quaternion.Euler(0f, 0f, -currentFill * 360f);


             // Aktualizuj aktualny fill
             currentFill += fillAmount;
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

        regionImage[0].color = ChooseRegionColor(0);
        regionImage[1].color = ChooseRegionColor(1);
        regionImage[2].color = ChooseRegionColor(2);
        regionImage[3].color = ChooseRegionColor(3);
        regionImage[4].color = ChooseRegionColor(4);
        regionImage[5].color = ChooseRegionColor(5);
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

    Color ChooseRegionColor(int indexMap){
        switch (mapType[indexMap])
        {
            case "Podstawa":
                return regionTypeColor[0];
            case "Ambasada":
                //0 to podstawa
                return regionTypeColor[1];
            case "Środowisko":
                return regionTypeColor[2];
            case "Przemysł":
                return regionTypeColor[3];
            case "Metropolia":
                return regionTypeColor[4];
            default:
                Debug.LogError("Invalid region ID.");
                return regionTypeColor[5];
        }

    }
}
