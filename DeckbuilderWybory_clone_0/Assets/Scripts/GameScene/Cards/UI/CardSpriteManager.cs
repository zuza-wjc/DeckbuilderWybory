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

    private Dictionary<string, Sprite> cardSpritesDictionary = new Dictionary<string, Sprite>();

    public List<CardSprite> cardSprites = new List<CardSprite>();

    private void OnEnable()
    {
        cardSpritesDictionary.Clear();
        foreach (var card in cardSprites)
        {
            if (!cardSpritesDictionary.ContainsKey(card.cardId))
            {
                cardSpritesDictionary.Add(card.cardId, card.cardSprite);
            }
            else
            {
                Debug.LogWarning($"Duplicate cardId found: {card.cardId}");
            }
        }
    }

    public Sprite GetCardSprite(string cardId)
    {
        if (cardSpritesDictionary.TryGetValue(cardId, out Sprite cardSprite))
        {
            return cardSprite;
        }
        else
        {
            Debug.LogError($"Card sprite for cardId '{cardId}' not found.");
            return null;
        }
    }

}
