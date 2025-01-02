using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveAndExitManager : MonoBehaviour
{
    public GameObject saveAndExitPanel;
    public AddCardsPanelController addCardsPanelController;

    public void OnLeave()
    {
        saveAndExitPanel.SetActive(true);
    }
    public void OnExit()
    {
        saveAndExitPanel.SetActive(false);
        SceneManager.LoadScene("Deck Building");
    }
    public void OnSave()
    {
        addCardsPanelController.SaveDeck();
        SceneManager.LoadScene("Deck Building");
    }
}
