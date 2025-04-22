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

        [Header("Animacja i hit‑react")]
        [SerializeField] private float hitReactCooldown = 0.23f;

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
        private Coroutine attackCoroutine;
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
                currentState = BossState.Retreat;
            else if (distance <= attackRange && PlayerInSight())
                currentState = BossState.Attack;
            else if (distance <= sightRange && PlayerInSight())
                currentState = BossState.Chase;
            else
                currentState = BossState.Patrol;

            switch (currentState)
            {
                case BossState.Chase: ChasePlayer(); break;
                case BossState.Attack: if (canAttack && !isAttacking) Attack(); break;
                case BossState.Retreat: RetreatFromPlayer(); break;
                case BossState.Patrol: Patrol(); break;
            }

            RotateShootPoint();
            UpdateShootPointPosition();
            UpdateMovementAnimation();
        }

        public override void TakeDamage(float damageAmount)
        {
            base.TakeDamage(damageAmount);
            animator.SetTrigger("Hit");
            InterruptAttack();
        }

        private void InterruptAttack()
        {
            if (isAttacking)
            {
                if (attackCoroutine != null)
                    StopCoroutine(attackCoroutine);
                isAttacking = false;
                animator.SetBool("IsAttacking", false);
                canAttack = false;
                StartCoroutine(ResetAttackCooldown());
            }
        }

        private IEnumerator ResetAttackCooldown()
        {
            yield return new WaitForSeconds(hitReactCooldown);
            canAttack = true;
        }

        public override void Attack()
        {
            if (isAttacking || !canAttack)
                return;

            attackCoroutine = StartCoroutine(PerformAttack());
        }

        private IEnumerator PerformAttack()
        {
            isAttacking = true;
            canAttack = false;
            animator.SetBool("IsAttacking", true);

            int attackType = Random.Range(0, 2);
            if (attackType == 0)
            {
                yield return new WaitForSeconds(0.05f);
                ShootArrow();
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

            yield return new WaitForSeconds(attackCooldown);
            animator.SetBool("IsAttacking", false);
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
                    rbArrow.velocity = shootPoint.right * speed;
            }
        }

        private bool PlayerInSight()
        {
            RaycastHit2D hit = Physics2D.Linecast(transform.position, player.transform.position);
            if (hit.collider == null || hit.collider.gameObject == player.gameObject)
                return true;
            if (tilemapCollider != null && hit.collider.gameObject == tilemapCollider.gameObject)
                return false;
            return true;
        }

        private void ChasePlayer()
        {
            Vector2 dir = (player.transform.position - transform.position).normalized;
            transform.position += (Vector3)(dir * chaseSpeed * Time.deltaTime);
        }

        private void RetreatFromPlayer()
        {
            Vector2 dir = (transform.position - player.transform.position).normalized;
            transform.position += (Vector3)(dir * chaseSpeed * Time.deltaTime);
        }

        private void Patrol()
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= changePatrolTargetInterval)
            {
                Vector2 rand2D = Random.insideUnitCircle * patrolRadius;
                patrolTarget = transform.position + new Vector3(rand2D.x, rand2D.y, 0);
                patrolTimer = 0f;
            }
            Vector3 diff = patrolTarget - transform.position;
            Vector3 dir = diff.normalized;
            transform.position += dir * patrolSpeed * Time.deltaTime;
        }

        private void RotateShootPoint()
        {
            if (!player || !shootPoint) return;
            Vector2 dir = (player.transform.position - shootPoint.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            shootPoint.rotation = Quaternion.Lerp(shootPoint.rotation, Quaternion.Euler(0,0,angle), Time.deltaTime * rotationSpeed);
        }

        private void UpdateShootPointPosition()
        {
            if (!player || !shootPoint) return;
            Vector2 dir = (player.transform.position - transform.position).normalized;
            shootPoint.position = (Vector2)transform.position + dir * shootPointRadius;
        }

        private void UpdateMovementAnimation()
        {
            Vector2 movement = Vector2.zero;
            switch (currentState)
            {
                case BossState.Chase:
                    movement = (player.transform.position - transform.position).normalized;
                    break;
                case BossState.Retreat:
                    movement = (transform.position - player.transform.position).normalized;
                    break;
                case BossState.Patrol:
                    movement = (patrolTarget - transform.position).normalized;
                    break;
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
            Vector3 dir = player ? (player.transform.position - transform.position).normalized : Vector3.right;
            Gizmos.DrawWireSphere(transform.position + dir * shootPointRadius, 0.1f);
        }
    }
}
