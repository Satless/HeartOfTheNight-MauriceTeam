using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("HP")]
    public int hp = 100;

    [Header("Shield")]
    public GameObject shieldPrefab;

    private GameObject shieldObject;

    public bool isProtected;

    public void EnableShield()
    {
        isProtected = true;

        if (shieldObject == null)
        {
            shieldObject = Instantiate(
                shieldPrefab,
                transform);
        }

        shieldObject.transform.localPosition = Vector3.zero;
    }

    public void DisableShield()
    {
        isProtected = false;

        if (shieldObject != null)
        {
            Destroy(shieldObject);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isProtected)
        {
            Debug.Log(gameObject.name + " đang được bảo vệ");
            return;
        }

        hp -= damage;

        Debug.Log(gameObject.name + " mất " + damage);

        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }
}