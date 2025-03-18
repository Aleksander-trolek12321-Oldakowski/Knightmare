using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    Rigidbody2D rb;
    Vector2 movementVector;
    Animator animator;
    public PlayerInput playerInputActions;
    private bool isAttacking = false;
    private float attackSpeedUI;

    [SerializeField] GameObject attackSpherePrefab;
    private bool isHoldingAttack = false;
    float damageRadius = 0.75f;
    [SerializeField] public float health = 10;
    [SerializeField] private float damage = 10;
    [SerializeField] private float speed = 3f;
    [SerializeField] private float range = 1f;
    [SerializeField] private float attackSpeed = 1f;
    [SerializeField] private bool canFire = false;
    [SerializeField] private bool canPoison = false;

    [SerializeField] private Vector2 respawnPoint;
    private bool isDead = false;

    [SerializeField] private int maxHearts = 3; 
    private int healthPerHeart = 4; 
    [SerializeField] private float currentHealth;

    [SerializeField] private List<Image> heartImages;
    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite threeQuartersHeart;
    [SerializeField] private Sprite halfHeart;
    [SerializeField] private Sprite quarterHeart;
    [SerializeField] private Sprite emptyHeart;
    private bool isWalking = false;
    void Awake()
    {
        AudioManager.Instance.PlaySound("MusicGame");
        AudioManager.Instance.StopSound("MusicMenu");

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        playerInputActions = new PlayerInput();
        playerInputActions.Player.Move.performed += MovePerformed;
        playerInputActions.Player.Move.canceled += OnMoveCanceled;

        playerInputActions.Player.Fire.started += OnAttackStarted;
        playerInputActions.Player.Fire.canceled += OnAttackCanceled;

        playerInputActions.Player.Enable();

    }
    private void Start()
    {
        attackSpeedUI =  attackSpeed;

        Stats.Instance.UpdateStats(damage, speed, attackSpeedUI);

    }

    void OnDestroy()
    {
        playerInputActions.Player.Move.performed -= MovePerformed;
        playerInputActions.Player.Move.canceled -= OnMoveCanceled;

        playerInputActions.Player.Fire.started -= OnAttackStarted;
        playerInputActions.Player.Fire.canceled -= OnAttackCanceled;

        playerInputActions.Player.Disable();
    }

    public void MovePerformed(InputAction.CallbackContext context)
    {
        if (!isWalking)
        {
            AudioManager.Instance.PlaySound("Walk");
            isWalking = true;
        }
        Vector2 input = context.ReadValue<Vector2>();
        movementVector.x = input.x;
        movementVector.y = input.y;
        movementVector *= speed;
        rb.velocity = movementVector;

        animator.SetFloat("LastXinput", movementVector.x);
        animator.SetFloat("LastYinput", movementVector.y);
        animator.SetFloat("Xinput", movementVector.x);
        animator.SetFloat("Yinput", movementVector.y);
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        isWalking = false;

        AudioManager.Instance.StopSound("Walk");

        movementVector = Vector2.zero;
        rb.velocity = movementVector;

        animator.SetFloat("Xinput", 0);
        animator.SetFloat("Yinput", 0);
    }

    void FixedUpdate()
    {
        rb.velocity = movementVector;
    }

    private void OnAttackStarted(InputAction.CallbackContext context)
    {
        isHoldingAttack = true;

        StartCoroutine(AttackWhileHolding());
    }

    private void OnAttackCanceled(InputAction.CallbackContext context)
    {
        isHoldingAttack = false;
    }

    private IEnumerator AttackWhileHolding()
    {
        while (isHoldingAttack)
        {
            if (!isAttacking)
            {
                PerformAttack();
                yield return new WaitForSeconds(0.4f);
            }
            yield return null;
        }
    }

    private void PerformAttack()
    {
        AudioManager.Instance.PlaySound("Sword");

        isAttacking = true;

        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePosition.z = 0;
        Vector2 attackDirection = (mousePosition - transform.position).normalized;
        Vector3 spherePosition = transform.position + (Vector3)attackDirection * 1.3f ;

        GameObject sphere = Instantiate(attackSpherePrefab, spherePosition, Quaternion.identity);       
        sphere.transform.localScale = Vector3.one * 1.5f * range;
        Destroy(sphere, 0.3f);
        Collider2D[] hitObjects = Physics2D.OverlapCircleAll(spherePosition, damageRadius * range);
        foreach (Collider2D collider in hitObjects)
        {
            IDamageable target = collider.GetComponent<IDamageable>();
            if (target != null)
            {

                target.TakeDamage(damage);
                if (canFire)
                {
                    target.ApplyDamageOverTime(DamageOverTime.Fire, 5f, 1f); 
                }

                if (canPoison)
                {
                    target.ApplyDamageOverTime(DamageOverTime.Poison, 5f, 1f); 
                }
            }
        }

        animator.SetFloat("AttackXinput", attackDirection.x);
        animator.SetFloat("AttackYinput", attackDirection.y);
        animator.SetBool("IsAttacking", true);

        StartCoroutine(AttackCooldown());
    }


    private IEnumerator AttackCooldown()
    {
        yield return new WaitForSeconds(0.5f);
        animator.SetBool("IsAttacking", false);
        yield return new WaitForSeconds(attackSpeed-0.5f);

        isAttacking = false;
    }
    public void ApplyItemStats(ItemData itemData)
    {
        if (itemData == null) return;
        attackSpeedUI= attackSpeedUI + (itemData.attackSpeed* -1);
        health += itemData.health;
        damage += itemData.damage;
        speed += itemData.speed;
        attackSpeed += itemData.attackSpeed;
        range += itemData.range;
        canFire = itemData.canFire;
        canPoison = itemData.canPoison;

        damage = Mathf.Clamp(damage, 0.1f, 5f);
        speed = Mathf.Clamp(speed, 0.1f, 5f);
        attackSpeed = Mathf.Clamp(attackSpeed, 0.1f, 3f);
        range = Mathf.Clamp(range, 0.1f, 3f);

        if (itemData.health > 0)
        {
            IncreaseMaxHealth(itemData.health); 
        }

        Stats.Instance.UpdateStats(damage, speed, attackSpeedUI);

    }

    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        health -= damageAmount;

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        AudioManager.Instance.StopSound("MusicGame");
        AudioManager.Instance.StopSound("Walk");
        if (isDead) return;
        isDead = true;

        rb.velocity = Vector2.zero; 
        playerInputActions.Player.Disable();

        animator.SetTrigger("Death");

        SceneManager.LoadScene("MainMenu");

        StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(1f); 

        transform.position = respawnPoint;
        animator.SetTrigger("Respawn");

        yield return new WaitForSeconds(0.6f);
        animator.SetTrigger("Start");
        yield return new WaitForSeconds(0.5f);

        health = 10;
        isDead = false;
        playerInputActions.Player.Enable(); 
    }
    public void IncreaseMaxHealth(float heartAmount)
    {
        float healthToAdd = heartAmount * healthPerHeart;

        int newMaxHearts = Mathf.FloorToInt((currentHealth + healthPerHeart - 1) / healthPerHeart);

        maxHearts = Mathf.Max(maxHearts, newMaxHearts);

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHearts * healthPerHeart);

        UpdateHearts();
    }

    private void UpdateHearts()
    {
        while (heartImages.Count < maxHearts)
        {
            GameObject newHeart = Instantiate(heartImages[0].gameObject, heartImages[0].transform.parent);
            newHeart.transform.SetSiblingIndex(heartImages.Count);
            heartImages.Add(newHeart.GetComponent<Image>());
        }

        for (int i = 0; i < heartImages.Count; i++)
        {
            float heartHealth = currentHealth - (i * healthPerHeart);

            if (heartHealth >= 4)
                heartImages[i].sprite = fullHeart;
            else if (heartHealth == 3)
                heartImages[i].sprite = threeQuartersHeart;
            else if (heartHealth == 2)
                heartImages[i].sprite = halfHeart;
            else if (heartHealth == 1)
                heartImages[i].sprite = quarterHeart;
            else
                heartImages[i].sprite = emptyHeart; 

            heartImages[i].enabled = (i < maxHearts);
        }
    }




}
