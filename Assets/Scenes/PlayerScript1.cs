using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class PlayerAgent : Agent
{
    private Rigidbody2D rb;
    public Animator animator;
    public int maxHealth = 100;
    public int currHealth;
    private float Move;
    public float speed;
    public float jump;
    private bool facingRight = true;
    private bool canAttack = true;

    public float attackCooldown = 0.5f;
    public HealthBar bar;

    public Vector2 Boxsize;
    public GameObject attackPoint;
    public float castDistance;
    public float radius;

    public LayerMask BGLayer;
    public LayerMask Player;

    public bool InitialRight;

    public PlayerAgent enemyAgent;  // Reference to the other player (enemy agent)

    public override void Initialize()
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

    public override void OnEpisodeBegin()
    {
        // Reset health at the start of an episode
        currHealth = maxHealth;
        bar.SetHealth(currHealth);

        if (enemyAgent != null)
        {
            enemyAgent.ResetEnemy();  // Optionally reset the enemy's health as well
        }

        // Let Unity handle the starting positions
    }

    public void ResetEnemy()
    {
        // Reset only the enemy's health
        currHealth = maxHealth;
        bar.SetHealth(currHealth);
        rb.velocity = Vector2.zero;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Collect information about this player's state
        sensor.AddObservation(rb.velocity);
        sensor.AddObservation(currHealth);
        sensor.AddObservation(transform.position.x);
        sensor.AddObservation(transform.position.y);
        sensor.AddObservation(isGrounded() ? 1f : 0f);
        sensor.AddObservation(facingRight ? 1f : 0f);

        // Collect information about the enemy's state
        if (enemyAgent != null)
        {
            sensor.AddObservation(enemyAgent.transform.position.x);  // Enemy position (x)
            sensor.AddObservation(enemyAgent.transform.position.y);  // Enemy position (y)
            sensor.AddObservation(enemyAgent.currHealth);            // Enemy health
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Get movement and attack actions from the neural network
        float moveInput = actions.ContinuousActions[0];  // First action (continuous action) for moving left/right
        float jumpInput = actions.ContinuousActions[1];  // Second action (continuous action) for jumping
        int attackInput = actions.DiscreteActions[0];    // Discrete action for attacking

        // Apply movement based on network's actions
        Move = Mathf.Clamp(moveInput, -1f, 1f);
        rb.velocity = new Vector2(Move * speed, rb.velocity.y);

        // Update animator with horizontal velocity
        animator.SetFloat("xVelocity", Mathf.Abs(rb.velocity.x));

        // Flip player direction based on movement
        if (Move > 0 && !facingRight)
        {
            Flip();
        }
        else if (Move < 0 && facingRight)
        {
            Flip();
        }

        // Handle jump if the network signals a jump and the agent is grounded
        if (jumpInput > 0 && isGrounded())
        {
            rb.AddForce(new Vector2(rb.velocity.x, jump * 10));
        }

        // Handle attack if the network signals an attack and cooldown allows it
        if (attackInput > 0 && canAttack)
        {
            Attack();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetKey(KeyCode.A) ? -1f : (Input.GetKey(KeyCode.D) ? 1f : 0f);
        continuousActions[1] = Input.GetKey(KeyCode.Space) ? 1f : 0f;

        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Input.GetKey(KeyCode.Mouse0) ? 1 : 0;  // Left-click for attack
    }

    public bool isGrounded()
    {
        return Physics2D.BoxCast(transform.position, Boxsize, 0, -transform.up, castDistance, BGLayer);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position - transform.up * castDistance, Boxsize);

        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.transform.position, radius);
        }
    }

    public void TakeDamage()
    {
        currHealth -= 20;
        bar.SetHealth(currHealth);

        if (currHealth <= 0)
        {
            EndEpisode();  // End the episode if the player dies
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    private void Attack()
    {
        Collider2D enemy = Physics2D.OverlapCircle(attackPoint.transform.position, radius, Player);
        if (enemy && enemy.GetComponent<PlayerAgent>() == enemyAgent)  // Ensure the hit is on the enemy agent
        {
            Debug.Log("Enemy Hit");
            enemyAgent.TakeDamage();

            // Add reward for successful hit
            AddReward(1.0f);
        }
        canAttack = false;
        animator.SetBool("isAttacking", true);
        Invoke("StopAttack", 0.2f);
        Invoke("ResetAttack", attackCooldown);
    }

    private void StopAttack()
    {
        animator.SetBool("isAttacking", false);
    }

    private void ResetAttack()
    {
        canAttack = true;
    }
}
