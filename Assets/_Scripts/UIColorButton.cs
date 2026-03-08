using UnityEngine;
using UnityEngine.UI;

public class UIColorButton : MonoBehaviour
{
    [Header("Color Settings")]
    public Color buttonColor = Color.black;

    [Header("Drawing Board")]
    public UIDrawingBoard drawingBoard;

    Image buttonImage;

    void Start()
    {
        buttonImage = GetComponent<Image>();

        // Show the color on the button
        if (buttonImage != null)
            buttonImage.color = buttonColor;
    }

    public void SelectColor()
    {
        if (drawingBoard != null)
            drawingBoard.SetLineColor(buttonColor);
    }
}