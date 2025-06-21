using UnityEngine.SceneManagement;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private string[] possibleScenesToLoad;
    [SerializeField] private string portalID;
    [SerializeField] private bool portalToNextLevel;
    [SerializeField] private GameObject nextLevelPortal;

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
            if (portalToNextLevel)
            {
                if (portalToNextLevel)
                {
                    if (nextLevelPortal != null)
                    {
                        nextLevelPortal.SetActive(true);
                    }
                }
          
            }
            else
            {
                AudioManager.Instance.PlaySound("Teleport");

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
    private void OnTriggerExit2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();
        if (player != null)
        {
            if (portalToNextLevel)
            {
                if (portalToNextLevel)
                {
                    if (nextLevelPortal != null)
                    {
                        nextLevelPortal.SetActive(false);
                    }
                }

            }
        }
   }
    public void SpawnPortalToNextLevel(string[] scenes)
    {
        possibleScenesToLoad = scenes;
        portalID = scenes[0];
    }
}
