// Skeleton_Meele.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace enemySpace
{
    public enum SkeletonState { Patrol, Chase, Attack }
    [RequireComponent(typeof(Rigidbody2D))]
    public class Skeleton_Meele : enemy
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
        private Coroutine damageCoroutine;

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
        public static int skeletonKillCounter;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            skeletonKillCounter = 0;
            if (player == null)
                player = FindObjectOfType<Player>();

            if (tilemapCollider == null)
                tilemapCollider = FindObjectOfType<TilemapCollider2D>();

            if (tilemap == null && tilemapCollider != null)
                tilemap = tilemapCollider.GetComponent<Tilemap>();
            startPosition = transform.position;
            SetNewPatrolTarget();
        }

        private void Update()
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

            // Atak
            if (distanceToPlayer <= attackRange && canAttack && !isAttacking)
            {
                ChangeState(ZombieState.Attack);
                Attack();
            }
            // Pościg
            else if (distanceToPlayer <= sightRange && PlayerInSight())
            {
                ChangeState(ZombieState.Chase);
            }
            // Powrót do patrolu
            else if (distanceToPlayer > loseSightRange)
            {
                startPosition = transform.position;
                ChangeState(ZombieState.Patrol);
            }

            HandleMovement();

            // Sprawdzenie śmierci
            if (health <= 0)
            {
                animator.SetTrigger("Death");
                DropLoot();
                skeletonKillCounter++;
                Die();
            }
        }

        private void ChangeState(ZombieState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                pathIndex = 0;
                currentPath = null;
                // Debug.Log($"Zmiana stanu na: {newState}");
            }
        }

        private void HandleMovement()
        {
            if (currentState == ZombieState.Patrol)
            {
                patrolTimer += Time.deltaTime;
                if (patrolTimer >= changePatrolTargetInterval || currentPath == null || pathIndex >= (currentPath?.Count ?? 0))
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
            else if (currentState == ZombieState.Attack)
            {
                // Podczas ataku nie aktualizujemy ścieżki
                rb.velocity = Vector2.zero;
            }
        }

        private void FixedUpdate()
        {
            if (currentState == ZombieState.Attack)
                return;

            if (currentPath != null && pathIndex < currentPath.Count)
            {
                Vector3 targetPos = currentPath[pathIndex];
                float speed = (currentState == ZombieState.Chase) ? chaseSpeed : patrolSpeed;

                Vector2 currentPos2D = rb.position;
                Vector2 targetPos2D = new Vector2(targetPos.x, targetPos.y);
                Vector2 dir = (targetPos2D - currentPos2D).normalized;

                rb.velocity = dir * speed;

                // Obrót animacji
                animator.SetFloat("Xinput", dir.x);
                animator.SetFloat("Yinput", dir.y);
                if (dir != Vector2.zero)
                {
                    animator.SetFloat("LastXinput", dir.x);
                    animator.SetFloat("LastYinput", dir.y);
                }

                // Jeżeli doszliśmy do węzła, przejdź do następnego
                if (Vector2.Distance(currentPos2D, targetPos2D) < 0.1f)
                {
                    pathIndex++;
                }
            }
            else
            {
                // Jeśli brak ścieżki, nie ruszaj się
                rb.velocity = Vector2.zero;
                animator.SetFloat("Xinput", 0f);
                animator.SetFloat("Yinput", 0f);
            }
        }

        public override void Attack()
        {
            if (isAttacking || !canAttack)
                return;

            AudioManager.Instance.PlaySound("ZombieAttack");
            isAttacking = true;
            canAttack = false;

            // Animacja ataku w kierunku gracza
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
            // Przerwij atak
            StopCoroutine("PerformAttack");
            animator.SetBool("IsAttacking", false);
            isAttacking = false;
            canAttack = false;

            animator.SetTrigger("Hit");
            yield return new WaitForSeconds(1f);

            // Powrót do odpowiedniego stanu
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist <= sightRange && PlayerInSight())
                ChangeState(ZombieState.Chase);
            else
                ChangeState(ZombieState.Patrol);
        }

        private IEnumerator PerformAttack()
        {
            yield return new WaitForSeconds(0.5f); // animacja wind-up
            // Jeżeli gracz dalej w zasięgu ataku:
            if (Vector3.Distance(transform.position, player.transform.position) <= attackRange)
            {
                player.TakeDamage(damage);
            }
            animator.SetBool("IsAttacking", false);

            yield return new WaitForSeconds(attackCooldown);
            isAttacking = false;
            canAttack = true;

            // Po ataku znów wchodzimy w pościg lub patrol
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

            // Zakładamy, że wallLayer i playerLayer to pola typu LayerMask (np. ustawione przez Inspector)
            int combinedMask = Wall | Player;
            RaycastHit2D hit = Physics2D.Raycast(startPos, dir, distance, combinedMask);
            Debug.DrawRay(startPos, dir * distance, Color.red, 0.1f);

            if (hit.collider == null)
                return false;

            int hitLayer = hit.collider.gameObject.layer;
            // Jeśli to warstwa „Wall” → gracz jest za przeszkodą
            if (((1 << hitLayer) & Wall.value) != 0)
                return false;

            // Jeśli to warstwa „Player” → widzimy gracza
            if (((1 << hitLayer) & Player.value) != 0)
                return true;

            return false;
        }

        private void SetNewPatrolTarget()
        {
            // Losuj punkt w obrębie patrolRadius
            for (int i = 0; i < 10; i++) // próbuj maksymalnie 10 razy
            {
                Vector3 raw = startPosition + (Vector3)(Random.insideUnitCircle * patrolRadius);
                Vector3Int cell = tilemap.WorldToCell(raw);
                Vector3 cellCenter = tilemap.GetCellCenterWorld(cell);

                // Jeżeli ściana w tym kafelku, spróbuj ponownie
                if (tilemapCollider.OverlapPoint(cellCenter))
                    continue;

                patrolTarget = cellCenter;
                return;
            }
            // Jeżeli nie znaleziono wolnego punktu, patrolTarget = startPosition
            patrolTarget = startPosition;
        }

        private void UpdatePath(Vector3 targetWorld)
        {
            if (!isPathUpdating)
                StartCoroutine(PathfindingRoutine(targetWorld));
        }

        private IEnumerator PathfindingRoutine(Vector3 targetWorld)
        {
            isPathUpdating = true;
            List<Vector3> newPath = FindPathAStar(transform.position, targetWorld);
            if (newPath != null && newPath.Count > 0)
            {
                currentPath = newPath;
                pathIndex = 0;
            }
            yield return new WaitForSeconds(pathUpdateInterval);
            isPathUpdating = false;
        }

        private bool IsCellBlocked(Vector3Int cell)
        {
            // Jeśli kafelka nie ma, to możemy przejść
            if (!tilemap.HasTile(cell))
                return false;
            // Jeśli jest kafelek (zakładamy, że to np. ściana), to blokujemy
            return true;
        }

        private List<Vector3> FindPathAStar(Vector3 startWorld, Vector3 targetWorld)
        {
            Vector3Int startCell  = tilemap.WorldToCell(startWorld);
            Vector3Int targetCell = tilemap.WorldToCell(targetWorld);

            if (IsCellBlocked(startCell) || IsCellBlocked(targetCell))
                return null;

            var openSet  = new MinHeap();
            var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            var gScore   = new Dictionary<Vector3Int, float>();
            var fScore   = new Dictionary<Vector3Int, float>();
            var visited  = new HashSet<Vector3Int>();

            gScore[startCell] = 0f;
            fScore[startCell] = Heuristic(startCell, targetCell);
            openSet.Add(startCell, fScore[startCell]);

            while (openSet.Count > 0)
            {
                Vector3Int current = openSet.Pop();
                if (current == targetCell)
                    return ReconstructPath(cameFrom, current);

                visited.Add(current);

                foreach (Vector3Int neighbor in GetNeighbors(current))
                {
                    if (visited.Contains(neighbor))
                        continue;

                    if (IsCellBlocked(neighbor))
                        continue;

                    float tentativeG = gScore[current] + Vector3Int.Distance(current, neighbor);
                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor]   = tentativeG;
                        fScore[neighbor]   = tentativeG + Heuristic(neighbor, targetCell);

                        if (!openSet.Contains(neighbor))
                            openSet.Add(neighbor, fScore[neighbor]);
                        else
                            openSet.UpdatePriority(neighbor, fScore[neighbor]);
                    }
                }
            }

            return null;
        }

        private float Heuristic(Vector3Int a, Vector3Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        private IEnumerable<Vector3Int> GetNeighbors(Vector3Int cell)
        {
            yield return new Vector3Int(cell.x + 1, cell.y, 0);
            yield return new Vector3Int(cell.x - 1, cell.y, 0);
            yield return new Vector3Int(cell.x, cell.y + 1, 0);
            yield return new Vector3Int(cell.x, cell.y - 1, 0);
        }

        private List<Vector3> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
        {
            var totalPath = new List<Vector3Int> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                totalPath.Add(current);
            }
            totalPath.Reverse();

            var worldPath = new List<Vector3>();
            foreach (Vector3Int cell in totalPath)
            {
                worldPath.Add(tilemap.GetCellCenterWorld(cell));
            }
            return worldPath;
        }

        private void DropLoot()
        {
            if (Random.value <= 0.2f)
            {
                Instantiate(moneyPrefab, transform.position, Quaternion.identity);
            }
        }

        #region --- A* Helper: MinHeap dla pary (Vector3Int, float) ---
        private class MinHeap
        {
            private List<(Vector3Int cell, float priority)> heap = new List<(Vector3Int, float)>();
            private Dictionary<Vector3Int, int> indices = new Dictionary<Vector3Int, int>();

            public int Count => heap.Count;

            public void Add(Vector3Int cell, float priority)
            {
                heap.Add((cell, priority));
                int i = heap.Count - 1;
                indices[cell] = i;
                BubbleUp(i);
            }

            public bool Contains(Vector3Int cell) => indices.ContainsKey(cell);

            public void UpdatePriority(Vector3Int cell, float newPriority)
            {
                if (!indices.TryGetValue(cell, out int i)) return;
                float oldPr = heap[i].priority;
                heap[i] = (cell, newPriority);
                if (newPriority < oldPr) BubbleUp(i);
                else BubbleDown(i);
            }

            public Vector3Int Pop()
            {
                var root = heap[0].cell;
                Swap(0, heap.Count - 1);
                heap.RemoveAt(heap.Count - 1);
                indices.Remove(root);
                BubbleDown(0);
                return root;
            }

            private void BubbleUp(int i)
            {
                while (i > 0)
                {
                    int parent = (i - 1) / 2;
                    if (heap[i].priority < heap[parent].priority)
                    {
                        Swap(i, parent);
                        i = parent;
                    }
                    else break;
                }
            }

            private void BubbleDown(int i)
            {
                int left = 2 * i + 1;
                int right = 2 * i + 2;
                int smallest = i;

                if (left < heap.Count && heap[left].priority < heap[smallest].priority)
                    smallest = left;
                if (right < heap.Count && heap[right].priority < heap[smallest].priority)
                    smallest = right;

                if (smallest != i)
                {
                    Swap(i, smallest);
                    BubbleDown(smallest);
                }
            }

            private void Swap(int i, int j)
            {
                var tmp = heap[i];
                heap[i] = heap[j];
                heap[j] = tmp;
                indices[heap[i].cell] = i;
                indices[heap[j].cell] = j;
            }
        }
        #endregion
    }
}
