using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Database;
using System.Threading.Tasks;

public class DeckChoosingManager : MonoBehaviour
{

    public GameObject chooseDeckPanel;
    public GameObject buttonPrefab;
    public Transform contentPanel;

    public Button defaultButton;
    public Button chooseAmbasadaButton;
    public Button chooseMetropoliaButton;
    public Button chooseSrodowiskoButton;
    public Button choosePrzemyslButton;
    public Text deckNameText;
    public GameObject defaultDeckChoosingPanel;

    private DatabaseReference dbRef;
    private string lobbyId;
    private string playerId;


    void Start()
    {
        lobbyId = DataTransfer.LobbyId;
        playerId = DataTransfer.PlayerId;
        // Pobierz listę nazw talii z PlayerPrefs
        List<string> deckNames = LoadDeckNames();
        CreateDeckIcons(deckNames);


        if (FirebaseApp.DefaultInstance == null || FirebaseInitializer.DatabaseReference == null)
        {
            Debug.LogError("Firebase is not initialized properly!");
            return;
        }

        dbRef = FirebaseInitializer.DatabaseReference.Child("sessions").Child(lobbyId).Child("players");


        //CreateButtons();

        defaultButton.onClick.AddListener(DefaultDeck);

    }
    public void ShowChoosingPanel()
    {
        chooseDeckPanel.SetActive(true);
    }
    public void BackToCustomDecks()
    {
        defaultDeckChoosingPanel.SetActive(false);
        chooseDeckPanel.SetActive(true);
    }
    public void CreateDeckIcons(List<string> deckNames)
    {
        foreach (string deckName in deckNames)
        {
            // Tworzymy obiekt prefabrykatu i przypisujemy go jako dziecko obiektu Panel
            GameObject icon = Instantiate(buttonPrefab, contentPanel);

            // Ustawiamy nazwę obiektu
            icon.name = deckName;

            // Znajdujemy komponent tekstowy dziecka i ustawiamy jego tekst na nazwę talii
            Text deckNameText = icon.GetComponentInChildren<Text>();
            if (deckNameText != null)
            {
                deckNameText.text = deckName;
            }

            // Znajdujemy komponent Button i przypisujemy zdarzenie kliknięcia
            icon.GetComponentInChildren<Text>().text = deckName;

            icon.GetComponent<Button>().onClick.AddListener(async () => await ChooseNondefaultDeck(deckName));
        }
    }
    
    private List<string> LoadDeckNames()
    {
        string decksJson = PlayerPrefs.GetString("decks", "{\"items\":[]}");

        // Deserializujemy JSON do obiektu ListWrapper
        ListWrapper listWrapper = JsonUtility.FromJson<ListWrapper>(decksJson);

        // Zwracamy listę decków
        return listWrapper.items;
    }

    // Klasa pomocnicza do konwersji List<string> na JSON
    [System.Serializable]
    public class ListWrapper
    {
        public List<string> items; // Lista decków
    }


    private async Task ChooseNondefaultDeck(string buttonName)
    {
        try
        {
            if (dbRef == null)
            {
                Debug.LogError("Database reference is null!");
                return;
            }

            await dbRef.Child(playerId).Child("stats").Child("deckType").SetValueAsync(buttonName);
            await dbRef.Child(playerId).Child("stats").Child("defaultDeckType").SetValueAsync(false);

            deckNameText.text = "Twoja talia: " + buttonName;
            Debug.Log($"DeckType '{buttonName}' set for player '{playerId}'.");

            chooseDeckPanel.SetActive(false);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error updating player stats: {ex.Message}");
        }
    }


    void DefaultDeck()
    {
        defaultDeckChoosingPanel.SetActive(true);
        chooseDeckPanel.SetActive(false);

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

            await dbRef.Child(playerId).Child("stats").Child("defaultDeckType").SetValueAsync(true);
            await dbRef.Child(playerId).Child("stats").Child("deckType").SetValueAsync(deckType);
            deckNameText.text = "Twoja talia: " + deckType;
            Debug.Log($"DeckType '{deckType}' set for player '{playerId}'.");

            // Wy��cz panel z przyciskami
            defaultDeckChoosingPanel.SetActive(false);
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
