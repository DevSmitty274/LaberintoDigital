using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class ResultadoFinal : MonoBehaviour
{
    [Header("Puntaje final")]
    [SerializeField] private Score score;   // opcional: si está vacío, lo busca solo
    [SerializeField] private TMP_Text textoPuntajeFinal;
    [SerializeField] private string prefijo = "PUNTAJE FINAL: ";
    [SerializeField] private float duracionConteo = 1.2f;
    [SerializeField] private float fuerzaGolpe = 0.4f;

    [Header("Ingreso de nombre")]
    [SerializeField] private GameObject panelNombre;
    [SerializeField] private TMP_Text[] letras = new TMP_Text[3];
    [SerializeField] private float escalaSeleccion = 1.4f;

    [Header("Ranking (opcional)")]
    [SerializeField] private TMP_Text textoRanking;
    [SerializeField] private int maxRanking = 5;

    private const string ClaveRanking = "RankingArcade";

    private int[] _indices = new int[3];
    private int _casilla;
    private int _puntaje;
    private bool _ingresando;
    private Action _alTerminar;
    private Sequence _seq;
    private Tween _conteo;
    private float _valorMostrado;

    [Serializable] private class Entrada { public string nombre; public int puntaje; }
    [Serializable] private class Lista { public List<Entrada> items = new List<Entrada>(); }

    // ================= API =================

    public void Mostrar(int puntaje, Action alTerminar)
    {
        Ocultar();

        // Si EndZone mandó 0, leemos el puntaje real directamente del Score
        if (score == null)
            score = FindAnyObjectByType<Score>(FindObjectsInactive.Include);

        if (score != null && score.Puntaje > puntaje)
            puntaje = score.Puntaje;

        Debug.Log($"ResultadoFinal: puntaje a mostrar = {puntaje} | Score encontrado: {score != null}");

        _puntaje = puntaje;
        _alTerminar = alTerminar;
        _casilla = 0;
        for (int i = 0; i < _indices.Length; i++) _indices[i] = 0;
        _valorMostrado = 0f;

        if (panelNombre != null) panelNombre.SetActive(false);
        if (textoRanking != null) textoRanking.gameObject.SetActive(false);

        if (textoPuntajeFinal == null)
        {
            Debug.LogWarning("ResultadoFinal: falta asignar Texto Puntaje Final.");
            IniciarIngresoNombre();
            return;
        }

        textoPuntajeFinal.gameObject.SetActive(true);
        textoPuntajeFinal.transform.localScale = Vector3.zero;
        ActualizarTextoPuntaje();

        _seq = DOTween.Sequence().SetUpdate(true);

        // 1) Entra el texto
        _seq.Append(textoPuntajeFinal.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));

        // 2) Cuenta hacia arriba
        _seq.Append(DOVirtual.Float(0f, _puntaje, duracionConteo, v =>
        {
            _valorMostrado = v;
            ActualizarTextoPuntaje();
        }).SetEase(Ease.OutQuad));

        // 3) Golpe de escala y valor exacto al final
        _seq.AppendCallback(() =>
        {
            _valorMostrado = _puntaje;
            ActualizarTextoPuntaje();
        });
        _seq.Append(textoPuntajeFinal.transform.DOPunchScale(Vector3.one * fuerzaGolpe, 0.4f, 6, 0.8f));

        _seq.AppendInterval(0.3f);
        _seq.OnComplete(IniciarIngresoNombre);
    }

    public void Ocultar()
    {
        _seq?.Kill();
        _conteo?.Kill();
        _ingresando = false;

        if (textoPuntajeFinal != null) textoPuntajeFinal.gameObject.SetActive(false);
        if (panelNombre != null) panelNombre.SetActive(false);
        if (textoRanking != null) textoRanking.gameObject.SetActive(false);
    }

    public void Subir() { CambiarLetra(+1); }
    public void Bajar() { CambiarLetra(-1); }
    public void Siguiente() { MoverCasilla(+1); }
    public void Anterior() { MoverCasilla(-1); }
    public void Confirmar() { if (_ingresando) Guardar(); }

    // ================= Ingreso de nombre =================

    void IniciarIngresoNombre()
    {
        if (panelNombre == null || letras == null || letras.Length < 3 ||
            letras[0] == null || letras[1] == null || letras[2] == null)
        {
            Debug.LogWarning("ResultadoFinal: faltan Panel Nombre o las 3 letras, se salta el ingreso de nombre.");
            Terminar();
            return;
        }

        panelNombre.SetActive(true);
        panelNombre.transform.localScale = Vector3.zero;
        panelNombre.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);

        for (int i = 0; i < 3; i++) letras[i].transform.localScale = Vector3.one;

        RefrescarLetras();
        _ingresando = true;
    }

    void Update()
    {
        if (!_ingresando || Keyboard.current == null) return;

        var k = Keyboard.current;
        if (k.upArrowKey.wasPressedThisFrame) Subir();
        if (k.downArrowKey.wasPressedThisFrame) Bajar();
        if (k.rightArrowKey.wasPressedThisFrame) Siguiente();
        if (k.leftArrowKey.wasPressedThisFrame) Anterior();
        if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)
            Confirmar();
    }

    void CambiarLetra(int delta)
    {
        if (!_ingresando) return;
        _indices[_casilla] = (_indices[_casilla] + delta + 26) % 26;
        RefrescarLetras();

        letras[_casilla].transform.DOKill();
        letras[_casilla].transform.localScale = Vector3.one * escalaSeleccion;
        letras[_casilla].transform.DOPunchScale(Vector3.one * 0.25f, 0.2f, 6, 0.8f).SetUpdate(true);
    }

    void MoverCasilla(int delta)
    {
        if (!_ingresando) return;
        _casilla = Mathf.Clamp(_casilla + delta, 0, 2);
        RefrescarLetras();
    }

    void RefrescarLetras()
    {
        for (int i = 0; i < 3; i++)
        {
            letras[i].text = ((char)('A' + _indices[i])).ToString();

            float escala = (i == _casilla) ? escalaSeleccion : 1f;
            letras[i].transform.DOKill();
            letras[i].transform.DOScale(escala, 0.12f).SetUpdate(true);
        }
    }

    string NombreActual()
    {
        return new string(new[]
        {
            (char)('A' + _indices[0]),
            (char)('A' + _indices[1]),
            (char)('A' + _indices[2])
        });
    }

    // ================= Ranking =================

    void Guardar()
    {
        _ingresando = false;

        Lista lista = Cargar();
        lista.items.Add(new Entrada { nombre = NombreActual(), puntaje = _puntaje });
        lista.items = lista.items.OrderByDescending(e => e.puntaje).Take(maxRanking).ToList();

        PlayerPrefs.SetString(ClaveRanking, JsonUtility.ToJson(lista));
        PlayerPrefs.Save();

        MostrarRanking(lista);
        Terminar();
    }

    Lista Cargar()
    {
        string json = PlayerPrefs.GetString(ClaveRanking, "");
        if (string.IsNullOrEmpty(json)) return new Lista();

        try { return JsonUtility.FromJson<Lista>(json) ?? new Lista(); }
        catch { return new Lista(); }
    }

    void MostrarRanking(Lista lista)
    {
        if (textoRanking == null) return;

        string txt = "";
        for (int i = 0; i < lista.items.Count; i++)
            txt += $"{i + 1:00}. {lista.items[i].nombre}  {lista.items[i].puntaje}\n";

        textoRanking.text = txt;
        textoRanking.gameObject.SetActive(true);
        textoRanking.transform.localScale = Vector3.zero;
        textoRanking.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    void Terminar()
    {
        var cb = _alTerminar;
        _alTerminar = null;
        cb?.Invoke();
    }

    void ActualizarTextoPuntaje()
    {
        if (textoPuntajeFinal != null)
            textoPuntajeFinal.text = prefijo + Mathf.RoundToInt(_valorMostrado);
    }

    void OnDisable()
    {
        _seq?.Kill();
        _conteo?.Kill();
        _ingresando = false;
    }
}