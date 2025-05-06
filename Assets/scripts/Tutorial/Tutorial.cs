using UnityEngine;

public class Tutorial : MonoBehaviour
{
    public GameObject targetObject;


    private void Start()
    {
        if (targetObject != null)
        {
            targetObject.SetActive(true); 
        }
   
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

        if (other.gameObject.layer == 3 && targetObject != null)
        {
            targetObject.SetActive(false); 
        }
    }
}
