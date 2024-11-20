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

public class Map : MonoBehaviour
{
    DatabaseReference dbRef;

    string lobbyId;
    string playerId;
    string playerName;
    string playerSupport;
    string regionMax;
    int lobbySize;
    int regionMaxInt;
    public GameObject mapPanel;

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

    public Text regionStatsName;
    public Text regionStatsMax;


    public GameObject buttonTemplate;
    public GameObject scrollViewContent;

    public GameObject segmentPrefab; // Prefab dla każdego segmentu
    public Transform chartContainer; // Kontener na segmenty (np. Panel)
    public Color[] segmentColors;
    public float[] valuesChart = new float[] {0,0,0,0,0,0,0,0 };

    private List<GameObject> createdButtons = new List<GameObject>();

    private Dictionary<string, string> playerNameToIdMap = new Dictionary<string, string>();


    public void Start()
    {

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        lobbyId = DataTransfer.LobbyId;
        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);

        // Sprawd�, czy sesja istnieje w bazie danych przed pobraniem danych mapy
        SessionExists().ContinueWith(task =>
        {
            if (task.Result)
            {
                dbRef.GetValueAsync().ContinueWithOnMainThread(snapshotTask =>
                {
                    if (snapshotTask.IsFaulted)
                    {
                        Debug.LogError("Error getting data from Firebase: " + snapshotTask.Exception);
                        return;
                    }

                    DataSnapshot snapshot = snapshotTask.Result;

                    if (snapshot.Exists)
                    {
                        playerId = DataTransfer.PlayerId;
                        Debug.Log("PlayerId: " + playerId);

                        // Odczytaj warto�ci region�w dla aktualnego lobby
                        foreach (var childSnapshot in snapshot.Children)
                        {
                            if (childSnapshot.Key == "map") // Zak�adaj�c, �e dane region�w s� w ga��zi "map"
                            {

                                foreach (var regionSnapshot in childSnapshot.Children)
                                {
                                    string regionName = regionSnapshot.Key;
                                    string regionValue = regionSnapshot.Child("currentSupport").Value.ToString();
                                    string regionMax = regionSnapshot.Child("maxSupport").Value.ToString();
                                    //float regionMaxInt = regionSnapshot.Child("maxSupport").Value.ToFloat();
                                    //float.TryParse(regionSnapshot.Child("maxSupport").Value.ToString(), out regionMaxFloat[i]);

                                    Debug.Log("regionValue = " + regionValue);

                                    // Ustaw warto�� regionu na podstawie tagu
                                    SetRegionValue(regionName, regionValue, regionMax);
                                    //float.TryParse(regionMax, out regionMaxFloat[i]);

                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("Data does not exist in the database.");
                    }
                });
            }
            else
            {
                Debug.Log("Session does not exist in the database.");
            }
        });



        region1Button.onClick.AddListener(() => OpenRegionStats("1"));
        region2Button.onClick.AddListener(() => OpenRegionStats("2"));
        region3Button.onClick.AddListener(() => OpenRegionStats("3"));
        region4Button.onClick.AddListener(() => OpenRegionStats("4"));
        region5Button.onClick.AddListener(() => OpenRegionStats("5"));
        region6Button.onClick.AddListener(() => OpenRegionStats("6"));



        clickBackground.onClick.AddListener(BackgroundClicked);

    }


    void SetRegionValue(string regionName, string regionValue, string regionMax)
    {
        // Przypisz warto�� regionValue do odpowiedniego regionu na podstawie nazwy regionu
        switch (regionName)
        {
            case "region1":
                regionText[0].text = regionValue + "/" + regionMax;
                break;
            case "region2":
                regionText[1].text = regionValue + "/" + regionMax;
                break;
            case "region3":
                regionText[2].text = regionValue + "/" + regionMax;
                break;
            case "region4":
                regionText[3].text = regionValue + "/" + regionMax;
                break;
            case "region5":
                regionText[4].text = regionValue + "/" + regionMax;
                break;
            case "region6":
                regionText[5].text = regionValue + "/" + regionMax;
                break;
            default:
                Debug.LogWarning("Unknown region name: " + regionName);
                break;
        }
    }

    void OpenRegionStats(string regionNumber){
        regionStatsName.text = "Region " + regionNumber;
        int regionN;
        int.TryParse(regionNumber, out regionN);
        regionStatsMax.text= regionText[regionN-1].text;

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

            regionMax = snapshot.Child("map").Child("region"+regionNumber).Child("maxSupport").Value.ToString();
            playerName = snapshot.Child("players").Child(playerId).Child("playerName").Value.ToString();
            int playerNumber =0;
            // Pobierz playerName i playerId dla ka�dego innego gracza ni� ty
            foreach (var childSnapshot in snapshot.Child("players").Children)
            {
                string otherPlayerId = childSnapshot.Key;
                //if (otherPlayerId != playerId)
                {
                    string otherPlayerName = childSnapshot.Child("playerName").Value.ToString();
                    playerNameToIdMap[otherPlayerName] = otherPlayerId;
                    playerSupport = snapshot.Child("players").Child(otherPlayerId).Child("stats").Child("regionSupport").Child(regionNumber).Value.ToString();//.Child(regionNumber).Value.ToString();
                    float.TryParse(playerSupport, out valuesChart[playerNumber]);
                    // Utw�rz przycisk dla innego gracza
                    CreateButton(otherPlayerName, playerSupport, playerNumber);
                    playerNumber++;

                }
            }

            //float total = 18f;
            //float total[] = regionMaxFloat;
            //Debug.LogError(regionMax);
            int.TryParse(regionMax, out regionMaxInt);
            CreateChart(valuesChart,regionMaxInt);//, total);
        });




        //SetValues(valuesChart);
    }

    void CreateButton(string otherPlayerName, string playerSupport, int playerNumber)
    {
        GameObject button = Instantiate(buttonTemplate, scrollViewContent.transform);
        button.SetActive(true);
        button.GetComponentInChildren<UnityEngine.UI.Text>().text = otherPlayerName + ": " + playerSupport;
        button.GetComponent<Image>().color = segmentColors[playerNumber];

        createdButtons.Add(button);
    }

    void BackgroundClicked()
    {
        regions.SetActive(true);
        regionStatsPanel.SetActive(false);

        DestroyButtons();
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



    async Task<bool> SessionExists()
    {
        var sessionCheck = await dbRef.Parent.Parent.GetValueAsync();
        return sessionCheck.Exists;
    }


}
