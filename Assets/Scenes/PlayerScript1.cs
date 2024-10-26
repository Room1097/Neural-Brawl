using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

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
    private bool isEpisodeEnding = false; // Flag to prevent looping

    public KeyCode moveLeftKey;
    public KeyCode moveRightKey;
    public KeyCode jumpKey;
    public KeyCode attackKey;
    public KeyCode shieldKey; // Key to activate shield

    public float attackCooldown = 0.5f;
    public HealthBar bar;

    public Vector2 Boxsize;
    public GameObject attackPoint;
    public float castDistance;
    public float radius;

    public LayerMask BGLayer;
    public LayerMask Player;

    public bool InitialRight;
    public PlayerAgent enemyAgent;

    private BehaviorParameters behaviorParameters;

    // Shield variables
    public Sprite shieldSprite;
    public Vector3 shieldScale = new Vector3(1f, 1f, 1f);
    public Vector3 shieldPositionOffset = new Vector3(1f, 0, 0);
    private GameObject activeShield;
    public float shieldDuration = 2f;
    public float shieldCooldown = 5f;
    private bool shieldActive = false;
    private bool canUseShield = true;

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

        Debug.Log("PlayerAgent initialized with max health: " + maxHealth);
    }

    public override void OnEpisodeBegin()
    {
        currHealth = maxHealth;
        bar.SetHealth(currHealth);
        canUseShield = true;
        shieldActive = false;
        isEpisodeEnding = false; // Reset the episode ending flag
        if (activeShield != null)
        {
            Destroy(activeShield);
        }

        // if (enemyAgent != null)
        // {
        //     enemyAgent.ResetEnemy();
        // }

        Debug.Log("Episode started. Health reset to " + currHealth);
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

    public void ResetEnemy()
    {
        currHealth = maxHealth;
        bar.SetHealth(currHealth);
        // rb.velocity = Vector2.zero;
        Debug.Log("Enemy reset. Health set to " + currHealth);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(rb.velocity);
        sensor.AddObservation(currHealth);
        sensor.AddObservation(transform.position.x);
        sensor.AddObservation(transform.position.y);
        sensor.AddObservation(isGrounded() ? 1f : 0f);
        sensor.AddObservation(facingRight ? 1f : 0f);
        sensor.AddObservation(shieldActive ? 1f : 0f);

        if (enemyAgent != null)
        {
            sensor.AddObservation(enemyAgent.transform.position.x);
            sensor.AddObservation(enemyAgent.transform.position.y);
            sensor.AddObservation(enemyAgent.currHealth);
            sensor.AddObservation(enemyAgent.shieldActive ? 1f : 0f); // Observation for enemy shield status
        }

        Debug.Log("Observations collected.");
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int moveAction = actions.DiscreteActions[0];
        int jumpAction = actions.DiscreteActions[1];
        int attackAction = actions.DiscreteActions[2];
        int shieldAction = actions.DiscreteActions[3]; // New action for shield

        // Track the agent's position before movement
        float previousDistanceToEnemy = Vector2.Distance(transform.position, enemyAgent.transform.position);

        Move = (moveAction == 1) ? -1f : (moveAction == 2) ? 1f : 0f;
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
            Debug.Log("Jump action performed.");
        }

        if (attackAction == 1 && canAttack)
        {
            Attack();
            Debug.Log("Attack action performed.");
        }

        if (shieldAction == 1 && canUseShield)
        {
            ActivateShield();
            Debug.Log("Shield action performed.");
        }

        // Calculate the new distance to the enemy after movement
        float currentDistanceToEnemy = Vector2.Distance(transform.position, enemyAgent.transform.position);

        // Reward for moving closer to the enemy
        if (currentDistanceToEnemy < previousDistanceToEnemy)
        {
            CustomAddReward(0.1f); // Small reward for getting closer
        }
        else
        {
            CustomAddReward(-0.1f); // Small penalty for moving away
        }

        // Additional penalty if the agent is too far from the enemy
        if (currentDistanceToEnemy > 5.0f)
        {
            CustomAddReward(-0.2f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Input.GetKey(moveLeftKey) ? 1 : (Input.GetKey(moveRightKey) ? 2 : 0);
        discreteActions[1] = Input.GetKey(jumpKey) ? 1 : 0;
        discreteActions[2] = Input.GetKey(attackKey) ? 1 : 0;
        discreteActions[3] = Input.GetKey(shieldKey) ? 1 : 0; // Shield action

        Debug.Log("Heuristic input: Move: " + discreteActions[0] + ", Jump: " + discreteActions[1] + ", Attack: " + discreteActions[2] + ", Shield: " + discreteActions[3]);
    }

    public bool isGrounded()
    {
        bool grounded = Physics2D.BoxCast(transform.position, Boxsize, 0, -transform.up, castDistance, BGLayer);
        Debug.Log("Is grounded: " + grounded);
        return grounded;
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
        activeShield.transform.localPosition = shieldPositionOffset; // Set as local position offset
        activeShield.transform.localScale = shieldScale;

        Debug.Log("Shield activated.");

        StartCoroutine(ShieldCoroutine());
    }

    private IEnumerator ShieldCoroutine()
    {
        yield return new WaitForSeconds(shieldDuration);
        Destroy(activeShield);
        shieldActive = false;

        Debug.Log("Shield deactivated.");

        yield return new WaitForSeconds(shieldCooldown);
        canUseShield = true;
        Debug.Log("Shield ready again.");
    }

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
            PlayerAgent enemyAgent = enemy.GetComponent<PlayerAgent>();

            if (enemyAgent.shieldActive)
            {
                Debug.Log("Enemy's shield absorbed the attack");
                CustomAddReward(-0.5f); // Penalty for hitting an active shield
            }
            else
            {
                Debug.Log("Enemy Hit");
                enemyAgent.TakeDamage(); // Deal damage to the enemy
                CustomAddReward(1.5f); // Increased reward for hitting the enemy
            }
        }
    }

    if (enemies.Length == 0)
    {
        Debug.Log("Attack missed.");
        CustomAddReward(-0.2f); // Penalty for missing the attack
    }

    canAttack = false;
    animator.SetBool("isAttacking", true);
    Invoke("StopAttack", 0.2f);
    Invoke("ResetAttack", attackCooldown);
}

private void StopAttack()
{
    animator.SetBool("isAttacking", false);
    Debug.Log("Attack animation stopped.");
}

private void ResetAttack()
{
    canAttack = true;
    Debug.Log("Attack cooldown ended. Ready to attack again.");
}
    public void TakeDamage()
    {
        if (!shieldActive && !isEpisodeEnding) // Check if the episode is ending to prevent multiple calls
        {
            currHealth -= 20;
            bar.SetHealth(currHealth);

            Debug.Log("Took damage. Current health: " + currHealth);

            if (currHealth <= 0)
            {
                Debug.Log("Player defeated.");
                isEpisodeEnding = true; // Set flag to prevent looping
                // EndEpisode();
            }
        }
        else if (shieldActive)
        {
            Debug.Log("Shield blocked the damage.");
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        transform.Rotate(0f, 180f, 0f);
        Debug.Log("Player flipped. Facing right: " + facingRight);
    }

    private void CustomAddReward(float reward)
    {
        AddReward(reward);
        Debug.Log("Added reward: " + reward);
    }
}
