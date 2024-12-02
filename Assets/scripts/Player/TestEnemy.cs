using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestEnemy : MonoBehaviour, IDamageable
{


    public void TakeDamage(int damageAmount)
    {

        Debug.Log("Damage " + damageAmount);

    }
}
