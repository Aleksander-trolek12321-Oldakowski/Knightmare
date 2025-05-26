// Boss_Skeleton.cs
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace enemy
{
    public enum BossState { Chase, Attack, Retreat, Patrol }

    public class Boss_Skeleton : enemy
    {
        [Header("Referencje")]
        [SerializeField] private Player player;
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private Transform shootPoint;
        [SerializeField] private TilemapCollider2D tilemapCollider;
        [SerializeField] private Tilemap tilemap;

        [Header("Animacja i hit-react")]
        [SerializeField] private float hitReactCooldown = 0.23f;

        [Header("Ustawienia ataku")]
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float tripleShotDelay = 0.3f;
        [SerializeField] private float shootPointRadius = 2f;
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private float retreatDistance = 2f;

        [Header("Ruch i wykrywanie")]
        [SerializeField] private float chaseSpeed = 2f;
        [SerializeField] private float sightRange = 10f;

        [Header("Patrol")]
        [SerializeField] private float patrolSpeed = 1f;
        [SerializeField] private float patrolRadius = 5f;
        [SerializeField] private float changePatrolTargetInterval = 3f;

        private BossState currentState;
        private bool isAttacking = false;
        private bool canAttack = true;
        private Coroutine attackCoroutine;
        private Vector3 patrolTarget;
        private float patrolTimer = 0f;

        // ** pola dla pathfindingu **
        private List<Vector3> currentPath;
        private int pathIndex = 0;
        private bool isPathUpdating = false;
        public float pathUpdateInterval = 0.75f;
        [SerializeField] private GameObject portalToNextLevel;

        public override void Start()
        {
            base.Start();
            if (portalToNextLevel != null && GameData.Instance.killedEnemies.Contains(uniqueID) == false)
                portalToNextLevel.SetActive(false);
        }
        void Awake()
        {
            if (player == null)
                player = FindObjectOfType<Player>();
            if (tilemapCollider == null)
                tilemapCollider = FindObjectOfType<TilemapCollider2D>();
            if (tilemap == null && tilemapCollider != null)
                tilemap = tilemapCollider.GetComponent<Tilemap>();

            currentState = BossState.Patrol;
            patrolTarget = transform.position;

         

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
            if (distance < retreatDistance)
                currentState = BossState.Retreat;
            else if (distance <= attackRange && PlayerInSight())
                currentState = BossState.Attack;
            else if (distance <= sightRange && PlayerInSight())
                currentState = BossState.Chase;
            else
                currentState = BossState.Patrol;

            // obsługa stanów
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
                    if (canAttack && !isAttacking)
                        Attack();
                    break;
            }

            // ruch po ścieżce
            if (currentPath != null && pathIndex < currentPath.Count && currentState != BossState.Attack)
            {
                Vector3 targetPos = currentPath[pathIndex];
                float speed = (currentState == BossState.Chase || currentState == BossState.Retreat)
                              ? chaseSpeed
                              : patrolSpeed;
                transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

                if (Vector3.Distance(transform.position, targetPos) < 0.1f)
                    pathIndex++;

                // animacja ruchu
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
                // ** potrójny strzał – zostawione bez zmian **
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
                    rbArrow.velocity = shootPoint.right * speed;
            }
        }

        private bool PlayerInSight()
        {
            RaycastHit2D hit = Physics2D.Linecast(transform.position, player.transform.position);
            if (hit.collider == null || hit.collider.gameObject == player.gameObject)
                return true;
            if (tilemapCollider != null && hit.collider.gameObject == tilemapCollider.gameObject)
                return false;
            return true;
        }

        private void PatrolPath()
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= changePatrolTargetInterval)
            {
                Vector2 rand = Random.insideUnitCircle * patrolRadius;
                patrolTarget = transform.position + new Vector3(rand.x, rand.y, 0f);
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

        // ** pathfinding **
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
                if (tilemapCollider.OverlapPoint(tilemap.GetCellCenterWorld(current)))
                    return null;

                path.Add(tilemap.GetCellCenterWorld(current));
                Vector3Int dir = new Vector3Int(
                    Mathf.Clamp(targetCell.x - current.x, -1, 1),
                    Mathf.Clamp(targetCell.y - current.y, -1, 1),
                    0
                );
                current += dir;
            }
            return path;
        }
        public override void Die()
        {
            AudioManager.Instance.PlaySound("BossDeath");
            AudioManager.Instance.StopPlaylist();
            if (portalToNextLevel!=null)
            portalToNextLevel.SetActive(true);

         

            base.Die();


        }
    }
}
