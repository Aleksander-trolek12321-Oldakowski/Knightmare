using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

namespace enemy
{
public class enemy : MonoBehaviour
{
    [SerializeField]
    int id;
    [SerializeField]
    string name;
    [SerializeField]
    public float health;
    [SerializeField]
    public float speed;
    [SerializeField]
    public float damage;
    [SerializeField]
    public float attackRange;
    [SerializeField]
    public float attackSpeed;
    [SerializeField]
    public float minimumDistance = 15f;
    
    public Transform playerTransform;
    public GameObject player;

    private void Start()
    {
        playerTransform = player.transform;
    }

    public virtual void Attack()
    {

    }

    public virtual void Movement()
    {
    }
}
}
