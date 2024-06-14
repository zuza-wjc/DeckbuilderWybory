using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonImageChanger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Sprite defaultSprite; // Domy�lny sprite
    public Sprite pressedSprite; // Sprite po naci�ni�ciu

    private Image buttonImage;

    void Start()
    {
        buttonImage = GetComponent<Image>();
        buttonImage.sprite = defaultSprite;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Zmiana obrazu przy naci�ni�ciu
        buttonImage.sprite = pressedSprite;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Powr�t do domy�lnego obrazu po zwolnieniu
        buttonImage.sprite = defaultSprite;
    }
}
