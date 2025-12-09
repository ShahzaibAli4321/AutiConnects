using UnityEngine;
using UnityEngine.UI;

// A small script to attach to each clickable option image.
// It stores the name of the object currently being displayed.
public class ImageIdentity : MonoBehaviour
{
    [HideInInspector] public string itemName;

    private Image optionImage;

    void Awake()
    {
        optionImage = GetComponent<Image>();
    }

    // Public method for the GameManager to set the new sprite and item name.
    public void SetContent(Sprite newSprite, string newName)
    {
        optionImage.sprite = newSprite;
        itemName = newName;
    }
}