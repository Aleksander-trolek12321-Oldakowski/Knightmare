using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lantern : MonoBehaviour
{
    public GameObject circle;
    public LayerMask PlayerMask;

    private void OnTriggerEnter2D(Collider2D other)
    {
     
        if (((1 << other.gameObject.layer) & PlayerMask) != 0)
        {
            AudioManager.Instance.PlaySound("Candle");
            circle.SetActive(true);
           // Destroy(gameObject);
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & PlayerMask) != 0)
        {
            AudioManager.Instance.StopSound("Candle");
        }
      
    }
}
