using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LitPanelizer : MonoBehaviour
{
    public Image? referenceimg;
    public Image? repairBkg;
    public List<Image?> _TabImages = new List<Image?>();
    public List<Image> _ButtonImages = new List<Image>();
    private Material litpanel;
    

    private void Awake()
    {
        litpanel = repairBkg!.material;
        
        var list = gameObject.GetComponentsInChildren<Transform>();
        foreach (var t in list)
        {
            if (t.GetComponent<Image>() != null)
            {
                if (t.GetComponent<Image>().material.name.StartsWith("REPLACE_"))
                {
                    t.GetComponent<Image>().material = litpanel;
                }
            }
        }

        foreach (var tabimage in _TabImages)
        {
            tabimage.sprite = referenceimg.sprite;
            tabimage.material = referenceimg.material;
        }

        foreach (var image in _ButtonImages)
        {
            image.material = litpanel;
        }
    }

    private void OnEnable()
    {
        foreach (var tabimage in _TabImages)
        {
            tabimage.sprite = referenceimg.sprite;
            tabimage.material = repairBkg.material;
            tabimage.material.mainTexture = referenceimg.mainTexture;
        }
    }
}
