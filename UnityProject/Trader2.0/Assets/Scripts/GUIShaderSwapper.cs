using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIShaderSwapper : MonoBehaviour
{
    public CanvasRenderer[]? CanvasRenderers;
    public List<Image>? Images = new List<Image>();

    public Material? litgui;
    private bool _hasrun = false;
    private void Awake()
    {
        if(_hasrun) return;
        if (!litgui)
        {
            var templist = Resources.FindObjectsOfTypeAll<Material>();
            foreach (var variable in templist)
            {
                if (variable.name != "litpanel") continue;
                litgui = variable;
            }
        }
        if (!litgui) return;
        foreach (var variable in Images!)
        {
            variable.material = litgui;
        }

        foreach (var variable in CanvasRenderers!)
        {
            variable.SetMaterial(litgui, litgui!.mainTexture);
        }

        _hasrun = true;

    }
}