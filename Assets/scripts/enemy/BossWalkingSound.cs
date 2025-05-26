using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossWalkingSound : MonoBehaviour
{
    public LayerMask PlayerMask;
    public Rigidbody2D bossRigidbody;
    public float movementThreshold = 0.1f;

    private bool isPlayerInside = false;
    private bool isSoundPlaying = false;

    private void Update()
    {
        if (isPlayerInside)
        {
            if (bossRigidbody != null && bossRigidbody.velocity.magnitude > movementThreshold)
            {
                if (!isSoundPlaying)
                {
                    AudioManager.Instance.PlaySound("BossWalkingSound");
                    isSoundPlaying = true;
                }
            }
            else
            {
                if (isSoundPlaying)
                {
                    AudioManager.Instance.StopSound("BossWalkingSound");
                    isSoundPlaying = false;
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & PlayerMask) != 0)
        {
            isPlayerInside = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & PlayerMask) != 0)
        {
            isPlayerInside = false;
            if (isSoundPlaying)
            {
                AudioManager.Instance.StopSound("BossWalkingSound");
                isSoundPlaying = false;
            }
        }
    }
}
