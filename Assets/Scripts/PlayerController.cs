using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Status")]
    public bool isTeamRed;
    public bool isSelected = false;

    [Header("Visuals")]
    public GameObject selectionRing; // Drag the Yellow Circle Child here!

    [Header("Stats")]
    public float moveSpeed = 6f;
    public float kickForce = 25f;
    public float kickCooldown = 3.0f;

    private float lastKickTime = -10f;
    private Transform enemyGoal;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Vector2 movement;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    void Start()
    {
        // Auto-find goals based on team
        if (isTeamRed)
        {
            GameObject goal = GameObject.Find("Goal_Right");
            if (goal != null) enemyGoal = goal.transform;
        }
        else
        {
            GameObject goal = GameObject.Find("Goal_Left");
            if (goal != null) enemyGoal = goal.transform;
        }
    }

    public void ReceiveInput(Vector2 moveDir, bool kickPressed)
    {
        movement = moveDir;

        // --- VISUAL UPDATE ---
        // 1. Show Ring if selected
        if (selectionRing != null)
        {
            selectionRing.SetActive(isSelected);
        }

        // 2. Dim color if NOT selected (Optional, but helps)
        if (isSelected) spriteRenderer.color = originalColor;
        else spriteRenderer.color = originalColor * 0.7f;
        // ---------------------

        // Kick Logic with Cooldown Check
        if (kickPressed)
        {
            if (Time.time >= lastKickTime + kickCooldown)
            {
                CheckForKick();
            }
        }
    }

    void CheckForKick()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.2f);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Ball"))
            {
                PerformSuperShot(hit.gameObject);
                lastKickTime = Time.time;
                break;
            }
        }
    }

    void PerformSuperShot(GameObject ball)
    {
        Rigidbody2D ballRb = ball.GetComponent<Rigidbody2D>();
        if (ballRb != null && enemyGoal != null)
        {
            Vector2 shotDirection = (enemyGoal.position - ball.transform.position).normalized;

            ballRb.linearVelocity = Vector2.zero;
            ballRb.AddForce(shotDirection * kickForce, ForceMode2D.Impulse);

            if (AudioManager.instance) AudioManager.instance.PlaySFX(AudioManager.instance.kickSound);

            StartCoroutine(FlashEffect(Color.yellow));
        }
    }

    void FixedUpdate()
    {
        if (isSelected)
            rb.linearVelocity = movement.normalized * moveSpeed;
        else
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 2f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Tell the ball "I hit you last!" (For the Curse Logic)
        if (collision.gameObject.CompareTag("Ball"))
        {
            BallStatus status = collision.gameObject.GetComponent<BallStatus>();
            if (status != null) status.lastHitByRed = isTeamRed;
        }
    }

    IEnumerator FlashEffect(Color flashColor)
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(0.2f);
        spriteRenderer.color = originalColor;
    }
}