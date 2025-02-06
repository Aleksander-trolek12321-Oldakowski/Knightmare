using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace enemy
{
public class Zombie : enemy
{
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Player player;
    private bool isAttacking = false;
    private bool canAttack = true;
    public float attackCooldown = 1.5f;
    public float turnSpeed = 10f;
    public float attackInaccuracy = 0.05f;

    private Vector2 currentAttackDirection;


    void Awake()
    {
        player = FindFirstObjectByType<Player>();
    }


        private Vector2 currentAttackDirection;

        void Awake()
        {
            player = FindFirstObjectByType<Player>();
        }

        Vector2 direction = (player.transform.position - transform.position).normalized;

        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

        if (distanceToPlayer > attackRange)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, minimumDistance, ~enemyLayer);

            Debug.DrawRay(transform.position, direction * attackRange, Color.red);

            if (hit.collider != null && hit.collider.GetComponent<Player>() == player)
            {
                base.Movement();

                Vector2 direction = (player.transform.position - transform.position).normalized;
                float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

                if (distanceToPlayer > attackRange)
                {
                    RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, minimumDistance, ~enemyLayer);
                    Debug.DrawRay(transform.position, direction * attackRange, Color.red);

                    if (hit.collider != null && hit.collider.GetComponent<Player>() == player)
                    {
                        animator.SetFloat("Xinput", direction.x);
                        animator.SetFloat("Yinput", direction.y);
                        animator.SetFloat("LastXinput", direction.x);
                        animator.SetFloat("LastYinput", direction.y);

                        transform.position = Vector2.MoveTowards(transform.position, player.transform.position, speed * Time.deltaTime);
                    }
                    else
                    {
                        Debug.Log("Zombie nie widzi gracza.");
                    }
                }
                else
                {
                    animator.SetFloat("Xinput", 0);
                    animator.SetFloat("Yinput", 0);
                    Debug.Log("Zombie jest w zasięgu ataku, nie porusza się.");
                }
            }
        }
        else
        {
            base.Attack();
            if (isAttacking || !canAttack)
            {
                return;
            }

            isAttacking = true;
            canAttack = false;

            Vector2 attackDir = (player.transform.position - transform.position).normalized;

            animator.SetFloat("AttackXinput", attackDir.x);
            animator.SetFloat("AttackYinput", attackDir.y);
            animator.SetBool("IsAttacking", true);

            Debug.Log("Zombie rozpoczyna atak");
            currentAttackDirection = attackDir;
            StartCoroutine(PerformAttack());
        }

        IEnumerator PerformAttack()
        {
            yield return new WaitForSeconds(attackSpeed);

            RaycastHit2D finalHit = Physics2D.Raycast(transform.position, currentAttackDirection, attackRange, ~enemyLayer);

            if (finalHit.collider != null && finalHit.collider.GetComponent<Player>() == player)
            {
                player.TakeDamage(damage);
                Debug.Log("Zombie zadał obrażenia graczowi!");
            }

            Debug.Log("Zombie zakończył atak.");
            animator.SetBool("IsAttacking", false);

            yield return new WaitForSeconds(attackCooldown);
            isAttacking = false;
            canAttack = true;
        }

        private void Update()
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

            if (distanceToPlayer <= attackRange && canAttack && !isAttacking)
            {
                Attack();
            }

            Movement();

            if (health <= 0)
            {
                animator.SetTrigger("Death_Zombie");
            }
        }
    }

    public override void Attack()
    {
        base.Attack();
        if(isAttacking || !canAttack)
        {
            return;
        }

        isAttacking = true;
        canAttack = false;

        Debug.Log("Zombie rozpoczyna atak");

        currentAttackDirection = (player.transform.position - transform.position).normalized;
        StartCoroutine(PerformAttack());
    }

    IEnumerator PerformAttack()
    {
        float attackTime = attackSpeed;
        
        while (attackTime > 0)
        {
            attackTime -= Time.deltaTime;

            Vector2 targetDirection = (player.transform.position - transform.position).normalized;
            targetDirection += new Vector2(
                Random.Range(-attackInaccuracy, attackInaccuracy),
                Random.Range(-attackInaccuracy, attackInaccuracy)
            ).normalized * 0.1f;

            currentAttackDirection = Vector2.Lerp(currentAttackDirection, targetDirection, Time.deltaTime * turnSpeed).normalized;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, currentAttackDirection, attackRange, ~enemyLayer);
            Debug.DrawRay(transform.position, currentAttackDirection * attackRange, Color.green);

            yield return null;
        }

        RaycastHit2D finalHit = Physics2D.Raycast(transform.position, currentAttackDirection, attackRange, ~enemyLayer);

        if (finalHit.collider != null && finalHit.collider.GetComponent<Player>() == player)
        {
            player.health -= damage;
            Debug.Log("Zombie zadał obrażenia graczowi po zakończeniu ataku!");
        }

        Debug.Log("Zombie zakończył atak.");
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
        canAttack = true;
    }


    private void Update()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

        if(distanceToPlayer <= attackRange && canAttack && !isAttacking)
        {
            Attack();
        }
        
        Movement();

    }
}
}