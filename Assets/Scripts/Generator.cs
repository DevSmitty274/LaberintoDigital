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

    [Header("Pelota y Cubo (se instancian automáticamente)")]
    [SerializeField]
    private Transform _esferaPrefab; // Prefab de la pelota

    [SerializeField]
    private Transform _cuboPrefab; // Prefab del cubo

    [SerializeField]
    private GameObject _trailPrefab; // Objeto vacío con TrailRenderer

    [SerializeField]
    private float _velocidad = 5f;

    [Header("Dificultad del cubo (en pasos dentro del laberinto)")]
    [SerializeField, Range(0f, 1f)]
    private float _minDificultad = 0.35f; // % del camino más largo posible

    [SerializeField, Range(0f, 1f)]
    private float _maxDificultad = 0.6f;

    private Transform _mazeContainer;
    private MazeCell[,] _mazeGrid;
    private Transform _trailObjeto;
    private Transform _esfera;
    private Transform _cubo;

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

        // --- Elegir celda central para la pelota ---
        int centerX = _mazeWidth / 2;
        int centerZ = _mazeDepth / 2;
        MazeCell startCell = _mazeGrid[centerX, centerZ];

        // --- BFS desde la celda central para medir distancias reales ---
        Dictionary<MazeCell, int> distancias = CalcularDistancias(startCell);

        // --- Elegir celda destino dentro de un rango de dificultad ---
        int maxDist = distancias.Values.Max();
        int minPasos = Mathf.RoundToInt(maxDist * _minDificultad);
        int maxPasos = Mathf.RoundToInt(maxDist * _maxDificultad);

        var candidatas = distancias
            .Where(kv => kv.Value >= minPasos && kv.Value <= maxPasos)
            .Select(kv => kv.Key)
            .ToList();

        // Fallback por si el rango queda vacío (laberintos muy chicos)
        MazeCell goalCell = candidatas.Count > 0
            ? candidatas[Random.Range(0, candidatas.Count)]
            : distancias.OrderByDescending(kv => kv.Value).First().Key;

        // --- Instanciar la pelota en el centro ---
        _esfera = Instantiate(_esferaPrefab, startCell.transform.position, Quaternion.identity);

        // --- Instanciar el cubo en la celda elegida ---
        _cubo = Instantiate(_cuboPrefab, goalCell.transform.position, Quaternion.identity);
        _cubo.SetParent(_floor, worldPositionStays: true);

        // --- Estela desde la celda de la esfera hasta la celda del cubo ---
        if (_esfera != null && _cubo != null && _trailPrefab != null)
        {
            List<MazeCell> path = SolveMaze(startCell, goalCell);

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
            if (cellToRight.IsVisited == false) yield return cellToRight;
        }

        if (x - 1 >= 0)
        {
            var cellToLeft = _mazeGrid[x - 1, z];
            if (cellToLeft.IsVisited == false) yield return cellToLeft;
        }

        if (z + 1 < _mazeDepth)
        {
            var cellToFront = _mazeGrid[x, z + 1];
            if (cellToFront.IsVisited == false) yield return cellToFront;
        }

        if (z - 1 >= 0)
        {
            var cellToBack = _mazeGrid[x, z - 1];
            if (cellToBack.IsVisited == false) yield return cellToBack;
        }
    }

    private void ClearWalls(MazeCell previousCell, MazeCell currentCell)
    {
        if (previousCell == null) return;

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

    // ---------- BFS: distancia (en pasos) desde una celda a todas las demás ----------
    private Dictionary<MazeCell, int> CalcularDistancias(MazeCell start)
    {
        var distancias = new Dictionary<MazeCell, int> { [start] = 0 };
        var queue = new Queue<MazeCell>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var neighbor in GetConnectedNeighbors(current))
            {
                if (!distancias.ContainsKey(neighbor))
                {
                    distancias[neighbor] = distancias[current] + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return distancias;
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
            if (current == goal) break;

            foreach (var neighbor in GetConnectedNeighbors(current))
            {
                if (visited.Contains(neighbor)) continue;

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

    public IEnumerable<MazeCell> GetConnectedNeighbors(MazeCell cell)
    {
        int x = (int)cell.transform.localPosition.x;
        int z = (int)cell.transform.localPosition.z;

        if (x + 1 < _mazeWidth && !cell.HasRightWall) yield return _mazeGrid[x + 1, z];
        if (x - 1 >= 0 && !cell.HasLeftWall) yield return _mazeGrid[x - 1, z];
        if (z + 1 < _mazeDepth && !cell.HasFrontWall) yield return _mazeGrid[x, z + 1];
        if (z - 1 >= 0 && !cell.HasBackWall) yield return _mazeGrid[x, z - 1];
    }

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

        _trailObjeto.position = _cubo.position;
    }
}