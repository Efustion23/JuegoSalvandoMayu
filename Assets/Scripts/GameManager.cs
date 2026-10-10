using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GamePhase { Menu, Tutorial, Playing, Break, Paused, Won, Lost }

[Serializable]
public class SectorConfig
{
    public string name;
    public float duration = 120f;
    public float minX, maxX;                       // infractores solo aparecen en este rango de x
    public float startInterval = 12f, minInterval = 6f;
    public int maxAlive = 3;
    public float waveAt;                           // segundo del sector en que llega una oleada (0 = sin oleada)
    public int waveSize = 3;
    [Range(0f, 1f)] public float runnerChance;     // fraccion de infractores corredores
    public Vector2 start;                          // donde aparece el guardian
    public Color lightColor = Color.white;         // ambiente del sector
    public float lightIntensity = 0.9f;
    public float lampIntensity = 0.9f;
}

// Flujo del juego: menu -> (tutorial) -> sector 1 -> sector 2 -> victoria/derrota. Cada sector tiene su Pureza:
// hay que terminar cada uno con Pureza >= winPurity.
public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }
    private static bool skipMenu;    // sobrevive a la recarga de escena ("Jugar de nuevo")

    [SerializeField] private RiverPurity[] purities;    // una por sector
    [SerializeField] private SectorConfig[] sectors;
    [SerializeField] private Backpack backpack;
    [SerializeField] private PlayerController player;
    [SerializeField] private InfractorSpawner spawner;
    [SerializeField] private Tutorial tutorial;
    [SerializeField] private ScreenFader fader;
    [SerializeField] private UnityEngine.Rendering.Universal.Light2D globalLight;
    [SerializeField] private float winPurity = 80f, startPurity = 60f;
    [SerializeField] private Text purityText, timeText, creditsText, backpackText, statsText, alertText, bannerText;
    [SerializeField] private Text menuInfoText, breakText, endText, menuFactText, pauseFactText;
    [SerializeField] private GameObject menuPanel, pausePanel, breakPanel, endPanel, quitButton;

    public GamePhase State { get; private set; }
    public RiverPurity Purity => purities[sectorIndex];
    public int Credits => Progress.Data.credits;
    public PlayerController Player => player;
    public IReadOnlyList<SectorConfig> Sectors => sectors;
    public SectorConfig Sector => sectors[sectorIndex];
    public int SectorIndex => sectorIndex;
    public float SectorTime => Sector.duration - timeLeft;
    public int Scared { get; private set; }
    public int Convinced { get; private set; }
    public int Polluted { get; private set; }
    public int Recycled { get; set; }
    public int Rescued { get; private set; }
    public int Planted { get; private set; }
    public int Streak { get; private set; }
    public int BestStreak { get; private set; }
    public bool Ended => State == GamePhase.Won || State == GamePhase.Lost;
    public bool Running => State == GamePhase.Playing || State == GamePhase.Tutorial;

    private int sectorIndex, chainCount;
    private float timeLeft, alertUntil, bannerUntil, lastChain;
    private UnityEngine.UI.Image[] breakStars, endStars;
    private GamePhase beforePause;
    private Coroutine ambience;

    void Awake()
    {
        I = this;
        foreach (var p in new[] { menuPanel, pausePanel, breakPanel, endPanel }) p.SetActive(false);
        breakStars = Achievements.MakeStars((RectTransform)breakPanel.transform, new Vector2(0f, 100f));
        endStars = Achievements.MakeStars((RectTransform)endPanel.transform, new Vector2(0f, 100f));
        alertText.text = bannerText.text = "";
#if UNITY_WEBGL
        quitButton.SetActive(false);   // el navegador no se cierra desde el juego
#endif
    }

    void Start()
    {
        ApplyUpgrades();
        SetAmbience(sectors[0], 0f);
        if (skipMenu) { skipMenu = false; BeginRun(); }
        else ShowMenu();
        StartCoroutine(fader.Fade(0f, 0.7f));   // revela la escena
    }

    // ---------- economia y mejoras ----------
    public void AddCredits(int n)
    {
        Progress.Data.credits += n;
        Progress.Save();
    }

    public bool TrySpend(int n)
    {
        if (Progress.Data.credits < n) return false;
        Progress.Data.credits -= n;
        Progress.Save();
        return true;
    }

    public void ApplyUpgrades()
    {
        backpack.SetBonus(3 * Progress.Data.backpack);
        player.SprintBonus = 0.3f * Progress.Data.boots;
        Pet.Apply(Progress.Data.pet);
    }

    // ---------- estadisticas del sector ----------
    // racha: infractores ahuyentados seguidos sin que nadie contamine; desde x2 da Eco-Creditos extra
    public void RegisterKick()
    {
        Scared++;
        Progress.Data.totalScared++;
        Achievements.Unlock("primera_patada");
        Combo();
    }

    // concientizar tambien cuenta para la racha: ahuyentar sin violencia da mas creditos y algo de pureza
    public void RegisterConvince()
    {
        Convinced++;
        Progress.Data.totalConvinced++;
        if (Progress.Data.totalConvinced >= 5) Achievements.Unlock("orador");
        AddCredits(12);
        Purity.Add(1.5f);
        Alert("¡Concientizado! +12 Eco-Créditos · Pureza +1.5%", 2.5f);
        Combo();
    }

    void Combo()
    {
        Streak++;
        BestStreak = Mathf.Max(BestStreak, Streak);
        if (Streak >= 5) Achievements.Unlock("racha5");
        if (Streak < 2) return;
        int bonus = 5 * Mathf.Min(Streak, 5);
        AddCredits(bonus);
        AudioManager.Play(Sfx.Combo, 0.9f + 0.09f * Mathf.Min(Streak, 10));   // cada racha suena un poco mas aguda
        Alert($"¡Racha x{Streak}! +{bonus} Eco-Créditos", 2f);
    }

    // patada cargada: cada infractor que el lanzado derriba suma al "pleno"
    public void ResetChain() { chainCount = 1; lastChain = Time.time; }

    public void RegisterChain()
    {
        if (Time.time - lastChain > 1.5f) chainCount = 1;
        lastChain = Time.time;
        chainCount++;
        int bonus = 10 * (chainCount - 1);
        AddCredits(bonus);
        if (chainCount == 2) Alert($"¡Choque! +{bonus} Eco-Créditos", 2f);
        else
        {
            Alert($"¡PLENO x{chainCount}! +{bonus} Eco-Créditos", 2.5f);
            AudioManager.Play(Sfx.Strike, 1f);
            Achievements.Unlock("pleno");
        }
    }

    // una bolsa llego al rio; la de un infractor corta la racha, la que arrastra la lluvia no
    public void RegisterPollution(bool breaksStreak = true)
    {
        Polluted++;
        if (breaksStreak) Streak = 0;
    }

    public void RegisterRescue()
    {
        Rescued++;
        if (Rescued >= 5) Achievements.Unlock("rescatista");
    }

    public void RegisterPlant()
    {
        Planted++;
        if (Planted >= 3) Achievements.Unlock("quenas");
    }

    // ---------- avisos ----------
    public void Alert(string msg, float seconds = 3f)
    {
        alertText.text = msg;
        alertUntil = Time.unscaledTime + seconds;
    }

    public void Banner(string msg, float seconds)
    {
        bannerText.text = msg;
        bannerUntil = Time.unscaledTime + seconds;
    }

    // ---------- botones de los menus (se conectan en la escena) ----------
    public void OnPlay() { AudioManager.Play(Sfx.Click); StartCoroutine(Transition(() => { menuPanel.SetActive(false); BeginRun(); })); }
    public void OnTutorial() { AudioManager.Play(Sfx.Click); StartCoroutine(Transition(() => { menuPanel.SetActive(false); StartTutorial(); })); }
    public void OnResume() => Pause(false);
    public void OnRestart() => StartCoroutine(Reload(true));
    public void OnMenu() => StartCoroutine(Reload(false));
    public void OnNextSector() { AudioManager.Play(Sfx.Click); StartCoroutine(Transition(() => { breakPanel.SetActive(false); StartSector(sectorIndex + 1); })); }

    // fundido a negro, cambio de estado y fundido de vuelta
    IEnumerator Transition(Action change)
    {
        yield return fader.Fade(1f, 0.3f);
        change();
        yield return fader.Fade(0f, 0.5f);
    }

    IEnumerator Reload(bool direct)
    {
        yield return fader.Fade(1f, 0.3f);
        skipMenu = direct;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // luz del sector: cambia suave el color de la luz global y la fuerza de las farolas
    void SetAmbience(SectorConfig s, float seconds)
    {
        if (ambience != null) StopCoroutine(ambience);
        ambience = StartCoroutine(FadeAmbience(s, seconds));
    }

    IEnumerator FadeAmbience(SectorConfig s, float seconds)
    {
        var lamps = new List<UnityEngine.Rendering.Universal.Light2D>();
        foreach (var l in FindObjectsByType<UnityEngine.Rendering.Universal.Light2D>(FindObjectsSortMode.None))
            if (l.lightType == UnityEngine.Rendering.Universal.Light2D.LightType.Point) lamps.Add(l);
        Color c0 = globalLight.color; float i0 = globalLight.intensity, l0 = lamps.Count > 0 ? lamps[0].intensity : s.lampIntensity;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float k = t / seconds;
            globalLight.color = Color.Lerp(c0, s.lightColor, k); globalLight.intensity = Mathf.Lerp(i0, s.lightIntensity, k);
            foreach (var l in lamps) l.intensity = Mathf.Lerp(l0, s.lampIntensity, k);
            yield return null;
        }
        globalLight.color = s.lightColor; globalLight.intensity = s.lightIntensity;
        foreach (var l in lamps) l.intensity = s.lampIntensity;
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- flujo ----------
    void ShowMenu()
    {
        State = GamePhase.Menu;
        Time.timeScale = 0f;
        menuInfoText.text = $"Récord de pureza {Progress.Data.bestPurity:0}%  ·  Eco-Créditos {Credits}  ·  Estrellas {Achievements.TotalStars()}/6  ·  Logros {Achievements.Count}/12";
        menuFactText.text = $"¿Sabías que? {WaterFacts.Random()}";
        menuPanel.SetActive(true);
    }

    void BeginRun()
    {
        if (Progress.Data.tutorialDone) StartSector(0);
        else StartTutorial();
    }

    void StartTutorial()
    {
        sectorIndex = 0;
        State = GamePhase.Tutorial;
        Time.timeScale = 1f;
        Purity.Set(startPurity);
        Warp(sectors[0].start);
        tutorial.Begin();
    }

    public void FinishTutorial()
    {
        Progress.Data.tutorialDone = true;
        Progress.Save();
        StartSector(0);
    }

    void StartSector(int i)
    {
        sectorIndex = i;
        var s = sectors[i];
        foreach (var inf in new List<Infractor>(Infractor.Active)) Destroy(inf.gameObject);
        foreach (var f in FindObjectsByType<FloatingTrash>(FindObjectsSortMode.None)) Destroy(f.gameObject);
        backpack.TakeAll();
        Scared = Convinced = Polluted = Recycled = Rescued = Planted = Streak = BestStreak = chainCount = 0;
        timeLeft = s.duration;
        Purity.Set(startPurity);
        Warp(s.start);
        spawner.Configure(s);
        Pet.Teleport(s.start);
        SetAmbience(s, 2.5f);
        State = GamePhase.Playing;
        Time.timeScale = 1f;
        Banner($"SECTOR {i + 1} · {s.name}", 3.5f);
    }

    void Warp(Vector2 pos)
    {
        Vector3 delta = (Vector3)pos - player.transform.position;
        player.transform.position = pos;
        player.GetComponent<Rigidbody2D>().position = pos;
        player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        var cam = FindFirstObjectByType<CinemachineCamera>();
        if (cam != null) cam.OnTargetObjectWarped(player.transform, delta);
    }

    void Pause(bool on)
    {
        if (on) { beforePause = State; State = GamePhase.Paused; }
        else State = beforePause;
        Time.timeScale = on ? 0f : 1f;
        AudioManager.Play(Sfx.Click);
        if (on) pauseFactText.text = $"¿Sabías que? {WaterFacts.Random()}\n\n<size=20><color=#ffd84a>L: logros</color></size>";
        pausePanel.SetActive(on);
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (Time.unscaledTime > alertUntil) alertText.text = "";
        if (Time.unscaledTime > bannerUntil) bannerText.text = "";

        switch (State)
        {
            case GamePhase.Menu:
                if (kb.enterKey.wasPressedThisFrame) OnPlay();
                break;
            case GamePhase.Tutorial:
            case GamePhase.Playing:
                UpdateHud();
                if (State == GamePhase.Playing) TickTimer();
                if (kb.escapeKey.wasPressedThisFrame && !Shop.AnyOpen) Pause(true);
                break;
            case GamePhase.Paused:
                if (kb.escapeKey.wasPressedThisFrame) Pause(false);
                else if (kb.rKey.wasPressedThisFrame) OnRestart();
                else if (kb.mKey.wasPressedThisFrame) OnMenu();
                break;
            case GamePhase.Break:
                if (kb.enterKey.wasPressedThisFrame) OnNextSector();
                break;
            case GamePhase.Won:
            case GamePhase.Lost:
                if (kb.rKey.wasPressedThisFrame) OnRestart();
                else if (kb.mKey.wasPressedThisFrame) OnMenu();
                break;
        }
    }

    void UpdateHud()
    {
        purityText.text = $"Pureza {Purity.Purity:0}%";
        int t = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
        timeText.text = State == GamePhase.Tutorial ? "Tutorial" : $"S{sectorIndex + 1} {t / 60}:{t % 60:00}";
        creditsText.text = $"{Credits}";
        backpackText.text = $"{backpack.Count}/{backpack.Capacity}";
        statsText.text = $"Fuera {Scared + Convinced} Río {Polluted} Racha x{Streak}";
    }

    void TickTimer()
    {
        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f) EndSector();
    }

    void EndSector()
    {
        float p = Purity.Purity;
        Progress.Data.bestPurity = Mathf.Max(Progress.Data.bestPurity, p);
        Progress.Save();
        bool last = sectorIndex == sectors.Length - 1;
        Time.timeScale = 0f;
        foreach (var inf in new List<Infractor>(Infractor.Active)) Destroy(inf.gameObject);
        int stars = p >= winPurity ? 1 : 0;
        bool star2 = p >= 90f, star3 = BestStreak >= 5 && Recycled >= 8;
        if (stars > 0) stars += (star2 ? 1 : 0) + (star3 ? 1 : 0);
        if (stars > 0)
        {
            Progress.Data.stars[sectorIndex] = Mathf.Max(Progress.Data.stars[sectorIndex], stars);
            if (Polluted == 0) Achievements.Unlock("cero_bolsas");
            if (stars == 3) Achievements.Unlock("constelacion");
            if (last) Achievements.Unlock("rio_limpio");
            Progress.Save();
        }
        string Gold(bool ok, string t) => ok ? $"<color=#ffd84a>{t}</color>" : $"<color=#6f7d94>{t}</color>";
        string starLine = stars > 0
            ? $"<size=44> \n</size><size=20>{Gold(true, "Certificado")}  ·  {Gold(star2, "Pureza 90%")}  ·  {Gold(star3, "Racha x5 y 8 recicladas")}</size>\n"   // el hueco de arriba es para las estrellas
            : "";
        string summary = starLine + $"<size=20>Recicladas {Recycled} · Rescatadas del río {Rescued} · Llegaron al río {Polluted}\n"
                       + $"Ahuyentados {Scared} · Concientizados {Convinced} · Mejor racha x{BestStreak} · Queñuas plantadas {Planted}</size>";
        string fact = $"\n<size=20><color=#bfe8a8>{WaterFacts.Wrap("¿Sabías que? " + WaterFacts.Random(), 62)}</color></size>";

        if (p < winPurity)
        {
            State = GamePhase.Lost;
            AudioManager.Play(Sfx.Lose, 1f);
            foreach (var st in endStars) st.gameObject.SetActive(false);
            endText.text = $"El río sigue contaminado\nSector {sectorIndex + 1} sin certificar\n\nPureza {p:0}% (meta {winPurity:0}%)\n{summary}\nEco-Créditos {Credits}{fact}";
            endPanel.SetActive(true);
        }
        else if (last)
        {
            State = GamePhase.Won;
            AudioManager.Play(Sfx.Win, 1f);
            player.Cheer(5f);
            foreach (var st in endStars) st.gameObject.SetActive(true);
            Achievements.ShowStars(endStars, stars, this);
            endText.text = $"¡RÍO LIMPIO!\nCertificación lograda: el Qhali vuelve a la vida\n\nPureza final {p:0}% (récord {Progress.Data.bestPurity:0}%)\n{summary}\nEco-Créditos {Credits}{fact}";
            endPanel.SetActive(true);
        }
        else
        {
            State = GamePhase.Break;
            AudioManager.Play(Sfx.Win, 1f);
            player.Cheer(5f);
            Achievements.ShowStars(breakStars, stars, this);
            breakText.text = $"Sector {sectorIndex + 1} certificado\nPureza {p:0}%\n{summary}\nSiguiente: Sector {sectorIndex + 2} · {sectors[sectorIndex + 1].name}{fact}";
            breakPanel.SetActive(true);
        }
    }
}
