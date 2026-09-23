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

    [Header("Estela / Solución")]
    [SerializeField]
    private Transform _cube; // objeto que recorre el camino (debe tener un Trail Renderer)

    [SerializeField]
    private Transform _sphere; // meta

    [SerializeField]
    private float _trailSpeed = 5f;

    [SerializeField]
    private Vector3 _sphereOffset = Vector3.zero; // ajusta en el Inspector para centrar la esfera en la celda

    [Header("Segundo cubo (solo trail, en loop)")]
    [SerializeField]
    private Transform _trailCube; // objeto duplicado, sin Mesh Renderer, con Trail Renderer

    [SerializeField]
    private float _trailCooldown = 2f; // segundos que el trail queda apagado al llegar

    private Transform _mazeContainer;
    private MazeCell[,] _mazeGrid;

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

        // --- Resolver el laberinto y animar el cubo dejando la estela ---
        MazeCell startCell = _mazeGrid[0, 0];
        MazeCell goalCell = _mazeGrid[_mazeWidth - 1, _mazeDepth - 1];

        if (_cube != null)
        {
            var trail = _cube.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.Clear();
            }

            if (_sphere != null)
            {
                _sphere.position = goalCell.transform.position + _sphereOffset;
            }

            // El cubo nace en la celda meta (junto a la esfera) y viaja hacia la
            // celda inicial, así la estela queda dibujada desde la esfera hacia el cubo.
            _cube.position = goalCell.transform.position;

            List<MazeCell> solution = SolveMaze(startCell, goalCell);
            solution.Reverse();
            StartCoroutine(MoveAlongPath(solution));
        }

        if (_trailCube != null)
        {
            // IMPORTANTE: SolveMaze() propio, para no compartir la misma List<MazeCell>
            // (y por lo tanto el mismo Reverse()) con el _cube principal.
            List<MazeCell> loopPath = SolveMaze(startCell, goalCell);
            StartCoroutine(LoopTrailCube(loopPath));
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

    private IEnumerable<MazeCell> GetConnectedNeighbors(MazeCell cell)
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

    // ---------- Movimiento del cubo (genera la estela vía TrailRenderer) ----------

    private IEnumerator MoveAlongPath(List<MazeCell> path)
    {
        foreach (var cell in path)
        {
            Vector3 target = cell.transform.position;
            target.y = _cube.position.y; // mantiene la altura del cubo

            while (Vector3.Distance(_cube.position, target) > 0.01f)
            {
                _cube.position = Vector3.MoveTowards(_cube.position, target, _trailSpeed * Time.deltaTime);
                yield return null;
            }
        }
    }

    // ---------- Segundo cubo (solo trail): recorre el camino en bucle ----------
    // Al llegar, apaga la emision y limpia el TrailRenderer ANTES de reposicionar,
    // asi no queda una linea recta desde el punto final hasta el nuevo inicio.

    private IEnumerator LoopTrailCube(List<MazeCell> path)
    {
        var trail = _trailCube.GetComponent<TrailRenderer>();

        while (true)
        {
            // Reposiciona al inicio del camino con el trail apagado y limpio.
            if (trail != null)
            {
                trail.emitting = false;
                trail.Clear();
            }

            _trailCube.position = path[0].transform.position;

            yield return null; // deja que el Clear() surta efecto antes de mover

            if (trail != null)
            {
                trail.emitting = true;
            }

            // Recorre el camino, celda por celda.
            foreach (var cell in path)
            {
                Vector3 target = cell.transform.position;
                target.y = _trailCube.position.y;

                while (Vector3.Distance(_trailCube.position, target) > 0.01f)
                {
                    _trailCube.position = Vector3.MoveTowards(
                        _trailCube.position,
                        target,
                        _trailSpeed * Time.deltaTime
                    );
                    yield return null;
                }
            }

            // Llego al final: apaga y limpia el trail, y espera antes de reiniciar.
            if (trail != null)
            {
                trail.emitting = false;
                trail.Clear();
            }

            yield return new WaitForSeconds(_trailCooldown);
        }
    }
}