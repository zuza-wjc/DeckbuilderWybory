using UnityEngine;

public class PassTurnPanelController : MonoBehaviour
{
    // Referencja do obiektu panelu PassTurnPanel
    [SerializeField] private GameObject passTurnPanel;

    // Funkcja aktywująca panel PassTurnPanel
    public void SetPassTurnPanelActive()
    {
        if (passTurnPanel != null)
        {
            passTurnPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("PassTurnPanel nie jest przypisany!");
        }
    }

    // Funkcja dezaktywująca panel PassTurnPanel
    public void SetPassTurnPanelInactive()
    {
        if (passTurnPanel != null)
        {
            passTurnPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("PassTurnPanel nie jest przypisany!");
        }
    }
}
