using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeartOfTheNight.UI
{
    /// <summary>
    /// Main Menu tạm (MenuDat) — test flow Auth → Menu → Level, tránh đụng mainMenu chung.
    /// </summary>
    public class MenuDatUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainButtonsRoot;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject logoutConfirmPopup;

        [Header("Account (Settings)")]
        [SerializeField] private GameObject signOutButton;
        [SerializeField] private GameObject linkGoogleButton;
        [SerializeField] private TMPro.TMP_Text accountEmailText;
        [SerializeField] private TMPro.TMP_Text accountStatusText;

        [Header("Scenes")]
        [SerializeField] private string playSceneName = "SelectLvDat";
        [SerializeField] private string authSceneName = "AuthScene";

        [Header("Guest mode (test)")]
        [Tooltip("Bật = coi như Guest → hiện LINK GOOGLE thay SIGN OUT")]
        [SerializeField] private bool treatAsGuest = true;

        private void Start()
        {
            ShowMain();
            RefreshAccountRow();
        }

        public void ShowMain()
        {
            SetActiveSafe(mainButtonsRoot, true);
            SetActiveSafe(settingsPanel, false);
            SetActiveSafe(logoutConfirmPopup, false);
        }

        public void OnPlay()
        {
            if (string.IsNullOrEmpty(playSceneName))
            {
                Debug.LogWarning("[MenuDatUI] Chưa gán playSceneName.");
                return;
            }

            SceneManager.LoadScene(playSceneName);
        }

        public void OnOpenSettings()
        {
            SetActiveSafe(settingsPanel, true);
            RefreshAccountRow();
        }

        public void OnCloseSettings()
        {
            SetActiveSafe(settingsPanel, false);
            SetActiveSafe(logoutConfirmPopup, false);
        }

        public void OnQuit()
        {
            Application.Quit();
#if UNITY_EDITOR
            if (Application.isEditor)
                Debug.Log("[MenuDatUI] Quit — trong Editor chỉ dừng Play bằng nút Stop.");
#endif
        }

        public void OnSignOutClicked()
        {
            SetActiveSafe(logoutConfirmPopup, true);
        }

        public void OnLogoutYes()
        {
            Debug.Log("[MenuDatUI] Sign out (placeholder) → AuthScene");
            if (!string.IsNullOrEmpty(authSceneName))
                SceneManager.LoadScene(authSceneName);
        }

        public void OnLogoutNo()
        {
            SetActiveSafe(logoutConfirmPopup, false);
        }

        public void OnLinkGoogle()
        {
            Debug.Log("[MenuDatUI] Link Google (placeholder)");
            treatAsGuest = false;
            if (accountEmailText != null)
                accountEmailText.text = "player@gmail.com";
            if (accountStatusText != null)
                accountStatusText.text = "STATUS: ONLINE | SLOT SYNCED";
            RefreshAccountRow();
        }

        public void RefreshAccountRow()
        {
            if (treatAsGuest)
            {
                if (accountEmailText != null)
                    accountEmailText.text = "GUEST (LOCAL SAVE)";
                if (accountStatusText != null)
                    accountStatusText.text = "STATUS: OFFLINE ACCOUNT";
            }

            SetActiveSafe(signOutButton, !treatAsGuest);
            SetActiveSafe(linkGoogleButton, treatAsGuest);
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null)
                go.SetActive(active);
        }
    }
}
