// Skeleton_Meele with interruptible melee attack
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
        [SerializeField] private LayerMask Wall;
        [SerializeField] private LayerMask Player;

        [Header("Ruch, Pościg i Patrol")]
        public float patrolSpeed = 1f;
        public float chaseSpeed = 2f;
        public float sightRange = 5f;
        public float loseSightRange = 10f;
        public float patrolRadius = 5f;
        public float changePatrolTargetInterval = 3f;

        [Header("Atak")]
        public float attackCooldown = 1.5f;
        public float hitReactCooldown = 1f;

        private SkeletonState currentState = SkeletonState.Patrol;
        private Vector3 startPosition;
        private Vector3 patrolTarget;
        private float patrolTimer = 0f;
        private Coroutine attackCoroutine;

        private Rigidbody2D rb;
        private bool isAttacking = false;
        private bool canAttack = true;

        private List<Vector3> currentPath;
        private int pathIndex;
        private bool isPathUpdating;
        public float pathUpdateInterval = 0.75f;

        public static int skeletonKillCounter = 0;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (player == null) player = FindObjectOfType<Player>();
            if (tilemapCollider == null) tilemapCollider = FindObjectOfType<TilemapCollider2D>();
            if (tilemap == null && tilemapCollider != null) tilemap = tilemapCollider.GetComponent<Tilemap>();

            startPosition = transform.position;
            SetNewPatrolTarget();
        }

        void Update()
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            bool inSight = PlayerInSight();

            if (dist <= attackRange && canAttack && !isAttacking && inSight)
            {
                ChangeState(SkeletonState.Attack);
                Attack();
            }
            else if (dist <= sightRange && inSight)
            {
                ChangeState(SkeletonState.Chase);
            }
            else if (dist > loseSightRange)
            {
                startPosition = transform.position;
                ChangeState(SkeletonState.Patrol);
            }

            HandleMovement();

            if (health <= 0)
            {
                animator.SetTrigger("Death");
                DropLoot();
                skeletonKillCounter++;
                Die();
            }
        }

        private void ChangeState(SkeletonState newState)
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
                case SkeletonState.Patrol:
                    patrolTimer += Time.deltaTime;
                    if (patrolTimer >= changePatrolTargetInterval)
                    {
                        SetNewPatrolTarget(); patrolTimer = 0f;
                    }
                    UpdatePath(patrolTarget);
                    MoveAlongPath(patrolSpeed);
                    break;
                case SkeletonState.Chase:
                    UpdatePath(player.transform.position);
                    MoveAlongPath(chaseSpeed);
                    break;
                case SkeletonState.Attack:
                    rb.velocity = Vector2.zero;
                    break;
            }
        }

        public override void Attack()
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (isAttacking || !canAttack || dist > attackRange) return;

            Vector3 attackDir = (player.transform.position - transform.position).normalized;
            animator.SetFloat("AttackXinput", attackDir.x);
            animator.SetFloat("AttackYinput", attackDir.y);
            animator.SetBool("IsAttacking", true);

            isAttacking = true;
            canAttack = false;
            attackCoroutine = StartCoroutine(PerformAttack());
        }

        private IEnumerator PerformAttack()
        {
            yield return new WaitForSeconds(0.5f);
            if (!isAttacking) yield break;
            
            if (Vector3.Distance(transform.position, player.transform.position) <= attackRange)
                player.TakeDamage(damage, transform.position);

            animator.SetBool("IsAttacking", false);
            isAttacking = false;
            yield return new WaitForSeconds(attackCooldown);
            canAttack = true;
        }

        public override void TakeDamage(float damageAmount)
        {
            base.TakeDamage(damageAmount);
            if (attackCoroutine != null) StopCoroutine(attackCoroutine);
            isAttacking = false;
            animator.SetBool("IsAttacking", false);
            canAttack = false;
            animator.SetTrigger("Hit");
            StartCoroutine(ResetAfterHit());
        }

        private IEnumerator ResetAfterHit()
        {
            yield return new WaitForSeconds(hitReactCooldown);
            canAttack = true;
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist <= sightRange && PlayerInSight()) ChangeState(SkeletonState.Chase);
            else ChangeState(SkeletonState.Patrol);
        }

        private void SetNewPatrolTarget()
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 raw = startPosition + (Vector3)(Random.insideUnitCircle * patrolRadius);
                Vector3Int cell = tilemap.WorldToCell(raw);
                Vector3 center = tilemap.GetCellCenterWorld(cell);
                if (tilemapCollider.OverlapPoint(center)) continue;
                patrolTarget = center; return;
            }
            patrolTarget = startPosition;
        }

        private void UpdatePath(Vector3 target) { if (!isPathUpdating) StartCoroutine(PathRoutine(target)); }
        private IEnumerator PathRoutine(Vector3 target)
        {
            isPathUpdating = true;
            var newPath = FindSimplePath(transform.position, target);
            if (newPath != null && newPath.Count > 0) { currentPath = newPath; pathIndex = 0; }
            yield return new WaitForSeconds(pathUpdateInterval);
            isPathUpdating = false;
        }

        private List<Vector3> FindSimplePath(Vector3 start, Vector3 target)
        {
            if (tilemap == null || tilemapCollider == null) return null;
            Vector3Int cur = tilemap.WorldToCell(start);
            Vector3Int tar = tilemap.WorldToCell(target);
            var path = new List<Vector3>();
            while (cur != tar)
            {
                Vector3Int dir = new Vector3Int(
                    Mathf.Clamp(tar.x - cur.x, -1, 1),
                    Mathf.Clamp(tar.y - cur.y, -1, 1), 0);
                cur += dir;
                var wp = tilemap.GetCellCenterWorld(cur);
                if (tilemapCollider.OverlapPoint(wp)) return null;
                path.Add(wp);
            }
            return path;
        }

        private void MoveAlongPath(float speed)
        {
            if (currentPath == null || pathIndex >= currentPath.Count) return;
            var targ = currentPath[pathIndex];
            var next = Vector2.MoveTowards(rb.position, (Vector2)targ, speed * Time.fixedDeltaTime);
            rb.MovePosition(next);
            Vector3 dir = (next - rb.position).normalized;
            animator.SetFloat("Xinput", dir.x);
            animator.SetFloat("Yinput", dir.y);
            if (Vector2.Distance(transform.position, targ) < 0.1f) pathIndex++;
        }

        private bool PlayerInSight()
        {
            Vector2 s = transform.position;
            Vector2 t = player.transform.position;
            Vector2 d = (t - s).normalized;
            float dist = Vector2.Distance(s, t);
            int mask = Wall | Player;
            var hit = Physics2D.Raycast(s, d, dist, mask);
            if (!hit) return false;
            int layer = hit.collider.gameObject.layer;
            if (((1 << layer) & Wall.value) != 0) return false;
            return ((1 << layer) & Player.value) != 0;
        }

        private void DropLoot()
        {
            if (Random.value <= 0.2f)
                Instantiate(moneyPrefab, transform.position, Quaternion.identity);
        }
    }
}
