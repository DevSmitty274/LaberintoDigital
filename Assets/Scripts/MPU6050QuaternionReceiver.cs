using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class MPU6050QuaternionReceiver : MonoBehaviour
{
    [Header("Red")]
    public int port = 4210;
    private UdpClient udpClient;
    private Thread receiveThread;
    private volatile bool running = false;
    [Header("Quaternion recibido del DMP (w, x, y, z), ya con offset aplicado")]
    public Quaternion sensorRotation = Quaternion.identity;
    [Header("Suavizado opcional")]
    public bool smooth = true;
    public float smoothSpeed = 15f; // más alto = responde más rápido
    public Rigidbody rb;
    // Buffer para lectura thread-safe del ultimo quaternion recibido
    private float qw = 1f, qx = 0f, qy = 0f, qz = 0f;
    private readonly object lockObj = new object();
    // Offset de calibracion inicial (para que "donde esta el sensor al arrancar" sea 0,0,0)
    private Quaternion initialOffset = Quaternion.identity;
    private bool offsetCaptured = false;
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
                if (running)
                {
                    Debug.Log("Error UDP: " + e.Message);
                }
            }
        }
    }
    void ParseData(string text)
    {
        // Formato esperado desde el ESP32: w,x,y,z
        string[] values = text.Split(',');
        if (values.Length == 4)
        {
            float w = 0f, x = 0f, y = 0f, z = 0f;
            bool ok = float.TryParse(values[0], out w) &&
                      float.TryParse(values[1], out x) &&
                      float.TryParse(values[2], out y) &&
                      float.TryParse(values[3], out z);

            if (ok && !float.IsNaN(w) && !float.IsNaN(x) && !float.IsNaN(y) && !float.IsNaN(z))
            {
                lock (lockObj)
                {
                    qw = w;
                    qx = x;
                    qy = y;
                    qz = z;
                }
            }
            else
            {
                Debug.LogWarning("Paquete UDP corrupto, ignorado: " + text);
            }
        }
    }

    void FixedUpdate()
    {
        float w, x, y, z;
        lock (lockObj)
        {
            w = qw; x = qx; y = qy; z = qz;
        }

        // El MPU6050 (DMP) entrega el quaternion en su propio sistema de referencia.
        // Unity usa mano izquierda, el DMP usa mano derecha -> por eso se remapean/invierten ejes.
        // Ajusta este mapeo si la orientacion no calza con tu montaje físico.
        Quaternion raw = new Quaternion(-x, -z, -y, w);

        bool hasRealData = !(w == 1f && x == 0f && y == 0f && z == 0f);

        // Captura el offset inicial UNA sola vez, apenas llega el primer dato real
        if (!offsetCaptured && hasRealData)
        {
            initialOffset = raw;
            offsetCaptured = true;
        }

        // Aplica el offset: "donde estaba el sensor al capturar" pasa a ser el 0,0,0 en Unity
        sensorRotation = Quaternion.Inverse(initialOffset) * raw;

        Quaternion targetRot = sensorRotation;

        if (smooth)
        {
            Quaternion smoothed = Quaternion.Slerp(rb.rotation, targetRot, smoothSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(smoothed);
        }
        else
        {
            rb.MoveRotation(targetRot);
        }
    }
    void OnApplicationQuit()
    {
        running = false;
        udpClient.Close();

        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(200);
        }
    }
}