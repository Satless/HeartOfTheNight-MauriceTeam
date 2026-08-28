using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class SoundManager_New : MonoBehaviour
{
    public static SoundManager_New Instance;

    [SerializeField] private SoundLibrary_New sfxLibrary;
    [SerializeField] private AudioSource sfxSource;

    // Cooldown quản lý thời gian chờ để chống nhiễu/stack tiếng
    //private Dictionary<string, float> lastPlayTimes = new Dictionary<string, float>();
    //[SerializeField] private float defaultCooldown = 0.15f;


    //[SerializeField] private AudioMixer audioMixer;
    //private void Start()
    //{
    //    if (audioMixer != null)
    //    {
    //        audioMixer.SetFloat("Master", Mathf.Log10(Mathf.Max(0.0001f, PlayerPrefs.GetFloat("Master", 1f))) * 20);
    //        audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(0.0001f, PlayerPrefs.GetFloat("MusicVolume", 1f))) * 20);
    //        audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(0.0001f, PlayerPrefs.GetFloat("SFXVolume", 1f))) * 20);
    //    }
    //}
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Chỉ giữ lại chính SoundManager, không lấy root
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // Xóa bản sao trùng lặp
        }
    }

    private void OnEnable()
    {
        AudioEvents.OnPlaySound2D += PlaySound2D;
        AudioEvents.OnPlaySound3D += PlaySound3D;
    }

    private void OnDisable()
    {
        AudioEvents.OnPlaySound2D -= PlaySound2D;
        AudioEvents.OnPlaySound3D -= PlaySound3D;
    }

    //private bool IsCooldownFinished(string categoryID, string subCategoryID, string actionName)
    //{
    //    string key = $"{categoryID}_{subCategoryID}_{actionName}";

    //    if (lastPlayTimes.TryGetValue(key, out float lastTime))
    //    {
    //        if (Time.time - lastTime < defaultCooldown)
    //        {
    //            return false;
    //        }
    //    }

    //    lastPlayTimes[key] = Time.time;
    //    return true;
    //}

    public void PlaySound3D(string categoryID, string subCategoryID, string actionName, Vector3 pos)
    {
        //if (!IsCooldownFinished(categoryID, subCategoryID, actionName)) return;

        AudioClip clip = sfxLibrary.GetClipFromName(categoryID, subCategoryID, actionName);
        if (clip != null)
        {
            // Nhân bản SfxSource đã gán sẵn Output Mixer SFX
            AudioSource tempSource = Instantiate(sfxSource, pos, Quaternion.identity);
            tempSource.clip = clip;
            tempSource.Play();

            // Tự động xóa GameObject tạm sau khi clip chạy xong
            Destroy(tempSource.gameObject, clip.length);
        }

        //AudioClip clip = sfxLibrary.GetClipFromName(categoryID, subCategoryID, actionName);
        //if (clip != null)
        //{
        //    // Đặt vị trí SFXSource về vị trí phát âm thanh 3D
        //    sfxSource.transform.position = pos;

        //    // Phát âm thanh đè qua PlayOneShot (đã được định hướng sang Audio Mixer SFX)
        //    sfxSource.PlayOneShot(clip);
        //}
    }

    public void PlaySound2D(string categoryID, string subCategoryID, string actionName)
    {
        //if (!IsCooldownFinished(categoryID, subCategoryID, actionName)) return;

        AudioClip clip = sfxLibrary.GetClipFromName(categoryID, subCategoryID, actionName);
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    
    //ui sfx
    public void PlaySound2DFromPath(string fullPath)
    {
        // Tách chuỗi theo dấu '/' (Ví dụ input: "UI/Button/Click")
        string[] parts = fullPath.Split('/');

        if (parts.Length == 3)
        {
            // Gọi lại hàm phát âm thanh 3 Tầng
            PlaySound2D(parts[0], parts[1], parts[2]);
        }
        else if (parts.Length == 2)
        {
            // Nếu chỉ nhập 2 tầng (Ví dụ: "UI/Click")
            PlaySound2D(parts[0], "Default", parts[1]);
        }
        else
        {
            Debug.LogWarning($"[SoundManager] Sai định dạng chuỗi! Hãy nhập dạng 'Tầng1/Tầng2/Tầng3' (Ví dụ: UI/Button/Click)");
        }
    }
}


//dùng lệnh dưới để chạy sfx một lần:
//AudioEvents.TriggerSound3D("tầng 1: player, monster,...", "tầng 2: tên quái hoặc cụ thể ra", "tầng 3: chức năng", transform.position);

//còn để chạy có cooldown thì như sau:
//vd: cho âm thanh chạy mỗi 0.2s với chức năng chạy của player


//private float stepTimer; -> tạo thời gian chạy

//private void Run(float lerpAmount)
//{
//    // Tốc độ mục tiêu = hướng input × tốc độ chạy tối đa (vd: 1 × 15 = 15, -1 × 15 = -15, 0 × 15 = 0)
//    float targetSpeed = _moveInput.x * Data.runMaxSpeed;
//    // Nội suy giữa vận tốc hiện tại và tốc độ mục tiêu.
//    // lerpAmount = 1 → nhảy thẳng sang targetSpeed (full quyền điều khiển).
//    // lerpAmount = 0 → giữ nguyên vận tốc hiện tại (không cho bẻ lái).
//    // lerpAmount ở giữa (vd: 0.13, 0.4) → chỉ trả một phần quyền bẻ lái (dùng cho dash end, wall jump).
//    targetSpeed = Mathf.Lerp(RB.linearVelocity.x, targetSpeed, lerpAmount);

//    // Chọn tỷ lệ gia tốc: đang giữ phím → dùng Accel (tăng tốc), buông phím → dùng Deccel (phanh).
//    // Trên không thì nhân thêm hệ số accelInAir/deccelInAir để giảm kiểm soát trên không trung.
//    float accelRate;
//    if (LastOnGroundTime > 0)
//        accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount : Data.runDeccelAmount;
//    else
//        accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount * Data.accelInAir : Data.runDeccelAmount * Data.deccelInAir;

//    // Buff ở đỉnh nhảy (hang time): khi gần đỉnh, tốc độ dọc ≈ 0 → tăng gia tốc và tốc độ tối đa
//    // để người chơi có cảm giác "lơ lửng" và dễ điều khiển hơn ở khoảnh khắc đỉnh nhảy.
//    if ((IsJumping || IsWallJumping || _isJumpFalling) && Mathf.Abs(RB.linearVelocity.y) < Data.jumpHangTimeThreshold)
//    {
//        accelRate *= Data.jumpHangAccelerationMult;
//        targetSpeed *= Data.jumpHangMaxSpeedMult;
//    }

//    // Bảo toàn quán tính — Nếu nhân vật đang bay nhanh hơn tốc độ tối đa (vd: sau Dash, knockback),
//    // và người chơi giữ phím cùng hướng bay, thì KHÔNG phanh lại. Chỉ áp dụng trên không.
//    if (Data.doConserveMomentum
//        && Mathf.Abs(RB.linearVelocity.x) > Mathf.Abs(targetSpeed) // Tốc độ hiện tại > tốc độ mục tiêu (đang bay nhanh hơn max)
//        && Mathf.Sign(RB.linearVelocity.x) == Mathf.Sign(targetSpeed) // Đang bay cùng hướng với phím giữ
//        && Mathf.Abs(targetSpeed) > 0.01f // Có đang giữ phím di chuyển (trái hoặc phải)
//        && LastOnGroundTime < 0) // Đang trên không (không chạm đất)
//        accelRate = 0; // Xóa gia tốc → giữ nguyên vận tốc hiện tại

//    float speedDif = targetSpeed - RB.linearVelocity.x; // Chênh lệch giữa tốc độ mong muốn và tốc độ hiện tại
//    float movement = speedDif * accelRate; // Lực cần áp dụng = chênh lệch × tỷ lệ gia tốc
//    RB.AddForce(movement * Vector2.right, ForceMode2D.Force); // Áp dụng lực ngang lên Rigidbody
//

//      [SerializeField] private float stepCooldown = 0.2f;
//      stepTimer -= Time.deltaTime;

//      if (stepTimer <= 0f)
//      {
//      // Phát tiếng bước chân 3D tại vị trí nhân vật
//          AudioEvents.TriggerSound3D("Player", "Walk", transform.position);

//          // Reset lại thời gian chờ (0.2s)
//          stepTimer = stepCooldown;
//       }
//        else
//       {
//          // Reset ngay khi dừng di chuyển để bước tiếp theo phát lại lập tức
//          stepTimer = 0f;
//       }
//} -> chạy mỗi 0.2s
//                      -huy