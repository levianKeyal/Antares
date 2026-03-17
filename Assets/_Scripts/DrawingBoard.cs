using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIDrawingBoard : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Line Settings")]
    public LineRenderer linePrefab;

    [Header("Render Settings")]
    public int baseSortingOrder = 100;
    public Color lineColor = Color.black;

    [Range(0.5f, 2f)]
    public float lineWidth = 1.2f;

    [Header("Drawing")]
    public float minPointDistance = 2f;
    public int curveResolution = 4;

    [Header("Dot Settings")]
    public float tapPointOffset = 0.5f;

    [Header("Eraser")]
    public float eraserRadius = 12f;
    public float eraseStep = 4f; // 🔥 Mejora 3: más precisión
    bool eraserMode = false;

    [Header("Drawing Panel")]
    public RectTransform rectTransform;

    [Header("Blackboard Canvas")]
    public Canvas canvas;

    [Header("Drawingboard Animations")]
    public Animator dbAnimator;

    Camera uiCamera;

    LineRenderer currentLine;

    List<Vector3> points = new List<Vector3>();
    List<LineRenderer> lines = new List<LineRenderer>();

    Stack<LineRenderer> linePool = new Stack<LineRenderer>();

    int lineCounter = 0;

    Material sharedMaterial;

    bool isDrawing = false;

    // 🔥 NUEVO: eraser smoothing
    Vector3 lastErasePosition;
    bool isErasing = false;

    void Awake()
    {
        if (canvas == null)
            canvas = GetComponentInChildren<Canvas>(true);

        if (canvas != null)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                uiCamera = null;
            else
                uiCamera = canvas.worldCamera;
        }

        sharedMaterial = new Material(Shader.Find("Sprites/Default"));
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

    bool IsPointerOverUIButton(PointerEventData eventData)
    {
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (var r in results)
        {
            if (r.gameObject == rectTransform.gameObject)
                continue;

            if (r.gameObject.GetComponent<UnityEngine.UI.Button>() != null)
                return true;
        }

        return false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsPointerOverUIButton(eventData))
            return;

        if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, eventData.position, uiCamera))
            return;

        Vector3 point = GetLocalPoint(eventData);

        if (eraserMode)
        {
            isDrawing = true;
            isErasing = true;
            lastErasePosition = point;

            EraseInterpolated(point);
            return;
        }

        StartLine();
        AddPointToLine(point);

        isDrawing = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDrawing)
            return;

        if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, eventData.position, uiCamera))
            return;

        Vector3 point = GetLocalPoint(eventData);

        if (eraserMode)
        {
            EraseInterpolated(point);
            return;
        }

        AddPointToLine(point);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDrawing)
            return;

        if (currentLine != null && points.Count == 1)
        {
            Vector3 p = points[0];
            Vector3 offset = new Vector3(tapPointOffset, 0f, 0f);

            currentLine.positionCount = 2;
            currentLine.SetPosition(0, p);
            currentLine.SetPosition(1, p + offset);
        }

        currentLine = null;
        isDrawing = false;
        isErasing = false; // 🔥 reset
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

        return new Vector3(localPoint.x, localPoint.y, 0f);
    }

    void StartLine()
    {
        currentLine = GetLineFromPool();

        currentLine.gameObject.SetActive(true);
        currentLine.transform.SetParent(canvas.transform, false);

        currentLine.transform.localPosition = Vector3.zero;
        currentLine.transform.localRotation = Quaternion.identity;
        currentLine.transform.localScale = Vector3.one;

        currentLine.transform.SetAsLastSibling();

        currentLine.sortingOrder = baseSortingOrder + lineCounter;
        lineCounter++;

        currentLine.material = sharedMaterial;
        currentLine.material.color = lineColor;

        currentLine.startWidth = lineWidth;
        currentLine.endWidth = lineWidth;

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
        line.transform.SetParent(canvas.transform, false);

        line.startWidth = lineWidth;
        line.endWidth = lineWidth;

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
        if (currentLine == null)
            return;

        if (points.Count > 0 &&
            Vector3.Distance(points[points.Count - 1], point) < minPointDistance)
            return;

        points.Add(point);

        if (points.Count < 4)
        {
            currentLine.positionCount = points.Count;
            currentLine.SetPositions(points.ToArray());
            return;
        }

        AddSmoothSegment();
    }

    void AddSmoothSegment()
    {
        int count = points.Count;

        Vector3 p0 = points[count - 4];
        Vector3 p1 = points[count - 3];
        Vector3 p2 = points[count - 2];
        Vector3 p3 = points[count - 1];

        List<Vector3> newPoints = new List<Vector3>();

        for (int j = 0; j < curveResolution; j++)
        {
            float t = j / (float)curveResolution;

            Vector3 point =
                0.5f *
                ((2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);

            newPoints.Add(point);
        }

        int oldCount = currentLine.positionCount;
        currentLine.positionCount = oldCount + newPoints.Count;

        for (int i = 0; i < newPoints.Count; i++)
            currentLine.SetPosition(oldCount + i, newPoints[i]);
    }

    // 🔥 NUEVO: interpolación del borrador
    void EraseInterpolated(Vector3 currentPos)
    {
        float distance = Vector3.Distance(lastErasePosition, currentPos);
        int steps = Mathf.CeilToInt(distance / eraseStep);

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps;
            Vector3 pos = Vector3.Lerp(lastErasePosition, currentPos, t);

            EraseAtPoint(pos);
        }

        lastErasePosition = currentPos;
    }

    // 🔥 MEJORADO: múltiples cortes + sin return
    void EraseAtPoint(Vector3 position)
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

                if (dist <= eraserRadius)
                {
                    SplitLine(line, j);

                    count = line.positionCount;
                    j = Mathf.Max(0, j - 1);
                }
            }
        }
    }

    // 🔥 MEJORADO: limpieza de fragmentos pequeños
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

        if (firstSegment.Count > 2)
        {
            line.positionCount = firstSegment.Count;
            line.SetPositions(firstSegment.ToArray());
        }
        else
        {
            ReturnLineToPool(line);
            lines.Remove(line);
        }

        if (secondSegment.Count > 2)
        {
            LineRenderer newLine = GetLineFromPool();

            newLine.gameObject.SetActive(true);
            newLine.transform.SetParent(canvas.transform, false);

            newLine.positionCount = secondSegment.Count;
            newLine.SetPositions(secondSegment.ToArray());

            newLine.material = line.material;

            newLine.startWidth = line.startWidth;
            newLine.endWidth = line.endWidth;

            newLine.sortingOrder = baseSortingOrder + lineCounter;
            lineCounter++;

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

    public void EnableEraser() => eraserMode = true;
    public void EnablePen() => eraserMode = false;
    public void ToggleEraser() => eraserMode = !eraserMode;

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

    public void SetLineColor(Color newColor)
    {
        lineColor = newColor;

        if (currentLine != null)
            currentLine.material.color = newColor;
    }

    public void DrawinBoardAnimIO()
    {
        dbAnimator.SetBool("DrawingBoardIO", !dbAnimator.GetBool("DrawingBoardIO"));
    }
}