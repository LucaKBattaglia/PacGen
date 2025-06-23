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
    private int _roomWidth = 3; // Width of the room (3 cells)
    private int _roomDepth = 2; // Depth of the room (2 cells)

    IEnumerator Start()
    {
        // Create the parent GameObject for organizing cells
        _mazeParent = new GameObject("Maze");

        // Initialize maze grid (no extra rows for rooms)
        _mazeGrid = new MazeCell[_mazeWidth, _mazeDepth];

        // Instantiate MazeCells for the maze
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

        // Create entryways into sections
        CreateSectionEntryways();

        // Create top and bottom rooms
        CreateRooms();

        // Generate maze sections
        yield return GenerateMazeSections();

        // Build NavMesh
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
    }

    private void CreateSectionEntryways()
    {
        int hallwayWidth = 1;
        int sectionWidth = (_mazeWidth - ((_sectionsHorizontal - 1) * hallwayWidth)) / _sectionsHorizontal;
        int sectionDepth = (_mazeDepth - ((_sectionsVertical - 1) * hallwayWidth)) / _sectionsVertical;

        foreach (var section in _sections)
        {
            int startX = (int)section.x;
            int startZ = (int)section.y;
            int width = (int)section.width;
            int depth = (int)section.height;

            // Create two entryways per section, ensuring they connect to internal hallways
            List<Vector2Int> entryPoints = new List<Vector2Int>();

            // Add entry points based on section position and transformation
            if (startX > 0) // Left hallway
            {
                int z = startZ + depth / 2; // Center vertically
                entryPoints.Add(new Vector2Int(startX, z));
            }
            if (startX + width < _mazeWidth) // Right hallway
            {
                int z = startZ + depth / 2;
                entryPoints.Add(new Vector2Int(startX + width, z));
            }
            if (startZ > 0) // Bottom hallway
            {
                int x = startX + width / 2; // Center horizontally
                entryPoints.Add(new Vector2Int(x, startZ));
            }
            if (startZ + depth < _mazeDepth) // Top hallway
            {
                int x = startX + width / 2;
                entryPoints.Add(new Vector2Int(x, startZ + depth));
            }

            // Select two entry points (prioritize hallways for boundary sections)
            entryPoints.Shuffle();
            for (int i = 0; i < Mathf.Min(2, entryPoints.Count); i++)
            {
                int x = entryPoints[i].x;
                int z = entryPoints[i].y;

                // Open walls based on hallway connection
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

        // Ensure top and bottom hallways are open for room connections
        int topHallwayZ = 0;
        int bottomHallwayZ = _mazeDepth - 1;
        int entranceX = _mazeWidth / 2;
        _mazeGrid[entranceX, topHallwayZ].ClearBackWall();
        _mazeGrid[entranceX, bottomHallwayZ].ClearFrontWall();
    }

    private void CreateRooms()
    {
        // Create top room (attached to top hallway at z = 0)
        int topRoomZ = -_roomDepth; // Starts at z = -2, extends to z = -1
        int topRoomX = _mazeWidth / 2 - _roomWidth / 2; // Center horizontally
        for (int x = topRoomX; x < topRoomX + _roomWidth; x++)
        {
            for (int z = topRoomZ; z < topRoomZ + _roomDepth; z++)
            {
                MazeCell cell = Instantiate(_mazeCellPrefab, new Vector3(x, 0, z), Quaternion.identity, _mazeParent.transform);
                cell.ClearAllWalls();
                // Set exterior walls
                if (x == topRoomX) cell.SetLeftWall(true);
                if (x == topRoomX + _roomWidth - 1) cell.SetRightWall(true);
                if (z == topRoomZ) cell.SetBackWall(true);
                // Open entrance to top hallway (at z = -1, x = center)
                if (x == topRoomX + _roomWidth / 2 && z == topRoomZ + _roomDepth - 1)
                    cell.ClearFrontWall();
            }
        }

        // Create bottom room (attached to bottom hallway at z = _mazeDepth - 1)
        int bottomRoomZ = _mazeDepth; // Starts at z = _mazeDepth, extends to z = _mazeDepth + 1
        int bottomRoomX = _mazeWidth / 2 - _roomWidth / 2; // Center horizontally
        for (int x = bottomRoomX; x < bottomRoomX + _roomWidth; x++)
        {
            for (int z = bottomRoomZ; z < bottomRoomZ + _roomDepth; z++)
            {
                MazeCell cell = Instantiate(_mazeCellPrefab, new Vector3(x, 0, z), Quaternion.identity, _mazeParent.transform);
                cell.ClearAllWalls();
                // Set exterior walls
                if (x == bottomRoomX) cell.SetLeftWall(true);
                if (x == bottomRoomX + _roomWidth - 1) cell.SetRightWall(true);
                if (z == bottomRoomZ + _roomDepth - 1) cell.SetFrontWall(true);
                // Open entrance to bottom hallway (at z = _mazeDepth, x = center)
                if (x == bottomRoomX + _roomWidth / 2 && z == bottomRoomZ)
                    cell.ClearBackWall();
            }
        }
    }

    private IEnumerator GenerateMazeSections()
    {
        HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();

        // Generate the first section (top-left)
        Rect firstSection = _sections[0];
        yield return GenerateSection((int)firstSection.x, (int)firstSection.y, (int)firstSection.width, (int)firstSection.height, visitedCells);

        // Transform and copy the first section to others
        for (int i = 1; i < _sections.Count; i++)
        {
            Rect section = _sections[i];
            TransformSection((int)firstSection.x, (int)firstSection.y, (int)section.x, (int)section.y, (int)firstSection.width, (int)firstSection.height);
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

    private void TransformSection(int sourceStartX, int sourceStartZ, int destStartX, int destStartZ, int width, int depth)
    {
        bool flipX = false;
        bool rotate180 = false;

        // Determine transformation based on section position (assuming 2x2 grid)
        if (destStartX == 0 && destStartZ + depth == _mazeDepth)
        {
            // Bottom-left: Rotate 180° and flip left-to-right
            flipX = true;
            rotate180 = true;
        }
        else if (destStartX + width == _mazeWidth && destStartZ == 0)
        {
            // Top-right: Flip left-to-right
            flipX = true;
        }
        else if (destStartX + width == _mazeWidth && destStartZ + depth == _mazeDepth)
        {
            // Bottom-right: Rotate 180°
            rotate180 = true;
        }

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                // Source coordinates
                int srcX = sourceStartX + x;
                int srcZ = sourceStartZ + z;

                // Destination coordinates with transformation
                int destX = flipX ? destStartX + (width - 1 - x) : destStartX + x;
                int destZ = rotate180 ? destStartZ + (depth - 1 - z) : destStartZ + z;

                MazeCell sourceCell = _mazeGrid[srcX, srcZ];
                MazeCell destCell = _mazeGrid[destX, destZ];

                if (!sourceCell.IsVisited) continue;

                destCell.Visit();

                // Adjust walls based on transformation
                if (flipX && rotate180)
                {
                    // Bottom-left: Rotate 180° then flip left-to-right (combined effect)
                    if (!sourceCell._leftWall.activeSelf) destCell.ClearRightWall();
                    if (!sourceCell._rightWall.activeSelf) destCell.ClearLeftWall();
                    if (!sourceCell._frontWall.activeSelf) destCell.ClearBackWall();
                    if (!sourceCell._backWall.activeSelf) destCell.ClearFrontWall();
                }
                else if (flipX)
                {
                    // Top-right: Flip left-to-right
                    if (!sourceCell._leftWall.activeSelf) destCell.ClearRightWall();
                    if (!sourceCell._rightWall.activeSelf) destCell.ClearLeftWall();
                    if (!sourceCell._frontWall.activeSelf) destCell.ClearFrontWall();
                    if (!sourceCell._backWall.activeSelf) destCell.ClearBackWall();
                }
                else if (rotate180)
                {
                    // Bottom-right: Rotate 180°
                    if (!sourceCell._leftWall.activeSelf) destCell.ClearRightWall();
                    if (!sourceCell._rightWall.activeSelf) destCell.ClearLeftWall();
                    if (!sourceCell._frontWall.activeSelf) destCell.ClearBackWall();
                    if (!sourceCell._backWall.activeSelf) destCell.ClearFrontWall();
                }
                else
                {
                    // Top-left: No transformation (handled by generation)
                    if (!sourceCell._leftWall.activeSelf) destCell.ClearLeftWall();
                    if (!sourceCell._rightWall.activeSelf) destCell.ClearRightWall();
                    if (!sourceCell._frontWall.activeSelf) destCell.ClearFrontWall();
                    if (!sourceCell._backWall.activeSelf) destCell.ClearBackWall();
                }
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

// Extension method for shuffling lists
public static class ListExtensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}