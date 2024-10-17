using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    private Rigidbody2D rb;
    public int maxHealth = 100;
    public int currHealth;
    private float Move;
    public float speed;
    public float jump;

    public HealthBar bar;

    public Vector2 Boxsize;
    public float castDistance;

    public LayerMask BGLayer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currHealth = maxHealth;
        bar.SetMaxHealth(maxHealth);
    }

    // Update is called once per frame
    void Update()
    {
    
        Move = Input.GetAxisRaw("Horizontal");
        // rb.AddForce(new Vector2(Move * speed, rb.velocity.y));
        rb.velocity = new Vector2(Move * speed, rb.velocity.y);
        // Debug.Log(isGrounded());/ß
        if (Input.GetButtonDown("Jump") && isGrounded())
        {
            rb.AddForce(new Vector2(rb.velocity.x, jump * 10));
            TakeDamage();
        }
    }
    public bool isGrounded()
    {
        if (Physics2D.BoxCast(transform.position, Boxsize, 0, -transform.up, castDistance, BGLayer))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position - transform.up * castDistance, Boxsize);
    }

    public void TakeDamage(){
        currHealth -= 20;
        bar.SetHealth(currHealth);
    }
}
