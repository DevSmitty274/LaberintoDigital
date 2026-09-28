using System.Net.Sockets;
using TMPro;
using UnityEngine;

public class Temporizador : MonoBehaviour
{
    public MPU6050QuaternionReceiver mpuRecibidor;

    [Header("Configuración")]
    [SerializeField] private float tiempoInicial = 60f; // segundos
    [SerializeField] private GameObject canvasFinal;    // tu Canvas que aparece al llegar a 0

    [Header("Opcional")]
    [SerializeField] private TMP_Text textoTiempo;      // texto para mostrar el contador

    private float tiempoRestante;
    private bool activo = true;

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
    }

    void TerminarTiempo()
    {
        canvasFinal.SetActive(true);
        // Time.timeScale = 0f; // Descomenta si quieres pausar el juego
    }
}
