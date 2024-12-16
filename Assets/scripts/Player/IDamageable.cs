using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{

    void TakeDamage(float damageAmount);
    void ApplyDamageOverTime(DamageOverTime effect, float duration, float damage);

}
