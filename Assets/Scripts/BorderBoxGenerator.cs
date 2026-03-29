using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UniformClockwiseBorderSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossDefinition bossDefinition;
    [SerializeField] private RectTransform imageContainer;
    [SerializeField] private RectTransform borderContainer;
    [SerializeField] private RectTransform boxPrefab;

    [Header("Border Settings")]
    [SerializeField] private float spacing = 4f;
    [SerializeField] private float padding = 8f;

    [Header("Box Size Limits")]
    [SerializeField] private float minBoxSize = 8f;
    [SerializeField] private float maxBoxSize = 40f;

    [Header("Debug")]
    [SerializeField] private float spawnDelay = 1f;
    [SerializeField] private bool snapToWholePixels = true;

    [Header("Stat Colors")]
    [SerializeField] private Color healthColor = Color.red;
    [SerializeField] private Color shieldColor = Color.blue;

    private readonly List<RectTransform> spawnedBoxes = new();
    private Coroutine spawnRoutine;

    private void Start()
    {
        GenerateBorder();
    }

    [ContextMenu("Generate Border")]
    public void GenerateBorder()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        ClearBoxes();

        int boxCount = GetTotalBoxCount();
        if (boxCount <= 0)
            return;

        if (!TryBuildLayout(boxCount, out List<Vector2> positions, out float boxSize, out int columns, out int rows))
            return;

        ResizeImageToInnerGrid(columns, rows, boxSize);

        spawnRoutine = StartCoroutine(SpawnBoxesOverTime(positions, boxSize));
    }

    private IEnumerator SpawnBoxesOverTime(List<Vector2> positions, float boxSize)
    {
        Vector2 finalSize = snapToWholePixels
            ? new Vector2(Mathf.Round(boxSize), Mathf.Round(boxSize))
            : new Vector2(boxSize, boxSize);

        for (int i = 0; i < positions.Count; i++)
        {
            RectTransform box = Instantiate(boxPrefab, borderContainer);

            box.anchorMin = new Vector2(0.5f, 0.5f);
            box.anchorMax = new Vector2(0.5f, 0.5f);
            box.pivot = new Vector2(0.5f, 0.5f);

            Vector2 pos = positions[i];
            if (snapToWholePixels)
                pos = Snap(pos);

            box.anchoredPosition = pos;
            box.sizeDelta = finalSize;

            Image boxImage = box.GetComponent<Image>();
            if (boxImage != null)
            {
                boxImage.color = GetBoxColorByIndex(i);
            }

            spawnedBoxes.Add(box);
            Debug.LogWarning("adding a box");

            if (spawnDelay > 0f)
                yield return new WaitForSeconds(spawnDelay);
        }

        spawnRoutine = null;
    }

    private Color GetBoxColorByIndex(int index)
    {
        if (bossDefinition == null)
            return healthColor;

        return index < bossDefinition.startingShield
            ? shieldColor
            : healthColor;
    }

    private int GetTotalBoxCount()
    {
        if (bossDefinition == null)
        {
            Debug.LogWarning("UniformClockwiseBorderSpawner requires a BossDefinition reference.");
            return 0;
        }

        return bossDefinition.startingShield + bossDefinition.maxHealth;
    }

    private bool TryBuildLayout(int boxCount, out List<Vector2> positions, out float boxSize, out int columns, out int rows)
    {
        positions = null;
        boxSize = 0f;
        columns = 0;
        rows = 0;

        if (imageContainer == null || borderContainer == null || boxPrefab == null || boxCount <= 0)
            return false;

        float availableWidth = borderContainer.rect.width;
        float availableHeight = borderContainer.rect.height;

        if (availableWidth <= 0f || availableHeight <= 0f)
            return false;

        if (!TryFindBestLayout(
                availableWidth,
                availableHeight,
                boxCount,
                spacing,
                minBoxSize,
                maxBoxSize,
                out boxSize,
                out columns,
                out rows))
        {
            return false;
        }

        if (snapToWholePixels)
            boxSize = Mathf.Floor(boxSize);

        float step = boxSize + spacing;

        positions = BuildClockwisePositions(
            totalBoxes: boxCount,
            columns: columns,
            rows: rows,
            step: step,
            snapToWholePixels: snapToWholePixels);

        return positions.Count > 0;
    }

    private void ResizeImageToInnerGrid(int columns, int rows, float boxSize)
    {
        float step = boxSize + spacing;

        float innerWidth = 0f;
        float innerHeight = 0f;

        if (columns >= 3)
            innerWidth = ((columns - 2) * step) - spacing;

        if (rows >= 3)
            innerHeight = ((rows - 2) * step) - spacing;

        innerWidth = Mathf.Max(1f, innerWidth);
        innerHeight = Mathf.Max(1f, innerHeight);

        if (snapToWholePixels)
        {
            innerWidth = Mathf.Round(innerWidth);
            innerHeight = Mathf.Round(innerHeight);
        }

        imageContainer.anchorMin = new Vector2(0.5f, 0.5f);
        imageContainer.anchorMax = new Vector2(0.5f, 0.5f);
        imageContainer.pivot = new Vector2(0.5f, 0.5f);
        imageContainer.anchoredPosition = Vector2.zero;

        imageContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, innerWidth);
        imageContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, innerHeight);
    }

    private static bool TryFindBestLayout(
        float availableWidth,
        float availableHeight,
        int totalBoxes,
        float spacing,
        float minSize,
        float maxSize,
        out float bestBoxSize,
        out int bestColumns,
        out int bestRows)
    {
        bestBoxSize = 0f;
        bestColumns = 0;
        bestRows = 0;

        float low = minSize;
        float high = maxSize;
        bool found = false;

        for (int i = 0; i < 30; i++)
        {
            float size = (low + high) * 0.5f;
            float step = size + spacing;

            int cols = Mathf.Max(2, Mathf.FloorToInt((availableWidth + spacing) / step));
            int rows = Mathf.Max(2, Mathf.FloorToInt((availableHeight + spacing) / step));

            int capacity = GetPerimeterCapacity(cols, rows);

            if (capacity >= totalBoxes)
            {
                found = true;
                bestBoxSize = size;
                bestColumns = cols;
                bestRows = rows;
                low = size;
            }
            else
            {
                high = size;
            }
        }

        return found;
    }

    private static int GetPerimeterCapacity(int columns, int rows)
    {
        if (columns <= 0 || rows <= 0)
            return 0;

        if (columns == 1 && rows == 1)
            return 1;

        if (rows == 1)
            return columns;

        if (columns == 1)
            return rows;

        return (columns * 2) + ((rows - 2) * 2);
    }

    private static List<Vector2> BuildClockwisePositions(
        int totalBoxes,
        int columns,
        int rows,
        float step,
        bool snapToWholePixels)
    {
        List<Vector2> positions = new(totalBoxes);

        float gridWidth = (columns - 1) * step;
        float gridHeight = (rows - 1) * step;

        float originX = -gridWidth * 0.5f;
        float originY = gridHeight * 0.5f;

        // Top row: left -> right
        for (int x = 0; x < columns && positions.Count < totalBoxes; x++)
        {
            Vector2 pos = new Vector2(
                originX + x * step,
                originY);

            positions.Add(snapToWholePixels ? Snap(pos) : pos);
        }

        // Right column: top -> bottom
        for (int y = 1; y < rows && positions.Count < totalBoxes; y++)
        {
            Vector2 pos = new Vector2(
                originX + (columns - 1) * step,
                originY - y * step);

            positions.Add(snapToWholePixels ? Snap(pos) : pos);
        }

        // Bottom row: right -> left
        for (int x = columns - 2; x >= 0 && positions.Count < totalBoxes; x--)
        {
            Vector2 pos = new Vector2(
                originX + x * step,
                originY - (rows - 1) * step);

            positions.Add(snapToWholePixels ? Snap(pos) : pos);
        }

        // Left column: bottom -> top
        for (int y = rows - 2; y > 0 && positions.Count < totalBoxes; y--)
        {
            Vector2 pos = new Vector2(
                originX,
                originY - y * step);

            positions.Add(snapToWholePixels ? Snap(pos) : pos);
        }

        return positions;
    }

    private static Vector2 Snap(Vector2 value)
    {
        return new Vector2(
            Mathf.Round(value.x),
            Mathf.Round(value.y));
    }

    [ContextMenu("Clear Border")]
    public void ClearBoxes()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        foreach (RectTransform box in spawnedBoxes)
        {
            if (box != null)
                Destroy(box.gameObject);
        }

        spawnedBoxes.Clear();
    }
}
