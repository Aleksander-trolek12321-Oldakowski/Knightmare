using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace enemy
{
    public enum SkeletonState { Patrol, Chase, Attack }

    public class Skeleton_Meele : enemy
    {
        [Header("Referencje")]
        [SerializeField] private TilemapCollider2D tilemapCollider;
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private Player player;
        [SerializeField] private GameObject moneyPrefab;
        [Header("Animacja i hit‑react")]
        [SerializeField] private Animator animator;
        [SerializeField] private float hitReactCooldown = 0.23f;

        [Header("Ruch, Pościg i Patrol")]
        public float patrolSpeed = 1f;
        public float chaseSpeed = 2f;
        public float sightRange = 7f;
        public float loseSightRange = 10f;
        public float patrolRadius = 5f;
        public float changePatrolTargetInterval = 3f;

        [Header("Atak")]
        public float attackCooldown = 1.5f;

        private SkeletonState currentState = SkeletonState.Patrol;
        private Vector3 startPosition;
        private Vector3 patrolTarget;
        private float patrolTimer = 0f;

        private Rigidbody2D rb;
        private bool isAttacking = false;
        private bool canAttack = true;
        private Coroutine attackCoroutine;

        private List<Vector3> currentPath;
        private int pathIndex = 0;
        private bool isPathUpdating = false;
        public float pathUpdateInterval = 0.75f;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (player == null)
                player = FindObjectOfType<Player>();
            if (tilemapCollider == null)
                tilemapCollider = FindObjectOfType<TilemapCollider2D>();
            if (tilemap == null && tilemapCollider != null)
                tilemap = tilemapCollider.GetComponent<Tilemap>();

            startPosition = transform.position;
            SetNewPatrolTarget();
        }

        void Update()
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

            if (distanceToPlayer <= attackRange && canAttack && !isAttacking)
            {
                ChangeState(SkeletonState.Attack);
                Attack();
            }
            else if (distanceToPlayer <= sightRange && PlayerInSight())
            {
                ChangeState(SkeletonState.Chase);
            }
            else if (distanceToPlayer > loseSightRange)
            {
                startPosition = transform.position;
                ChangeState(SkeletonState.Patrol);
            }

            HandleMovement();

            if (health <= 0)
            {
                animator.SetTrigger("Death");
                DropLoot();
                Die();
            }
        }

        void ChangeState(SkeletonState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
            }
        }

        void HandleMovement()
        {
            if (currentState == SkeletonState.Patrol)
            {
                patrolTimer += Time.deltaTime;
                if (patrolTimer >= changePatrolTargetInterval)
                {
                    SetNewPatrolTarget();
                    patrolTimer = 0f;
                }
                UpdatePath(patrolTarget);
            }
            else if (currentState == SkeletonState.Chase)
            {
                UpdatePath(player.transform.position);
            }
        }

        void FixedUpdate()
        {
            Vector3 movementDirection = Vector3.zero;
            if (currentPath != null && pathIndex < currentPath.Count && currentState != SkeletonState.Attack)
            {
                Vector3 targetPos = currentPath[pathIndex];
                float moveSpeed = (currentState == SkeletonState.Chase) ? chaseSpeed : patrolSpeed;
                Vector3 newPos = Vector3.MoveTowards(rb.position, targetPos, moveSpeed * Time.fixedDeltaTime);
                movementDirection = (newPos - transform.position).normalized;
                rb.MovePosition(newPos);
                if (Vector3.Distance(rb.position, targetPos) < 0.1f)
                    pathIndex++;
            }
            if (movementDirection != Vector3.zero)
            {
                animator.SetFloat("Xinput", movementDirection.x);
                animator.SetFloat("Yinput", movementDirection.y);
                animator.SetFloat("LastXinput", movementDirection.x);
                animator.SetFloat("LastYinput", movementDirection.y);
            }
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

            isAttacking = true;
            canAttack = false;
            attackCoroutine = StartCoroutine(PerformAttack());
        }

        private IEnumerator PerformAttack()
        {
            yield return new WaitForSeconds(0.5f);
            if (Vector3.Distance(transform.position, player.transform.position) <= attackRange)
            {
                player.TakeDamage(damage);
            }
            animator.SetBool("IsAttacking", true);
            yield return new WaitForSeconds(attackCooldown);
            animator.SetBool("IsAttacking", false);
            isAttacking = false;
            canAttack = true;
            currentState = PlayerInSight() ? SkeletonState.Chase : SkeletonState.Patrol;
        }

        private bool PlayerInSight()
        {
            Vector2 startPos = transform.position;
            Vector2 targetPos = player.transform.position;
            Vector2 dir = (targetPos - startPos).normalized;
            float distance = Vector2.Distance(startPos, targetPos);
            RaycastHit2D hit = Physics2D.Raycast(startPos, dir, distance, LayerMask.GetMask("Player"));
            return hit.collider != null;
        }

        private void SetNewPatrolTarget()
        {
            patrolTarget = startPosition + (Vector3)(Random.insideUnitCircle * patrolRadius);
        }

        private void DropLoot()
        {
            if (Random.value <= 0.2f)
                Instantiate(moneyPrefab, transform.position, Quaternion.identity);
        }

        private void UpdatePath(Vector3 targetPosition)
        {
            if (!isPathUpdating)
                StartCoroutine(PathfindingRoutine(targetPosition));
        }

        IEnumerator PathfindingRoutine(Vector3 targetPosition)
        {
            isPathUpdating = true;
            List<Vector3> newPath = FindPath(transform.position, targetPosition);
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
            Vector3Int startCell = tilemap.WorldToCell(startWorld);
            Vector3Int targetCell = tilemap.WorldToCell(targetWorld);
            List<Vector3> path = new List<Vector3>();
            Vector3Int current = startCell;
            while (current != targetCell)
            {
                if (tilemapCollider.OverlapPoint(tilemap.GetCellCenterWorld(current)))
                    return null;
                path.Add(tilemap.GetCellCenterWorld(current));
                Vector3Int direction = new Vector3Int(
                    Mathf.Clamp(targetCell.x - current.x, -1, 1),
                    Mathf.Clamp(targetCell.y - current.y, -1, 1), 0);
                current += direction;
            }
            return path;
        }
    }
}