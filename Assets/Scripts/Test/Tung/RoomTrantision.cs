using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RoomTransition : MonoBehaviour
{
    [Header("Điểm đến (Phòng mới)")]
    public Transform nextRoomSpawnPoint;

    [Header("Hiệu ứng màn hình")]
    public Image blackScreen;
    public float fadeSpeed = 3f;

    private bool isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isTransitioning)
        {
            StartCoroutine(TransitionRoutine(collision.gameObject));
        }
    }

    IEnumerator TransitionRoutine(GameObject playerObj)
    {
        isTransitioning = true;

        Rigidbody2D pRb = playerObj.GetComponent<Rigidbody2D>();
        if (pRb != null)
        {
            pRb.linearVelocity = Vector2.zero; // Xóa sạch đà di chuyển cũ
            pRb.simulated = false; // "Đóng băng" hoàn toàn vật lý của Player
        }

        // --- 1. MỜ DẦN SANG ĐEN ---
        blackScreen.gameObject.SetActive(true);
        while (blackScreen.color.a < 1f)
        {
            Color c = blackScreen.color;
            c.a += Time.deltaTime * fadeSpeed;
            blackScreen.color = c;
            yield return null;
        }

        // --- 2. DỊCH CHUYỂN PLAYER & CAMERA ---
        playerObj.transform.position = nextRoomSpawnPoint.position;
        Camera.main.transform.position = new Vector3(nextRoomSpawnPoint.position.x, nextRoomSpawnPoint.position.y, Camera.main.transform.position.z);

        yield return new WaitForSeconds(0.2f);

        // --- 3. SÁNG DẦN LÊN ---
        while (blackScreen.color.a > 0f)
        {
            Color c = blackScreen.color;
            c.a -= Time.deltaTime * fadeSpeed;
            blackScreen.color = c;
            yield return null;
        }
        //as
        blackScreen.gameObject.SetActive(false);

        // ==========================================
        // MỞ KHÓA VẬT LÝ TRỞ LẠI
        // ==========================================
        if (pRb != null)
        {
            pRb.simulated = true; // Cho phép Player hoạt động vật lý bình thường
        }

        isTransitioning = false;
    }
}