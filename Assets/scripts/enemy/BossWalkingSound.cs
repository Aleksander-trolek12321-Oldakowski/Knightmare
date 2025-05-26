using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossWalkingSound : MonoBehaviour
{
    public LayerMask PlayerMask;

    private void OnTriggerEnter2D(Collider2D other)
    {

        if (((1 << other.gameObject.layer) & PlayerMask) != 0)
        {
            AudioManager.Instance.PlaySound("BossWalkingSound");
       
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & PlayerMask) != 0)
        {
            AudioManager.Instance.StopSound("BossWalkingSound");
        }

    }
}
