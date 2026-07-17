using UnityEngine;

public class PlayerTest : MonoBehaviour
{
    private HealthPoint healthPoint;

    private void Awake()
    {
        healthPoint = GetComponent<HealthPoint>();
    }

    private void Update()
    {
        //this is just to test if health bar really works
        if (Input.GetKeyDown(KeyCode.E))
        {
            healthPoint.TakeDamage(10);
        }
    }
}
