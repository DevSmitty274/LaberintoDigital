using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CilindroSigueEsfera : MonoBehaviour
{
    [SerializeField]
    private Generator _generator; // Referencia al script que genera y expone el laberinto

    [SerializeField]
    private Transform _esfera; // Punto de INICIO del recorrido

    [SerializeField]
    private Transform _punteroPrefab; // Prefab del cual se instancia el clon que recorre el camino

    [SerializeField]
    private Transform _metaFija; // Posición FIJA de la meta

    [SerializeField]
    private float _velocidad = 5f;

    [Header("Configuracion del cilindro")]
    [SerializeField]
    private float _radio = 0.2f;

    [SerializeField]
    private int _segmentosCirculo = 8;

    [SerializeField]
    private float _distanciaMinimaNuevoAnillo = 0.1f;

    private MeshFilter _meshFilter;
    private Mesh _mesh;

    private Transform _puntero; // instancia/clon creada en tiempo de ejecucion

    private List<Vector3> _puntosCentro = new List<Vector3>();
    private List<Vector3> _vertices = new List<Vector3>();
    private List<int> _triangulos = new List<int>();

    void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _mesh = new Mesh();
        _mesh.name = "CilindroCamino";
        _meshFilter.mesh = _mesh;
    }

    void Start()
    {
        StartCoroutine(EsperarYSeguir());
    }

    private IEnumerator EsperarYSeguir()
    {
        yield return null;

        if (_generator == null || _esfera == null || _punteroPrefab == null || _metaFija == null)
        {
            Debug.LogWarning("Faltan referencias en CilindroSigueEsfera.");
            yield break;
        }

        // Se instancia un CLON del prefab, en vez de usar un objeto ya puesto en la escena
        _puntero = Instantiate(_punteroPrefab, _esfera.position, Quaternion.identity);

        // Reinicia el mesh por si se vuelve a llamar
        _puntosCentro.Clear();
        _vertices.Clear();
        _triangulos.Clear();
        _mesh.Clear();

        AgregarAnillo(_puntero.position);

        MazeCell startCell = GetClosestCell(_esfera.position);
        MazeCell goalCell = GetClosestCell(_metaFija.position);

        List<MazeCell> path = SolveMaze(startCell, goalCell);

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

    // ---------- Movimiento del puntero (clon), agregando anillos al mesh a medida que avanza ----------

    private IEnumerator SeguirCaminoHaciaMeta(List<MazeCell> path)
    {
        foreach (var cell in path)
        {
            Vector3 target = cell.transform.position;

            while (Vector3.Distance(_puntero.position, target) > 0.05f)
            {
                _puntero.position = Vector3.MoveTowards(
                    _puntero.position,
                    target,
                    _velocidad * Time.deltaTime
                );

                Vector3 ultimoCentro = _puntosCentro[_puntosCentro.Count - 1];
                if (Vector3.Distance(_puntero.position, ultimoCentro) >= _distanciaMinimaNuevoAnillo)
                {
                    AgregarAnillo(_puntero.position);
                }

                yield return null;
            }
        }

        _puntero.position = _metaFija.position;
        AgregarAnillo(_puntero.position);
    }

    // ---------- Construccion incremental del mesh tipo tubo ----------

    private void AgregarAnillo(Vector3 centro)
    {
        _puntosCentro.Add(centro);

        Vector3 direccion;
        if (_puntosCentro.Count == 1)
        {
            direccion = Vector3.forward;
        }
        else
        {
            direccion = (centro - _puntosCentro[_puntosCentro.Count - 2]).normalized;
        }

        Quaternion rotacionAnillo = Quaternion.LookRotation(direccion);

        int indiceBaseAnillo = _vertices.Count;

        for (int i = 0; i < _segmentosCirculo; i++)
        {
            float angulo = (i / (float)_segmentosCirculo) * Mathf.PI * 2f;
            Vector3 offsetLocal = new Vector3(Mathf.Cos(angulo), Mathf.Sin(angulo), 0f) * _radio;
            Vector3 offsetMundo = rotacionAnillo * offsetLocal;

            _vertices.Add(centro + offsetMundo);
        }

        if (_puntosCentro.Count > 1)
        {
            int indiceAnilloAnterior = indiceBaseAnillo - _segmentosCirculo;

            for (int i = 0; i < _segmentosCirculo; i++)
            {
                int siguiente = (i + 1) % _segmentosCirculo;

                int a = indiceAnilloAnterior + i;
                int b = indiceAnilloAnterior + siguiente;
                int c = indiceBaseAnillo + i;
                int d = indiceBaseAnillo + siguiente;

                _triangulos.Add(a);
                _triangulos.Add(c);
                _triangulos.Add(b);

                _triangulos.Add(b);
                _triangulos.Add(c);
                _triangulos.Add(d);
            }
        }

        ActualizarMesh();
    }

    private void ActualizarMesh()
    {
        _mesh.Clear();
        _mesh.SetVertices(_vertices);
        _mesh.SetTriangles(_triangulos, 0);
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }
}