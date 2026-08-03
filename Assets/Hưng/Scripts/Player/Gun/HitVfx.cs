using UnityEngine;
using System.Collections;

/// <summary>
/// Script gắn trên Prefab hiệu ứng va chạm (tia lửa, nổ...).
/// Tự động trả về Pool sau khi chạy xong animation/particle.
/// </summary>
public class HitVfx : MonoBehaviour
{
    private ParticleSystem _particle;
    private Animator _anim;

    private void Awake()
    {
        _particle = GetComponent<ParticleSystem>();
        _anim = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        float duration = 1f; // Mặc định nếu không tìm thấy gì

        if (_particle != null)
        {
            _particle.Play();
            duration = _particle.main.duration;
        }
        else if (_anim != null)
        {
            AnimatorStateInfo state = _anim.GetCurrentAnimatorStateInfo(0);
            duration = state.length > 0 ? state.length : 1f;
        }

        StartCoroutine(WaitAndReturn(duration));
    }

    private void OnDisable()
    {
        StopAllCoroutines(); // Dừng coroutine khi bị Despawn sớm
    }

    private IEnumerator WaitAndReturn(float time)
    {
        yield return new WaitForSeconds(time);
        gameObject.Despawn();
    }
}
