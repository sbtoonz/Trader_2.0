using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIShaderSwapper : MonoBehaviour
{
    public CanvasRenderer[]? CanvasRenderers;
    public List<Image>? Images = new List<Image>();

    public Material? litgui;
    private void Awake()
    {
        var templist = Resources.FindObjectsOfTypeAll<Material>();
        foreach (var VARIABLE in templist)
        {
            if (VARIABLE.name == "litpanel")
            {
                litgui = VARIABLE;
            }
        }
        foreach (var variable in Images!)
        {
            variable.material = litgui;
        }

        foreach (var variable in CanvasRenderers!)
        {
            variable.SetMaterial(litgui, litgui!.mainTexture);
        }
    }
}