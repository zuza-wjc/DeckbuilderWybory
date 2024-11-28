using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelControler : MonoBehaviour
{
    [System.Serializable]
    public class Panel
    {
        public string panelName; // Nazwa panelu
        public GameObject panelObject; // Obiekt panelu
    }

    public Panel[] panels; // Tablica paneli

    public void ShowPanel(string panelName)
    {
        HideAllPanels(); // Ukryj wszystkie panele

        // Szukaj panelu po nazwie i wyswietl go
        foreach (Panel panel in panels)
        {
            if (panel.panelName == panelName)
            {
                panel.panelObject.SetActive(true);
                break;
            }
        }
    }

    private void HideAllPanels()
    {
        foreach (Panel panel in panels)
        {
            panel.panelObject.SetActive(false);
        }
    }
}
