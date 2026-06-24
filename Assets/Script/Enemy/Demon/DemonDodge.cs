using UnityEngine;

public class DemonDodge : MonoBehaviour
{
    [SerializeField] private Transform player; 
    [SerializeField] private float minDistance = 2.0f; 
    [SerializeField] private float dodgeSpeed = 3.0f; 

    private void Update()
    {
        if (player == null) return;

        // distance between demon and player
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // if distance is smaller than minimum
        if (distanceToPlayer < minDistance)
        {
            Dodge();
        }
    }

    private void Dodge()
    {
        // calculate the direction of player so that demon can move
        Vector2 direction = (transform.position - player.position).normalized;

        // move demon in that direction
        transform.position += (Vector3)direction * dodgeSpeed * Time.deltaTime;
    }
}