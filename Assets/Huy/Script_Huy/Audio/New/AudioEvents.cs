using System;
using UnityEngine;

public static class AudioEvents
{
    // Event 2D & 3D hỗ trợ 3 Tầng
    public static event Action<string, string, string> OnPlaySound2D;
    public static event Action<string, string, string, Vector3> OnPlaySound3D;

    // Event cho Music
    public static event Action<string, float> OnPlayMusic;

    // Phát âm thanh 2D (Ví dụ: UI)
    public static void TriggerSound2D(string categoryID, string subCategoryID, string actionName)
        => OnPlaySound2D?.Invoke(categoryID, subCategoryID, actionName);

    // Phát âm thanh 3D tại vị trí World Space
    public static void TriggerSound3D(string categoryID, string subCategoryID, string actionName, Vector3 position)
        => OnPlaySound3D?.Invoke(categoryID, subCategoryID, actionName, position);

    // Phát nhạc nền
    public static void TriggerMusic(string trackName, float fadeDuration = 0.5f)
        => OnPlayMusic?.Invoke(trackName, fadeDuration);
}