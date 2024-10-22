using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;  // Import this to use BehaviorParameters

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

    private BehaviorParameters behaviorParameters;  // Reference to BehaviorParameters component

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currHealth = maxHealth;
        bar.SetMaxHealth(maxHealth);

        behaviorParameters = GetComponent<BehaviorParameters>();

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
    }

    public void ResetEnemy()
    {
        currHealth = maxHealth;
        bar.SetHealth(currHealth);
        rb.velocity = Vector2.zero;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(rb.velocity);
        sensor.AddObservation(currHealth);
        sensor.AddObservation(transform.position.x);
        sensor.AddObservation(transform.position.y);
        sensor.AddObservation(isGrounded() ? 1f : 0f);
        sensor.AddObservation(facingRight ? 1f : 0f);

        if (enemyAgent != null)
        {
            sensor.AddObservation(enemyAgent.transform.position.x);
            sensor.AddObservation(enemyAgent.transform.position.y);
            sensor.AddObservation(enemyAgent.currHealth);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int moveAction = actions.DiscreteActions[0];
        int jumpAction = actions.DiscreteActions[1];
        int attackAction = actions.DiscreteActions[2];

        // Print the actions received for debugging
        Debug.Log($"Decisions: Move = {moveAction}, Jump = {jumpAction}, Attack = {attackAction}");

        if (enemyAgent != null)
        {
            float distanceToEnemy = enemyAgent.transform.position.x - transform.position.x;
            Move = (moveAction == 1) ? -1f : (moveAction == 2) ? 1f : 0f;

            float previousDistance = Mathf.Abs(distanceToEnemy);
            float newDistance = Mathf.Abs(enemyAgent.transform.position.x - transform.position.x);
            if (newDistance < previousDistance)
            {
                CustomAddReward(0.1f); // Reward for getting closer
            }
            else if (newDistance == previousDistance)
            {
                CustomAddReward(-0.2f);
            }
            else
            {
                CustomAddReward(-0.1f); // Penalty for moving away
            }
        }

        rb.velocity = new Vector2(Move * speed, rb.velocity.y);
        animator.SetFloat("xVelocity", Mathf.Abs(rb.velocity.x));

        if (Move > 0 && !facingRight)
        {
            Flip();
        }
        else if (Move < 0 && facingRight)
        {
            Flip();
        }

        if (jumpAction == 1 && isGrounded())
        {
            rb.AddForce(new Vector2(rb.velocity.x, jump * 10));
        }

        if (attackAction == 1 && canAttack)
        {
            Attack();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Input.GetKey(KeyCode.A) ? 1 : (Input.GetKey(KeyCode.D) ? 2 : 0);
        discreteActions[1] = Input.GetKey(KeyCode.Space) ? 1 : 0;
        discreteActions[2] = Input.GetKey(KeyCode.W) ? 1 : 0;
        Debug.Log($"Heuristic Decisions: Move = {discreteActions[0]}, Jump = {discreteActions[1]}, Attack = {discreteActions[2]}");
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
        if (enemy)
        {
            Debug.Log("Enemy Hit");
            enemy.GetComponent<PlayerAgent>().TakeDamage();
            CustomAddReward(1.0f); // Reward for hitting the enemy
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

    private void CustomAddReward(float reward)
    {
        AddReward(reward);
        Debug.Log($"Reward Added: {reward}");
    }
}
