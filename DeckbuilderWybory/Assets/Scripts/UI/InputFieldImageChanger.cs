using UnityEngine.UI;
using UnityEngine;

public class InputFieldImageChanger : MonoBehaviour
{
    private InputField inputField; // Referencja do InputField
    private Image inputFieldImage; // Referencja do obrazu InputField
    public Sprite fillImage; // Nowy obraz, który ma byæ ustawiony
    public Sprite nullImage;

    void Start()
    {
        inputField = GetComponent<InputField>();
        inputFieldImage = inputField.targetGraphic as Image;

        inputField.onValueChanged.AddListener(OnInputFieldChanged);
    }

    void OnInputFieldChanged(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            // Zmieñ obraz, jeœli InputField zawiera jakiœ tekst
            inputFieldImage.sprite = fillImage;
        }
        else
        {
            inputFieldImage.sprite = nullImage;
        }
    }
}
