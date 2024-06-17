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

public class MapManager : MonoBehaviour
{
    DatabaseReference dbRef;

    string lobbyId;
    string playerId;
    string playerName;
    int support;
    int supportAddValueReload;
    int supportOnMap;
    string supportAddValue;
    int lobbySize;
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

    public string cardIdMap;

    private Dictionary<string, string> playerNameToIdMap = new Dictionary<string, string>();

    public async void FetchDataFromDatabase()
    {
        InitializeFirebase();

        lobbyId = DataTransfer.LobbyId;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId);

        bool sessionExists = await SessionExists();
        if (!sessionExists)
        {
            Debug.Log("Session does not exist in the database.");
            return;
        }

        var snapshot = await dbRef.GetValueAsync();
        if (snapshot.Exists)
        {
            playerId = DataTransfer.PlayerId;
            Debug.Log("PlayerId: " + playerId);

            foreach (var childSnapshot in snapshot.Children)
            {
                if (childSnapshot.Key == "map")
                {
                    foreach (var regionSnapshot in childSnapshot.Children)
                    {
                        string regionName = regionSnapshot.Key;
                        string regionValue = regionSnapshot.Value.ToString();
                        SetRegionValue(regionName, regionValue);
                    }
                }
            }
        }
        else
        {
            Debug.Log("Data does not exist in the database.");
        }

        InitializeRegionButtons();
    }

    void InitializeFirebase()
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            FirebaseInitializer firebaseInitializer = FindObjectOfType<FirebaseInitializer>();
            if (firebaseInitializer == null)
            {
                Debug.LogError("FirebaseInitializer not found in the scene!");
                return;
            }
        }
    }

    void InitializeRegionButtons()
    {
        region1Button.onClick.AddListener(() => RegionClicked("region1", cardIdMap));
        region2Button.onClick.AddListener(() => RegionClicked("region2", cardIdMap));
        region3Button.onClick.AddListener(() => RegionClicked("region3", cardIdMap));
        region4Button.onClick.AddListener(() => RegionClicked("region4", cardIdMap));
        region5Button.onClick.AddListener(() => RegionClicked("region5", cardIdMap));
        region6Button.onClick.AddListener(() => RegionClicked("region6", cardIdMap));
    }

    void SetRegionValue(string regionName, string regionValue)
    {
        switch (regionName)
        {
            case "region1":
                region1Text.text = regionValue;
                break;
            case "region2":
                region2Text.text = regionValue;
                break;
            case "region3":
                region3Text.text = regionValue;
                break;
            case "region4":
                region4Text.text = regionValue;
                break;
            case "region5":
                region5Text.text = regionValue;
                break;
            case "region6":
                region6Text.text = regionValue;
                break;
            default:
                Debug.LogWarning("Unknown region name: " + regionName);
                break;
        }
    }

    async void RegionClicked(string regionName, string cardId)
    {
        bool sessionExists = await SessionExists();
        if (!sessionExists)
        {
            Debug.Log("Session does not exist in the database.");
            return;
        }

        var cardValueSnapshot = await dbRef.Child("players").Child(playerId).Child("deck").Child(cardId).Child("cardValue").GetValueAsync();
        if (cardValueSnapshot == null)
        {
            Debug.Log("Card value not found.");
            return;
        }
        int cardValue = int.Parse(cardValueSnapshot.Value.ToString());

        var supportSnapshot = await dbRef.Child("players").Child(playerId).Child("stats").Child("support").GetValueAsync();
        if (supportSnapshot == null)
        {
            Debug.Log("Support value not found.");
            return;
        }
        int currentSupport = int.Parse(supportSnapshot.Value.ToString());

        currentSupport += cardValue;
        await dbRef.Child("players").Child(playerId).Child("stats").Child("support").SetValueAsync(currentSupport);

        await ReloadMapValues(regionName, cardValue);

        // Zamkniêcie overlay po zakoñczeniu wszystkich operacji
        mapPanel.SetActive(false);
    }

    async Task ReloadMapValues(string regionName, int supportSubstractValue)
    {
        var snapshot = await dbRef.Child("map").Child(regionName).GetValueAsync();
        if (snapshot == null)
        {
            Debug.LogError("Error fetching supportOnMap value.");
            return;
        }

        int supportOnMap = int.Parse(snapshot.Value.ToString());
        supportOnMap -= supportSubstractValue;
        await dbRef.Child("map").Child(regionName).SetValueAsync(supportOnMap);
    }

    async Task<bool> SessionExists()
    {
        var sessionCheck = await dbRef.Parent.Parent.GetValueAsync();
        return sessionCheck.Exists;
    }

    public void SetCardIdMap(string cardId)
    {
        cardIdMap = cardId;
    }
}
