using UnityEngine;
using System.Collections;

public class DemonController : MonoBehaviour
{
    public enum DemonState { Idle, Attacking, Dodging }
    public DemonState currentState = DemonState.Idle;

    [SerializeField] private float idleDuration = 1.0f; 
    private float idleTimer;

    private DemonAttack demonAttack;
    private DemonDodge demonDodge;

    private bool isStuck = false;

    private void Awake()
    {
        demonAttack = GetComponent<DemonAttack>();
        demonDodge = GetComponent<DemonDodge>();
    }

    private void Update()
    {
        float dist = Vector2.Distance(transform.position, demonDodge.player.position);

        switch (currentState)
        {
            case DemonState.Attacking:
                // if attack, do not swith to idle or dodge state
                return;

            case DemonState.Dodging:
                // only dodge when not attack
                bool moved = demonDodge.ExecuteDodge();

                // condition to dodge: 
                // 1. Player has gotten far away
                // 2. OR get stuck at wall
                if (dist >= demonDodge.minDistance + 1.0f)
                {
                    isStuck = false; // reset stuck if player got far away
                    currentState = DemonState.Idle;
                    idleTimer = 0;
                }
                else if (!moved)
                {
                    // if stuck but player doesnt get far, temporarily stay idle
                }
                break;

            case DemonState.Idle:
                // dodge more than attack
                if (dist < demonDodge.minDistance && !isStuck)
                {
                    currentState = DemonState.Dodging;
                }
                else
                {
                    // only count time when NOT in dodge hitbox
                    idleTimer += Time.deltaTime;
                    if (idleTimer >= idleDuration && dist < demonAttack.attackRange)
                    {
                        StartCoroutine(PerformAttack());
                    }
                }
                break;
        }
    }

    private IEnumerator PerformAttack()
    {
        currentState = DemonState.Attacking;
        yield return StartCoroutine(demonAttack.AttackSequence());

        currentState = DemonState.Idle;
        idleTimer = 0;
    }

    
}