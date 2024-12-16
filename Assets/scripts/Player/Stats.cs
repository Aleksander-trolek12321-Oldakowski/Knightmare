using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Stats : MonoBehaviour
{
    public static Stats Instance { get; private set; }

    
    public Slider damageBar;
    public Slider speedBar;
    public Slider attackSpeedBar;
    public float maxDamage;
    public float maxSpeed;
    public float maxAttackSpeed;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        damageBar.maxValue = maxDamage;
        speedBar.maxValue = maxSpeed;
        attackSpeedBar.maxValue = maxAttackSpeed;
    }
  

    public void UpdateStats(float damage, float speed, float attackSpeed)
    {
        damageBar.value = damage;
        speedBar.value = speed;
        attackSpeedBar.value = attackSpeed;
    }
   
}
