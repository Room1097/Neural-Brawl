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
    public bool jumping = false;
    private bool facingRight = true;  // Track which direction the player is facing

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

        // Update animator with velocity values
        animator.SetFloat("xVelocity", Math.Abs(rb.velocity.x));
        animator.SetFloat("yVelocity", rb.velocity.y);

        // Update jumping state
        bool isGroundedNow = isGrounded();
        animator.SetBool("Jumping", !isGroundedNow);

        if (Input.GetButtonDown("Jump") && isGroundedNow)
        {
            rb.AddForce(new Vector2(rb.velocity.x, jump * 10));
            jumping = true;
            TakeDamage();
        }

        if (isGroundedNow)
        {
            jumping = false;
        }

        // Flip player direction based on movement
        if (Move > 0 && !facingRight)
        {
            Flip();
        }
        else if (Move < 0 && facingRight)
        {
            Flip();
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
}
