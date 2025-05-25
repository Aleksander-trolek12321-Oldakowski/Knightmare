// Boss_Mage.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace enemy
{
    // Wykorzystujemy wspólne enum BossState z Boss_Skeleton
    public class Boss_Mage : enemy
    {
        [Header("References")]
        [SerializeField] private Player player;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject fireballPrefab;
        [SerializeField] private Transform shootPoint;
        [SerializeField] private List<GameObject> necroPrefabs;
        [SerializeField] private TilemapCollider2D tilemapCollider;
        [SerializeField] private Tilemap tilemap;

        [Header("Animation & Hit React")]
        [SerializeField] private float hitReactCooldown = 0.23f;

        [Header("Attack Settings")]
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private float shootPointRadius = 2f;
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private float retreatDistance = 3f;

        [Header("Movement & Detection")]
        [SerializeField] private float chaseSpeed = 2.5f;
        [SerializeField] private float sightRange = 12f;

        [Header("Patrol")]
        [SerializeField] private float patrolSpeed = 1f;
        [SerializeField] private float patrolRadius = 5f;
        [SerializeField] private float changePatrolTargetInterval = 3f;

        [Header("Necromancy")]
        [SerializeField] private int minSpawn = 1;
        [SerializeField] private int maxSpawn = 3;
        [SerializeField] private float spawnDelay = 0.5f;
        [SerializeField] private float spawnRadius = 1.5f;

        private BossState currentState;
        private bool isAttacking = false;
        private bool canAttack = true;
        private Coroutine attackCoroutine;

        private Vector3 patrolTarget;
        private float patrolTimer = 0f;

        private List<Vector3> currentPath;
        private int pathIndex = 0;
        private bool isPathUpdating = false;
        [SerializeField] private float pathUpdateInterval = 0.75f;
        [SerializeField] private GameObject portalToNextLevel;

        public override void Start()
        {
            base.Start();
            if (portalToNextLevel != null && !GameData.Instance.killedEnemies.Contains(uniqueID))
                portalToNextLevel.SetActive(false);
        }

        void Awake()
        {
            if (player == null) player = FindObjectOfType<Player>();
            if (tilemapCollider == null) tilemapCollider = FindObjectOfType<TilemapCollider2D>();
            if (tilemap == null && tilemapCollider != null) tilemap = tilemapCollider.GetComponent<Tilemap>();

            currentState = BossState.Patrol;
            patrolTarget = transform.position;
        }

        void Update()
        {
            if (health <= 0)
            {
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
                case BossState.Chase:
                    UpdatePath(player.transform.position);
                    break;
                case BossState.Retreat:
                    UpdatePath(transform.position + (transform.position - player.transform.position));
                    break;
                case BossState.Patrol:
                    PatrolPath();
                    break;
                case BossState.Attack:
                    if (canAttack && !isAttacking) Attack();
                    break;
            }

            if (currentPath != null && pathIndex < currentPath.Count && currentState != BossState.Attack)
            {
                Vector3 targetPos = currentPath[pathIndex];
                float speed = (currentState == BossState.Chase || currentState == BossState.Retreat)
                              ? chaseSpeed : patrolSpeed;
                transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

                if (Vector3.Distance(transform.position, targetPos) < 0.1f) pathIndex++;

                Vector3 dir = (targetPos - transform.position).normalized;
                if (dir != Vector3.zero)
                {
                    animator.SetFloat("Xinput", dir.x);
                    animator.SetFloat("Yinput", dir.y);
                    animator.SetFloat("LastXinput", dir.x);
                    animator.SetFloat("LastYinput", dir.y);
                }
            }

            RotateShootPoint();
            UpdateShootPointPosition();
        }

        public override void TakeDamage(float amount)
        {
            base.TakeDamage(amount);
            InterruptAttack();
        }

        private void InterruptAttack()
        {
            if (isAttacking)
            {
                if (attackCoroutine != null) StopCoroutine(attackCoroutine);
                isAttacking = false;
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
            if (isAttacking || !canAttack) return;
            attackCoroutine = StartCoroutine(PerformAttack());
        }

        private IEnumerator PerformAttack()
        {
            isAttacking = true;
            canAttack = false;

            float roll = Random.value;
            if (roll <= 0.8f)
            {
                yield return new WaitForSeconds(0.05f);
                ShootFireball();
            }
            else
            {
                int count = Random.Range(minSpawn, maxSpawn + 1);
                for (int i = 0; i < count; i++)
                {
                    yield return new WaitForSeconds(spawnDelay);
                    SpawnMinion();
                }
            }

            yield return new WaitForSeconds(attackCooldown);
            isAttacking = false;
            canAttack = true;
        }

        private void ShootFireball()
        {
            if (fireballPrefab == null || shootPoint == null) return;
            GameObject ball = Instantiate(fireballPrefab, shootPoint.position, shootPoint.rotation);
            Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = shootPoint.right * speed;
        }

        private void SpawnMinion()
        {
            if (necroPrefabs == null || necroPrefabs.Count == 0) return;
            int idx = Random.Range(0, necroPrefabs.Count);
            Vector2 offset = Random.insideUnitCircle.normalized * spawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(offset.x, offset.y, 0);
            Instantiate(necroPrefabs[idx], spawnPos, Quaternion.identity);
        }

        private bool PlayerInSight()
        {
            RaycastHit2D hit = Physics2D.Linecast(transform.position, player.transform.position);
            if (hit.collider == null || hit.collider.gameObject == player.gameObject) return true;
            if (tilemapCollider != null && hit.collider.gameObject == tilemapCollider.gameObject) return false;
            return true;
        }

        private void PatrolPath()
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= changePatrolTargetInterval)
            {
                Vector2 rand = Random.insideUnitCircle * patrolRadius;
                patrolTarget = transform.position + new Vector3(rand.x, rand.y, 0);
                patrolTimer = 0f;
            }
            UpdatePath(patrolTarget);
        }

        private void RotateShootPoint()
        {
            if (player == null || shootPoint == null) return;
            Vector2 dir = (player.transform.position - shootPoint.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            shootPoint.rotation = Quaternion.Lerp(shootPoint.rotation, Quaternion.Euler(0, 0, angle), Time.deltaTime * rotationSpeed);
        }

        private void UpdateShootPointPosition()
        {
            if (player == null || shootPoint == null) return;
            Vector2 dir = (player.transform.position - transform.position).normalized;
            shootPoint.position = (Vector2)transform.position + dir * shootPointRadius;
        }

        private void UpdatePath(Vector3 target)
        {
            if (!isPathUpdating) StartCoroutine(PathfindingRoutine(target));
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

        private List<Vector3> FindPath(Vector3 start, Vector3 end)
        {
            Vector3Int s = tilemap.WorldToCell(start);
            Vector3Int t = tilemap.WorldToCell(end);
            List<Vector3> path = new List<Vector3>();
            Vector3Int cur = s;
            while (cur != t)
            {
                if (tilemapCollider.OverlapPoint(tilemap.GetCellCenterWorld(cur))) return null;
                path.Add(tilemap.GetCellCenterWorld(cur));
                Vector3Int dir = new Vector3Int(
                    Mathf.Clamp(t.x - cur.x, -1, 1),
                    Mathf.Clamp(t.y - cur.y, -1, 1), 0);
                cur += dir;
            }
            return path;
        }

        public override void Die()
        {
            if (portalToNextLevel != null) portalToNextLevel.SetActive(true);
            base.Die();
        }
    }
}
