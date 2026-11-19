using UnityEngine;
using UnityEngine.SceneManagement;

public class EndZone : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private GameObject canvasFinal;
    [SerializeField] private GameObject textoGanaste;
    [SerializeField] private GameObject textoPerdiste;
    [SerializeField] private GameObject botonSiguiente;
    [SerializeField] private GameObject botonReiniciar;
    [SerializeField] private GameObject imagenFondo; // "Image (1)"

    [Header("UI de juego")]
    [SerializeField] private CanvasGroup uiTiempoPuntos; // "UI de tiempo y puntos"

    [Header("Referencias")]
    [SerializeField] private Generator _generator;
    [SerializeField] private SceneFader _fader;
    [SerializeField] private Score _score;
    [SerializeField] private ResultadoFinal _resultado;
    [SerializeField] private CameraFOVIntro _camaraFOV;
    public MPU6050QuaternionReceiver mpu;

    private bool _finalMostrado = false;

    void Awake()
    {
        if (_fader == null)
            _fader = FindAnyObjectByType<SceneFader>(FindObjectsInactive.Include);
        if (_score == null)
            _score = FindAnyObjectByType<Score>(FindObjectsInactive.Include);
        if (_resultado == null)
            _resultado = FindAnyObjectByType<ResultadoFinal>(FindObjectsInactive.Include);
        if (_camaraFOV == null)
            _camaraFOV = FindAnyObjectByType<CameraFOVIntro>(FindObjectsInactive.Include);

        Debug.Log($"EndZone Awake -> fader:{_fader != null} score:{_score != null} resultado:{_resultado != null} camara:{_camaraFOV != null} uiJuego:{uiTiempoPuntos != null}", this);

        if (canvasFinal != null) canvasFinal.SetActive(false);
    }

    public void TiempoAgotado()
    {
        MostrarFinal(false);
    }

    public void MostrarFinal(bool gano)
    {
        if (_finalMostrado) return;
        _finalMostrado = true;

        Debug.Log("EndZone: MostrarFinal, gano = " + gano);

        MostrarUIJuego(false); // se oculta apenas termina el nivel
        CerrarMPU();

        // El zoom se abre de nuevo (vuelve al FOV inicial) mientras se cubre la pantalla
        if (_camaraFOV != null) _camaraFOV.ReturnToStart();

        if (_fader != null)
        {
            ActivarConPadres(_fader.gameObject);
            _fader.FadeOut(SceneFader.FadeType.Shutters, () => MostrarCanvas(gano));
        }
        else
        {
            MostrarCanvas(gano);
        }
    }

    void MostrarCanvas(bool gano)
    {
        Debug.Log("EndZone: MostrarCanvas (el fade terminó)");

        ActivarConPadres(canvasFinal);
        if (imagenFondo != null) imagenFondo.SetActive(true);
        if (textoGanaste != null) textoGanaste.SetActive(gano);
        if (textoPerdiste != null) textoPerdiste.SetActive(!gano);

        if (botonSiguiente != null) botonSiguiente.SetActive(false);
        if (botonReiniciar != null) botonReiniciar.SetActive(false);

        if (_resultado != null)
        {
            ActivarConPadres(_resultado.gameObject);
            int puntos = _score != null ? _score.Puntaje : 0;
            Debug.Log("EndZone: llamando a ResultadoFinal.Mostrar con puntaje " + puntos);
            _resultado.Mostrar(puntos, () => MostrarBotones(gano));
        }
        else
        {
            Debug.LogWarning("EndZone: no hay ResultadoFinal en la escena, se muestran los botones directo.");
            MostrarBotones(gano);
        }
    }

    void MostrarBotones(bool gano)
    {
        ActivarConPadres(botonReiniciar);
        botonSiguiente.SetActive(gano);
        botonReiniciar.SetActive(true);
        Debug.Log($"EndZone: botones visibles -> reiniciar:{botonReiniciar.activeInHierarchy} siguiente:{botonSiguiente.activeInHierarchy}");
    }

    void ActivarConPadres(GameObject go)
    {
        Transform t = go.transform;
        while (t != null)
        {
            t.gameObject.SetActive(true);
            t = t.parent;
        }
    }

    void CerrarMPU()
    {
        if (mpu != null) mpu.enabled = false; // detiene FixedUpdate, el socket sigue vivo
    }

    void MostrarUIJuego(bool visible)
    {
        if (uiTiempoPuntos == null)
        {
            Debug.LogError("EndZone: uiTiempoPuntos NO está asignado", this);
            return;
        }

        Debug.Log("EndZone: UI de juego alpha = " + (visible ? 1 : 0), this);
        uiTiempoPuntos.alpha = visible ? 1f : 0f;
        uiTiempoPuntos.interactable = visible;
        uiTiempoPuntos.blocksRaycasts = visible;
    }

    void OcultarUIFinal()
    {
        if (imagenFondo != null) imagenFondo.SetActive(false);
        if (textoGanaste != null) textoGanaste.SetActive(false);
        if (textoPerdiste != null) textoPerdiste.SetActive(false);
        if (botonSiguiente != null) botonSiguiente.SetActive(false);
        if (botonReiniciar != null) botonReiniciar.SetActive(false);
        if (canvasFinal != null) canvasFinal.SetActive(false);
    }

    public void SiguienteNivel()
    {
        Debug.Log("EndZone: CLICK en Siguiente");
        _finalMostrado = false;

        OcultarUIFinal(); // primero, antes de nada que pueda fallar
        if (_resultado != null)
        {
            _resultado.Ocultar();
            _resultado.gameObject.SetActive(false); // por si Ocultar() no lo desactiva
        }

        MostrarUIJuego(true); // la UI de tiempo y puntos vuelve

        Time.timeScale = 1f;
        _generator.GenerarNuevo();

        if (_camaraFOV != null) _camaraFOV.PlayIntro();

        if (_fader != null)
        {
            ActivarConPadres(_fader.gameObject);
            _fader.FadeIn(SceneFader.FadeType.Shutters, () => _fader.gameObject.SetActive(false));
        }
        if (mpu != null) mpu.enabled = true;
    }

    public void ReiniciarNivel()
    {
        Debug.Log("EndZone: CLICK en Reiniciar -> recargando escena " + SceneManager.GetActiveScene().name);
        Time.timeScale = 1f;
        MazeSaveSystem.MarcarReinicio();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}