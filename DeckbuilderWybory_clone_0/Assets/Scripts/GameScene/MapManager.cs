using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
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

    public delegate void MapManagerActionCompleted();
    public event MapManagerActionCompleted OnMapManagerActionCompleted;

    private Dictionary<string, string> playerNameToIdMap = new Dictionary<string, string>();

    public void FetchDataFromDatabase()
    {
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

        dbRef.GetValueAsync().ContinueWithOnMainThread(task => {
            if (task.IsFaulted)
            {
                Debug.LogError("Error getting data from Firebase: " + task.Exception);
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

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
            }
        });

        region1Button.onClick.AddListener(() => RegionClicked("region1", cardIdMap));
        region2Button.onClick.AddListener(() => RegionClicked("region2", cardIdMap));
        region3Button.onClick.AddListener(() => RegionClicked("region3", cardIdMap));
        region4Button.onClick.AddListener(() => RegionClicked("region4", cardIdMap));
        region5Button.onClick.AddListener(() => RegionClicked("region5", cardIdMap));
        region6Button.onClick.AddListener(() => RegionClicked("region6", cardIdMap));
    }

    void SetRegionValue(string regionName, string regionValue)
    {
        // Przypisz warto�� regionValue do odpowiedniego regionu na podstawie nazwy regionu
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

    void RegionClicked(string regionName, string cardId)
    {
        Task<int> getCardValueTask = dbRef.Child("players").Child(playerId).Child("deck").Child(cardId).Child("cardValue").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.Log("Error fetching support value: " + task.Exception);
                return 0;
            }

            DataSnapshot snapshot = task.Result;
            return int.Parse(snapshot.Value.ToString());
        });

        Task<int> getSupportTask = dbRef.Child("players").Child(playerId).Child("stats").Child("support").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.Log("Error fetching support value: " + task.Exception);
                return 0;
            }

            DataSnapshot snapshot = task.Result;
            return int.Parse(snapshot.Value.ToString());
        });

        // Poczekaj na zako�czenie obu operacji asynchronicznych
        Task.WhenAll(getCardValueTask, getSupportTask).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.Log("Error fetching support values: " + task.Exception);
                return;
            }

            // Pobierz wyniki z zako�czonych operacji
            supportAddValueReload = getCardValueTask.Result;
            support = getSupportTask.Result;

            // Zaktualizuj warto�� i wsad� do bazy danych
            support += supportAddValueReload;
            dbRef.Child("players").Child(playerId).Child("stats").Child("support").SetValueAsync(support);

            // Zaktualizuj warto�� na mapie
            reloadMapValues(regionName, supportAddValueReload);
        });

        if (OnMapManagerActionCompleted != null)
            OnMapManagerActionCompleted();
    }


    void reloadMapValues(string regionName, int supportSubstractValue)
    {
        // Pobierz suppoer po nazwie regionu
        dbRef.Child("map").Child(regionName).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                // Obs�u� b��d pobierania warto�ci
                Debug.LogError("Error fetching supportOnMap value: " + task.Exception);
                return;
            }

            // Pobierz warto�� z snapshotu
            DataSnapshot snapshot = task.Result;
            supportOnMap = int.Parse(snapshot.Value.ToString());

            // Odejmij supportSubstractValue
            supportOnMap -= supportSubstractValue;

            // Zapisz zaktualizowan� warto�� do bazy danych
            dbRef.Child("map").Child(regionName).SetValueAsync(supportOnMap).ContinueWith(updateTask =>
            {
                if (updateTask.IsFaulted)
                {
                    // Obs�u� b��d zapisu do bazy danych
                    Debug.LogError("Error updating supportOnMap value: " + updateTask.Exception);
                }
                else
                {
                    Debug.Log("SupportOnMap updated successfully.");
                }
            });
        });
    }


    public void SetCardIdMap(string cardId)
    {
        cardIdMap = cardId;
    }
}
