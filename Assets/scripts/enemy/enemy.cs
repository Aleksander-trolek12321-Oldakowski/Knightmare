using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

namespace enemy
{
public class enemy : MonoBehaviour
public class enemy
{
    public int id;
    public string name;
    public float health;
    public float speed;
    public float damage;
    public float attackRange;
    
    public Transform playerTransform;
    public GameObject player;

    private void Start()
    {
        playerTransform = player.transform;
    }

    private void Update()
    {
        Movement();
    }

    public GameObject player;

    protected virtual void Attack()
    {

    }

    public virtual void Movement()
    {
    }
}
}
