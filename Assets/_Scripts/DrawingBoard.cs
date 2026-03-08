using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIDrawingBoard : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Line Settings")]
    public LineRenderer linePrefab;

    [Range(1f, 20f)]
    public float lineWidth = 6f;

    public Color lineColor = Color.black;

    [Header("Runtime Controls")]
    public Slider widthSlider;

    [Header("Render Settings")]
    public int baseSortingOrder = 10;

    RectTransform rectTransform;
    Camera uiCamera;

    LineRenderer currentLine;
    List<Vector3> points = new List<Vector3>();

    // List to store all drawn lines
    List<LineRenderer> lines = new List<LineRenderer>();

    int lineCounter = 0;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            uiCamera = null;
        else
            uiCamera = canvas.worldCamera;
    }

    void Start()
    {
        if (widthSlider != null)
        {
            widthSlider.minValue = 1f;
            widthSlider.maxValue = 20f;
            widthSlider.value = lineWidth;

            widthSlider.onValueChanged.AddListener(SetLineWidth);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, eventData.position, uiCamera))
            return;

        StartLine();
        AddPoint(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, eventData.position, uiCamera))
            return;

        AddPoint(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        currentLine = null;
    }

    void StartLine()
    {
        currentLine = Instantiate(linePrefab, transform);

        // Ensure new line renders above previous ones
        currentLine.sortingOrder = baseSortingOrder + lineCounter;
        lineCounter++;

        currentLine.transform.SetAsLastSibling();

        currentLine.startWidth = lineWidth;
        currentLine.endWidth = lineWidth;

        currentLine.material = new Material(Shader.Find("Sprites/Default"));
        currentLine.material.color = lineColor;

        currentLine.useWorldSpace = false;

        // Store line for undo
        lines.Add(currentLine);

        points.Clear();
    }

    void AddPoint(PointerEventData eventData)
    {
        if (currentLine == null) return;

        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            uiCamera,
            out localPoint
        );

        Vector3 point = new Vector3(localPoint.x, localPoint.y, -1f);

        if (points.Count > 0 && Vector3.Distance(points[points.Count - 1], point) < 2f)
            return;

        points.Add(point);

        currentLine.positionCount = points.Count;
        currentLine.SetPositions(points.ToArray());
    }

    public void ClearBoard()
    {
        foreach (LineRenderer line in lines)
        {
            if (line != null)
                Destroy(line.gameObject);
        }

        lines.Clear();
        lineCounter = 0;
    }

    // Undo last line
    public void UndoLastLine()
    {
        if (lines.Count == 0)
            return;

        LineRenderer lastLine = lines[lines.Count - 1];

        if (lastLine != null)
            Destroy(lastLine.gameObject);

        lines.RemoveAt(lines.Count - 1);

        lineCounter = Mathf.Max(0, lineCounter - 1);
    }

    // Called by slider
    public void SetLineWidth(float width)
    {
        lineWidth = width;

        if (currentLine != null)
        {
            currentLine.startWidth = width;
            currentLine.endWidth = width;
        }
    }

    // Called by color buttons
    public void SetLineColor(Color newColor)
    {
        lineColor = newColor;

        if (currentLine != null)
        {
            currentLine.material.color = newColor;
        }
    }
}