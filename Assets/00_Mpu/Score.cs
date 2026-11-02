using UnityEngine;
using TMPro;
using DG.Tweening;

public class Score : MonoBehaviour
{
    [SerializeField] private Generator generator;

    [Header("Textos")]
    [SerializeField] private TMP_Text textoPuntaje;    // puntaje acumulado (con degradado)
    [SerializeField] private TMP_Text plantillaGanado; // texto "+N" (déjalo DESACTIVADO, hermano del puntaje)
    [SerializeField] private string prefijo = "Puntaje: ";

    [Header("Animación del +N")]
    [SerializeField] private Vector2 desdeAfuera = new Vector2(-600f, 0f);
    [SerializeField] private float duracionEntrada = 0.35f;
    [SerializeField] private float pausa = 0.25f;
    [SerializeField] private float duracionVuelo = 0.45f;

    [Header("Animación del total")]
    [SerializeField] private float duracionConteo = 0.4f;
    [SerializeField] private float fuerzaGolpe = 0.35f;

    private int puntaje;
    private float puntajeMostrado;
    private Tween tweenConteo;
    private Tween tweenGolpe;

    public int Puntaje => puntaje;

    void OnEnable()
    {
        Coleccionable.OnRecogido += Sumar;
        if (generator != null) generator.OnMazeGenerated += ReiniciarPuntaje;
    }

    void OnDisable()
    {
        Coleccionable.OnRecogido -= Sumar;
        if (generator != null) generator.OnMazeGenerated -= ReiniciarPuntaje;
        DetenerTweens();
    }

    void Start()
    {
        if (textoPuntaje != null) textoPuntaje.transform.localScale = Vector3.one;
        if (plantillaGanado != null) plantillaGanado.gameObject.SetActive(false);
        ActualizarTexto();
    }

    void Sumar(int puntos)
    {
        puntaje += puntos;

        AnimarGanado(puntos);

        // Cada coleccionable vuelve a dibujar el camino desde la pelota hasta el cubo
        if (generator != null) generator.DibujarSolucion();
    }

    // ---------- +N: entra desde afuera y vuela hacia el puntaje ----------

    void AnimarGanado(int puntos)
    {
        if (plantillaGanado == null || textoPuntaje == null)
        {
            AnimarTotal();
            return;
        }

        TMP_Text popup = Instantiate(plantillaGanado, plantillaGanado.transform.parent);
        popup.gameObject.SetActive(true);
        popup.text = "+" + puntos;

        RectTransform rt = popup.rectTransform;
        Vector2 posFinal = rt.anchoredPosition;

        CanvasGroup cg = popup.GetComponent<CanvasGroup>();
        if (cg == null) cg = popup.gameObject.AddComponent<CanvasGroup>();

        rt.anchoredPosition = posFinal + desdeAfuera;
        rt.localScale = Vector3.one * 0.3f;
        cg.alpha = 0f;

        Sequence seq = DOTween.Sequence().SetUpdate(true);

        seq.Append(rt.DOAnchorPos(posFinal, duracionEntrada).SetEase(Ease.OutBack));
        seq.Join(rt.DOScale(1.3f, duracionEntrada).SetEase(Ease.OutBack));
        seq.Join(DOTween.To(() => cg.alpha, v => cg.alpha = v, 1f, duracionEntrada * 0.6f));

        seq.AppendInterval(pausa);

        seq.Append(rt.DOMove(textoPuntaje.rectTransform.position, duracionVuelo).SetEase(Ease.InBack));
        seq.Join(rt.DOScale(0.4f, duracionVuelo).SetEase(Ease.InQuad));
        seq.Join(DOTween.To(() => cg.alpha, v => cg.alpha = v, 0f, duracionVuelo * 0.4f)
                        .SetDelay(duracionVuelo * 0.6f));

        seq.OnComplete(() =>
        {
            if (popup != null) Destroy(popup.gameObject);
            AnimarTotal();
        });
    }

    // ---------- Total: cuenta hacia arriba y golpe de escala (sin tocar el color/degradado) ----------

    void AnimarTotal()
    {
        if (textoPuntaje == null) return;

        tweenConteo?.Kill();
        tweenConteo = DOTween.To(() => puntajeMostrado, v =>
        {
            puntajeMostrado = v;
            ActualizarTexto();
        }, puntaje, duracionConteo).SetEase(Ease.OutQuad).SetUpdate(true);

        tweenGolpe?.Kill();
        textoPuntaje.transform.localScale = Vector3.one;
        tweenGolpe = textoPuntaje.transform
            .DOPunchScale(Vector3.one * fuerzaGolpe, 0.35f, 6, 0.8f)
            .SetUpdate(true);
    }

    void ReiniciarPuntaje()
    {
        DetenerTweens();
        puntaje = 0;
        puntajeMostrado = 0;

        if (textoPuntaje != null) textoPuntaje.transform.localScale = Vector3.one;
        ActualizarTexto();
    }

    void DetenerTweens()
    {
        tweenConteo?.Kill();
        tweenGolpe?.Kill();
        if (textoPuntaje != null) textoPuntaje.transform.localScale = Vector3.one;
    }

    void ActualizarTexto()
    {
        if (textoPuntaje != null)
            textoPuntaje.text = prefijo + Mathf.RoundToInt(puntajeMostrado);
    }
}