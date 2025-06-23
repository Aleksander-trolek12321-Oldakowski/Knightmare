using enemySpace;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class KillCounter : MonoBehaviour
{
    public TextMeshProUGUI boomlingKillText;
    public TextMeshProUGUI skeletonMeleeKillText;
    public TextMeshProUGUI zombieKillText;

    private int boomlingKills;
    private int skeletonMeleeKills;
    private int zombieKills;

    void Start()
    {
        if (GameData.Instance != null)
        {
            boomlingKills        = GameData.Instance.boomlingKills;
            skeletonMeleeKills   = GameData.Instance.skeletonMeleeKills;
            zombieKills          = GameData.Instance.zombieKills;
        }
        else
        {
            boomlingKills        = Boomling.boomlingKillCounter;
            skeletonMeleeKills   = Skeleton_Meele.skeletonKillCounter;
            zombieKills          = Zombie.zombieKillCounter;
        }

        UpdateKillTexts();
    }

    void UpdateKillTexts()
    {
        if (boomlingKillText != null )
            boomlingKillText.text = boomlingKills + "";

        if (skeletonMeleeKillText != null)
            skeletonMeleeKillText.text = skeletonMeleeKills +"";

        if (zombieKillText != null)
            zombieKillText.text = zombieKills +"";
    }
}
