using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class MapManager : MonoBehaviour
{
    DatabaseReference dbRef;

    string lobbyId;
    string playerId;
    string playerName;
    int lobbySize;
    public GameObject mapPanel;

    public string cardIdMap;
 
    private Dictionary<string, string> playerNameToIdMap = new Dictionary<string, string>();

    void Start()
    {
        // Przeszukaj przyciski w scenie i wypisz ich tagi w logach
        Button[] buttons = FindObjectsOfType<Button>();
        foreach (Button button in buttons)
        {
            if (button.tag != "Untagged")
            {
                Debug.Log("Button tag: " + button.tag);
            }
        }
    }

    public void FetchDataFromDatabase()
    {
        // SprawdŸ, czy Firebase jest ju¿ zainicjalizowany
        if (FirebaseApp.DefaultInstance == null)
        {
            // Jeœli nie, inicjalizuj Firebase
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

                    // Odczytaj wartoœci regionów dla aktualnego lobby
                    foreach (var childSnapshot in snapshot.Children)
                    {
                        if (childSnapshot.Key == "map") // Zak³adaj¹c, ¿e dane regionów s¹ w ga³êzi "map"
                        {
                            foreach (var regionSnapshot in childSnapshot.Children)
                            {
                                string regionName = regionSnapshot.Key;
                                string regionValue = regionSnapshot.Value.ToString();
                                Debug.Log("Region: " + regionName + ", Value: " + regionValue);
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
    }

    public void SetCardIdMap(string cardId)
    {
        cardIdMap = cardId;
    }
}
