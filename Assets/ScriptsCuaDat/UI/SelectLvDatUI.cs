using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeartOfTheNight.UI
{
    /// <summary>
    /// Select Level tạm (SelectLvDat) — sandbox, không đụng SelectLevel chung của nhóm.
    /// </summary>
    public class SelectLvDatUI : MonoBehaviour
    {
        [Header("Level scenes (đổi trong Inspector)")]
        [SerializeField] private string level1Scene = "DatScene";
        [SerializeField] private string level2Scene = "Khanh_Level1-1";
        [SerializeField] private string level3Scene = "Khanh_Level2-1";
        [SerializeField] private string level4Scene = "Khanh_Level3-1";

        [Header("Navigation")]
        [SerializeField] private string backSceneName = "MenuDat";

        public void LoadLevel1() => Load(level1Scene);
        public void LoadLevel2() => Load(level2Scene);
        public void LoadLevel3() => Load(level3Scene);
        public void LoadLevel4() => Load(level4Scene);

        public void OnBack()
        {
            if (string.IsNullOrEmpty(backSceneName))
            {
                Debug.LogWarning("[SelectLvDatUI] Chưa gán backSceneName.");
                return;
            }

            SceneManager.LoadScene(backSceneName);
        }

        private static void Load(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[SelectLvDatUI] Scene name trống.");
                return;
            }

            Debug.Log($"[SelectLvDatUI] Load → {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
    }
}
