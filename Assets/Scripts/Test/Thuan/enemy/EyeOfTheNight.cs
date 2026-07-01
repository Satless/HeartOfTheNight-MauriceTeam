using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeOfTheNight : MonoBehaviour
{
    [Header("hp")]
    public int hp = 150;

    [Header("thời gian khiên bật")] 
    public float shieldDuration = 5f;

    [Header("thời gian hồi chiêu")] 
    public float cooldown = 10f;

    private bool shieldActive;

    private List<Enemy> protectedEnemies =
        new List<Enemy>();

    [System.Obsolete]
    private void Start()
    {
        StartCoroutine(ShieldLoop());
    }

    [System.Obsolete]
    IEnumerator ShieldLoop()
    {
        while (true)
        {
            Debug.Log("Cooldown Start");

            yield return new WaitForSeconds(cooldown);

            ActivateShield();

            yield return new WaitUntil(
                () => shieldActive == false);
        }
    }

    [System.Obsolete]
    void ActivateShield()
    {
        Debug.Log("Shield Activated");

        shieldActive = true;

        protectedEnemies.Clear();

        Enemy[] enemies =
            FindObjectsOfType<Enemy>();

        foreach (Enemy enemy in enemies)
        {
            enemy.EnableShield();

            protectedEnemies.Add(enemy);
        }

        StartCoroutine(ShieldDuration());
    }

    IEnumerator ShieldDuration()
    {
        yield return new WaitForSeconds(
            shieldDuration);

        DeactivateShield();
    }

    void DeactivateShield()
    {
        Debug.Log("Shield End");

        shieldActive = false;

        foreach (Enemy enemy in protectedEnemies)
        {
            if (enemy != null)
            {
                enemy.DisableShield();
            }
        }

        protectedEnemies.Clear();
    }
}