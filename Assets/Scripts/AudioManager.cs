using UnityEngine;
using UnityEngine.InputSystem;

public enum Sfx { Kick, Impact, Pickup, Recycle, Splash, Alert, Buy, Deny, Click, Win, Lose }

// Musica de fondo y efectos. Los clips se asignan en el Inspector: para usar la banda sonora final
// (Jazzy HipHop / Chillhop) basta con arrastrar el clip nuevo a "music".
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;

    [SerializeField] private AudioClip music;
    [SerializeField] private AudioClip[] sfx;          // en el orden del enum Sfx
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

    private AudioSource musicSource, sfxSource;
    private bool muted;

    void Awake()
    {
        instance = this;
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = music; musicSource.loop = true; musicSource.playOnAwake = false;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        musicSource.Play();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame) muted = !muted;
        // la musica baja en pausa y menus para que se escuchen los SFX
        bool quiet = GameManager.I != null && !GameManager.I.Running;
        musicSource.volume = muted ? 0f : musicVolume * (quiet ? 0.5f : 1f);
    }

    public static void Play(Sfx id)
    {
        if (instance == null || instance.muted) return;
        instance.sfxSource.PlayOneShot(instance.sfx[(int)id], instance.sfxVolume);
    }
}
