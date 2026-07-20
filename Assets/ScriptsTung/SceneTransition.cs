using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    [Header("Tên Scene muốn chuyển tới")]
    public string nextSceneName;

    [Header("Cài đặt Hiệu ứng")]
    public Image blackScreen;

    [Tooltip("Thời gian màn hình tối đi / sáng lên (Tính bằng giây)")]
    public float fadeDuration = 1f;

    [Tooltip("Thời gian giữ màn hình đen trước khi sáng lên ở Scene mới (Giây)")]
    public float delayBeforeFadeIn = 0.5f;

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

        // Đóng băng vật lý của Player
        Rigidbody2D pRb = playerObj.GetComponent<Rigidbody2D>();
        if (pRb != null)
        {
            pRb.linearVelocity = Vector2.zero;
            pRb.simulated = false;
        }

        // Giữ Canvas và Trạm chuyển cảnh sống sót qua Scene mới
        GameObject canvasCuaManHinhDen = blackScreen.transform.root.gameObject;
        DontDestroyOnLoad(canvasCuaManHinhDen);
        DontDestroyOnLoad(this.gameObject);

        // --- 1. MỜ DẦN SANG ĐEN ---
        blackScreen.gameObject.SetActive(true);
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            Color c = blackScreen.color;
            // Dùng Lerp để chuyển màu alpha từ 0 -> 1 mượt mà theo đúng thời gian fadeDuration
            c.a = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            blackScreen.color = c;
            yield return null;
        }

        // Đảm bảo đen hoàn toàn 100%
        Color finalColor = blackScreen.color;
        finalColor.a = 1f;
        blackScreen.color = finalColor;

        // --- 2. LOAD SCENE MỚI ---
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
        while (!asyncLoad.isDone)
        {
            yield return null; // Đợi load xong 100%
        }

        // --- TẠM DỪNG MỘT CHÚT CHO MƯỢT ---
        // Giữ màn hình đen một lúc ở Scene mới để mọi thứ load xong hẳn rồi mới sáng lên
        yield return new WaitForSeconds(delayBeforeFadeIn);

        // --- 3. SÁNG DẦN LÊN (Tại Scene mới) ---
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            Color c = blackScreen.color;
            // Dùng Lerp chuyển alpha từ 1 -> 0 để sáng dần lên
            c.a = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            blackScreen.color = c;
            yield return null;
        }

        // ==========================================
        // DỌN DẸP (Chỉ dọn sau khi đã hoàn thành 100% thời gian fade)
        // ==========================================
        blackScreen.gameObject.SetActive(false);
        Destroy(canvasCuaManHinhDen); // Hủy Canvas
        isTransitioning = false;
        Destroy(this.gameObject); // Hủy Trạm chuyển cảnh
    }
}