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

    private Vector2 moveInput;
    private Vector2 facing = Vector2.down;
    private bool sprinting;
    private float nextKick, kickStart = -10f, pickupUntil, cheerUntil, hopStart = -10f;
    private bool struck;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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

    void OnPatada(InputValue value)
    {
        if (!value.isPressed || Time.timeScale == 0f || Time.time < nextKick) return;
        nextKick = Time.time + kickCooldown;
        kickStart = Time.time;
        struck = false;
        cheerUntil = 0f;
        AudioManager.Play(Sfx.Kick);
    }

    // Reacciones que otros scripts pueden pedir
    public void Pickup() => pickupUntil = Time.time + 0.18f;
    public void Cheer(float seconds) => cheerUntil = Time.unscaledTime + seconds;   // tiempo real: tambien en los paneles de fin
    public void Hop() => hopStart = Time.time;

    void Strike()
    {
        struck = true;
        Vector2 center = (Vector2)transform.position + Vector2.down * 0.2f + facing * 0.9f;
        foreach (var hit in Physics2D.OverlapCircleAll(center, 0.75f))
        {
            var inf = hit.GetComponent<Infractor>();
            if (inf == null || !inf.TryKick(transform.position)) continue;
            Instantiate(impactFx, inf.transform.position, Quaternion.identity);
            Instantiate(wordFx, inf.transform.position + Vector3.up * 0.8f, Quaternion.identity).GetComponent<FloatText>().Show(words[Random.Range(0, words.Length)]);
            AudioManager.Play(Sfx.Impact);
            ScreenShake.Shake(0.3f, 0.22f);
            StartCoroutine(HitStop());
        }
    }

    System.Collections.IEnumerator HitStop()
    {
        if (Time.timeScale != 1f) yield break;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(0.07f);
        if (Time.timeScale == 0.05f) Time.timeScale = 1f;
    }

    void FixedUpdate()
    {
        float t = Time.time - kickStart;
        if (Kicking)
            rb.linearVelocity = (t > kickTimes[1] && t < kickTimes[3]) ? facing * LungeSpeed : Vector2.zero;
        else
            rb.linearVelocity = moveInput * moveSpeed * (sprinting ? sprintMultiplier + SprintBonus : 1f);
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
