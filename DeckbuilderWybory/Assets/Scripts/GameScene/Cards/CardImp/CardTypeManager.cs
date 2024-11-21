using UnityEngine;

public class CardTypeManager : MonoBehaviour
{
    public AddRemoveCardImp addRemoveCardImp;
    public AsMuchAsCardImp asMuchAsCardImp;
    public CardCardImp cardCardImp;
    public OptionsCardImp optionsCardImp;
    public RandomCardImp randomCardImp;

    public void OnCardDropped(string cardIdDropped, bool ignoreCost)
    {
        string cardType = cardIdDropped.Substring(0, 2);

        switch (cardType)
        {
            case "AD":
                addRemoveCardImp.CardLibrary(cardIdDropped, ignoreCost);
                break;
            case "AS":
                asMuchAsCardImp.CardLibrary(cardIdDropped, ignoreCost);
                break;
            case "CA":
                cardCardImp.CardLibrary(cardIdDropped, ignoreCost);
                break;
            case "OP":
                optionsCardImp.CardLibrary(cardIdDropped, ignoreCost);
                break;
            case "RA":
                randomCardImp.CardLibrary(cardIdDropped, ignoreCost);
                break;

        }
    }


}
