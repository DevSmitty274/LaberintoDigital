using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class MPU6050Receiver : MonoBehaviour
{

//140 o -140 en z o 270 en y o 100 en x
    public int port = 4210;

    private UdpClient udpClient;
    private Thread receiveThread;
    private volatile bool running = false;

    public Vector3 accel;
    public Vector3 gyro;

    public float threshold = 2f;

    public Vector3 minPosition = new Vector3(-5f, -5f, -5f);
    public Vector3 maxPosition = new Vector3(5f, 5f, 5f);

    public Rigidbody rb;

    void Start()
    {
        udpClient = new UdpClient(port);
        running = true;
        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();

        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
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
    public Vector3 minRotation = new Vector3(-140f, -270f, -140f);
    public Vector3 maxRotation = new Vector3(100f, 270f, 140f);

    void FixedUpdate()
    {
        if (Mathf.Abs(gyro.x) > threshold || Mathf.Abs(gyro.y) > threshold || Mathf.Abs(gyro.z) > threshold)
        {
            Quaternion deltaRot = Quaternion.Euler(gyro * Time.fixedDeltaTime);
            Quaternion newRot = rb.rotation * deltaRot;

            Vector3 euler = newRot.eulerAngles;
            euler = ClampEuler(euler, minRotation, maxRotation);

            rb.MoveRotation(Quaternion.Euler(euler));
        }

        Vector3 pos = rb.position;
        pos.x = Mathf.Clamp(pos.x, minPosition.x, maxPosition.x);
        pos.y = Mathf.Clamp(pos.y, minPosition.y, maxPosition.y);
        pos.z = Mathf.Clamp(pos.z, minPosition.z, maxPosition.z);
        rb.MovePosition(pos);
    }

    Vector3 ClampEuler(Vector3 euler, Vector3 min, Vector3 max)
    {
        euler.x = ClampAngle(euler.x, min.x, max.x);
        euler.y = ClampAngle(euler.y, min.y, max.y);
        euler.z = ClampAngle(euler.z, min.z, max.z);
        return euler;
    }

    float ClampAngle(float angle, float min, float max)
    {
        // Convierte de rango 0-360 a -180/180 para que el clamp tenga sentido
        if (angle > 180f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
    void OnApplicationQuit()
    {
        running = false;
        udpClient.Close();
        receiveThread.Abort();
    }
}