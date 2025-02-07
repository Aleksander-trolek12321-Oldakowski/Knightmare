using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace enemy
{
    public class Zombie : enemy
    {
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private Player player;
        private bool isAttacking = false;
        private bool canAttack = true;
        public float attackCooldown = 1.5f;
        public float turnSpeed = 10f;
        public float attackInaccuracy = 0.05f;
        public Animator animator;
        private bool isWalking = false;

        private Vector2 currentAttackDirection;

        void Awake()
        {
            player = FindFirstObjectByType<Player>();
        }

        public override void Movement()
        {
            if (isAttacking)
            {
                isWalking = false;
                return;
            }

            Vector2 direction = (player.transform.position - transform.position).normalized;
            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

            if (distanceToPlayer > attackRange)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, attackRange, playerLayer);
                Debug.DrawRay(transform.position, direction * attackRange, Color.red);

                if (hit.collider != null && hit.collider.GetComponent<Player>() == player)
                {
                    // Sprawdzamy, czy nie ma przeszkód na drodze do gracza
                    if (!CheckForObstacles(direction))
                    {
                        animator.SetFloat("Xinput", direction.x);
                        animator.SetFloat("Yinput", direction.y);
                        animator.SetFloat("LastXinput", direction.x);
                        animator.SetFloat("LastYinput", direction.y);

                        transform.position = Vector2.MoveTowards(transform.position, player.transform.position, speed * Time.deltaTime);
                        isWalking = true;
                    }
                    else
                    {
                        // Jeśli są przeszkody, próbujemy znaleźć alternatywną ścieżkę
                        Vector2 alternativeDirection = FindAlternativeDirection(direction);
                        if (alternativeDirection != Vector2.zero)
                        {
                            animator.SetFloat("Xinput", alternativeDirection.x);
                            animator.SetFloat("Yinput", alternativeDirection.y);
                            animator.SetFloat("LastXinput", alternativeDirection.x);
                            animator.SetFloat("LastYinput", alternativeDirection.y);

                            transform.position = Vector2.MoveTowards(transform.position, (Vector2)transform.position + alternativeDirection, speed * Time.deltaTime);
                            isWalking = true;
                        }
                        else
                        {
                            isWalking = false;
                        }
                    }
                }
                else
                {
                    isWalking = false;
                }
            }
            else
            {
                isWalking = false;
            }

            animator.SetBool("IsWalking", isWalking);
        }

        private bool CheckForObstacles(Vector2 direction)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, attackRange, ~playerLayer);
            return hit.collider != null;
        }

        private Vector2 FindAlternativeDirection(Vector2 originalDirection)
        {
            // Sprawdzamy kilka alternatywnych kierunków
            Vector2[] directions = new Vector2[]
            {
                new Vector2(originalDirection.y, -originalDirection.x), // 90 stopni w prawo
                new Vector2(-originalDirection.y, originalDirection.x), // 90 stopni w lewo
                new Vector2(originalDirection.x, originalDirection.y + 0.5f).normalized, // lekko w górę
                new Vector2(originalDirection.x, originalDirection.y - 0.5f).normalized // lekko w dół
            };

            foreach (Vector2 dir in directions)
            {
                if (!CheckForObstacles(dir))
                {
                    return dir;
                }
            }

            return Vector2.zero;
        }

        public override void Attack()
        {
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
}