using UnityEngine;
using UnityEngine.UI;

public class DeckInstruction : MonoBehaviour
{
    int pageCount=1;

    public Button pageNextButton;
    public Button pagePreviousButton;
    public Text instructionText;
    public Text instructionTitle;
    public Text pageNumber;
    public Image[] screenshot;
    public GameObject screenshotsPanel;

    public ScrollRect scrollRect;

    public Button backToMenu;


    void Start()
    {
        instructionText.text="Witamy w Twoich Taliach! Możesz tu stworzyć aż do 8 własnych talii, które zostaną zapisane na Twoim urządzeniu. Dzięki temu możesz korzystać z nich wielokrotnie, dopóki masz zainstalowaną naszą aplikację. Jeśli chcesz DODAĆ TALIĘ kliknij przycisk plus. Jeśli chcesz USUNĄĆ jedną z nich wystarczy że klikniesz przycisk w prawym górnym rogu danej talii. Jeśli chcesz ZMODYFIKOWAĆ talię kliknij przycisk z nazwą talii którą chcesz zmienić.";
        instructionTitle.text="Twoje Talie";
        pageNumber.text="1/6";
        pagePreviousButton.interactable=false;

        foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
        screenshot[pageCount-1].gameObject.SetActive(true);

        pagePreviousButton.onClick.AddListener(PreviousPage);
        pageNextButton.onClick.AddListener(NextPage);

    }

    public void PreviousPage()
    {
        if (pageCount<=1)
            pageCount=1;
        else
            pageCount--;
        ChangeInstruction(pageCount);
    }

    public void NextPage()
    {
        if (pageCount>=6)
            pageCount=6;
        else
            pageCount++;
        ChangeInstruction(pageCount);
    }

    public void ChangeInstruction(int pageCount)
    {
        switch (pageCount)
        {
            case 1:
                instructionText.text= "Witamy w Twoich Taliach! Możesz tu stworzyć aż do 8 własnych talii, które zostaną zapisane na Twoim urządzeniu. Dzięki temu możesz korzystać z nich wielokrotnie, dopóki masz zainstalowaną naszą aplikację. Jeśli chcesz DODAĆ TALIĘ kliknij przycisk plus. Jeśli chcesz USUNĄĆ jedną z nich wystarczy że klikniesz przycisk w prawym górnym rogu danej talii. Jeśli chcesz ZMODYFIKOWAĆ talię kliknij przycisk z nazwą talii którą chcesz zmienić.";
                instructionTitle.text="Twoje Talie";
                pageNumber.text="1/6";
                pagePreviousButton.interactable=false;
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 2:
                instructionText.text="Każda talia może mieć maksymalnie 30 kart: 20 kart PODSTAWOWYCH i 10 kart SPECJALNYCH. Rozróżniamy 4 typy kart specjalnych: AMBASADA - kolor fioletowy, METROPOLIA - kolor czerwony, ŚRODOWISKO - kolor zielony oeaz PRZEMYSŁ - kolor żółty. Każdy typ różni się kartami jakie możesz dodać do talii, możesz używać tylko jednego typu w jednej talii.";
                instructionTitle.text="Liczba Kart";
                pageNumber.text="2/6";
                pagePreviousButton.interactable=true;
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                screenshot[6].gameObject.SetActive(true);
                screenshot[7].gameObject.SetActive(true);
                screenshot[8].gameObject.SetActive(true);
                return;
            case 3:
                instructionText.text="Aby DODAĆ karty do swojej talii wystarczy że klikniesz kartę którą chcesz dodać, a następnie zmienisz licznik ilości kart w talii. Tylko niektóre kart można dodać w ilości większej niż 1.";
                instructionTitle.text="Dodawanie Kart";
                pageNumber.text="3/6";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 4:
                instructionText.text="Z lewej strony ekranu możesz zobaczyć LISTĘ KART które zostały dodane do tej pory. Zobaczysz tam liczbę sztuk tej karty w talii, jej nazwę oraz kolor reprezentujący jakiego typu jest ta karta.";
                instructionTitle.text="Karty";
                pageNumber.text="4/6";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 5:
                instructionText.text="W każdej chwili możesz zmienić NAZWĘ TALII klikając pole u góry ekranu, obok licznika kart w talii.";
                instructionTitle.text="Nazwa Talii";
                pageNumber.text="5/6";
                pageNextButton.interactable=true;
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 6:
                instructionText.text="Aby zapisać talię kliknij ZAPISZ. Teraz możesz wyjść i zacząć rozgrywkę.";
                instructionTitle.text="Koniec";
                pageNumber.text="6/6";
                pageNextButton.interactable=false;
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;

            default:
                Debug.Log("Outside of correct values");
                return;

        }

    }



    public void ScrollToTop()
    {
        if (scrollRect != null)
        {
            // Ustaw pozycję scrolla na górę
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }



}
