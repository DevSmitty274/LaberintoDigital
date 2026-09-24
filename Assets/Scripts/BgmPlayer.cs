using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class BgmPlayer : MonoBehaviour
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private AudioMixerGroup _musicMixerGroup;

    [Header("Parámetros expuestos en el Mixer")]
    [SerializeField] private string _masterVolumeParam = "Master";
    [SerializeField] private string _musicVolumeParam = "Bgm";
    [SerializeField] private string _sfxVolumeParam = "Sfx";

    [Header("Clips de música")]
    [SerializeField] private AudioClip _musica1;
    [SerializeField] private AudioClip _musica2;
    [SerializeField] private AudioClip _track1;

    [Header("Fuente")]
    [SerializeField] private AudioSource _source;

    private void Awake()
    {
        if (_source == null) _source = gameObject.AddComponent<AudioSource>();

        _source.playOnAwake = false;
        _source.loop = false; // PlayOneShot no usa loop de todas formas
        _source.outputAudioMixerGroup = _musicMixerGroup;
        _source.volume = 1f; // el volumen real lo maneja el Mixer
    }

    // ---------------- Reproducir una sola música con PlayOneShot ----------------

    public void PlayMusica1(float volume = 1f)
    {
        PlayOneShotUnico(_musica1, volume);
    }

    public void PlayMusica2(float volume = 1f)
    {
        PlayOneShotUnico(_musica2, volume);
    }

    public void PlayTrack1(float volume = 1f)
    {
        PlayOneShotUnico(_track1, volume);
    }

    /// <summary>
    /// Detiene cualquier música anterior y reproduce solo el clip indicado con PlayOneShot.
    /// </summary>
    private void PlayOneShotUnico(AudioClip clip, float volume)
    {
        if (clip == null) return;

        _source.Stop(); // aseguramos que no se solape con una reproducción anterior
        _source.PlayOneShot(clip, volume);
    }

    // ---------------- Volumen real vía Mixer (Master / Music / SFX) ----------------

    public void SetMasterVolume(float linear01)
    {
        SetMixerVolume(_masterVolumeParam, linear01);
    }

    public void SetMusicVolume(float linear01)
    {
        SetMixerVolume(_musicVolumeParam, linear01);
    }

    public void SetSfxVolume(float linear01)
    {
        SetMixerVolume(_sfxVolumeParam, linear01);
    }

    private void SetMixerVolume(string param, float linear01)
    {
        if (_audioMixer == null || string.IsNullOrEmpty(param)) return;

        float dB = linear01 <= 0.0001f ? -80f : Mathf.Log10(linear01) * 20f;
        _audioMixer.SetFloat(param, dB);
    }

    // ---------------- (Opcional) Leer el volumen actual, útil para inicializar sliders ----------------

    public float GetMasterVolume() => GetMixerVolume(_masterVolumeParam);
    public float GetMusicVolume() => GetMixerVolume(_musicVolumeParam);
    public float GetSfxVolume() => GetMixerVolume(_sfxVolumeParam);

    private float GetMixerVolume(string param)
    {
        if (_audioMixer == null || string.IsNullOrEmpty(param)) return 1f;

        if (_audioMixer.GetFloat(param, out float dB))
        {
            return dB <= -80f ? 0f : Mathf.Pow(10f, dB / 20f);
        }

        return 1f;
    }
}