using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class MPU6050Receiver : MonoBehaviour
{
    public int port = 4210;

    private UdpClient udpClient;
    private Thread receiveThread;
    private volatile bool running = false;

    public Vector3 accel;
    public Vector3 gyro;

    public float threshold = 2f;

    public Vector3 minPosition = new Vector3(-5f, -5f, -5f);
    public Vector3 maxPosition = new Vector3(5f, 5f, 5f);

    void Start()
    {
        udpClient = new UdpClient(port);
        running = true;
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }
    void ReceiveData()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, port);
        while (running)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEP);
                string text = System.Text.Encoding.UTF8.GetString(data);
                ParseData(text);
            }
            catch (System.Exception e)
            {
                Debug.Log("Error UDP: " + e.Message);
            }
        }
    }

    void ParseData(string text)
    {
        string[] values = text.Split(',');
        if (values.Length == 6)
        {
            float ax = float.Parse(values[0]);
            float ay = float.Parse(values[1]);
            float az = float.Parse(values[2]);
            float gx = float.Parse(values[3]);
            float gy = float.Parse(values[4]);
            float gz = float.Parse(values[5]);

            accel = new Vector3(ax, ay, az);
            gyro = new Vector3(gx, gy, gz);
        }
    }
    void Update()
    {
        if (Mathf.Abs(gyro.x) > threshold || Mathf.Abs(gyro.y) > threshold || Mathf.Abs(gyro.z) > threshold)
        {
            transform.Rotate(gyro * Time.deltaTime, Space.World);
        }

        Vector3 pos = transform.position;

        pos.x = Mathf.Clamp(pos.x, minPosition.x, maxPosition.x);
        pos.y = Mathf.Clamp(pos.y, minPosition.y, maxPosition.y);
        pos.z = Mathf.Clamp(pos.z, minPosition.z, maxPosition.z);

        transform.position = pos;
    }
void OnApplicationQuit()
    {
        running = false;
        udpClient.Close();
        receiveThread.Abort();
    }
}