using System;
using UnityEngine;

[Serializable]
public class MazeData
{
    public int seed;     // semilla del laberinto
    public int width;
    public int depth;
    public int goalX;    // celda donde está el cubo
    public int goalZ;
}

// Clase estática: NO se destruye al recargar la escena.
// Solo se reutiliza el laberinto cuando se pulsa el botón de reiniciar.
public static class MazeSaveSystem
{
    static MazeData _current;
    static bool _reiniciando;

    // Se ejecuta una vez cada vez que das Play: limpia todo.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetAlIniciar()
    {
        _current = null;
        _reiniciando = false;
    }

    // Guarda los datos del laberinto actual (solo en memoria).
    public static void Save(MazeData data)
    {
        _current = data;
    }

    // Borra el laberinto guardado y cancela cualquier reinicio pendiente.
    // Llamar desde el botón de siguiente.
    public static void Clear()
    {
        _current = null;
        _reiniciando = false;
    }

    // Llamar desde el botón de reiniciar, justo antes de recargar la escena.
    public static void MarcarReinicio()
    {
        _reiniciando = true;
    }

    // Devuelve true solo si venimos de pulsar reiniciar y hay datos guardados.
    // La marca se consume: la próxima carga de escena (sin botón) será un laberinto nuevo.
    public static bool TryLoadParaReinicio(out MazeData data)
    {
        data = _current;
        bool usar = _reiniciando && _current != null;
        _reiniciando = false;
        return usar;
    }
}