using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIShaderSwapper : MonoBehaviour
{
    public CanvasRenderer[]? CanvasRenderers;
    public List<Image>? Images = new List<Image>();

    public Material? litgui;
    private bool _hasrun = false;

    private void Start()
    {
        // Delay to let Valheim load its materials first
        Invoke(nameof(SwapShaders), 0.5f);
    }

    private void SwapShaders()
    {
        if(_hasrun) return;

        if (!litgui)
        {
            var templist = Resources.FindObjectsOfTypeAll<Material>();
            Debug.Log($"[GUIShaderSwapper] Searching {templist.Length} materials for 'litpanel'");

            foreach (var variable in templist)
            {
                if (variable.name != "litpanel") continue;
                litgui = variable;
                Debug.Log($"[GUIShaderSwapper] Found litpanel material: {litgui.shader.name}");
                break;
            }
        }

        if (!litgui)
        {
            Debug.LogWarning($"[GUIShaderSwapper] Could not find 'litpanel' material! UI will use REPLACE_ materials.");
            return;
        }

        var allImages = GetComponentsInChildren<Image>(true);
        foreach (var img in allImages)
        {
            if (img != null)
            {
                img.material = litgui;
            }
        }
        Debug.Log($"[GUIShaderSwapper] Applied litpanel to {allImages.Length} Images (hierarchy scan)");

        var allRenderers = GetComponentsInChildren<CanvasRenderer>(true);
        foreach (var cr in allRenderers)
        {
            if (cr != null)
            {
                cr.SetMaterial(litgui, litgui.mainTexture);
            }
        }
        Debug.Log($"[GUIShaderSwapper] Applied litpanel to {allRenderers.Length} CanvasRenderers (hierarchy scan)");

        _hasrun = true;
    }
}