// Skeleton_Meele.cs
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
        [Header("Animacja i hit-react")]
        [SerializeField] private Animator animator;
        [SerializeField] private float hitReactCooldown = 1f;

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

        private bool isAttacking = false;
        private bool canAttack = true;
        private Coroutine attackCoroutine;
        private bool isReactingToHit = false;

        // ** nowe pola dla pathfindingu **
        private List<Vector3> currentPath;
        private int pathIndex = 0;
        private bool isPathUpdating = false;
        public float pathUpdateInterval = 0.75f;

        void Awake()
        {
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

            if (!isReactingToHit && distanceToPlayer <= attackRange && canAttack && !isAttacking)
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

            HandleStateMovement();

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
                currentState = newState;
        }

        private void HandleStateMovement()
        {
            switch (currentState)
            {
                case SkeletonState.Patrol:
                    patrolTimer += Time.deltaTime;
                    if (patrolTimer >= changePatrolTargetInterval)
                    {
                        SetNewPatrolTarget();
                        patrolTimer = 0f;
                    }
                    UpdatePath(patrolTarget);
                    break;

                case SkeletonState.Chase:
                    UpdatePath(player.transform.position);
                    break;

                case SkeletonState.Attack:
                    break;
            }

            // ruch wzdłuż ścieżki
            if (currentPath != null && pathIndex < currentPath.Count && currentState != SkeletonState.Attack)
            {
                Vector3 targetPos = currentPath[pathIndex];
                float moveSpeed = (currentState == SkeletonState.Chase) ? chaseSpeed : patrolSpeed;
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

                Vector3 dir = (targetPos - transform.position).normalized;
                if (dir != Vector3.zero)
                {
                    animator.SetFloat("Xinput", dir.x);
                    animator.SetFloat("Yinput", dir.y);
                    animator.SetFloat("LastXinput", dir.x);
                    animator.SetFloat("LastYinput", dir.y);
                }

                if (Vector3.Distance(transform.position, targetPos) < 0.1f)
                    pathIndex++;
            }
        }

        public override void TakeDamage(float damageAmount)
        {
            base.TakeDamage(damageAmount);
            if (!isReactingToHit)
                StartCoroutine(HitReactRoutine());
            InterruptAttack();
        }

        private IEnumerator HitReactRoutine()
        {
            isReactingToHit = true;
            animator.SetTrigger("Hit");
            yield return new WaitForSeconds(hitReactCooldown);
            isReactingToHit = false;
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
                player.TakeDamage(damage);

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
            float dist = Vector2.Distance(startPos, targetPos);
            RaycastHit2D hit = Physics2D.Raycast(startPos, dir, dist, LayerMask.GetMask("Player"));
            return hit.collider != null;
        }

        private void SetNewPatrolTarget()
        {
            // Losowanie nowego celu patrolu
            Vector3 newPatrolTarget = startPosition + (Vector3)(Random.insideUnitCircle * patrolRadius);

            // Sprawdzenie, czy w kierunku nowego celu nie ma przeszkody
            if (IsObstacleInPath(newPatrolTarget))
            {
                // Jeśli jest przeszkoda, próbujemy ustawić nowy cel
                SetNewPatrolTarget();
            }
            else
            {
                patrolTarget = newPatrolTarget;
            }
        }

        private bool IsObstacleInPath(Vector3 targetPosition)
        {
            // Wykonaj raycast od obecnej pozycji do nowego celu patrolu
            Vector2 direction = (targetPosition - transform.position).normalized;
            float distance = Vector2.Distance(transform.position, targetPosition);

            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, LayerMask.GetMask("Obstacles"));
            return hit.collider != null;  // Jeśli napotkano przeszkodę, zwróć true
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

        private IEnumerator PathfindingRoutine(Vector3 targetPosition)
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
                // jeśli na komórce jest ściana – przerwij
                if (tilemapCollider.OverlapPoint(tilemap.GetCellCenterWorld(current)))
                    return null;

                path.Add(tilemap.GetCellCenterWorld(current));
                Vector3Int dir = new Vector3Int(
                    Mathf.Clamp(targetCell.x - current.x, -1, 1),
                    Mathf.Clamp(targetCell.y - current.y, -1, 1),
                    0);
                current += dir;
            }
            return path;
        }
    }
}
