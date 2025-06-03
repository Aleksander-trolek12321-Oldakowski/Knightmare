// Boss_Skeleton.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace enemy
{
    public enum BossState { Chase, Attack, Retreat, Patrol }

    [RequireComponent(typeof(Rigidbody2D))]
    public class Boss_Skeleton : enemy
    {
        [Header("Referencje")]
        [SerializeField] private Player player;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private Transform shootPoint;
        [SerializeField] private TilemapCollider2D tilemapCollider;
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private LayerMask wallLayer;   // Warstwa ścian
        [SerializeField] private LayerMask playerLayer; // Warstwa gracza

        [Header("Animacja i hit-react")]
        [SerializeField] private float hitReactCooldown = 0.23f;

        [Header("Ustawienia ataku")]
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float tripleShotDelay = 0.3f;
        [SerializeField] private float shootPointRadius = 2f;
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private float retreatDistance = 2f;
        [SerializeField] private float arrowSpeed = 10f;

        [Header("Ruch i wykrywanie")]
        [SerializeField] private float chaseSpeed = 2f;
        [SerializeField] private float sightRange = 10f;

        [Header("Patrol")]
        [SerializeField] private float patrolSpeed = 1f;
        [SerializeField] private float patrolRadius = 5f;                  // Maksymalny dystans od centrum patrolu
        [SerializeField] private float changePatrolTargetInterval = 3f;

        [Header("Pathfinding")]
        public float pathUpdateInterval = 0.75f;

        [SerializeField] private GameObject portalToNextLevel;

        private BossState currentState;
        private bool isAttacking = false;
        private bool canAttack = true;
        private Coroutine attackCoroutine;
        private Vector3 patrolTarget;
        private float patrolTimer = 0f;

        // To, aby boss nie wychodził poza stały obszar
        private Vector3 patrolCenter;

        private List<Vector3> currentPath;
        private int pathIndex = 0;
        private bool isPathUpdating = false;

        private Rigidbody2D rb;

        public override void Start()
        {
            base.Start();
            if (portalToNextLevel != null && GameData.Instance.killedEnemies.Contains(uniqueID) == false)
                portalToNextLevel.SetActive(false);
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            if (player == null)
                player = FindObjectOfType<Player>();
            if (tilemapCollider == null)
                tilemapCollider = FindObjectOfType<TilemapCollider2D>();
            if (tilemap == null && tilemapCollider != null)
                tilemap = tilemapCollider.GetComponent<Tilemap>();

            currentState = BossState.Patrol;
            patrolCenter = transform.position; // Ustawiamy centrum patrolu na transform.position bossa
            patrolTarget = patrolCenter;       // Początkowo boss stoi w centrum
        }

        private void Update()
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

            // Obsługa stanów
            switch (currentState)
            {
                case BossState.Chase:
                    UpdatePath(player.transform.position);
                    break;
                case BossState.Retreat:
                    Vector3 retreatTarget = transform.position + (transform.position - player.transform.position);
                    UpdatePath(retreatTarget);
                    break;
                case BossState.Patrol:
                    PatrolPath();
                    break;
                case BossState.Attack:
                    if (canAttack && !isAttacking)
                        Attack();
                    break;
            }

            // Ruch (velocity + animacja)
            HandleMovement();

            RotateShootPoint();
            UpdateShootPointPosition();
        }

        private void HandleMovement()
        {
            if (currentState == BossState.Attack)
            {
                // Zerujemy prędkość podczas ataku
                rb.velocity = Vector2.zero;
                animator.SetFloat("Xinput", 0f);
                animator.SetFloat("Yinput", 0f);
                return;
            }

            if (currentPath != null && pathIndex < currentPath.Count)
            {
                Vector3 targetPos = currentPath[pathIndex];
                float speed = (currentState == BossState.Chase || currentState == BossState.Retreat)
                              ? chaseSpeed
                              : patrolSpeed;

                Vector2 currentPos2D = rb.position;
                Vector2 targetPos2D = new Vector2(targetPos.x, targetPos.y);
                Vector2 dir = (targetPos2D - currentPos2D).normalized;

                rb.velocity = dir * speed;

                // Animacja ruchu
                animator.SetFloat("Xinput", dir.x);
                animator.SetFloat("Yinput", dir.y);
                if (dir != Vector2.zero)
                {
                    animator.SetFloat("LastXinput", dir.x);
                    animator.SetFloat("LastYinput", dir.y);
                }

                if (Vector2.Distance(currentPos2D, targetPos2D) < 0.1f)
                {
                    pathIndex++;
                }
            }
            else
            {
                rb.velocity = Vector2.zero;
                animator.SetFloat("Xinput", 0f);
                animator.SetFloat("Yinput", 0f);
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
                    rbArrow.velocity = shootPoint.right * arrowSpeed;
            }
        }

        private bool PlayerInSight()
        {
            Vector2 startPos = transform.position;
            Vector2 targetPos = player.transform.position;
            Vector2 dir = (targetPos - startPos).normalized;
            float distance = Vector2.Distance(startPos, targetPos);

            int combinedMask = wallLayer | playerLayer;
            RaycastHit2D hit = Physics2D.Raycast(startPos, dir, distance, combinedMask);
            Debug.DrawRay(startPos, dir * distance, Color.red, 0.1f);

            if (hit.collider == null)
                return false;

            // Jeśli trafił w ścianę → gracz ukryty
            if (((1 << hit.collider.gameObject.layer) & wallLayer) != 0)
                return false;

            // Trafiliśmy w gracza
            return ((1 << hit.collider.gameObject.layer) & playerLayer) != 0;
        }

        private void PatrolPath()
        {
            patrolTimer += Time.deltaTime;

            // Jeżeli czas na zmianę celu lub brak ścieżki / dotarliśmy do ostatniego punktu:
            if (patrolTimer >= changePatrolTargetInterval
                || currentPath == null
                || pathIndex >= (currentPath?.Count ?? 0))
            {
                // Losujemy nowy punkt w obrębie koła o promieniu patrolRadius wokół patrolCenter
                Vector2 rand = Random.insideUnitCircle * patrolRadius;
                Vector3 candidate = patrolCenter + new Vector3(rand.x, rand.y, 0f);

                // Sprawdzamy, czy wylosowany kafelek nie jest ścianą
                Vector3Int cell = tilemap.WorldToCell(candidate);
                Vector3 cellCenter = tilemap.GetCellCenterWorld(cell);
                if (!tilemapCollider.OverlapPoint(cellCenter))
                {
                    patrolTarget = cellCenter;
                }
                else
                {
                    // Fallback do środka obszaru patrolu
                    patrolTarget = patrolCenter;
                }

                patrolTimer = 0f;
            }

            UpdatePath(patrolTarget);
        }

        private void RotateShootPoint()
        {
            if (!player || !shootPoint) return;
            Vector2 dir = (player.transform.position - shootPoint.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            shootPoint.rotation = Quaternion.Lerp(
                shootPoint.rotation,
                Quaternion.Euler(0, 0, angle),
                Time.deltaTime * rotationSpeed
            );
        }

        private void UpdateShootPointPosition()
        {
            if (!player || !shootPoint) return;
            Vector2 dir = (player.transform.position - transform.position).normalized;
            shootPoint.position = (Vector2)transform.position + dir * shootPointRadius;
        }

        // Pathfinding
        private void UpdatePath(Vector3 targetPosition)
        {
            if (!isPathUpdating)
                StartCoroutine(PathfindingRoutine(targetPosition));
        }

        private IEnumerator PathfindingRoutine(Vector3 targetPosition)
        {
            isPathUpdating = true;
            List<Vector3> newPath = FindPathAStar(transform.position, targetPosition);
            if (newPath != null && newPath.Count > 0)
            {
                currentPath = newPath;
                pathIndex = 0;
            }
            yield return new WaitForSeconds(pathUpdateInterval);
            isPathUpdating = false;
        }

        private List<Vector3> FindPathAStar(Vector3 startWorld, Vector3 targetWorld)
        {
            Vector3Int startCell = tilemap.WorldToCell(startWorld);
            Vector3Int targetCell = tilemap.WorldToCell(targetWorld);

            // Jeśli start lub cel w ścianie → zwróć null
            if (tilemapCollider.OverlapPoint(tilemap.GetCellCenterWorld(startCell)) ||
                tilemapCollider.OverlapPoint(tilemap.GetCellCenterWorld(targetCell)))
                return null;

            var openSet = new MinHeap();
            var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            var gScore = new Dictionary<Vector3Int, float>();
            var fScore = new Dictionary<Vector3Int, float>();
            var visited = new HashSet<Vector3Int>();

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

                    Vector3 worldCenter = tilemap.GetCellCenterWorld(neighbor);
                    if (tilemapCollider.OverlapPoint(worldCenter))
                        continue;

                    float tentativeG = gScore[current] + Vector3Int.Distance(current, neighbor);
                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        fScore[neighbor] = tentativeG + Heuristic(neighbor, targetCell);

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

        public override void Die()
        {
            AudioManager.Instance.PlaySound("BossDeath");
            AudioManager.Instance.StopPlaylist();

            if (portalToNextLevel != null)
            {
                // Odczekaj 5 sekund, zanim aktywujesz portal
                StartCoroutine(ActivatePortalAfterDelay(5f));
            }
        }

        private IEnumerator ActivatePortalAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            portalToNextLevel.SetActive(true);
            base.Die();
        }

        // A* Helper: MinHeap dla pary (Vector3Int, float)
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
    }
}
