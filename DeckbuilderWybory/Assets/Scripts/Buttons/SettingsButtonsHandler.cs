using UnityEngine;
using UnityEngine.UI;

public class SettingsButtonsHandler : MonoBehaviour
{
    public Sprite onSprite;
    public Sprite offSprite;

    private Image buttonImage;

    private bool toggle = true;

    void Start()
    {
        buttonImage = GetComponent<Image>();
        buttonImage.sprite = onSprite;

        toggle = !DataTransfer.IsMuted;
        UpdateButtonSprite();
    }

    public void ToggleButton()
    {
        toggle = !toggle;

        DataTransfer.IsMuted = !toggle;

        UpdateButtonSprite();
    }

    private void UpdateButtonSprite()
    {
        buttonImage.sprite = toggle ? onSprite : offSprite;
    }
}
