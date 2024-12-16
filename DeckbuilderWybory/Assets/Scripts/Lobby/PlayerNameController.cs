using UnityEngine;
using UnityEngine.UI;

public class PlayerNameController : MonoBehaviour
{
    public InputField inputField;
    public Text errorMessage;
    public GameObject sectionToChange;
    public GameObject sectionFromChange;

    public void OnSubmit()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            if (errorMessage != null)
            {
                errorMessage.text = "Pole nie mo¿e byæ puste!";
            }

        }
        else
        {
            DataTransfer.PlayerName = inputField.text;

            if (errorMessage != null)
            {
                errorMessage.text = "";
            }

            if (sectionFromChange != null && sectionToChange != null)
            {
                sectionFromChange.SetActive(false);
                sectionToChange.SetActive(true);
            }
        }
    }
}
