using UnityEngine;
using UnityEngine.SceneManagement;
public class Destroy1 : MonoBehaviour
{
    public GameObject canvasLose;
    [SerializeField]
    private Generator _generator;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pelota"))
        {
            Destroy(other.gameObject);
            canvasLose.SetActive(true);
        }
    }
    public void ReiniciarNivel()
    {
        MazeSaveSystem.MarcarReinicio();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        //_generator.ReiniciarMismoLaberinto();
    }
}