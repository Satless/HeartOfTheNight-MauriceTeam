using UnityEngine;

/// <summary>
/// Gắn lên gốc Enemy/Boss. Khi object bị Destroy trong lúc màn còn chơi thì cộng 1 kill.
/// Không dùng scene.isLoaded: Unity 6 báo false cả khi Destroy từng con, nên kill luôn ra 0.
/// </summary>
[DisallowMultipleComponent]
public class EnemyKillReporter : MonoBehaviour
{
    private bool _reported;

    private void OnDestroy()
    {
        if (_reported || !Application.isPlaying)
            return;

        _reported = true;
        LevelStatsTracker.NotifyEnemyKilled(gameObject);
    }
}
