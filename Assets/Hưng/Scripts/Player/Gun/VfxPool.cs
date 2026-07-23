using System.Collections.Generic;
using UnityEngine;

public class VfxPool : MonoBehaviour
{
    private readonly Dictionary<string, Stack<HitVfx>> _pools = new Dictionary<string, Stack<HitVfx>>();

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
            GameObject obj = Instantiate(prefab, transform);
            vfxInstance = obj.GetComponent<HitVfx>();
            if (vfxInstance == null)
            {
                vfxInstance = obj.AddComponent<HitVfx>();
            }
            vfxInstance.Init(this, key);
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
