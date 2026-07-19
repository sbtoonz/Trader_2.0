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

        if (Images != null)
        {
            foreach (var variable in Images)
            {
                if (variable != null)
                {
                    variable.material = litgui;
                }
            }
            Debug.Log($"[GUIShaderSwapper] Applied litpanel to {Images.Count} Images");
        }

        if (CanvasRenderers != null)
        {
            foreach (var variable in CanvasRenderers)
            {
                if (variable != null)
                {
                    variable.SetMaterial(litgui, litgui.mainTexture);
                }
            }
            Debug.Log($"[GUIShaderSwapper] Applied litpanel to {CanvasRenderers.Length} CanvasRenderers");
        }

        _hasrun = true;
    }
}