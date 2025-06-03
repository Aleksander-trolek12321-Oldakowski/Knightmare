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
        [SerializeField] private LayerMask playerLayer;

        [Header("Ruch, Pościg i Patrol")]
        public float patrolSpeed = 1f;
        public float chaseSpeed = 2f;
        public float sightRange = 5f;
        public float loseSightRange = 10f;  // Gdy gracz się oddali, zombie wróci do patrolu
        public float patrolRadius = 5f;
        public float changePatrolTargetInterval = 3f;

        [Header("Atak")]
        public float attackCooldown = 1.5f;
        private Vector3 attackOrigin;

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

        public static int zombieKillCounter;


        void Awake()
        {
            zombieKillCounter = 0;
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
            Debug.Log($"Zombie position: {transform.position}, Player position: {player.transform.position}");
            Debug.Log($"Odległość do gracza: {distanceToPlayer}, aktualny stan: {currentState}");
            
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
                // Jeśli chcesz, aby nazwa wyzwalacza była taka sama jak w Skeleton_Meele ("Death")
                animator.SetTrigger("Death");
                DropLoot();
                zombieKillCounter++;
                Die();
            }
        }

        void ChangeState(ZombieState newState)
        {
            if(currentState != newState)
            {
                currentState = newState;
                Debug.Log($"Zmiana stanu na: {newState}");
            }
        }

        void HandleMovement()
        {
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
        }

        void FixedUpdate()
        {
            Vector3 movementDirection = Vector3.zero;

            if (currentPath != null && pathIndex < currentPath.Count && currentState != ZombieState.Attack)
            {
                Vector3 targetPos = currentPath[pathIndex];
                float moveSpeed = (currentState == ZombieState.Chase) ? chaseSpeed : patrolSpeed;
                Vector3 newPos = Vector3.MoveTowards(rb.position, targetPos, moveSpeed * Time.fixedDeltaTime);
                movementDirection = (newPos - transform.position).normalized;
                rb.MovePosition(newPos);

                if (Vector3.Distance(rb.position, targetPos) < 0.1f)
                {
                    pathIndex++;
                }
            }
            
            if (movementDirection != Vector3.zero)
            {
                animator.SetFloat("Xinput", movementDirection.x);
                animator.SetFloat("Yinput", movementDirection.y);
                animator.SetFloat("LastXinput", movementDirection.x);
                animator.SetFloat("LastYinput", movementDirection.y);
            }
        }

        public override void Attack()
        {
            if (isAttacking || !canAttack)
                return;
            AudioManager.Instance.PlaySound("ZombieAttack");

            isAttacking = true;
            canAttack = false;
            attackOrigin = transform.position;

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
            yield return new WaitForSeconds(3f);
            if (Vector3.Distance(attackOrigin, player.transform.position) <= attackRange)
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
            int layerMask = LayerMask.GetMask("Player"); 
            RaycastHit2D hit = Physics2D.Raycast(startPos, dir, distance, layerMask);

            Debug.DrawRay(startPos, dir * distance, Color.red, 0.5f);

            return hit.collider != null;
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
                if (tilemapCollider.OverlapPoint(tilemap.GetCellCenterWorld(current)))
                {
                    return null;
                }
                
                path.Add(tilemap.GetCellCenterWorld(current));

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
