using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestEnemy : MonoBehaviour, IDamageable
{

    [SerializeField] private float health = 50;
    [SerializeField] private bool isOnFire = false;
    [SerializeField] private bool isPoisoned = false;

    public void TakeDamage(float damageAmount)
    {
        health -= damageAmount;
        if (health <= 0)
        {
            Die();
        }
    }

    public void ApplyDamageOverTime(DamageOverTime effect, float duration, float damage)
    {
        switch (effect)
        {
            case DamageOverTime.Fire:
                if (!isOnFire)
                {
                    Debug.Log("Fire");

                    isOnFire = true;
                    StartCoroutine(ApplyFireDamage(duration, damage));
                }
                break;

            case DamageOverTime.Poison:
                if (!isPoisoned)
                {
                    Debug.Log("Poison");

                    isPoisoned = true;
                    StartCoroutine(ApplyPoisonDamage(duration, damage));
                }
                break;
        }
    }

    private IEnumerator ApplyFireDamage(float duration, float damage)
    {
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            TakeDamage(damage);
            elapsedTime += 1f;
            yield return new WaitForSeconds(1f);
        }

        isOnFire = false;
    }

    private IEnumerator ApplyPoisonDamage(float duration, float damage)
    {
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            TakeDamage(damage);
            elapsedTime += 1f;
            yield return new WaitForSeconds(1f);
        }

        isPoisoned = false;
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
