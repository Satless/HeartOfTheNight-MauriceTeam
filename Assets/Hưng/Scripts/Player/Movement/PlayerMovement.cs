/*
	Tạo bởi @DawnosaurDev tại youtube.com/c/DawnosaurStudios
	Refactored theo quy tắc dự án:
	  - FSM: PlayerState enum + TransitionToState() thay thế bool lồng chéo
	  - Zero GC: cache WaitForSeconds trong Awake()
	  - Data-Driven: mọi thông số đọc từ ScriptableObject (PlayerData)
	Logic vật lý và gameplay giữ nguyên 100%.
 */

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HeartOfTheNight.Player
{
    public class PlayerMovement : MonoBehaviour, INhanKnockback
    {
	//Scriptable object chứa tất cả các thông số di chuyển — không hardcode
	[Tooltip("Kéo thẳng ScriptableObject PlayerData vào đây")]
	public PlayerData Data;


	#region COMPONENTS
    public Rigidbody2D RB { get; private set; }
	private PlayerAnimation _animation;
	#endregion

	#region FSM
	// Trạng thái chính của player — mọi thay đổi đi qua TransitionToState()
	// Nghiêm cấm set CurrentState trực tiếp từ ngoài method này
	public enum PlayerState
	{
		Grounded,
		Jumping,
		WallJumping,
		Falling,
		Dashing,
		Sliding,
		DroppingThrough,
		LedgeClimbing,
		KnockedBack
	}

	public PlayerState CurrentState { get; private set; }

	/// <summary>
	/// Điểm duy nhất thay đổi trạng thái. Xử lý OnExit state cũ và OnEnter state mới.
	/// </summary>
	private void TransitionToState(PlayerState newState)
	{
		// --- OnExit state cũ ---
		// (Dashing exit được xử lý trong coroutine StartDash, không cần ở đây)

		// Debug.Log($"State: {CurrentState} -> {newState}");
		CurrentState = newState;
		_animation?.OnStateChanged(newState);

		// --- OnEnter state mới ---
		switch (newState)
		{
			case PlayerState.Grounded:
				_isJumpCut = false;
				_isJumpFalling = false;
				_bonusJumpsLeft = Data.bonusJumpAmount;
				break;

			case PlayerState.Jumping:
				_isJumpCut = false;
				_isJumpFalling = false;
				break;

			case PlayerState.WallJumping:
				_isJumpCut = false;
				_isJumpFalling = false;
				_wallJumpStartTime = Time.time;
				break;

			case PlayerState.Falling:
				_isJumpFalling = true;
				break;

			case PlayerState.Dashing:
				_isJumpCut = false;
				StartCoroutine(nameof(StartDash), _lastDashDir);
				break;

			case PlayerState.Sliding:
				// Gravity = 0 được xử lý trong HandleGravity()
				break;

			case PlayerState.DroppingThrough:
				LastOnGroundTime = 0;
				StartCoroutine(nameof(DropThroughRoutine), _ignoredPlatform);
				break;

			case PlayerState.LedgeClimbing:
				_isJumpCut = false;
				_isJumpFalling = false;
				StartCoroutine(nameof(LedgeClimbRoutine));
				break;
		}
	}
	#endregion

	#region STATE PARAMETERS
	// Property public chỉ đọc — wrapper cho hệ thống ngoài (UI, Audio...)
	public bool IsFacingRight { get; private set; }
	public bool IsJumping => CurrentState == PlayerState.Jumping;
	public bool IsWallJumping => CurrentState == PlayerState.WallJumping;
	public bool IsWallJumpLocked => IsWallJumping && Time.time - _wallJumpStartTime < Data.wallJumpTime;
	public bool IsDashing => CurrentState == PlayerState.Dashing;
	public bool IsSliding => CurrentState == PlayerState.Sliding;
	public bool IsLedgeClimbing => CurrentState == PlayerState.LedgeClimbing;

        #region BUFF MULTIPLIERS
        [Header("Buff Multipliers (Item Hệ Thống)")]
        [Tooltip("Hệ số nhân tốc độ chạy (Ăn giày tăng tốc)")]
        public float moveSpeedMultiplier = 1f;
        [Tooltip("Hệ số nhân lực nhảy (Ăn lò xo)")]
        public float jumpForceMultiplier = 1f;
        #endregion

        // Timers coyote time & input buffer
        public float LastOnGroundTime { get; private set; }
	public float LastOnWallTime { get; private set; }
	public float LastOnWallRightTime { get; private set; }
	public float LastOnWallLeftTime { get; private set; }

	// Cờ nội bộ jump — không phải trạng thái chính, chỉ ảnh hưởng gravity
	[Header("Debug Tracking")]
	[Tooltip("Đánh dấu người chơi nhả phím nhảy sớm (để tăng trọng lực kéo xuống)")]
	[SerializeField, ReadOnly] private bool _isJumpCut;
	[Tooltip("Đang rơi xuống sau khi nhảy")]
	[SerializeField, ReadOnly] private bool _isJumpFalling;

	// Nhảy tường
	[Tooltip("Thời điểm bắt đầu Wall Jump (dùng để tính thời gian khóa bẻ lái ngang)")]
	[SerializeField, ReadOnly] private float _wallJumpStartTime;
	public float WallJumpStartTime => _wallJumpStartTime;
	[Tooltip("Hướng bật tường (-1 hoặc 1)")]
	[SerializeField, ReadOnly] private int _lastWallJumpDir;

	// Lướt
	[Tooltip("Số lượt Dash còn lại")]
	[SerializeField, ReadOnly] private int _dashesLeft;
	[Tooltip("Cờ báo hiệu đang chạy Coroutine hồi Dash")]
	[SerializeField, ReadOnly] private bool _dashRefilling;
	[Tooltip("Vector hướng Dash vừa kích hoạt")]
	[SerializeField, ReadOnly] private Vector2 _lastDashDir;
	[Tooltip("Đang trong giai đoạn Dash Attack (lao đi với tốc độ cao, không trọng lực)")]
	[SerializeField, ReadOnly] private bool _isDashAttacking;

	// Nhảy đôi
	[Tooltip("Số lần nhảy đôi trên không còn lại")]
	[SerializeField, ReadOnly] private int _bonusJumpsLeft;
	#endregion

	#region INPUT PARAMETERS
	private InputSystem_Actions _input;
	private Vector2 _moveInput;
	public Vector2 MoveInput => _moveInput;

	public float LastPressedJumpTime { get; private set; }
	public float LastPressedDashTime { get; private set; }

	private bool _isPressingJump;
	private bool _isPressingDash;
	/// <summary>Phím nhảy đang được nhấn giữ — cho Debug Panel.</summary>
	public bool IsPressingJump => _isPressingJump;
	/// <summary>Phím dash đang được nhấn giữ — cho Debug Panel.</summary>
	public bool IsPressingDash => _isPressingDash;
	#endregion

	#region CHECK PARAMETERS
	[Header("Visuals")]
	[Tooltip("Kéo child phần thân dưới (chân) vào đây (Duoi)")]
	[SerializeField] private Transform _lowerBodyVisual;

	[Header("Checks")] 
	[Tooltip("Kéo child kiểm tra dưới chân")]
	[SerializeField] private Transform _groundCheckPoint;
	public Transform GroundCheckPoint => _groundCheckPoint;
	[Tooltip("Kích thước hộp kiểm tra dưới chân, dùng physic thay vì collider để tránh lỗi)")]
	[SerializeField] private Vector2 _groundCheckSize = new Vector2(0.49f, 0.03f);
	[Space(5)]
	[Tooltip("Kéo child kiểm tra tường bên phải")]
	[SerializeField] private Transform _rightWallCheckPoint;
	[Tooltip("Kéo child kiểm tra tường bên trái")]
	[SerializeField] private Transform _leftWallCheckPoint;
	[Tooltip("Kích thước hộp kiểm tra tường, dùng physic thay vì collider để tránh lỗi)")]
	[SerializeField] private Vector2 _wallCheckSize = new Vector2(0.5f, 1f);

	[Header("Ledge Climb (Input-driven)")]
	[Tooltip("Chieu dai tia raycast ngang de do tuong (phai dai hon khoang cach tu wallCheckPoint den mep tuong)")]
	[SerializeField] private float _ledgeRayLength = 0.6f;
	[Tooltip("Offset Y tu wallCheckPoint len phia dau de ban tia dau (tia nay phai KHONG cham tuong de xac nhan da qua mep)")]
	[SerializeField] private float _ledgeHeadRayOffsetY = 0.7f;
	[Tooltip("Thoi gian dich chuyen muot len tren mep tuong (giay)")]
	[SerializeField] private float _ledgeClimbDuration = 0.15f;
	[Tooltip("Khoang cach day ngang qua mep tuong khi ledge climb (Unity unit)")]
	[SerializeField] private float _ledgeClimbHorizontalPush = 0.5f;
	[Tooltip("Khoang cach day len tren khi ledge climb (Unity unit)")]
	[SerializeField] private float _ledgeClimbVerticalPush = 0.8f;

    

    #endregion

    #region LAYERS
    [Header("Layers")]
	[Tooltip("Chọn layer kiểm tra mặt đất")]
	[SerializeField] private LayerMask _groundLayer;
	#endregion

	#region GC CACHE
	// Cache WaitForSeconds để tránh tạo object mới mỗi lần RefillDash chạy
	private WaitForSeconds _dashRefillWait;
	private WaitForSecondsRealtime _sleepWait;
	#endregion

	#region ONEWAY PLATFORM CACHE
	private Collider2D[] _playerColliders;
	private Collider2D _ignoredPlatform;
	private Collider2D[] _overlapResults = new Collider2D[10];
	private ContactFilter2D _groundFilter;
	private Collider2D _hurtboxCollider; // Cache Hurtbox để tắt bật khi lướt
	#endregion
			
	#region SFX - Huy
	private float _footstepTimer;
    private float _slideSoundTimer; // thời gian để chạy sfx
	private float _wallClimbTimer;
	private bool _wasGroundedLastFrame;
    #endregion


        // -------------------------------------------------------------------------

			private void Awake()
		{
			RB = GetComponent<Rigidbody2D>();
			_animation = GetComponent<PlayerAnimation>();
			_playerColliders = GetComponentsInChildren<Collider2D>();

			// Tự động tìm Hurtbox (để làm cơ chế bất tử khi lướt)
			foreach (var col in _playerColliders)
			{
				if (col.isTrigger && col.gameObject.name == "Hurtbox")
				{
					_hurtboxCollider = col;
					break;
				}
			}

			// Cache coroutine wait — Zero GC Alloc
			_dashRefillWait = new WaitForSeconds(Data.dashRefillTime);
			_sleepWait = new WaitForSecondsRealtime(Data.dashSleepTime);  //Phải dùng Realtime vì timeScale = 0
		
			_groundFilter.useTriggers = false;
			_groundFilter.SetLayerMask(_groundLayer);
			_groundFilter.useLayerMask = true;
		
			_input = new InputSystem_Actions();

			_input.Player.Jump.started += (InputAction.CallbackContext context) => 
			{
				_isPressingJump = true;
				if (_moveInput.y < -0.1f && TryGetOneWayPlatformBelow(out Collider2D platform))
				{
					LastPressedJumpTime = 0; // Xóa buffer nhảy, tránh kẹt nhảy đôi
					_ignoredPlatform = platform;
					TransitionToState(PlayerState.DroppingThrough);
				}
				else
				{
					OnJumpInput();
				}
			};
			_input.Player.Jump.canceled += (InputAction.CallbackContext context) => 
			{
				_isPressingJump = false;
				OnJumpUpInput();
			};
		
			_input.Player.Dash.started += (InputAction.CallbackContext context) => 
			{
				_isPressingDash = true;
				OnDashInput();
			};

			_input.Player.Dash.canceled += (InputAction.CallbackContext context) => 
			{
				_isPressingDash = false;
			};
		}

	private void OnEnable()
	{
		GameplayEvents.OnGameplayInputEnabled += HandleGameplayInputEnabled;
		HandleGameplayInputEnabled(GameplayEvents.InputEnabled);
	}

	private void OnDisable()
	{
		GameplayEvents.OnGameplayInputEnabled -= HandleGameplayInputEnabled;
		_input.Disable();
	}

	private void HandleGameplayInputEnabled(bool inputEnabled)
	{
		if (_input == null)
			return;
		if (inputEnabled)
			_input.Enable();
		else
			_input.Disable();
	}

	private void Start()
	{
		SetGravityScale(Data.gravityScale);
		IsFacingRight = true;
		CurrentState = PlayerState.Grounded;
	}

	private void Update()
	{
		HandleTimers();
		
		if (CurrentState == PlayerState.KnockedBack)
		{
			// Vẫn kiểm tra va chạm tường và cho phép bám tường để hủy Knockback sớm
			HandleCollisionChecks();
			HandleSlideChecks();
			return; 
		}
		
		HandleInput();
		HandleCollisionChecks();
		HandleLedgeChecks();
		HandleJumpChecks();
		HandleDashChecks();
		HandleSlideChecks();
		HandleGravity();


		bool isGrounded = LastOnGroundTime > 0;

		// BẮT SỰ KIỆN ĐÁP ĐẤT: Frame trước đang ở trên không (false), frame này đã chạm đất (true)
		if (isGrounded && !_wasGroundedLastFrame)
			{
				// Phát tiếng đáp đất 3D tại vị trí chân/người chơi
				AudioEvents.TriggerSound3D("Player", "Land", "n", transform.position);
			}

		// Cập nhật lại trạng thái cho frame tiếp theo
    	_wasGroundedLastFrame = isGrounded;
    }

    private void FixedUpdate()
	{
		if (CurrentState == PlayerState.KnockedBack) return;
		
		HandleRun();

		if (IsSliding)
		{
			if (_moveInput.y > 0)
				WallClimb();
			else
				Slide();
		}
    }

	// -------------------------------------------------------------------------

	#region UPDATE SUB-METHODS
	/// <summary>Đếm ngược toàn bộ timer mỗi frame.</summary>
	private void HandleTimers()
	{
		LastOnGroundTime -= Time.deltaTime;
		LastOnWallTime -= Time.deltaTime;
		LastOnWallRightTime -= Time.deltaTime;
		LastOnWallLeftTime -= Time.deltaTime;
		LastPressedJumpTime -= Time.deltaTime;
		LastPressedDashTime -= Time.deltaTime;
	}

	/// <summary>Đọc input trục di chuyển mỗi frame.</summary>
	private void HandleInput()
	{
		_moveInput = _input.Player.Move.ReadValue<Vector2>();

		if (!IsDashing && _moveInput.x != 0)
			CheckDirectionToFace(_moveInput.x > 0);
	}

	/// <summary>Kiểm tra va chạm ground/wall, cập nhật timer coyote.</summary>
	private void HandleCollisionChecks()
	{
		if (IsJumping || IsLedgeClimbing) return;

		// Ground check — skip toàn bộ khi đang dash để tránh HandleJumpChecks()
		// ép state về Grounded giữa chừng trong khi coroutine StartDash vẫn chạy.
		if (!IsDashing)
		{
			if (IsSolidGround(_groundCheckPoint.position, _groundCheckSize))
				LastOnGroundTime = Data.coyoteTime;
		}

		// Wall check — chỉ skip ở Phase 1 (dash attack ghi đè velocity mỗi frame),
		// cho phép cập nhật timer tường ở Phase 2 để coroutine có thể thoát sớm khi chạm tường.
		if (!_isDashAttacking)
		{
			// Right wall check
			if (IsSolidWall(_rightWallCheckPoint.position, _wallCheckSize) && !IsWallJumping)
				LastOnWallRightTime = Data.coyoteTime;

			// Left wall check
			if (IsSolidWall(_leftWallCheckPoint.position, _wallCheckSize) && !IsWallJumping)
				LastOnWallLeftTime = Data.coyoteTime;

			LastOnWallTime = Mathf.Max(LastOnWallLeftTime, LastOnWallRightTime);
		}
	}

	/// <summary>
	/// Leo mep chu dong: giu huong vao tuong (A/D) + giu len (W) + raycast xac nhan co mep phia tren.
	/// Input-driven — khong phu thuoc IsLowerHalfTouchingWall() hay dau cua velocity, tranh giat trang thai o goc mep.
	/// </summary>
	private void HandleLedgeChecks()
	{
		if (IsLedgeClimbing || IsDashing || IsWallJumping) return;
		if (LastOnGroundTime > 0) return; // Dang dung dat thi khong can leo mep

		bool pressingUp = _moveInput.y > 0.1f;
		bool pressingIntoRightWall = LastOnWallRightTime > 0 && _moveInput.x > 0.1f;
		bool pressingIntoLeftWall  = LastOnWallLeftTime  > 0 && _moveInput.x < -0.1f;

		if (pressingUp && (pressingIntoRightWall || pressingIntoLeftWall) && DetectLedge())
			TransitionToState(PlayerState.LedgeClimbing);
	}

	/// <summary>Quản lý các điều kiện chuyển trạng thái liên quan đến nhảy.</summary>
	private void HandleJumpChecks()
	{
		if (IsLedgeClimbing) return;

		// Jumping → Falling khi bắt đầu rơi
		if (IsJumping && RB.linearVelocity.y < 0)
			TransitionToState(PlayerState.Falling);

		// WallJumping hết thời gian
		if (IsWallJumping && Time.time - _wallJumpStartTime > Data.wallJumpTime)
			TransitionToState(PlayerState.Falling);

		// Chạm đất → reset về Grounded
		if (LastOnGroundTime > 0 && !IsJumping && !IsWallJumping && CurrentState != PlayerState.Grounded)
			TransitionToState(PlayerState.Grounded);
		
		// Bước ra khỏi mép (Coyote Fall): đang Grounded, hết thời gian châm chước, và đang rơi
		// FSM giữ nguyên Grounded trong suốt coyote time để tránh jitter.
		// Animation rơi được xử lý riêng bên PlayerAnimation (visual-only).
		if (CurrentState == PlayerState.Grounded && LastOnGroundTime <= 0 && RB.linearVelocity.y < 0)
			TransitionToState(PlayerState.Falling);

		if (IsDashing) return;

		// Thực hiện nhảy thường
		if (CanJump() && LastPressedJumpTime > 0)
		{
			TransitionToState(PlayerState.Jumping);
			Jump();
		}
		// Nhảy tường
		else if (CanWallJump() && LastPressedJumpTime > 0)
		{
			_lastWallJumpDir = (LastOnWallRightTime > 0) ? -1 : 1;
			TransitionToState(PlayerState.WallJumping);
			WallJump(_lastWallJumpDir);
		}
		// Nhảy đôi
		else if (CanDoubleJump() && LastPressedJumpTime > 0)
		{
			TransitionToState(PlayerState.Jumping);
			_bonusJumpsLeft--;
			Jump();
		}
	}

	/// <summary>Kiểm tra và kích hoạt dash.</summary>
	private void HandleDashChecks() 
	{
		if (IsLedgeClimbing) return;

		if (!CanDash() || LastPressedDashTime <= 0) return;

		//Đóng băng game một khoảnh khắc để đọc input hướng chính xác
		Sleep();

		// Dùng Coyote Time của tường thay vì IsSliding, vì nếu người chơi vừa bấm nút lùi ra khỏi tường
		// thì IsSliding sẽ ngay lập tức bị false, làm mất khả năng lướt bật ngược.
		bool nearWall = (LastOnWallRightTime > 0 || LastOnWallLeftTime > 0) && LastOnGroundTime <= 0;

		if (nearWall && _moveInput.y != 0 && !Data.horizontalDashOnly)
		{
			// Lướt dọc khi bám tường (lên/xuống tùy input). Bị horizontalDashOnly chi phối.
			_lastDashDir = (_moveInput.y > 0) ? Vector2.up : Vector2.down;
		}
		else if (nearWall && Data.allowReverseWallClingDash)
		{
			// Bật: Lướt bật ra xa tường (không bị horizontalDashOnly chi phối)
			_lastDashDir = (LastOnWallRightTime > 0) ? Vector2.left : Vector2.right;
			
			// Bắt buộc quay mặt ra ngoài tường khi lướt bật ngược,
			// bất kể người chơi có đang giữ phím ép vào tường hay không.
			CheckDirectionToFace(_lastDashDir.x > 0);
		}
		else
		{
			_lastDashDir = (_moveInput != Vector2.zero)
				? _moveInput
				: (IsFacingRight ? Vector2.right : Vector2.left);

			// Nếu chỉ cho lướt ngang, loại bỏ thành phần dọc
			if (Data.horizontalDashOnly)
			{
				_lastDashDir.y = 0;

				// Nếu người chơi chỉ giữ lên/xuống → fallback về hướng mặt nhân vật
				if (_lastDashDir.x == 0)
					_lastDashDir.x = IsFacingRight ? 1 : -1;
			}

			// Bảo vệ: Nếu tắt allowReverseWallClingDash mà người chơi lỡ tay bấm Dash
			// trong lúc hướng lướt đâm thẳng vào tường -> Hủy lướt để khỏi phí lượt.
			if (nearWall)
			{
				if ((LastOnWallRightTime > 0 && _lastDashDir.x > 0) || (LastOnWallLeftTime > 0 && _lastDashDir.x < 0))
				{
				    LastPressedDashTime = 0; // Xóa buffer phím lướt
				    return; // Thoát hàm luôn, không tốn lượt dash
			    }
		    }
	    }

	    TransitionToState(PlayerState.Dashing);
    }

	/// <summary>Kiểm tra điều kiện slide wall.</summary>
	private void HandleSlideChecks()
	{
		if (IsLedgeClimbing) return;

		bool shouldSlide = CanSlide() && ((LastOnWallLeftTime > 0 && _moveInput.x < 0) || (LastOnWallRightTime > 0 && _moveInput.x > 0)); //Dễ hiểu thì biến bool ở đây sẽ là true hoặc false nếu sau dấu = đúng hoặc sai, đây là if lồng trong bool.

		if (shouldSlide && CurrentState != PlayerState.Sliding)
			TransitionToState(PlayerState.Sliding);
		else if (!shouldSlide && CurrentState == PlayerState.Sliding)
		{
			// NẾU shouldSlide = true (được phép trượt) MÀ lại bị rớt xuống dòng này (do nhân vật đang trượt sẵn trên tường), thì lệnh else if này sẽ tự động bị bỏ qua (kết thúc hàm) vì nó yêu cầu shouldSlide = false (!shouldSlide).
			
			// Xử lý chống giật mép tường (Ledge Jitter Fix):
			// Nếu mất slide do phần hông/chân đã vượt qua mép tường, và nhân vật đang trèo lên
			if (!IsLowerHalfTouchingWall() && RB.linearVelocity.y > 0)
			{
				// Trợ lực nảy nhẹ lên trên để dứt điểm quá trình trèo, tránh rơi lại vào tường gây giật
				RB.linearVelocity = new Vector2(RB.linearVelocity.x, Data.wallClimbSpeed);
			}

			if (LastOnGroundTime > 0)
				TransitionToState(PlayerState.Grounded);
			else
				TransitionToState(PlayerState.Falling);
		}
	}

	/// <summary>
	/// Điều chỉnh gravity mỗi frame dựa trên state hiện tại.
	/// Gravity không được quản lý trong TransitionToState vì nó cần cập nhật liên tục.
	/// </summary>
	private void HandleGravity()
	{
		if (IsLedgeClimbing) return; // Gravity duoc quan ly boi LedgeClimbRoutine

		if (_isDashAttacking)
		{
			SetGravityScale(0);
			return;
		}

		if (IsSliding)
		{
			SetGravityScale(0);
		}
		else if (RB.linearVelocity.y < 0 && _moveInput.y < 0)
		{
			//Rơi nhanh — giữ phím xuống khi đang rơi
			SetGravityScale(Data.gravityScale * Data.fastFallGravityMult);
			RB.linearVelocity = new Vector2(RB.linearVelocity.x, Mathf.Max(RB.linearVelocity.y, -Data.maxFastFallSpeed));
		}
		else if (_isJumpCut)
		{
			//Thả nút nhảy giữa chừng
			SetGravityScale(Data.gravityScale * Data.jumpCutGravityMult);
			RB.linearVelocity = new Vector2(RB.linearVelocity.x, Mathf.Max(RB.linearVelocity.y, -Data.maxFallSpeed));
		}
		else if ((IsJumping || IsWallJumping || _isJumpFalling) && Mathf.Abs(RB.linearVelocity.y) < Data.jumpHangTimeThreshold)
		{
			//Hang time ở đỉnh jump — giảm gravity để cảm giác nhảy tự nhiên hơn
			SetGravityScale(Data.gravityScale * Data.jumpHangGravityMult);
		}
		else if (RB.linearVelocity.y < 0)
		{
			//Rơi bình thường
			SetGravityScale(Data.gravityScale * Data.fallGravityMult);
			RB.linearVelocity = new Vector2(RB.linearVelocity.x, Mathf.Max(RB.linearVelocity.y, -Data.maxFallSpeed));
		}
		else
		{
			//Trọng lực mặc định
			SetGravityScale(Data.gravityScale);
		}
	}
	#endregion

	// -------------------------------------------------------------------------

	#region FIXED UPDATE SUB-METHODS
	/// <summary>Xử lý di chuyển ngang, gọi từ FixedUpdate.</summary>
	private void HandleRun()
	{
		if (IsLedgeClimbing) return; // MovePosition trong coroutine xu ly, khong can thiep

		if (IsDashing)
		{
			if (!_isDashAttacking) // Chỉ trả lại một chút quyền bẻ lái ở Phase 2. Ở Phase 1 Coroutine đã ép chết vận tốc.
				Run(Data.dashEndRunLerp);
			return;
		}

		Run(IsWallJumping ? Data.wallJumpRunLerp : 1f); // Đang nhảy tường thì trả một chút quyền điều khiển hướng, còn không thì trả full quyền điều khiển hướng
	}
	#endregion

	// -------------------------------------------------------------------------

    #region INPUT CALLBACKS
    public void OnJumpInput()
	{
		LastPressedJumpTime = Data.jumpInputBufferTime;
	}

	public void OnJumpUpInput()
	{
		if (CanJumpCut() || CanWallJumpCut())
			_isJumpCut = true;
	}

	public void OnDashInput()
	{
		LastPressedDashTime = Data.dashInputBufferTime;
	}
    #endregion

	// -------------------------------------------------------------------------

    #region GENERAL METHODS
    /// <summary>Gán hệ số trọng lực cho Rigidbody. Dùng để tăng/giảm trọng lực theo ngữ cảnh
	/// (vd: giảm khi ở đỉnh nhảy, tăng khi rơi, tắt hẳn khi dash).</summary>
	public void SetGravityScale(float scale)
	{
		RB.gravityScale = scale;
	}

	/// <summary>Đóng băng game trong khoảnh khắc (hiệu ứng "Hit Stop / Freeze Frame").
	/// Gọi trước khi Dash để người chơi có thời gian chọn hướng lướt chính xác.</summary>
	private void Sleep()
    {
		if (!GameplayEvents.InputEnabled)
			return;
		StartCoroutine(nameof(PerformSleep));
    }

	private IEnumerator PerformSleep()
    {
		Time.timeScale = 0;
		yield return _sleepWait;
		var hp = GetComponent<PlayerHealth>();
		// Pause + Level Complete đều câm input qua event — đừng bật timeScale khi overlay đang đứng game.
		if ((hp == null || !hp.IsDead) && GameplayEvents.InputEnabled)
			Time.timeScale = 1;
	}
    #endregion

	// -------------------------------------------------------------------------

    #region RUN METHODS
    private void Run(float lerpAmount)
	{
            // Tốc độ mục tiêu = hướng input × tốc độ chạy tối đa (vd: 1 × 15 = 15, -1 × 15 = -15, 0 × 15 = 0)
            float targetSpeed = _moveInput.x * (Data.runMaxSpeed * moveSpeedMultiplier);
            // Nội suy giữa vận tốc hiện tại và tốc độ mục tiêu.
            // lerpAmount = 1 → nhảy thẳng sang targetSpeed (full quyền điều khiển).
            // lerpAmount = 0 → giữ nguyên vận tốc hiện tại (không cho bẻ lái).
            // lerpAmount ở giữa (vd: 0.13, 0.4) → chỉ trả một phần quyền bẻ lái (dùng cho dash end, wall jump).
            targetSpeed = Mathf.Lerp(RB.linearVelocity.x, targetSpeed, lerpAmount);

		// Chọn tỷ lệ gia tốc: đang giữ phím → dùng Accel (tăng tốc), buông phím → dùng Deccel (phanh).
		// Trên không thì nhân thêm hệ số accelInAir/deccelInAir để giảm kiểm soát trên không trung.
		float accelRate;
		if (LastOnGroundTime > 0)
			accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount : Data.runDeccelAmount;
		else
			accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? Data.runAccelAmount * Data.accelInAir : Data.runDeccelAmount * Data.deccelInAir;

		// Buff ở đỉnh nhảy (hang time): khi gần đỉnh, tốc độ dọc ≈ 0 → tăng gia tốc và tốc độ tối đa
		// để người chơi có cảm giác "lơ lửng" và dễ điều khiển hơn ở khoảnh khắc đỉnh nhảy.
		if ((IsJumping || IsWallJumping || _isJumpFalling) && Mathf.Abs(RB.linearVelocity.y) < Data.jumpHangTimeThreshold)
		{
			accelRate *= Data.jumpHangAccelerationMult;
			targetSpeed *= Data.jumpHangMaxSpeedMult;
		}

		// Bảo toàn quán tính — Nếu nhân vật đang bay nhanh hơn tốc độ tối đa (vd: sau Dash, knockback),
		// và người chơi giữ phím cùng hướng bay, thì KHÔNG phanh lại. Chỉ áp dụng trên không.
		if(Data.doConserveMomentum
			&& Mathf.Abs(RB.linearVelocity.x) > Mathf.Abs(targetSpeed) // Tốc độ hiện tại > tốc độ mục tiêu (đang bay nhanh hơn max)
			&& Mathf.Sign(RB.linearVelocity.x) == Mathf.Sign(targetSpeed) // Đang bay cùng hướng với phím giữ
			&& Mathf.Abs(targetSpeed) > 0.01f // Có đang giữ phím di chuyển (trái hoặc phải)
			&& LastOnGroundTime < 0) // Đang trên không (không chạm đất)
			accelRate = 0; // Xóa gia tốc → giữ nguyên vận tốc hiện tại

		float speedDif = targetSpeed - RB.linearVelocity.x; // Chênh lệch giữa tốc độ mong muốn và tốc độ hiện tại
		float movement = speedDif * accelRate; // Lực cần áp dụng = chênh lệch × tỷ lệ gia tốc
		RB.AddForce(movement * Vector2.right, ForceMode2D.Force); // Áp dụng lực ngang lên Rigidbody

		if (LastOnGroundTime > 0 && Mathf.Abs(RB.linearVelocity.x) > 0.5f)
		{
		    _footstepTimer -= Time.deltaTime;
		    if (_footstepTimer <= 0f)
		    {
				//SoundManager.Instance.PlaySound3D("Player", "Run", transform.position);

				AudioEvents.TriggerSound3D("Player", "Move", "n1", transform.position);
                _footstepTimer = 0.35f; 
            }
        }
    }

	private void Turn()
	{
		IsFacingRight = !IsFacingRight;

		if (_lowerBodyVisual != null)
		{
			Vector3 scale = _lowerBodyVisual.localScale; 
			scale.x *= -1;
			_lowerBodyVisual.localScale = scale;
		}
		else
		{
			// Fallback an toàn nếu quên chưa kéo Transform
			Vector3 scale = transform.localScale; 
			scale.x *= -1;
			transform.localScale = scale;
		}
	}
    #endregion

	// -------------------------------------------------------------------------

    #region JUMP METHODS
    private void Jump()
	{
		LastPressedJumpTime = 0;
		LastOnGroundTime = 0;

		//SoundManager.Instance.PlaySound3D("Player", "Jump", transform.position);
		AudioEvents.TriggerSound3D("Player", "Jump", "n", transform.position);

		// Reset đà rơi về 0 → nhảy luôn đạt đúng jumpHeight dù đang rơi nhanh cỡ nào
		RB.linearVelocity = new Vector2(RB.linearVelocity.x, 0f);
            RB.AddForce(Vector2.up * (Data.jumpForce * jumpForceMultiplier), ForceMode2D.Impulse);
        }

	private void WallJump(int dir)
	{
		LastPressedJumpTime = 0;
		LastOnGroundTime = 0;
		LastOnWallRightTime = 0;
		LastOnWallLeftTime = 0;

        //SoundManager.Instance.PlaySound3D("Player", "WallJump", transform.position);
        AudioEvents.TriggerSound3D("Player", "WallJump", "n", transform.position);


        Vector2 force = new Vector2(Data.wallJumpForce.x, Data.wallJumpForce.y);
		force.x *= dir; //Lực ngược chiều tường

		// Giữ nguyên bù vận tốc ngang (chaining tech)
		if (Mathf.Sign(RB.linearVelocity.x) != Mathf.Sign(force.x))
			force.x -= RB.linearVelocity.x;

		// Giới hạn trần vận tốc ngang sau wall jump.
		// Ngăn doConserveMomentum "đóng băng" tốc độ cao mãi khi player nhấn cùng hướng bật tường.
		float targetVelX = RB.linearVelocity.x + force.x;
		targetVelX = Mathf.Clamp(targetVelX, -Data.wallJumpMaxSpeedX, Data.wallJumpMaxSpeedX);
		force.x = targetVelX - RB.linearVelocity.x;

		// Reset đà rơi dọc về 0 để tránh "bắn đại bác"
		RB.linearVelocity = new Vector2(RB.linearVelocity.x, 0f);

		RB.AddForce(force, ForceMode2D.Impulse);

		// Xoay mặt nhân vật theo hướng nhảy tường
		if (Data.doTurnOnWallJump)
			CheckDirectionToFace(dir > 0);
	}
	#endregion

	// -------------------------------------------------------------------------

	#region DASH METHODS
	private IEnumerator StartDash(Vector2 dir)
	{
		LastOnGroundTime = 0;
		LastPressedDashTime = 0;

		//SoundManager.Instance.PlaySound3D("Player","Dash", transform.position);
		AudioEvents.TriggerSound3D("Player", "Dash", "n", transform.position);

		// Nếu bật lockFacingToDashDirection, ép quay mặt đúng theo hướng lướt ngang
		if (Data.lockFacingToDashDirection && Mathf.Abs(dir.x) > 0.1f)
		{
			CheckDirectionToFace(dir.x > 0);
		}

		float startTime = Time.time; // Thời gian bắt đầu giai đoạn 1
		_dashesLeft--;
		_isDashAttacking = true;
		SetGravityScale(0);

		// [I-FRAMES] Tắt Hurtbox ở Giai đoạn 1 (Lướt tốc độ cao)
		if (_hurtboxCollider != null) _hurtboxCollider.enabled = false;

		// Phase 1 — dash attack: giữ vận tốc cố định (tham khảo Celeste)
		while (Time.time - startTime <= Data.dashAttackTime)
		{
			RB.linearVelocity = dir.normalized * Data.dashSpeed;
			yield return null;
		}

		startTime = Time.time; // Thời gian bắt đầu giai đoạn 2
		_isDashAttacking = false;

		// [I-FRAMES] Bật lại Hurtbox ở Giai đoạn 2 (Hãm phanh)
		if (_hurtboxCollider != null) _hurtboxCollider.enabled = true;

		// Phase 2 — dash end: Hãm phanh
		SetGravityScale(Data.gravityScale);
		RB.linearVelocity = Data.dashEndSpeed * dir.normalized;

        //SoundManager.Instance.PlaySound3D("Player", "StopDash", transform.position);
		AudioEvents.TriggerSound3D("Player", "StopDash", "n", transform.position);

        // Phase 2: chờ hết dashEndTime, nhưng thoát sớm nếu chạm tường đúng hướng bám
        while (Time.time - startTime <= Data.dashEndTime)
		{
			// Wall check timer đã được HandleCollisionChecks() cập nhật ở Phase 2
			// (vì !_isDashAttacking). Nếu chạm tường + giữ phím vào tường → bám ngay.
			bool wallClingReady = ((LastOnWallLeftTime > 0 && _moveInput.x < 0) || (LastOnWallRightTime > 0 && _moveInput.x > 0)) && RB.linearVelocity.y <= 0;
			if (wallClingReady)
			{
				TransitionToState(PlayerState.Sliding);
				yield break; // Thoát coroutine, không chạy xuống Falling
			}
			yield return null;
		}

		// Lướt xong mà không chạm tường → Falling.
		// HandleJumpChecks() sẽ tự sửa về Grounded ở frame tiếp nếu đang chạm đất.
		TransitionToState(PlayerState.Falling);
	}

	private IEnumerator RefillDash(int amount)
	{
		_dashRefilling = true;
		yield return _dashRefillWait; // Dùng cache, không tạo object mới — Zero GC
		_dashRefilling = false;
		_dashesLeft = Mathf.Min(Data.dashAmount, _dashesLeft + amount);
	}
	#endregion

	// -------------------------------------------------------------------------

	#region SLIDE & WALL CLIMB METHODS
	private void Slide()
	{
		float speedDif = Data.slideSpeed - RB.linearVelocity.y;	
		float movement = speedDif * Data.slideAccel;
		movement = Mathf.Clamp(movement, -Mathf.Abs(speedDif)  * (1 / Time.fixedDeltaTime), Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime));
		RB.AddForce(movement * Vector2.up);

        _slideSoundTimer -= Time.fixedDeltaTime;
        if (_slideSoundTimer <= 0f)
        {
            //SoundManager.Instance.PlaySound3D("Player", "Slide", transform.position);
			AudioEvents.TriggerSound3D("Player", "Slide", "n", transform.position);
            _slideSoundTimer = 0.2f; // Phát lại sau mỗi 0.2s
        }
    }

	private void WallClimb()
	{
		// Giống Slide() nhưng đẩy lên trên thay vì kéo xuống
		float speedDif = Data.wallClimbSpeed - RB.linearVelocity.y;
		float movement = speedDif * Data.wallClimbAccel;
		movement = Mathf.Clamp(movement, -Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime), Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime));
		RB.AddForce(movement * Vector2.up);

		_wallClimbTimer -= Time.fixedDeltaTime;
         if (_wallClimbTimer <= 0f)
         {
				//SoundManager.Instance.PlaySound3D("Player", "WallClimb", transform.position);
				AudioEvents.TriggerSound3D("Player", "WallClimb", "n", transform.position);
                _wallClimbTimer = 0.3f; // Phát lại sau mỗi 0.2s
         }
        }
    #endregion

	// -------------------------------------------------------------------------

	#region LEDGE CLIMB METHODS
	/// <summary>
	/// Phat hien mep tuong bang 2 tia raycast ngang.
	/// Tia vai: ban tu wallCheckPoint -> phai cham tuong (than con bam).
	/// Tia dau: ban tu wallCheckPoint + offsetY -> phai KHONG cham tuong (dau da nho len khoi mep).
	/// </summary>
	private bool DetectLedge()
	{
		bool wasOnRightWall = LastOnWallRightTime > LastOnWallLeftTime;
		Vector2 rayDir = wasOnRightWall ? Vector2.right : Vector2.left;
		Transform wallCheckPoint = wasOnRightWall ? _rightWallCheckPoint : _leftWallCheckPoint;

		Vector2 shoulderOrigin = (Vector2)wallCheckPoint.position + Vector2.down * (_wallCheckSize.y / 2f);
		RaycastHit2D shoulderHit = Physics2D.Raycast(shoulderOrigin, rayDir, _ledgeRayLength, _groundLayer);

		Vector2 headOrigin = (Vector2)wallCheckPoint.position + Vector2.up * _ledgeHeadRayOffsetY;
		RaycastHit2D headHit = Physics2D.Raycast(headOrigin, rayDir, _ledgeRayLength, _groundLayer);

		return shoulderHit.collider != null && headHit.collider == null;
	}

	/// <summary>
	/// Coroutine dich chuyen nhan vat muot len tren + ngang qua mep tuong.
	/// Trong suot qua trinh: gravity = 0, velocity = 0, cac Handle*Checks khac bi khoa qua guard IsLedgeClimbing.
	/// </summary>
	private IEnumerator LedgeClimbRoutine()
	{
		bool wasOnRightWall = LastOnWallRightTime > LastOnWallLeftTime;
		float pushDirX = wasOnRightWall ? 1f : -1f;

		SetGravityScale(0);
		RB.linearVelocity = Vector2.zero;

		Vector2 startPos = RB.position;
		Vector2 highPos = startPos + new Vector2(pushDirX * _ledgeClimbHorizontalPush, _ledgeClimbVerticalPush);

		// Dò tìm chính xác độ cao của mép tường để không bị lơ lửng
		RaycastHit2D hit = Physics2D.Raycast(highPos, Vector2.down, _ledgeClimbVerticalPush, _groundLayer);
		Vector2 targetPos = highPos;

		if (hit.collider != null)
		{
			// Tính khoảng cách từ tâm nhân vật đến dưới bàn chân
			float footOffset = _groundCheckPoint.position.y - transform.position.y;
			// Đặt nhân vật sao cho bàn chân vừa khít với mặt đất (cộng thêm 0.02f để bù sai số BoxCast của GroundCheck)
			targetPos = new Vector2(highPos.x, hit.point.y - footOffset + 0.02f);
		}

		float elapsed = 0f;
		while (elapsed < _ledgeClimbDuration)
		{
			elapsed += Time.fixedDeltaTime;
			float t = Mathf.Clamp01(elapsed / _ledgeClimbDuration);
			float easedT = 1f - (1f - t) * (1f - t); // Ease-out
			RB.MovePosition(Vector2.Lerp(startPos, targetPos, easedT));
			yield return new WaitForFixedUpdate();
		}

		RB.MovePosition(targetPos);
		RB.linearVelocity = Vector2.zero;

		SetGravityScale(Data.gravityScale);
		TransitionToState(PlayerState.Grounded);
	}
	#endregion

	// -------------------------------------------------------------------------

	#region ONEWAY PLATFORM METHODS
	private bool TryGetOneWayPlatformBelow(out Collider2D platform)
	{
		platform = null;
		int hitCount = Physics2D.OverlapBox(_groundCheckPoint.position, _groundCheckSize, 0f, _groundFilter, _overlapResults);
		for (int i = 0; i < hitCount; i++)
		{
			var hit = _overlapResults[i];
			if (hit.isTrigger) continue;
			if (hit.GetComponent<HeartOfTheNight.Common.OneWayPlatform>() != null || hit.GetComponent<PlatformEffector2D>() != null)
			{
				platform = hit;
				return true;
			}
		}
		return false;
	}

	private bool IsSolidGround(Vector2 position, Vector2 size)
	{
		int hitCount = Physics2D.OverlapBox(position, size, 0f, _groundFilter, _overlapResults);
		for (int i = 0; i < hitCount; i++)
		{
			var col = _overlapResults[i];
			if (col.isTrigger) continue;
			
			if (col.GetComponent<HeartOfTheNight.Common.OneWayPlatform>() != null || col.GetComponent<PlatformEffector2D>() != null)
			{
				// Chỉ tính là mặt đất nếu nhân vật đang RƠI XUỐNG (vy <= 0.01)
				if (RB.linearVelocity.y <= 0.01f)
				{
					// Nếu đang rớt xuyên qua chính miếng ván này thì bỏ qua không tính là đất
					if (CurrentState == PlayerState.DroppingThrough && _ignoredPlatform == col)
						continue;
					
					return true;
				}
			}
			else
			{
				return true; // Mặt đất thường
			}
		}
		return false;
	}

	private bool IsSolidWall(Vector2 position, Vector2 size)
	{
		int hitCount = Physics2D.OverlapBox(position, size, 0f, _groundFilter, _overlapResults);
		for (int i = 0; i < hitCount; i++)
		{
			var col = _overlapResults[i];
			if (col.isTrigger) continue;
			
			// Wall check bỏ qua hoàn toàn OneWayPlatform (không thể bám tường gỗ)
			if (col.GetComponent<HeartOfTheNight.Common.OneWayPlatform>() != null || col.GetComponent<PlatformEffector2D>() != null)
				continue;

			return true; 
		}
		return false;
	}

	private IEnumerator DropThroughRoutine(Collider2D platformCol)
	{
		if (platformCol == null) yield break;

		foreach (var col in _playerColliders)
			if (col != null) Physics2D.IgnoreCollision(col, platformCol, true);

		float timeout = 1f; // Chờ tối đa 1 giây để lọt qua
		while (timeout > 0f)
		{
			timeout -= Time.deltaTime;
			
			// Đã rơi lọt hoàn toàn xuống dưới ván
			if (GetPlayerTopY() < platformCol.bounds.min.y) break;
			
			// Hoặc nếu nhảy/dash ngược lên trên và đã ngoi lên hoàn toàn khỏi ván
			if (CurrentState != PlayerState.DroppingThrough && GetPlayerBottomY() > platformCol.bounds.max.y) break;

			yield return null;
		}

		yield return new WaitForSeconds(0.05f); // 1 chút delay an toàn

		foreach (var col in _playerColliders)
			if (col != null) Physics2D.IgnoreCollision(col, platformCol, false);

		if (CurrentState == PlayerState.DroppingThrough)
			TransitionToState(PlayerState.Falling);
			
		if (_ignoredPlatform == platformCol)
			_ignoredPlatform = null;
	}

	private float GetPlayerTopY()
	{
		float top = float.NegativeInfinity;
		bool found = false;
		foreach (var col in _playerColliders)
		{
			if (col == null || col.isTrigger) continue;
			top = Mathf.Max(top, col.bounds.max.y);
			found = true;
		}
		return found ? top : transform.position.y;
	}

	private float GetPlayerBottomY()
	{
		float bottom = float.PositiveInfinity;
		bool found = false;
		foreach (var col in _playerColliders)
		{
			if (col == null || col.isTrigger) continue;
			bottom = Mathf.Min(bottom, col.bounds.min.y);
			found = true;
		}
		return found ? bottom : transform.position.y;
	}
	#endregion

	// -------------------------------------------------------------------------

    #region CHECK METHODS
    public void CheckDirectionToFace(bool isMovingRight)
	{
		if (isMovingRight != IsFacingRight)
			Turn();
	}

	private bool CanJump() =>
		LastOnGroundTime > 0 && CurrentState != PlayerState.Jumping;

	private bool CanDoubleJump() =>
		_bonusJumpsLeft > 0 && LastOnGroundTime <= 0 && !IsWallJumping;

	private bool CanWallJump() =>
		LastPressedJumpTime > 0 && LastOnWallTime > 0 && LastOnGroundTime <= 0
		&& (!IsWallJumping
			|| (LastOnWallRightTime > 0 && _lastWallJumpDir == 1)
			|| (LastOnWallLeftTime > 0 && _lastWallJumpDir == -1));

	private bool CanJumpCut() =>
		IsJumping && RB.linearVelocity.y > 0;

	private bool CanWallJumpCut() =>
		IsWallJumping && RB.linearVelocity.y > 0;

	private bool CanDash()
	{
		// Refill dash khi chạm đất hoặc bám tường
		if (!IsDashing && _dashesLeft < Data.dashAmount && (LastOnGroundTime > 0 || LastOnWallTime > 0) && !_dashRefilling)
			StartCoroutine(nameof(RefillDash), 1);

		return _dashesLeft > 0;
	}

	private bool IsLowerHalfTouchingWall()
	{
		// Tìm toạ độ Y của phần hông/chân (nằm giữa wallCheckPoint và dưới cùng chân)
		float bottomY = GetPlayerBottomY();
		float waistY = (_rightWallCheckPoint.position.y + bottomY) / 2f;
		
		Vector2 rightWaistPos = new Vector2(_rightWallCheckPoint.position.x, waistY);
		Vector2 leftWaistPos = new Vector2(_leftWallCheckPoint.position.x, waistY);
		
		// Trả về true nếu nửa DƯỚI của nhân vật vẫn đang chạm tường ở một trong hai bên
		return IsSolidWall(rightWaistPos, _wallCheckSize) || IsSolidWall(leftWaistPos, _wallCheckSize);
	}

	public bool CanSlide() =>
		LastOnWallTime > 0 && !IsJumping && !IsWallJumping && !IsDashing && LastOnGroundTime <= 0 && IsLowerHalfTouchingWall();
    #endregion

	// -------------------------------------------------------------------------

    #region EDITOR METHODS
    private void OnDrawGizmosSelected()
    {
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(_groundCheckPoint.position, _groundCheckSize);
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(_rightWallCheckPoint.position, _wallCheckSize);
		Gizmos.DrawWireCube(_leftWallCheckPoint.position, _wallCheckSize);

		// Ledge Climb Raycasts (Gizmos)
		if (_rightWallCheckPoint != null)
		{
			// Tia vai (Vang) - phai cham tuong (ban tu day hop)
			Gizmos.color = Color.yellow;
			Vector2 shoulderR = (Vector2)_rightWallCheckPoint.position + Vector2.down * (_wallCheckSize.y / 2f);
			Vector2 shoulderL = (Vector2)_leftWallCheckPoint.position + Vector2.down * (_wallCheckSize.y / 2f);
			Gizmos.DrawLine(shoulderR, shoulderR + Vector2.right * _ledgeRayLength);
			Gizmos.DrawLine(shoulderL, shoulderL + Vector2.left * _ledgeRayLength);

			// Tia dau (Do) - phai KHONG cham tuong
			Gizmos.color = Color.red;
			Vector2 headR = (Vector2)_rightWallCheckPoint.position + Vector2.up * _ledgeHeadRayOffsetY;
			Vector2 headL = (Vector2)_leftWallCheckPoint.position + Vector2.up * _ledgeHeadRayOffsetY;
			Gizmos.DrawLine(headR, headR + Vector2.right * _ledgeRayLength);
			Gizmos.DrawLine(headL, headL + Vector2.left * _ledgeRayLength);
		}
	}

	#region KNOCKBACK (INhanKnockback)
	public void ApplyKnockback(Vector2 direction, float force)
	{
		// Có thể bỏ qua knockback nếu đang Dash (khung hình bất tử)
		if (CurrentState == PlayerState.Dashing) return;

		TransitionToState(PlayerState.KnockedBack);
		RB.linearVelocity = direction.normalized * force;
		
		// Châm chước: Tặng lại lượt nhảy đôi để người chơi có cơ hội cứu mạng giữa không trung
		_bonusJumpsLeft = Data.bonusJumpAmount;
		
		StartCoroutine(KnockbackRoutine());
	}

	private IEnumerator KnockbackRoutine()
	{
		yield return new WaitForSeconds(0.25f);
		if (CurrentState == PlayerState.KnockedBack)
		{
			TransitionToState(PlayerState.Falling);
		}
	}
	#endregion

    #endregion
    }
}

// tạo bởi Dawnosaur :D