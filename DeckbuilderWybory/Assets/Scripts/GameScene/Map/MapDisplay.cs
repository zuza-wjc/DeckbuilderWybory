using Firebase;
using Firebase.Database;
using UnityEngine;
using TMPro;
using System.Threading.Tasks;

public class MapDisplay : MonoBehaviour
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
}
