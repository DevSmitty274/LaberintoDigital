using UnityEngine;
using TMPro;    

public class Puntajemanager : MonoBehaviour
{
    [SerializeField] private Generator generator;   // objeto con el Generator
    [SerializeField] private TMP_Text textoPuntaje; // texto donde se muestra el puntaje
    [SerializeField] private string prefijo = "Puntaje: ";

    private int puntaje;

    public int Puntaje => puntaje;

    void OnEnable()
    {
        Coleccionable.OnRecogido += Sumar;
        if (generator != null) generator.OnMazeGenerated += ReiniciarPuntaje;
    }

    void OnDisable()
    {
        Coleccionable.OnRecogido -= Sumar;
        if (generator != null) generator.OnMazeGenerated -= ReiniciarPuntaje;
    }

    void Start()
    {
        ActualizarTexto();
    }

    void Sumar(int puntos)
    {
        puntaje += puntos;
        ActualizarTexto();
    }

    // Se llama con Siguiente y con Reiniciar: el puntaje vuelve a 0
    void ReiniciarPuntaje()
    {
        puntaje = 0;
        ActualizarTexto();
    }

    void ActualizarTexto()
    {
        if (textoPuntaje != null) textoPuntaje.text = prefijo + puntaje;
    }
}
