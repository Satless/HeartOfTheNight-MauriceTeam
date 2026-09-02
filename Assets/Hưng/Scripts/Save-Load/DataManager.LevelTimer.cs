using System;
using UnityEngine;

namespace HeartOfTheNight.Hung
{
    /// <summary>
    /// Đồng hồ bấm giờ riêng cho từng màn. Số giây nằm trong GameData.scenePlayTimes nên sống qua
    /// lần reload scene lúc hồi sinh: chết thì treo đồng hồ, hồi sinh chạy tiếp từ đúng chỗ đã dừng.
    /// </summary>
    public partial class DataManager
    {
        private static readonly string[] NonLevelScenes =
        {
            "mainMenu", SelectLevelScene, "AuthScene", "LevelComplete",
            "SceneStory", "stoty", "deadscreen", "MenuDat", "SelectLvDat",
            StoryFlow.Story1, StoryFlow.Story2, StoryFlow.Story3, StoryFlow.EndScene,
        };

        private ScenePlayTimeEntry _levelTimerEntry;
        private GameData _levelTimerOwner;
        private string _levelTimerScene = "";
        private bool _levelTimerPaused;
        private bool _keepLevelTimeOnNextLoad;

        /// <summary>Số giây đã chơi trong màn hiện tại. Menu / màn hình kết thúc trả 0.</summary>
        public float LevelTimeSeconds => _levelTimerEntry != null ? _levelTimerEntry.playSeconds : 0f;

        /// <summary>Đang ở trong một màn chơi nên đồng hồ có nghĩa (dùng để ẩn/hiện UI).</summary>
        public bool HasLevelTimer => _levelTimerEntry != null;

        public bool IsLevelTimerPaused => _levelTimerPaused;

        /// <summary>Treo đồng hồ. Gọi khi player chết — kể cả khi Time.timeScale vẫn đang chạy.</summary>
        public void PauseLevelTimer()
        {
            if (_levelTimerPaused) return;

            _levelTimerPaused = true;
            Debug.Log($"[LevelTimer] Dừng ở {FormatLevelTime(LevelTimeSeconds)} — {_levelTimerScene}");
        }

        /// <summary>Cho đồng hồ chạy tiếp từ số giây đã dừng.</summary>
        public void ResumeLevelTimer()
        {
            if (!_levelTimerPaused) return;

            _levelTimerPaused = false;
            Debug.Log($"[LevelTimer] Chạy tiếp từ {FormatLevelTime(LevelTimeSeconds)} — {_levelTimerScene}");
        }

        /// <summary>Hồi sinh / Continue phải giữ số giây; vào màn từ Select Level hay qua cửa thì đếm lại.</summary>
        private void KeepLevelTimeAcrossNextLoad() => _keepLevelTimeOnNextLoad = true;

        private void TickLevelTimer(string sceneName, float unscaledDelta)
        {
            if (!IsLevelScene(sceneName))
            {
                ClearLevelTimer();
                return;
            }

            // Data bị thay object (load slot / load cloud) thì reference cũ trỏ vào save đã bỏ.
            if (_levelTimerEntry == null || _levelTimerOwner != Data || _levelTimerScene != sceneName)
                BindLevelTimer(sceneName, keepExistingSeconds: true);

            if (_levelTimerPaused || _levelTimerEntry == null)
                return;

            _levelTimerEntry.playSeconds += unscaledDelta;
        }

        private void SyncLevelTimerToLoadedScene(string sceneName)
        {
            bool keep = _keepLevelTimeOnNextLoad;
            _keepLevelTimeOnNextLoad = false;

            if (!IsLevelScene(sceneName))
            {
                ClearLevelTimer();
                return;
            }

            // Hồi sinh load lại đúng scene cũ, nên phải bỏ treo tường minh chứ không thể dựa vào đổi scene.
            BindLevelTimer(sceneName, keepExistingSeconds: keep);
            ResumeLevelTimer();
        }

        private void BindLevelTimer(string sceneName, bool keepExistingSeconds)
        {
            if (Data == null) Data = new GameData();

            _levelTimerEntry = Data.GetOrCreateScenePlayTime(sceneName);
            _levelTimerOwner = Data;
            _levelTimerScene = sceneName;

            if (!keepExistingSeconds && _levelTimerEntry != null)
                _levelTimerEntry.playSeconds = 0f;
        }

        private void ClearLevelTimer()
        {
            _levelTimerEntry = null;
            _levelTimerOwner = null;
            _levelTimerScene = "";
            _levelTimerPaused = false;
        }

        public static bool IsLevelScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return false;

            if (StoryFlow.IsCinematic(sceneName))
                return false;

            for (int i = 0; i < NonLevelScenes.Length; i++)
            {
                if (string.Equals(sceneName, NonLevelScenes[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }

        /// <summary>Giây → "MM:SS", quá một tiếng thì "H:MM:SS".</summary>
        public static string FormatLevelTime(float seconds, bool forceHours = false)
        {
            if (float.IsNaN(seconds) || seconds < 0f)
                seconds = 0f;

            int total = Mathf.FloorToInt(seconds);
            int hours = total / 3600;
            int minutes = total % 3600 / 60;
            int secs = total % 60;

            return forceHours || hours > 0
                ? $"{hours}:{minutes:00}:{secs:00}"
                : $"{minutes:00}:{secs:00}";
        }
    }
}
