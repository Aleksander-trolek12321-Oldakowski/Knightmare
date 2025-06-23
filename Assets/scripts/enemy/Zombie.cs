// Zombie with interruptible melee attack and feedback color
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
        [SerializeField] private LayerMask Wall;
        [SerializeField] private LayerMask Player;

        [Header("Ruch, Pościg i Patrol")]
        public float patrolSpeed = 1f;
        public float chaseSpeed = 2f;
        public float sightRange = 5f;
        public float loseSightRange = 10f;
        public float patrolRadius = 5f;
        public float changePatrolTargetInterval = 3f;

        [Header("Atak melee")]
        public float attackCooldown = 1.5f;
        public float hitReactCooldown = 1f;

        [Header("Kolor po obrażeniach")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private Color damageColor = new Color(1f, 0.45f, 0.45f);

        private ZombieState currentState = ZombieState.Patrol;
        private Vector3 startPosition;
        private Vector3 patrolTarget;
        private float patrolTimer = 0f;

        private Rigidbody2D rb;
        private bool isAttacking = false;
        private bool canAttack = true;
        private Coroutine attackCoroutine;
        private Coroutine damageCoroutine;

        private List<Vector3> currentPath;
        private int pathIndex;
        private bool isPathUpdating;
        public float pathUpdateInterval = 0.5f;

        public static int zombieKillCounter = 0;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (player == null) player = FindObjectOfType<Player>();
            if (tilemapCollider == null) tilemapCollider = FindObjectOfType<TilemapCollider2D>();
            if (tilemap == null && tilemapCollider != null) tilemap = tilemapCollider.GetComponent<Tilemap>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            originalColor = spriteRenderer.color;
            startPosition = transform.position;
            SetNewPatrolTarget();
        }

        void Update()
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            bool inSight = PlayerInSight();

            if (distance <= attackRange && !isAttacking && canAttack && inSight)
            {
                ChangeState(ZombieState.Attack);
                Attack();
            }
            else if (distance <= sightRange && inSight)
            {
                ChangeState(ZombieState.Chase);
            }
            else if (distance > loseSightRange)
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

        public override void Attack()
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (isAttacking || !canAttack || distance > attackRange) return;

            Vector3 dir = (player.transform.position - transform.position).normalized;
            animator.SetFloat("AttackXinput", dir.x);
            animator.SetFloat("AttackYinput", dir.y);
            animator.SetBool("IsAttacking", true);

            isAttacking = true;
            canAttack = false;
            attackCoroutine = StartCoroutine(PerformAttack());
        }

        private IEnumerator PerformAttack()
        {
            yield return new WaitForSeconds(0.5f);
            if (!isAttacking) yield break;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist <= attackRange && PlayerInSight())
                player.TakeDamage(damage);

            animator.SetBool("IsAttacking", false);
            isAttacking = false;
            yield return new WaitForSeconds(attackCooldown);
            canAttack = true;
        }

        public override void TakeDamage(float damageAmount)
        {
            base.TakeDamage(damageAmount);
            AudioManager.Instance.PlaySound("ZombieDamageTaken");

            // Interrupt attack coroutine
            if (attackCoroutine != null)
                StopCoroutine(attackCoroutine);
            isAttacking = false;
            animator.SetBool("IsAttacking", false);
            canAttack = false;

            // Damage color effect
            if (damageCoroutine != null)
                StopCoroutine(damageCoroutine);
            damageCoroutine = StartCoroutine(HandleDamageEffect());
        }

        private IEnumerator HandleDamageEffect()
        {
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(1f);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(hitReactCooldown);
            canAttack = true;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist <= sightRange && PlayerInSight())
                ChangeState(ZombieState.Chase);
            else
                ChangeState(ZombieState.Patrol);
        }

        private void SetNewPatrolTarget()
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 raw = startPosition + (Vector3)(Random.insideUnitCircle * patrolRadius);
                Vector3Int cell = tilemap.WorldToCell(raw);
                Vector3 center = tilemap.GetCellCenterWorld(cell);
                if (tilemapCollider.OverlapPoint(center)) continue;
                patrolTarget = center;
                return;
            }
            patrolTarget = startPosition;
        }

        private void UpdatePath(Vector3 target)
        {
            if (!isPathUpdating)
                StartCoroutine(PathingRoutine(target));
        }

        private IEnumerator PathingRoutine(Vector3 target)
        {
            isPathUpdating = true;
            var newPath = FindSimplePath(transform.position, target);
            if (newPath != null && newPath.Count > 0)
            {
                currentPath = newPath;
                pathIndex = 0;
            }
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
                var dir = new Vector3Int(
                    Mathf.Clamp(tar.x - cur.x, -1, 1),
                    Mathf.Clamp(tar.y - cur.y, -1, 1), 0);
                cur += dir;
                var pt = tilemap.GetCellCenterWorld(cur);
                if (tilemapCollider.OverlapPoint(pt)) return null;
                path.Add(pt);
            }
            return path;
        }

        private void MoveAlongPath(float speed)
        {
            if (currentPath == null || pathIndex >= currentPath.Count) return;
            var targetPos = currentPath[pathIndex];
            var next = Vector2.MoveTowards(rb.position, (Vector2)targetPos, speed * Time.fixedDeltaTime);
            rb.MovePosition(next);
            var dir = (targetPos - transform.position).normalized;
            animator.SetFloat("Xinput", dir.x);
            animator.SetFloat("Yinput", dir.y);
            if (Vector3.Distance(transform.position, targetPos) < 0.1f) pathIndex++;
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
            int lay = hit.collider.gameObject.layer;
            if (((1 << lay) & Wall.value) != 0) return false;
            return ((1 << lay) & Player.value) != 0;
        }

        private void DropLoot()
        {
            if (Random.value <= 0.2f)
                Instantiate(moneyPrefab, transform.position, Quaternion.identity);
        }
    }
}