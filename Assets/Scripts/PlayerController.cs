using UnityEngine;
using UnityEngine.InputSystem;

// Movimiento, patada y animaciones del guardian. El sprite vive en un hijo ("Visual") para poder
// inclinarlo, estirarlo y hacerlo saltar sin deformar el collider.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.6f;
    [SerializeField] private float kickCooldown = 0.45f;
    [SerializeField] private GameObject kickSwoosh;      // arco blanco, hijo del jugador
    [SerializeField] private GameObject impactFx;        // particulas al conectar la patada
    [SerializeField] private GameObject wordFx;          // "¡PAF!" flotante
    [SerializeField] private ParticleSystem dust;
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Sprite[] sheet;             // hoja base: 6 columnas; filas 0-2 quieto (abajo/lado/arriba), 3-5 caminar
    [SerializeField] private Sprite[] kickSide, kickDown, kickUp;   // 4 fotogramas cada una
    [SerializeField] private Sprite[] crouch;            // lado, frente, espalda
    [SerializeField] private Sprite[] cheer;             // 2 fotogramas, brazos arriba
    [SerializeField] private bool sideSpritesFaceLeft;   // la hoja mira a la derecha

    public float SprintBonus { get; set; }   // Botas de Sprint

    private static readonly float[] kickTimes = { 0f, 0.07f, 0.15f, 0.23f };   // inicio de cada fotograma
    private const float KickLength = 0.32f, LungeSpeed = 4f;
    private static readonly string[] words = { "¡PAF!", "¡ZAS!", "¡PUM!", "¡TOMA!" };

    private const float ChargeMin = 0.2f, ChargeFull = 1.1f;   // segundos de espacio para empezar a cargar / carga maxima
    private static readonly string[] powerWords = { "¡BOOM!", "¡BOLOS!", "¡PATADÓN!" };

    private Vector2 moveInput;
    private Vector2 facing = Vector2.down;
    private bool sprinting;
    private float nextKick, kickStart = -10f, pickupUntil, cheerUntil, hopStart = -10f;
    private bool struck;
    private Rigidbody2D rb;

    private bool charging;
    private float chargeStart, kickPower, nextTick;
    private SpriteRenderer chargeBack, chargeFill;

    // 0 = suelta rapido (patada normal); 1 = carga maxima
    float ChargeLevel => charging ? Mathf.Clamp01((Time.time - chargeStart - ChargeMin) / (ChargeFull - ChargeMin)) : 0f;
    bool ChargingVisible => charging && Time.time - chargeStart > ChargeMin;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        var px = PixelSprite.Make(new[] { "w" }, new System.Collections.Generic.Dictionary<char, Color32> { ['w'] = new Color32(255, 255, 255, 255) }, 0.5f);
        chargeBack = MakeBar("CargaFondo", px, new Color(0.05f, 0.06f, 0.12f, 0.85f), 19);
        chargeFill = MakeBar("CargaRelleno", px, Color.white, 20);
        chargeBack.transform.localScale = new Vector3(1.3f * 16f, 0.2f * 16f, 1f);
    }

    SpriteRenderer MakeBar(string name, Sprite sprite, Color color, int order)
    {
        var g = new GameObject(name);
        g.transform.SetParent(transform, false);
        g.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        var r = g.AddComponent<SpriteRenderer>();
        r.sprite = sprite; r.color = color; r.sortingLayerName = "Decoration"; r.sortingOrder = order; r.enabled = false;
        return r;
    }

    bool Kicking => Time.time - kickStart < KickLength;

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        if (moveInput.sqrMagnitude > 0.01f)
            facing = Mathf.Abs(moveInput.x) > Mathf.Abs(moveInput.y) ? new Vector2(Mathf.Sign(moveInput.x), 0) : new Vector2(0, Mathf.Sign(moveInput.y));
    }

    void OnSprint(InputValue value)
    {
        sprinting = value.isPressed;
    }

    // Espacio: un toque corto da la patada normal al soltar; si se mantiene, se carga y la patada sale mas fuerte
    void OnPatada(InputValue value)
    {
        if (value.isPressed)
        {
            if (Time.timeScale == 0f || Time.time < nextKick || Kicking || !KickHeld()) return;
            charging = true;
            chargeStart = Time.time;
            nextTick = ChargeMin;
            return;
        }
        ReleaseKick();
    }

    // estado real de los controles de patada: si se pierde un evento de soltar, la carga no se queda trabada
    static bool KickHeld() =>
        (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) ||
        (Mouse.current != null && Mouse.current.leftButton.isPressed) ||
        (Gamepad.current != null && Gamepad.current.buttonWest.isPressed);

    void ReleaseKick()
    {
        if (!charging) return;
        charging = false;
        float p = Time.time - chargeStart < ChargeMin ? 0f : Mathf.Clamp01((Time.time - chargeStart - ChargeMin) / (ChargeFull - ChargeMin));
        if (Time.timeScale == 0f) return;
        kickPower = p;
        nextKick = Time.time + kickCooldown + 0.5f * p;
        kickStart = Time.time;
        struck = false;
        cheerUntil = 0f;
        AudioManager.Play(p > 0.3f ? Sfx.PowerKick : Sfx.Kick);
    }

    // Reacciones que otros scripts pueden pedir
    public void Pickup() => pickupUntil = Time.time + 0.18f;
    public void Cheer(float seconds) => cheerUntil = Time.unscaledTime + seconds;   // tiempo real: tambien en los paneles de fin
    public void Hop() => hopStart = Time.time;

    void Strike()
    {
        struck = true;
        float p = kickPower;
        Vector2 center = (Vector2)transform.position + Vector2.down * 0.2f + facing * (0.9f + 0.5f * p);
        if (p > 0.2f) GameManager.I.ResetChain();
        foreach (var hit in Physics2D.OverlapCircleAll(center, 0.75f + 0.4f * p))
        {
            var inf = hit.GetComponent<Infractor>();
            if (inf == null || !inf.TryKick(transform.position, p)) continue;
            Instantiate(impactFx, inf.transform.position, Quaternion.identity).transform.localScale *= 1f + p;
            string word = p > 0.5f ? powerWords[Random.Range(0, powerWords.Length)] : words[Random.Range(0, words.Length)];
            Instantiate(wordFx, inf.transform.position + Vector3.up * 0.8f, Quaternion.identity).GetComponent<FloatText>().Show(word);
            AudioManager.Play(Sfx.Impact);
            ScreenShake.Shake(0.3f + 0.6f * p, 0.22f + 0.2f * p);
            StartCoroutine(HitStop(p));
            if (p >= 0.95f) Achievements.Unlock("patadon");
        }
    }

    System.Collections.IEnumerator HitStop(float power = 0f)
    {
        if (Time.timeScale != 1f) yield break;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(0.07f + 0.08f * power);
        if (Time.timeScale == 0.05f) Time.timeScale = 1f;
    }

    void FixedUpdate()
    {
        float t = Time.time - kickStart;
        if (Kicking)
            rb.linearVelocity = (t > kickTimes[1] && t < kickTimes[3]) ? facing * LungeSpeed * (1f + 1.2f * kickPower) : Vector2.zero;
        else if (ChargingVisible)
            rb.linearVelocity = moveInput * moveSpeed * 0.4f;   // cargando: avanza despacio
        else
            rb.linearVelocity = moveInput * moveSpeed * (sprinting ? sprintMultiplier + SprintBonus : 1f);
    }

    // barra de carga sobre la cabeza, tics de sonido que suben de tono y destello al llegar al maximo
    void UpdateCharge()
    {
        if (charging && (Time.timeScale == 0f || !GameManager.I.Running)) charging = false;   // pausa o menu: se cancela
        if (charging && !KickHeld()) ReleaseKick();
        bool show = ChargingVisible;
        chargeBack.enabled = chargeFill.enabled = show;
        if (!show) return;
        float lvl = ChargeLevel;
        chargeFill.color = Color.Lerp(new Color(1f, 0.9f, 0.3f), new Color(1f, 0.25f, 0.15f), lvl);
        if (lvl >= 1f) chargeFill.color = Color.Lerp(chargeFill.color, Color.white, Mathf.Abs(Mathf.Sin(Time.time * 18f)));
        float w = 1.2f * Mathf.Max(lvl, 0.04f);
        chargeFill.transform.localPosition = new Vector3(-0.6f + w / 2f, 1.55f, 0f);
        chargeFill.transform.localScale = new Vector3(w * 16f, 0.12f * 16f, 1f);
        if (Time.time - chargeStart >= nextTick && lvl < 1f)
        {
            nextTick += 0.16f;
            AudioManager.Play(Sfx.Charge, 0.8f + 1.3f * lvl, 0.7f);
        }
        else if (lvl >= 1f && nextTick < 90f)
        {
            nextTick = 99f;
            AudioManager.Play(Sfx.Charge, 2.6f);
            ScreenShake.Shake(0.08f, 0.1f);
        }
    }

    void Update()
    {
        // anima segun la velocidad real: contra una pared o un obstaculo no camina en el sitio
        bool moving = rb.linearVelocity.sqrMagnitude > 0.04f && !Kicking;
        bool sideways = facing.x != 0;
        bool celebrating = Time.unscaledTime < cheerUntil && !moving;
        int dir = sideways ? 1 : (facing.y > 0 ? 2 : 0);          // 0 abajo, 1 lado, 2 arriba
        Sprite sprite;

        if (Kicking)
        {
            float t = Time.time - kickStart;
            int f = t >= kickTimes[3] ? 3 : t >= kickTimes[2] ? 2 : t >= kickTimes[1] ? 1 : 0;
            if (f >= 1 && !struck) Strike();
            sprite = (dir == 1 ? kickSide : dir == 0 ? kickDown : kickUp)[f];
        }
        else if (ChargingVisible)
            sprite = crouch[dir];
        else if (celebrating)
            sprite = cheer[(int)(Time.unscaledTime * 5f) % 2];
        else if (Time.time < pickupUntil)
            sprite = crouch[dir];
        else
        {
            int row = dir + (moving ? 3 : 0);
            int frame = (int)(Time.time * (moving ? (sprinting ? 12f : 8f) : 3f)) % 6;
            sprite = sheet[row * 6 + frame];
        }
        visual.sprite = sprite;
        if (sideways && !celebrating) visual.flipX = sideSpritesFaceLeft ? facing.x > 0 : facing.x < 0;
        else if (celebrating) visual.flipX = false;

        Animate(moving);
        UpdateCharge();
    }

    // rebote, inclinacion al correr, agachado y salto (todo sobre el hijo Visual)
    void Animate(bool moving)
    {
        var v = visual.transform;
        float bob = moving ? Mathf.Abs(Mathf.Sin(Time.time * (sprinting ? 14f : 9f))) * (sprinting ? 0.07f : 0.04f) : 0f;
        float hopT = Time.time - hopStart;
        float hop = hopT < 0.3f ? Mathf.Sin(hopT / 0.3f * Mathf.PI) * 0.35f : 0f;
        Vector3 targetScale = Vector3.one;
        float tilt = 0f;
        if (moving && sprinting) { targetScale = new Vector3(0.95f, 1.07f, 1f); tilt = facing.x != 0 ? -7f * facing.x : 0f; }
        if (Time.time < pickupUntil) targetScale = new Vector3(1.08f, 0.92f, 1f);
        if (ChargingVisible) targetScale = new Vector3(1f + 0.12f * ChargeLevel, 0.92f - 0.1f * ChargeLevel, 1f);   // se agacha tomando impulso
        v.localPosition = new Vector3(0f, bob + hop, 0f);
        v.localScale = Vector3.Lerp(v.localScale, targetScale, Time.unscaledDeltaTime * 14f);
        v.localRotation = Quaternion.Lerp(v.localRotation, Quaternion.Euler(0f, 0f, tilt), Time.unscaledDeltaTime * 14f);

        kickSwoosh.SetActive(Kicking && Time.time - kickStart >= kickTimes[1] && Time.time - kickStart < kickTimes[3]);
        if (kickSwoosh.activeSelf)
        {
            kickSwoosh.transform.localPosition = facing * 0.95f + Vector2.down * 0.2f;
            kickSwoosh.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg);
        }
        var em = dust.emission;
        em.rateOverDistance = sprinting ? 14f : 5f;
    }
}
