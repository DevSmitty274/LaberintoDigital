using UnityEngine;
using UnityEngine.SceneManagement;
public class Destroy1 : MonoBehaviour
{
    public GameObject canvasLose;
    [SerializeField]
    private Generator _generator;

    public MPU6050QuaternionReceiver mpu;


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Pelota"))
        {
            Destroy(other.gameObject);
            canvasLose.SetActive(true);
        }
    }

    public void Apagarmpu()
    {
        mpu.running = false;
        mpu.udpClient.Close();

        if (mpu.receiveThread != null && mpu.receiveThread.IsAlive)
        {
            mpu.receiveThread.Join(200);
        }

        {
            ReiniciarNivel();
        }
    }
    public void ReiniciarNivel()
    {
        MazeSaveSystem.MarcarReinicio();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        //_generator.ReiniciarMismoLaberinto();
    }
}