using System.Net.Sockets;
using TMPro;
using UnityEngine;

public class Temporizador : MonoBehaviour
{
    public MPU6050QuaternionReceiver mpuRecibidor;

    [Header("Laberinto")]
    [SerializeField] private Generator generator;       // arrastra aquí el objeto con el Generator

    [Header("Configuración")]
    [SerializeField] private float tiempoInicial = 60f; // segundos
    [SerializeField] private GameObject canvasFinal;    // tu Canvas que aparece al llegar a 0

    [Header("Opcional")]
    [SerializeField] private TMP_Text textoTiempo;      // texto para mostrar el contador

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
        canvasFinal.SetActive(false); // aseguramos que empiece oculto
    }

    void Update()
    {
        if (!activo) return;

        tiempoRestante -= Time.deltaTime;

        if (tiempoRestante <= 0f)
        {
            tiempoRestante = 0f;
            activo = false;
            TerminarTiempo();
            TerminarConexión();
        }

        ActualizarTexto();
    }

    // Se llama cada vez que el laberinto se genera (Siguiente) o se reinicia (Reiniciar)
    void ReiniciarTemporizador()
    {
        tiempoRestante = tiempoInicial;
        activo = true;

        if (canvasFinal != null) canvasFinal.SetActive(false);
        Time.timeScale = 1f; // por si activas la pausa en TerminarTiempo()

        if (conexionCerrada)
        {
            // TODO: aquí hay que volver a abrir la conexión del MPU6050.
            // Depende del script MPU6050QuaternionReceiver (pásamelo para completarlo).
            conexionCerrada = false;
        }

        ActualizarTexto();
    }

    void ActualizarTexto()
    {
        if (textoTiempo == null) return;

        int minutos = Mathf.FloorToInt(tiempoRestante / 60f);
        int segundos = Mathf.FloorToInt(tiempoRestante % 60f);
        textoTiempo.text = $"{minutos:00}:{segundos:00}";
    }

    void TerminarConexión()
    {
        mpuRecibidor.running = false;
        mpuRecibidor.udpClient.Close();

        if (mpuRecibidor.receiveThread != null && mpuRecibidor.receiveThread.IsAlive)
        {
            mpuRecibidor.receiveThread.Join(200);
        }

        conexionCerrada = true;
    }

    void TerminarTiempo()
    {
        canvasFinal.SetActive(true);
        // Time.timeScale = 0f; // Descomenta si quieres pausar el juego
    }
}
