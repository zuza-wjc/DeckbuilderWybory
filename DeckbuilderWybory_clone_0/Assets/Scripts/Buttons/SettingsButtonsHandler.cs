using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsButtonsHandler : MonoBehaviour
{
    public Sprite onSprite; // Domy�lny sprite
    public Sprite offSprite; // Sprite po naci�ni�ciu

    private Image buttonImage;

    private bool toggle = true;
    void Start()
    {
        buttonImage = GetComponent<Image>();
        buttonImage.sprite = onSprite;
    }

    public void toggleButton()
    {
        if (toggle)
        {
            buttonImage.sprite = offSprite;
            toggle = false;
        }
        else
        {
            buttonImage.sprite = onSprite;
            toggle = true;
        }
    }

}
