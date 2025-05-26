using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossWalkingSound : MonoBehaviour
{
    public LayerMask PlayerMask;

    private bool isPlayerInside = false;
    private bool isSoundPlaying = false;

    private Vector2 lastPosition;

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (isPlayerInside)
        {
            if ((Vector2)transform.position != lastPosition)
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

            lastPosition = transform.position;
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
