using UnityEngine;
using UnityEngine.UI;

public class Instruction : MonoBehaviour
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
        instructionText.text="Witamy w Instrukcji gry Talia Władzy. Rozgrywka uruchomi się automatycznie gdy wszyscy gracze w lobby będą obecni i zaznaczą że są gotowi poprzez wciśnięcie przycisku z lewej strony ekranu.";
        instructionTitle.text="Lobby";
        pageNumber.text="1/15";
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
        if (pageCount>=15)
            pageCount=15;
        else
            pageCount++;
        ChangeInstruction(pageCount);
    }

    public void ChangeInstruction(int pageCount)
    {
        switch (pageCount)
        {
            case 1:
                instructionText.text="Witamy w Instrukcji gry Talia Władzy. Jeśli chcesz zmodyfikować swoją talię, wejdź w TWOJE TALIE w MENU. W talii musisz mieć 10 kart Podstawowych i 15 kart Specjalnych wybranego typu.";
                instructionTitle.text="Twoje Talie";
                pageNumber.text="1/15";
                pagePreviousButton.interactable=false;
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 2:
                instructionText.text="Rozgrywka uruchomi się automatycznie gdy wszyscy gracze w lobby będą obecni i zaznaczą że są gotowi poprzez wciśnięcie przycisku z lewej strony ekranu.";
                instructionTitle.text="Lobby";
                pageNumber.text="2/15";
                pagePreviousButton.interactable=true;
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 3:
                instructionText.text="Po rozpoczęciu rozgrywki tak będzie wyglądał ekran. Twoim celem jest uzbieranie jak największej liczby poparcia zanim gra się skończy.";
                instructionTitle.text="Cel";
                pageNumber.text="3/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 4:
                instructionText.text="U góry, od lewej strony ekranu znajduje się kolejno CZAS do końca twojej tury i przycisk PAS, który klikasz jeśli chcesz skończyć swoją turę wcześniej. Zaraz pod tym możesz sprawdzić którego gracza jest obecnie TURA, a poniżej która jest obecnie RUNDA. Ostatnia runda zawsze będzie zapisana jako 10 (nawet po użyciu kart modyfikujących ich liczbę)";
                instructionTitle.text="Czas";
                pageNumber.text="4/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 5:
                instructionText.text="Pod licznikiem rund znajdziesz jakie EFEKTY kart obecnie na ciebie wpływają.";
                instructionTitle.text="Efekty";
                pageNumber.text="5/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 6:
                instructionText.text="Po lewej stronie ekranu, na dole, znajdziesz twoje obecne statystki. W lewym kole jest twój BUDŻET oraz na kolorowo twój DOCHÓD. Zagranie karty najczęściej kosztuje Ciebie budżet oraz w każdej rundzie dostajesz tyle budżetu ile wynosi Twój dochód. W prawym kole znajdziesz sumę twojego obecnego POPARCIA. Zaczynasz rozgrywkę z niskim poparciem.";
                instructionTitle.text="Statystyki Gracza";
                pageNumber.text="6/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 7:
                instructionText.text="Na dole po środku ekranu znajdziesz swoje KARTY NA RĘCE. Liczba kart na ręce dopełnia się do 4 po zakończeniu tury. Aby zagrać kartę musisz przeciągnąć ją na jasne pole w górnej części ekranu. W tym obszarze jest też wyświetlany TEKST FABULARNY kiedy jakikolwiek gracz zagra swoją kartę. Można zagrywać karty tylko w swojej turze.";
                instructionTitle.text="Karty";
                pageNumber.text="7/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                screenshot[16].gameObject.SetActive(true);

                return;
            case 8:
                instructionText.text="Żeby zobaczyć DETALE wybranej karty wystarczy ją kliknąć. W lewym górnym rogu jest ile wynosi KOSZT zagrania jej. Możesz też zdecydować się SPRZEDAĆ ją. Aby to zrobić kliknij przycisk dolara po lewej stronie karty, zobaczysz wtedy ile możesz dostać za sprzedanie jej. Aby wyjść z widoku karty kliknij gdziekolwiek na ekranie.";
                instructionTitle.text="Sprzedaż";
                pageNumber.text="8/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                screenshot[17].gameObject.SetActive(true);
                return;
            case 9:
                instructionText.text="Istnieją też karty specjalne charakterystyczne dla TYPU talii jaką grasz, możesz rozpoznać je po ramce innego koloru. Niektóre z nich mają pod opisem ich działania na kolorowo zapisany BONUS, który uaktywnia się gdy ta karta zostaje zagrana na region o tym samym typie co ta karta.";
                instructionTitle.text="Typy Kart";
                pageNumber.text="9/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 10:
                instructionText.text="Na dole ekranu z prawej strony możesz sprawdzić HISTORIĘ ostatnich zagranych. Przez kogo była zagrana oraz na kogo. Poniżej możesz sprawdzić ile i jakie karty zostały w Twojej TALII. Nie są one pokazane w kolejności w jakiej będziesz je otrzymywać. UWAGA: gdy skończą się karty w tali nie będziesz w stanie otrzymać ich więcej.";
                instructionTitle.text="Historia i Talia";
                pageNumber.text="10/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 11:
                instructionText.text="U góry ekranu po prawej znajdziesz 3 przyciski, kolejno od góry: Przycisk WYJŚCIA, który pozwoli Tobie wyjść z rozgrywki i zakończy ją dla innych graczy. Przycisk STATYSTYK MAPY oraz niżej przycisk STATYSTYK GRACZY.";
                instructionTitle.text="Statystyki";
                pageNumber.text="11/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 12:
                instructionText.text="W statystykach mapy zobaczysz 6 regionów. Kolor regionu wskazuje na jego TYP. Każdy region ma zapisane ile poparcia jest w nim już zajętego na ile można maksymalnie w nim zdobyć. Aby zobaczyć dokładniejsze statystyki regionu, kliknij w jego nazwę.";
                instructionTitle.text="Statystyki Mapy";
                pageNumber.text="12/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 13:
                instructionText.text="W detalach regionu możesz potwierdzić jaki to typ regionu oraz zobaczysz ile w nim poparcia ma każdy gracz, tak samo jak wykres przedstawiający to graficznie.  Możesz kliknąć gracza by zobaczyć jak ma rozłożone poparcie w innych regionach oraz ich typy za pomocą kolorów. Aby wyjść z detali kliknij gdziekolwiek poza wyświetlone okna.";
                instructionTitle.text="Detale Regionu";
                pageNumber.text="13/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                screenshot[18].gameObject.SetActive(true);
                return;
            case 14:
                instructionText.text="W statystykach graczy zobaczysz wszystkie wartości wszystkich graczy. Z lewej: Nazwa, Budżet, Przychód, Poparcie w całości, Poparcie w poszczególnych regionach. Z prawej: Używany typ talii, liczba kart wciąż w talii.";
                instructionTitle.text="Statystyki Graczy";
                pageNumber.text="14/15";
                pageNextButton.interactable=true;
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                screenshot[19].gameObject.SetActive(true);
                return;
            case 15:
                instructionText.text="Na koniec gry wygrywa osoba z największym poparciem. W wypadku remisu sprawdzana jest liczba regionów a w następnej kolejności wartość budżetu. Miłej Gry!";
                instructionTitle.text="Koniec Gry";
                pageNumber.text="15/15";
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
