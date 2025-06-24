using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace enemySpace
{
    public enum BoomlingState { Patrol, Chase, Explode }

    [RequireComponent(typeof(Animator))]
    public class Boomling : enemy
    {
        [Header("References for Pathfinding")]
        [SerializeField] private TilemapCollider2D tilemapCollider;
        [SerializeField] private Tilemap tilemap;
        private List<Vector3> currentPath;
        private int pathIndex;
        private bool isPathUpdating;
        public float pathUpdateInterval = 0.75f;

        [Header("Movement Settings")]
        public float patrolSpeed = 1f;
        public float chaseSpeed = 2f;
        public float sightRange = 7f;
        public float loseSightRange = 10f;
        public float patrolRadius = 5f;
        public float changePatrolTargetInterval = 3f;

        [Header("Explosion Settings")]
        public float explosionRadius = 3f;
        public float explosionDamage = 50f;
        public float explosionDelay = 0.2f;

        private BoomlingState currentState = BoomlingState.Patrol;
        private Vector3 startPosition;
        private Vector3 patrolTarget;
        private float patrolTimer;
        private bool hasExploded;
        private Animator animator;
        private Player player;
        public static int boomlingKillCounter = 0;

        public GameObject moneyPrefab;

        void Awake()
        {

            animator = GetComponent<Animator>();
            player = FindObjectOfType<Player>();
            if (tilemapCollider == null)
                tilemapCollider = FindObjectOfType<TilemapCollider2D>();
            if (tilemap == null && tilemapCollider != null)
                tilemap = tilemapCollider.GetComponent<Tilemap>();

            startPosition = transform.position;
            SetNewPatrolTarget();
            attackRange = explosionRadius;
        }

        void Update()
        {
            if (player == null) return;

            if (health <= 0 && !hasExploded)
            {
                hasExploded = true;
                boomlingKillCounter++;
                DropLoot();
                Die();
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

            if (!hasExploded && distanceToPlayer <= explosionRadius)
            {
                ChangeState(BoomlingState.Explode);
            }
            else if (distanceToPlayer <= sightRange && distanceToPlayer > explosionRadius)
            {
                ChangeState(BoomlingState.Chase);
            }
            else if (distanceToPlayer > loseSightRange)
            {
                ChangeState(BoomlingState.Patrol);
            }

            switch (currentState)
            {
                case BoomlingState.Patrol:
                    HandlePatrol();
                    break;
                case BoomlingState.Chase:
                    HandleChase();
                    break;
                case BoomlingState.Explode:
                    if (!hasExploded)
                        StartCoroutine(PerformExplosion());
                    break;
            }
        }

        private void ChangeState(BoomlingState newState)
        {
            if (currentState == newState)
                return;

            currentState = newState;
            if (newState == BoomlingState.Explode)
            {
                currentPath = null;
            }
        }

        private void HandlePatrol()
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= changePatrolTargetInterval)
            {
                SetNewPatrolTarget();
                patrolTimer = 0f;
            }
            UpdatePath(patrolTarget);
            MoveAlongPath(patrolSpeed);
        }

        private void HandleChase()
        {
            if (player != null)
            {
                UpdatePath(player.transform.position);
                MoveAlongPath(chaseSpeed);
            }
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
            if (tilemap == null || tilemapCollider == null)
                return null;
            Vector3Int startCell = tilemap.WorldToCell(startWorld);
            Vector3Int targetCell = tilemap.WorldToCell(targetWorld);
            List<Vector3> path = new List<Vector3>();
            Vector3Int current = startCell;

            while (current != targetCell)
            {
                if (tilemapCollider.OverlapPoint(tilemap.GetCellCenterWorld(current)))
                    return null;

                path.Add(tilemap.GetCellCenterWorld(current));
                Vector3Int dir = new Vector3Int(
                    Mathf.Clamp(targetCell.x - current.x, -1, 1),
                    Mathf.Clamp(targetCell.y - current.y, -1, 1),
                    0);
                current += dir;
            }
            path.Add(tilemap.GetCellCenterWorld(targetCell));
            return path;
        }

        private void MoveAlongPath(float speed)
        {
            if (currentPath == null || pathIndex >= currentPath.Count)
                return;
            Vector3 targetPos = currentPath[pathIndex];
            Vector3 dir = (targetPos - transform.position).normalized;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            animator.SetFloat("Xinput", dir.x);
            animator.SetFloat("Yinput", dir.y);
            animator.SetFloat("LastXinput", dir.x);
            animator.SetFloat("LastYinput", dir.y);

            if (Vector3.Distance(transform.position, targetPos) < 0.1f)
                pathIndex++;
        }

        private void SetNewPatrolTarget()
        {
            Vector3 newTarget;
            do
            {
                newTarget = startPosition + (Vector3)(Random.insideUnitCircle * patrolRadius);
            } while (IsObstacleInPath(newTarget));
            patrolTarget = newTarget;
        }

        private bool IsObstacleInPath(Vector3 targetPosition)
        {
            Vector2 direction = (targetPosition - transform.position).normalized;
            float distance = Vector2.Distance(transform.position, targetPosition);
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, LayerMask.GetMask("Obstacles"));
            return hit.collider != null;
        }

        public override void Attack()
        {
        }

        private IEnumerator PerformExplosion()
        {
            if (hasExploded)
                yield break;

            hasExploded = true;
            animator.SetTrigger("Explode");
            yield return new WaitForSeconds(explosionDelay);

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            foreach (var hit in hits)
            {
                IDamageable target = hit.GetComponent<IDamageable>();
                if (target != null)
                    player.TakeDamage(damage, transform.position);
            }

            Die();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }

        private void DropLoot()
        {
            if (Random.value <= 0.2f)
                Instantiate(moneyPrefab, transform.position, Quaternion.identity);
        }
    }
}