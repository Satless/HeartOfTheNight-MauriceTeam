using UnityEngine;
using UnityEngine.UI;

public class Healthbar : MonoBehaviour
{
    [SerializeField] private HealthPoint playerHealth;
    [SerializeField] private Image totalhealthBar;
    [SerializeField] private Image currenthealthBar;

    private void Start()
    {
        totalhealthBar.fillAmount = 1;
    }
    private void Update()
    {
        currenthealthBar.fillAmount = playerHealth.currentHealth / 100f;
    }
}