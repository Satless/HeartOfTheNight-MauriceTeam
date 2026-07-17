using System.Collections;
using UnityEngine;

public class KamikazeTest1 : MonoBehaviour
{
    //=========================
    // THÔNG SỐ DI CHUYỂN
    //=========================
    [Header("Movement")]
    public float speed = 6f;                 // Tốc độ di chuyển

    //=========================
    // PHẠM VI PHÁT HIỆN PLAYER
    //=========================
    [Header("Detection")]
    public float detectionRange = 6f;        // Bán kính phát hiện Player

    //=========================
    // THÔNG SỐ KÍCH NỔ
    //=========================
    [Header("Explosion")]
    public float explodeRange = 1.5f;        // Khoảng cách bắt đầu kích nổ
    public float explodeDelay = 1.5f;        // Thời gian nhấp nháy trước khi nổ
    public float flashInterval = 0.1f;       // Tốc độ nhấp nháy
    public float attackAnimationTime = 0.6f; // Thời gian Animation Attack

    //=========================
    // CHỈ SỐ
    //=========================
    [Header("Stats")]
    public int damage = 30;
    public int hp = 1;

    //=========================
    // BIẾN
    //=========================
    private Transform player;
    private bool chasing = false;
    private bool exploding = false;

    private Animator animator;
    private SpriteRenderer sr;

    private void Start()
    {
        // Tìm Player
        GameObject obj = GameObject.FindGameObjectWithTag("Player");

        if (obj != null)
            player = obj.transform;

        // Lấy Animator
        animator = GetComponent<Animator>();

        // Lấy SpriteRenderer
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        // Nếu không có Player hoặc đang nổ thì dừng
        if (player == null || exploding)
            return;

        // Tính khoảng cách đến Player
        float distance = Vector2.Distance(transform.position, player.position);

        // Nếu Player đi vào vùng phát hiện
        if (distance <= detectionRange)
        {
            chasing = true;
        }

        // Đuổi Player
        if (chasing)
        {
            // Lật hướng nhìn
            if (player.position.x > transform.position.x)
                transform.localScale = new Vector3(1, 1, 1);
            else
                transform.localScale = new Vector3(-1, 1, 1);

            // Di chuyển tới Player
            transform.position = Vector2.MoveTowards(
                transform.position,
                player.position,
                speed * Time.deltaTime);

            // Cập nhật khoảng cách
            distance = Vector2.Distance(transform.position, player.position);

            // Nếu Player vào vùng nổ
            if (distance <= explodeRange)
            {
                StartCoroutine(Explode());
            }
        }
    }

    //=========================================
    // Coroutine xử lý quá trình kích nổ
    //=========================================
    IEnumerator Explode()
    {
        exploding = true;

        // Dừng di chuyển
        chasing = false;

        float timer = 0f;

        //=========================
        // Nhấp nháy đỏ
        //=========================
        while (timer < explodeDelay)
        {
            if (sr != null)
                sr.color = Color.red;

            yield return new WaitForSeconds(flashInterval);

            if (sr != null)
                sr.color = Color.white;

            yield return new WaitForSeconds(flashInterval);

            timer += flashInterval * 2f;
        }

        // Trả màu bình thường
        if (sr != null)
            sr.color = Color.white;

        //=========================
        // Chạy Animation Attack
        //=========================
        if (animator != null)
            animator.SetTrigger("Attack");

        // Chờ Animation chạy xong
        yield return new WaitForSeconds(attackAnimationTime);

        //=========================
        // Gây sát thương
        //=========================
        if (player != null &&
            Vector2.Distance(transform.position, player.position) <= explodeRange)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();

            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }

        // Hủy Enemy
        Destroy(gameObject);
    }

    //=========================================
    // Enemy nhận sát thương
    //=========================================
    public void TakeDamage(int dmg)
    {
        hp -= dmg;

        if (hp <= 0)
        {
            StopAllCoroutines();
            Destroy(gameObject);
        }
    }

    //=========================================
    // Vẽ Gizmos trong Scene
    //=========================================
    private void OnDrawGizmosSelected()
    {
        // Vùng phát hiện
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Vùng kích nổ
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explodeRange);
    }
}