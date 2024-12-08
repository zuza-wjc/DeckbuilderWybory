using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;

public class DeckViewController : MonoBehaviour
{
    public Button viewAmbasadaButton;
    public Button viewMetropoliaButton;
    public Button viewSrodowiskoButton;
    public Button viewPrzemyslButton;

    public DeckViewManager deckViewManager;
    private DatabaseReference dbRef;

    // Start is called before the first frame update
    void Start()
    {
        dbRef = FirebaseInitializer.DatabaseReference
            .Child("readyDecks");

        if (viewAmbasadaButton != null)
        {
            viewAmbasadaButton.onClick.AddListener(() => OnViewAmbasadaButtonClicked("ambasada"));
        }
        else
        {
            Debug.LogError("No cards!");
        }
        if (viewMetropoliaButton != null)
        {
            viewMetropoliaButton.onClick.AddListener(() => OnViewMetropoliaButtonClicked("metropolia"));
        }
        else
        {
            Debug.LogError("No cards!");
        }
        if (viewSrodowiskoButton != null)
        {
            viewSrodowiskoButton.onClick.AddListener(() => OnViewSrodowiskoButtonClicked("srodowisko"));
        }
        else
        {
            Debug.LogError("No cards!");
        }
        if (viewPrzemyslButton != null)
        {
            viewPrzemyslButton.onClick.AddListener(() => OnViewPrzemyslButtonClicked("przemysl"));
        }
        else
        {
            Debug.LogError("No cards!");
        }
    }

    private async void OnViewAmbasadaButtonClicked(string deckType)
    {
        if (deckViewManager != null)
        {
            await deckViewManager.ShowDeckCardsForViewing(deckType);
        }
        else
        {
            Debug.LogError("CardSelectionUI reference is missing!");
        }
    }

    private async void OnViewMetropoliaButtonClicked(string deckType)
    {
        if (deckViewManager != null)
        {
            await deckViewManager.ShowDeckCardsForViewing(deckType);
        }
        else
        {
            Debug.LogError("CardSelectionUI reference is missing!");
        }
    }

    private async void OnViewSrodowiskoButtonClicked(string deckType)
    {
        if (deckViewManager != null)
        {
            await deckViewManager.ShowDeckCardsForViewing(deckType);
        }
        else
        {
            Debug.LogError("CardSelectionUI reference is missing!");
        }
    }

    private async void OnViewPrzemyslButtonClicked(string deckType)
    {
        if (deckViewManager != null)
        {
            await deckViewManager.ShowDeckCardsForViewing(deckType);
        }
        else
        {
            Debug.LogError("CardSelectionUI reference is missing!");
        }
    }

    void OnDestroy()
    {
        if (viewAmbasadaButton != null)
        {
            viewAmbasadaButton.onClick.RemoveAllListeners();
        }
        if (viewMetropoliaButton != null)
        {
            viewMetropoliaButton.onClick.RemoveAllListeners();
        }
        if (viewSrodowiskoButton != null)
        {
            viewSrodowiskoButton.onClick.RemoveAllListeners();
        }
        if (viewPrzemyslButton != null)
        {
            viewPrzemyslButton.onClick.RemoveAllListeners();
        }
    }
}
