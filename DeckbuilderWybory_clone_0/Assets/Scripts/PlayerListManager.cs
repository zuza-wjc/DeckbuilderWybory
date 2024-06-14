using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using System.Linq;

public class NewBehaviourScript : MonoBehaviour
{
    DatabaseReference dbRef;

    string lobbyId;
    string playerId;
    string playerName;
    int lobbySize;
    public GameObject playerListPanel;

    public GameObject buttonTemplate;
    public GameObject scrollViewContent;

    // Start is called before the first frame update
    void Start()
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

        FetchDataFromDatabase();
    }

    void FetchDataFromDatabase()
    {
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
                    // Pobierz playerId z klucza w g��wnym s�owniku players
                    //playerId = snapshot.Child("players").Children.First().Key;
                    playerId = DataTransfer.PlayerId;
                    Debug.Log("PlayerId: " + playerId);
                    playerName = snapshot.Child("players").Child(playerId).Child("playerName").Value.ToString();
                    int.TryParse(snapshot.Child("lobbySize").Value.ToString(), out lobbySize);

                    // Pobierz playerName dla ka�dego innego gracza ni� ty
                    foreach (var childSnapshot in snapshot.Child("players").Children)
                    {
                        string otherPlayerId = childSnapshot.Key;
                        if (otherPlayerId != playerId)
                        {
                            string otherPlayerName = childSnapshot.Child("playerName").Value.ToString();
                            // Utw�rz przycisk dla innego gracza
                            CreateButton(otherPlayerName);
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


    void CreateButton(string otherPlayerName)
    {
        GameObject button = Instantiate(buttonTemplate, scrollViewContent.transform);
        button.SetActive(true);
        button.GetComponentInChildren<UnityEngine.UI.Text>().text = otherPlayerName;
        // Mo�esz r�wnie� ustawi� atrybuty przycisku w zale�no�ci od potrzeb

        // Dodanie funkcji obs�ugi zdarzenia dla klikni�cia w przycisk
        button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => TaskOnClick(otherPlayerName));
    }

    void TaskOnClick(string otherPlayerName)
    {
        Debug.Log("Clicked on " + otherPlayerName);
        playerListPanel.SetActive(false);
        // Mo�esz doda� tutaj swoj� logik�, co ma si� sta� po klikni�ciu przycisku
    }
}
