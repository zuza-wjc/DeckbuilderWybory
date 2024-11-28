using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonImageChanger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Sprite defaultSprite; // Domyœlny sprite
    public Sprite pressedSprite; // Sprite po naciœniêciu

    private Image buttonImage;

    void Start()
    {
        buttonImage = GetComponent<Image>();
        buttonImage.sprite = defaultSprite;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Zmiana obrazu przy naciœniêciu
        buttonImage.sprite = pressedSprite;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Powrót do domyœlnego obrazu po zwolnieniu
        buttonImage.sprite = defaultSprite;
    }
}
