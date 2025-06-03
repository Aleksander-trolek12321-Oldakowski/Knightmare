using enemy;
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

    [SerializeField] private bool changeCameraSize = false;
    [SerializeField] private float newCameraSize = 4;
    
    [SerializeField] private bool canSlow = false;
    [SerializeField] private float slowDuration = 2f;
    [SerializeField] private float slowAmount = 0.5f;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private bool hasThorns = false;
    [SerializeField] private float thornsDamage = 2f;
    [SerializeField] public float thornsCooldown =2f;
    private float lastThornsTime =0;
    private ItemData currentEquippedItem;

    private Dictionary<GameObject, float> thornsTimers = new Dictionary<GameObject, float>();
    public float GetDamage() => damage;
    public float GetSpeed() => speed;
    public float GetAttackSpeed() => attackSpeed;
    public float GetRange() => range;
    public float GetCurrentHealth() => currentHealth;
    public int GetMaxHearts() => maxHearts;

    public bool GetCanPoison() => canPoison;
    public bool GetCanFire() => canFire;
    public bool GetCanSlow() => canSlow;
    public bool GetHasThorns() => hasThorns;
    public ItemData GetItem() => currentEquippedItem;

    void Awake()
    {
  

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
        string currentScene = SceneManager.GetActiveScene().name;

        AudioManager.Instance.StopSound("BossMusicAfterKill");
        AudioManager.Instance.StopSound("MusicMenu");
        AudioManager.Instance.StopPlaylist();
        AudioManager.Instance.StopSound("MusicSpecialRoom");


        switch (currentScene)
        {
            case "Blood room":
                AudioManager.Instance.PlaySound("MusicSpecialRoom");
                break; 
            case "trap room":
                AudioManager.Instance.PlaySound("MusicSpecialRoom");
                break;
            default:
                AudioManager.Instance.PlayPlaylist("MusicGame1", "MusicGame2");
                break;
        }
        if (GameData.Instance != null && SceneManager.GetActiveScene().name == GameData.Instance.previousSceneName)
        {
            transform.position = GameData.Instance.returnPosition;
        }
        currentHealth = maxHearts * healthPerHeart;

        if (GameData.Instance != null && GameData.Instance.playerHealth>0)
        {
            GameData.Instance.LoadPlayerData(this);

            foreach (Sprite icon in GameData.Instance.collectedItemIcons)
            {
                InventoryUI.Instance.AddItemToUI(icon);
            }
        }else
        {
            UpdateHearts();
            attackSpeedUI = attackSpeed;


            Stats.Instance.UpdateStats(damage, speed, attackSpeedUI);

        }

      

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

                if (canSlow)
                {
                    target.ApplySlow(slowAmount, slowDuration);
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
        currentEquippedItem = itemData;


        attackSpeedUI += (itemData.attackSpeed * -1);
        damage += itemData.damage;
        speed += itemData.speed;
        attackSpeed += itemData.attackSpeed;
        range += itemData.range;

        if (itemData.canFire)
        {
            canFire = itemData.canFire;
        }

        if (itemData.canPoison)
        {
            canPoison = itemData.canPoison;
        }

        if (itemData.canSlow)
        {
            canSlow = itemData.canSlow;
        }

        if (itemData.changeCameraSize)
        {
            mainCamera.orthographicSize = newCameraSize;
        }

        if (itemData.hasThorns)
        {
            hasThorns = true;
           
        }
        damage = Mathf.Clamp(damage, 0.1f, 5f);
        speed = Mathf.Clamp(speed, 0.1f, 5f);
        attackSpeed = Mathf.Clamp(attackSpeed, 0.1f, 3f);
        range = Mathf.Clamp(range, 0.1f, 3f);

        if (itemData.health != 0)
        {
            IncreaseMaxHealth(itemData.health); 
        }

        Stats.Instance.UpdateStats(damage, speed, attackSpeedUI);
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!hasThorns || isDead) return;

        GameObject enemyObject = collision.gameObject;
        IDamageable enemy = enemyObject.GetComponent<IDamageable>();
        if (enemy == null) return;

        float lastHitTime = thornsTimers.ContainsKey(enemyObject) ? thornsTimers[enemyObject] : -Mathf.Infinity;

        if (Time.time - lastHitTime >= thornsCooldown)
        {
            enemy.TakeDamage(thornsDamage);
            thornsTimers[enemyObject] = Time.time;
        }
    }


    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHearts * healthPerHeart);

        UpdateHearts(); 

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    [SerializeField] private GameObject gameOverScreen; // Dodaj to pole na górze klasy


    private void Die()
    {
        AudioManager.Instance.StopSound("MusicGame");
        AudioManager.Instance.StopSound("Walk");
        if (isDead) return;
        isDead = true;

        rb.velocity = Vector2.zero;
        playerInputActions.Player.Disable();

        animator.SetTrigger("Death");
     
    //    SceneManager.LoadScene("MainMenu");

       // StartCoroutine(Respawn());

        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);

        }
        Time.timeScale = 0f;
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(1f);

        transform.position = respawnPoint;
        animator.SetTrigger("Respawn");

        yield return new WaitForSeconds(0.6f);
        animator.SetTrigger("Start");
        yield return new WaitForSeconds(0.5f);

        currentHealth = maxHearts * healthPerHeart; 
        isDead = false;
        playerInputActions.Player.Enable();

        UpdateHearts(); 
    }

    public void IncreaseMaxHealth(float heartAmount)
    {
        float healthToAdd = heartAmount * healthPerHeart; 

        if (healthToAdd > 0)
        {
            currentHealth += healthToAdd;
            int newMaxHearts = Mathf.FloorToInt((currentHealth + healthPerHeart - 1) / healthPerHeart);
            maxHearts = Mathf.Max(maxHearts, newMaxHearts);
        }
        else
        {
            TakeDamage(-healthToAdd);
        }

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
    public void Heal(float healAmount)
    {
        if (isDead) return;

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHearts * healthPerHeart);
        UpdateHearts();
    }

    public void ApplyLoadedStats(float dmg, float spd, float atkSpd, float rng, float health, int hearts,
       bool poison, bool fire, bool slow, bool thorns, ItemData item)
    {
        damage = dmg;
        speed = spd;
        attackSpeed = atkSpd;
        range = rng;
        currentHealth = health;
        maxHearts = hearts;
        canPoison = poison;
        canFire = fire;
        canSlow = slow;
        hasThorns = thorns;
        currentEquippedItem = item;
        UpdateHearts();
        attackSpeedUI = (attackSpeed - 1);
        attackSpeedUI = (1 - attackSpeedUI);
        Stats.Instance.UpdateStats(damage, speed, attackSpeedUI);
    }

    public void ReplaceItem(ItemData newItem)
    {
        if (currentEquippedItem != null)
        {
            attackSpeedUI -= (currentEquippedItem.attackSpeed * -1);
            damage -= currentEquippedItem.damage;
            speed -= currentEquippedItem.speed;
            attackSpeed -= currentEquippedItem.attackSpeed;
            range -= currentEquippedItem.range;

            if (currentEquippedItem.canFire) canFire = false;
            if (currentEquippedItem.canPoison) canPoison = false;
            if (currentEquippedItem.canSlow) canSlow = false;
            if (currentEquippedItem.hasThorns) hasThorns = false;

            if (currentEquippedItem.health > 0)
            {
                maxHearts -= Mathf.RoundToInt(currentEquippedItem.health);
                currentHealth = Mathf.Min(currentHealth, maxHearts * 4);
            }
            UpdateHearts();

        }

    }


}
