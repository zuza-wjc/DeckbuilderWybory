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
                    // Pobierz playerId z klucza w g³ównym s³owniku players
                    //playerId = snapshot.Child("players").Children.First().Key;
                    playerId = DataTransfer.PlayerId;
                    Debug.Log("PlayerId: " + playerId);
                    playerName = snapshot.Child("players").Child(playerId).Child("playerName").Value.ToString();
                    int.TryParse(snapshot.Child("lobbySize").Value.ToString(), out lobbySize);

                    // Pobierz playerName dla ka¿dego innego gracza ni¿ ty
                    foreach (var childSnapshot in snapshot.Child("players").Children)
                    {
                        string otherPlayerId = childSnapshot.Key;
                        if (otherPlayerId != playerId)
                        {
                            string otherPlayerName = childSnapshot.Child("playerName").Value.ToString();
                            // Utwórz przycisk dla innego gracza
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
        // Mo¿esz równie¿ ustawiæ atrybuty przycisku w zale¿noœci od potrzeb

        // Dodanie funkcji obs³ugi zdarzenia dla klikniêcia w przycisk
        button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => TaskOnClick(otherPlayerName));
    }

    void TaskOnClick(string otherPlayerName)
    {
        Debug.Log("Clicked on " + otherPlayerName);
        playerListPanel.SetActive(false);
        // Mo¿esz dodaæ tutaj swoj¹ logikê, co ma siê staæ po klikniêciu przycisku
    }
}
