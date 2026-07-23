using System.Collections.Generic;
using UnityEngine;

public class VfxPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [Tooltip("Số lượng hiệu ứng tạo sẵn lúc đầu cho MỖI LOẠI")]
    [SerializeField] private int _initialSizePerType;

    private readonly Dictionary<string, Stack<HitVfx>> _pools = new Dictionary<string, Stack<HitVfx>>();

    public void Prewarm(GameObject prefab)
    {
        if (prefab == null) return;
        string key = prefab.name;

        if (!_pools.ContainsKey(key))
        {
            _pools[key] = new Stack<HitVfx>();
            for (int i = 0; i < _initialSizePerType; i++)
            {
                HitVfx vfxInstance = CreateVfx(prefab, key);
                vfxInstance.gameObject.SetActive(false);
                _pools[key].Push(vfxInstance);
            }
        }
    }

    private HitVfx CreateVfx(GameObject prefab, string key)
    {
        GameObject obj = Instantiate(prefab, transform);
        HitVfx vfxInstance = obj.GetComponent<HitVfx>();
        if (vfxInstance == null)
        {
            vfxInstance = obj.AddComponent<HitVfx>();
        }
        vfxInstance.Init(this, key);
        return vfxInstance;
    }

    public void SpawnVfx(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return;
        
        string key = prefab.name;

        if (!_pools.ContainsKey(key))
        {
            _pools[key] = new Stack<HitVfx>();
        }

        HitVfx vfxInstance;
        if (_pools[key].Count > 0)
        {
            vfxInstance = _pools[key].Pop();
        }
        else
        {
            vfxInstance = CreateVfx(prefab, key);
        }

        vfxInstance.transform.position = position;
        vfxInstance.gameObject.SetActive(true);
    }

    public void Return(HitVfx vfx, string key)
    {
        vfx.gameObject.SetActive(false);
        if (_pools.ContainsKey(key))
        {
            _pools[key].Push(vfx);
        }
        else
        {
            Destroy(vfx.gameObject);
        }
    }
}
