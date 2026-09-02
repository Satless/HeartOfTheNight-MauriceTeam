using System.Collections;
using HeartOfTheNight.Hung;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HeartOfTheNight.UI
{
    /// <summary>
    /// AuthScene: splash → click hiện login.
    /// Guest: popup xác nhận. Google: Firebase OAuth (mở trình duyệt chọn Gmail).
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

        [Header("Google")]
        [SerializeField] private TMP_Text googleAccountLabel;

        [Header("Flow")]
        [Tooltip("Scene sau khi đăng nhập / chơi khách thành công")]
        [SerializeField] private string nextSceneName = "mainMenu";

        private const string NetworkRequiredMessage =
            "NO INTERNET.\n\n" +
            "GOOGLE SAVES ARE TIED TO YOUR GMAIL (CLOUD).\n" +
            "THIS PC CANNOT LOAD THEM WHILE OFFLINE.\n\n" +
            "PLAY AS GUEST = A SEPARATE LOCAL SAVE ON THIS DEVICE.\n" +
            "IT WILL NOT CONTINUE YOUR GOOGLE PROGRESS FROM ANOTHER PC.\n\n" +
            "RETRY WHEN YOU HAVE NETWORK TO SIGN IN WITH GOOGLE.";

        private const string GuestConfirmMessage =
            "PLAY WITHOUT A GOOGLE ACCOUNT?\n\n" +
            "PROGRESS STAYS ON THIS DEVICE ONLY.\n" +
            "THIS IS NOT YOUR GOOGLE CLOUD SAVE.\n\n" +
            "TO LOAD SAVES FROM ANOTHER PC, SIGN IN WITH GOOGLE (NEEDS NETWORK).";

        private bool waitingForClick = true;
        private bool googleCancelled;
        private Coroutine googleRoutine;
        private Coroutine guestRoutine;
        private Button googlePrimaryButton;
        private Button googleSecondaryButton;

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

        private void OnDestroy()
        {
            googleCancelled = true;
            StopAuthRoutines();
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

            googleCancelled = false;
            ShowGoogleWaitingPanel();
            StopAuthRoutines();
            googleRoutine = StartCoroutine(GoogleSignInRoutine());
        }

        public void OnGoogleRetry()
        {
            OnSignInWithGoogle();
        }

        public void OnGoogleCancel()
        {
            googleCancelled = true;
            StopAuthRoutines();
            if (DataManager.Instance != null)
                DataManager.Instance.CancelGoogleSignIn();
            SetActiveSafe(googleAuthPopup, false);
        }

        public void OnPlayAsGuest()
        {
            SetActiveSafe(googleAuthPopup, false);
            SetActiveSafe(networkPopup, false);
            ApplyPopupCopy(guestConfirmPopup, "warning", GuestConfirmMessage);
            ShowPopup(guestConfirmPopup);
        }

        public void OnGuestContinue()
        {
            StopAuthRoutines();
            guestRoutine = StartCoroutine(GuestContinueRoutine());
        }

        public void OnGuestSignInInstead()
        {
            SetActiveSafe(guestConfirmPopup, false);
        }

        public void ShowNetworkPopup()
        {
            SetActiveSafe(googleAuthPopup, false);
            SetActiveSafe(guestConfirmPopup, false);
            ApplyPopupCopy(networkPopup, "network", NetworkRequiredMessage);
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
            googleCancelled = true;
            StopAuthRoutines();
            if (DataManager.Instance != null)
                DataManager.Instance.CancelGoogleSignIn();
            HideAllPopups();
        }

        private IEnumerator GoogleSignInRoutine()
        {
            var dataManager = DataManager.EnsureExists();
            float timeout = 25f;
            while (dataManager.IsFirebaseInitializing && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            if (googleCancelled)
                yield break;

            if (!dataManager.IsFirebaseReady && !dataManager.IsFirebaseInitializing)
            {
                ShowGoogleError("Firebase is not ready. Check the Console, then retry.");
                yield break;
            }

            bool done = false;
            bool ok = false;
            string message = "";
            dataManager.SignInWithGoogle((success, result) =>
            {
                ok = success;
                message = result ?? "";
                done = true;
            });

            float wait = 180f;
            while (!done)
            {
                if (googleCancelled)
                    yield break;
                wait -= Time.unscaledDeltaTime;
                if (wait <= 0f)
                {
                    if (DataManager.Instance != null)
                        DataManager.Instance.CancelGoogleSignIn();
                    ShowGoogleError("Timed out waiting for Google.\nFinish sign-in in the browser, or retry.");
                    yield break;
                }
                yield return null;
            }

            if (googleCancelled || this == null)
                yield break;

            if (!ok)
            {
                ShowGoogleError(FormatGoogleError(message));
                yield break;
            }

            AuthSession.SignInWithGoogle(message);
            GoNext();
        }

        private IEnumerator GuestContinueRoutine()
        {
            AuthSession.SignInAsGuest();
            var dataManager = DataManager.EnsureExists();
            if (dataManager != null)
                dataManager.SignOutFirebase();
            GoNext();
            yield break;
        }

        private void ShowGoogleWaitingPanel()
        {
            EnsureGoogleAuthPopup();
            SetActiveSafe(guestConfirmPopup, false);
            SetActiveSafe(networkPopup, false);
            SetGoogleLabel(
                "SIGN IN WITH GOOGLE\n\n" +
                "A browser window will open.\n" +
                "Choose your Google account, then return to Unity.\n\n" +
                "Waiting for Google...");
            SetGooglePrimary("WAITING...", false);
            if (googleSecondaryButton != null)
                googleSecondaryButton.interactable = true;
            SetNamedText(googleAuthPopup.transform, "Btn_Secondary", "CANCEL");
            ShowPopup(googleAuthPopup);
        }

        private void ShowGoogleError(string error)
        {
            EnsureGoogleAuthPopup();
            SetGoogleLabel("SIGN IN WITH GOOGLE\n\n" + error);
            SetGooglePrimary("RETRY", true);
            SetNamedText(googleAuthPopup.transform, "Btn_Secondary", "CANCEL");
            ShowPopup(googleAuthPopup);
        }

        private void SetGooglePrimary(string text, bool interactable)
        {
            SetNamedText(googleAuthPopup.transform, "Btn_Primary", text);
            if (googlePrimaryButton != null)
                googlePrimaryButton.interactable = interactable;
        }

        private void SetGoogleLabel(string text)
        {
            if (googleAccountLabel != null)
                googleAccountLabel.text = text;
        }

        private static string FormatGoogleError(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "Google sign-in failed. Retry?";

            string lower = raw.ToLowerInvariant();
            if (lower.Contains("operation_not_allowed") ||
                lower.Contains("not enabled") ||
                lower.Contains("disabled"))
            {
                return "Google provider is disabled.\n\n" +
                       "Firebase Console → Authentication →\n" +
                       "Sign-in method → Google → Enable.\n\n" +
                       raw;
            }

            if (lower.Contains("missing google web client") ||
                (lower.Contains("client id") && lower.Contains("secret")))
            {
                return raw;
            }

            if (lower.Contains("redirect_uri"))
            {
                return "Redirect URI mismatch.\n\n" +
                       "Google Cloud Console → Credentials → Web client\n" +
                       "Authorized redirect URIs must include:\n" +
                       "http://localhost:53421/\n\n" +
                       raw;
            }

            if (lower.Contains("not supported on non-mobile"))
            {
                return "Desktop Google sign-in is not using SignInWithProvider anymore.\nRetry after Unity recompiles.";
            }

            return raw + "\n\nRetry after finishing Google sign-in in the browser.";
        }

        private void EnsureGoogleAuthPopup()
        {
            if (googleAuthPopup != null)
            {
                CacheGoogleButtons();
                return;
            }

            if (guestConfirmPopup == null)
            {
                Debug.LogWarning("[AuthLoginUI] Thiếu guestConfirmPopup nên không clone được panel Google.");
                return;
            }

            googleAuthPopup = Instantiate(guestConfirmPopup, guestConfirmPopup.transform.parent);
            googleAuthPopup.name = "Popup_GoogleAuth";

            SetNamedText(googleAuthPopup.transform, "Title", "google");
            googleAccountLabel = FindNamed<TMP_Text>(googleAuthPopup.transform, "Txt_Message");

            SetNamedText(googleAuthPopup.transform, "Btn_Primary", "WAITING...");
            SetNamedText(googleAuthPopup.transform, "Btn_Secondary", "CANCEL");

            googlePrimaryButton = FindNamed<Button>(googleAuthPopup.transform, "Btn_Primary");
            googleSecondaryButton = FindNamed<Button>(googleAuthPopup.transform, "Btn_Secondary");
            BindButton(googlePrimaryButton, OnGoogleRetry);
            BindButton(googleSecondaryButton, OnGoogleCancel);
            BindButton(FindNamed<Button>(googleAuthPopup.transform, "Btn_Close"), OnClosePopup);
        }

        private void CacheGoogleButtons()
        {
            if (googlePrimaryButton == null)
                googlePrimaryButton = FindNamed<Button>(googleAuthPopup.transform, "Btn_Primary");
            if (googleSecondaryButton == null)
                googleSecondaryButton = FindNamed<Button>(googleAuthPopup.transform, "Btn_Secondary");
        }

        private void StopAuthRoutines()
        {
            if (googleRoutine != null)
            {
                StopCoroutine(googleRoutine);
                googleRoutine = null;
            }

            if (guestRoutine != null)
            {
                StopCoroutine(guestRoutine);
                guestRoutine = null;
            }
        }

        private void ShowPopup(GameObject popup)
        {
            if (popup == null)
                return;
            popup.transform.SetAsLastSibling();
            popup.SetActive(true);
        }

        private void ApplyPopupCopy(GameObject popup, string title, string message)
        {
            if (popup == null)
                return;

            SetNamedText(popup.transform, "Title", title);
            SetNamedText(popup.transform, "Txt_Message", message);
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
            if (root == null)
                return;

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
