using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Bắt buộc phải có để load Scene
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    [Header("Tên Scene muốn chuyển tới")]
    public string nextSceneName;

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

        GameObject canvasCuaManHinhDen = blackScreen.transform.root.gameObject;
        DontDestroyOnLoad(canvasCuaManHinhDen);

        // 1. THÊM DÒNG NÀY: Giữ cho vật thể chứa script này sống sót để chạy nốt code
        DontDestroyOnLoad(this.gameObject);

        // --- 1. MỜ DẦN SANG ĐEN ---
        blackScreen.gameObject.SetActive(true);
        while (blackScreen.color.a < 1f)
        {
            Color c = blackScreen.color;
            c.a += Time.deltaTime * fadeSpeed;
            blackScreen.color = c;
            yield return null;
        }

        Color finalColor = blackScreen.color;
        finalColor.a = 1f;
        blackScreen.color = finalColor;

        // --- 2. LOAD SCENE MỚI ---
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // --- 3. SÁNG DẦN LÊN (Tại Scene mới) ---
        while (blackScreen.color.a > 0f)
        {
            Color c = blackScreen.color;
            c.a -= Time.deltaTime * fadeSpeed;
            blackScreen.color = c;
            yield return null;
        }

        // ==========================================
        // DỌN DẸP
        // ==========================================
        blackScreen.gameObject.SetActive(false);
        Destroy(canvasCuaManHinhDen);

        isTransitioning = false;

        // 2. THÊM DÒNG NÀY: Hủy vật thể chuyển cảnh này đi vì đã làm xong nhiệm vụ
        Destroy(this.gameObject);
    }
}