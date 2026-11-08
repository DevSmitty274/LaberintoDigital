using System.Collections;
using UnityEngine;

public class CameraFOVIntro : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Field of View")]
    [SerializeField] private float startFOV = 100f;
    [SerializeField] private float endFOV = 35f;

    [Header("Timers")]
    [Tooltip("Seconds to wait before the zoom starts.")]
    [SerializeField, Min(0f)] private float startDelay = 0f;
    [Tooltip("Seconds the zoom takes from start FOV to end FOV.")]
    [SerializeField, Min(0f)] private float duration = 3f;
    [Tooltip("Seconds the zoom out takes when the final screen appears.")]
    [SerializeField, Min(0f)] private float returnDuration = 1f;

    [Header("Easing")]
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine _current;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Start()
    {
        PlayIntro();
    }

    // Zoom de entrada: startFOV -> endFOV
    public void PlayIntro()
    {
        Stop();
        _current = StartCoroutine(AnimateFOV(startFOV, endFOV, duration, startDelay));
    }

    // Zoom de regreso: FOV actual -> startFOV (lo llama EndZone)
    public void ReturnToStart()
    {
        Stop();
        _current = StartCoroutine(AnimateFOV(targetCamera.fieldOfView, startFOV, returnDuration, 0f));
    }

    private void Stop()
    {
        if (_current != null)
        {
            StopCoroutine(_current);
            _current = null;
        }
    }

    private IEnumerator AnimateFOV(float from, float to, float time, float delay)
    {
        targetCamera.fieldOfView = from;

        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.unscaledDeltaTime;   // funciona con timeScale = 0
            float t = easing.Evaluate(Mathf.Clamp01(elapsed / time));
            targetCamera.fieldOfView = Mathf.Lerp(from, to, t);
            yield return null;
        }

        targetCamera.fieldOfView = to;
        _current = null;
    }
}