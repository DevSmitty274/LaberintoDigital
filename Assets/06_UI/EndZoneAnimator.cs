using UnityEngine;
using TMPro;
using DG.Tweening;

public class EndTextAnimator : MonoBehaviour
{
    public enum Mode { Win, Lose }

    [SerializeField] private Mode mode;
    [SerializeField] private TMP_Text text;
    [SerializeField] private float delayPerLetter = 0.12f;
    [SerializeField] private float letterDuration = 1.2f;

    private float[] progress;
    private bool playing;
    private Tween shakeTween;

    void Reset() => text = GetComponent<TMP_Text>();

    void OnEnable()
    {
        text.ForceMeshUpdate();
        int count = text.textInfo.characterCount;
        progress = new float[count];

        for (int i = 0; i < count; i++)
        {
            int index = i; // copia local para el closure
            DOTween.To(() => progress[index], x => progress[index] = x, 1f, letterDuration)
                   .SetDelay(i * delayPerLetter)
                   .SetEase(mode == Mode.Win ? Ease.OutBack : Ease.OutBounce)
                   .SetUpdate(true)   // funciona aunque Time.timeScale = 0
                   .SetTarget(this);
        }

        if (mode == Mode.Lose)
        {
            shakeTween = transform.DOShakePosition(0.6f, 25f, 30)
                                  .SetDelay(letterDuration * 0.6f)
                                  .SetUpdate(true)
                                  .SetTarget(this);
        }

        playing = true;
    }

    void OnDisable()
    {
        playing = false;
        DOTween.Kill(this);
    }

    void LateUpdate()
    {
        if (!playing) return;

        text.ForceMeshUpdate();
        TMP_TextInfo info = text.textInfo;
        float t = Time.unscaledTime;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            if (!c.isVisible) continue;

            int mi = c.materialReferenceIndex;
            int vi = c.vertexIndex;
            Vector3[] verts = info.meshInfo[mi].vertices;
            Color32[] colors = info.meshInfo[mi].colors32;

            float p = progress[i];
            Vector3 center = (verts[vi] + verts[vi + 2]) / 2f;

            Vector3 offset;
            float angle;
            float scale;
            Color32 color;

            if (mode == Mode.Win)
            {
                // Entrada en espiral desde lejos
                Vector3 dir = new Vector3(Mathf.Cos(i * 2.4f), Mathf.Sin(i * 2.4f), 0);
                offset = dir * 600f * (1f - p);
                angle = 720f * (1f - p);
                scale = Mathf.Max(0f, p);

                // Ola continua una vez que llegó la letra
                if (p >= 0.99f)
                    offset.y += Mathf.Sin(t * 4f + i * 0.5f) * 12f;

                // Arcoíris
                color = Color.HSVToRGB(Mathf.Repeat(t * 0.5f + i * 0.08f, 1f), 0.8f, 1f);
            }
            else
            {
                // Caída desde arriba con rebote (el ease hace el rebote)
                offset = new Vector3(0, 800f * (1f - p), 0);
                angle = (1f - p) * (i % 2 == 0 ? -90f : 90f);
                scale = 1f;

                // Glitch: temblor con ruido una vez que cayó
                if (p >= 0.99f)
                {
                    float nx = Mathf.PerlinNoise(t * 15f, i) - 0.5f;
                    float ny = Mathf.PerlinNoise(i, t * 15f) - 0.5f;
                    offset += new Vector3(nx, ny, 0) * 8f;
                }

                // Se va poniendo rojo
                color = Color.Lerp(Color.white, new Color(0.85f, 0.1f, 0.1f), p);
            }

            Quaternion rot = Quaternion.Euler(0, 0, angle);

            for (int j = 0; j < 4; j++)
            {
                Vector3 v = verts[vi + j] - center;
                verts[vi + j] = rot * (v * scale) + center + offset;
                colors[vi + j] = color;
            }
        }

        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
    }
}