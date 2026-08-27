using UnityEngine;

namespace HeartOfTheNight.UI
{
    /// <summary>
    /// Trạng thái đăng nhập (PlayerPrefs). AuthScene / Firebase Google ghi, Settings trên mainMenu đọc.
    /// </summary>
    public static class AuthSession
    {
        public const string GuestEmail = "GUEST (LOCAL SAVE)";
        public const string GuestStatus = "STATUS: OFFLINE ACCOUNT";
        public const string GoogleStatus = "STATUS: ONLINE | SLOT SYNCED";

        private const string KeyIsGuest = "Auth.IsGuest";
        private const string KeyEmail = "Auth.Email";

        public static bool IsGuest => PlayerPrefs.GetInt(KeyIsGuest, 1) == 1;

        public static string Email =>
            PlayerPrefs.GetString(KeyEmail, GuestEmail);

        public static string Status =>
            IsGuest ? GuestStatus : GoogleStatus;

        public static void SignInAsGuest()
        {
            PlayerPrefs.SetInt(KeyIsGuest, 1);
            PlayerPrefs.SetString(KeyEmail, GuestEmail);
            PlayerPrefs.Save();
        }

        public static void SignInWithGoogle(string email = "player@gmail.com")
        {
            PlayerPrefs.SetInt(KeyIsGuest, 0);
            PlayerPrefs.SetString(KeyEmail, string.IsNullOrWhiteSpace(email) ? "player@gmail.com" : email);
            PlayerPrefs.Save();
        }

        public static void SignOut()
        {
            PlayerPrefs.DeleteKey(KeyIsGuest);
            PlayerPrefs.DeleteKey(KeyEmail);
            PlayerPrefs.Save();
        }
    }
}
