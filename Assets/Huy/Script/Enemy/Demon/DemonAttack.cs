using UnityEngine;
using System.Collections;

public class DemonAttack : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject warningRingPrefab;
    [SerializeField] private GameObject dangerSignPrefab;
    [SerializeField] private GameObject firePrefab;
    [SerializeField] private Transform player;
    [SerializeField] public float attackRange = 7f;
    [SerializeField] private float cooldown = 3f;
    [SerializeField] private float warningDuration = 1.5f;
    [SerializeField] private float signDuration = 0.5f;

    private Vector3 lastPosition;
    private float lastAttackTime;
    private bool isAttacking = false;

    //private void Update()
    //{
    //    // if current position is the same as old position
    //    bool isStationary = Vector3.Distance(transform.position, lastPosition) < 0.01f;
    //    lastPosition = transform.position;

    //    // condition: stand still + no cooldown + enough range to prepare for attack
    //    if (isStationary && !isAttacking && Time.time >= lastAttackTime + cooldown)
    //    {
    //        float distance = Vector3.Distance(transform.position, player.position);
    //        if (distance <= attackRange)
    //        {
    //            StartCoroutine(AttackSequence());
    //        }
    //    }
    //}

    public IEnumerator AttackSequence()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // warning ring
        GameObject warning = Instantiate(warningRingPrefab, player.position, Quaternion.identity);

        float elapsed = 0f;
        while (elapsed < warningDuration)
        {
            if (warning != null) warning.transform.position = player.position;
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (warning != null) Destroy(warning);

        // warning ring
        Vector3 spawnPosition = player.position;
        GameObject sign = Instantiate(dangerSignPrefab, spawnPosition, Quaternion.identity);

        yield return new WaitForSeconds(signDuration);

        if (sign != null) Destroy(sign);

        // summon eye attack
        GameObject fireObj = Instantiate(firePrefab, spawnPosition, Quaternion.identity);

        // automatically calculate time to make eye attack disappear
        float waitTime = 1.0f;
        if (fireObj != null && fireObj.TryGetComponent<FireDestroy>(out FireDestroy destroyScript))
        {
            waitTime = 3.0f;
        }


        yield return new WaitForSeconds(waitTime);

        isAttacking = false;
    }

    //attack range
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}