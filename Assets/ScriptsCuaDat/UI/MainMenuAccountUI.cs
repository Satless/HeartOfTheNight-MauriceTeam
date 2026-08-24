using HeartOfTheNight.Hung;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        [SerializeField] private GameObject switchGoogleConfirmPopup;

        private void OnEnable()
        {
            RefreshAccountRow();
        }

        private void OnDisable()
        {
            SetActiveSafe(logoutConfirmPopup, false);
            SetActiveSafe(switchGoogleConfirmPopup, false);
            if (DataManager.Instance != null)
                DataManager.Instance.CancelSwitchToExistingGoogle();
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
            SetActiveSafe(logoutConfirmPopup, false);
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
                if (this == null)
                    return;

                if (!ok)
                {
                    if (emailOrError == DataManager.ExistingGoogleAccountNotice)
                    {
                        ShowSwitchGoogleConfirm();
                        return;
                    }

                    Debug.LogError("[MainMenuAccountUI] Link Google thất bại: " + emailOrError);
                    return;
                }

                AuthSession.SignInWithGoogle(emailOrError);
                RefreshAccountRow();
            });
        }

        public void OnSwitchGoogleYes()
        {
            SetActiveSafe(switchGoogleConfirmPopup, false);
            var dm = DataManager.EnsureExists();
            dm.ConfirmSwitchToExistingGoogle((ok, emailOrError) =>
            {
                if (this == null)
                    return;

                if (!ok)
                {
                    Debug.LogError("[MainMenuAccountUI] Switch Google thất bại: " + emailOrError);
                    return;
                }

                AuthSession.SignInWithGoogle(emailOrError);
                RefreshAccountRow();
            });
        }

        public void OnSwitchGoogleNo()
        {
            SetActiveSafe(switchGoogleConfirmPopup, false);
            if (DataManager.Instance != null)
                DataManager.Instance.CancelSwitchToExistingGoogle();
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

        private void ShowSwitchGoogleConfirm()
        {
            EnsureSwitchGooglePopup();
            if (switchGoogleConfirmPopup == null)
            {
                Debug.LogWarning("[MainMenuAccountUI] Thiếu popup xác nhận. Tự chuyển sang Google account.");
                OnSwitchGoogleYes();
                return;
            }

            switchGoogleConfirmPopup.transform.SetAsLastSibling();
            switchGoogleConfirmPopup.SetActive(true);
        }

        private void EnsureSwitchGooglePopup()
        {
            if (switchGoogleConfirmPopup != null)
                return;
            if (logoutConfirmPopup == null)
                return;

            switchGoogleConfirmPopup = Instantiate(logoutConfirmPopup, logoutConfirmPopup.transform.parent);
            switchGoogleConfirmPopup.name = "Popup_SwitchGoogleConfirm";

            SetNamedText(switchGoogleConfirmPopup.transform, "Title", "google");
            SetNamedText(
                switchGoogleConfirmPopup.transform,
                "Txt_Message",
                "THIS GOOGLE ACCOUNT ALREADY HAS SAVE DATA.\n\n" +
                "GUEST PROGRESS STAYS ON THIS DEVICE.\n" +
                "CONTINUE TO LOAD THAT ACCOUNT'S SLOTS?");
            SetNamedText(switchGoogleConfirmPopup.transform, "Btn_Yes", "▶  CONTINUE");
            SetNamedText(switchGoogleConfirmPopup.transform, "Btn_No", "STAY GUEST");

            BindButton(FindNamed<Button>(switchGoogleConfirmPopup.transform, "Btn_Yes"), OnSwitchGoogleYes);
            BindButton(FindNamed<Button>(switchGoogleConfirmPopup.transform, "Btn_No"), OnSwitchGoogleNo);
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private static void SetNamedText(Transform root, string objectName, string text)
        {
            var named = FindNamedTransform(root, objectName);
            if (named == null)
                return;

            var tmp = named.GetComponent<TMP_Text>();
            if (tmp == null)
                tmp = named.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
                tmp.text = text;
        }

        private static T FindNamed<T>(Transform root, string objectName) where T : Component
        {
            var named = FindNamedTransform(root, objectName);
            return named != null ? named.GetComponent<T>() : null;
        }

        private static Transform FindNamedTransform(Transform root, string objectName)
        {
            if (root == null)
                return null;

            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == objectName)
                    return all[i];
            }

            return null;
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null)
                go.SetActive(active);
        }
    }
}
