using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HeartOfTheNight.UI
{
    /// <summary>
    /// AuthScene: splash → click hiện login.
    /// Guest: popup xác nhận. Google: panel chọn tài khoản (mock, Firebase Google gắn sau).
    /// </summary>
    public class AuthLoginUI : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] private GameObject loginRoot;
        [Tooltip("Bảng login (Window_Login). Để trống thì dùng loginRoot.")]
        [SerializeField] private GameObject loginPanel;
        [Tooltip("Chữ ACCOUNT phía trên bảng — ẩn lúc splash.")]
        [SerializeField] private GameObject loginTitle;
        [SerializeField] private GameObject guestConfirmPopup;
        [SerializeField] private GameObject networkPopup;
        [SerializeField] private GameObject googleAuthPopup;

        [Header("Google mock")]
        [SerializeField] private string mockGoogleEmail = "player@gmail.com";
        [SerializeField] private TMP_InputField googleEmailInput;
        [SerializeField] private TMP_Text googleAccountLabel;

        [Header("Flow")]
        [Tooltip("Scene sau khi đăng nhập / chơi khách thành công")]
        [SerializeField] private string nextSceneName = "mainMenu";

        private bool waitingForClick = true;

        private void Start()
        {
            SetActiveSafe(loginRoot, true);
            SetActiveSafe(guestConfirmPopup, false);
            SetActiveSafe(networkPopup, false);
            SetActiveSafe(LoginWindow, false);
            SetActiveSafe(loginTitle, false);
            EnsureGoogleAuthPopup();
            SetActiveSafe(googleAuthPopup, false);
            waitingForClick = true;
        }

        private void Update()
        {
            if (!waitingForClick)
                return;

            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.anyKeyDown)
                ShowLogin();
        }

        private GameObject LoginWindow => loginPanel != null ? loginPanel : loginRoot;

        public void ShowLogin()
        {
            waitingForClick = false;
            SetActiveSafe(loginRoot, true);
            SetActiveSafe(LoginWindow, true);
            SetActiveSafe(loginTitle, true);
            HideAllPopups();
        }

        public void OnSignInWithGoogle()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                ShowNetworkPopup();
                return;
            }

            ShowGoogleAuthPopup();
        }

        public void OnGoogleContinue()
        {
            string email = ReadGoogleEmail();
            Debug.Log($"[AuthLoginUI] Google mock sign-in → {email}");
            AuthSession.SignInWithGoogle(email);
            GoNext();
        }

        public void OnGoogleCancel()
        {
            SetActiveSafe(googleAuthPopup, false);
        }

        public void OnPlayAsGuest()
        {
            SetActiveSafe(googleAuthPopup, false);
            SetActiveSafe(networkPopup, false);
            ShowPopup(guestConfirmPopup);
        }

        public void OnGuestContinue()
        {
            Debug.Log("[AuthLoginUI] Play as guest → mainMenu");
            AuthSession.SignInAsGuest();
            GoNext();
        }

        public void OnGuestSignInInstead()
        {
            SetActiveSafe(guestConfirmPopup, false);
        }

        public void ShowNetworkPopup()
        {
            SetActiveSafe(googleAuthPopup, false);
            SetActiveSafe(guestConfirmPopup, false);
            ShowPopup(networkPopup);
        }

        public void OnNetworkRetry()
        {
            SetActiveSafe(networkPopup, false);
            OnSignInWithGoogle();
        }

        public void OnNetworkPlayAsGuest()
        {
            SetActiveSafe(networkPopup, false);
            OnPlayAsGuest();
        }

        public void OnClosePopup()
        {
            HideAllPopups();
        }

        private void ShowGoogleAuthPopup()
        {
            EnsureGoogleAuthPopup();
            SetActiveSafe(guestConfirmPopup, false);
            SetActiveSafe(networkPopup, false);

            if (googleEmailInput != null)
                googleEmailInput.text = mockGoogleEmail;

            RefreshGoogleAccountLabel();
            ShowPopup(googleAuthPopup);
        }

        private void RefreshGoogleAccountLabel()
        {
            if (googleAccountLabel == null)
                return;

            googleAccountLabel.text =
                "SIGN IN WITH GOOGLE\n\n" +
                "Continue as\n" +
                mockGoogleEmail +
                "\n\nMock sign-in for testing. Real Google Auth will replace this panel.";
        }

        private string ReadGoogleEmail()
        {
            if (googleEmailInput != null && !string.IsNullOrWhiteSpace(googleEmailInput.text))
                return googleEmailInput.text.Trim();
            return string.IsNullOrWhiteSpace(mockGoogleEmail) ? "player@gmail.com" : mockGoogleEmail;
        }

        private void EnsureGoogleAuthPopup()
        {
            if (googleAuthPopup != null)
                return;
            if (guestConfirmPopup == null)
            {
                Debug.LogWarning("[AuthLoginUI] Thiếu guestConfirmPopup nên không clone được panel Google.");
                return;
            }

            googleAuthPopup = Instantiate(guestConfirmPopup, guestConfirmPopup.transform.parent);
            googleAuthPopup.name = "Popup_GoogleAuth";

            SetNamedText(googleAuthPopup.transform, "Title", "google");
            googleAccountLabel = FindNamed<TMP_Text>(googleAuthPopup.transform, "Txt_Message");
            RefreshGoogleAccountLabel();

            SetNamedText(googleAuthPopup.transform, "Btn_Primary", "▶  CONTINUE");
            SetNamedText(googleAuthPopup.transform, "Btn_Secondary", "CANCEL");

            BindButton(FindNamed<Button>(googleAuthPopup.transform, "Btn_Primary"), OnGoogleContinue);
            BindButton(FindNamed<Button>(googleAuthPopup.transform, "Btn_Secondary"), OnGoogleCancel);
            BindButton(FindNamed<Button>(googleAuthPopup.transform, "Btn_Close"), OnClosePopup);
        }

        private void ShowPopup(GameObject popup)
        {
            if (popup == null)
                return;
            popup.transform.SetAsLastSibling();
            popup.SetActive(true);
        }

        private void HideAllPopups()
        {
            SetActiveSafe(guestConfirmPopup, false);
            SetActiveSafe(networkPopup, false);
            SetActiveSafe(googleAuthPopup, false);
        }

        private void GoNext()
        {
            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogWarning("[AuthLoginUI] Chưa gán nextSceneName.");
                return;
            }

            SceneManager.LoadScene(nextSceneName);
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
