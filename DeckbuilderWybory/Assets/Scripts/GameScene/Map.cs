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
    int lobbySize;
    public GameObject mapPanel;

    public TextMeshProUGUI region1Text;
    public TextMeshProUGUI region2Text;
    public TextMeshProUGUI region3Text;
    public TextMeshProUGUI region4Text;
    public TextMeshProUGUI region5Text;
    public TextMeshProUGUI region6Text;


    private Dictionary<string, string> playerNameToIdMap = new Dictionary<string, string>();

    public void Start()
    {

        Debug.LogError("I'm alive !");
        // Sprawd�, czy Firebase jest ju� zainicjalizowany
        if (FirebaseApp.DefaultInstance == null)
        {
            // Je�li nie, inicjalizuj Firebase
            FirebaseInitializer firebaseInitializer = FindObjectOfType<FirebaseInitializer>();
            if (firebaseInitializer == null)
            {
                Debug.LogError("FirebaseInitializer not found in the scene!");
                return;
            }
        }

        lobbyId = DataTransfer.LobbyId;

        dbRef = FirebaseDatabase.DefaultInstance.RootReference.Child("sessions").Child(lobbyId);

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
                                    string regionValue = regionSnapshot.Value.ToString();

                                    Debug.Log("regionValue = " + regionValue);

                                    // Ustaw warto�� regionu na podstawie tagu
                                    SetRegionValue(regionName, regionValue);
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


    }


    void SetRegionValue(string regionName, string regionValue)
    {
        // Przypisz warto�� regionValue do odpowiedniego regionu na podstawie nazwy regionu
        switch (regionName)
        {
            case "region1":
                region1Text.text = regionValue + "/15";
                break;
            case "region2":
                region2Text.text = regionValue + "/19";
                break;
            case "region3":
                region3Text.text = regionValue + "/16";
                break;
            case "region4":
                region4Text.text = regionValue + "/18";
                break;
            case "region5":
                region5Text.text = regionValue + "/16";
                break;
            case "region6":
                region6Text.text = regionValue + "/16";
                break;
            default:
                Debug.LogWarning("Unknown region name: " + regionName);
                break;
        }
    }


    async Task<bool> SessionExists()
    {
        var sessionCheck = await dbRef.Parent.Parent.GetValueAsync();
        return sessionCheck.Exists;
    }


}
