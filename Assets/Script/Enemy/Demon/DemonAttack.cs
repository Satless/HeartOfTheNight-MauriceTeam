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
    // [SerializeField] private float cooldown = 3f;
    [SerializeField] private float warningDuration = 1.5f;
    [SerializeField] private float signDuration = 0.5f;

    private Vector3 lastPosition;
    private float lastAttackTime;
    // private bool isAttacking = false;

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
        // isAttacking = true;
        lastAttackTime = Time.time;

        // create warning ring
        GameObject warning = Instantiate(warningRingPrefab, player.position, Quaternion.identity);

        // follow the player
        float elapsed = 0f;
        while (elapsed < warningDuration)
        {
            warning.transform.position = player.position; //update the position to the player
            elapsed += Time.deltaTime;
            yield return null; //wait for next frame
        }
        Destroy(warning);

        //create danger sign
        Vector3 spawnPosition = player.position;
        GameObject sign = Instantiate(dangerSignPrefab, spawnPosition, Quaternion.identity);

        yield return new WaitForSeconds(signDuration); //indication duration

        Destroy(sign);

        // at the last position, create fire
        Instantiate(firePrefab, spawnPosition, Quaternion.identity);

        // isAttacking = false;
    }

    //attack range
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}