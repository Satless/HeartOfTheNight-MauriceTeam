using UnityEngine;
using DG.Tweening;

namespace HeartOfTheNight.Player
{
    public class PlayerAnimation : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Kéo phần Trên vào đây")]
    [SerializeField] private GameObject _upperBodyObject;
    [Tooltip("Kéo phần Dưới vào đây, lấy cái có animator ấy")]
    [SerializeField] private Animator _lowerAnimator;
    
    [Header("Settings")]
    [Tooltip("Thời gian giữ súng trên tay sau khi nhả chuột (giây)")]
    [SerializeField] private float _keepGunOutDuration;
    [Tooltip("Tỷ lệ thời lượng hiển thị dáng đạp tường so với tổng thời gian WallJumpTime. \n(VD: 0.6 = hiện dáng đạp trong 60% thời gian đầu tiên, sau đó chuyển sang dáng Bay)")]
    [Range(0f, 1f)]
    [SerializeField] private float _wallPushPoseRatio;

    [Header("VFX References")]
    [Tooltip("Kéo cục khói bám tường ở TuongPhai vào đây")]
    [SerializeField] private ParticleSystem _rightWallVfx;
    [Tooltip("Kéo cục khói bám tường ở TuongTrai vào đây")]
    [SerializeField] private ParticleSystem _leftWallVfx;
    [Tooltip("Data chứa hiệu ứng khói nhảy (Kéo file Jump.asset vào đây)")]
    [SerializeField] private StatusEffectData _jumpVfxData;
    [Tooltip("Data chứa hiệu ứng bốc khói/lửa khi quá nhiệt (Kéo file QuaNhiet.asset vào đây)")]
    [SerializeField] private StatusEffectData _overheatVfxData;

    private PlayerMovement _movement;
    private PlayerAttack _attack;

    // Cache lại các parameter hash để tối ưu hiệu năng (Zero GC)
    private static readonly int VelocityYKey = Animator.StringToHash("VelocityY");
    private static readonly int RunSpeedKey = Animator.StringToHash("RunSpeed");

    // Hỗ trợ logic cất súng
    [Header("Debug Tracking")]
    [Tooltip("Thời điểm bắn đạn cuối cùng")]
    [SerializeField, ReadOnly] private float _lastShootInputTime = -999f;
    [Tooltip("Cờ báo hiệu đang cầm súng (ngăn các hoạt ảnh không tay)")]
    [SerializeField, ReadOnly] private bool _isHoldingGun;
    // Cờ đặc biệt: Vừa chuyển súng thì bắt buộc show súng một lúc
    private float _lastWeaponSwitchTime = -999f;
    // Track trạng thái trước đó để phát Landing VFX khi đáp đất từ trên không
    private PlayerMovement.PlayerState _previousState = PlayerMovement.PlayerState.Grounded;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _attack = GetComponent<PlayerAttack>();
    }

    private void OnEnable()
    {
        if (_attack != null) _attack.OnRecoil += HandleRecoil;
    }

    private void OnDisable()
    {
        if (_attack != null) _attack.OnRecoil -= HandleRecoil;
    }

    private void HandleRecoil(float dirX, float fireRate)
    {
        if (_upperBodyObject != null)
        {
            _upperBodyObject.transform.DOKill();
            float recoilDuration = Mathf.Min(0.1f, fireRate * 0.8f);
            Vector3 recoilForce = new Vector3(-dirX * 0.15f, 0.03f, 0f);
            _upperBodyObject.transform.DOPunchPosition(recoilForce, recoilDuration, 1, 0.5f).SetRelative(true);
        }
    }

    private void Start()
    {
        // Đảm bảo các Particle System liên tục luôn ở trạng thái Play để hệ thống chỉ cần bật/tắt Emission
        if (_rightWallVfx != null && !_rightWallVfx.isPlaying) _rightWallVfx.Play();
        if (_leftWallVfx != null && !_leftWallVfx.isPlaying) _leftWallVfx.Play();

        // Khởi tạo sẵn (Prewarm) khói nhảy và khói quá nhiệt vào kho Pooling (Zero GC)
        if (_jumpVfxData != null && _jumpVfxData.effectVfxPrefab != null)
        {
            _jumpVfxData.effectVfxPrefab.Prewarm(_jumpVfxData.prewarmCount);
        }
        
        if (_overheatVfxData != null && _overheatVfxData.effectVfxPrefab != null)
        {
            _overheatVfxData.effectVfxPrefab.Prewarm(_overheatVfxData.prewarmCount);
        }
    }

    private void Update()
    {
        UpdateGunState();
        HandleBlendTreeParams();
        HandleMoonwalk();

        // -------------------------------------------------------------
        // XỬ LÝ SUB-STATE LIÊN TỤC (Grounded & Sliding)
        // Những state này cần thay đổi animation dựa vào Input của người chơi
        // -------------------------------------------------------------
        var state = _movement.CurrentState;
        
        // Các state khóa cứng toàn thân (không bị ghi đè bởi dáng đi/đứng khi cầm súng hay rơi lơ lửng)
        bool isDoingFullBodyAction = state == PlayerMovement.PlayerState.Dashing 
                                  || state == PlayerMovement.PlayerState.WallJumping 
                                  || state == PlayerMovement.PlayerState.KnockedBack
                                  || state == PlayerMovement.PlayerState.Sliding
                                  || state == PlayerMovement.PlayerState.LedgeClimbing;
        
        bool shouldShowUpperBody = _isHoldingGun && !isDoingFullBodyAction;
        if (_upperBodyObject.activeSelf != shouldShowUpperBody)
        {
            _upperBodyObject.SetActive(shouldShowUpperBody);
        }


        if (state == PlayerMovement.PlayerState.Grounded)
        {
            // VISUAL-ONLY COYOTE FALL: FSM vẫn giữ Grounded (để CanJump() hoạt động),
            // nhưng nếu velocity.y âm đáng kể → người chơi đã rời mép → hiện animation rơi.
            // Ngưỡng -0.5f lọc bỏ nhiễu vật lý trên mặt phẳng (sleep mode ~1e-05).
            bool isCoyoteFalling = _movement.RB.linearVelocity.y < -0.5f
                                && _movement.LastOnGroundTime > 0
                                && _movement.LastOnGroundTime < _movement.Data.coyoteTime;

            if (isCoyoteFalling)
            {
                // Hiện animation rơi nhưng KHÔNG thay đổi FSM state
                if (_isHoldingGun)
                {
                    bool isMoving = Mathf.Abs(_movement.RB.linearVelocity.x) > 0.1f;
                    PlayAnim(isMoving ? "ThanDuoi-dichuyen" : "ThanDuoi-dungban");
                }
                else
                    PlayAnim("Nhay");
            }
            else
            {
                // Kết hợp cả Input và Velocity để giải quyết triệt để lỗi Moonwalk và lỗi trượt Move
                bool isMoving = Mathf.Abs(_movement.MoveInput.x) > 0.1f && Mathf.Abs(_movement.RB.linearVelocity.x) > 0.1f;
                if (_isHoldingGun)
                    PlayAnim(isMoving ? "ThanDuoi-dichuyen" : "ThanDuoi-dungban");
                else
                    PlayAnim(isMoving ? "Duoi-move" : "Duoi-ide");
            }
                
            // Chắc chắn tắt khói tường
            if (_rightWallVfx != null)
            {
                var em = _rightWallVfx.emission;
                em.enabled = false;
            }
            if (_leftWallVfx != null)
            {
                var em = _leftWallVfx.emission;
                em.enabled = false;
            }
        }
        else if (state == PlayerMovement.PlayerState.Sliding)
        {
            if (_movement.MoveInput.y > 0)
                PlayAnim("Duoi-leotuong");
            else
                PlayAnim("Duoi-TruotTuong");

            // Xác định đang bám tường nào dựa vào timer trong PlayerMovement
            bool onRightWall = _movement.LastOnWallRightTime > 0;
            bool onLeftWall = _movement.LastOnWallLeftTime > 0;
            
            // Khói ma sát chỉ xuất hiện khi thực sự trượt XUỐNG (trọng lực kéo)
            // Không hiện khi leo lên (velocity.y > 0) hoặc kẹt góc (velocity.y ≈ 0)
            bool isSlidingDown = _movement.RB.linearVelocity.y < 0;

            if (_rightWallVfx != null)
            {
                var em = _rightWallVfx.emission;
                em.enabled = onRightWall && isSlidingDown;
            }

            if (_leftWallVfx != null)
            {
                var em = _leftWallVfx.emission;
                em.enabled = onLeftWall && isSlidingDown;
            }
        }
        else if (state == PlayerMovement.PlayerState.WallJumping)
        {
            // Trong một khoảng thời gian đầu (tỷ lệ với wallJumpTime): giữ dáng đạp tường (Duoi-TruotTuong).
            // Mẹo ở LateUpdate sẽ lật mặt nhân vật úp vào tường để tạo cảm giác dùng chân đạp ra.
            // Sau đó: chuyển sang dáng bay lơ lửng (Nhay).
            float pushDuration = _movement.Data.wallJumpTime * _wallPushPoseRatio;
            if (Time.time - _movement.WallJumpStartTime < pushDuration)
                PlayAnim("Duoi-TruotTuong");
            else
                PlayAnim("Nhay");
                
            // Chắc chắn tắt VFX vì đang rời tường
            if (_rightWallVfx != null) { var em = _rightWallVfx.emission; em.enabled = false; }
            if (_leftWallVfx != null)  { var em = _leftWallVfx.emission;  em.enabled = false; }
        }
        else if (!isDoingFullBodyAction)
        {
            // Đang trên không (vì đã lọt qua Grounded và Dashing/Sliding)
            if (_isHoldingGun)
            {
                bool isMoving = Mathf.Abs(_movement.RB.linearVelocity.x) > 0.1f;
                PlayAnim(isMoving ? "ThanDuoi-dichuyen" : "ThanDuoi-dungban");
            }
            else
            {
                // Đang bay lơ lửng (Falling, Jumping...) — WallJumping không rơi vào đây (isDoingFullBodyAction)
                PlayAnim("Nhay");
            }

            // Tắt hết khói bụi liên tục khi đang bay lơ lửng
            if (_rightWallVfx != null)
            {
                var em = _rightWallVfx.emission;
                em.enabled = false;
            }
            if (_leftWallVfx != null)
            {
                var em = _leftWallVfx.emission;
                em.enabled = false;
            }
        }

    }

    private void LateUpdate()
    {
        // 1. ĐỒNG BỘ QUAY MẶT THÂN TRÊN THEO HƯỚNG SÚNG (Được chuyển sang từ PlayerAttack)
        if (_upperBodyObject != null && _attack != null)
        {
            Vector3 upperScale = _upperBodyObject.transform.localScale;
            
            if (_movement.IsDashing && _movement.Data.lockFacingToDashDirection)
            {
                upperScale.x = _movement.IsFacingRight ? Mathf.Abs(upperScale.x) : -Mathf.Abs(upperScale.x);
            }
            else
            {
                upperScale.x = _attack.IsAimingRight ? Mathf.Abs(upperScale.x) : -Mathf.Abs(upperScale.x);
            }
            _upperBodyObject.transform.localScale = upperScale;
        }

        // 2. ĐỒNG BỘ HÓA HƯỚNG NHÌN VÀ QUAY MẶT THÂN DƯỚI (Visual Facing)
        // Dùng LateUpdate để đè lên các thay đổi scale từ PlayerMovement.Turn() (nếu có)
        Vector3 lowerScale = _lowerAnimator.transform.localScale;

        if (_isHoldingGun && _upperBodyObject.activeSelf && _attack != null)
        {
            // KHI CÓ SÚNG (VÀ ĐANG HIỆN): Ép phần thân dưới (chân) quay theo hướng súng
            // để tránh hiện tượng vặn xoắn, bất kể PlayerMovement đang đi hướng nào.
            float upperSign = _attack.IsAimingRight ? 1f : -1f;
            
            if (_movement.IsDashing && _movement.Data.lockFacingToDashDirection)
            {
                upperSign = _movement.IsFacingRight ? 1f : -1f;
            }

            lowerScale.x = Mathf.Abs(lowerScale.x) * upperSign;
        }
        else
        {
            // KHI CẤT SÚNG: Trả phần thân dưới quay theo hướng vật lý (IsFacingRight của PlayerMovement)
            float moveSign = _movement.IsFacingRight ? 1f : -1f;

            // MẸO VISUAL: Khi vừa búng tường, vật lý đã quay mặt ra ngoài,
            // nhưng ta muốn giữ dáng "đạp tường" hướng vào trong tường (0.15s đầu tiên).
            if (_movement.CurrentState == PlayerMovement.PlayerState.WallJumping && _currentAnim == "Duoi-TruotTuong")
            {
                moveSign *= -1f;
            }

            lowerScale.x = Mathf.Abs(lowerScale.x) * moveSign;
        }

        _lowerAnimator.transform.localScale = lowerScale;
    }

    private void UpdateGunState()
    {
        // 1. XỬ LÝ LOGIC HIỆN/ẨN THÂN TRÊN CẦM SÚNG
        bool isShooting = _attack != null && _attack.IsPressingFire;
        
        if (isShooting)
        {
            _lastShootInputTime = Time.time;
        }

        // Kiểm tra xem người chơi có đang trong trạng thái "Rút súng" không
        bool isGrounded = _movement.CurrentState == PlayerMovement.PlayerState.Grounded;
        bool isJustSwitched = Time.time - _lastWeaponSwitchTime <= _keepGunOutDuration;
        
        if (isGrounded)
        {
            // Trên mặt đất: giữ súng thêm một lúc sau khi nhả chuột HOẶC vừa đổi súng
            _isHoldingGun = (Time.time - _lastShootInputTime <= _keepGunOutDuration) || isJustSwitched;
        }
        else
        {
            // Trên không: chỉ rút súng khi đang nhấn/giữ chuột HOẶC vừa đổi súng
            _isHoldingGun = isShooting || isJustSwitched;
        }
    }

    /// <summary>
    /// Được gọi từ PlayerAttack khi người chơi ấn phím chuyển súng.
    /// Giúp nhân vật vào trạng thái "rút súng" dạng Idle một lúc để nhìn thấy súng mới.
    /// </summary>
    public void TriggerWeaponSwitchDisplay()
    {
        _lastWeaponSwitchTime = Time.time;
    }

    /// <summary>
    /// Được gọi từ PlayerAttack khi súng bị quá nhiệt, sinh hiệu ứng khói/lửa.
    /// </summary>
    public void PlayOverheatVfx(Vector3 spawnPosition)
    {
        if (_overheatVfxData != null && _overheatVfxData.effectVfxPrefab != null)
        {
            _overheatVfxData.effectVfxPrefab.Spawn(spawnPosition, Quaternion.identity);
        }
    }

    /// <summary>
    /// Được gọi từ PlayerAttack để ép hiện thân trên NGAY LẬP TỨC trước khi bắn,
    /// tránh trường hợp Animator đang inactive khiến SetTrigger("Fire") bị bỏ qua
    /// do thứ tự Update() giữa PlayerAttack và PlayerAnimation không xác định.
    /// </summary>
    public void ShowUpperBodyImmediately()
    {
        if (_upperBodyObject != null && !_upperBodyObject.activeSelf)
        {
            _upperBodyObject.SetActive(true);
        }
    }

    private void HandleBlendTreeParams()
    {
        // Liên tục cập nhật vận tốc Y cho Blend Tree (Nhay/Lolung/Roi)
        _lowerAnimator.SetFloat(VelocityYKey, _movement.RB.linearVelocity.y);
    }

    private void HandleMoonwalk()
    {
        // Nếu không cầm súng, nhân vật cứ chạy bình thường tiến về phía trước
        if (!_isHoldingGun) 
        {
            _lowerAnimator.SetFloat(RunSpeedKey, 1f);
            return;
        }

        bool isMoving = Mathf.Abs(_movement.MoveInput.x) > 0.1f && Mathf.Abs(_movement.RB.linearVelocity.x) > 0.1f;

        if (!isMoving || _movement.CurrentState != PlayerMovement.PlayerState.Grounded)
        {
            // Không di chuyển hoặc trên không thì tốc độ phát anim là 1 (bình thường)
            _lowerAnimator.SetFloat(RunSpeedKey, 1f);
        }
        else
        {
            // Khi CÓ DI CHUYỂN: So sánh hướng ngắm súng và hướng di chuyển vật lý
            bool isAimingRight = _attack != null && _attack.IsAimingRight;
            
            // Nếu ngắm và chạy ngược hướng nhau -> Moonwalk
            bool isMoonwalk = (isAimingRight != _movement.IsFacingRight);

            // Gán vào biến RunSpeed trong Animator (Multiplier của node ThanDuoi-dichuyen)
            _lowerAnimator.SetFloat(RunSpeedKey, isMoonwalk ? -1f : 1f);
        }
    }

    /// <summary>
    /// FSM Callback: Được gọi từ PlayerMovement.TransitionToState() 
    /// mỗi khi trạng thái vật lý thay đổi.
    /// </summary>
    public void OnStateChanged(PlayerMovement.PlayerState newState)
    {
        UpdateGunState(); // Ép lấy trạng thái súng chuẩn xác nhất của frame này

        // Chỉ xử lý các state One-shot (chỉ kích hoạt 1 lần khi vào state)
        switch (newState)
        {
            case PlayerMovement.PlayerState.Grounded:
                // Landing VFX: Chỉ nổ khói khi đáp đất từ trạng thái trên không
                bool wasAirborne = _previousState == PlayerMovement.PlayerState.Falling
                                || _previousState == PlayerMovement.PlayerState.Jumping
                                || _previousState == PlayerMovement.PlayerState.WallJumping
                                || _previousState == PlayerMovement.PlayerState.DroppingThrough;
                if (wasAirborne && _jumpVfxData != null && _jumpVfxData.effectVfxPrefab != null && _movement.GroundCheckPoint != null)
                {
                    _jumpVfxData.effectVfxPrefab.Spawn(_movement.GroundCheckPoint.position, Quaternion.Euler(-90, 0, 0));
                }
                break;

            case PlayerMovement.PlayerState.Jumping:
                // Khói bụi Nhảy (Burst) - Gọi Pooling Spawn tại dưới chân
                if (_jumpVfxData != null && _jumpVfxData.effectVfxPrefab != null && _movement.GroundCheckPoint != null)
                {
                    _jumpVfxData.effectVfxPrefab.Spawn(_movement.GroundCheckPoint.position, Quaternion.Euler(-90, 0, 0));
                }

                // Fallthrough (dùng chung logic với Falling)
                goto case PlayerMovement.PlayerState.Falling;

            case PlayerMovement.PlayerState.DroppingThrough:
            case PlayerMovement.PlayerState.Falling:
                // Nếu đang rút súng → dùng animation thân dưới cầm súng
                if (_isHoldingGun)
                {
                    bool isMoving = Mathf.Abs(_movement.RB.linearVelocity.x) > 0.1f;
                    PlayAnim(isMoving ? "ThanDuoi-dichuyen" : "ThanDuoi-dungban");
                }
                else
                    PlayAnim("Nhay");
                break;
                
            case PlayerMovement.PlayerState.WallJumping:
                // Khởi đầu cú nhảy tường luôn là dáng bám tường (đạp ván)
                PlayAnim("Duoi-TruotTuong");
                // Tắt VFX khói tường ngay khi bật
                if (_rightWallVfx != null) { var em = _rightWallVfx.emission; em.enabled = false; }
                if (_leftWallVfx != null)  { var em = _leftWallVfx.emission;  em.enabled = false; }
                break;
                
            case PlayerMovement.PlayerState.KnockedBack:
                // Nhảy và Rơi đang dùng chung clip "Nhay" trong Animator
                PlayAnim("Nhay");
                break;
                
            case PlayerMovement.PlayerState.LedgeClimbing:
                PlayAnim("Duoi-leotuong");
                break;
                
            case PlayerMovement.PlayerState.Dashing:
                PlayAnim("Duoi-Dash");
                break;
        }

        _previousState = newState;
    }

    [Header("State Tracking")]
    [Tooltip("Tên animation hiện tại đang được yêu cầu chạy")]
    [SerializeField, ReadOnly] private string _currentAnim;
    /// <summary>Tên animation đang chạy (cho Debug Panel đọc).</summary>
    public string CurrentAnimName => _currentAnim;
    
    public void TriggerDeath()
    {
        // Ẩn thân trên (súng)
        if (_upperBodyObject != null) _upperBodyObject.SetActive(false);
        
        // Chạy anim chết
        PlayAnim("Duoi-chet");
        
        // Tắt script này để Update không đè anim khác lên
        this.enabled = false; 
    }

    public void DetachVisualsForDeath()
    {
        // Tách phần thân dưới (đang chạy anim chết) ra khỏi Player gốc 
        // để nó không bị biến mất khi ta Destroy Player gốc.
        // Sau khi thảo luận lại với Đạt thì xem nên làm thế nào
        if (_lowerAnimator != null)
        {
            _lowerAnimator.transform.SetParent(null);
        }
    }

    /// <summary>
    /// Hàm helper để gọi Animator.Play mà không làm reset frame nếu anim đang chạy
    /// </summary>
    private void PlayAnim(string animName)
    {
        if (_currentAnim == animName) return;
        
        // Debug.Log($"[PlayAnim] Yêu cầu chuyển sang: '{animName}'");
        _lowerAnimator.Play(animName);
        _currentAnim = animName;
    }
}
}