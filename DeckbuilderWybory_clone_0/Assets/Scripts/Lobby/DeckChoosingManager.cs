using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Database;
using System.Threading.Tasks;

public class DeckChoosingManager : MonoBehaviour
{
    public Button chooseAmbasadaButton;
    public Button chooseMetropoliaButton;
    public Button chooseSrodowiskoButton;
    public Button choosePrzemyslButton;
    public GameObject deckChoosingPanel;

    private DatabaseReference dbRef;
    private string lobbyId;
    private string playerId;

    // Start is called before the first frame update
    void Start()
    {
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;

        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");

        if (chooseAmbasadaButton != null)
        {
            chooseAmbasadaButton.onClick.AddListener(() => OnAmbasadaButtonClicked("ambasada"));
        }
        else
        {
            Debug.LogError("No button!");
        }
        if (chooseMetropoliaButton != null)
        {
            chooseMetropoliaButton.onClick.AddListener(() => OnMetropoliaButtonClicked("metropolia"));
        }
        else
        {
            Debug.LogError("No button!");
        }
        if (chooseSrodowiskoButton != null)
        {
            chooseSrodowiskoButton.onClick.AddListener(() => OnSrodowiskoButtonClicked("srodowisko"));
        }
        else
        {
            Debug.LogError("No button!");
        }
        if (choosePrzemyslButton != null)
        {
            choosePrzemyslButton.onClick.AddListener(() => OnPrzemyslButtonClicked("przemysl"));
        }
        else
        {
            Debug.LogError("No button!");
        }
    }

    private async void OnAmbasadaButtonClicked(string deckType)
    {
        await UpdatePlayerStats(deckType);
    }

    private async void OnMetropoliaButtonClicked(string deckType)
    {
        await UpdatePlayerStats(deckType);
    }

    private async void OnSrodowiskoButtonClicked(string deckType)
    {
        await UpdatePlayerStats(deckType);
    }

    private async void OnPrzemyslButtonClicked(string deckType)
    {
        await UpdatePlayerStats(deckType);
    }

    private async Task UpdatePlayerStats(string deckType)
    {
        try
        {
            if (dbRef == null)
            {
                Debug.LogError("Database reference is null!");
                return;
            }

            await dbRef.Child(playerId).Child("stats").Child("deckType").SetValueAsync(deckType);
            Debug.Log($"DeckType '{deckType}' set for player '{playerId}'.");

            // Wy³¹cz panel z przyciskami
            deckChoosingPanel.SetActive(false);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error updating player stats: {ex.Message}");
        }
    }


    void OnDestroy()
    {
        if (chooseAmbasadaButton != null)
        {
            chooseAmbasadaButton.onClick.RemoveAllListeners();
        }
        if (chooseMetropoliaButton != null)
        {
            chooseMetropoliaButton.onClick.RemoveAllListeners();
        }
        if (chooseSrodowiskoButton != null)
        {
            chooseSrodowiskoButton.onClick.RemoveAllListeners();
        }
        if (choosePrzemyslButton != null)
        {
            choosePrzemyslButton.onClick.RemoveAllListeners();
        }
    }
}
