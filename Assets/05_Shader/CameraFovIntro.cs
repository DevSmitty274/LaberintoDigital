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

    [Header("Easing")]
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void Start()
    {
        StartCoroutine(AnimateFOV());
    }

    private IEnumerator AnimateFOV()
    {
        targetCamera.fieldOfView = startFOV;

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = easing.Evaluate(Mathf.Clamp01(elapsed / duration));
            targetCamera.fieldOfView = Mathf.Lerp(startFOV, endFOV, t);
            yield return null;
        }

        targetCamera.fieldOfView = endFOV;
    }
}