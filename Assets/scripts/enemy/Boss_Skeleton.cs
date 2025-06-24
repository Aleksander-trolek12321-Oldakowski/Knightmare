using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace enemySpace
{
    public enum BossState { Chase, Attack, Patrol, Retreat }

    [RequireComponent(typeof(Rigidbody2D))]
    public class Boss_Skeleton : enemy
    {
        [Header("Referencje")]
        [SerializeField] private Player player;
        [SerializeField] private Animator animator;
        [SerializeField] private TilemapCollider2D tilemapCollider;
        [SerializeField] private Tilemap tilemap;

        [Header("Ruch i wykrywanie")]
        [SerializeField] private float chaseSpeed = 2f;
        [SerializeField] private float sightRange = 10f;

        [Header("Patrol")]
        [SerializeField] private float patrolSpeed = 1f;
        [SerializeField] private float patrolRadius = 5f;
        [SerializeField] private float changePatrolTargetInterval = 3f;

        [Header("Atak melee")]
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float hitReactCooldown = 0.23f;

        private BossState currentState;
        private bool isAttacking = false;
        private bool canAttack = true;
        private Coroutine meleeCoroutine;

        private Vector3 patrolCenter;
        private Vector3 patrolTarget;
        private float patrolTimer = 0f;

        // Pathfinding
        private List<Vector3> currentPath;
        private int pathIndex = 0;
        private bool isPathUpdating = false;
        public float pathUpdateInterval = 0.75f;

        private Rigidbody2D rb;

        public GameObject PortalToNextLevel;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (player == null) player = FindObjectOfType<Player>();
            if (tilemapCollider == null) tilemapCollider = FindObjectOfType<TilemapCollider2D>();
            if (tilemap == null && tilemapCollider != null) tilemap = tilemapCollider.GetComponent<Tilemap>();

            currentState = BossState.Patrol;
            patrolCenter = transform.position;
            SetNewPatrolTarget();
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
            bool inSight = PlayerInSight();

            // State decision
            if (distance <= attackRange && inSight)
                currentState = BossState.Attack;
            else if (distance <= sightRange && inSight)
                currentState = BossState.Chase;
            else
                currentState = BossState.Patrol;

            // State actions
            switch (currentState)
            {
                case BossState.Chase:
                    UpdatePath(player.transform.position);
                    MoveAlongPath();
                    break;
                case BossState.Patrol:
                    PatrolPath();
                    MoveAlongPath();
                    break;
                case BossState.Attack:
                    // if out of range -> chase
                    if (distance > attackRange)
                    {
                        currentState = BossState.Chase;
                        break;
                    }
                    if (canAttack && !isAttacking)
                        Attack();
                    break;
            }
        }

        public override void Attack()
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (isAttacking || !canAttack || distance > attackRange) return;

            // Trigger melee animation
            Vector3 dir = (player.transform.position - transform.position).normalized;
            animator.SetFloat("AttackXinput", dir.x);
            animator.SetFloat("AttackYinput", dir.y);
            animator.SetBool("IsAttacking", true);

            isAttacking = true;
            canAttack = false;
            meleeCoroutine = StartCoroutine(PerformMeleeAttack());
        }

        private IEnumerator PerformMeleeAttack()
        {
            // Wind-up
            yield return new WaitForSeconds(0.5f);
            // Only apply if still attacking
            if (!isAttacking) yield break;

            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance <= attackRange && PlayerInSight())
                player.TakeDamage(damage, transform.position);
            animator.SetBool("IsAttacking", false);
            isAttacking = false;
            yield return new WaitForSeconds(attackCooldown);
            canAttack = true;
        }

        public override void TakeDamage(float damageAmount)
        {
            base.TakeDamage(damageAmount);
            animator.SetTrigger("Hit");

            if (isAttacking)
            {
                // Interrupt attack
                if (meleeCoroutine != null)
                    StopCoroutine(meleeCoroutine);
                isAttacking = false;
                animator.SetBool("IsAttacking", false);
                // delay next attack
                canAttack = false;
                StartCoroutine(ResetAttackCooldown());
            }
        }

        private IEnumerator ResetAttackCooldown()
        {
            yield return new WaitForSeconds(hitReactCooldown);
            canAttack = true;
        }

        private void SetNewPatrolTarget()
        {
            patrolTimer = 0f;
            Vector2 rand = Random.insideUnitCircle * patrolRadius;
            patrolTarget = patrolCenter + new Vector3(rand.x, rand.y, 0f);
        }

        private void PatrolPath()
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= changePatrolTargetInterval)
                SetNewPatrolTarget();
            UpdatePath(patrolTarget);
        }

        private void UpdatePath(Vector3 target)
        {
            if (!isPathUpdating)
                StartCoroutine(PathfindingRoutine(target));
        }

        private IEnumerator PathfindingRoutine(Vector3 target)
        {
            isPathUpdating = true;
            List<Vector3> newPath = FindPath(transform.position, target);
            if (newPath != null && newPath.Count > 0)
            {
                currentPath = newPath;
                pathIndex = 0;
            }
            yield return new WaitForSeconds(pathUpdateInterval);
            isPathUpdating = false;
        }

        private List<Vector3> FindPath(Vector3 startWorld, Vector3 targetWorld)
        {
            Vector3Int current = tilemap.WorldToCell(startWorld);
            Vector3Int target = tilemap.WorldToCell(targetWorld);
            var path = new List<Vector3>();
            while (current != target)
            {
                var dir = new Vector3Int(
                    Mathf.Clamp(target.x - current.x, -1, 1),
                    Mathf.Clamp(target.y - current.y, -1, 1), 0);
                current += dir;
                var worldPt = tilemap.GetCellCenterWorld(current);
                if (tilemapCollider.OverlapPoint(worldPt)) return null;
                path.Add(worldPt);
            }
            return path;
        }

        private void MoveAlongPath()
        {
            if (currentPath == null || pathIndex >= currentPath.Count) return;
            var targetPos = currentPath[pathIndex];
            float speed = currentState == BossState.Chase ? chaseSpeed : patrolSpeed;
            var next = Vector2.MoveTowards(rb.position, (Vector2)targetPos, speed * Time.deltaTime);
            rb.MovePosition(next);

            if (Vector2.Distance(transform.position, targetPos) < 0.1f) pathIndex++;
            var dir = (targetPos - transform.position).normalized;
            if (dir != Vector3.zero)
            {
                animator.SetFloat("Xinput", dir.x);
                animator.SetFloat("Yinput", dir.y);
                animator.SetFloat("LastXinput", dir.x);
                animator.SetFloat("LastYinput", dir.y);
            }
        }

        private bool PlayerInSight()
        {
            var hit = Physics2D.Linecast(transform.position, player.transform.position, LayerMask.GetMask("Wall", "Player"));
            return hit.collider == null || hit.collider.gameObject == player.gameObject;
        }

        public override void Die()
        {
            PortalToNextLevel.SetActive(true);
            base.Die();
        }
    }
}