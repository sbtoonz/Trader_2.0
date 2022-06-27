using UnityEngine;

public class PlayerBaseMaker : MonoBehaviour
{
    private EffectArea _playerBase;
    private GameObject _holder;
    private void OnEnable()
    {
        if (_playerBase != null) return;
        _holder = new GameObject();
        _holder.SetActive(false);
        var temp = Instantiate(_holder, transform.position, Quaternion.identity);
        _playerBase = temp.AddComponent<EffectArea>();
        _playerBase.m_type = EffectArea.Type.NoMonsters;
        var sphere = temp.AddComponent<SphereCollider>();
        sphere.radius = 5;
        sphere.isTrigger = true;
    }
}
