using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SymmetricalMazeGenerator : MonoBehaviour
{
    [SerializeField] private MazeCell _mazeCellPrefab;
    [SerializeField] private int _mazeWidth;
    [SerializeField] private int _mazeDepth;

    [SerializeField] private int _sectionsHorizontal;
    [SerializeField] private int _sectionsVertical;

    private GameObject _mazeParent;
    private MazeCell[,] _mazeGrid;
    private List<Rect> _sections = new List<Rect>();

    IEnumerator Start()
    {
        // Create the parent GameObject for organizing cells
        _mazeParent = new GameObject("Maze");

        // Initialize maze grid
        _mazeGrid = new MazeCell[_mazeWidth, _mazeDepth];

        // Instantiate MazeCells
        for (int x = 0; x < _mazeWidth; x++)
        {
            for (int z = 0; z < _mazeDepth; z++)
            {
                _mazeGrid[x, z] = Instantiate(_mazeCellPrefab, new Vector3(x, 0, z), Quaternion.identity, _mazeParent.transform);
            }
        }

        // Divide maze into sections
        DivideMazeIntoSections();

        // Clear hallways
        ClearHallways();

        // Generate maze sections
        yield return GenerateMazeSections();
    }

    private void DivideMazeIntoSections()
    {
        int hallwayWidth = 1;
        int sectionWidth = (_mazeWidth - ((_sectionsHorizontal - 1) * hallwayWidth)) / _sectionsHorizontal;
        int sectionDepth = (_mazeDepth - ((_sectionsVertical - 1) * hallwayWidth)) / _sectionsVertical;

        for (int i = 0; i < _sectionsHorizontal; i++)
        {
            for (int j = 0; j < _sectionsVertical; j++)
            {
                int x = i * (sectionWidth + hallwayWidth);
                int z = j * (sectionDepth + hallwayWidth);
                _sections.Add(new Rect(x, z, sectionWidth, sectionDepth));
            }
        }
    }

    private void ClearHallways()
    {
        int hallwayWidth = 1;

        // Clear vertical hallways
        for (int i = 1; i < _sectionsHorizontal; i++)
        {
            int hallwayX = i * ((_mazeWidth - ((_sectionsHorizontal - 1) * hallwayWidth)) / _sectionsHorizontal) + (i - 1) * hallwayWidth;
            for (int z = 0; z < _mazeDepth; z++)
            {
                _mazeGrid[hallwayX, z].ClearAllWalls();
            }
        }

        // Clear horizontal hallways
        for (int j = 1; j < _sectionsVertical; j++)
        {
            int hallwayZ = j * ((_mazeDepth - ((_sectionsVertical - 1) * hallwayWidth)) / _sectionsVertical) + (j - 1) * hallwayWidth;
            for (int x = 0; x < _mazeWidth; x++)
            {
                _mazeGrid[x, hallwayZ].ClearAllWalls();
            }
        }

        // Ensure walls around hallways
        foreach (var section in _sections)
        {
            int startX = (int)section.x;
            int startZ = (int)section.y;
            int width = (int)section.width;
            int depth = (int)section.height;

            for (int x = startX; x < startX + width; x++)
            {
                if (startZ + depth < _mazeDepth) _mazeGrid[x, startZ + depth].SetFrontWall(true);
            }

            for (int z = startZ; z < startZ + depth; z++)
            {
                if (startX + width < _mazeWidth) _mazeGrid[startX + width, z].SetRightWall(true);
            }
        }
    }

    private IEnumerator GenerateMazeSections()
    {
        HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();

        // Generate the first section normally
        Rect firstSection = _sections[0];
        yield return GenerateSection((int)firstSection.x, (int)firstSection.y, (int)firstSection.width, (int)firstSection.height, visitedCells);

        // Mirror the first section to other sections
        for (int i = 1; i < _sections.Count; i++)
        {
            Rect section = _sections[i];
            MirrorSection((int)firstSection.x, (int)firstSection.y, (int)section.x, (int)section.y, (int)firstSection.width, (int)firstSection.height);
        }

        // Visit all hallway cells
        for (int x = 0; x < _mazeWidth; x++)
        {
            for (int z = 0; z < _mazeDepth; z++)
            {
                if (!_mazeGrid[x, z].IsVisited)
                {
                    _mazeGrid[x, z].Visit();
                }
            }
        }
    }

    private IEnumerator GenerateSection(int startX, int startZ, int width, int depth, HashSet<Vector2Int> visitedCells)
    {
        int x = Random.Range(startX, startX + width);
        int z = Random.Range(startZ, startZ + depth);

        yield return GenerateMaze(null, _mazeGrid[x, z], startX, startZ, width, depth, visitedCells);
    }

    private void MirrorSection(int sourceStartX, int sourceStartZ, int destStartX, int destStartZ, int width, int depth)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                MazeCell sourceCell = _mazeGrid[sourceStartX + x, sourceStartZ + z];
                MazeCell destCell = _mazeGrid[destStartX + x, destStartZ + z];

                if (!sourceCell.IsVisited) continue;

                destCell.Visit();

                if (!sourceCell._leftWall.activeSelf) destCell.ClearLeftWall();
                if (!sourceCell._rightWall.activeSelf) destCell.ClearRightWall();
                if (!sourceCell._frontWall.activeSelf) destCell.ClearFrontWall();
                if (!sourceCell._backWall.activeSelf) destCell.ClearBackWall();
            }
        }
    }

    private IEnumerator GenerateMaze(MazeCell previousCell, MazeCell currentCell, int startX, int startZ, int width, int depth, HashSet<Vector2Int> visitedCells)
    {
        currentCell.Visit();
        visitedCells.Add(new Vector2Int((int)currentCell.transform.position.x, (int)currentCell.transform.position.z));

        yield return new WaitForSeconds(0.01f);

        MazeCell nextCell;
        do
        {
            nextCell = GetNextCell(currentCell, startX, startZ, width, depth, visitedCells);

            if (nextCell != null)
            {
                ClearWallsBetween(currentCell, nextCell);
                yield return GenerateMaze(currentCell, nextCell, startX, startZ, width, depth, visitedCells);
            }
        } while (nextCell != null);
    }

    private MazeCell GetNextCell(MazeCell currentCell, int startX, int startZ, int width, int depth, HashSet<Vector2Int> visitedCells)
    {
        int x = (int)currentCell.transform.position.x;
        int z = (int)currentCell.transform.position.z;

        List<MazeCell> neighbors = new List<MazeCell>();

        if (x + 1 < startX + width && !visitedCells.Contains(new Vector2Int(x + 1, z))) neighbors.Add(_mazeGrid[x + 1, z]);
        if (x - 1 >= startX && !visitedCells.Contains(new Vector2Int(x - 1, z))) neighbors.Add(_mazeGrid[x - 1, z]);
        if (z + 1 < startZ + depth && !visitedCells.Contains(new Vector2Int(x, z + 1))) neighbors.Add(_mazeGrid[x, z + 1]);
        if (z - 1 >= startZ && !visitedCells.Contains(new Vector2Int(x, z - 1))) neighbors.Add(_mazeGrid[x, z - 1]);

        if (neighbors.Count > 0)
        {
            return neighbors[Random.Range(0, neighbors.Count)];
        }

        return null;
    }

    private void ClearWallsBetween(MazeCell current, MazeCell next)
    {
        int dx = (int)(next.transform.position.x - current.transform.position.x);
        int dz = (int)(next.transform.position.z - current.transform.position.z);

        if (dx == 1)
        {
            current.ClearRightWall();
            next.ClearLeftWall();
        }
        else if (dx == -1)
        {
            current.ClearLeftWall();
            next.ClearRightWall();
        }
        else if (dz == 1)
        {
            current.ClearFrontWall();
            next.ClearBackWall();
        }
        else if (dz == -1)
        {
            current.ClearBackWall();
            next.ClearFrontWall();
        }
    }
}

