using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CuboSigueEsfera : MonoBehaviour
{
    [SerializeField]
    private Generator _generator; // Referencia al script que genera y expone el laberinto

    [SerializeField]
    private Transform _esfera; // Punto de INICIO del recorrido

    [SerializeField]
    private Transform _trail; // Objeto que se mueve, tiene el TrailRenderer

    [SerializeField]
    private Transform _metaFija; // Posición FIJA de la meta (guardada antes de mover el trail)

    [SerializeField]
    private float _velocidad = 5f;

    void Start()
    {
        var trailRenderer = _trail.GetComponent<TrailRenderer>();
        if (trailRenderer != null)
        {
            trailRenderer.textureMode = LineTextureMode.Tile;
        }

        StartCoroutine(EsperarYSeguir());
    }

    private IEnumerator EsperarYSeguir()
    {
        yield return null;

        if (_generator == null || _esfera == null || _trail == null || _metaFija == null)
        {
            Debug.LogWarning("Faltan referencias en CuboSigueEsfera.");
            yield break;
        }

        _trail.position = _esfera.position;

        MazeCell startCell = GetClosestCell(_esfera.position);
        MazeCell goalCell = GetClosestCell(_metaFija.position);

        List<MazeCell> path = SolveMaze(startCell, goalCell);

        var trailRenderer = _trail.GetComponent<TrailRenderer>();
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }

        yield return StartCoroutine(SeguirCaminoHaciaMeta(path));
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

    // ---------- Movimiento del trail, reiniciando pero conservando el largo previo ----------

    private IEnumerator SeguirCaminoHaciaMeta(List<MazeCell> path)
    {
        var trailRenderer = _trail.GetComponent<TrailRenderer>();

        foreach (var cell in path)
        {
            Vector3 target = cell.transform.position; // usa el Y real de la celda

            while (Vector3.Distance(_trail.position, target) > 0.05f)
            {
                _trail.position = Vector3.MoveTowards(
                    _trail.position,
                    target,
                    _velocidad * Time.deltaTime
                );

                yield return null;
            }

            // Al llegar a esta celda, se reinicia el trazo pero conservando el largo (cantidad de puntos)
            if (trailRenderer != null)
            {
                ReiniciarTrailConservandoLargo(trailRenderer);
            }
        }

        _trail.position = _metaFija.position;
    }

    // ---------- Borra el trazo pero lo re-llena con el mismo largo, en la posicion actual del cubo ----------

    private void ReiniciarTrailConservandoLargo(TrailRenderer trailRenderer)
    {
        int puntosPrevios = trailRenderer.positionCount;

        trailRenderer.Clear();

        if (puntosPrevios <= 0)
        {
            return; // no habia trazo previo, no hay nada que conservar
        }

        // Se crea un array con la misma cantidad de puntos que tenia antes,
        // todos ubicados en la posicion actual del cubo/trail
        Vector3[] nuevosPuntos = new Vector3[puntosPrevios];
        for (int i = 0; i < puntosPrevios; i++)
        {
            nuevosPuntos[i] = _trail.position;
        }

        // SetPositions ajusta positionCount automaticamente segun el largo del array
        trailRenderer.SetPositions(nuevosPuntos);
    }
}