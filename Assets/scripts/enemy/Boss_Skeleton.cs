using System.Collections;
using UnityEngine;

namespace enemy
{
    public class Boss_Skeleton : enemy
    {
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private Transform shootPoint;
        [SerializeField] private float tripleShotDelay = 0.3f;
        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private float detectionRadius = 10f;
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private Animator animator;
        [SerializeField] private float shootPointRadius = 2f;
        public float attackCooldown = 1.5f;

        private Transform target;
        private bool isAttacking = false;
        private bool canAttack = true;

        void Update()
        {
            if (target != null)
            {
                float distanceToTarget = Vector2.Distance(transform.position, target.position);

                UpdateMovementAnimation();
                
                if (distanceToTarget <= attackRange && canAttack)
                {
                    Attack();
                }
                else if (distanceToTarget > attackRange)
                {
                    ChasePlayer();
                }

                RotateShootPoint();
                UpdateShootPointPosition();
            }
            else
            {
                DetectPlayer();
            }

            if (health <= 0)
            {
                animator.SetTrigger("Death");
                Die();
            }
        }

        private void UpdateShootPointPosition()
        {
            if(target == null) return;
            
            Vector2 directionToTarget = (target.position - transform.position).normalized;
            shootPoint.position = (Vector2)transform.position + directionToTarget * shootPointRadius;
        }

        private void RotateShootPoint()
        {
            if(target == null) return;
            
            Vector2 direction = (target.position - shootPoint.position).normalized;
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            shootPoint.rotation = Quaternion.Lerp(
                shootPoint.rotation,
                Quaternion.Euler(0, 0, targetAngle),
                Time.deltaTime * rotationSpeed
            );
        }

        private void UpdateMovementAnimation()
        {
            if (target != null && !isAttacking)
            {
                Vector2 direction = (target.position - transform.position).normalized;
                animator.SetFloat("Xinput", direction.x);
                animator.SetFloat("Yinput", direction.y);
                animator.SetFloat("LastXinput", direction.x);
                animator.SetFloat("LastYinput", direction.y);
            }
        }

        private void ChasePlayer()
        {
            Vector2 direction = (target.position - transform.position).normalized;
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }

        private void DetectPlayer()
        {
            Collider2D detected = Physics2D.OverlapCircle(transform.position, detectionRadius, targetLayer);
            if (detected != null) target = detected.transform;
        }

        public override void Attack()
        {
            if (!isAttacking && canAttack)
            {
                StartCoroutine(PerformAttack());
            }
        }

        private IEnumerator PerformAttack()
        {
            isAttacking = true;
            canAttack = false;

            Vector2 attackDir = (target.position - transform.position).normalized;
            animator.SetFloat("AttackXinput", attackDir.x);
            animator.SetFloat("AttackYinput", attackDir.y);
            animator.SetBool("IsAttacking", true);

            int attackType = Random.Range(0, 2);
            yield return attackType == 0 ? SingleShot() : TripleShot();

            animator.SetBool("IsAttacking", false);
            yield return new WaitForSeconds(attackCooldown);
            
            isAttacking = false;
            canAttack = true;
        }

        private IEnumerator SingleShot()
        {
            ShootArrow();
            yield return null;
        }

        private IEnumerator TripleShot()
        {
            for (int i = 0; i < 3; i++)
            {
                ShootArrow();
                yield return new WaitForSeconds(tripleShotDelay);
            }
        }

        private void ShootArrow()
        {
            if (arrowPrefab && shootPoint)
            {
                GameObject arrow = Instantiate(
                    arrowPrefab, 
                    shootPoint.position, 
                    shootPoint.rotation
                );
                
                arrow.GetComponent<Rigidbody2D>().velocity = shootPoint.right * speed;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, shootPointRadius);
        }
    }
}