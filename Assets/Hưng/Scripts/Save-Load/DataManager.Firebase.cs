using System;
using System.Collections.Generic;
using System.IO;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

namespace HeartOfTheNight.Hung
{
    public partial class DataManager
    {
        private FirebaseAuth _auth;
        private FirebaseUser _user;
        private DatabaseReference _dbRef;
        private bool _isFirebaseReady;
        private bool _isFirebaseInitializing;
        private bool _googleAuthBusy;
        private int _googleFlowSerial;
        private readonly List<Action<bool, string>> _googleCallbacks = new List<Action<bool, string>>();
        private Credential _pendingExistingGoogleCredential;

        public bool IsFirebaseInitializing => _isFirebaseInitializing;
        public bool IsFirebaseReady => _isFirebaseReady;

        private void InitializeFirebase()
        {
            _isFirebaseInitializing = true;
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    _auth = FirebaseAuth.DefaultInstance;
                    _dbRef = FirebaseDatabase.DefaultInstance.RootReference;
                    _isFirebaseReady = true;
                    _isFirebaseInitializing = false;
                    RestorePersistedGoogleUser();
                }
                else
                {
                    _isFirebaseInitializing = false;
                    Debug.LogError($"[Firebase] Không thể khởi tạo Firebase: {task.Result}. Fallback sang Local Load.");
                    if (!ShouldPreserveLiveRamSave())
                        LoadGameLocal();
                }
            });
        }

        /// <summary>
        /// Chỉ khôi phục Google. Guest = local. Không tạo Anonymous (tránh mở RTDB nếu rules chỉ cần auth != null).
        /// </summary>
        private void RestorePersistedGoogleUser()
        {
            var current = _auth != null ? _auth.CurrentUser : null;
            if (current != null && current.IsAnonymous)
            {
                _auth.SignOut();
                current = null;
            }

            if (current != null && !current.IsAnonymous)
            {
                BindFirebaseUser(current);
                LogAuthDev("Restored Google session.");
                if (!ShouldPreserveLiveRamSave())
                    LoadGameCloud();
                return;
            }

            _user = null;
            if (!ShouldPreserveLiveRamSave())
                LoadGameLocal();
        }

        public void SignInWithGoogle(Action<bool, string> onComplete)
        {
            BeginGoogleAuth(preferLink: false, onComplete);
        }

        public void LinkGoogleAccount(Action<bool, string> onComplete)
        {
            if (_auth == null)
            {
                onComplete?.Invoke(false, "Firebase is not ready yet.");
                return;
            }

            var current = _auth.CurrentUser;
            if (current != null && !current.IsAnonymous)
            {
                BindFirebaseUser(current);
                onComplete?.Invoke(true, ResolveAccountLabel(current));
                return;
            }

            BeginGoogleAuth(preferLink: current != null && current.IsAnonymous, onComplete);
        }

        public void CancelGoogleSignIn()
        {
            _googleFlowSerial++;
            _googleAuthBusy = false;
            _googleCallbacks.Clear();
            _pendingExistingGoogleCredential = null;
            GoogleDesktopOAuth.Cancel();
        }

        public void ConfirmSwitchToExistingGoogle(Action<bool, string> onComplete)
        {
            var credential = _pendingExistingGoogleCredential;
            _pendingExistingGoogleCredential = null;
            if (credential == null || _auth == null)
            {
                onComplete?.Invoke(false, "No pending Google account switch.");
                return;
            }

            if (onComplete != null)
                _googleCallbacks.Add(onComplete);

            _googleAuthBusy = true;
            int serial = ++_googleFlowSerial;
            SignInWithCredentialOnly(credential, serial);
        }

        public void CancelSwitchToExistingGoogle()
        {
            _pendingExistingGoogleCredential = null;
        }

        public void EnsureAnonymousAuth(Action<bool, string> onComplete)
        {
            if (_auth == null)
            {
                onComplete?.Invoke(false, "Firebase is not ready yet.");
                return;
            }

            var current = _auth.CurrentUser;
            if (current != null && current.IsAnonymous)
            {
                BindFirebaseUser(current);
                onComplete?.Invoke(true, null);
                return;
            }

            if (current != null)
                _auth.SignOut();

            _auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled || task.IsFaulted)
                {
                    Debug.LogError("[Firebase] Anonymous sign-in failed: " + task.Exception);
                    onComplete?.Invoke(false, FormatAuthError(task.Exception));
                    return;
                }

                BindFirebaseUser(_auth.CurrentUser);
                if (!ShouldPreserveLiveRamSave())
                    LoadGameLocal();
                onComplete?.Invoke(true, null);
            });
        }

        public void SignOutFirebase()
        {
            CancelGoogleSignIn();
            _playTimeDirty = false;
            _playTimeSaveTimer = 0f;

            if (_auth != null)
                _auth.SignOut();

            BindFirebaseUser(null);
            // Bind no-ops if đã unsigned; vẫn bỏ RAM Google để SaveGameLocal không ghi vào folder guest.
            Data = new GameData { slotIndex = ActiveSlotIndex, hasSave = false };
            CompleteCloudSlotIndex();
            HeartOfTheNight.Rooms.PlayerKeyInventory.NotifyChanged();
            Debug.Log("[Firebase] Signed out.");
        }

        private void BeginGoogleAuth(bool preferLink, Action<bool, string> onComplete)
        {
            if (_auth == null)
            {
                onComplete?.Invoke(false, "Firebase is not ready yet.");
                return;
            }

            if (onComplete != null)
                _googleCallbacks.Add(onComplete);

            if (_googleAuthBusy)
                return;

            _googleAuthBusy = true;
            int serial = ++_googleFlowSerial;

#if UNITY_EDITOR || UNITY_STANDALONE
            StartDesktopGoogleOAuth(preferLink, serial);
#else
            StartMobileGoogleProvider(preferLink, serial);
#endif
        }

        private void StartDesktopGoogleOAuth(bool preferLink, int serial)
        {
            string clientId = ResolveGoogleWebClientId();
            string clientSecret = (googleWebClientSecret ?? "").Trim();
            int port = googleLoopbackPort > 0 ? googleLoopbackPort : GoogleDesktopOAuth.DefaultPort;

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                CompleteGoogleAuth(serial, false, GoogleDesktopOAuth.MissingConfigMessage(port));
                return;
            }

            GoogleDesktopOAuth.RequestIdToken(clientId, clientSecret, port, (ok, idToken, accessToken, error) =>
            {
                if (serial != _googleFlowSerial)
                    return;

                if (!ok)
                {
                    CompleteGoogleAuth(serial, false, string.IsNullOrEmpty(error) ? "Google sign-in failed." : error);
                    return;
                }

                Credential credential = GoogleAuthProvider.GetCredential(idToken, string.IsNullOrEmpty(accessToken) ? null : accessToken);
                SignInWithGoogleCredential(credential, preferLink, serial);
            });
        }

        private void StartMobileGoogleProvider(bool preferLink, int serial)
        {
            var provider = CreateGoogleProvider();
            var current = _auth.CurrentUser;
            if (preferLink && current != null && current.IsAnonymous)
            {
                current.LinkWithProviderAsync(provider).ContinueWithOnMainThread(task =>
                {
                    if (serial != _googleFlowSerial)
                        return;

                    _googleAuthBusy = false;
                    if (task.IsFaulted && IsGoogleCredentialInUse(task.Exception))
                    {
                        Debug.Log("[Firebase] Google đã gắn tài khoản khác — chuyển sang SignIn.");
                        _googleAuthBusy = true;
                        _auth.SignInWithProviderAsync(CreateGoogleProvider()).ContinueWithOnMainThread(signInTask =>
                        {
                            if (serial != _googleFlowSerial)
                                return;
                            _googleAuthBusy = false;
                            HandleGoogleAuthTask(signInTask);
                        });
                        return;
                    }

                    HandleGoogleAuthTask(task);
                });
                return;
            }

            _auth.SignInWithProviderAsync(provider).ContinueWithOnMainThread(task =>
            {
                if (serial != _googleFlowSerial)
                    return;
                _googleAuthBusy = false;
                HandleGoogleAuthTask(task);
            });
        }

        private void SignInWithGoogleCredential(Credential credential, bool preferLink, int serial)
        {
            var current = _auth.CurrentUser;
            if (preferLink && current != null && current.IsAnonymous)
            {
                current.LinkWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
                {
                    if (serial != _googleFlowSerial)
                        return;

                    if (task.IsFaulted && IsGoogleCredentialInUse(task.Exception))
                    {
                        _pendingExistingGoogleCredential = credential;
                        Debug.Log("[Firebase] Google account already has saves — chờ người chơi xác nhận.");
                        CompleteGoogleAuth(serial, false, ExistingGoogleAccountNotice);
                        return;
                    }

                    HandleCompletedAuthTask(task, serial);
                });
                return;
            }

            SignInWithCredentialOnly(credential, serial);
        }

        private void SignInWithCredentialOnly(Credential credential, int serial)
        {
            _auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
            {
                if (serial != _googleFlowSerial)
                    return;
                HandleCompletedAuthTask(task, serial);
            });
        }

        private void HandleCompletedAuthTask(System.Threading.Tasks.Task task, int serial)
        {
            if (task == null || task.IsCanceled)
            {
                CompleteGoogleAuth(serial, false, "Google sign-in was cancelled.");
                return;
            }

            if (task.IsFaulted)
            {
                Debug.LogError("[Firebase] Google sign-in failed: " + task.Exception);
                CompleteGoogleAuth(serial, false, FormatAuthError(task.Exception));
                return;
            }

            FirebaseUser user = _auth != null ? _auth.CurrentUser : null;
            if (user == null)
            {
                CompleteGoogleAuth(serial, false, "Google sign-in returned no user.");
                return;
            }

            BindFirebaseUser(user);
            LoadGameCloud();
            string label = ResolveAccountLabel(user);
            LogAuthDev("Google sign-in OK.");
            CompleteGoogleAuth(serial, true, label);
        }

        private void CompleteGoogleAuth(int serial, bool ok, string message)
        {
            if (serial != _googleFlowSerial)
                return;

            _googleAuthBusy = false;
            var callbacks = _googleCallbacks.ToArray();
            _googleCallbacks.Clear();
            for (int i = 0; i < callbacks.Length; i++)
                callbacks[i]?.Invoke(ok, message);
        }

        private string ResolveGoogleWebClientId()
        {
            if (!string.IsNullOrWhiteSpace(googleWebClientId))
                return googleWebClientId.Trim().Trim('"');

            return TryReadWebClientIdFromGoogleServices();
        }

        private static string TryReadWebClientIdFromGoogleServices()
        {
            string[] paths =
            {
                Path.Combine(Application.streamingAssetsPath, "google-services-desktop.json"),
                Path.Combine(Application.streamingAssetsPath, "google-services.json")
            };

            for (int i = 0; i < paths.Length; i++)
            {
                if (!File.Exists(paths[i]))
                    continue;

                try
                {
                    string json = File.ReadAllText(paths[i]);
                    int typeIndex = json.IndexOf("\"client_type\": 3", StringComparison.Ordinal);
                    if (typeIndex < 0)
                        typeIndex = json.IndexOf("\"client_type\":3", StringComparison.Ordinal);
                    if (typeIndex < 0)
                        continue;

                    int searchFrom = Math.Max(0, typeIndex - 400);
                    int idKey = json.IndexOf("\"client_id\"", searchFrom, StringComparison.Ordinal);
                    if (idKey < 0 || idKey > typeIndex)
                        continue;

                    int firstQuote = json.IndexOf('"', idKey + 12);
                    int secondQuote = firstQuote >= 0 ? json.IndexOf('"', firstQuote + 1) : -1;
                    if (firstQuote < 0 || secondQuote < 0)
                        continue;

                    string id = json.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                    if (id.EndsWith(".apps.googleusercontent.com"))
                        return id;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Firebase] Không đọc được Web client ID từ google-services: " + ex.Message);
                }
            }

            return null;
        }

        private void HandleGoogleAuthTask(System.Threading.Tasks.Task<AuthResult> task)
        {
            bool ok = false;
            string message = "Google sign-in failed.";

            if (task == null || task.IsCanceled)
            {
                message = "Google sign-in was cancelled.";
            }
            else if (task.IsFaulted)
            {
                Debug.LogError("[Firebase] Google sign-in failed: " + task.Exception);
                message = FormatAuthError(task.Exception);
            }
            else
            {
                FirebaseUser user = task.Result != null ? task.Result.User : _auth.CurrentUser;
                if (user == null)
                {
                    message = "Google sign-in returned no user.";
                }
                else
                {
                    BindFirebaseUser(user);
                    LoadGameCloud();
                    message = ResolveAccountLabel(user);
                    ok = true;
                    LogAuthDev("Google sign-in OK.");
                }
            }

            var callbacks = _googleCallbacks.ToArray();
            _googleCallbacks.Clear();
            for (int i = 0; i < callbacks.Length; i++)
                callbacks[i]?.Invoke(ok, message);
        }

        private void BindFirebaseUser(FirebaseUser user)
        {
            string oldId = _user != null ? _user.UserId : null;
            string newId = user != null ? user.UserId : null;
            _user = user;
            if (oldId != newId)
            {
                Data = new GameData { slotIndex = ActiveSlotIndex, hasSave = false };
                _playTimeDirty = false;
                _playTimeSaveTimer = 0f;
                InvalidateCloudSlotIndex();
                if (user != null && !user.IsAnonymous)
                    RefreshCloudSlotIndex(null);
            }
        }

        private static FederatedOAuthProvider CreateGoogleProvider()
        {
            var data = new FederatedOAuthProviderData
            {
                ProviderId = GoogleAuthProvider.ProviderId,
                Scopes = new List<string> { "email", "profile" },
                CustomParameters = new Dictionary<string, string>
                {
                    { "prompt", "select_account" }
                }
            };
            return new FederatedOAuthProvider(data);
        }

        private static string ResolveAccountLabel(FirebaseUser user)
        {
            if (user == null)
                return "GOOGLE ACCOUNT";
            if (!string.IsNullOrEmpty(user.Email))
                return user.Email;

            foreach (var info in user.ProviderData)
            {
                if (info != null && !string.IsNullOrEmpty(info.Email))
                    return info.Email;
            }

            if (!string.IsNullOrEmpty(user.DisplayName))
                return user.DisplayName;
            return "GOOGLE ACCOUNT";
        }

        private static bool IsGoogleCredentialInUse(Exception exception)
        {
            if (exception is AggregateException aggregate)
                return IsGoogleCredentialInUse(aggregate);
            return exception != null && IsGoogleCredentialInUse(new AggregateException(exception));
        }

        private static bool IsGoogleCredentialInUse(AggregateException exception)
        {
            if (exception == null)
                return false;

            foreach (var inner in exception.Flatten().InnerExceptions)
            {
                if (inner is FirebaseAccountLinkException)
                    return true;

                if (inner is FirebaseException firebaseEx)
                {
                    var code = (AuthError)firebaseEx.ErrorCode;
                    if (code == AuthError.CredentialAlreadyInUse ||
                        code == AuthError.AccountExistsWithDifferentCredentials ||
                        code == AuthError.EmailAlreadyInUse)
                        return true;
                }

                if (inner != null && !string.IsNullOrEmpty(inner.Message))
                {
                    string message = inner.Message.ToLowerInvariant();
                    if (message.Contains("already in use") ||
                        message.Contains("already associated") ||
                        message.Contains("credential-already-in-use") ||
                        message.Contains("account-exists-with-different-credential"))
                        return true;
                }
            }

            return false;
        }

        private static string FormatAuthError(Exception exception)
        {
            if (exception is AggregateException aggregate)
                return FormatAuthError(aggregate);
            if (exception == null)
                return "Google sign-in failed.";
            return exception.Message;
        }

        private static string FormatAuthError(AggregateException exception)
        {
            if (exception == null)
                return "Google sign-in failed.";

            foreach (var inner in exception.Flatten().InnerExceptions)
            {
                if (inner is FirebaseException firebaseEx)
                    return $"[{firebaseEx.ErrorCode}] {firebaseEx.Message}";
                if (inner != null && !string.IsNullOrEmpty(inner.Message))
                    return inner.Message;
            }

            Exception baseEx = exception.GetBaseException();
            return baseEx != null ? baseEx.Message : exception.Message;
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private static void LogAuthDev(string message)
        {
            Debug.Log("[Firebase] " + message);
        }
    }
}
