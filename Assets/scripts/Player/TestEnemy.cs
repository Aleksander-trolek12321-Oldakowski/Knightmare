using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestEnemy : MonoBehaviour, IDamageable
{


    public void TakeDamage(float damageAmount)
    {

        Debug.Log("Damage " + damageAmount);

    }
}
