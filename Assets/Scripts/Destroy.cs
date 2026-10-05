using UnityEngine;
using UnityEngine.SceneManagement; 
public class Destroy : MonoBehaviour
{
    public GameObject canvasWin;
    [SerializeField]
    private Generator _generator;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pelota"))
        {
            Destroy(other.gameObject);
            canvasWin.SetActive(true);
        }
    }
    public void SiguienteNivel()
    {
        _generator.GenerarNuevo();
    }
}