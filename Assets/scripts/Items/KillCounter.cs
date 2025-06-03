using enemy;
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
        boomlingKills = Boomling.boomlingKillCounter;
        skeletonMeleeKills = Skeleton_Meele.skeletonKillCounter;
        zombieKills = Zombie.zombieKillCounter;

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
