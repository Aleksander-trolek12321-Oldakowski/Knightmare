using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace enemySpace
{
    public enum ZombieState { Patrol, Chase, Attack }

    [RequireComponent(typeof(Rigidbody2D))]
    public class Zombie : enemy
    {
        [Header("Referencje")]
        [SerializeField] private TilemapCollider2D tilemapCollider;
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private Player player;
        [SerializeField] private GameObject moneyPrefab;
        [SerializeField] private Animator animator;
        [SerializeField] private LayerMask Wall;   // Warstwa ścian
        [SerializeField] private LayerMask Player; // Warstwa gracza

        [Header("Ruch, Pościg i Patrol")]
        public float patrolSpeed = 1f;
        public float chaseSpeed = 2f;
        public float sightRange = 5f;
        public float loseSightRange = 10f;
        public float patrolRadius = 5f;
        public float changePatrolTargetInterval = 3f;

        [Header("Atak")]
        public float attackCooldown = 1.5f;

        [Header("Kolor po obrażeniach")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private Color damageColor = new Color(1f, 0.45f, 0.45f);
        private Coroutine damageCoroutine;

        private ZombieState currentState = ZombieState.Patrol;
        private Vector3 startPosition;
        private Vector3 patrolTarget;
        private float patrolTimer = 0f;

        private Rigidbody2D rb;
        private bool isAttacking = false;
        private bool canAttack = true;

        // Simplified path and movement
        private List<Vector3> currentPath;
        private int pathIndex;
        private bool isPathUpdating = false;
        public float pathUpdateInterval = 0.5f;

        public static int zombieKillCounter = 0;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            if (player == null)
                player = FindObjectOfType<Player>();

            if (tilemapCollider == null)
                tilemapCollider = FindObjectOfType<TilemapCollider2D>();

            if (tilemap == null && tilemapCollider != null)
                tilemap = tilemapCollider.GetComponent<Tilemap>();

            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            originalColor = spriteRenderer.color;
            startPosition = transform.position;
            SetNewPatrolTarget();
        }

        void Update()
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

            if (distanceToPlayer <= attackRange && canAttack && !isAttacking)
            {
                ChangeState(ZombieState.Attack);
                Attack();
            }
            else if (distanceToPlayer <= sightRange && PlayerInSight())
            {
                ChangeState(ZombieState.Chase);
            }
            else if (distanceToPlayer > loseSightRange)
            {
                startPosition = transform.position;
                ChangeState(ZombieState.Patrol);
            }

            HandleMovement();

            if (health <= 0)
            {
                animator.SetTrigger("Death");
                DropLoot();
                zombieKillCounter++;
                Die();
            }
        }

        private void ChangeState(ZombieState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                currentPath = null;
                pathIndex = 0;
            }
        }

        private void HandleMovement()
        {
            switch (currentState)
            {
                case ZombieState.Patrol:
                    patrolTimer += Time.deltaTime;
                    if (patrolTimer >= changePatrolTargetInterval)
                    {
                        SetNewPatrolTarget();
                        patrolTimer = 0f;
                    }
                    UpdatePath(patrolTarget);
                    MoveAlongPath(patrolSpeed);
                    break;

                case ZombieState.Chase:
                    UpdatePath(player.transform.position);
                    MoveAlongPath(chaseSpeed);
                    break;

                case ZombieState.Attack:
                    rb.velocity = Vector2.zero;
                    break;
            }
        }

        private void FixedUpdate()
        {
            // Velocity handled in MoveAlongPath or attack state
        }

        public override void Attack()
        {
            if (isAttacking || !canAttack)
                return;

            AudioManager.Instance.PlaySound("ZombieAttack");
            isAttacking = true;
            canAttack = false;

            Vector3 attackDir = (player.transform.position - transform.position).normalized;
            animator.SetFloat("AttackXinput", attackDir.x);
            animator.SetFloat("AttackYinput", attackDir.y);
            animator.SetBool("IsAttacking", true);

            StartCoroutine(PerformAttack());
        }

        public override void TakeDamage(float damageAmount)
        {
            base.TakeDamage(damageAmount);
            AudioManager.Instance.PlaySound("ZombieDamageTaken");

            if (damageCoroutine != null)
                StopCoroutine(damageCoroutine);

            damageCoroutine = StartCoroutine(HandleDamageEffect());
        }

        private IEnumerator HandleDamageEffect()
        {
            StopCoroutine("PerformAttack");
            animator.SetBool("IsAttacking", false);
            isAttacking = false;
            canAttack = false;

            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(1f);

            spriteRenderer.color = originalColor;
            canAttack = true;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist <= sightRange && PlayerInSight())
                ChangeState(ZombieState.Chase);
            else
                ChangeState(ZombieState.Patrol);
        }

        private IEnumerator PerformAttack()
        {
            yield return new WaitForSeconds(0.5f);
            if (Vector3.Distance(transform.position, player.transform.position) <= attackRange)
            {
                player.TakeDamage(damage, transform.position);
            }
            animator.SetBool("IsAttacking", false);

            yield return new WaitForSeconds(attackCooldown);
            isAttacking = false;
            canAttack = true;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist <= sightRange && PlayerInSight())
                ChangeState(ZombieState.Chase);
            else
                ChangeState(ZombieState.Patrol);
        }

        private bool PlayerInSight()
        {
            Vector2 startPos  = transform.position;
            Vector2 targetPos = player.transform.position;
            Vector2 dir       = (targetPos - startPos).normalized;
            float distance    = Vector2.Distance(startPos, targetPos);

            int combinedMask = Wall | Player;
            RaycastHit2D hit = Physics2D.Raycast(startPos, dir, distance, combinedMask);

            if (hit.collider == null)
                return false;

            int hitLayer = hit.collider.gameObject.layer;
            if (((1 << hitLayer) & Wall.value) != 0)
                return false;

            return ((1 << hitLayer) & Player.value) != 0;
        }

        private void SetNewPatrolTarget()
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 raw = startPosition + (Vector3)(Random.insideUnitCircle * patrolRadius);
                Vector3Int cell = tilemap.WorldToCell(raw);
                Vector3 cellCenter = tilemap.GetCellCenterWorld(cell);

                if (tilemapCollider.OverlapPoint(cellCenter))
                    continue;

                patrolTarget = cellCenter;
                return;
            }
            patrolTarget = startPosition;
        }

        private void UpdatePath(Vector3 targetPosition)
        {
            if (!isPathUpdating)
                StartCoroutine(PathfindingRoutine(targetPosition));
        }

        private IEnumerator PathfindingRoutine(Vector3 targetPosition)
        {
            isPathUpdating = true;
            List<Vector3> newPath = FindSimplePath(transform.position, targetPosition);
            if (newPath != null && newPath.Count > 0)
            {
                currentPath = newPath;
                pathIndex = 0;
            }
            yield return new WaitForSeconds(pathUpdateInterval);
            isPathUpdating = false;
        }

        private List<Vector3> FindSimplePath(Vector3 startWorld, Vector3 targetWorld)
        {
            if (tilemap == null || tilemapCollider == null)
                return null;

            Vector3Int startCell = tilemap.WorldToCell(startWorld);
            Vector3Int targetCell = tilemap.WorldToCell(targetWorld);
            var path = new List<Vector3>();
            Vector3Int current = startCell;

            while (current != targetCell)
            {
                Vector3Int dir = new Vector3Int(
                    Mathf.Clamp(targetCell.x - current.x, -1, 1),
                    Mathf.Clamp(targetCell.y - current.y, -1, 1),
                    0);
                Vector3Int next = current + dir;

                Vector3 nextWorld = tilemap.GetCellCenterWorld(next);
                if (tilemapCollider.OverlapPoint(nextWorld))
                    return null;

                path.Add(nextWorld);
                current = next;
            }
            return path;
        }

        private void MoveAlongPath(float speed)
        {
            if (currentPath == null || pathIndex >= currentPath.Count)
                return;

            Vector3 targetPos = currentPath[pathIndex];
            Vector3 dir = (targetPos - transform.position).normalized;
            rb.MovePosition(Vector2.MoveTowards(rb.position, (Vector2)targetPos, speed * Time.fixedDeltaTime));

            animator.SetFloat("Xinput", dir.x);
            animator.SetFloat("Yinput", dir.y);
            if (dir != Vector3.zero)
            {
                animator.SetFloat("LastXinput", dir.x);
                animator.SetFloat("LastYinput", dir.y);
            }

            if (Vector3.Distance(transform.position, targetPos) < 0.1f)
                pathIndex++;
        }

        private void DropLoot()
        {
            if (Random.value <= 0.2f)
                Instantiate(moneyPrefab, transform.position, Quaternion.identity);
        }
    }
}
