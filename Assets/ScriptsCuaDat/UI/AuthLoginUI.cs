using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeartOfTheNight.UI
{
    /// <summary>
    /// Màn Auth: splash (background + logo) → bấm bất kỳ hiện bảng login.
    /// Giống flow MainMenu của Thuận (click Background → OpenMenu).
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
            SetActiveSafe(guestConfirmPopup, false);
            SetActiveSafe(networkPopup, false);
        }

        public void OnSignInWithGoogle()
        {
            // TODO: Firebase Google + kiểm tra mạng
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                ShowNetworkPopup();
                return;
            }

            Debug.Log("[AuthLoginUI] Google sign-in (placeholder) → mainMenu");
            AuthSession.SignInWithGoogle();
            GoNext();
        }

        public void OnPlayAsGuest()
        {
            SetActiveSafe(guestConfirmPopup, true);
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
            SetActiveSafe(networkPopup, true);
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
            SetActiveSafe(guestConfirmPopup, false);
            SetActiveSafe(networkPopup, false);
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

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null)
                go.SetActive(active);
        }
    }
}
