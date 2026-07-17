using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Kéo Animator của phần Dưới (chân/fullbody) vào đây")]
    [SerializeField] private Animator _lowerAnimator;
    [Tooltip("Kéo Transform của phần Trên (UpperBody cầm súng) vào đây để bật/tắt")]
    [SerializeField] private GameObject _upperBodyObject;
    
    [Header("Settings")]
    [Tooltip("Thời gian giữ súng trên tay sau khi nhả chuột (giây)")]
    [SerializeField] private float _keepGunOutDuration = 1.5f;

    private PlayerMovement _movement;
    private PlayerAttack _attack;

    // Cache lại các parameter hash để tối ưu hiệu năng (Zero GC)
    private static readonly int VelocityYKey = Animator.StringToHash("VelocityY");
    private static readonly int RunSpeedKey = Animator.StringToHash("RunSpeed");

    // Hỗ trợ logic cất súng
    private float _lastShootInputTime = -999f;
    private bool _isHoldingGun;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _attack = GetComponent<PlayerAttack>();
    }

    private void Start()
    {
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
                                     (state == PlayerMovement.PlayerState.Sliding);
        
        bool shouldShowUpperBody = _isHoldingGun && !isDoingFullBodyAction;
        if (_upperBodyObject.activeSelf != shouldShowUpperBody)
        {
            _upperBodyObject.SetActive(shouldShowUpperBody);
        }


        if (state == PlayerMovement.PlayerState.Grounded)
        {
            // Kết hợp cả Input và Velocity để giải quyết triệt để lỗi Moonwalk và lỗi trượt Move
            bool isMoving = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f && Mathf.Abs(_movement.RB.linearVelocity.x) > 0.1f;
            if (_isHoldingGun)
                PlayAnim(isMoving ? "ThanDuoi-dichuyen" : "ThanDuoi-dungban");
            else
                PlayAnim(isMoving ? "Duoi-move" : "Duoi-ide");
        }
        else if (state == PlayerMovement.PlayerState.Sliding)
        {
            if (Input.GetAxisRaw("Vertical") > 0)
                PlayAnim("Duoi-leotuong");
            else
                PlayAnim("Duoi-TruotTuong");
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
                // Vừa nhả chuột giữa không trung -> Trả lại animation gốc!
                if (state == PlayerMovement.PlayerState.WallJumping)
                    PlayAnim("Duoi-TruotTuong");
                else 
                    PlayAnim("Nhay");
            }
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

        bool isMoving = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f && Mathf.Abs(_movement.RB.linearVelocity.x) > 0.1f;

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
                if (_isHoldingGun)
                {
                    bool isMoving = Mathf.Abs(_movement.RB.linearVelocity.x) > 0.1f;
                    PlayAnim(isMoving ? "ThanDuoi-dichuyen" : "ThanDuoi-dungban");
                }
                else
                    PlayAnim("Duoi-TruotTuong");
                break;
                
            case PlayerMovement.PlayerState.Dashing:
                PlayAnim("Duoi-Dash");
                break;
        }
    }

    private string _currentAnim;
    
    /// <summary>
    /// Hàm helper để gọi Animator.Play mà không làm reset frame nếu anim đang chạy
    /// </summary>
    private void PlayAnim(string animName)
    {
        if (_currentAnim == animName) return;
        
        Debug.Log($"[PlayAnim] Yêu cầu chuyển sang: '{animName}'");
        _lowerAnimator.Play(animName);
        _currentAnim = animName;
    }
}
