using HeartOfTheNight.Hung;
using HeartOfTheNight.Player;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class RoomTransition : MonoBehaviour
{
    public enum TransitionType { SameScene, NextLevel }

    public int nextLevelIndex;


    [Header("Loại chuyển cảnh")]
    public TransitionType transitionType = TransitionType.SameScene;

    [Header("Nếu là Same Scene (Chuyển phòng)")]
    public Transform nextRoomSpawnPoint;
    public RoomDoor targetDoor;

    [Header("Nếu là Next Level (Chuyển Scene)")]
    public string nextSceneName;
    public string spawnIDInNextScene; // Tên ID của cửa đích bên Scene mới
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
            pRb.linearVelocity = Vector2.zero;
            pRb.simulated = false;
        }

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

        if (transitionType == TransitionType.SameScene)
        {
            if (nextRoomSpawnPoint == null)
            {
                Debug.LogError("Chưa gán Next Room Spawn Point!");
                isTransitioning = false;
                yield break;
            }

            playerObj.transform.position = nextRoomSpawnPoint.position;
            Camera.main.transform.position = new Vector3(nextRoomSpawnPoint.position.x, nextRoomSpawnPoint.position.y, Camera.main.transform.position.z);

            if (targetDoor != null) targetDoor.Open();

            yield return new WaitForSeconds(0.2f);

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

            if (pRb != null) pRb.simulated = true;
            isTransitioning = false;
        }
        else if (transitionType == TransitionType.NextLevel)
        {
            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogError("Chưa nhập tên Scene tiếp theo!");
                isTransitioning = false;
                yield break;
            }

            // Ép tuyệt đối tới đúng namespace chứa PlayerHealth
            var hp = playerObj.GetComponent<HeartOfTheNight.Player.PlayerHealth>();
            if (hp != null) hp.HealToFull();

            // Ép tuyệt đối tới đúng namespace chứa DataManager của Hùng
            if (HeartOfTheNight.Hung.DataManager.Instance != null)
            {
                // THÊM LOGIC MỞ KHÓA LEVEL TẠI ĐÂY
                if (nextLevelIndex > HeartOfTheNight.Hung.DataManager.Instance.Data.maxUnlockedLevel)
                {
                    HeartOfTheNight.Hung.DataManager.Instance.Data.maxUnlockedLevel = nextLevelIndex;
                }

                HeartOfTheNight.Hung.DataManager.Instance.Data.currentScene = nextSceneName;
                HeartOfTheNight.Hung.DataManager.Instance.Data.targetSpawnID = spawnIDInNextScene;
                HeartOfTheNight.Hung.DataManager.Instance.SaveGame();
            }

            SceneManager.LoadScene(nextSceneName);
        }
    }
}