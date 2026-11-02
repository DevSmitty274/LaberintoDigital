using UnityEngine;
using UnityEngine.Rendering;
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

    [Header("Tubo de la solución (se crea automáticamente)")]
    [SerializeField]
    private bool _mostrarSolucion = true;

    [SerializeField]
    private Material _materialTubo;

    [SerializeField]
    private float _velocidadTubo = 5f; // celdas por segundo

    [SerializeField]
    private float _radioTubo = 0.1f;

    [SerializeField]
    private float _alturaTubo = 0f; // altura sobre el piso de la celda

    [SerializeField, Min(3)]
    private int _segmentosTubo = 8;

    [SerializeField]
    private float _distanciaMinimaAnillo = 0.1f;

    [SerializeField, Min(0f)]
    private float _segundosAntesDeDesaparecer = 3f; // tiempo visible tras terminar de dibujarse

    [Header("Dificultad del cubo (en pasos dentro del laberinto)")]
    [SerializeField, Range(0f, 1f)]
    private float _minDificultad = 0.35f; // % del camino más largo posible

    [SerializeField, Range(0f, 1f)]
    private float _maxDificultad = 0.6f;

    [Header("Objetos coleccionables (puntaje)")]
    [SerializeField]
    private Coleccionable _coleccionablePrefab;

    [SerializeField, Min(0)]
    private int _cantidadColeccionables = 5;

    [SerializeField]
    private float _alturaColeccionable = 0.3f; // altura sobre el piso de la celda

    private Transform _mazeContainer;
    private MazeCell[,] _mazeGrid;
    private Transform _esfera;
    private Transform _cubo;

    // Datos del tubo
    private GameObject _tuboObjeto;
    private Mesh _meshTubo;
    private Vector3 _direccionInicialTubo = Vector3.forward;
    private readonly List<Vector3> _puntosTubo = new List<Vector3>();
    private readonly List<Vector3> _verticesTubo = new List<Vector3>();
    private readonly List<int> _triangulosTubo = new List<int>();

    // --- Exposición pública para scripts externos ---
    public MazeCell[,] MazeGrid => _mazeGrid;
    public int MazeWidth => _mazeWidth;
    public int MazeDepth => _mazeDepth;
    public Transform MazeContainer => _mazeContainer;
    public Transform Esfera => _esfera;
    public Transform Cubo => _cubo;

    // Se dispara cada vez que el laberinto se (re)genera.
    public event System.Action OnMazeGenerated;

    private MazeData _datosActuales;
    private Coroutine _corrutinaTubo;
    private readonly List<Coleccionable> _coleccionables = new List<Coleccionable>();

    // true si la última generación fue un reinicio del mismo laberinto (botón Reiniciar)
    public bool EsReinicio { get; private set; }

    void Start()
    {
        CrearContenedor();

        // Compatibilidad: si la escena se recargó con MarcarReinicio(), se reutiliza el laberinto guardado
        bool hayDatos = MazeSaveSystem.TryLoadParaReinicio(out MazeData data);
        Construir(hayDatos ? data : null);
    }

    // ================= API pública (para los botones) =================

    // Genera un laberinto nuevo SIN recargar la escena
    public void GenerarNuevo()
    {
        EsReinicio = false;
        Construir(null);
    }

    // Reconstruye el MISMO laberinto SIN recargar la escena
    public void ReiniciarMismoLaberinto()
    {
        EsReinicio = true;
        Construir(_datosActuales);
    }

    // ================= Construcción =================

    private void CrearContenedor()
    {
        GameObject containerObj = new GameObject("MazeContainer");
        _mazeContainer = containerObj.transform;
        _mazeContainer.SetParent(_floor, worldPositionStays: false);
        _mazeContainer.localPosition = Vector3.zero;
        _mazeContainer.localRotation = Quaternion.identity;

        Vector3 floorScale = _floor.lossyScale;
        _mazeContainer.localScale = new Vector3(
            1f / floorScale.x,
            1f / floorScale.y,
            1f / floorScale.z
        );
    }

    private void LimpiarLaberinto()
    {
        if (_corrutinaTubo != null)
        {
            StopCoroutine(_corrutinaTubo);
            _corrutinaTubo = null;
        }
        BorrarTubo();
        LimpiarColeccionables();

        if (_mazeGrid != null)
        {
            foreach (MazeCell celda in _mazeGrid)
            {
                if (celda == null) continue;
                celda.gameObject.SetActive(false);
                Destroy(celda.gameObject);
            }
        }
    }

    private void Construir(MazeData data)
    {
        LimpiarLaberinto();

        bool hayDatos = data != null && data.width == _mazeWidth && data.depth == _mazeDepth;

        int seed = hayDatos ? data.seed : Random.Range(int.MinValue, int.MaxValue);
        Random.InitState(seed);

        _mazeContainer.localPosition = Vector3.zero;
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

                _mazeGrid[x, z].transform.localPosition = new Vector3(x, 0, z);
            }
        }

        GenerateMaze(null, _mazeGrid[0, 0]);
        _mazeContainer.localPosition = _mazeOffset;

        int centerX = _mazeWidth / 2;
        int centerZ = _mazeDepth / 2;
        MazeCell startCell = _mazeGrid[centerX, centerZ];

        MazeCell goalCell;

        if (hayDatos)
        {
            goalCell = _mazeGrid[data.goalX, data.goalZ];
            _datosActuales = data;
        }
        else
        {
            Dictionary<MazeCell, int> distancias = CalcularDistancias(startCell);

            int maxDist = distancias.Values.Max();
            int minPasos = Mathf.RoundToInt(maxDist * _minDificultad);
            int maxPasos = Mathf.RoundToInt(maxDist * _maxDificultad);

            var candidatas = distancias
                .Where(kv => kv.Value >= minPasos && kv.Value <= maxPasos)
                .Select(kv => kv.Key)
                .ToList();

            goalCell = candidatas.Count > 0
                ? candidatas[Random.Range(0, candidatas.Count)]
                : distancias.OrderByDescending(kv => kv.Value).First().Key;

            _datosActuales = new MazeData
            {
                seed = seed,
                width = _mazeWidth,
                depth = _mazeDepth,
                goalX = (int)goalCell.transform.localPosition.x,
                goalZ = (int)goalCell.transform.localPosition.z
            };
            MazeSaveSystem.Save(_datosActuales);
        }

        // --- Pelota ---
        if (_esfera == null)
        {
            _esfera = Instantiate(_esferaPrefab, startCell.transform.position, Quaternion.identity);
        }
        else
        {
            _esfera.position = startCell.transform.position;

            Rigidbody rb = _esfera.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        // --- Cubo ---
        if (_cubo == null)
        {
            _cubo = Instantiate(_cuboPrefab, goalCell.transform.position, Quaternion.identity);
            _cubo.SetParent(_floor, worldPositionStays: true);
        }
        else
        {
            _cubo.position = goalCell.transform.position;
        }

        // --- Tubo de la solución: se dibuja solo al generar el laberinto ---
        if (_mostrarSolucion)
        {
            List<MazeCell> path = SolveMaze(startCell, goalCell);
            CrearTubo();
            _corrutinaTubo = StartCoroutine(DibujarTubo(path));
        }

        GenerarColeccionables(seed, startCell, goalCell);

        OnMazeGenerated?.Invoke();
    }

    // ================= Coleccionables =================

    private void GenerarColeccionables(int seed, MazeCell startCell, MazeCell goalCell)
    {
        if (_coleccionablePrefab == null || _cantidadColeccionables <= 0) return;

        var libres = new List<MazeCell>();
        foreach (MazeCell celda in _mazeGrid)
        {
            if (celda != startCell && celda != goalCell) libres.Add(celda);
        }

        var rng = new System.Random(seed + 7919);
        for (int i = libres.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (libres[i], libres[j]) = (libres[j], libres[i]);
        }

        int cantidad = Mathf.Min(_cantidadColeccionables, libres.Count);

        for (int i = 0; i < cantidad; i++)
        {
            Coleccionable item = Instantiate(_coleccionablePrefab, _mazeContainer);
            item.transform.localPosition = libres[i].transform.localPosition + Vector3.up * _alturaColeccionable;
            item.transform.localRotation = Quaternion.identity;
            item.Configurar(_esfera);
            _coleccionables.Add(item);
        }
    }

    private void LimpiarColeccionables()
    {
        foreach (Coleccionable item in _coleccionables)
        {
            if (item == null) continue;
            item.gameObject.SetActive(false);
            Destroy(item.gameObject);
        }
        _coleccionables.Clear();
    }

    // ================= Generación del laberinto =================

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

    // ================= Búsqueda de caminos (BFS) =================

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

    // ================= NUEVO: redibujar el tubo desde la pelota =================

    // Lo llama Score al recoger un coleccionable.
    // Borra el tubo actual y dibuja uno nuevo desde la celda de la pelota hasta la del cubo.
    public void DibujarSolucion()
    {
        if (!_mostrarSolucion || _esfera == null || _cubo == null || _mazeGrid == null) return;

        MazeCell inicio = CeldaMasCercana(_esfera.position);
        MazeCell meta = CeldaMasCercana(_cubo.position);
        if (inicio == null || meta == null) return;

        if (_corrutinaTubo != null)
        {
            StopCoroutine(_corrutinaTubo);
            _corrutinaTubo = null;
        }
        BorrarTubo();

        List<MazeCell> path = SolveMaze(inicio, meta);
        CrearTubo();
        _corrutinaTubo = StartCoroutine(DibujarTubo(path));
    }

    private MazeCell CeldaMasCercana(Vector3 posicionMundo)
    {
        MazeCell masCercana = null;
        float minDist = float.MaxValue;

        foreach (MazeCell celda in _mazeGrid)
        {
            if (celda == null) continue;
            float dist = (celda.transform.position - posicionMundo).sqrMagnitude;
            if (dist < minDist)
            {
                minDist = dist;
                masCercana = celda;
            }
        }

        return masCercana;
    }

    // ================= Tubo de la solución =================

    private void CrearTubo()
    {
        GameObject tubo = new GameObject("CaminoSolucion");
        _tuboObjeto = tubo;
        tubo.transform.SetParent(_mazeContainer, worldPositionStays: false);
        tubo.transform.localPosition = Vector3.zero;
        tubo.transform.localRotation = Quaternion.identity;
        tubo.transform.localScale = Vector3.one;

        _meshTubo = new Mesh();
        _meshTubo.name = "CaminoSolucion";
        _meshTubo.indexFormat = IndexFormat.UInt32;

        tubo.AddComponent<MeshFilter>().mesh = _meshTubo;
        MeshRenderer renderer = tubo.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _materialTubo;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    private Vector3 PuntoTubo(MazeCell celda)
    {
        return celda.transform.localPosition + Vector3.up * _alturaTubo;
    }

    private IEnumerator DibujarTubo(List<MazeCell> path)
    {
        Vector3 punta = PuntoTubo(path[0]);

        _direccionInicialTubo = path.Count > 1
            ? (PuntoTubo(path[1]) - punta).normalized
            : Vector3.forward;

        AgregarAnillo(punta);

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 target = PuntoTubo(path[i]);

            while (punta != target)
            {
                punta = Vector3.MoveTowards(punta, target, _velocidadTubo * Time.deltaTime);

                Vector3 ultimo = _puntosTubo[_puntosTubo.Count - 1];
                if (punta == target || Vector3.Distance(punta, ultimo) >= _distanciaMinimaAnillo)
                {
                    AgregarAnillo(punta);
                }

                yield return null;
            }
        }

        yield return new WaitForSeconds(_segundosAntesDeDesaparecer);
        BorrarTubo();
    }

    private void BorrarTubo()
    {
        if (_tuboObjeto != null)
        {
            Destroy(_tuboObjeto);
            _tuboObjeto = null;
        }

        if (_meshTubo != null)
        {
            Destroy(_meshTubo);
            _meshTubo = null;
        }

        _puntosTubo.Clear();
        _verticesTubo.Clear();
        _triangulosTubo.Clear();
    }

    private void AgregarAnillo(Vector3 centro)
    {
        _puntosTubo.Add(centro);

        Vector3 direccion = _direccionInicialTubo;
        if (_puntosTubo.Count > 1)
        {
            Vector3 d = centro - _puntosTubo[_puntosTubo.Count - 2];
            if (d.sqrMagnitude > 0.000001f)
            {
                direccion = d.normalized;
            }
        }

        Quaternion rotacionAnillo = Quaternion.LookRotation(direccion);
        int indiceBase = _verticesTubo.Count;

        for (int i = 0; i < _segmentosTubo; i++)
        {
            float angulo = (i / (float)_segmentosTubo) * Mathf.PI * 2f;
            Vector3 offset = new Vector3(Mathf.Cos(angulo), Mathf.Sin(angulo), 0f) * _radioTubo;
            _verticesTubo.Add(centro + rotacionAnillo * offset);
        }

        if (_puntosTubo.Count > 1)
        {
            int indiceAnterior = indiceBase - _segmentosTubo;

            for (int i = 0; i < _segmentosTubo; i++)
            {
                int siguiente = (i + 1) % _segmentosTubo;

                int a = indiceAnterior + i;
                int b = indiceAnterior + siguiente;
                int c = indiceBase + i;
                int d = indiceBase + siguiente;

                _triangulosTubo.Add(a);
                _triangulosTubo.Add(b);
                _triangulosTubo.Add(c);

                _triangulosTubo.Add(b);
                _triangulosTubo.Add(d);
                _triangulosTubo.Add(c);
            }
        }

        _meshTubo.Clear();
        _meshTubo.SetVertices(_verticesTubo);
        _meshTubo.SetTriangles(_triangulosTubo, 0);
        _meshTubo.RecalculateNormals();
        _meshTubo.RecalculateBounds();
    }
}