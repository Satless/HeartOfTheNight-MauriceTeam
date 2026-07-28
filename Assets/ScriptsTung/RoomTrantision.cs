using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class RoomTransition : MonoBehaviour
{
    [Header("Điểm đến (Phòng mới)")]
    public Transform nextRoomSpawnPoint;

    // THÊM BIẾN NÀY: Tham chiếu đến script RoomDoor của cửa đích
    [Header("Cửa ở phòng đích (Để gọi Animation Mở)")]
    public RoomDoor targetDoor;

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

        if (nextRoomSpawnPoint == null)
        {
            Debug.LogError("Chưa gán Next Room Spawn Point!");
            isTransitioning = false;
            yield break;
        }

        if (pRb != null)
        {
            pRb.linearVelocity = Vector2.zero;
            pRb.simulated = false;
        }

        // 1. Chỉ chạy hiệu ứng mờ nếu ĐÃ GÁN Black Screen
        if (blackScreen != null)
        {
            blackScreen.gameObject.SetActive(true);
            while (blackScreen.color.a < 1f)
            {
                Color c = blackScreen.color;
                c.a += Time.deltaTime * fadeSpeed;
                blackScreen.color = c;
                yield return null;
            }
        }

        // 2. Dịch chuyển Player
        playerObj.transform.position = nextRoomSpawnPoint.position;
        Camera.main.transform.position = new Vector3(nextRoomSpawnPoint.position.x, nextRoomSpawnPoint.position.y, Camera.main.transform.position.z);

        if (targetDoor != null)
        {
            targetDoor.Open();
        }

        yield return new WaitForSeconds(0.2f);

        // 3. Sáng dần lên (Nếu có Black Screen)
        if (blackScreen != null)
        {
            while (blackScreen.color.a > 0f)
            {
                Color c = blackScreen.color;
                c.a -= Time.deltaTime * fadeSpeed;
                blackScreen.color = c;
                yield return null;
            }
            blackScreen.gameObject.SetActive(false);
        }

        if (pRb != null)
        {
            pRb.simulated = true;
        }

        isTransitioning = false;
    }
}