using UnityEngine;

public class ShaderReplacer : MonoBehaviour
{
    [Tooltip("Use this Field For Normal Renderers")] [SerializeField]
    internal Renderer[] _renderers;
    private void OnEnable()
    {
        foreach (var renderer in _renderers)
        {
            foreach (var material in renderer.sharedMaterials)
            {
                string name = material.shader.name;
                material.shader = Shader.Find(name);
            }
        }
    }
}