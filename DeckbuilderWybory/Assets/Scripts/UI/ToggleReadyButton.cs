using UnityEngine;
using UnityEngine.UI;
using System.Collections; // Dodajemy przestrzeñ nazw dla Coroutine

public class ToggleReadyButton : MonoBehaviour
{
    public Text buttonText; // Komponent tekstowy przypisany w Inspectorze
    public Button readyButton;

    public void ToggleText()
    {
        if (readyButton != null)
        {
            if (buttonText != null)
            {
                buttonText.text = buttonText.text == "NIEGOTOWY" ? "GOTOWY" : "NIEGOTOWY";
            }
            else
            {
                Debug.LogError("Komponent Text jest pusty!");
            }

            // Dezaktywuj przycisk na 1 sekundê
            StartCoroutine(DisableButtonForOneSecond());
        }
    }

    private IEnumerator DisableButtonForOneSecond()
    {
        // Dezaktywujemy przycisk
        readyButton.interactable = false;

        // Czekamy 1 sekundê
        yield return new WaitForSeconds(1f);

        // Ponownie aktywujemy przycisk
        readyButton.interactable = true;
    }
}