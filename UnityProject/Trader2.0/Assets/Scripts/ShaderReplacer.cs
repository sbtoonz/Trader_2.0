using System;
using UnityEngine;

public class ShaderReplacer : MonoBehaviour
{
    [Tooltip("Use this Field For Normal Renderers")] [SerializeField]
    internal Renderer[] _renderers;

    private bool _flipped = false;
    private void OnEnable()
    {
        if (_flipped) return;
        if(ZNetScene.m_instance.m_namedPrefabs.Count <= 0) return;
        foreach (var renderer in _renderers)
        {
            foreach (var material in renderer.sharedMaterials)
            {
                string name = material.shader.name;
                material.shader = Shader.Find(name);
                _flipped = true;
            }
        }
    }

    private void Awake()
    {
        if (_flipped) return;
        if(ZNetScene.m_instance.m_namedPrefabs.Count <= 0) return;
        foreach (var renderer in _renderers)
        {
            foreach (var material in renderer.sharedMaterials)
            {
                string name = material.shader.name;
                material.shader = Shader.Find(name);
                _flipped = true;
            }
        }
    }
}