using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HeartOfTheNight.UI
{
    /// <summary>
    /// Điều khiển màn Auth (Login Google / Guest + popup confirm / network).
    /// Auth Firebase thật gắn sau — hiện chỉ UI + chuyển mainMenu.
    /// </summary>
    public class AuthLoginUI : MonoBehaviour
    {
        [Header("Screens")]
        [SerializeField] private GameObject loginRoot;
        [SerializeField] private GameObject guestConfirmPopup;
        [SerializeField] private GameObject networkPopup;

        [Header("Flow")]
        [Tooltip("Scene sau khi đăng nhập / chơi khách thành công")]
        [SerializeField] private string nextSceneName = "MenuDat";

        private void Start()
        {
            ShowLogin();
        }

        public void ShowLogin()
        {
            SetActiveSafe(loginRoot, true);
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

            Debug.Log("[AuthLoginUI] Google sign-in (placeholder) → next scene");
            GoNext();
        }

        public void OnPlayAsGuest()
        {
            SetActiveSafe(guestConfirmPopup, true);
        }

        public void OnGuestContinue()
        {
            Debug.Log("[AuthLoginUI] Play as guest → next scene");
            // TODO: giữ / kích hoạt anonymous Firebase (DataManager hiện đã anonymous)
            GoNext();
        }

        public void OnGuestSignInInstead()
        {
            SetActiveSafe(guestConfirmPopup, false);
            // Quay lại login — user bấm Google
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
