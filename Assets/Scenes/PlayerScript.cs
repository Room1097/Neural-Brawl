using System;  // Required for Math.Abs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    private Rigidbody2D rb;
    public Animator animator;
    public int maxHealth = 100;
    public int currHealth;
    private float Move;
    public float speed;
    public float jump;
    private bool facingRight = true;  // Track which direction the player is facing
    private bool canAttack = true;    // Track if the player can attack (cooldown logic)

    public float attackCooldown = 0.5f;  // Cooldown time in seconds

    public HealthBar bar;

    public Vector2 Boxsize;
    public GameObject attackPoint;
    public float castDistance;
    public float radius;

    public LayerMask BGLayer;
    public LayerMask Player;
    public KeyCode moveLeftKey;
    public KeyCode moveRightKey;
    public KeyCode jumpKey;
    public KeyCode attackKey;
    public KeyCode shieldKey; // Key to activate shield

    public bool InitialRight;

    // Shield variables
    public Sprite shieldSprite; // Reference to the shield sprite
    public Vector3 shieldScale = new Vector3(1f, 1f, 1f); // Scale of the shield
    public Vector3 shieldPositionOffset = new Vector3(1f, 0, 0); // Position offset of the shield
    private GameObject activeShield; // Current active shield
    public float shieldDuration = 2f; // Duration of the shield
    public float shieldCooldown = 5f; // Cooldown time for shield
    private bool shieldActive = false; // Track if the shield is active
    private bool canUseShield = true; // Track if the shield can be used

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currHealth = maxHealth;
        bar.SetMaxHealth(maxHealth);
        if (!InitialRight)
        {
            Flip();
        }
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        HandleAttack();
        HandleShield(); // Check for shield input
    }

    private void HandleMovement()
    {
        Move = 0; // Default movement to 0
        if (Input.GetKey(moveLeftKey))
        {
            Move = -1;
        }
        else if (Input.GetKey(moveRightKey))
        {
            Move = 1;
        }

        rb.linearVelocity = new Vector2(Move * speed, rb.linearVelocity.y);
        animator.SetFloat("xVelocity", Math.Abs(rb.linearVelocity.x));

        if (Move > 0 && !facingRight)
        {
            Flip();
        }
        else if (Move < 0 && facingRight)
        {
            Flip();
        }
    }

    private void HandleJump()
    {
        if (Input.GetKeyDown(jumpKey) && isGrounded())
        {
            rb.AddForce(new Vector2(0, jump * 10), ForceMode2D.Impulse);
        }
    }

    private void HandleAttack()
    {
        if (Input.GetKeyDown(attackKey) && canAttack)
        {
            Attack();
        }
    }

    private void HandleShield()
    {
        if (Input.GetKeyDown(shieldKey) && canUseShield)
        {
            ActivateShield();
        }
    }

    private void ActivateShield()
{
    shieldActive = true;
    canUseShield = false;

    // Create the shield GameObject and set its properties
    activeShield = new GameObject("Shield");
    SpriteRenderer spriteRenderer = activeShield.AddComponent<SpriteRenderer>();
    spriteRenderer.sprite = shieldSprite;
    spriteRenderer.sortingOrder = 1;

    // Set the shield's parent to the current player object
    activeShield.transform.SetParent(transform);

    // Position and scale the shield relative to the player
    Vector3 shieldPosition = transform.position + (facingRight ? shieldPositionOffset : new Vector3(-shieldPositionOffset.x, shieldPositionOffset.y, shieldPositionOffset.z));
    activeShield.transform.localPosition = shieldPositionOffset; // Set as local position offset
    activeShield.transform.localScale = shieldScale;

    StartCoroutine(ShieldCoroutine());
}


    private IEnumerator ShieldCoroutine()
    {
        yield return new WaitForSeconds(shieldDuration); // Wait for shield duration
        Destroy(activeShield); // Destroy the shield
        shieldActive = false;

        yield return new WaitForSeconds(shieldCooldown); // Wait for cooldown
        canUseShield = true; // Allow shield usage again
    }

    public bool isGrounded()
    {
        return Physics2D.BoxCast(transform.position, Boxsize, 0, -transform.up, castDistance, BGLayer);
    }

    private void OnDrawGizmos()
    {
        // Draw the box that checks for ground (isGrounded)
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position - transform.up * castDistance, Boxsize);

        // Draw the attack range at the attackPoint
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.transform.position, radius);
        }

        // Draw the shield position offset
        Gizmos.color = Color.blue; // Color for shield position offset
        Vector3 shieldPosition = transform.position + (facingRight ? shieldPositionOffset : new Vector3(-shieldPositionOffset.x, shieldPositionOffset.y, shieldPositionOffset.z));
        Gizmos.DrawLine(transform.position, shieldPosition); // Draw line to shield position
        Gizmos.DrawSphere(shieldPosition, 0.1f); // Draw a small sphere at the shield position
    }

    public void TakeDamage()
    {
        if (!shieldActive) // Only take damage if shield is not active
        {
            currHealth -= 20;
            currHealth = Mathf.Max(currHealth, 0); // Prevent negative health
            bar.SetHealth(currHealth);

            if (currHealth <= 0)
            {
                // Trigger death logic here (e.g., disable the player)
            }
        }
    }

    // Flip the player by rotating on the Y-axis
    private void Flip()
    {
        facingRight = !facingRight;
        transform.Rotate(0f, 180f, 0f);  // Rotate the player by 180 degrees around the Y-axis
    }

    // Trigger the attack animation
    private void Attack()
    {
        // Get all enemies in the attack radius
        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPoint.transform.position, radius, Player);

        // Loop through each enemy collider detected
        foreach (Collider2D enemy in enemies)
        {
            // Ensure the enemy is not the player itself
            if (enemy.gameObject != this.gameObject)
            {
                enemy.GetComponent<PlayerScript>().TakeDamage();  // Deal damage to the enemy
            }
        }

        canAttack = false;  // Disable further attacks during cooldown
        animator.SetBool("isAttacking", true);  // Start the attack animation
        Invoke("StopAttack", 0.2f);  // Stop the attack animation after 0.2 seconds
        Invoke("ResetAttack", attackCooldown);  // Reset the attack ability after cooldown
    }

    // Stop the attack animation
    private void StopAttack()
    {
        animator.SetBool("isAttacking", false);
    }

    // Reset the attack ability after cooldown
    private void ResetAttack()
    {
        canAttack = true;
    }
}
