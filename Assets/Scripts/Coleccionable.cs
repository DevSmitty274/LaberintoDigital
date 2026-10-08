using UnityEngine;
using System;

public class Coleccionable : MonoBehaviour
{
    // Lo escucha el PuntajeManager
    public static event Action<int> OnRecogido;

    [SerializeField] private int _puntos = 1;

    private Transform _esfera;

    // Lo llama el Generator al instanciar el objeto
    public void Configurar(Transform esfera)
    {
        _esfera = esfera;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_esfera == null) return;

        // Solo reacciona a la pelota (o a una parte de ella)
        if (other.transform != _esfera && !other.transform.IsChildOf(_esfera)) return;

        OnRecogido?.Invoke(_puntos);
        Destroy(gameObject);
    }
}
