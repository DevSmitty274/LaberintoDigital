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
        Debug.Log($"MazeSaveSystem: Save -> seed {data.seed}");
    }

    // Borra el laberinto guardado y cancela cualquier reinicio pendiente.
    public static void Clear()
    {
        _current = null;
        _reiniciando = false;
        Debug.Log("MazeSaveSystem: Clear");
    }

    // Llamar desde el botón de reiniciar, justo antes de recargar la escena.
    public static void MarcarReinicio()
    {
        _reiniciando = true;
        Debug.Log($"MazeSaveSystem: MarcarReinicio (hay datos guardados: {_current != null})");
    }

    // Devuelve true solo si venimos de pulsar reiniciar y hay datos guardados.
    public static bool TryLoadParaReinicio(out MazeData data)
    {
        data = _current;
        bool usar = _reiniciando && _current != null;
        Debug.Log($"MazeSaveSystem: TryLoadParaReinicio -> reiniciando:{_reiniciando} datos:{_current != null} -> usar:{usar}");
        _reiniciando = false;
        return usar;
    }
}