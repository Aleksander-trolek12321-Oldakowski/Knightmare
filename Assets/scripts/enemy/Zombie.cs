using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
        private bool isWalking = false;  // Flaga kontrolująca animację chodzenia

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
                return; // Jeśli atakujemy, nie poruszamy się
            }

            Vector2 direction = (player.transform.position - transform.position).normalized;
            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

            if (distanceToPlayer > attackRange)
            {
                RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, attackRange, playerLayer);
                Debug.DrawRay(transform.position, direction * attackRange, Color.red);

                if (hit.collider != null && hit.collider.GetComponent<Player>() == player)
                {
                    animator.SetFloat("Xinput", direction.x);
                    animator.SetFloat("Yinput", direction.y);
                    animator.SetFloat("LastXinput", direction.x);
                    animator.SetFloat("LastYinput", direction.y);

                    transform.position = Vector2.MoveTowards(transform.position, player.transform.position, speed * Time.deltaTime);
                    isWalking = true;  // Ustawiamy flagę na true, gdy zombie się porusza
                }
                else
                {
                    isWalking = false; // Jeśli nie widzimy gracza, zatrzymujemy animację chodzenia
                }
            }
            else
            {
                isWalking = false; // Jeśli jesteśmy w zasięgu ataku, nie chodzimy
            }

            animator.SetBool("IsWalking", isWalking);  // Przekazujemy flagę do animacji
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
