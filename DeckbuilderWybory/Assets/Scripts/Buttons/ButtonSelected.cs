using UnityEngine;
using UnityEngine.UI;

public class ButtonSelected : MonoBehaviour
{
    public Button buttonA; // Pierwszy przycisk
    public Button buttonB; // Drugi przycisk
    public Sprite spriteA; // Sprite, który ma byæ ustawiony na przycisku A
    public Sprite spriteB; // Sprite, który ma byæ ustawiony na przycisku B

    private Image imageA; // Komponent Image przycisku A
    private Image imageB; // Komponent Image przycisku B

    void Start()
    {
        // Pobieramy komponenty Image przycisków
        imageA = buttonA.GetComponent<Image>();
        imageB = buttonB.GetComponent<Image>();

        // Dodajemy nas³uchiwacze na przyciski
        buttonA.onClick.AddListener(OnButtonAClicked);
        buttonB.onClick.AddListener(OnButtonBClicked);
    }

    void OnButtonAClicked()
    {
        Debug.Log("Przycisk A klikniêty!");

        // Zmiana sprite'ów: A -> B, B -> A
        imageA.sprite = spriteB;
        imageB.sprite = spriteA;
    }

    void OnButtonBClicked()
    {
        Debug.Log("Przycisk B klikniêty!");

        // Zmiana sprite'ów: A -> B, B -> A
        imageA.sprite = spriteA;
        imageB.sprite = spriteB;
    }
}
