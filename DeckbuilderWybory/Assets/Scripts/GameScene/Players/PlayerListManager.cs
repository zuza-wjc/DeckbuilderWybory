using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using System.Linq;

public class PlayerListManager : MonoBehaviour
{
    DatabaseReference dbRef;

    string lobbyId;
    string playerId;
    string playerName;
    int lobbySize;
    public GameObject playerListPanel;

    public string cardIdOnEnemy;
    string enemyId;

    public GameObject buttonTemplate;
    public GameObject scrollViewContent;

    private Dictionary<string, string> playerNameToIdMap = new Dictionary<string, string>();

    // Start is called before the first frame update
    void Start()
    {
        // SprawdŸ, czy Firebase jest ju¿ zainicjalizowany
        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        lobbyId = DataTransfer.LobbyId;
        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId);

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
                    playerId = DataTransfer.PlayerId;
                    Debug.Log("PlayerId: " + playerId);
                    playerName = snapshot.Child("players").Child(playerId).Child("playerName").Value.ToString();
                    int.TryParse(snapshot.Child("lobbySize").Value.ToString(), out lobbySize);

                    // Pobierz playerName i playerId dla ka¿dego innego gracza ni¿ ty
                    foreach (var childSnapshot in snapshot.Child("players").Children)
                    {
                        string otherPlayerId = childSnapshot.Key;
                        if (otherPlayerId != playerId)
                        {
                            string otherPlayerName = childSnapshot.Child("playerName").Value.ToString();
                            playerNameToIdMap[otherPlayerName] = otherPlayerId;
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

        // Ustaw enemyId na podstawie mapy playerNameToIdMap
        if (playerNameToIdMap.TryGetValue(otherPlayerName, out string otherPlayerId))
        {
            enemyId = otherPlayerId;
        }
        else
        {
            Debug.LogError("PlayerId not found for the given playerName: " + otherPlayerName);
            return;
        }

        CardTypeOnEnemy cardTypeOnEnemy = FindObjectOfType<CardTypeOnEnemy>();
        if (cardTypeOnEnemy != null)
        {
            cardTypeOnEnemy.OnCardDropped(cardIdOnEnemy, enemyId);
        }
        else
        {
            Debug.LogError("CardTypeOnEnemy component not found in the scene!");
        }
        playerListPanel.SetActive(false);
    }

    public void SetCardIdOnEnemy(string cardId)
    {
        cardIdOnEnemy = cardId;
    }
}
