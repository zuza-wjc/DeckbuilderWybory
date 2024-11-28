using UnityEngine;
using UnityEngine.UI;

public class CardPanelOpener : MonoBehaviour
{
    public GameObject cardPanel;
    public Image panelImage;
    public Button closeButton;

    private void Start()
    {
        if (cardPanel != null)
        {
            cardPanel.SetActive(false);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OpenPanel);
        }
    }

    private void OpenPanel()
    {
        if (cardPanel != null && panelImage != null)
        {
            panelImage.sprite = GetComponentInParent<Image>().sprite;
            cardPanel.SetActive(true);
        }
    }

    private void ClosePanel()
    {
        if (cardPanel != null)
        {
            cardPanel.SetActive(false);
        }
    }
}
