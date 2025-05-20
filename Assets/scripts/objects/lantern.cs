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
            circle.SetActive(true);
            Destroy(gameObject);
        }
    }
}
