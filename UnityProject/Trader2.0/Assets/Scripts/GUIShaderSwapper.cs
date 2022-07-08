using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIShaderSwapper : MonoBehaviour
{
    public CanvasRenderer[] CanvasRenderers;
    public List<Image> Images = new List<Image>();

    public Material litgui;
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
        CanvasRenderers = GetComponentsInChildren<CanvasRenderer>();

        foreach (var VARIABLE in Images)
        {
            VARIABLE.material = litgui;
        }

        foreach (var VARIABLE in CanvasRenderers)
        {
            VARIABLE.SetMaterial(litgui, litgui.mainTexture);
        }
    }
}