using System.Collections;
using UnityEngine;

namespace enemy
{
    public class Boss_Skeleton : enemy
    {
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private Transform shootPoint;
        [SerializeField] private float tripleShotDelay = 0.3f;
        [SerializeField] private Vector2 patrolAreaMin;
        [SerializeField] private Vector2 patrolAreaMax;
        [SerializeField] private float patrolWaitTime = 2f;
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private LayerMask targetLayer;
        [SerializeField] private float rayDistance = 1.5f;
        [SerializeField] private float detectionRadius = 10f;
        [SerializeField] private float rotationSpeed = 5f;

        private Transform target;
        private Vector2 nextPatrolPoint;
        private bool isChasing = false;
        private bool isAttacking = false;

        void Start()
        {
            SetNextPatrolPoint();
        }

        void Update()
        {
            if (target != null)
            {
                float distanceToTarget = Vector2.Distance(transform.position, target.position);

                if (distanceToTarget <= attackRange)
                {
                    Attack();
                }
                else if (distanceToTarget <= minimumDistance)
                {
                    isChasing = true;
                    MoveTowards(target.position);
                }
                else
                {
                    isChasing = false;
                    Patrol();
                }

                RotateTowardsTarget(); // Realistyczny obrót w stronę gracza
            }
            else
            {
                Patrol();
                DetectPlayer(); // Sprawdź, czy gracz jest w pobliżu
            }
        }

        private void Patrol()
        {
            if (isChasing || isAttacking) return;

            float distanceToPatrolPoint = Vector2.Distance(transform.position, nextPatrolPoint);

            if (distanceToPatrolPoint > 0.1f)
            {
                MoveTowards(nextPatrolPoint);
            }
            else
            {
                StartCoroutine(SwitchPatrolPoint());
            }
        }

        private void SetNextPatrolPoint()
        {
            float randomX = Random.Range(patrolAreaMin.x, patrolAreaMax.x);
            float randomY = Random.Range(patrolAreaMin.y, patrolAreaMax.y);
            nextPatrolPoint = new Vector2(randomX, randomY);
        }

        private IEnumerator SwitchPatrolPoint()
        {
            yield return new WaitForSeconds(patrolWaitTime);
            SetNextPatrolPoint();
        }

        private void MoveTowards(Vector2 destination)
        {
            Vector2 direction = (destination - (Vector2)transform.position).normalized;

            if (IsPathBlocked(direction))
            {
                direction = FindAlternativeDirection(direction);
            }

            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }

        private bool IsPathBlocked(Vector2 direction)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, rayDistance, obstacleLayer);
            return hit.collider != null;
        }

        private Vector2 FindAlternativeDirection(Vector2 originalDirection)
        {
            Vector2 leftDirection = Quaternion.Euler(0, 0, 45) * originalDirection;
            Vector2 rightDirection = Quaternion.Euler(0, 0, -45) * originalDirection;

            if (!IsPathBlocked(leftDirection))
            {
                return leftDirection.normalized;
            }
            if (!IsPathBlocked(rightDirection))
            {
                return rightDirection.normalized;
            }

            return Vector2.zero;
        }

        private void RotateTowardsTarget()
        {
            if (target == null) return;

            Vector2 direction = (target.position - transform.position).normalized;
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float angle = Mathf.LerpAngle(transform.eulerAngles.z, targetAngle, Time.deltaTime * rotationSpeed);
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void DetectPlayer()
        {
            Collider2D detected = Physics2D.OverlapCircle(transform.position, detectionRadius, targetLayer);
            if (detected != null)
            {
                target = detected.transform;
            }
        }

        public override void Attack()
        {
            if (!isAttacking)
            {
                StartCoroutine(PerformAttack());
            }
        }

        private IEnumerator PerformAttack()
        {
            isAttacking = true;

            int attackType = Random.Range(0, 2);

            if (attackType == 0)
            {
                yield return SingleShot();
            }
            else
            {
                yield return TripleShot();
            }

            yield return new WaitForSeconds(attackSpeed);
            isAttacking = false;
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
            if (arrowPrefab != null && shootPoint != null)
            {
                GameObject arrow = Instantiate(arrowPrefab, shootPoint.position, shootPoint.rotation);
                Vector2 direction = (target.position - shootPoint.position).normalized;
                arrow.GetComponent<Rigidbody2D>().velocity = direction * speed;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, nextPatrolPoint);
        }
    }
}
