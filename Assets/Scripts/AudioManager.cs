using UnityEngine;
using UnityEngine.InputSystem;

public enum Sfx { Kick, Impact, Pickup, Recycle, Splash, Alert, Buy, Deny, Click, Win, Lose, Charge, PowerKick, Convince, Refuse, Combo, Achievement, Star, Pet, Plant, Rescue, Strike }

// Musica de fondo y efectos. Los clips se asignan en el Inspector: para usar la banda sonora final
// (Jazzy HipHop / Chillhop) basta con arrastrar el clip nuevo a "music".
public class AudioManager : MonoBehaviour
{
    private const int Voices = 12;

    private static AudioManager instance;

    [SerializeField] private AudioClip music;
    [SerializeField] private AudioClip[] sfx;          // en el orden del enum Sfx
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

    private AudioSource musicSource;
    private AudioSource[] voices;
    private int nextVoice;
    private bool muted;

    void Awake()
    {
        instance = this;
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = music; musicSource.loop = true; musicSource.playOnAwake = false;
        voices = new AudioSource[Voices];
        for (int i = 0; i < Voices; i++) { voices[i] = gameObject.AddComponent<AudioSource>(); voices[i].playOnAwake = false; }
        musicSource.Play();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame) muted = !muted;
        // la musica baja en pausa y menus para que se escuchen los SFX
        bool quiet = GameManager.I != null && !GameManager.I.Running;
        musicSource.volume = muted ? 0f : musicVolume * (quiet ? 0.5f : 1f);
    }

    // pitch = 0: leve variacion al azar para que un golpe repetido no suene identico; otro valor lo fija (sube en las rachas)
    public static void Play(Sfx id, float pitch = 0f, float volume = 1f)
    {
        if (instance == null || instance.muted || (int)id >= instance.sfx.Length || instance.sfx[(int)id] == null) return;
        var v = instance.voices[instance.nextVoice];
        instance.nextVoice = (instance.nextVoice + 1) % Voices;
        v.pitch = pitch > 0f ? pitch : Random.Range(0.94f, 1.06f);
        v.PlayOneShot(instance.sfx[(int)id], instance.sfxVolume * volume);
    }
}
