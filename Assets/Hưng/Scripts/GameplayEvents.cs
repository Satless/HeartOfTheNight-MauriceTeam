using System;

/// <summary>
/// Observer cho input gameplay. Pause / LevelComplete bắn; Player / HUD tự câm map.
/// Không FindObjects, không disable MonoBehaviour (giữ coroutine dash/trèo).
/// </summary>
public static class GameplayEvents
{
    public static bool InputEnabled { get; private set; } = true;

    public static event Action<bool> OnGameplayInputEnabled;

    public static void SetGameplayInputEnabled(bool enabled)
    {
        InputEnabled = enabled;
        OnGameplayInputEnabled?.Invoke(enabled);
    }
}
