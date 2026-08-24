using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace HeartOfTheNight.Hung
{
    /// <summary>
    /// Google OAuth cho Editor / Windows: mở trình duyệt, nhận code qua localhost, đổi lấy id_token.
    /// SignInWithProvider của Firebase không chạy trên desktop.
    /// </summary>
    public static class GoogleDesktopOAuth
    {
        public const int DefaultPort = 53421;

        public static string RedirectUri(int port = DefaultPort)
        {
            // Web client trên Google Cloud chấp nhận localhost hơn 127.0.0.1
            return $"http://localhost:{port}/";
        }

        private static readonly object Gate = new object();
        private static HttpListener _listener;
        private static volatile bool _cancelled;

        public static void Cancel()
        {
            _cancelled = true;
            StopListener();
        }

        public static void RequestIdToken(
            string clientId,
            string clientSecret,
            int port,
            Action<bool, string, string, string> onComplete)
        {
            if (onComplete == null)
                return;

            Cancel();
            _cancelled = false;

            var sync = SynchronizationContext.Current;
            void Done(bool ok, string idToken, string accessToken, string error)
            {
                if (sync != null)
                    sync.Post(_ => onComplete(ok, idToken, accessToken, error), null);
                else
                    onComplete(ok, idToken, accessToken, error);
            }

            clientId = (clientId ?? "").Trim().Trim('"');
            clientSecret = (clientSecret ?? "").Trim().Trim('"');
            if (port <= 0)
                port = DefaultPort;

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                Done(false, null, null, MissingConfigMessage(port));
                return;
            }

            HttpListener listener;
            string redirectUri;
            try
            {
                listener = StartListener(port, out redirectUri);
            }
            catch (Exception ex)
            {
                Done(false, null, null,
                    "Cannot listen on localhost:" + port + "\n" +
                    ex.Message + "\nClose the app using that port, then retry.");
                return;
            }

            Debug.Log("[Google OAuth] redirect_uri=" + redirectUri + " — dán đúng chuỗi này vào Authorized redirect URIs.");

            string state = Base64Url(RandomBytes(16));
            string verifier = Base64Url(RandomBytes(32));
            string challenge = Base64Url(Sha256(Encoding.ASCII.GetBytes(verifier)));
            string authUrl =
                "https://accounts.google.com/o/oauth2/v2/auth" +
                "?response_type=code" +
                "&client_id=" + Uri.EscapeDataString(clientId) +
                "&redirect_uri=" + Uri.EscapeDataString(redirectUri) +
                "&scope=" + Uri.EscapeDataString("openid email profile") +
                "&prompt=select_account" +
                "&state=" + Uri.EscapeDataString(state) +
                "&code_challenge=" + Uri.EscapeDataString(challenge) +
                "&code_challenge_method=S256";

            Task.Run(() =>
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();
                    if (_cancelled)
                    {
                        WriteHtml(context, "Cancelled. You can close this tab.");
                        Done(false, null, null, "Google sign-in was cancelled.");
                        return;
                    }

                    string query = context.Request.Url != null ? context.Request.Url.Query : "";
                    var args = ParseQuery(query);
                    string error = GetArg(args, "error");
                    string errorDesc = GetArg(args, "error_description");
                    string code = GetArg(args, "code");
                    string returnedState = GetArg(args, "state");

                    if (!string.IsNullOrEmpty(error))
                    {
                        WriteHtml(context, "Google sign-in failed. You can close this tab.");
                        string detail = string.IsNullOrEmpty(errorDesc) ? error : error + ": " + errorDesc;
                        if (error.IndexOf("redirect_uri", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            (errorDesc != null && errorDesc.IndexOf("redirect_uri", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            detail += "\n\nAdd this exact URI in Google Cloud Console → Credentials → Web client → Authorized redirect URIs:\n" +
                                      redirectUri;
                        }

                        Done(false, null, null, detail);
                        return;
                    }

                    if (string.IsNullOrEmpty(code) || returnedState != state)
                    {
                        WriteHtml(context, "Invalid Google response. You can close this tab.");
                        Done(false, null, null, "Invalid OAuth callback from Google.");
                        return;
                    }

                    WriteHtml(context, "Google sign-in OK. You can close this tab and return to Unity.");

                    if (_cancelled)
                    {
                        Done(false, null, null, "Google sign-in was cancelled.");
                        return;
                    }

                    if (!ExchangeCode(clientId, clientSecret, redirectUri, code, verifier, out string idToken, out string accessToken, out string tokenError))
                    {
                        Done(false, null, null, tokenError);
                        return;
                    }

                    Done(true, idToken, accessToken, null);
                }
                catch (Exception ex)
                {
                    if (_cancelled || ex is HttpListenerException || ex is ObjectDisposedException)
                    {
                        Done(false, null, null, "Google sign-in was cancelled.");
                        return;
                    }

                    Done(false, null, null, ex.Message);
                }
                finally
                {
                    StopListener();
                }
            });

            Application.OpenURL(authUrl);
        }

        public static string MissingConfigMessage(int port = DefaultPort)
        {
            return
                "Missing Google Web Client ID / Secret.\n\n" +
                "1) Firebase Console → Authentication → Sign-in method → Google → Enable.\n" +
                "2) Google Cloud Console → APIs & Services → Credentials.\n" +
                "   Open 'Web client (auto created by Google Service)'.\n" +
                "3) Copy Client ID + Client secret into DataManager (Inspector).\n" +
                "4) Authorized redirect URIs → add exactly:\n" +
                RedirectUri(port);
        }

        private static HttpListener StartListener(int port, out string redirectUri)
        {
            lock (Gate)
            {
                StopListener();
                var listener = new HttpListener();
                string localhostPrefix = $"http://localhost:{port}/";
                string loopbackPrefix = $"http://127.0.0.1:{port}/";
                listener.Prefixes.Add(localhostPrefix);
                try
                {
                    listener.Prefixes.Add(loopbackPrefix);
                }
                catch
                {
                    // optional
                }

                try
                {
                    listener.Start();
                    redirectUri = localhostPrefix;
                    _listener = listener;
                    return listener;
                }
                catch (HttpListenerException)
                {
                    try
                    {
                        listener.Close();
                    }
                    catch
                    {
                        // ignored
                    }

                    listener = new HttpListener();
                    listener.Prefixes.Add(loopbackPrefix);
                    listener.Start();
                    redirectUri = loopbackPrefix;
                    _listener = listener;
                    return listener;
                }
            }
        }

        private static void StopListener()
        {
            lock (Gate)
            {
                if (_listener == null)
                    return;
                try
                {
                    _listener.Close();
                }
                catch
                {
                    // ignored
                }

                _listener = null;
            }
        }

        private static void WriteHtml(HttpListenerContext context, string message)
        {
            string html =
                "<html><body style='font-family:sans-serif;background:#111;color:#eee;padding:40px'>" +
                "<h2>Heart Of The Night</h2><p>" + WebUtility.HtmlEncode(message) + "</p></body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(html);
            try
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();
            }
            catch
            {
                // ignored
            }
        }

        private static bool ExchangeCode(
            string clientId,
            string clientSecret,
            string redirectUri,
            string code,
            string verifier,
            out string idToken,
            out string accessToken,
            out string error)
        {
            idToken = null;
            accessToken = null;
            error = "Token exchange failed.";

            string form =
                "code=" + Uri.EscapeDataString(code) +
                "&client_id=" + Uri.EscapeDataString(clientId) +
                "&client_secret=" + Uri.EscapeDataString(clientSecret) +
                "&redirect_uri=" + Uri.EscapeDataString(redirectUri) +
                "&grant_type=authorization_code" +
                "&code_verifier=" + Uri.EscapeDataString(verifier);

            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://oauth2.googleapis.com/token");
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                byte[] body = Encoding.UTF8.GetBytes(form);
                request.ContentLength = body.Length;
                using (var stream = request.GetRequestStream())
                    stream.Write(body, 0, body.Length);

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    string json = reader.ReadToEnd();
                    idToken = ExtractJsonString(json, "id_token");
                    accessToken = ExtractJsonString(json, "access_token");
                    if (string.IsNullOrEmpty(idToken))
                    {
                        error = "Google token response had no id_token.\n" + json;
                        return false;
                    }

                    return true;
                }
            }
            catch (WebException webEx)
            {
                string json = "";
                try
                {
                    if (webEx.Response != null)
                    {
                        using (var reader = new StreamReader(webEx.Response.GetResponseStream()))
                            json = reader.ReadToEnd();
                    }
                }
                catch
                {
                    // ignored
                }

                string googleError = ExtractJsonString(json, "error");
                string googleDesc = ExtractJsonString(json, "error_description");
                error = string.IsNullOrEmpty(googleDesc) ? (googleError ?? webEx.Message) : googleDesc;
                if (!string.IsNullOrEmpty(json) &&
                    json.IndexOf("redirect_uri", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    error += "\n\nAdd this exact URI in Google Cloud Console → Web client → Authorized redirect URIs:\n" +
                             redirectUri;
                }

                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(query))
                return map;

            if (query.StartsWith("?"))
                query = query.Substring(1);

            string[] parts = query.Split('&');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                int eq = part.IndexOf('=');
                if (eq <= 0)
                    continue;
                string key = Uri.UnescapeDataString(part.Substring(0, eq));
                string value = Uri.UnescapeDataString(part.Substring(eq + 1).Replace('+', ' '));
                map[key] = value;
            }

            return map;
        }

        private static string GetArg(Dictionary<string, string> args, string key)
        {
            return args.TryGetValue(key, out string value) ? value : null;
        }

        private static string ExtractJsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
                return null;

            string needle = "\"" + key + "\"";
            int keyIndex = json.IndexOf(needle, StringComparison.Ordinal);
            if (keyIndex < 0)
                return null;

            int colon = json.IndexOf(':', keyIndex + needle.Length);
            if (colon < 0)
                return null;

            int start = json.IndexOf('"', colon + 1);
            if (start < 0)
                return null;

            int end = start + 1;
            while (end < json.Length)
            {
                if (json[end] == '"' && json[end - 1] != '\\')
                    break;
                end++;
            }

            if (end >= json.Length)
                return null;

            return json.Substring(start + 1, end - start - 1);
        }

        private static byte[] RandomBytes(int count)
        {
            byte[] bytes = new byte[count];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return bytes;
        }

        private static byte[] Sha256(byte[] data)
        {
            using (var sha = SHA256.Create())
                return sha.ComputeHash(data);
        }

        private static string Base64Url(byte[] data)
        {
            return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
