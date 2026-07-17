using UnityEngine;

public class DemonDodge : MonoBehaviour
{
    [SerializeField] public Transform player; 
    [SerializeField] public float minDistance = 2.0f;
    [SerializeField] public float dodgeSpeed = 3.0f;
    [SerializeField] public LayerMask wallLayer;

    private DemonController controller;

    private void Awake()
    {
        controller = GetComponent<DemonController>();
    }
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

    public bool ExecuteDodge()
    {
        // if attack then do NOT dodge
        if (controller.currentState == DemonController.DemonState.Attacking) return false;

        Vector2 direction = (transform.position - player.position).normalized;
        Vector2 targetPos = (Vector2)transform.position + direction * dodgeSpeed * Time.deltaTime;

        if (!Physics2D.OverlapCircle(targetPos, 0.5f, wallLayer))
        {
            transform.position = targetPos;
            return true;
        }
        return false; // wall block
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(transform.position, minDistance);
    }
}