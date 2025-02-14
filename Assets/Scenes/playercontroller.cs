using System.Collections;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    public Animator animator;
    public int maxHealth = 100;
    public int currHealth;
    private float move;
    public float speed;
    public float jump;
    private bool facingRight = true;
    private bool canAttack = true;
    public HealthBar bar;

    public Vector2 Boxsize;
    public GameObject attackPoint;
    public float castDistance;
    public float radius;

    public LayerMask BGLayer;
    public LayerMask Player;

    public bool InitialRight;
    public PlayerController enemy;

    // Shield variables
    public Sprite shieldSprite;
    public Vector3 shieldScale = new Vector3(1f, 1f, 1f);
    public Vector3 shieldPositionOffset = new Vector3(1f, 0, 0);
    private GameObject activeShield;
    public float shieldDuration = 2f;
    public float shieldCooldown = 5f;
    private bool shieldActive = false;
    private bool canUseShield = true;

    // Input Keys
    public KeyCode moveLeftKey;
    public KeyCode moveRightKey;
    public KeyCode jumpKey;
    public KeyCode attackKey;
    public KeyCode shieldKey;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currHealth = maxHealth;
        if (bar != null) bar.SetMaxHealth(maxHealth);
        if (!InitialRight) Flip();
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        move = 0;
        if (Input.GetKey(moveLeftKey)) move = -1;
        if (Input.GetKey(moveRightKey)) move = 1;
        rb.linearVelocity = new Vector2(move * speed, rb.linearVelocity.y);
        animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));

        if (move > 0 && !facingRight) Flip();
        if (move < 0 && facingRight) Flip();

        if (Input.GetKeyDown(jumpKey) && isGrounded())
        {
            rb.AddForce(new Vector2(0, jump * 10));
        }

        if (Input.GetKeyDown(attackKey) && canAttack)
        {
            Attack();
        }

        if (Input.GetKeyDown(shieldKey) && canUseShield)
        {
            ActivateShield();
        }
    }

    bool isGrounded()
    {
        return Physics2D.BoxCast(transform.position, Boxsize, 0, -transform.up, castDistance, BGLayer);
    }

    void ActivateShield()
    {
        if (activeShield != null || !canUseShield) return;

        shieldActive = true;
        canUseShield = false;
        activeShield = new GameObject("Shield");
        SpriteRenderer spriteRenderer = activeShield.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = shieldSprite;
        spriteRenderer.sortingOrder = 1;

        Vector3 offset = facingRight ? shieldPositionOffset : new Vector3(-shieldPositionOffset.x, shieldPositionOffset.y, 0);
        activeShield.transform.SetParent(transform);
        activeShield.transform.localPosition = offset;
        activeShield.transform.localScale = shieldScale;

        StartCoroutine(ShieldCoroutine());
    }

    IEnumerator ShieldCoroutine()
    {
        yield return new WaitForSeconds(shieldDuration);
        Destroy(activeShield);
        shieldActive = false;
        yield return new WaitForSeconds(shieldCooldown);
        canUseShield = true;
    }

    void Attack()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPoint.transform.position, radius, Player);
        foreach (Collider2D enemyCollider in enemies)
        {
            if (enemyCollider.gameObject != gameObject)
            {
                PlayerController enemy = enemyCollider.GetComponent<PlayerController>();
                if (enemy.shieldActive)
                {
                    Debug.Log("Enemy's shield absorbed the attack");
                }
                else
                {
                    enemy.TakeDamage();
                }
            }
        }
        canAttack = false;
        animator.SetBool("isAttacking", true);
        Invoke("StopAttack", 0.2f);
        Invoke("ResetAttack", 0.5f);
    }

    void StopAttack()
    {
        animator.SetBool("isAttacking", false);
    }

    void ResetAttack()
    {
        canAttack = true;
    }

    public void TakeDamage()
    {
        if (!shieldActive)
        {
            currHealth -= 20;
            bar.SetHealth(currHealth);
            if (currHealth <= 0)
            {
                Debug.Log("Player defeated");
            }
        }
        else
        {
            Debug.Log("Shield blocked the damage.");
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }
}
