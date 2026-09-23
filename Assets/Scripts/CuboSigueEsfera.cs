using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CuboSigueEsfera : MonoBehaviour
{
    [SerializeField]
    private Generator _generator; // Referencia al script que genera y expone el laberinto

    [SerializeField]
    private Transform _esfera; // Punto de destino (meta)

    [SerializeField]
    private Transform _cubo; // Objeto que se mueve (debe tener su propio Trail Renderer)

    [SerializeField]
    private float _velocidad = 5f;

    void Start()
    {
        StartCoroutine(EsperarYSeguir());
    }

    private IEnumerator EsperarYSeguir()
    {
        // Espera un frame para asegurarse de que Generator ya construyó el laberinto
        yield return null;

        if (_generator == null || _esfera == null || _cubo == null)
        {
            Debug.LogWarning("Faltan referencias en CuboSigueEsfera.");
            yield break;
        }

        MazeCell startCell = GetClosestCell(_cubo.position);
        MazeCell goalCell = GetClosestCell(_esfera.position);

        List<MazeCell> path = SolveMaze(startCell, goalCell);

        var trail = _cubo.GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.Clear();
        }

        yield return StartCoroutine(SeguirCaminoHaciaEsfera(path));
    }

    // ---------- Solver (BFS) usando el laberinto expuesto por Generator ----------

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

            foreach (var neighbor in _generator.GetConnectedNeighbors(current))
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

    // Encuentra la celda más cercana a una posición del mundo
    private MazeCell GetClosestCell(Vector3 worldPosition)
    {
        MazeCell[,] grid = _generator.MazeGrid;
        MazeCell closest = grid[0, 0];
        float minDist = float.MaxValue;

        foreach (var cell in grid)
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

    // ---------- Movimiento del cubo, celda por celda, respetando paredes ----------

    private IEnumerator SeguirCaminoHaciaEsfera(List<MazeCell> path)
    {
        foreach (var cell in path)
        {
            Vector3 target = cell.transform.position;
            target.y = _cubo.position.y; // mantiene la altura del cubo

            while (Vector3.Distance(_cubo.position, target) > 0.05f)
            {
                _cubo.position = Vector3.MoveTowards(
                    _cubo.position,
                    target,
                    _velocidad * Time.deltaTime
                );
                yield return null;
            }
        }

        // Al llegar, se ajusta exactamente a la posición de la esfera
        _cubo.position = _esfera.position;
    }
}