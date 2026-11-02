using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    public MPU6050QuaternionReceiver mpuRecibidor;

    [Header("Laberinto")]
    [SerializeField] private Generator generator;

    [Header("Configuración")]
    [SerializeField] private float tiempoInicial = 60f;

    [Header("Fin")]
    [SerializeField] private EndZone endZone;

    [Header("Opcional")]
    [SerializeField] private TMP_Text textoTiempo;

    private float tiempoRestante;
    private bool activo = true;
    private bool conexionCerrada = false;

    void OnEnable()
    {
        if (generator != null) generator.OnMazeGenerated += ReiniciarTemporizador;
    }

    void OnDisable()
    {
        if (generator != null) generator.OnMazeGenerated -= ReiniciarTemporizador;
    }

    void Start()
    {
        tiempoRestante = tiempoInicial;
        ActualizarTexto();
    }

    void Update()
    {
        if (!activo) return;

        tiempoRestante -= Time.deltaTime;

        if (tiempoRestante <= 0f)
        {
            tiempoRestante = 0f;
            activo = false;
            ActualizarTexto();
            TerminarTiempo();
            return;
        }

        ActualizarTexto();
    }

    //Para siguiente nivel
    void ReiniciarTemporizador()
    {
        tiempoRestante = tiempoInicial;
        activo = true;
        Time.timeScale = 1f;
        ActualizarTexto();
    }
    void ActualizarTexto()
    {
        if (textoTiempo == null) return;

        int minutos = Mathf.FloorToInt(tiempoRestante / 60f);
        int segundos = Mathf.FloorToInt(tiempoRestante % 60f);
        textoTiempo.text = $"Tiempo: {minutos:00}:{segundos:00}";
    }

    void TerminarConexion()
    {
        if (conexionCerrada || mpuRecibidor == null) return;

        try
        {
            mpuRecibidor.running = false;
            mpuRecibidor.udpClient?.Close();

            if (mpuRecibidor.receiveThread != null && mpuRecibidor.receiveThread.IsAlive)
                mpuRecibidor.receiveThread.Join(200);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Timer: error cerrando MPU: " + e.Message);
        }

        conexionCerrada = true;
    }

    void TerminarTiempo()
    {
        TerminarConexion();

        if (endZone != null) endZone.TiempoAgotado();
        else Debug.LogWarning("Timer: no hay EndZone asignado.");
    }
}