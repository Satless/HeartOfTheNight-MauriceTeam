using UnityEngine;

[CreateAssetMenu(menuName = "Player Data With Dash")] //Tạo một đối tượng playerData mới bằng cách nhấp chuột phải vào Menu Project rồi chọn Create/Player/Player Data và kéo vào người chơi
public class PlayerDataWithDash : ScriptableObject
{
	[Header("Gravity")]
	[HideInInspector] public float gravityStrength; //Lực hướng xuống (trọng lực) cần thiết để đạt được jumpHeight và jumpTimeToApex mong muốn.
	[HideInInspector] public float gravityScale; //Độ lớn trọng lực của người chơi dưới dạng hệ số nhân của trọng lực (được thiết lập trong ProjectSettings/Physics2D).
										  //Cũng là giá trị mà rigidbody2D.gravityScale của người chơi được thiết lập.
	[Space(5)]
	public float fallGravityMult; //Hệ số nhân cho gravityScale của người chơi khi đang rơi.
	public float maxFallSpeed; //Tốc độ rơi tối đa (vận tốc tới hạn) của người chơi khi đang rơi.
	[Space(5)]
	public float fastFallGravityMult; //Hệ số nhân lớn hơn cho gravityScale của người chơi khi đang rơi và phím mũi tên xuống được nhấn.
									  //Thường thấy trong các game như Celeste, cho phép người chơi rơi nhanh hơn nếu họ muốn.
	public float maxFastFallSpeed; //Tốc độ rơi tối đa (vận tốc tới hạn) của người chơi khi thực hiện rơi nhanh.
	
	[Space(20)]

	[Header("Run")]
	public float runMaxSpeed; //Tốc độ mục tiêu mà chúng ta muốn người chơi đạt được.
	public float runAcceleration; //Tốc độ mà người chơi tăng tốc lên tốc độ tối đa, có thể đặt bằng runMaxSpeed để tăng tốc tức thời hoặc bằng 0 để không tăng tốc
	[HideInInspector] public float runAccelAmount; //Lực thực tế (nhân với speedDiff) áp dụng lên người chơi.
	public float runDecceleration; //Tốc độ mà người chơi giảm tốc từ tốc độ hiện tại, có thể đặt bằng runMaxSpeed để giảm tốc tức thời hoặc bằng 0 để không giảm tốc
	[HideInInspector] public float runDeccelAmount; //Lực thực tế (nhân với speedDiff) áp dụng lên người chơi.
	[Space(5)]
	[Range(0f, 1)] public float accelInAir; //Hệ số nhân áp dụng cho gia tốc khi ở trên không.
	[Range(0f, 1)] public float deccelInAir;
	[Space(5)]
	public bool doConserveMomentum = true;

	[Space(20)]

	[Header("Jump")]
	public float jumpHeight; //Độ cao cú nhảy của người chơi
	public float jumpTimeToApex; //Thời gian giữa lúc áp dụng lực nhảy và đạt được độ cao mong muốn. Các giá trị này cũng kiểm soát trọng lực và lực nhảy của người chơi.
	[HideInInspector] public float jumpForce; //Lực thực tế áp dụng (hướng lên) lên người chơi khi họ nhảy.

	[Header("Both Jumps")]
	public float jumpCutGravityMult; //Hệ số nhân để tăng trọng lực nếu người chơi nhả nút nhảy trong khi vẫn đang nhảy
	[Range(0f, 1)] public float jumpHangGravityMult; //Giảm trọng lực khi gần tới đỉnh (độ cao tối đa mong muốn) của cú nhảy
	public float jumpHangTimeThreshold; //Tốc độ (gần bằng 0) nơi người chơi sẽ trải qua hiệu ứng "lơ lửng" thêm. Vận tốc y của người chơi gần bằng 0 nhất tại đỉnh cú nhảy (hãy nghĩ đến độ dốc của một parabol hoặc hàm bậc hai)
	[Space(0.5f)]
	public float jumpHangAccelerationMult; 
	public float jumpHangMaxSpeedMult; 				

	[Header("Wall Jump")]
	public Vector2 wallJumpForce; //Lực thực tế (lần này do chúng ta thiết lập) áp dụng lên người chơi khi nhảy tường.
	[Space(5)]
	[Range(0f, 1f)] public float wallJumpRunLerp; //Giảm bớt ảnh hưởng của sự di chuyển của người chơi khi đang nhảy tường.
	[Range(0f, 1.5f)] public float wallJumpTime; //Thời gian sau khi nhảy tường mà di chuyển của người chơi bị làm chậm lại.
	public bool doTurnOnWallJump; //Người chơi sẽ xoay mặt về hướng nhảy tường

	[Space(20)]

	[Header("Slide")]
	public float slideSpeed;
	public float slideAccel;

	[Header("Double Jump")]
	public int bonusJumpAmount; //Số lần nhảy đôi cho phép

	[Header("Wall Climb")]
	public float wallClimbSpeed; //Tốc độ leo lên khi bám tường (dương = lên)
	public float wallClimbAccel; //Gia tốc để đạt wallClimbSpeed

    [Header("Assists")]
	[Range(0.01f, 0.5f)] public float coyoteTime; //Thời gian châm chước sau khi rơi khỏi nền đất, nơi bạn vẫn có thể nhảy
	[Range(0.01f, 0.5f)] public float jumpInputBufferTime; //Thời gian châm chước sau khi nhấn nút nhảy nơi một cú nhảy sẽ được tự động thực hiện ngay khi các yêu cầu (ví dụ: đang ở trên mặt đất) được đáp ứng.

	[Space(20)]

	[Header("Dash")]
	public int dashAmount;
	public float dashSpeed;
	public float dashSleepTime; //Khoảng thời gian game bị đóng băng khi nhấn lướt nhưng trước khi đọc phím hướng và áp dụng lực
	[Space(5)]
	public float dashAttackTime;
	[Space(5)]
	public float dashEndTime; //Thời gian sau khi bạn kết thúc giai đoạn kéo ban đầu, làm mượt sự chuyển đổi trở lại trạng thái nhàn rỗi (hoặc bất kỳ trạng thái tiêu chuẩn nào)
	public Vector2 dashEndSpeed; //Làm chậm người chơi, giúp lướt (dash) có cảm giác phản hồi tốt hơn (được sử dụng trong Celeste)
	[Range(0f, 1f)] public float dashEndRunLerp; //Làm chậm ảnh hưởng của di chuyển của người chơi trong khi đang lướt
	[Space(5)]
	public float dashRefillTime;
	[Space(5)]
	[Range(0.01f, 0.5f)] public float dashInputBufferTime;
	

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
