using UnityEngine.SceneManagement;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private string[] possibleScenesToLoad;
    [SerializeField] private string portalID;

    private void Start()
    {
        if (GameData.Instance.destroyedPortals.Contains(SceneManager.GetActiveScene().name + "_" + portalID))
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();
        if (player != null)
        {
            GameData.Instance.SavePlayerData(player);
            GameData.Instance.SaveSceneName(player);

            string uniqueID = SceneManager.GetActiveScene().name + "_" + portalID;
            if (!GameData.Instance.destroyedPortals.Contains(uniqueID))
            {
                GameData.Instance.destroyedPortals.Add(uniqueID);
            }

            Destroy(gameObject);

            string randomScene = possibleScenesToLoad[Random.Range(0, possibleScenesToLoad.Length)];
            SceneManager.LoadScene(randomScene);
        }
    }
}
