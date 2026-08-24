using HeartOfTheNight.Hung;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeartOfTheNight.UI
{
    /// <summary>
    /// Thông tin tài khoản trên AccountPanel của mainMenu (Thuận).
    /// Gắn sẵn trong scene — chỉnh layout trong Editor.
    /// </summary>
    public class MainMenuAccountUI : MonoBehaviour
    {
        [SerializeField] private string authSceneName = "AuthScene";
        [SerializeField] private TMP_Text accountEmailText;
        [SerializeField] private TMP_Text accountStatusText;
        [SerializeField] private GameObject signOutButton;
        [SerializeField] private GameObject linkGoogleButton;
        [SerializeField] private GameObject logoutConfirmPopup;

        private void OnEnable()
        {
            RefreshAccountRow();
        }

        private void OnDisable()
        {
            if (logoutConfirmPopup != null)
                logoutConfirmPopup.SetActive(false);
        }

        public void OnSignOutClicked()
        {
            if (logoutConfirmPopup == null)
                return;

            logoutConfirmPopup.transform.SetAsLastSibling();
            logoutConfirmPopup.SetActive(true);
        }

        public void OnLogoutYes()
        {
            AuthSession.SignOut();
            if (DataManager.Instance != null)
                DataManager.Instance.SignOutFirebase();
            if (!string.IsNullOrEmpty(authSceneName))
                SceneManager.LoadScene(authSceneName);
        }

        public void OnLogoutNo()
        {
            if (logoutConfirmPopup != null)
                logoutConfirmPopup.SetActive(false);
        }

        public void OnLinkGoogle()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Debug.LogWarning("[MainMenuAccountUI] Cần mạng để liên kết Google.");
                return;
            }

            DataManager.EnsureExists().LinkGoogleAccount((ok, emailOrError) =>
            {
                if (!ok)
                {
                    Debug.LogError("[MainMenuAccountUI] Link Google thất bại: " + emailOrError);
                    return;
                }

                AuthSession.SignInWithGoogle(emailOrError);
                RefreshAccountRow();
            });
        }

        public void RefreshAccountRow()
        {
            if (accountEmailText != null)
                accountEmailText.text = AuthSession.Email;
            if (accountStatusText != null)
                accountStatusText.text = AuthSession.Status;

            SetActiveSafe(signOutButton, !AuthSession.IsGuest);
            SetActiveSafe(linkGoogleButton, AuthSession.IsGuest);
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null)
                go.SetActive(active);
        }
    }
}
