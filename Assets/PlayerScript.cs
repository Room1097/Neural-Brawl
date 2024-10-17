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
    public float castDistance;

    public LayerMask BGLayer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currHealth = maxHealth;
        bar.SetMaxHealth(maxHealth);
    }

    void Update()
    {
        Move = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(Move * speed, rb.velocity.y);

        // Update animator with horizontal velocity
        animator.SetFloat("xVelocity", Math.Abs(rb.velocity.x));

        // Flip player direction based on movement
        if (Move > 0 && !facingRight)
        {
            Flip();
        }
        else if (Move < 0 && facingRight)
        {
            Flip();
        }

        // Handle jump
        if (Input.GetButtonDown("Jump") && isGrounded())
        {
            rb.AddForce(new Vector2(rb.velocity.x, jump * 10));
            TakeDamage();
        }

        // Handle attack with cooldown
        if (Input.GetKeyDown(KeyCode.W) && canAttack)
        {
            Attack();
        }
    }

    public bool isGrounded()
    {
        return Physics2D.BoxCast(transform.position, Boxsize, 0, -transform.up, castDistance, BGLayer);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position - transform.up * castDistance, Boxsize);
    }

    public void TakeDamage()
    {
        currHealth -= 20;
        bar.SetHealth(currHealth);
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
