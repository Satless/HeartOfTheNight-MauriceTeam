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

    // Cache state hiện tại
    private PlayerMovement.PlayerState _lastState;
    
    // Hỗ trợ logic cất súng
    private float _lastShootInputTime;
    private bool _isHoldingGun;

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _attack = GetComponent<PlayerAttack>();
    }

    private void Start()
    {
        _lastState = _movement.CurrentState;
    }

    private void Update()
    {
        UpdateGunState();
        HandleBlendTreeParams();
        HandleMoonwalk();
        HandleStateAnimations();
    }

    private void LateUpdate()
    {
        // Đồng bộ hóa HƯỚNG NHÌN VÀ QUAY MẶT (Visual Facing)
        // Dùng LateUpdate để đè lên các thay đổi scale từ PlayerMovement.Turn() (nếu có)
        Vector3 lowerScale = _lowerAnimator.transform.localScale;

        if (_isHoldingGun)
        {
            // KHI CÓ SÚNG: Ép phần thân dưới (chân) quay theo hướng súng (chuột)
            // để tránh hiện tượng vặn xoắn, bất kể PlayerMovement đang đi hướng nào.
            float upperSign = Mathf.Sign(_upperBodyObject.transform.localScale.x);
            lowerScale.x = Mathf.Abs(lowerScale.x) * upperSign;
        }
        else
        {
            // KHI CẤT SÚNG: Trả phần thân dưới quay theo hướng vật lý (IsFacingRight của PlayerMovement)
            float moveSign = _movement.IsFacingRight ? 1f : -1f;
            lowerScale.x = Mathf.Abs(lowerScale.x) * moveSign;
        }

        _lowerAnimator.transform.localScale = lowerScale;
    }

    private void UpdateGunState()
    {
        // 1. XỬ LÝ LOGIC HIỆN/ẨN THÂN TRÊN CẦM SÚNG
        if (Input.GetMouseButton(0))
        {
            _lastShootInputTime = Time.time;
        }

        // Kiểm tra xem người chơi có đang trong trạng thái "Rút súng" không
        _isHoldingGun = Time.time - _lastShootInputTime <= _keepGunOutDuration;
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

        bool isMoving = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f;

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

    private void HandleStateAnimations()
    {
        var state = _movement.CurrentState;

        // Các hành động full-body bắt buộc cất súng
        bool isDoingFullBodyAction = (state == PlayerMovement.PlayerState.Dashing) || 
                                     (state == PlayerMovement.PlayerState.Sliding);
        
        // Chỉ hiện thân trên khi: Đang rút súng VÀ Không làm hành động full-body
        bool shouldShowUpperBody = _isHoldingGun && !isDoingFullBodyAction;

        if (_upperBodyObject.activeSelf != shouldShowUpperBody)
        {
            _upperBodyObject.SetActive(shouldShowUpperBody);
        }

        // --- KHÔNG DÙNG MŨI TÊN - GỌI TRỰC TIẾP QUA CODE ---
        if (state != _lastState || state == PlayerMovement.PlayerState.Grounded)
        {
            switch (state)
            {
                case PlayerMovement.PlayerState.Grounded:
                    bool isMoving = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f;
                    
                    if (_isHoldingGun)
                    {
                        // Đang cầm súng
                        if (isMoving)
                            _lowerAnimator.Play("ThanDuoi-dichuyen");
                        else
                            _lowerAnimator.Play("ThanDuoi-dungban");
                    }
                    else
                    {
                        // Đã cất súng (Full body)
                        if (isMoving)
                            _lowerAnimator.Play("Duoi-move");
                        else
                            _lowerAnimator.Play("Duoi-ide");
                    }
                    break;

                case PlayerMovement.PlayerState.Jumping:
                case PlayerMovement.PlayerState.Falling:
                case PlayerMovement.PlayerState.WallJumping:
                    // Chuyển quyền điều khiển cho Blend Tree tên "Nhay"
                    _lowerAnimator.Play("Nhay");
                    break;

                case PlayerMovement.PlayerState.Dashing:
                    _lowerAnimator.Play("Luoi-dash");
                    break;

                case PlayerMovement.PlayerState.Sliding:
                    // Phân biệt trượt hay leo dựa vào vận tốc Y
                    if (_movement.RB.linearVelocity.y > 0.1f)
                        _lowerAnimator.Play("Duoi-leotuong");
                    else
                        _lowerAnimator.Play("Duoi-TruotTuong");
                    break;
            }
        }

        _lastState = state;
    }
}
