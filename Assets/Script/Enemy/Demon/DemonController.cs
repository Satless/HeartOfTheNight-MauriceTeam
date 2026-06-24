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
            case DemonState.Idle:
                idleTimer += Time.deltaTime;
                // if player is in dodge distance, dodge
                if (dist < demonDodge.minDistance)
                {
                    currentState = DemonState.Dodging;
                }
                // if stand still in the range and long enough, attack
                else if (idleTimer >= idleDuration && dist < demonAttack.attackRange)
                {
                    StartCoroutine(PerformAttack());
                }
                break;

            case DemonState.Dodging:
                demonDodge.ExecuteDodge(); 
                // if player from dodge distance, return to idle
                if (dist > demonDodge.minDistance + 0.5f) // avoid shaking
                {
                    currentState = DemonState.Idle;
                    idleTimer = 0;
                }
                break;
        }
    }

    private IEnumerator PerformAttack()
    {
        currentState = DemonState.Attacking;
        yield return StartCoroutine(demonAttack.AttackSequence()); // call courptine for attack again
        currentState = DemonState.Idle;
        idleTimer = 0;
    }
}