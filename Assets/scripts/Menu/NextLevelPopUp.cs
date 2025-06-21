using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class NextLevelPopUp : MonoBehaviour
{
    [SerializeField] private GameObject nextLevelPortal;
    private Player player;

  
    public void OnEnable()
    {
        EventSystem.current.SetSelectedGameObject(null);

    }
    public void Yes()
    {
        AudioManager.Instance.PlaySound("Teleport");

        SceneManager.LoadScene("level 2");

    }
    public void No()
    {
        nextLevelPortal.SetActive(false);

    }

    public void Menu()
    {
        AudioManager.Instance.StopSound("BossMusicAfterKill");

        SceneManager.LoadScene("MainMenu");

    }


}
