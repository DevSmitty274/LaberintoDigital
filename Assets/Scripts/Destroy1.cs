using UnityEngine;
using UnityEngine.SceneManagement;
public class Destroy1 : MonoBehaviour
{
    public GameObject canvasLose;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pelota"))
        {
            Destroy(other.gameObject);
            canvasLose.SetActive(true);
        }
    }
}