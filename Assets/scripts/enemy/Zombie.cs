using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace enemy
{
public class Zombie : enemy
{
    public float minimumDistance = 15f;
    public override void Movement()
    {
        base.Movement();

        Vector2 direction = (playerTransform.position - transform.position).normalized;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer > attackRange)
        {
            LayerMask playerLayer = LayerMask.GetMask("Player");
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, minimumDistance, playerLayer);

            Debug.DrawRay(transform.position, direction * attackRange, Color.red);

            if (hit.collider != null && hit.collider.CompareTag("Player"))
            {
                Debug.Log($"Zombie widzi gracza i porusza się! (Dystans: {distanceToPlayer})");
                transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, speed * Time.deltaTime);
            }
            else
            {
                Debug.Log("Zombie nie widzi gracza.");
            }
        }
        else
        {
            Debug.Log("Zombie jest w zasięgu ataku, nie porusza się.");
        }
    }
}
}
