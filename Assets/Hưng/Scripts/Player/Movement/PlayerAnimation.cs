using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Kéo phần Trên vào đây")]
    [SerializeField] private GameObject _upperBodyObject;
    [Tooltip("Kéo phần Dưới vào đây, lấy cái có animator ấy")]
    [SerializeField] private Animator _lowerAnimator;
    
    [Header("Settings")]
    [Tooltip("Thời gian giữ súng trên tay sau khi nhả chuột (giây)")]
    [SerializeField] private float _keepGunOutDuration = 1.5f;

    [Header("Movement VFX (Prefabs)")]
    [Tooltip("Khói khi nhảy (Sinh 1 lần - Bắt buộc gắn HitVfx/AutoDespawn)")]
    [SerializeField] private GameObject _jumpVfxPrefab;
    [Tooltip("Khói khi chạy (Sinh liên tục - Xóa script HitVfx đi, bật Looping)")]
    [SerializeField] private GameObject _runVfxPrefab;
    [Tooltip("Khói khi bám tường (Sinh liên tục - Xóa script HitVfx đi, bật Looping)")]
    [SerializeField] private GameObject _wallVfxPrefab;

    private GameObject _runVfxInstance;
    private GameObject _wallVfxInstance;
    private ParticleSystem[] _runParticles;
    private ParticleSystem[] _wallParticles;

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

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _attack = GetComponent<PlayerAttack>();
    }

    private void Start()
    {
        // Khởi tạo VFX liên tục (Chạy & Bám tường) dính chặt vào Player để bật tắt, không dùng Pool.
        if (_runVfxPrefab != null)
        {
            _runVfxInstance = Instantiate(_runVfxPrefab, transform);
            _runParticles = _runVfxInstance.GetComponentsInChildren<ParticleSystem>();
            ToggleParticles(_runParticles, false);
        }

        if (_wallVfxPrefab != null)
        {
            _wallVfxInstance = Instantiate(_wallVfxPrefab, transform);
            _wallParticles = _wallVfxInstance.GetComponentsInChildren<ParticleSystem>();
            ToggleParticles(_wallParticles, false);
        }
    }

    private void ToggleParticles(ParticleSystem[] particles, bool isPlaying)
    {
        if (particles == null) return;
        foreach (var p in particles)
        {
            if (isPlaying && !p.isPlaying) p.Play();
            else if (!isPlaying && p.isPlaying) p.Stop();
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
        
        // Các hành động full-body BẮT BUỘC cất súng (không cho bắn)
        bool isDoingFullBodyAction = (state == PlayerMovement.PlayerState.Dashing) || 
                                     (state == PlayerMovement.PlayerState.Sliding) ||
                                     (state == PlayerMovement.PlayerState.WallJumping);
        
        bool shouldShowUpperBody = _isHoldingGun && !isDoingFullBodyAction;
        if (_upperBodyObject.activeSelf != shouldShowUpperBody)
        {
            _upperBodyObject.SetActive(shouldShowUpperBody);
        }


        if (state == PlayerMovement.PlayerState.Grounded)
        {
            // Kết hợp cả Input và Velocity để giải quyết triệt để lỗi Moonwalk và lỗi trượt Move
            bool isMoving = Mathf.Abs(_movement.MoveInput.x) > 0.1f && Mathf.Abs(_movement.RB.linearVelocity.x) > 0.1f;
            if (_isHoldingGun)
                PlayAnim(isMoving ? "ThanDuoi-dichuyen" : "ThanDuoi-dungban");
            else
                PlayAnim(isMoving ? "Duoi-move" : "Duoi-ide");

            // Bật khói chạy
            if (isMoving)
            {
                if (_runVfxInstance) _runVfxInstance.transform.position = _movement.GroundCheckPoint.position;
                ToggleParticles(_runParticles, true);
            }
            else
            {
                ToggleParticles(_runParticles, false);
            }
        }
        else if (state == PlayerMovement.PlayerState.Sliding)
        {
            if (_movement.MoveInput.y > 0)
                PlayAnim("Duoi-leotuong");
            else
                PlayAnim("Duoi-TruotTuong");

            // Tắt khói chạy
            ToggleParticles(_runParticles, false);

            // Bật khói tường và dời nó qua trái hoặc phải tùy thuộc đang bám bên nào
            if (_wallVfxInstance != null)
            {
                bool isRightWall = _movement.LastOnWallRightTime > 0;
                Transform targetWall = isRightWall ? _movement.RightWallCheckPoint : _movement.LeftWallCheckPoint;
                _wallVfxInstance.transform.position = targetWall.position;
                
                // Lật VFX khói tường để bụi luôn văng ra ngoài
                Vector3 wallScale = _wallVfxInstance.transform.localScale;
                wallScale.x = isRightWall ? -Mathf.Abs(wallScale.x) : Mathf.Abs(wallScale.x);
                _wallVfxInstance.transform.localScale = wallScale;
                
                ToggleParticles(_wallParticles, true);
            }
        }
        else if (!isDoingFullBodyAction)
        {
            // Đang trên không (vì đã lọt qua Grounded và Dashing/Sliding)
            ToggleParticles(_runParticles, false);
            ToggleParticles(_wallParticles, false);

            if (_isHoldingGun)
            {
                bool isMoving = Mathf.Abs(_movement.RB.linearVelocity.x) > 0.1f;
                PlayAnim(isMoving ? "ThanDuoi-dichuyen" : "ThanDuoi-dungban");
            }
            else
            {
                // Vừa nhả chuột giữa không trung -> Trả lại animation gốc!
                if (state == PlayerMovement.PlayerState.WallJumping)
                    PlayAnim("Duoi-TruotTuong");
                else 
                    PlayAnim("Nhay");
            }
        }
        else
        {
            // Tắt VFX khi đang Dash, Fall...
            ToggleParticles(_runParticles, false);
            ToggleParticles(_wallParticles, false);
        }

    }

    private void LateUpdate()
    {
        // Đồng bộ hóa HƯỚNG NHÌN VÀ QUAY MẶT (Visual Facing)
        // Dùng LateUpdate để đè lên các thay đổi scale từ PlayerMovement.Turn() (nếu có)
        Vector3 lowerScale = _lowerAnimator.transform.localScale;

        if (_isHoldingGun && _upperBodyObject.activeSelf)
        {
            // KHI CÓ SÚNG (VÀ ĐANG HIỆN): Ép phần thân dưới (chân) quay theo hướng súng (chuột)
            // để tránh hiện tượng vặn xoắn, bất kể PlayerMovement đang đi hướng nào.
            float upperSign = Mathf.Sign(_upperBodyObject.transform.localScale.x);
            lowerScale.x = Mathf.Abs(lowerScale.x) * upperSign;
        }
        else
        {
            // KHI CẤT SÚNG: Trả phần thân dưới quay theo hướng vật lý (IsFacingRight của PlayerMovement)
            float moveSign = _movement.IsFacingRight ? 1f : -1f;

            // MẸO VISUAL: Khi vừa búng tường, vật lý đã quay mặt ra ngoài,
            // nhưng ta muốn giữ dáng "đạp tường" hướng vào trong tường.
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
        bool isShooting = Input.GetMouseButton(0);
        
        if (isShooting)
        {
            _lastShootInputTime = Time.time;
        }

        // Kiểm tra xem người chơi có đang trong trạng thái "Rút súng" không
        bool isGrounded = _movement.CurrentState == PlayerMovement.PlayerState.Grounded;
        
        if (isGrounded)
        {
            // Trên mặt đất: giữ súng thêm một lúc sau khi nhả chuột
            _isHoldingGun = Time.time - _lastShootInputTime <= _keepGunOutDuration;
        }
        else
        {
            // Trên không: chỉ rút súng khi đang nhấn/giữ chuột
            _isHoldingGun = isShooting;
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
            bool isAimingRight = _upperBodyObject.transform.localScale.x > 0;
            
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
            case PlayerMovement.PlayerState.Jumping:
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
                    
                // Spawn khói nhảy (One-shot) nếu đang Jumping
                if (newState == PlayerMovement.PlayerState.Jumping && _jumpVfxPrefab != null)
                {
                    _jumpVfxPrefab.Spawn(_movement.GroundCheckPoint.position, Quaternion.identity);
                }
                break;
                
            case PlayerMovement.PlayerState.WallJumping:
                PlayAnim("Duoi-TruotTuong");
                break;
                
            case PlayerMovement.PlayerState.Dashing:
                PlayAnim("Duoi-Dash");
                break;
        }
    }

    [Header("State Tracking")]
    [Tooltip("Tên animation hiện tại đang được yêu cầu chạy")]
    [SerializeField, ReadOnly] private string _currentAnim;
    
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
