using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Analytics;

namespace enemySpace
{
public class enemy : MonoBehaviour, IDamageable
    {
    [SerializeField]
    public string uniqueID;
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
    [SerializeField] 
    private bool isOnFire = false;
    [SerializeField]
    private bool isPoisoned = false;
        public virtual void Start()
        {
            GenerateUniqueID();

            if (GameData.Instance.killedEnemies.Contains(uniqueID))
            {
                Destroy(gameObject);
            }

        }
        public void GenerateUniqueID()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Vector3 position = transform.position;

            int posX = Mathf.RoundToInt(position.x);
            int posY = Mathf.RoundToInt(position.y);
            int posZ = Mathf.RoundToInt(position.z);

            uniqueID = $"{posX}_{posY}_{posZ}";
        }

        public virtual void Attack()
    {

    }

    public virtual void Movement()
    {
    }


        public virtual void TakeDamage(float damageAmount)
        {
            health -= damageAmount;
        }

        public virtual void ApplyDamageOverTime(DamageOverTime effect, float duration, float damage)
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

        public void ApplySlow(float amount, float duration)
        {
            StartCoroutine(SlowEffect(amount, duration));
        }

        private IEnumerator SlowEffect(float amount, float duration)
        {
            float originalSpeed = speed;
            speed *= amount;

            yield return new WaitForSeconds(duration);

            speed = originalSpeed;
        }


        public virtual void  Die()
        {
            if (!GameData.Instance.killedEnemies.Contains(uniqueID))
            {
                GameData.Instance.killedEnemies.Add(uniqueID);
                GameData.Instance.SaveToDisk();
            }


            Destroy(gameObject);

        }
    }
}
