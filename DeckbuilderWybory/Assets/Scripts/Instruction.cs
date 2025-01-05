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
        instructionText.text="Witamy w instrukcji gry Talia Władzy. Jeśli chcesz zmodyfikować swoje talie, wejdź w TWOJE TALIE w MENU. W talii musisz mieć 20 kart Podstawowych i 10 kart Specjalnych jednego wybranego typu.";
        instructionTitle.text="Twoje Talie";
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
                instructionText.text="Witamy w instrukcji gry Talia Władzy. Jeśli chcesz zmodyfikować swoje talie, wejdź w TWOJE TALIE w MENU. W talii musisz mieć 20 kart Podstawowych i 10 kart Specjalnych jednego wybranego typu.";
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
                instructionText.text="U góry, po lewej stronie ekranu znajduje się kolejno CZAS do końca twojej tury i przycisk PAS, który klikasz jeśli chcesz zakończyć swoją turę wcześniej. Zaraz pod tym możesz sprawdzić który gracz rozgrywa obecnie swoją TURĘ, a poniżej która jest obecnie RUNDA. 10 runda jest zawsze ostatnią, nawet po użyciu kart modyfikujących ich liczbę";
                instructionTitle.text="Czas";
                pageNumber.text="4/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 5:
                instructionText.text="Pod licznikiem rund i nad statystykami gracza znajdziesz jakie EFEKTY kart obecnie na Ciebie wpływają. Jeśli nie jesteś pewien co dany symbol oznacza, możesz nacisnąć dany efekt lub wejść w historię kart.";
                instructionTitle.text="Efekty";
                pageNumber.text="5/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                screenshot[15].gameObject.SetActive(true);
                screenshot[16].gameObject.SetActive(true);
                return;
            case 6:
                instructionText.text="Po lewej stronie ekranu, na dole, znajdziesz Twoje obecne statystki. W lewym kole znajdziesz Twój BUDŻET wraz z DOCHODEM, który dostjesz raz na rundę. Budżet wykorzystywany jest do zagrywania kart. W prawym kole znajdziesz sumę Twojego obecnego POPARCIA. Rozgrywkę zaczynasz z poparciem wynoszącym 8% rozdzielonym na losowe regiony.";
                instructionTitle.text="Statystyki Gracza";
                pageNumber.text="6/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                screenshot[17].gameObject.SetActive(true);
                return;
            case 7:
                instructionText.text="Na dole po środku ekranu znajdziesz swoje KARTY NA RĘCE. Liczba kart na ręce dopełnia się do 4 po zakończeniu tury. Aby zagrać kartę musisz przeciągnąć ją na jasne pole w górnej części ekranu. W tym obszarze jest również wyświetlany TEKST FABULARNY w momencie zagrania karty przez dowolnego gracz. Karty można zagrywać karty tylko w swojej turze.";
                instructionTitle.text="Karty";
                pageNumber.text="7/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                screenshot[18].gameObject.SetActive(true);

                return;
            case 8:
                instructionText.text="Żeby zobaczyć szczegóły wybranej karty wystarczy ją kliknąć. W lewym górnym rogu dowiesz się ile wynosi KOSZT zagrania danej karty. Możesz też zdecydować się ją SPRZEDAĆ, aby to zrobić kliknij przycisk dolara po lewej stronie karty, zobaczysz wtedy ile możesz dostać za sprzedanie jej. Aby wyjść z widoku karty kliknij dowolne miejsce na ekranie.";
                instructionTitle.text="Sprzedaż";
                pageNumber.text="8/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                screenshot[19].gameObject.SetActive(true);
                return;
            case 9:
                instructionText.text="Istnieją 4 typy kart specjalnych: AMBASADA, METROPOLIA, ŚRODOWISKO i PRZEMYSŁ, możesz rozpoznać je po ramce innego koloru. Niektóre z tych kart mają pod opisem działania zapisany BONUS w tym samym kolorze co ramka karty. Bonus uaktywnia się gdy karta zostaje zagrana na region o tym samym typie co karta specjalna.";
                instructionTitle.text="Typy Kart";
                pageNumber.text="9/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                screenshot[20].gameObject.SetActive(true);
                screenshot[21].gameObject.SetActive(true);
                screenshot[22].gameObject.SetActive(true);
                screenshot[23].gameObject.SetActive(true);
                screenshot[24].gameObject.SetActive(true);
                return;
            case 10:
                instructionText.text="Na dole ekranu, z prawej strony możesz sprawdzić HISTORIĘ ostatnich zagranych kart, przez kogo została zagrana oraz na kogo. Poniżej możesz sprawdzić ile i jakie karty pozostały w Twojej TALII. Nie są one pokazane w kolejności w jakiej będziesz je otrzymywać. UWAGA: gdy skończą się karty w talii nie będziesz ich więcej otrzymywać.";
                instructionTitle.text="Historia i Talia";
                pageNumber.text="10/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 11:
                instructionText.text="U góry ekranu po prawej znajdziesz 3 przyciski, kolejno od góry: Przycisk WYJŚCIA, który pozwoli Ci wyjść z rozgrywki i zakończy ją dla innych graczy. Przycisk STATYSTYK MAPY oraz poniżej przycisk STATYSTYK GRACZY.";
                instructionTitle.text="Statystyki";
                pageNumber.text="11/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                screenshot[25].gameObject.SetActive(true);
                screenshot[26].gameObject.SetActive(true);
                return;
            case 12:
                instructionText.text="W statystykach mapy zobaczysz 6 regionów. Kolor regionu wskazuje na jego TYP, są to te same typy co kart specjalnych. Każdy region ma zapisane ile poparcia jest w nim już zajęte oraz ile można maksymalnie go w nim zdobyć. Aby zobaczyć dokładniejsze statystyki regionu, kliknij na jego nazwę.";
                instructionTitle.text="Statystyki Mapy";
                pageNumber.text="12/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                return;
            case 13:
                instructionText.text="W szczegółach regionu możesz potwierdzić jaki to typ, ile poparcia ma w nim każdy gracz oraz wykres przedstawiający to graficznie. Możesz kliknąć nazwę gracza by zobaczyć jak ma rozłożone poparcie w innych regionach oraz ich typy rozróżniane za pomocą kolorów. Aby wyjść ze szczegółów kliknij poza wyświetlone okna.";
                instructionTitle.text="Detale Regionu";
                pageNumber.text="13/15";
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                screenshot[27].gameObject.SetActive(true);
                return;
            case 14:
                instructionText.text="W statystykach graczy zobaczysz wszystkie wartości pozostałych graczy. Z lewej: nazwa, budżet, przychód, suma poparcia, poparcie w poszczególnych regionach. Z prawej: typ używanej talii, liczba pozostałych kart w talii.";
                instructionTitle.text="Statystyki Graczy";
                pageNumber.text="14/15";
                pageNextButton.interactable=true;
                ScrollToTop();
                foreach (Transform child in screenshotsPanel.transform) child.gameObject.SetActive(false);
                screenshot[pageCount-1].gameObject.SetActive(true);
                screenshot[28].gameObject.SetActive(true);
                return;
            case 15:
                instructionText.text="Po 10 rundach gre wygrywa gracz z największym poparciem. W przypadku remisu sprawdzana jest liczba regionów w jakich gracz ma niezerowe poparcie, a w następnej kolejności wartość budżetu. Miłej Gry!";
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
