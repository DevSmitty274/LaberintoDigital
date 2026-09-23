using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Generator : MonoBehaviour
{
    [SerializeField]
    private MazeCell _mazeCellPrefab;

    [SerializeField]
    private int _mazeWidth;

    [SerializeField]
    private int _mazeDepth;

    [SerializeField]
    private Transform _floor; // Suelo ya ubicado en la escena

    [SerializeField]
    private Vector3 _mazeOffset = Vector3.zero;

    [Header("Estela entre esfera y cubo")]
    [SerializeField]
    private Transform _esfera; // Punto de partida

    [SerializeField]
    private Transform _cubo; // Punto de destino

    [SerializeField]
    private GameObject _trailPrefab; // Objeto vacío con TrailRenderer

    [SerializeField]
    private float _velocidad = 5f;

    private Transform _mazeContainer;
    private MazeCell[,] _mazeGrid;
    private Transform _trailObjeto;

    // --- Exposición pública para scripts externos (ej. CuboSigueEsfera) ---
    public MazeCell[,] MazeGrid => _mazeGrid;
    public int MazeWidth => _mazeWidth;
    public int MazeDepth => _mazeDepth;

    void Start()
    {
        // Creamos un contenedor vacío, hijo del suelo, que compensa su escala
        GameObject containerObj = new GameObject("MazeContainer");
        _mazeContainer = containerObj.transform;
        _mazeContainer.SetParent(_floor, worldPositionStays: false);
        _mazeContainer.localPosition = Vector3.zero;
        _mazeContainer.localRotation = Quaternion.identity;

        // Contrarrestamos la escala del suelo para que el laberinto no se deforme
        Vector3 floorScale = _floor.lossyScale;
        _mazeContainer.localScale = new Vector3(
            1f / floorScale.x,
            1f / floorScale.y,
            1f / floorScale.z
        );

        _mazeGrid = new MazeCell[_mazeWidth, _mazeDepth];

        for (int x = 0; x < _mazeWidth; x++)
        {
            for (int z = 0; z < _mazeDepth; z++)
            {
                _mazeGrid[x, z] = Instantiate(
                    _mazeCellPrefab,
                    Vector3.zero,
                    Quaternion.identity,
                    _mazeContainer
                );

                // Posición LOCAL dentro del contenedor (ya no del suelo directamente)
                _mazeGrid[x, z].transform.localPosition = new Vector3(x, 0, z);
            }
        }

        GenerateMaze(null, _mazeGrid[0, 0]);
        _mazeContainer.localPosition = _mazeOffset;

        // --- Estela desde la celda de la esfera hasta la celda del cubo ---
        if (_esfera != null && _cubo != null && _trailPrefab != null)
        {
            MazeCell startCell = GetClosestCell(_esfera.position);
            MazeCell goalCell = GetClosestCell(_cubo.position);

            List<MazeCell> path = SolveMaze(startCell, goalCell);

            // Instancia el objeto que llevará la estela, en la posición de la esfera
            GameObject instancia = Instantiate(
                _trailPrefab,
                _esfera.position,
                Quaternion.identity
            );

            _trailObjeto = instancia.transform;

            _trailObjeto.SetParent(_mazeContainer, worldPositionStays: true);

            StartCoroutine(SeguirCaminoHaciaCubo(path));
        }
    }

    private void GenerateMaze(MazeCell previousCell, MazeCell currentCell)
    {
        currentCell.Visit();
        ClearWalls(previousCell, currentCell);

        MazeCell nextCell;

        do
        {
            nextCell = GetNextUnvisitedCell(currentCell);

            if (nextCell != null)
            {
                GenerateMaze(currentCell, nextCell);
            }
        } while (nextCell != null);
    }

    private MazeCell GetNextUnvisitedCell(MazeCell currentCell)
    {
        var unvisitedCells = GetUnvisitedCells(currentCell);

        return unvisitedCells.OrderBy(_ => Random.Range(1, 10)).FirstOrDefault();
    }

    private IEnumerable<MazeCell> GetUnvisitedCells(MazeCell currentCell)
    {
        int x = (int)currentCell.transform.localPosition.x;
        int z = (int)currentCell.transform.localPosition.z;

        if (x + 1 < _mazeWidth)
        {
            var cellToRight = _mazeGrid[x + 1, z];

            if (cellToRight.IsVisited == false)
            {
                yield return cellToRight;
            }
        }

        if (x - 1 >= 0)
        {
            var cellToLeft = _mazeGrid[x - 1, z];

            if (cellToLeft.IsVisited == false)
            {
                yield return cellToLeft;
            }
        }

        if (z + 1 < _mazeDepth)
        {
            var cellToFront = _mazeGrid[x, z + 1];

            if (cellToFront.IsVisited == false)
            {
                yield return cellToFront;
            }
        }

        if (z - 1 >= 0)
        {
            var cellToBack = _mazeGrid[x, z - 1];

            if (cellToBack.IsVisited == false)
            {
                yield return cellToBack;
            }
        }
    }

    private void ClearWalls(MazeCell previousCell, MazeCell currentCell)
    {
        if (previousCell == null)
        {
            return;
        }

        if (previousCell.transform.localPosition.x < currentCell.transform.localPosition.x)
        {
            previousCell.ClearRightWall();
            currentCell.ClearLeftWall();
            return;
        }

        if (previousCell.transform.localPosition.x > currentCell.transform.localPosition.x)
        {
            previousCell.ClearLeftWall();
            currentCell.ClearRightWall();
            return;
        }

        if (previousCell.transform.localPosition.z < currentCell.transform.localPosition.z)
        {
            previousCell.ClearFrontWall();
            currentCell.ClearBackWall();
            return;
        }

        if (previousCell.transform.localPosition.z > currentCell.transform.localPosition.z)
        {
            previousCell.ClearBackWall();
            currentCell.ClearFrontWall();
            return;
        }
    }

    // ---------- Solver (BFS) sobre el laberinto ya generado ----------

    private List<MazeCell> SolveMaze(MazeCell start, MazeCell goal)
    {
        var visited = new HashSet<MazeCell>();
        var cameFrom = new Dictionary<MazeCell, MazeCell>();
        var queue = new Queue<MazeCell>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == goal)
            {
                break;
            }

            foreach (var neighbor in GetConnectedNeighbors(current))
            {
                if (visited.Contains(neighbor))
                {
                    continue;
                }

                visited.Add(neighbor);
                cameFrom[neighbor] = current;
                queue.Enqueue(neighbor);
            }
        }

        var path = new List<MazeCell> { goal };
        var node = goal;

        while (node != start)
        {
            node = cameFrom[node];
            path.Add(node);
        }

        path.Reverse();
        return path;
    }

    // --- Expuesto como public para que CuboSigueEsfera.cs pueda usarlo ---
    public IEnumerable<MazeCell> GetConnectedNeighbors(MazeCell cell)
    {
        int x = (int)cell.transform.localPosition.x;
        int z = (int)cell.transform.localPosition.z;

        if (x + 1 < _mazeWidth && !cell.HasRightWall)
        {
            yield return _mazeGrid[x + 1, z];
        }

        if (x - 1 >= 0 && !cell.HasLeftWall)
        {
            yield return _mazeGrid[x - 1, z];
        }

        if (z + 1 < _mazeDepth && !cell.HasFrontWall)
        {
            yield return _mazeGrid[x, z + 1];
        }

        if (z - 1 >= 0 && !cell.HasBackWall)
        {
            yield return _mazeGrid[x, z - 1];
        }
    }

    // Encuentra la celda más cercana a una posición del mundo (para ubicar
    // en qué celda del grid caen la esfera y el cubo)
    private MazeCell GetClosestCell(Vector3 worldPosition)
    {
        MazeCell closest = _mazeGrid[0, 0];
        float minDist = float.MaxValue;

        foreach (var cell in _mazeGrid)
        {
            float dist = Vector3.Distance(cell.transform.position, worldPosition);
            if (dist < minDist)
            {
                minDist = dist;
                closest = cell;
            }
        }

        return closest;
    }

    // ---------- Movimiento del objeto de estela, celda por celda, respetando paredes ----------

    private IEnumerator SeguirCaminoHaciaCubo(List<MazeCell> path)
    {
        foreach (var cell in path)
        {
            Vector3 target = cell.transform.position;
            target.y = _trailObjeto.position.y;

            while (Vector3.Distance(_trailObjeto.position, target) > 0.05f)
            {
                _trailObjeto.position = Vector3.MoveTowards(
                    _trailObjeto.position,
                    target,
                    _velocidad * Time.deltaTime
                );
                yield return null;
            }
        }

        // Al llegar al cubo, se detiene y la estela queda dibujada
        _trailObjeto.position = _cubo.position;
    }
}