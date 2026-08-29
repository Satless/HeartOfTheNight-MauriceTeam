using UnityEngine;

/// <summary>
/// Gắn lên gốc Enemy/Boss. Khi object bị Destroy trong lúc màn còn load thì cộng 1 kill.
/// Không đếm lúc unload scene (chết / qua màn).
/// </summary>
[DisallowMultipleComponent]
public class EnemyKillReporter : MonoBehaviour
{
    private bool _reported;

    private void OnDestroy()
    {
        if (_reported || !Application.isPlaying)
            return;

        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)
            return;

        _reported = true;
        LevelStatsTracker.NotifyEnemyKilled(gameObject);
    }
}
