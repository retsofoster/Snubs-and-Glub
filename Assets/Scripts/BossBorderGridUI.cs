using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BossBorderGridUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossDefinition bossDefinition;
    [SerializeField] private GridLayoutGroup borderGrid;
    [SerializeField] private GameObject gridCellPrefab;
    [SerializeField] private RectTransform bossCard;
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private Image bossPortraitImage;

    [Header("Cell Colors")]
    [SerializeField] private Color healthColor = Color.red;
    [SerializeField] private Color shieldColor = Color.blue;
    [SerializeField] private Color emptyColor = Color.clear;

    private readonly List<GameObject> spawnedCells = new();

    private int columns;
    private int rows;
    private int innerColumns;
    private int innerRows;

    private void Start()
    {
        Build();
    }

    [ContextMenu("Build Grid")]
    public void Build()
    {
        if (bossDefinition == null || borderGrid == null || gridCellPrefab == null || bossCard == null)
        {
            Debug.LogWarning("BossBorderGridUI is missing required references.");
            return;
        }

        //ClearGrid();

        int totalStats = bossDefinition.maxHealth + bossDefinition.startingShield;

        int totalLifeCells = bossDefinition.startingShield + bossDefinition.maxHealth + 4; 
        int sideSize = totalLifeCells / 4;
        int totalCells = sideSize * sideSize;
        //FindBestGrid(totalStats, out columns, out rows, out innerColumns, out innerRows);

        borderGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        borderGrid.constraintCount = sideSize;

        if (bossNameText != null)
        {
            bossNameText.text = bossDefinition.bossName;
        }

        if (bossPortraitImage != null)
        {
            bossPortraitImage.sprite = bossDefinition.portrait;
            bossPortraitImage.enabled = bossDefinition.portrait != null;
        }

        float cellWidth = borderGrid.cellSize.x;
        float cellHeight = borderGrid.cellSize.y;
        float spacingX = borderGrid.spacing.x;
        float spacingY = borderGrid.spacing.y;

        float bossWidth = (sideSize - 2) * cellWidth + (innerColumns - 1) * spacingX;
        float bossHeight = (sideSize - 2) * cellHeight + (innerRows - 1) * spacingY;
        bossCard.sizeDelta = new Vector2(bossWidth, bossHeight);

        List<Color> borderColors = new List<Color>();


        borderColors.Add(shieldColor);
        Debug.LogWarning("First cell");
        for (int i = 1; i < sideSize * sideSize; i++)
        {
            if(i % sideSize == 0)
            {
                borderColors.Add(shieldColor);
                Debug.LogWarning("sheild cell");
            }
            else if(i < sideSize)
            {
                Debug.LogWarning("health cell");
                borderColors.Add(healthColor);
            }
            else if(((i + 1) % sideSize) == 0)
            {
                borderColors.Add(healthColor);
                Debug.LogWarning("health cell");
            }
            else if(((i - 1) % sideSize) == 0)
            {
                borderColors.Add(healthColor);
                Debug.LogWarning("health cell");
            }
            else if(i > totalCells - sideSize)
            {
                borderColors.Add(healthColor);
                Debug.LogWarning("health cell");
            }
            else
            {
                borderColors.Add(emptyColor);
                Debug.LogWarning("regular cell");
            }
        }

        int borderColorIndex = 0;

        int startCol = (columns - innerColumns) / 2;
        int endCol = startCol + innerColumns - 1;
        int startRow = (sideSize - innerRows) / 2;
        int endRow = startRow + innerRows - 1;

        for (int row = 0; row < sideSize; row++)
        {
            for (int col = 0; col < sideSize; col++)
            {
                // bool isInnerBossArea =
                //     row >= startRow && row <= endRow &&
                //     col >= startCol && col <= endCol;

                GameObject cell = Instantiate(gridCellPrefab, borderGrid.transform);
                spawnedCells.Add(cell);

                Image image = cell.GetComponent<Image>();

                // if (isInnerBossArea)
                // {
                //     image.color = emptyColor;
                // }
                // else
                // {
                    image.color = borderColors[borderColorIndex];
                    borderColorIndex++;
                //}
            }
        }
    }

    private void FindBestGrid(int targetBorderCount, out int bestCols, out int bestRows, out int bestInnerCols, out int bestInnerRows)
    {
        bestCols = 0;
        bestRows = 0;
        bestInnerCols = 0;
        bestInnerRows = 0;

        for (int outerRows = 3; outerRows <= 12; outerRows++)
        {
            for (int outerCols = 3; outerCols <= 12; outerCols++)
            {
                for (int innerRowsCandidate = 1; innerRowsCandidate < outerRows; innerRowsCandidate++)
                {
                    for (int innerColsCandidate = 1; innerColsCandidate < outerCols; innerColsCandidate++)
                    {
                        bool centeredWidthMatchesParity = (outerCols - innerColsCandidate) % 2 == 0;
                        bool centeredHeightMatchesParity = (outerRows - innerRowsCandidate) % 2 == 0;

                        if (!centeredWidthMatchesParity || !centeredHeightMatchesParity)
                        {
                            continue;
                        }

                        int totalCells = outerCols * outerRows;
                        int innerCells = innerColsCandidate * innerRowsCandidate;
                        int borderCells = totalCells - innerCells;

                        if (borderCells == targetBorderCount)
                        {
                            bestCols = outerCols;
                            bestRows = outerRows;
                            bestInnerCols = innerColsCandidate;
                            bestInnerRows = innerRowsCandidate;
                            return;
                        }
                    }
                }
            }
        }

        Debug.LogError($"Could not find a centered grid for target border count: {targetBorderCount}");
    }

    private void ClearGrid()
    {
        for (int i = borderGrid.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(borderGrid.transform.GetChild(i).gameObject);
        }

        spawnedCells.Clear();
    }
}