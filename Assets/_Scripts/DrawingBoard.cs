using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIDrawingBoard : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Line Settings")]
    public LineRenderer linePrefab;

    [Header("Render Settings")]
    public int baseSortingOrder = 100;

    [Range(0.5f, 2f)]
    public float lineWidth = 1.2f;

    public Color lineColor = Color.black;

    [Header("Runtime Controls")]
    public Slider widthSlider;

    [Header("Drawing")]
    public float minPointDistance = 2f;
    public int curveResolution = 8;

    [Header("Eraser")]
    public float eraserRadius = 12f;
    bool eraserMode = false;

    [Header("WebGL Optimization")]
    [Range(0.5f, 5f)]
    public float simplifyTolerance = 1.5f;

    public int maxPointsPerLine = 250;

    RectTransform rectTransform;
    Camera uiCamera;

    LineRenderer currentLine;

    List<Vector3> points = new List<Vector3>();
    List<LineRenderer> lines = new List<LineRenderer>();

    Stack<LineRenderer> linePool = new Stack<LineRenderer>();

    int lineCounter = 0;

    Material sharedMaterial;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            uiCamera = null;
        else
            uiCamera = canvas.worldCamera;

        sharedMaterial = new Material(Shader.Find("Sprites/Default"));
    }

    void Start()
    {
        if (widthSlider != null)
        {
            widthSlider.minValue = 0.5f;
            widthSlider.maxValue = 2f;
            widthSlider.value = lineWidth;

            widthSlider.onValueChanged.AddListener(SetLineWidth);
        }
    }

    void OnEnable()
    {
        FitToFullScreen();
    }

    void FitToFullScreen()
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;

        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, eventData.position, uiCamera))
            return;

        Vector3 point = GetLocalPoint(eventData);

        if (eraserMode)
        {
            Erase(point);
            return;
        }

        StartLine();
        AddPointToLine(point);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, eventData.position, uiCamera))
            return;

        Vector3 point = GetLocalPoint(eventData);

        if (eraserMode)
        {
            Erase(point);
            return;
        }

        AddPointToLine(point);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        currentLine = null;
    }

    Vector3 GetLocalPoint(PointerEventData eventData)
    {
        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            uiCamera,
            out localPoint
        );

        // Z must be 0 for UI space
        return new Vector3(localPoint.x, localPoint.y, 0f);
    }

    void StartLine()
    {
        currentLine = GetLineFromPool();

        currentLine.gameObject.SetActive(true);

        currentLine.transform.SetParent(transform, false);

        currentLine.transform.localPosition = Vector3.zero;
        currentLine.transform.localRotation = Quaternion.identity;
        currentLine.transform.localScale = Vector3.one;

        currentLine.transform.SetAsLastSibling();

        // Ensure new line is drawn above previous ones
        currentLine.sortingOrder = baseSortingOrder + lineCounter;
        lineCounter++;

        currentLine.startWidth = lineWidth;
        currentLine.endWidth = lineWidth;

        currentLine.material = sharedMaterial;
        currentLine.material.color = lineColor;

        currentLine.useWorldSpace = false;

        lines.Add(currentLine);

        points.Clear();
    }

    LineRenderer GetLineFromPool()
{
    LineRenderer line;

    if (linePool.Count > 0)
        line = linePool.Pop();
    else
        line = Instantiate(linePrefab);

    line.positionCount = 0;

    line.transform.SetParent(transform, false);

    return line;
}

    void ReturnLineToPool(LineRenderer line)
    {
        line.positionCount = 0;
        line.gameObject.SetActive(false);
        linePool.Push(line);
    }

    void AddPointToLine(Vector3 point)
    {
        if (points.Count > 0 &&
            Vector3.Distance(points[points.Count - 1], point) < minPointDistance)
            return;

        points.Add(point);

        if (points.Count > maxPointsPerLine)
            SimplifyCurrentLine();

        List<Vector3> smoothed = SmoothLine(points);

        currentLine.positionCount = smoothed.Count;
        currentLine.SetPositions(smoothed.ToArray());
    }

    List<Vector3> SmoothLine(List<Vector3> rawPoints)
    {
        List<Vector3> smoothed = new List<Vector3>();

        if (rawPoints.Count < 3)
            return new List<Vector3>(rawPoints);

        for (int i = 0; i < rawPoints.Count - 1; i++)
        {
            Vector3 p0 = i == 0 ? rawPoints[i] : rawPoints[i - 1];
            Vector3 p1 = rawPoints[i];
            Vector3 p2 = rawPoints[i + 1];
            Vector3 p3 = (i + 2 < rawPoints.Count) ? rawPoints[i + 2] : p2;

            for (int j = 0; j < curveResolution; j++)
            {
                float t = j / (float)curveResolution;

                Vector3 point =
                    0.5f *
                    ((2f * p1) +
                    (-p0 + p2) * t +
                    (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
                    (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);

                smoothed.Add(point);
            }
        }

        smoothed.Add(rawPoints[rawPoints.Count - 1]);

        return smoothed;
    }

    void SimplifyCurrentLine()
    {
        List<Vector3> simplified = DouglasPeucker(points, simplifyTolerance);
        points = simplified;
    }

    void Erase(Vector3 position) 
    { 
        for (int i = lines.Count - 1; i >= 0; i--) 
        {
            LineRenderer line = lines[i];
            int count = line.positionCount;

            for (int j = 0; j < count - 1; j++) 
            { 
                Vector3 p1 = line.GetPosition(j);
                Vector3 p2 = line.GetPosition(j + 1);
                float dist = DistancePointToSegment(position, p1, p2);
                if (dist <= eraserRadius) { SplitLine(line, j);
                    return; 
                } 
            } 
        } 
    }

    void SplitLine(LineRenderer line, int index)
    {
        int count = line.positionCount;

        if (count < 4)
        {
            ReturnLineToPool(line);
            lines.Remove(line);
            return;
        }

        List<Vector3> firstSegment = new List<Vector3>();
        List<Vector3> secondSegment = new List<Vector3>();

        for (int i = 0; i <= index; i++)
            firstSegment.Add(line.GetPosition(i));

        for (int i = index + 1; i < count; i++)
            secondSegment.Add(line.GetPosition(i));

        // --- FIRST SEGMENT (reuse original line) ---
        if (firstSegment.Count > 1)
        {
            line.positionCount = firstSegment.Count;
            line.SetPositions(firstSegment.ToArray());
        }
        else
        {
            ReturnLineToPool(line);
            lines.Remove(line);
        }

        // --- SECOND SEGMENT (create new line) ---
        if (secondSegment.Count > 1)
        {
            LineRenderer newLine = GetLineFromPool();

            newLine.gameObject.SetActive(true);

            // VERY IMPORTANT: parent to drawing board
            newLine.transform.SetParent(transform, false);

            // Reset transform to avoid pooling offsets
            newLine.transform.localPosition = Vector3.zero;
            newLine.transform.localRotation = Quaternion.identity;
            newLine.transform.localScale = Vector3.one;

            newLine.useWorldSpace = false;

            newLine.positionCount = secondSegment.Count;
            newLine.SetPositions(secondSegment.ToArray());

            newLine.startWidth = line.startWidth;
            newLine.endWidth = line.endWidth;

            newLine.material = sharedMaterial;
            newLine.material.color = line.material.color;

            // Ensure correct render order
            newLine.sortingOrder = baseSortingOrder + lineCounter;
            lineCounter++;

            newLine.transform.SetAsLastSibling();

            lines.Add(newLine);
        }
    }
    float DistancePointToSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        Vector3 ap = point - a;

        float magnitudeAB = ab.sqrMagnitude;
        float abapProduct = Vector3.Dot(ap, ab);
        float distance = abapProduct / magnitudeAB;

        if (distance < 0)
            return Vector3.Distance(point, a);
        else if (distance > 1)
            return Vector3.Distance(point, b);
        else
        {
            Vector3 projection = a + distance * ab;
            return Vector3.Distance(point, projection);
        }
    }
    public void EnableEraser()
    {
        eraserMode = true;
    }

    public void EnablePen()
    {
        eraserMode = false;
    }

    public void ToggleEraser()
    {
        eraserMode = !eraserMode;
    }

    public void ClearBoard()
    {
        foreach (var line in lines)
            ReturnLineToPool(line);

        lines.Clear();

        lineCounter = 0;
    }

    public void UndoLastLine()
    {
        if (lines.Count == 0)
            return;

        LineRenderer last = lines[lines.Count - 1];

        ReturnLineToPool(last);

        lines.RemoveAt(lines.Count - 1);
    }

    public void SetLineWidth(float width)
    {
        lineWidth = width;

        if (currentLine != null)
        {
            currentLine.startWidth = width;
            currentLine.endWidth = width;
        }
    }

    public void SetLineColor(Color newColor)
    {
        lineColor = newColor;

        if (currentLine != null)
            currentLine.material.color = newColor;
    }

    List<Vector3> DouglasPeucker(List<Vector3> pointList, float epsilon)
    {
        if (pointList.Count < 3)
            return new List<Vector3>(pointList);

        int firstIndex = 0;
        int lastIndex = pointList.Count - 1;

        List<int> pointIndexsToKeep = new List<int>();
        pointIndexsToKeep.Add(firstIndex);
        pointIndexsToKeep.Add(lastIndex);

        DouglasPeuckerReduction(pointList, firstIndex, lastIndex, epsilon, ref pointIndexsToKeep);

        List<Vector3> returnPoints = new List<Vector3>();

        pointIndexsToKeep.Sort();

        foreach (int index in pointIndexsToKeep)
            returnPoints.Add(pointList[index]);

        return returnPoints;
    }

    void DouglasPeuckerReduction(List<Vector3> points, int firstIndex, int lastIndex, float epsilon, ref List<int> pointIndexsToKeep)
    {
        float maxDistance = 0;
        int indexFarthest = 0;

        for (int i = firstIndex; i < lastIndex; i++)
        {
            float distance = PerpendicularDistance(points[firstIndex], points[lastIndex], points[i]);

            if (distance > maxDistance)
            {
                maxDistance = distance;
                indexFarthest = i;
            }
        }

        if (maxDistance > epsilon && indexFarthest != 0)
        {
            pointIndexsToKeep.Add(indexFarthest);

            DouglasPeuckerReduction(points, firstIndex, indexFarthest, epsilon, ref pointIndexsToKeep);
            DouglasPeuckerReduction(points, indexFarthest, lastIndex, epsilon, ref pointIndexsToKeep);
        }
    }

    float PerpendicularDistance(Vector3 point1, Vector3 point2, Vector3 point)
    {
        float area = Mathf.Abs(
            point1.x * point2.y +
            point2.x * point.y +
            point.x * point1.y -
            point2.x * point1.y -
            point.x * point2.y -
            point1.x * point.y
        );

        float bottom = Mathf.Sqrt(
            Mathf.Pow(point1.x - point2.x, 2) +
            Mathf.Pow(point1.y - point2.y, 2)
        );

        return area / bottom * 2f;
    }
}