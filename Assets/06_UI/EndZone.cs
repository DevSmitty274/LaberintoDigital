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

    [Header("Referencias")]
    [SerializeField] private Generator _generator;
    [SerializeField] private SceneFader _fader;
    [SerializeField] private Score _score;
    [SerializeField] private ResultadoFinal _resultado;
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

        Debug.Log($"EndZone Awake -> fader:{_fader != null} score:{_score != null} resultado:{_resultado != null}");

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
        CerrarMPU();

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
        if (textoGanaste != null) textoGanaste.SetActive(gano);
        if (textoPerdiste != null) textoPerdiste.SetActive(!gano);

        botonSiguiente.SetActive(false);
        botonReiniciar.SetActive(false);

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
        botonSiguiente.SetActive(gano);
        botonReiniciar.SetActive(true);
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
        try
        {
            if (mpu != null)
            {
                mpu.running = false;
                mpu.udpClient?.Close();
                if (mpu.receiveThread != null && mpu.receiveThread.IsAlive)
                    mpu.receiveThread.Join(200);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Error cerrando MPU: " + e.Message);
        }
    }

    public void SiguienteNivel()
    {
        _finalMostrado = false;
        if (_resultado != null) _resultado.Ocultar();
        canvasFinal.SetActive(false);
        Time.timeScale = 1f;
        _generator.GenerarNuevo();

        if (_fader != null)
            _fader.FadeIn(SceneFader.FadeType.Shutters, () => _fader.gameObject.SetActive(false));
    }

    public void ReiniciarNivel()
    {
        Time.timeScale = 1f;
        MazeSaveSystem.MarcarReinicio();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}