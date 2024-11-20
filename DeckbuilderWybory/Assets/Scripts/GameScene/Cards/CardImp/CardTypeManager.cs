using UnityEngine;

public class CardTypeManager : MonoBehaviour
{
    public AddRemoveCardImp addRemoveCardImp;
    public AsMuchAsCardImp asMuchAsCardImp;

    public void OnCardDropped(string cardIdDropped)
    {
        string cardType = cardIdDropped.Substring(0, 2);

        switch (cardType)
        {
            case "AD":
                addRemoveCardImp.CardLibrary(cardIdDropped);
                break;
            case "AS":
                asMuchAsCardImp.CardLibrary(cardIdDropped);
                break;

        }
    }


}
