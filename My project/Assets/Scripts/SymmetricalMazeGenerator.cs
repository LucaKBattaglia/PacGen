using Unity.AI.Navigation;
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
    private int _roomWidth = 3;
    private int _roomDepth = 2;

    IEnumerator Start()
    {
        _mazeParent = new GameObject("Maze");
        _mazeGrid = new MazeCell[_mazeWidth, _mazeDepth];

        for (int x = 0; x < _mazeWidth; x++)
        {
            for (int z = 0; z < _mazeDepth; z++)
            {
                _mazeGrid[x, z] = Instantiate(_mazeCellPrefab, new Vector3(x, 0, z), Quaternion.identity, _mazeParent.transform);
            }
        }

        DivideMazeIntoSections();
        ClearHallways();
        CreateSectionEntryways();
        CreateRooms();
        yield return GenerateMazeSections();

        CreateExternalMazeEntrances();

        // Generate navigation grid for pathfinding
        PathPlanner pathPlanner = FindObjectOfType<PathPlanner>();
        if (pathPlanner != null)
        {
            pathPlanner.GenerateNavigationGrid(_mazeWidth, _mazeDepth);
        }

        GetComponent<NavMeshSurface>().BuildNavMesh();
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

        for (int i = 1; i < _sectionsHorizontal; i++)
        {
            int hallwayX = i * ((_mazeWidth - ((_sectionsHorizontal - 1) * hallwayWidth)) / _sectionsHorizontal) + (i - 1) * hallwayWidth;
            for (int z = 0; z < _mazeDepth; z++)
                _mazeGrid[hallwayX, z].ClearAllWalls();
        }

        for (int j = 1; j < _sectionsVertical; j++)
        {
            int hallwayZ = j * ((_mazeDepth - ((_sectionsVertical - 1) * hallwayWidth)) / _sectionsVertical) + (j - 1) * hallwayWidth;
            for (int x = 0; x < _mazeWidth; x++)
                _mazeGrid[x, hallwayZ].ClearAllWalls();
        }
    }

    private void CreateSectionEntryways()
    {
        int sectionWidth = (_mazeWidth - ((_sectionsHorizontal - 1) * 1)) / _sectionsHorizontal;
        int sectionDepth = (_mazeDepth - ((_sectionsVertical - 1) * 1)) / _sectionsVertical;

        foreach (var section in _sections)
        {
            int startX = (int)section.x;
            int startZ = (int)section.y;
            int width = (int)section.width;
            int depth = (int)section.height;

            List<Vector2Int> entryPoints = new List<Vector2Int>();

            if (startX > 0)
                entryPoints.Add(new Vector2Int(startX, startZ + depth / 2));
            if (startX + width < _mazeWidth)
                entryPoints.Add(new Vector2Int(startX + width, startZ + depth / 2));
            if (startZ > 0)
                entryPoints.Add(new Vector2Int(startX + width / 2, startZ));
            if (startZ + depth < _mazeDepth)
                entryPoints.Add(new Vector2Int(startX + width / 2, startZ + depth));

            entryPoints.Shuffle();
            for (int i = 0; i < Mathf.Min(2, entryPoints.Count); i++)
            {
                int x = entryPoints[i].x;
                int z = entryPoints[i].y;

                if (x == startX && z >= startZ && z < startZ + depth && startX > 0)
                {
                    _mazeGrid[x, z].ClearLeftWall();
                    _mazeGrid[x - 1, z].ClearRightWall();
                }
                else if (x == startX + width && z >= startZ && z < startZ + depth)
                {
                    _mazeGrid[x - 1, z].ClearRightWall();
                    _mazeGrid[x, z].ClearLeftWall();
                }
                else if (z == startZ && x >= startX && x < startX + width && startZ > 0)
                {
                    _mazeGrid[x, z].ClearBackWall();
                    _mazeGrid[x, z - 1].ClearFrontWall();
                }
                else if (z == startZ + depth && x >= startX && x < startX + width)
                {
                    _mazeGrid[x, z - 1].ClearFrontWall();
                    _mazeGrid[x, z].ClearBackWall();
                }
            }
        }
    }

    private void CreateRooms()
    {
        int topRoomX = _mazeWidth / 2 - _roomWidth / 2;
        int topRoomZ = -_roomDepth;
        for (int x = topRoomX; x < topRoomX + _roomWidth; x++)
        {
            for (int z = topRoomZ; z < topRoomZ + _roomDepth; z++)
            {
                MazeCell cell = Instantiate(_mazeCellPrefab, new Vector3(x, 0, z), Quaternion.identity, _mazeParent.transform);
                cell.ClearAllWalls();
                if (x == topRoomX) cell.SetLeftWall(true);
                if (x == topRoomX + _roomWidth - 1) cell.SetRightWall(true);
                if (z == topRoomZ) cell.SetBackWall(true);
                if (x == topRoomX + _roomWidth / 2 && z == topRoomZ + _roomDepth - 1)
                    cell.ClearFrontWall();
            }
        }

        int bottomRoomX = _mazeWidth / 2 - _roomWidth / 2;
        int bottomRoomZ = _mazeDepth;
        for (int x = bottomRoomX; x < bottomRoomX + _roomWidth; x++)
        {
            for (int z = bottomRoomZ; z < bottomRoomZ + _roomDepth; z++)
            {
                MazeCell cell = Instantiate(_mazeCellPrefab, new Vector3(x, 0, z), Quaternion.identity, _mazeParent.transform);
                cell.ClearAllWalls();
                if (x == bottomRoomX) cell.SetLeftWall(true);
                if (x == bottomRoomX + _roomWidth - 1) cell.SetRightWall(true);
                if (z == bottomRoomZ + _roomDepth - 1) cell.SetFrontWall(true);
                if (x == bottomRoomX + _roomWidth / 2 && z == bottomRoomZ)
                    cell.ClearBackWall();
            }
        }
    }

    private void CreateExternalMazeEntrances()
    {
        int midTop = _mazeWidth / 2;
        int midBottom = _mazeWidth / 2;
        int midLeft = _mazeDepth / 2;
        int midRight = _mazeDepth / 2;

        // Top wall entrance (Z = 0)
        _mazeGrid[midTop, 0].ClearBackWall();
        for (int i = 1; i <= 2; i++)
        {
            MazeCell cell = Instantiate(_mazeCellPrefab, new Vector3(midTop, 0, -i), Quaternion.identity, _mazeParent.transform);
            cell.ClearAllWalls();
            if (i == 1) cell.ClearFrontWall(); // Connect to maze
            cell.SetBackWall(true);            // Always keep back wall
            cell.SetFrontWall(true);           // Always keep front wall
            cell.SetLeftWall(false);           // Ensure left wall is off
            cell.SetRightWall(false);          // Ensure right wall is off
        }

        // Bottom wall entrance (Z = _mazeDepth - 1)
        _mazeGrid[midBottom, _mazeDepth - 1].ClearFrontWall();
        for (int i = 1; i <= 2; i++)
        {
            MazeCell cell = Instantiate(_mazeCellPrefab, new Vector3(midBottom, 0, _mazeDepth - 1 + i), Quaternion.identity, _mazeParent.transform);
            cell.ClearAllWalls();
            if (i == 1) cell.ClearBackWall(); // Connect to maze
            cell.SetFrontWall(true);
            cell.SetBackWall(true);
            cell.SetLeftWall(false);
            cell.SetRightWall(false);
        }

        // Left wall entrance (X = 0)
        _mazeGrid[0, midLeft].ClearLeftWall();
        for (int i = 1; i <= 2; i++)
        {
            MazeCell cell = Instantiate(_mazeCellPrefab, new Vector3(-i, 0, midLeft), Quaternion.identity, _mazeParent.transform);
            cell.ClearAllWalls();
            if (i == 1) cell.ClearRightWall(); // Connect to maze
            cell.SetFrontWall(true);
            cell.SetBackWall(true);
            cell.SetLeftWall(false);
            cell.SetRightWall(false);
        }

        // Right wall entrance (X = _mazeWidth - 1)
        _mazeGrid[_mazeWidth - 1, midRight].ClearRightWall();
        for (int i = 1; i <= 2; i++)
        {
            MazeCell cell = Instantiate(_mazeCellPrefab, new Vector3(_mazeWidth - 1 + i, 0, midRight), Quaternion.identity, _mazeParent.transform);
            cell.ClearAllWalls();
            if (i == 1) cell.ClearLeftWall(); // Connect to maze
            cell.SetFrontWall(true);
            cell.SetBackWall(true);
            cell.SetLeftWall(false);
            cell.SetRightWall(false);
        }
    }


    private IEnumerator GenerateMazeSections()
    {
        HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();
        Rect firstSection = _sections[0];
        yield return GenerateSection((int)firstSection.x, (int)firstSection.y, (int)firstSection.width, (int)firstSection.height, visitedCells);

        for (int i = 1; i < _sections.Count; i++)
        {
            Rect section = _sections[i];
            TransformSection((int)firstSection.x, (int)firstSection.y, (int)section.x, (int)section.y, (int)firstSection.width, (int)firstSection.height);
        }

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

    private void TransformSection(int sx, int sz, int dx, int dz, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                MazeCell src = _mazeGrid[sx + x, sz + z];
                MazeCell dest = _mazeGrid[dx + x, dz + z];

                if (!src.IsVisited) continue;
                dest.Visit();
                if (!src._leftWall.activeSelf) dest.ClearLeftWall();
                if (!src._rightWall.activeSelf) dest.ClearRightWall();
                if (!src._frontWall.activeSelf) dest.ClearFrontWall();
                if (!src._backWall.activeSelf) dest.ClearBackWall();
            }
        }
    }

    private IEnumerator GenerateMaze(MazeCell prev, MazeCell curr, int sx, int sz, int w, int d, HashSet<Vector2Int> visited)
    {
        curr.Visit();
        visited.Add(new Vector2Int((int)curr.transform.position.x, (int)curr.transform.position.z));
        yield return new WaitForSeconds(0.01f);

        MazeCell next;
        do
        {
            next = GetNextCell(curr, sx, sz, w, d, visited);
            if (next != null)
            {
                ClearWallsBetween(curr, next);
                yield return GenerateMaze(curr, next, sx, sz, w, d, visited);
            }
        } while (next != null);
    }

    private MazeCell GetNextCell(MazeCell cell, int sx, int sz, int w, int d, HashSet<Vector2Int> visited)
    {
        int x = (int)cell.transform.position.x;
        int z = (int)cell.transform.position.z;

        List<MazeCell> neighbors = new List<MazeCell>();
        if (x + 1 < sx + w && !visited.Contains(new Vector2Int(x + 1, z))) neighbors.Add(_mazeGrid[x + 1, z]);
        if (x - 1 >= sx && !visited.Contains(new Vector2Int(x - 1, z))) neighbors.Add(_mazeGrid[x - 1, z]);
        if (z + 1 < sz + d && !visited.Contains(new Vector2Int(x, z + 1))) neighbors.Add(_mazeGrid[x, z + 1]);
        if (z - 1 >= sz && !visited.Contains(new Vector2Int(x, z - 1))) neighbors.Add(_mazeGrid[x, z - 1]);

        if (neighbors.Count > 0)
            return neighbors[Random.Range(0, neighbors.Count)];

        return null;
    }

    private void ClearWallsBetween(MazeCell a, MazeCell b)
    {
        int dx = (int)(b.transform.position.x - a.transform.position.x);
        int dz = (int)(b.transform.position.z - a.transform.position.z);

        if (dx == 1) { a.ClearRightWall(); b.ClearLeftWall(); }
        else if (dx == -1) { a.ClearLeftWall(); b.ClearRightWall(); }
        else if (dz == 1) { a.ClearFrontWall(); b.ClearBackWall(); }
        else if (dz == -1) { a.ClearBackWall(); b.ClearFrontWall(); }
    }
}

// Extension for shuffling
public static class ListExtensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            (list[i], list[rnd]) = (list[rnd], list[i]);
        }
    }
}
