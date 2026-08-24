using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Collections;
using HeartOfTheNight.Common;

public class AutoUniversalHealthBar : MonoBehaviour
{
    [Header("Cài đặt UI Minimalist")]
    public Canvas canvas;
    public Image fillImage;

    [Header("Màu sắc & Nhấp nháy")]
    public Color highHealthColor = Color.green;
    public Color mediumHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;
    public Color hitFlashColor = Color.white;

    [Header("Cài đặt Thời gian")]
    public float catchupSpeed = 10f;
    public float timeToHide = 3f;

    private Quaternion startRotation;
    private Vector3 originalCanvasScale;

    // Tự động soi ngầm 100%, sếp không cần khai báo bất cứ biến Health nào ra Inspector!
    private Component enemyScript;
    private FieldInfo currentHpField;
    private FieldInfo maxHpField;

    private float targetFill = 1f;
    private float hideTimer = 0f;
    private Color currentBaseColor;

    private bool isFlashing = false;
    private bool isDying = false;
    private CanvasGroup canvasGroup; // Dùng cái này để ẩn hiện mượt mà, không làm chết script

    void Start()
    {
        if (canvas != null)
        {
            startRotation = canvas.transform.rotation;
            originalCanvasScale = canvas.transform.localScale;

            // Tự động thêm CanvasGroup để làm trong suốt UI (Script vẫn chạy ngầm)
            canvasGroup = canvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }

        // Tự động móc vào IDamageable để đọc kết quả
        enemyScript = GetComponentInParent<IDamageable>() as Component;
        if (enemyScript != null)
        {
            System.Type type = enemyScript.GetType();
            currentHpField = type.GetField("currentHealth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            maxHpField = type.GetField("maxHealth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }

    void Update()
    {
        // Nếu không soi được code quái hoặc quái chết rồi thì ngừng
        if (isDying || enemyScript == null || currentHpField == null || maxHpField == null) return;

        int curHp = (int)currentHpField.GetValue(enemyScript);
        int maxHp = (int)maxHpField.GetValue(enemyScript);
        float newFill = (float)curHp / maxHp;

        // 🔥 TỰ ĐỘNG PHÁT HIỆN SẾP VỪA GỌI TAKEDAMAGE BÊN QUÁI VÀ MÁU BỊ TỤT!
        if (newFill < targetFill)
        {
            // Bật UI lên ngay lập tức
            if (canvasGroup != null && canvasGroup.alpha == 0f)
            {
                canvas.transform.localScale = originalCanvasScale;
                canvasGroup.alpha = 1f;
            }

            hideTimer = 0f; // Reset đồng hồ đi ngủ

            if (curHp <= 0)
            {
                StartCoroutine(TVEffectRoutine());
                targetFill = newFill;
                return;
            }
            else
            {
                StartCoroutine(HitFlashRoutine());
            }
        }

        targetFill = newFill;

        // Đổi 3 màu
        if (targetFill > 0.6f) currentBaseColor = highHealthColor;
        else if (targetFill > 0.3f) currentBaseColor = mediumHealthColor;
        else currentBaseColor = lowHealthColor;

        // Tụt máu mượt mà
        fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFill, Time.deltaTime * catchupSpeed);
        if (!isFlashing) fillImage.color = currentBaseColor;

        // Tự động đi ngủ sau 3 giây không nhận TakeDamage
        if (canvasGroup != null && canvasGroup.alpha > 0f)
        {
            hideTimer += Time.deltaTime;
            if (hideTimer >= timeToHide)
            {
                canvasGroup.alpha = 0f; // Tàng hình
            }
        }
    }

    void LateUpdate()
    {
        // Chống lật UI (Chỉ chạy khi UI đang hiện để tiết kiệm hiệu năng)
        if (canvas != null && canvasGroup != null && canvasGroup.alpha > 0f)
        {
            canvas.transform.rotation = startRotation;
            if (!isDying)
            {
                Vector3 fixScale = canvas.transform.localScale;
                if (transform.parent != null && transform.parent.localScale.x < 0)
                    fixScale.x = -Mathf.Abs(fixScale.x);
                else
                    fixScale.x = Mathf.Abs(fixScale.x);
                canvas.transform.localScale = fixScale;
            }
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        isFlashing = true;
        fillImage.color = hitFlashColor;
        yield return new WaitForSeconds(0.08f);
        isFlashing = false;
    }

    private IEnumerator TVEffectRoutine()
    {
        isDying = true;
        float timer = 0;
        Vector3 startScale = canvas.transform.localScale;
        Vector3 flatScale = new Vector3(startScale.x, 0f, startScale.z);

        while (timer < 0.1f)
        {
            timer += Time.deltaTime;
            canvas.transform.localScale = Vector3.Lerp(startScale, flatScale, timer / 0.1f);
            yield return null;
        }

        timer = 0;
        Vector3 dotScale = new Vector3(0f, 0f, startScale.z);

        while (timer < 0.15f)
        {
            timer += Time.deltaTime;
            canvas.transform.localScale = Vector3.Lerp(flatScale, dotScale, timer / 0.15f);
            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 0f; // Tắt ngúm Tivi
    }
}