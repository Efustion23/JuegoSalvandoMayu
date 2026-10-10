using UnityEngine;
using UnityEngine.InputSystem;

public enum Sfx { Kick, Impact, Pickup, Recycle, Splash, Alert, Buy, Deny, Click, Win, Lose, Charge, PowerKick, Convince, Refuse, Combo, Achievement, Star, Pet, Plant, Rescue, Strike }

// Musica de fondo y efectos. Cada pantalla tiene su pista (menu, Sector 1, Sector 2, victoria, derrota) y al
// cambiar de pantalla se pasa de una a otra con un fundido. Si falta una pista suena "music" (la provisional).
public class AudioManager : MonoBehaviour
{
    private const int Voices = 12;
    private const float FadeSeconds = 1.2f;

    private static AudioManager instance;

    [SerializeField] private AudioClip music;
    [SerializeField] private AudioClip menuMusic, sector1Music, sector2Music, victoryMusic, defeatMusic;
    [SerializeField] private AudioClip[] sfx;          // en el orden del enum Sfx
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.22f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

    private AudioSource musicSource, fadingSource;
    private AudioSource[] voices;
    private int nextVoice;
    private bool muted;
    private float fadeIn = 1f;

    void Awake()
    {
        instance = this;
        musicSource = NewMusicSource();
        fadingSource = NewMusicSource();
        voices = new AudioSource[Voices];
        for (int i = 0; i < Voices; i++) { voices[i] = gameObject.AddComponent<AudioSource>(); voices[i].playOnAwake = false; }
    }

    AudioSource NewMusicSource()
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.loop = true; s.playOnAwake = false;
        return s;
    }

    // la pista que corresponde a lo que se ve en pantalla
    AudioClip Wanted()
    {
        var gm = GameManager.I;
        AudioClip clip;
        if (gm == null || gm.State == GamePhase.Menu) clip = menuMusic;
        else if (gm.State == GamePhase.Won || gm.State == GamePhase.Break) clip = victoryMusic;
        else if (gm.State == GamePhase.Lost) clip = defeatMusic;
        else clip = gm.SectorIndex == 0 ? sector1Music : sector2Music;   // tutorial, juego y pausa
        return clip != null ? clip : music;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame) muted = !muted;

        var want = Wanted();
        if (want != musicSource.clip)
        {
            // la pista actual se apaga en fundido mientras entra la nueva desde el principio
            (musicSource, fadingSource) = (fadingSource, musicSource);
            musicSource.clip = want;
            if (want != null) musicSource.Play();
            fadeIn = 0f;
        }
        fadeIn = Mathf.Min(1f, fadeIn + Time.unscaledDeltaTime / FadeSeconds);
        if (fadeIn >= 1f && fadingSource.isPlaying) fadingSource.Stop();

        // baja en pausa y en la tienda para que se escuchen los SFX
        bool quiet = GameManager.I != null && (GameManager.I.State == GamePhase.Paused || Shop.AnyOpen);
        float baseVolume = muted ? 0f : musicVolume * (quiet ? 0.5f : 1f);
        musicSource.volume = baseVolume * fadeIn;
        fadingSource.volume = baseVolume * (1f - fadeIn);
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
