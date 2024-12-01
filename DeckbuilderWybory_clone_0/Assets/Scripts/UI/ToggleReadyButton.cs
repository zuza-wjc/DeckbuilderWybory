using UnityEngine;
using UnityEngine.UI;

public class ToggleReadyButton : MonoBehaviour
{
    public Text buttonText; // Komponent tekstowy przypisany w Inspectorze

    public void ToggleText()
    {
        if (buttonText != null)
        {
            buttonText.text = buttonText.text == "NIEGOTOWY" ? "GOTOWY" : "NIEGOTOWY";
        }
        else
        {
            Debug.LogError("Komponent Text jest pusty!");
        }
    }
}
