using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

namespace enemy
{
public class enemy
{
    public int id;
    public string name;
    public float health;
    public float speed;
    public float damage;
    public float attackRange;
    public GameObject player;

    protected virtual void Attack()
    {

    }

    public void Movement()
    {

    }
}
}
