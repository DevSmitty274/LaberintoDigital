using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class TextGradientScroll : MonoBehaviour
{
    public Gradient gradient;          // Define tus colores en el inspector
    public float speed = 0.5f;
    public float scale = 0.1f;         // Qué tanto varía el color entre letras

    TMP_Text tmp;

    void Awake() => tmp = GetComponent<TMP_Text>();

    void Update()
    {
        tmp.ForceMeshUpdate();
        var info = tmp.textInfo;

        for (int i = 0; i < info.characterCount; i++)
        {
            var c = info.characterInfo[i];
            if (!c.isVisible) continue;

            var colors = info.meshInfo[c.materialReferenceIndex].colors32;
            int v = c.vertexIndex;

            for (int j = 0; j < 4; j++)
            {
                float t = Mathf.Repeat(Time.time * speed + (v + j) * scale * 0.25f + i * scale, 1f);
                colors[v + j] = gradient.Evaluate(t);
            }
        }

        for (int i = 0; i < info.meshInfo.Length; i++)
        {
            info.meshInfo[i].mesh.colors32 = info.meshInfo[i].colors32;
            tmp.UpdateGeometry(info.meshInfo[i].mesh, i);
        }
    }
}