using UnityEngine;
using System; // Bắt buộc thêm using này

[RequireComponent(typeof(Animator))]
public class EnemySpawnerVFX : MonoBehaviour
{
    private GameObject prefabToSpawn;
    private Action<GameObject> onEnemySpawned; // Callback báo cáo

    // Hàm Init giờ nhận thêm 1 hành động (Callback)
    public void Init(GameObject enemyPrefab, Action<GameObject> callback = null)
    {
        prefabToSpawn = enemyPrefab;
        onEnemySpawned = callback;
    }

    // Vẫn gọi bằng Animation Event ở frame cuối
    public void SpawnActualEnemy()
    {
        if (prefabToSpawn != null)
        {
            GameObject realEnemy = Instantiate(prefabToSpawn, transform.position, Quaternion.identity);

            // Gọi callback để trả con quái thật về cho Room
            onEnemySpawned?.Invoke(realEnemy);
        }
        Destroy(gameObject);
    }
}