using UnityEngine;
using UnityEngine.InputSystem;

public class HighResCapture : MonoBehaviour
{
    public Camera cam;
    public int ancho = 7680;
    public int alto = 4320;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
            Capturar();
    }

    [ContextMenu("Capturar")]
    public void Capturar()
    {
        if (cam == null)
        {
            Debug.LogError("Falta asignar la cámara en el Inspector.");
            return;
        }

        var rt = new RenderTexture(ancho, alto, 24);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
        tex.Apply();

        cam.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);

        string carpeta = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string ruta = System.IO.Path.Combine(carpeta, $"captura_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
        System.IO.File.WriteAllBytes(ruta, tex.EncodeToPNG());
        DestroyImmediate(tex);

        Debug.Log("Guardado en: " + ruta);
    }
}