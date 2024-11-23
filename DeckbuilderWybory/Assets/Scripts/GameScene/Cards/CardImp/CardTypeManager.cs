using UnityEngine;

public class CardTypeManager : MonoBehaviour
{
    public AddRemoveCardImp addRemoveCardImp;
    public AsMuchAsCardImp asMuchAsCardImp;
    public CardCardImp cardCardImp;
    public OptionsCardImp optionsCardImp;
    public RandomCardImp randomCardImp;
    public UniqueCardImp uniqueCardImp;

    public void OnCardDropped(string instanceId, string cardIdDropped, bool ignoreCost)
    {
        string cardType = cardIdDropped.Substring(0, 2);

        switch (cardType)
        {
            case "AD":
                addRemoveCardImp.CardLibrary(instanceId,cardIdDropped, ignoreCost);
                break;
            case "AS":
                asMuchAsCardImp.CardLibrary(instanceId,cardIdDropped, ignoreCost);
                break;
            case "CA":
                cardCardImp.CardLibrary(instanceId,cardIdDropped, ignoreCost);
                break;
            case "OP":
                optionsCardImp.CardLibrary(instanceId,cardIdDropped, ignoreCost);
                break;
            case "RA":
                randomCardImp.CardLibrary(instanceId,cardIdDropped, ignoreCost);
                break;
            case "UN":
                uniqueCardImp.CardLibrary(instanceId,cardIdDropped, ignoreCost);
                break;
            default:
                Debug.LogError($"Unknown card type: {cardIdDropped}");
                break;

        }
    }


}
