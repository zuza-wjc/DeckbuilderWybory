using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardSpriteManager", menuName = "Card Management/CardSpriteManager")]
public class CardSpriteManager : ScriptableObject
{
    [System.Serializable]
    public class CardSprite
    {
        public string cardId; 
        public Sprite cardSprite;
    }

    public List<CardSprite> cardSprites = new List<CardSprite>();

    public Sprite GetCardSprite(string cardId)
    {
        CardSprite card = cardSprites.Find(x => x.cardId == cardId);
        return card != null ? card.cardSprite : null;
    }
}
