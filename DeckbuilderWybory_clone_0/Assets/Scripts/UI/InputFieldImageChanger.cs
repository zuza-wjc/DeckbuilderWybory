using UnityEngine.UI;
using UnityEngine;

public class InputFieldImageChanger : MonoBehaviour
{
    private InputField inputField;
    private Image inputFieldImage;
    public Sprite fillImage;
    public Sprite nullImage;

    void Start()
    {
        inputField = GetComponent<InputField>();
        inputFieldImage = inputField.targetGraphic as Image;

        inputField.shouldHideMobileInput = true;

        inputField.onValueChanged.AddListener(OnInputFieldChanged);
    }

    void OnInputFieldChanged(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            inputFieldImage.sprite = fillImage;
        }
        else
        {
            inputFieldImage.sprite = nullImage;
        }
    }
}
