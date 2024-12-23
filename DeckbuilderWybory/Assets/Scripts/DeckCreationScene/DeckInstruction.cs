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
        instructionText.text="Witamy w Twoich Taliach! Możesz tutaj stworzyć aż do 8 własnych talii. Jeśli chcesz DODAĆ TALIĘ kliknij przycisk plus. Jeśli chcesz którąś USUNĄĆ wystarczy że klikniesz trzy kropki w prawym górnym rogu talii którą chcesz usunąć. Jeśli chcesz ZMODYFIKOWAĆ talię kliknij w przycisk z nazwą talii którą chcesz zmienić.";
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
                instructionText.text="Witamy w Twoich Taliach! Możesz tutaj stworzyć aż do 8 własnych talii. Jeśli chcesz DODAĆ TALIĘ kliknij przycisk plus. Jeśli chcesz którąś USUNĄĆ wystarczy że klikniesz trzy kropki w prawym górnym rogu talii którą chcesz usunąć. Jeśli chcesz ZMODYFIKOWAĆ talię kliknij w przycisk z nazwą talii którą chcesz zmienić.";
                instructionTitle.text="Twoje Talie";
                pageNumber.text="1/6";
                pagePreviousButton.interactable=false;
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 2:
                instructionText.text="Każda talia musi mieć 30 kart: 20 kart PODSTAWOWYCH i 10 kart SPECJALNYCH. Są 4 typy kart specjalnych i możesz użyć tylko jednego typu w jednej talii.";
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
                instructionText.text="Aby DODAĆ karty do swojej talii wystarczy że klikniesz kartę którą chcesz dodać, a następnie wybierzesz ile chcesz mieć jej w talii. Każda karta ma własną wartość ile możesz maksymalnie dodać jej do talii.";
                instructionTitle.text="Dodawanie Kart";
                pageNumber.text="3/6";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 4:
                instructionText.text="Z lewej strony ekranu możesz zobaczyć LISTĘ KART które zostały dodane do tej pory. Zobaczysz tam liczbę reprezentującą liczbę sztuk tej karty w talii, jej nazwę oraz przez kolor możesz rozpoznać jakiego typu jest ta karta.";
                instructionTitle.text="Karty";
                pageNumber.text="4/6";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 5:
                instructionText.text="Gdy wybierzesz już 30 kart możesz zmienić NAZWĘ TALII klikając pole u góry ekranu po lewej, obok licznika kart w talii.";
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
