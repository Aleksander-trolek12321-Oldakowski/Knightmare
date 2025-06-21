using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnPortal : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();
        if (player != null && !string.IsNullOrEmpty(GameData.Instance.previousSceneName))
        {
            AudioManager.Instance.PlaySound("Teleport");

            GameData.Instance.SavePlayerData(player);
            SceneManager.LoadScene(GameData.Instance.previousSceneName);
        }
    }
}
