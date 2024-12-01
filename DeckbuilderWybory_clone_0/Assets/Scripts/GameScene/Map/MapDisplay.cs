using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Threading.Tasks;

public class MapDisplay : MonoBehaviour
{
    DatabaseReference dbRef;

    string lobbyId;
    string playerId;
    string playerName;
    string[] playerValues = new string[6];
    string playerSupport;
    string[] mapType = new string[] {"0","0","0","0","0","0"};

    string regionMax;
    int regionMaxInt;
    public GameObject mapPanel;
    public Text regionType;
    public Image[] regionImage;

    public Text[] regionText;

    public Button region1Button;
    public Button region2Button;
    public Button region3Button;
    public Button region4Button;
    public Button region5Button;
    public Button region6Button;

    public Button clickBackground;

    public GameObject regions;
    public GameObject regionStatsPanel;
    public Image regionStatsBackgroundPanel;
    public Color[] regionTypeColor;

    public Text regionStatsName;
    public Text regionStatsMax;


    public GameObject buttonTemplate;
    public GameObject scrollViewContent;

    public GameObject segmentPrefab; // Prefab dla każdego segmentu
    public Transform chartContainer; // Kontener na segmenty (np. Panel)
    public Color[] segmentColors;
    public float[] valuesChart = new float[] {0,0,0,0,0,0,0,0 };


    private List<GameObject> createdButtons = new List<GameObject>();

    public GameObject regionPlayerStatsPanel;
    public Transform regionPlayerStatsContainer;

    public Text regionStatsPlayerName;
    public GameObject buttonRegionPlayerTemplate;
    public GameObject scrollViewContentRegionPlayer;

    //private Dictionary<string, string> playerNameToIdMap = new Dictionary<string, string>();


    public async void Start()
    {
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        lobbyId = DataTransfer.LobbyId;
        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);

        await CalculateRegionValues();
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

        // Zamiast bezpoœrednich wartoœci, u¿ywamy dostêpu do "poparcie"
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

        // Reszta kodu pozostaje bez zmian
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

        region1Button.onClick.AddListener(() => OpenRegionStats("1", maxRegion1, regionValues));
        region2Button.onClick.AddListener(() => OpenRegionStats("2", maxRegion2, regionValues));
        region3Button.onClick.AddListener(() => OpenRegionStats("3", maxRegion3, regionValues));
        region4Button.onClick.AddListener(() => OpenRegionStats("4", maxRegion4, regionValues));
        region5Button.onClick.AddListener(() => OpenRegionStats("5", maxRegion5, regionValues));
        region6Button.onClick.AddListener(() => OpenRegionStats("6", maxRegion6, regionValues));

        clickBackground.onClick.AddListener(BackgroundClicked);

    }


    void UpdateMap(int[] regionValues, int maxRegion1, int maxRegion2, int maxRegion3, int maxRegion4, int maxRegion5, int maxRegion6)
    {
        if (regionValues.Length != 6)
        {
            Debug.LogError("Region values array has an incorrect size.");
            return;
        }

        regionText[0].text = $"{regionValues[0]}/{maxRegion1}";
        regionText[1].text = $"{regionValues[1]}/{maxRegion2}";
        regionText[2].text = $"{regionValues[2]}/{maxRegion3}";
        regionText[3].text = $"{regionValues[3]}/{maxRegion4}";
        regionText[4].text = $"{regionValues[4]}/{maxRegion5}";
        regionText[5].text = $"{regionValues[5]}/{maxRegion6}";

        regionImage[0].color = ChooseRegionColor(0);
        regionImage[1].color = ChooseRegionColor(1);
        regionImage[2].color = ChooseRegionColor(2);
        regionImage[3].color = ChooseRegionColor(3);
        regionImage[4].color = ChooseRegionColor(4);
        regionImage[5].color = ChooseRegionColor(5);

    }

    void OpenRegionStats(string regionNumber, int maxRegion, int[] regionValues){
        regionStatsName.text = "Region " + regionNumber;
        int regionN;
        int.TryParse(regionNumber, out regionN);
        //regionStatsMax.text= regionText[regionN-1].text;
        regionStatsMax.text= $"{regionValues[regionN-1]}/{maxRegion}";
        string regionNumberDb=(regionN-1).ToString();


        regionStatsBackgroundPanel.color= ChooseRegionColor(regionN-1);
        regionType.text="TYP: " + mapType[regionN-1].ToUpper();

        valuesChart = new float[valuesChart.Length];

    //imie: wartoscod gracza z regionNUmber

        dbRef.GetValueAsync().ContinueWithOnMainThread(snapshotTask =>
        {
            if (snapshotTask.IsFaulted)
            {
                Debug.LogError("Error getting data from Firebase: " + snapshotTask.Exception);
                return;
            }

            DataSnapshot snapshot = snapshotTask.Result;
            int playerNumber =0;
            // Pobierz playerName i playerId dla ka dego innego gracza ni  ty
            foreach (var childSnapshot in snapshot.Child("players").Children)
            {
                playerId = childSnapshot.Key;
                playerName = childSnapshot.Child("playerName").Value.ToString();
                //mapType[0] = snapshot.Child("map").Child("region"+regionNumber).Child("type").Value.ToString();

                playerSupport = snapshot.Child("players").Child(playerId).Child("stats").Child("support").Child(regionNumberDb).Value.ToString();
                float.TryParse(playerSupport, out valuesChart[playerNumber]);
                //values chart to jest wszystkich graczy w regionie

                Debug.LogError(valuesChart[playerNumber]);
                playerValues[0] = snapshot.Child("players").Child(playerId).Child("stats").Child("support").Child("0").Value.ToString();
                playerValues[1] = snapshot.Child("players").Child(playerId).Child("stats").Child("support").Child("1").Value.ToString();
                playerValues[2] = snapshot.Child("players").Child(playerId).Child("stats").Child("support").Child("2").Value.ToString();
                playerValues[3] = snapshot.Child("players").Child(playerId).Child("stats").Child("support").Child("3").Value.ToString();
                playerValues[4] = snapshot.Child("players").Child(playerId).Child("stats").Child("support").Child("4").Value.ToString();
                playerValues[5] = snapshot.Child("players").Child(playerId).Child("stats").Child("support").Child("5").Value.ToString();
                //playerValue to sa wszystkie regiony jednego gracza

                // Utw rz przycisk dla innego gracza
                CreateButton(playerName, playerValues, playerNumber, regionN-1);

                playerNumber++;

            }

            //float total = 18f;
            CreateChart(valuesChart,maxRegion);//, total);
        });

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

    void CreateButton(string playerName, string[] playerSupport, int playerNumber, int regionN)
    {
        GameObject button = Instantiate(buttonTemplate, scrollViewContent.transform);
        button.SetActive(true);
        button.GetComponentInChildren<UnityEngine.UI.Text>().text = playerName + ": " + playerSupport[regionN];
        button.GetComponent<Image>().color = segmentColors[playerNumber];

        createdButtons.Add(button);
        //wszystkie regiony i wartosci jednego gracza
        string[] currentPlayerSupport = (string[])playerSupport.Clone();
        button.GetComponent<Button>().onClick.AddListener(() => OnButtonClick(playerName, currentPlayerSupport));

    }

    void OnButtonClick(string playerName, string[] playerSupport)
    {
        regionStatsPlayerName.text = playerName;
        foreach (Transform child in regionPlayerStatsContainer)
        {
            Destroy(child.gameObject);
        }
        regionPlayerStatsPanel.SetActive(true);
        //tuu dodaj imie wybranego
        //nazwa to region 1,2,3 itd
        //kolory to typy regionów
        for(int i=0;i<6;i++)
        {
            //pamietaj by kolory odpowiadaly kolorom typów regionów
            GameObject button = Instantiate(buttonRegionPlayerTemplate, scrollViewContentRegionPlayer.transform);
            button.SetActive(true);
            button.GetComponentInChildren<UnityEngine.UI.Text>().text = "Region " + (i+1) + ": " + playerSupport[i];
            button.GetComponent<Image>().color = ChooseRegionColor(i);
        }
    }

    void BackgroundClicked()
    {
        if(regionPlayerStatsPanel.activeSelf){
            regionPlayerStatsPanel.SetActive(false);
        }
        else{
            regions.SetActive(true);
            regionStatsPanel.SetActive(false);

            DestroyButtons();
        }
    }

    void DestroyButtons()
    {
        foreach (var button in createdButtons)
        {
            Destroy(button);
        }
        createdButtons.Clear();

        // Usuń istniejące segmenty (jeśli są)
        foreach (Transform child in chartContainer)
        {
            Destroy(child.gameObject);
        }

    }



     public void CreateChart(float[] values, int total)
        {
            // Tworzenie segmentów
            float currentFill = 0f;
            //float[] total = regionMaxFloat[];

            for (int i = 0; i < values.Length; i++)
            {

                // Oblicz proporcję wartości
                float fillAmount = values[i] / total;

                // Stwórz nowy segment
                GameObject newSegment = Instantiate(segmentPrefab, chartContainer);
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

                /*if (values[i]!=0){
                newSegment.GetComponentInChildren<UnityEngine.UI.Text>().text = values[i].ToString();

                float segmentCenterAngle = ((fillAmount / 2f) + currentFill  ) * 360f;
                // Oblicz pozycję etykiety w obrębie segmentu, używając kątów
                float x = Mathf.Cos(Mathf.Deg2Rad * segmentCenterAngle) * 160f; // 50f to promień, dostosuj do swoich potrzeb
                float y = Mathf.Sin(Mathf.Deg2Rad * segmentCenterAngle) * 160f; // 50f to promień, dostosuj do swoich potrzeb

                // Ustaw pozycję etykiety
                newSegment.GetComponentInChildren<UnityEngine.UI.Text>().transform.localPosition  = new Vector3(x, y, 0f);

                };*/
                // Aktualizuj aktualny fill
                currentFill += fillAmount;
            }
        }


}