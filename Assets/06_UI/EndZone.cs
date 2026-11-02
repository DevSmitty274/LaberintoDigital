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
    public MPU6050QuaternionReceiver mpu;

    private bool _finalMostrado = false;

    void Awake()
    {
        if (_fader == null)
            _fader = FindAnyObjectByType<SceneFader>(FindObjectsInactive.Include);

        if (canvasFinal != null) canvasFinal.SetActive(false);
    }

    // Lo llama el Timer cuando el tiempo llega a 0
    public void TiempoAgotado()
    {
        MostrarFinal(false);
    }

    // Lo llama ZonaTrigger al entrar la pelota
    public void MostrarFinal(bool gano)
    {
        if (_finalMostrado) return;
        _finalMostrado = true;

        CerrarMPU();

        if (_fader != null)
        {
            ActivarConPadres(_fader.gameObject);
            // La pantalla se cubre y SE QUEDA cubierta
            _fader.FadeOut(SceneFader.FadeType.Shutters, () => MostrarCanvas(gano));
        }
        else
        {
            MostrarCanvas(gano);
        }
    }

    void MostrarCanvas(bool gano)
    {
        ActivarConPadres(canvasFinal);
        textoGanaste.SetActive(gano);
        textoPerdiste.SetActive(!gano);
        botonSiguiente.SetActive(gano);
        botonReiniciar.SetActive(true);
        // Sin FadeIn aquí: el fader se queda cubriendo.
        // El canvas final debe tener un Sort Order mayor que el del fader.
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
        canvasFinal.SetActive(false);
        Time.timeScale = 1f;
        _generator.GenerarNuevo();

        // Aquí sí se destapa la pantalla
        if (_fader != null)
            _fader.FadeIn(SceneFader.FadeType.Shutters, () => _fader.gameObject.SetActive(false));

        // TODO: reabrir la conexión del MPU (necesito MPU6050QuaternionReceiver)
    }

    // Conecta el botón Reiniciar directo a este método
    public void ReiniciarNivel()
    {
        Time.timeScale = 1f;
        MazeSaveSystem.MarcarReinicio();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}