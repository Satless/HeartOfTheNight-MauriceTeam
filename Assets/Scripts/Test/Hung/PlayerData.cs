using UnityEngine;

[CreateAssetMenu(menuName = "Player Data")] //Tạo một đối tượng playerData mới bằng cách nhấp chuột phải vào Menu Project rồi chọn Create/Player/Player Data và kéo vào người chơi
public class PlayerData : ScriptableObject
{
	[Header("Gravity")]
	[HideInInspector] public float gravityStrength; //Lực hướng xuống (trọng lực) cần thiết để đạt được jumpHeight và jumpTimeToApex mong muốn.
	[HideInInspector] public float gravityScale; //Độ lớn trọng lực của người chơi dưới dạng hệ số nhân của trọng lực (được thiết lập trong ProjectSettings/Physics2D).
										  //Cũng là giá trị mà rigidbody2D.gravityScale của người chơi được thiết lập.
	[Space(5)]
	[Tooltip("Tăng trọng lực khi rơi. Giúp nhân vật bay lên thì từ từ nhưng rơi xuống thì nhanh, tạo cảm giác đầm tay.")]
	public float fallGravityMult;
	[Tooltip("Tốc độ rơi tối đa. Dù rớt từ trên đỉnh núi xuống cũng không rớt nhanh hơn số này, giúp người chơi kịp nhìn đường.")]
	public float maxFallSpeed;
	[Space(5)]
	[Tooltip("Trọng lực rơi siêu tốc (khi người chơi giữ phím Xuống để chủ động lao nhanh xuống đất).")]
	public float fastFallGravityMult; //Thường thấy trong các game như Celeste, cho phép người chơi rơi nhanh hơn nếu họ muốn.
	[Tooltip("Tốc độ rơi tối đa khi người chơi chủ động giữ phím rơi nhanh.")]
	public float maxFastFallSpeed;
	
	[Space(20)]

	[Header("Run")]
	[Tooltip("Tốc độ chạy tối đa của nhân vật.")]
	public float runMaxSpeed;
	[Tooltip("Gia tốc chạy từ từ đến tối đa. Số càng lớn nhân vật vọt đi càng lẹ. (Đặt bằng runMaxSpeed để đạt tốc độ tối đa ngay lập tức).")]
	public float runAcceleration;
	[HideInInspector] public float runAccelAmount; //Lực thực tế (nhân với speedDiff) áp dụng lên người chơi.
	[Tooltip("Độ giảm tốc từ từ về 0. Số càng lớn dừng lại càng gấp. (Số thấp sẽ làm nhân vật bị trượt như đi trên băng).")]
	public float runDecceleration;
	[HideInInspector] public float runDeccelAmount; //Lực thực tế (nhân với speedDiff) áp dụng lên người chơi.
	[Space(5)]
	[Tooltip("Độ nhạy bẻ lái khi ở trên không. (1 = Đổi hướng linh hoạt như dưới đất, 0 = Bị khóa cứng hướng bay).")]
	[Range(0f, 1)] public float accelInAir;
	[Tooltip("Lực cản không khí khi buông tay khỏi bàn phím. (0 = Bay lố theo quán tính, 1 = Thắng gấp và rơi thẳng đứng xuống).")]
	[Range(0f, 1)] public float deccelInAir;
	[Space(5)]
	[Tooltip("Bảo toàn quán tính. Giữ nguyên tốc độ nếu nhân vật đang lao đi nhanh hơn runMaxSpeed")]
	public bool doConserveMomentum = true;

	[Space(20)]

	[Header("Jump")]
	[Tooltip("Độ cao cú nhảy của người chơi (tính bằng Unity Unit)")]
	public float jumpHeight;
	[Tooltip("Thời gian để bay lên đạt đỉnh cú nhảy (tính bằng giây). Biến này sẽ tự động tính toán lại Trọng Lực và Lực Nhảy.")]
	public float jumpTimeToApex;
	[HideInInspector] public float jumpForce; //Lực thực tế áp dụng (hướng lên) lên người chơi khi họ nhảy.

	[Header("Both Jumps")]
	[Tooltip("Tăng trọng lực (rơi lẹ hơn) nếu người chơi nhả nút nhảy ra sớm giữa chừng.")]
	public float jumpCutGravityMult;
	[Tooltip("Giảm trọng lực khi đang lơ lửng ở đỉnh cú nhảy (0 là không có trọng lực, 1 là trọng lực bình thường)")]
	[Range(0f, 1)] public float jumpHangGravityMult;
	[Tooltip("Ngưỡng vận tốc trục Y để kích hoạt trạng thái 'lơ lửng'")]
	public float jumpHangTimeThreshold;
	[Space(0.5f)]
	[Tooltip("Gia tốc lơ lửng. Độ nhạy bẻ lái trên không")]
	public float jumpHangAccelerationMult;
	[Tooltip("Tốc độ lơ lửng. Tốc độ ngang trên không khi lơ lửng")]
	public float jumpHangMaxSpeedMult;

	[Header("Wall Jump")]
	[Tooltip("Lực bật nhảy khỏi tường (Trục X = bật ra xa, Trục Y = bật lên cao)")]
	public Vector2 wallJumpForce;
	[Space(5)]
	[Tooltip("Độ bẻ lái sau khi nhảy tường.\nCàng gần 0 thì người chơi càng khó bẻ lái quay lại tường ngay lập tức.\nCòn về 1 thì toàn quyền kiểm soát nhưng tốc độ nhảy tường thuộc hoàn toàn vào runMaxSpeed")]
	[Range(0f, 1f)] public float wallJumpRunLerp; //Giảm bớt ảnh hưởng của sự di chuyển của người chơi khi đang nhảy tường.
	[Tooltip("Thời gian bị 'Khóa vô lăng' (mất kiểm soát di chuyển ngang) ngay sau khi nhảy tường.")]
	[Range(0f, 1.5f)] public float wallJumpTime; //Thời gian sau khi nhảy tường mà di chuyển của người chơi bị làm chậm lại.
	[Tooltip("Tự động xoay mặt nhân vật hướng ra ngoài khi bật nhảy tường.")]
	public bool doTurnOnWallJump; //Người chơi sẽ xoay mặt về hướng nhảy tường

	[Header("Double Jump")]
	[Tooltip("Số lượng cú nhảy trên không (Nhảy đôi) cho phép.")]
	public int bonusJumpAmount; //Số lần nhảy đôi cho phép

	[Space(20)]

	[Header("Slide")]
	[Tooltip("Tốc độ trượt từ từ xuống tường. Nên đặt số âm để trượt xuống, chứ dương là thành leo đấy.")]
	public float slideSpeed; // Tốc độ trượt tường (âm = xuống, để dương là thành leo lên đấy)
	[Tooltip("Gia tốc hãm phanh khi bám tường (Giúp nhân vật từ từ chậm lại thành trượt thay vì rớt tự do).")]
	public float slideAccel; // Gia tốc trượt tường (dùng để hãm tốc độ khi trượt xuống)

	[Header("Wall Climb")]
	[Tooltip("Tốc độ leo tường. Số dương.")]
	public float wallClimbSpeed; //Tốc độ leo lên khi bám tường (dương = lên)
	[Tooltip("Gia tốc bám tường. Số càng lớn thì bám hoặc hãm phanh trên tường càng dính.")]
	public float wallClimbAccel; //Gia tốc để leo tường (Dùng để hãm phanh)

    [Header("Assists")]
	[Tooltip("Coyote Time (Thời gian ma). Thời gian châm chước cho phép người chơi nhảy kể cả khi đã rớt chân khỏi mép đất (Tạo cảm giác công bằng).")]
	[Range(0.01f, 0.5f)] public float coyoteTime; //Thời gian châm chước sau khi rơi khỏi nền đất, nơi bạn vẫn có thể nhảy
	[Tooltip("Input Buffer (Ghi nhớ phím Nhảy). Bấm nhảy sớm lúc đang lơ lửng trên không, ngay khi chạm đất game sẽ tự nhảy giùm.")]
	[Range(0.01f, 0.5f)] public float jumpInputBufferTime; //Thời gian châm chước sau khi nhấn nút nhảy nơi một cú nhảy sẽ được tự động thực hiện ngay khi các yêu cầu (ví dụ: đang ở trên mặt đất) được đáp ứng.

	[Space(20)]

	[Header("Dash")]
	[Tooltip("Số lượng cú lướt cho phép trước khi phải chạm đất để hồi lại.")]
	public int dashAmount; // Số lần lướt
	[Tooltip("Tốc độ vọt đi của nhân vật khi lướt.")]
	public float dashSpeed; // Tốc độ lướt
	[Tooltip("Thời gian 'Ngưng đọng' (Hitstop). Game sẽ đóng băng 1 tích tắc ngay lúc bấm lướt để tạo cảm giác lấy đà cực mạnh (Game Feel).")]
	public float dashSleepTime; //Khoảng thời gian game bị đóng băng khi nhấn lướt nhưng trước khi đọc phím hướng và áp dụng lực
	[Space(5)]
	[Tooltip("Thời lượng duy trì trạng thái lướt (Tính bằng giây, thường rất ngắn).")]
	public float dashAttackTime; // Thời gian lướt
	[Space(5)]
	[Tooltip("Thời gian của giai đoạn 'Chậm dần' sau khi hết lướt, giúp trả lại quyền điều khiển cho người chơi một cách mượt mà.")]
	public float dashEndTime; // Kết thúc giai đoạn 1, đồng thời trả dần quyền điều khiển cũng như phục hồi trọng lực
	[Tooltip("Vận tốc bị hãm xuống ở cuối giai đoạn lướt (Tạo cảm giác phanh gấp, bám sát cơ chế di chuyển của Celeste).")]
	public Vector2 dashEndSpeed; //Làm chậm người chơi, giúp lướt (dash) có cảm giác phản hồi tốt hơn (được sử dụng trong Celeste)
	[Tooltip("Độ ảnh hưởng của phím di chuyển ngang trong giai đoạn cuối lướt (0 = Không thể bẻ lái, 1 = Bẻ lái bình thường).")]
	[Range(0f, 1f)] public float dashEndRunLerp; //Làm chậm ảnh hưởng của di chuyển của người chơi trong khi đang lướt
	[Space(5)]
	[Tooltip("Thời gian chờ để hồi lại lượt lướt sau khi chạm đất.")]
	public float dashRefillTime; // Thời gian hồi lướt
	[Space(5)]
	[Tooltip("Input Buffer (Ghi nhớ phím Lướt). Thời gian ghi nhớ phím Lướt dù bấm sớm trước khi chạm đất.")]
	[Range(0.01f, 0.5f)] public float dashInputBufferTime; //Thời gian châm chước sau khi nhấn nút lướt nơi một lần lướt sẽ được tự động thực hiện ngay khi các yêu cầu (ví dụ: có thể lướt) được đáp ứng.
	

	//Unity Callback, được gọi khi inspector cập nhật
    private void OnValidate()
    {
		//Tính toán độ lớn trọng lực bằng công thức (gravity = 2 * jumpHeight / timeToJumpApex^2) 
		gravityStrength = -(2 * jumpHeight) / (jumpTimeToApex * jumpTimeToApex);
		
		//Tính toán gravity scale của rigidbody (ví dụ: độ lớn trọng lực tương đối so với giá trị trọng lực của Unity, xem project settings/Physics2D)
		gravityScale = gravityStrength / Physics2D.gravity.y;

		//Tính toán lực gia tốc và giảm tốc khi chạy bằng công thức: amount = ((1 / Time.fixedDeltaTime) * acceleration) / runMaxSpeed
		runAccelAmount = (50 * runAcceleration) / runMaxSpeed;
		runDeccelAmount = (50 * runDecceleration) / runMaxSpeed;

		//Tính toán lực nhảy (jumpForce) bằng công thức (initialJumpVelocity = gravity * timeToJumpApex)
		jumpForce = Mathf.Abs(gravityStrength) * jumpTimeToApex;

		#region Variable Ranges
		runAcceleration = Mathf.Clamp(runAcceleration, 0.01f, runMaxSpeed);
		runDecceleration = Mathf.Clamp(runDecceleration, 0.01f, runMaxSpeed);
		#endregion
	}
}
