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

public class PlayerMovement : MonoBehaviour
{
	//Scriptable object chứa tất cả các thông số di chuyển — không hardcode
	[Tooltip("Kéo thẳng ScriptableObject PlayerData vào đây")]
	public PlayerData Data;

	#region COMPONENTS
    public Rigidbody2D RB { get; private set; }
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
		Sliding
	}

	public PlayerState CurrentState { get; private set; }

	/// <summary>
	/// Điểm duy nhất thay đổi trạng thái. Xử lý OnExit state cũ và OnEnter state mới.
	/// </summary>
	private void TransitionToState(PlayerState newState)
	{
		// --- OnExit state cũ ---
		// (Dashing exit được xử lý trong coroutine StartDash, không cần ở đây)

		CurrentState = newState;

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

	// Timers coyote time & input buffer
	public float LastOnGroundTime { get; private set; }
	public float LastOnWallTime { get; private set; }
	public float LastOnWallRightTime { get; private set; }
	public float LastOnWallLeftTime { get; private set; }

	// Cờ nội bộ jump — không phải trạng thái chính, chỉ ảnh hưởng gravity
	private bool _isJumpCut;
	private bool _isJumpFalling;

	// Nhảy tường
	private float _wallJumpStartTime;
	private int _lastWallJumpDir;

	// Lướt
	private int _dashesLeft;
	private bool _dashRefilling;
	private Vector2 _lastDashDir;
	private bool _isDashAttacking;

	// Nhảy đôi
	private int _bonusJumpsLeft;
	#endregion

	#region INPUT PARAMETERS
	private Vector2 _moveInput;

	public float LastPressedJumpTime { get; private set; }
	public float LastPressedDashTime { get; private set; }
	#endregion

	#region CHECK PARAMETERS
	[Header("Visuals")]
	[Tooltip("Kéo child phần thân dưới (chân) vào đây (Duoi)")]
	[SerializeField] private Transform _lowerBodyVisual;

	[Header("Checks")] 
	[Tooltip("Kéo child kiểm tra dưới chân")]
	[SerializeField] private Transform _groundCheckPoint;
	[Tooltip("Kích thước hộp kiểm tra dưới chân, dùng physic thay vì collider để tránh lỗi)")]
	[SerializeField] private Vector2 _groundCheckSize = new Vector2(0.49f, 0.03f);
	[Space(5)]
	[Tooltip("Kéo child kiểm tra tường trước mặt (bên phải)")]
	[SerializeField] private Transform _frontWallCheckPoint;
	[Tooltip("Kéo child kiểm tra tường sau lưng (bên trái)")]
	[SerializeField] private Transform _backWallCheckPoint;
	[Tooltip("Kích thước hộp kiểm tra tường, dùng physic thay vì collider để tránh lỗi)")]
	[SerializeField] private Vector2 _wallCheckSize = new Vector2(0.5f, 1f);
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

	// -------------------------------------------------------------------------

    private void Awake()
	{
		RB = GetComponent<Rigidbody2D>();

		// Cache coroutine wait — Zero GC Alloc
		_dashRefillWait = new WaitForSeconds(Data.dashRefillTime);
		_sleepWait = new WaitForSecondsRealtime(Data.dashSleepTime);  //Phải dùng Realtime vì timeScale = 0
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
		HandleInput();
		HandleCollisionChecks();
		HandleJumpChecks();
		HandleDashChecks();
		HandleSlideChecks();
		HandleGravity();
    }

    private void FixedUpdate()
	{
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

	/// <summary>Đọc input và gọi callback tương ứng.</summary>
	private void HandleInput()
	{
		_moveInput.x = Input.GetAxisRaw("Horizontal");
		_moveInput.y = Input.GetAxisRaw("Vertical");

		if (_moveInput.x != 0)
			CheckDirectionToFace(_moveInput.x > 0);

		if(Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.J))
			OnJumpInput();

		if (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.C) || Input.GetKeyUp(KeyCode.J))
			OnJumpUpInput();

		if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.K))
			OnDashInput();
	}

	/// <summary>Kiểm tra va chạm ground/wall, cập nhật timer coyote.</summary>
	private void HandleCollisionChecks()
	{
		if (IsJumping) return;

		// Ground check — skip toàn bộ khi đang dash để tránh HandleJumpChecks()
		// ép state về Grounded giữa chừng trong khi coroutine StartDash vẫn chạy.
		if (!IsDashing)
		{
			if (Physics2D.OverlapBox(_groundCheckPoint.position, _groundCheckSize, 0, _groundLayer))
				LastOnGroundTime = Data.coyoteTime;
		}

		// Wall check — chỉ skip ở Phase 1 (dash attack ghi đè velocity mỗi frame),
		// cho phép cập nhật timer tường ở Phase 2 để coroutine có thể thoát sớm khi chạm tường.
		if (!_isDashAttacking)
		{
			// Right wall check
			if (((Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, _groundLayer) && IsFacingRight)
					|| (Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, _groundLayer) && !IsFacingRight)) && !IsWallJumping)
				LastOnWallRightTime = Data.coyoteTime;

			// Left wall check
			if (((Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, _groundLayer) && !IsFacingRight)
				|| (Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, _groundLayer) && IsFacingRight)) && !IsWallJumping)
				LastOnWallLeftTime = Data.coyoteTime;

			LastOnWallTime = Mathf.Max(LastOnWallLeftTime, LastOnWallRightTime);
		}
	}

	/// <summary>Quản lý các điều kiện chuyển trạng thái liên quan đến nhảy.</summary>
	private void HandleJumpChecks()
	{
		// Jumping → Falling khi bắt đầu rơi
		if (IsJumping && RB.linearVelocity.y < 0)
			TransitionToState(PlayerState.Falling);

		// WallJumping hết thời gian
		if (IsWallJumping && Time.time - _wallJumpStartTime > Data.wallJumpTime)
			TransitionToState(PlayerState.Falling);

		// Chạm đất → reset về Grounded
		if (LastOnGroundTime > 0 && !IsJumping && !IsWallJumping && CurrentState != PlayerState.Grounded)
			TransitionToState(PlayerState.Grounded);

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
		if (!CanDash() || LastPressedDashTime <= 0) return;

		//Đóng băng game một khoảnh khắc để đọc input hướng chính xác
		Sleep();

		if (IsSliding && _moveInput.y != 0 && !Data.horizontalDashOnly)
		{
			// Lướt dọc khi bám tường (lên/xuống tùy input). Bị horizontalDashOnly chi phối.
			_lastDashDir = (_moveInput.y > 0) ? Vector2.up : Vector2.down;
		}
		else if (IsSliding && Data.allowReverseWallClingDash)
		{
			// Bật: Lướt bật ra xa tường (không bị horizontalDashOnly chi phối)
			_lastDashDir = (LastOnWallRightTime > 0) ? Vector2.left : Vector2.right;
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
			if (IsSliding)
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
		bool shouldSlide = CanSlide() && ((LastOnWallLeftTime > 0 && _moveInput.x < 0) || (LastOnWallRightTime > 0 && _moveInput.x > 0)); //Dễ hiểu thì biến bool ở đây sẽ là true hoặc false nếu sau dấu = đúng hoặc sai, đây là if lồng trong bool.

		if (shouldSlide && CurrentState != PlayerState.Sliding)
			TransitionToState(PlayerState.Sliding);
		else if (!shouldSlide && CurrentState == PlayerState.Sliding)
		// NẾU shouldSlide = true (được phép trượt) MÀ lại bị rớt xuống dòng này (do nhân vật đang trượt sẵn trên tường), thì lệnh else if này sẽ tự động bị bỏ qua (kết thúc hàm) vì nó yêu cầu shouldSlide = false (!shouldSlide).
    		if (LastOnGroundTime > 0)
        		TransitionToState(PlayerState.Grounded);
    		else
        		TransitionToState(PlayerState.Falling);
	}

	/// <summary>
	/// Điều chỉnh gravity mỗi frame dựa trên state hiện tại.
	/// Gravity không được quản lý trong TransitionToState vì nó cần cập nhật liên tục.
	/// </summary>
	private void HandleGravity()
	{
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
		StartCoroutine(nameof(PerformSleep));
    }

	private IEnumerator PerformSleep()
    {
		Time.timeScale = 0; // Dừng toàn bộ game (vật lý, animation, timer)
		yield return _sleepWait; // Chờ dashSleepTime giây THỰC (dùng WaitForSecondsRealtime vì timeScale = 0)
		Time.timeScale = 1; // Trả game về tốc độ bình thường
	}
    #endregion

	// -------------------------------------------------------------------------

    #region RUN METHODS
    private void Run(float lerpAmount)
	{
		// Tốc độ mục tiêu = hướng input × tốc độ chạy tối đa (vd: 1 × 15 = 15, -1 × 15 = -15, 0 × 15 = 0)
		float targetSpeed = _moveInput.x * Data.runMaxSpeed;
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
	}

	private void Turn()
	{
		IsFacingRight = !IsFacingRight;

		if (_lowerBodyVisual != null)
		{
			Vector3 scale = _lowerBodyVisual.localScale; 
			scale.x *= -1;
			_lowerBodyVisual.localScale = scale;

			// Lật vị trí các điểm check để vật lý hoạt động đúng mà không cần lật Root
			if (_frontWallCheckPoint != null)
			{
				Vector3 pos = _frontWallCheckPoint.localPosition;
				pos.x *= -1;
				_frontWallCheckPoint.localPosition = pos;
			}
			if (_backWallCheckPoint != null)
			{
				Vector3 pos = _backWallCheckPoint.localPosition;
				pos.x *= -1;
				_backWallCheckPoint.localPosition = pos;
			}
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

		// Bù vận tốc âm trước khi apply lực — đảm bảo luôn nhảy đúng độ cao
		float force = Data.jumpForce;
		if (RB.linearVelocity.y < 0)
			force -= RB.linearVelocity.y;

		RB.AddForce(Vector2.up * force, ForceMode2D.Impulse);
	}

	private void WallJump(int dir)
	{
		LastPressedJumpTime = 0;
		LastOnGroundTime = 0;
		LastOnWallRightTime = 0;
		LastOnWallLeftTime = 0;

		Vector2 force = new Vector2(Data.wallJumpForce.x, Data.wallJumpForce.y);
		force.x *= dir; //Lực ngược chiều tường

		// Bù vận tốc ngang và dọc để đảm bảo wall jump đạt đúng độ lớn
		if (Mathf.Sign(RB.linearVelocity.x) != Mathf.Sign(force.x))
			force.x -= RB.linearVelocity.x;
		if (RB.linearVelocity.y < 0)
			force.y -= RB.linearVelocity.y;

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

		// Nếu bật lockFacingToDashDirection, ép quay mặt đúng theo hướng lướt ngang
		if (Data.lockFacingToDashDirection && Mathf.Abs(dir.x) > 0.1f)
		{
			CheckDirectionToFace(dir.x > 0);
		}

		float startTime = Time.time; // Thời gian bắt đầu giai đoạn 1
		_dashesLeft--;
		_isDashAttacking = true;
		SetGravityScale(0);

		// Phase 1 — dash attack: giữ vận tốc cố định (tham khảo Celeste)
		while (Time.time - startTime <= Data.dashAttackTime)
		{
			RB.linearVelocity = dir.normalized * Data.dashSpeed;
			yield return null;
		}

		startTime = Time.time; // Thời gian bắt đầu giai đoạn 2
		_isDashAttacking = false;

		// Phase 2 — dash end: Hãm phanh
		SetGravityScale(Data.gravityScale);
		RB.linearVelocity = Data.dashEndSpeed * dir.normalized;

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
	}

	private void WallClimb()
	{
		// Giống Slide() nhưng đẩy lên trên thay vì kéo xuống
		float speedDif = Data.wallClimbSpeed - RB.linearVelocity.y;
		float movement = speedDif * Data.wallClimbAccel;
		movement = Mathf.Clamp(movement, -Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime), Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime));
		RB.AddForce(movement * Vector2.up);
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

	public bool CanSlide() =>
		LastOnWallTime > 0 && !IsJumping && !IsWallJumping && !IsDashing && LastOnGroundTime <= 0;
    #endregion

	// -------------------------------------------------------------------------

    #region EDITOR METHODS
    private void OnDrawGizmosSelected()
    {
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(_groundCheckPoint.position, _groundCheckSize);
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(_frontWallCheckPoint.position, _wallCheckSize);
		Gizmos.DrawWireCube(_backWallCheckPoint.position, _wallCheckSize);
	}
    #endregion
}

// tạo bởi Dawnosaur :D