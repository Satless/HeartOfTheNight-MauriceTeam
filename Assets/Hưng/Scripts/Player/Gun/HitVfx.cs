using UnityEngine;
using System.Collections;

public class HitVfx : MonoBehaviour
{
    private VfxPool _pool;
    private string _poolKey;
    private ParticleSystem _particle;
    private Animator _anim;

    private void Awake()
    {
        _particle = GetComponent<ParticleSystem>();
        _anim = GetComponent<Animator>();
    }

    public void Init(VfxPool pool, string key)
    {
        _pool = pool;
        _poolKey = key;
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

    private IEnumerator WaitAndReturn(float time)
    {
        yield return new WaitForSeconds(time);
        if (_pool != null)
        {
            _pool.Return(this, _poolKey);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
