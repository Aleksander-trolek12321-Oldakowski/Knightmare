using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace enemy
{
    public enum ZombieState { Patrol, Chase, Attack }

    public class Zombie : enemy
    {
        [Header("Referencje")]
        [SerializeField] private TilemapCollider2D tilemapCollider;
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private Player player;
        [SerializeField] private GameObject moneyPrefab;
        [SerializeField] private Animator animator;

        [Header("Ruch, Pościg i Patrol")]
        public float patrolSpeed = 1f;
        public float chaseSpeed = 2f;
        public float sightRange = 10f;
        public float loseSightRange = 12f;  // Gdy gracz się oddali, zombie wróci do patrolu
        public float patrolRadius = 5f;
        public float changePatrolTargetInterval = 3f;

        [Header("Atak")]
        public float attackCooldown = 1.5f;

        private ZombieState currentState = ZombieState.Patrol;
        private Vector3 startPosition;
        private Vector3 patrolTarget;
        private float patrolTimer = 0f;

        private Rigidbody2D rb;
        private bool isAttacking = false;
        private bool canAttack = true;

        private List<Vector3> currentPath;
        private int pathIndex = 0;
        private bool isPathUpdating = false;
        public float pathUpdateInterval = 0.75f;


        [Header("Kolor po obrażeniach")] // ZMIANA
        [SerializeField] private SpriteRenderer spriteRenderer; // ZMIANA
        private Color originalColor; // ZMIANA
        private Color damageColor = new Color(1f, 0.45f, 0.45f); // #FF7373 jako Color
        private Coroutine damageCoroutine;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            if (player == null)
                player = FindObjectOfType<Player>();

            if (tilemapCollider == null)
                tilemapCollider = FindObjectOfType<TilemapCollider2D>();

            if (tilemap == null && tilemapCollider != null)
                tilemap = tilemapCollider.GetComponent<Tilemap>();

            if (spriteRenderer == null) // ZMIANA
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(); // domyślnie dzieciak

            originalColor = spriteRenderer.color; // ZMIANA

            startPosition = transform.position;
            SetNewPatrolTarget();
        }

        void Update()
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

            if (distanceToPlayer <= attackRange && canAttack && !isAttacking)
            {
                currentState = ZombieState.Attack;
                Attack();
            }
            else if (distanceToPlayer <= sightRange && PlayerInSight())
            {
                currentState = ZombieState.Chase;
            }
            else if (distanceToPlayer > loseSightRange)
            {
                currentState = ZombieState.Patrol;
            }

            if (currentState == ZombieState.Patrol)
            {
                patrolTimer += Time.deltaTime;
                if (patrolTimer >= changePatrolTargetInterval)
                {
                    SetNewPatrolTarget();
                    patrolTimer = 0f;
                }
                UpdatePath(patrolTarget);
            }
            else if (currentState == ZombieState.Chase)
            {
                UpdatePath(player.transform.position);
            }

            if (health <= 0)
            {
                animator.SetTrigger("Death_Zombie");
                DropLoot();
                Die();
            }
        }

        void FixedUpdate()
        {
            if (currentPath != null && pathIndex < currentPath.Count && currentState != ZombieState.Attack)
            {
                Vector3 targetPos = currentPath[pathIndex];
                float moveSpeed = (currentState == ZombieState.Chase) ? chaseSpeed : patrolSpeed;
                Vector3 newPos = Vector3.MoveTowards(rb.position, targetPos, moveSpeed * Time.fixedDeltaTime);
                rb.MovePosition(newPos);

                if (Vector3.Distance(rb.position, targetPos) < 0.1f)
                {
                    pathIndex++;
                }
            }
        }

        public override void Attack()
        {
            if (isAttacking || !canAttack)
                return;

            isAttacking = true;
            canAttack = false;
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
            // Zatrzymaj atak
            StopCoroutine("PerformAttack");
            animator.SetBool("IsAttacking", false);
            isAttacking = false;
            canAttack = false;

            // Kolor na czerwony
            spriteRenderer.color = damageColor;

            yield return new WaitForSeconds(1f); // efekt trwa 1 sekundę

            spriteRenderer.color = originalColor;
            canAttack = true;

            // Powrót do odpowiedniego stanu
            if (Vector3.Distance(transform.position, player.transform.position) <= sightRange && PlayerInSight())
                ChangeState(ZombieState.Chase);
            else
                ChangeState(ZombieState.Patrol);
        }

        IEnumerator PerformAttack()
        {
            yield return new WaitForSeconds(0.3f);
            if (Vector3.Distance(transform.position, player.transform.position) <= attackRange)
            {
                player.TakeDamage(damage);
            }
            animator.SetBool("IsAttacking", false);
            yield return new WaitForSeconds(attackCooldown);
            isAttacking = false;
            canAttack = true;
            currentState = PlayerInSight() ? ZombieState.Chase : ZombieState.Patrol;
        }

        private bool PlayerInSight()
        {
            Vector2 startPos = transform.position;
            Vector2 targetPos = player.transform.position;
            Vector2 dir = (targetPos - startPos).normalized;
            float distance = Vector2.Distance(startPos, targetPos);
            RaycastHit2D hit = Physics2D.Raycast(startPos, dir, distance);

            if (hit.collider != null)
            {
                return hit.collider.GetComponent<Player>() != null;
            }
            return false;
        }

        private void SetNewPatrolTarget()
        {
            patrolTarget = startPosition + (Vector3)(Random.insideUnitCircle * patrolRadius);
        }

        private void DropLoot()
        {
            if (Random.value <= 0.2f)
            {
                Instantiate(moneyPrefab, transform.position, Quaternion.identity);
            }
        }

        private void UpdatePath(Vector3 targetPosition)
        {
            if (!isPathUpdating)
            {
                StartCoroutine(PathfindingRoutine(targetPosition));
            }
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
                // Check for collision on the current cell
                if (tilemapCollider.OverlapPoint(tilemap.GetCellCenterWorld(current)))
                {
                    return null;
                }
                
                // Add the current cell to the path
                path.Add(tilemap.GetCellCenterWorld(current));

                // Compute direction by clamping the difference between target and current cells
                Vector3Int direction = new Vector3Int(
                    Mathf.Clamp(targetCell.x - current.x, -1, 1),
                    Mathf.Clamp(targetCell.y - current.y, -1, 1),
                    0);
                
                current += direction;
            }
            
            return path;
        }
    }
}
