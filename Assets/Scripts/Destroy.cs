using UnityEngine;
using UnityEngine.SceneManagement; 
public class Destroy : MonoBehaviour
{
    public GameObject canvasWin;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pelota"))
        {
            Destroy(other.gameObject);
            canvasWin.SetActive(true);
        }
    }
    public void ReiniciarNivel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}