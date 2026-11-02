using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneFader : MonoBehaviour
{
    public float FadeDuration = 1f;

    [Tooltip("Marcado: FadeOut va de 0 a 1 (tu shader original). Desmárcalo si queda al revés.")]
    public bool CubreEnUno = true;

    private static readonly int _FadeAmount = Shader.PropertyToID("_FadeAmount");
    private static readonly int _useRadialWipe = Shader.PropertyToID("_useRadialWipe");
    private static readonly int _useShutters = Shader.PropertyToID("_useShutters");
    private static readonly int _usePlainBlack = Shader.PropertyToID("_usePlainBlack");
    private static readonly int _useGoop = Shader.PropertyToID("_useGoop");

    private int? _lastEffect;
    private Image _image;
    private Material _material;
    private Coroutine _fadeActual;

    public enum FadeType { Shutters, RadialWipe, PlainBlack, Goop }

    private float ValorCubierto => CubreEnUno ? 1f : 0f;
    private float ValorDespejado => CubreEnUno ? 0f : 1f;

    private void Awake()
    {
        Inicializar();
    }

    private void Inicializar()
    {
        if (_material != null) return;

        _image = GetComponent<Image>();
        if (_image == null)
        {
            Debug.LogError("SceneFader necesita un componente Image en el mismo objeto.", this);
            return;
        }

        _image.material = new Material(_image.material);
        _material = _image.material;
    }

    // FadeOut: la pantalla se cubre y se queda cubierta
    public void FadeOut(FadeType type, Action onComplete = null)
    {
        Iniciar(type, ValorDespejado, ValorCubierto, onComplete);
    }

    // FadeIn: la pantalla se destapa
    public void FadeIn(FadeType type, Action onComplete = null)
    {
        Iniciar(type, ValorCubierto, ValorDespejado, onComplete);
    }

    private void Iniciar(FadeType type, float desde, float hasta, Action onComplete)
    {
        Inicializar();
        if (_material == null) { onComplete?.Invoke(); return; }

        ChangeFadeEffect(type);

        if (_fadeActual != null) StopCoroutine(_fadeActual);
        _fadeActual = StartCoroutine(HandleFade(desde, hasta, onComplete));
    }

    public void ChangeFadeEffect(FadeType type)
    {
        if (_lastEffect.HasValue)
            _material.SetFloat(_lastEffect.Value, 0f);

        switch (type)
        {
            case FadeType.Shutters: SwitchEffect(_useShutters); break;
            case FadeType.RadialWipe: SwitchEffect(_useRadialWipe); break;
            case FadeType.PlainBlack: SwitchEffect(_usePlainBlack); break;
            case FadeType.Goop: SwitchEffect(_useGoop); break;
        }
    }

    private void SwitchEffect(int effect)
    {
        _material.SetFloat(effect, 1f);
        _lastEffect = effect;
    }

    private IEnumerator HandleFade(float startAmount, float targetAmount, Action onComplete)
    {
        float elapsed = 0f;
        _material.SetFloat(_FadeAmount, startAmount);

        while (elapsed < FadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _material.SetFloat(_FadeAmount, Mathf.Lerp(startAmount, targetAmount, elapsed / FadeDuration));
            yield return null;
        }

        _material.SetFloat(_FadeAmount, targetAmount);
        _fadeActual = null;
        onComplete?.Invoke();
    }
}