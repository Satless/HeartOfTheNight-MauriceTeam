using UnityEngine;

public class DemonDodge : MonoBehaviour
{
    [SerializeField] public Transform player; 
    [SerializeField] public float minDistance = 2.0f; 
    [SerializeField] public float dodgeSpeed = 3.0f; 

    private void Update()
    {
        if (player == null) return;

        // distance between demon and player
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // if distance is smaller than minimum
        if (distanceToPlayer < minDistance)
        {
            ExecuteDodge();
        }
    }

    public void ExecuteDodge()
    {
        // calculate the direction of player so that demon can move
        Vector2 direction = (transform.position - player.position).normalized;

        // move demon in that direction
        transform.position += (Vector3)direction * dodgeSpeed * Time.deltaTime;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(transform.position, minDistance);
    }
}