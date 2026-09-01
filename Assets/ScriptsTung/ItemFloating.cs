using UnityEngine;

public class ItemFloating : MonoBehaviour
{
    [Header("Cài đặt Bắn Tung Tóe")]
    public float minForceX = -3f;
    public float maxForceX = 3f;
    public float minForceY = 4f;
    public float maxForceY = 7f;

    [Header("Cài đặt Lơ Lửng")]
    public float floatSpeed = 5f;
    public float floatAmplitude = 0.25f;

    private Rigidbody2D rb;
    private Vector3 startPos;
    private bool hasLandOnGround = false;
    private bool isLooted = false;
    private float spawnTime;

    public bool HasLanded => hasLandOnGround;

    void Start()
    {
        spawnTime = Time.time;
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.simulated = true;
            rb.bodyType = RigidbodyType2D.Dynamic; // Đảm bảo là vật thể rơi được
            rb.gravityScale = 1.5f;
            // Bật Continuous để chống lỗi rớt xuyên Ground khi lực nảy quá mạnh (đặc biệt item đặt sẵn)
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            // Bắn tung tóe ngẫu nhiên khi vừa rớt ra
            float randomX = Random.Range(minForceX, maxForceX);
            float randomY = Random.Range(minForceY, maxForceY);
            rb.linearVelocity = new Vector2(randomX, randomY);
        }
    }

    void Update()
    {
        if (isLooted) return;

        // Khi đã chạm sàn nhà thì lơ lửng bồng bềnh tại chỗ
        if (hasLandOnGround)
        {
            float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(startPos.x, newY, startPos.z);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasLandOnGround || isLooted) return;
        
        // Ngăn chặn việc item vừa sinh ra bị dính luôn vào sàn (nếu quái chết quá sát mặt đất)
        // Phải đợi ít nhất 0.1s sau khi pop-out mới được tính là chạm đất
        if (Time.time - spawnTime < 0.1f) return;

        // Kiểm tra xem va chạm có phải là Layer "Ground" không
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // 🔥 NẾU CHẠM SÀN (Ở dưới chân):
                if (contact.normal.y > 0.5f)
                {
                    hasLandOnGround = true;
                    startPos = transform.position;

                    // 🔥 BÍ QUYẾT FIX LỖI NHẶT ĐỒ Ở ĐÂY:
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector2.zero;
                        rb.bodyType = RigidbodyType2D.Kinematic; // Tắt trọng lực, giữ nguyên va chạm
                    }

                    // Biến item thành Trigger để Player đi xuyên qua và gọi hàm OnTriggerEnter2D nhặt đồ!
                    Collider2D[] cols = GetComponents<Collider2D>();
                    foreach (Collider2D col in cols)
                    {
                        col.isTrigger = true;
                    }

                    break;
                }
            }
        }
    }

    public void StopFloating()
    {
        isLooted = true;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    public void ResetLandedState()
    {
        hasLandOnGround = false;
    }
}