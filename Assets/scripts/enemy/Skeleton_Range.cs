using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace enemy
{
public class Skeleton_Range : enemy
{
    [SerializeField] private Player player;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float minDis = 3f;
    [SerializeField] private float shootCooldown = 2f;
    [SerializeField] private float arrowSpeed = 10f;

    private bool canShoot = true;

    public override void Movement()
    {
        base.Movement();

        Vector2 direction = (player.transform.position - transform.position).normalized;
        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

        RaycastHit2D visionHit = Physics2D.Raycast(transform.position, direction, minimumDistance);
        Debug.DrawRay(transform.position, direction * attackRange);

        if(visionHit != null && visionHit.collider.GetComponent<Player>() == player)
        {
            if(distanceToPlayer > attackRange)
            {
                transform.position = Vector2.MoveTowards(transform.position, player.transform.position, speed * Time.deltaTime);
            }

            if(distanceToPlayer < minDis)
            {
                transform.position = Vector2.MoveTowards(transform.position, transform.position - (Vector3)direction, speed * Time.deltaTime);
            }


        }
    }

        public override void Attack()
        {
            base.Attack();

            if(canShoot)
            {
                StartCoroutine(ShootArrow());
            }
        }

        IEnumerator ShootArrow()
        {
            canShoot = false;

            Vector2 shootDirection = (player.transform.position - transform.position).normalized;

            float angle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            Vector3 arrowSpawnPoint = transform.position + (Vector3)shootDirection * 0.5f;
            GameObject arrow = Instantiate(arrowPrefab, arrowSpawnPoint, Quaternion.identity);
            Rigidbody2D arrowRb = arrow.GetComponent<Rigidbody2D>();
            arrowRb.velocity = shootDirection * arrowSpeed;

            RaycastHit2D hit = Physics2D.Raycast(shootPoint.position, shootDirection, attackRange);
            Debug.DrawRay(shootPoint.position, shootDirection * attackRange, Color.red, 1f);

            yield return new WaitForSeconds(shootCooldown);

            canShoot = true;
        }

        private void Update()
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

            if(distanceToPlayer <= minimumDistance)
            {
                Movement();
                if(distanceToPlayer <= attackRange && distanceToPlayer > minDis)
                {
                    Attack();
                }
            }
        }
    }
}
