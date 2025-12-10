using System.Collections;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using TMPro;

public class PlayerAgent : Agent
{
    // --- RESEARCH / ABLATION SETTINGS ---
    [Header("Ablation Studies")]
    public bool useDistanceReward = true; 
    public bool useShieldReward = true;    
    public bool useCombatReward = true;    
    // ------------------------------------

    [Header("References")]
    private Rigidbody2D rb;
    public Animator animator;
    public HealthBar bar;
    
    // --- FIX: Replaced 'activeShield' with a permanent reference to improve performance
    [Tooltip("Drag the child GameObject containing the Shield Sprite here.")]
    public GameObject shieldVisuals; 

    [Header("Stats")]
    public int maxHealth = 100;
    public int currHealth;
    public float speed = 5f;
    public float jump = 5f;
    public float attackCooldown = 0.5f;
    public float shieldDuration = 2f;
    public float shieldCooldown = 5f;

    [Header("Combat Setup")]
    public Vector2 Boxsize;
    public GameObject attackPoint;
    public float castDistance;
    public float radius;
    public LayerMask BGLayer;
    public LayerMask Player;
    public bool InitialRight;
    public PlayerAgent enemyAgent;

    [Header("Inputs (Heuristic)")]
    public KeyCode moveLeftKey;
    public KeyCode moveRightKey;
    public KeyCode jumpKey;
    public KeyCode attackKey;
    public KeyCode shieldKey; 

    // Internal State
    private float Move;
    private bool facingRight = true;
    private bool canAttack = true;
    private bool shieldActive = false;
    private bool canUseShield = true;
    
    // --- FIX: Variable to store previous distance for correct reward calculation
    private float lastDistanceToEnemy;

    // UI Counters
    private int totalEpisodes = 0;
    private int totalDeaths = 0;
    public TextMeshProUGUI episodeCounterText;
    public TextMeshProUGUI deathCounterText;

    //logger
    public StatsLogger statsLogger;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (bar != null) bar.SetMaxHealth(maxHealth);
        else Debug.LogError("HealthBar reference not assigned.");

        currHealth = maxHealth;

        if (!InitialRight) Flip();

        // Ensure shield is off at start
        if (shieldVisuals != null) shieldVisuals.SetActive(false);
    }

    public override void OnEpisodeBegin()
    {
        currHealth = maxHealth;
        if (bar != null) bar.SetHealth(currHealth);

        // Reset States
        canUseShield = true;
        shieldActive = false;
        canAttack = true;
        
        // --- FIX: Use SetActive instead of Destroy() to stop lag spikes
        if (shieldVisuals != null) shieldVisuals.SetActive(false);

        // --- FIX: Reset physics velocity so agent doesn't "slide" into new episode
        rb.linearVelocity = Vector2.zero;

        // --- FIX: Initialize distance variable to prevent weird rewards on frame 1
        if (enemyAgent != null)
        {
            lastDistanceToEnemy = Vector2.Distance(transform.position, enemyAgent.transform.position);
        }

        StopAllCoroutines(); 
        StartCoroutine(EpisodeTimerCoroutine(60));

        totalEpisodes++;
        UpdateUI();
    }

    private IEnumerator EpisodeTimerCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        EndEpisodeAfterTimeout();
    }

    

    private void EndEpisodeAfterTimeout()
    {
        if (enemyAgent != null)
        {
            if (currHealth < enemyAgent.currHealth)
            {
                // Enemy Wins
                if(statsLogger != null) 
                    statsLogger.LogMatch(totalEpisodes, enemyAgent.name, 60f, enemyAgent.currHealth);
                
                totalDeaths++;
                CustomAddReward(-0.5f);
            }
            else if (currHealth > enemyAgent.currHealth)
            {
                // We Win
                if(statsLogger != null) 
                    statsLogger.LogMatch(totalEpisodes, this.name, 60f, currHealth);
                    
                enemyAgent.totalDeaths++;
                CustomAddReward(0.5f);
            }
            else 
            {
                // Draw
                if(statsLogger != null) 
                    statsLogger.LogMatch(totalEpisodes, "Draw", 60f, currHealth);
            }
        }

        UpdateUI();
        enemyAgent.EndEpisode();
        EndEpisode();
    }

    public void ResetEnemy()
    {
        currHealth = maxHealth;
        bar.SetHealth(currHealth);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // --- FIX: Normalization! Neural networks hate large numbers like "100".
        // Divide by max values to keep inputs between 0 and 1.
        
        sensor.AddObservation(rb.linearVelocity.x / 10f); // Assuming max speed is around 10
        sensor.AddObservation(rb.linearVelocity.y / 50f);
        
        sensor.AddObservation(currHealth / (float)maxHealth); // Normalized Health
        
        // Relative position is better than absolute position
        Vector2 relativePos = (enemyAgent.transform.position - transform.position);
        sensor.AddObservation(relativePos.x / 20f); // Divide by approx arena width
        sensor.AddObservation(relativePos.y / 10f); 

        sensor.AddObservation(isGrounded() ? 1f : 0f);
        sensor.AddObservation(facingRight ? 1f : 0f);
        sensor.AddObservation(shieldActive ? 1f : 0f);

        if (enemyAgent != null)
        {
            sensor.AddObservation(enemyAgent.currHealth / (float)maxHealth); // Normalized Enemy Health
            sensor.AddObservation(enemyAgent.isShieldActive() ? 1f : 0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int moveAction = actions.DiscreteActions[0];
        int jumpAction = actions.DiscreteActions[1];
        int attackAction = actions.DiscreteActions[2];
        int shieldAction = actions.DiscreteActions[3];

        // 1. Movement Logic
        Move = (moveAction == 1) ? -1f : (moveAction == 2) ? 1f : 0f;
        rb.linearVelocity = new Vector2(Move * speed, rb.linearVelocity.y);
        animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));

        // Orientation
        if (Move > 0 && !facingRight) Flip();
        else if (Move < 0 && facingRight) Flip();

        // 2. Jump Logic
        if (jumpAction == 1 && isGrounded())
        {
            rb.AddForce(new Vector2(rb.linearVelocity.x, jump * 10)); // Be careful with force accumulation
        }

        // 3. Attack Logic
        if (attackAction == 1 && canAttack)
        {
            StartCoroutine(AttackCoroutine());
        }

        // 4. Shield Logic
        if (shieldAction == 1 && canUseShield)
        {
            StartCoroutine(ShieldCoroutine());
        }

        // --- FIX: Distance Reward Logic ---
        // We calculate reward based on the change in distance compared to LAST frame.
        if (useDistanceReward && enemyAgent != null)
        {
            float currentDistance = Vector2.Distance(transform.position, enemyAgent.transform.position);
            float distanceChange = lastDistanceToEnemy - currentDistance; // Positive if got closer

            // Only reward approach if enemy is weak (Hunting behavior)
            if (distanceChange > 0 && enemyAgent.currHealth <= 40)
            {
                CustomAddReward(0.001f); // Small persistent reward
            }
            else if (distanceChange < 0)
            {
                CustomAddReward(-0.001f); 
            }

            lastDistanceToEnemy = currentDistance;
        }
    }

    private IEnumerator AttackCoroutine()
    {
        canAttack = false;
        animator.SetBool("isAttacking", true);
        
        PerformAttackHitCheck(); 

        yield return new WaitForSeconds(0.2f); 
        animator.SetBool("isAttacking", false);

        yield return new WaitForSeconds(attackCooldown); 
        canAttack = true;
    }

    private void PerformAttackHitCheck()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPoint.transform.position, radius, Player);
        bool hitSomething = false;

        foreach (Collider2D enemy in enemies)
        {
            if (enemy.gameObject != this.gameObject)
            {
                PlayerAgent target = enemy.GetComponent<PlayerAgent>();
                hitSomething = true;

                if (target.isShieldActive())
                {
                    Debug.Log("Attack Blocked!");
                    CustomAddReward(-0.05f); // Slight penalty for hitting a shield
                }
                else
                {
                    Debug.Log("Enemy Hit");
                    target.TakeDamage();

                    if (useCombatReward)
                    {
                        CustomAddReward(0.5f);
                        if (target.currHealth <= 0) CustomAddReward(1.0f);
                    }
                }
            }
        }

        if (!hitSomething)
        {
             // Penalty for swinging at air (encourages precision)
             CustomAddReward(-0.02f);
        }
    }

    // --- FIX: Converted Shield logic to simple SetActive toggle
    private IEnumerator ShieldCoroutine()
    {
        if (shieldActive || !canUseShield) yield break;

        shieldActive = true;
        canUseShield = false;

        if (shieldVisuals != null) shieldVisuals.SetActive(true);

        yield return new WaitForSeconds(shieldDuration);

        if (shieldVisuals != null) shieldVisuals.SetActive(false);
        shieldActive = false;

        Debug.Log("Shield Deactivated");

        yield return new WaitForSeconds(shieldCooldown);
        canUseShield = true;
        Debug.Log("Shield Ready");
    }

    public void TakeDamage()
    {
        if (!shieldActive)
        {
            currHealth -= 20;
            if (bar != null) bar.SetHealth(currHealth);
            
            CustomAddReward(-0.2f); 

            if (currHealth <= 0)
            {
                if(statsLogger != null) 
                {
                    statsLogger.LogMatch(totalEpisodes, enemyAgent.name, Time.timeSinceLevelLoad, enemyAgent.currHealth);
                }
                totalDeaths++;
                UpdateUI();
                CustomAddReward(-1.0f); 
                enemyAgent.EndEpisode();
                EndEpisode();
            }
        }
        else
        {
            if (useShieldReward) CustomAddReward(0.2f); 
        }
    }

    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Input.GetKey(moveLeftKey) ? 1 : (Input.GetKey(moveRightKey) ? 2 : 0);
        discreteActions[1] = Input.GetKey(jumpKey) ? 1 : 0;
        discreteActions[2] = Input.GetKey(attackKey) ? 1 : 0;
        discreteActions[3] = Input.GetKey(shieldKey) ? 1 : 0;
    }

    public bool isGrounded()
    {
        return Physics2D.BoxCast(transform.position, Boxsize, 0, -transform.up, castDistance, BGLayer);
    }
    
    public bool isShieldActive() => shieldActive;

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }

    private void UpdateUI()
    {
        if (episodeCounterText != null) episodeCounterText.text = "Episodes: " + totalEpisodes;
        if (deathCounterText != null) deathCounterText.text = "Deaths: " + totalDeaths;
    }

    private void CustomAddReward(float reward)
    {
        AddReward(reward);
        
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
}