using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace enemy
{
    public enum BossState { Chase, Attack, Retreat, Patrol }

    public class Boss_Skeleton : enemy
    {
        [Header("Referencje")]
        [SerializeField] private Player player;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private Transform shootPoint;
        [SerializeField] private TilemapCollider2D tilemapCollider;
        [SerializeField] private Tilemap tilemap;

        [Header("Ustawienia ataku")]
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float tripleShotDelay = 0.3f;
        [SerializeField] private float shootPointRadius = 2f;
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private float retreatDistance = 2f;

        [Header("Ruch i wykrywanie")]
        [SerializeField] private float chaseSpeed = 2f;
        [SerializeField] private float sightRange = 10f;

        [Header("Patrol")]
        [SerializeField] private float patrolSpeed = 1f;
        [SerializeField] private float patrolRadius = 5f;
        [SerializeField] private float changePatrolTargetInterval = 3f;

        private BossState currentState;
        private bool isAttacking = false;
        private bool canAttack = true;

        private Vector3 patrolTarget;
        private float patrolTimer = 0f;

        void Awake()
        {
            if (player == null)
                player = FindObjectOfType<Player>();

            if (tilemapCollider == null)
                tilemapCollider = FindObjectOfType<TilemapCollider2D>();

            if (tilemap == null && tilemapCollider != null)
                tilemap = tilemapCollider.GetComponent<Tilemap>();


            currentState = BossState.Patrol;
            patrolTarget = transform.position;
        }

        void Update()
        {
            if (health <= 0)
            {
                animator.SetTrigger("Death");
                Die();
                return;
            }

            float distance = Vector2.Distance(transform.position, player.transform.position);

            if (distance < retreatDistance)
            {
                currentState = BossState.Retreat;
            }
            else if (distance <= attackRange && PlayerInSight())
            {
                currentState = BossState.Attack;
            }
            else if (distance <= sightRange && PlayerInSight())
            {
                currentState = BossState.Chase;
            }
            else
            {
                currentState = BossState.Patrol;
            }

            switch (currentState)
            {
                case BossState.Chase:
                    ChasePlayer();
                    break;
                case BossState.Attack:
                    if (canAttack && !isAttacking)
                        Attack();
                    break;
                case BossState.Retreat:
                    RetreatFromPlayer();
                    break;
                case BossState.Patrol:
                    Patrol();
                    break;
            }

            RotateShootPoint();
            UpdateShootPointPosition();
            UpdateMovementAnimation();
        }

        private bool PlayerInSight()
        {
            Vector2 startPos = transform.position;
            Vector2 targetPos = player.transform.position;
            RaycastHit2D hit = Physics2D.Linecast(startPos, targetPos);
            
            if (hit.collider == null)
                return true;
            if (hit.collider.gameObject == player.gameObject)
                return true;
            if (tilemapCollider != null && hit.collider.gameObject == tilemapCollider.gameObject)
                return false;
            
            return true;
        }

        private void ChasePlayer()
        {
            Vector2 direction = (player.transform.position - transform.position).normalized;
            transform.position += (Vector3)(direction * chaseSpeed * Time.deltaTime);
        }

        private void RetreatFromPlayer()
        {
            Vector2 direction = (transform.position - player.transform.position).normalized;
            transform.position += (Vector3)(direction * chaseSpeed * Time.deltaTime);
        }

        private void Patrol()
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= changePatrolTargetInterval)
            {
                patrolTarget = transform.position + (Vector3)Random.insideUnitCircle * patrolRadius;
                patrolTimer = 0f;
            }
            Vector2 direction = (patrolTarget - transform.position).normalized;
            transform.position += (Vector3)(direction * patrolSpeed * Time.deltaTime);
        }

        public override void Attack()
        {
            if (isAttacking || !canAttack)
                return;

            StartCoroutine(PerformAttack());
        }

        private IEnumerator PerformAttack()
        {
            isAttacking = true;

            Vector2 attackDir = (player.transform.position - transform.position).normalized;
            animator.SetFloat("AttackXinput", attackDir.x);
            animator.SetFloat("AttackYinput", attackDir.y);
            animator.SetBool("IsAttacking", true);

            int attackType = Random.Range(0, 2);
            if (attackType == 0)
            {
                yield return new WaitForSeconds(0.05f);
                ShootArrow();
                yield return null;
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    yield return new WaitForSeconds(0.05f);
                    ShootArrow();
                    yield return new WaitForSeconds(tripleShotDelay);
                }
            }

            animator.SetBool("IsAttacking", false);
            yield return new WaitForSeconds(attackCooldown);

            isAttacking = false;
            canAttack = true;
        }

        private void ShootArrow()
        {
            if (arrowPrefab && shootPoint)
            {
                GameObject arrow = Instantiate(arrowPrefab, shootPoint.position, shootPoint.rotation);
                Rigidbody2D rbArrow = arrow.GetComponent<Rigidbody2D>();
                if (rbArrow != null)
                {
                    rbArrow.velocity = shootPoint.right * speed;
                }
            }
        }

        private void RotateShootPoint()
        {
            if (player == null || shootPoint == null)
                return;

            Vector2 direction = (player.transform.position - shootPoint.position).normalized;
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            shootPoint.rotation = Quaternion.Lerp(shootPoint.rotation, Quaternion.Euler(0, 0, targetAngle), Time.deltaTime * rotationSpeed);
        }

        private void UpdateShootPointPosition()
        {
            if (player == null || shootPoint == null)
                return;

            Vector2 direction = (player.transform.position - transform.position).normalized;
            shootPoint.position = (Vector2)transform.position + direction * shootPointRadius;
        }

        private void UpdateMovementAnimation()
        {
            Vector2 movement = Vector2.zero;
            if (currentState == BossState.Chase)
                movement = (player.transform.position - transform.position).normalized;
            else if (currentState == BossState.Retreat)
                movement = (transform.position - player.transform.position).normalized;
            else if (currentState == BossState.Patrol)
            {
                movement = (patrolTarget - transform.position).normalized;
            }

            if (movement != Vector2.zero)
            {
                animator.SetFloat("Xinput", movement.x);
                animator.SetFloat("Yinput", movement.y);
                animator.SetFloat("LastXinput", movement.x);
                animator.SetFloat("LastYinput", movement.y);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, retreatDistance);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, sightRange);

            Gizmos.color = Color.blue;
            Vector3 shootDirection = (player != null) ? (player.transform.position - transform.position).normalized : Vector3.right;
            Gizmos.DrawWireSphere(transform.position + shootPointRadius * shootDirection, 0.1f);
        }
    }
}
