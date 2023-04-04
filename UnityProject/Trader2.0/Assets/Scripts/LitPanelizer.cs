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
        foreach (Transform child1 in transform)
        {
            var t = child1.gameObject.GetComponentsInChildren<Renderer>();
            foreach (var r in t)
            {
                if (r.material.name.StartsWith("REPLACE_"))
                {
                    r.material = litpanel;
                }
            }

            if (child1.childCount <= 0) continue;
            {
                foreach (Transform child2 in child1)
                {
                    var t2 = child2.gameObject.GetComponentsInChildren<Renderer>();
                    foreach (var r in t2)
                    {
                        if (r.material.name.StartsWith("REPLACE_"))
                        {
                            r.material = litpanel;
                        }
                    }

                    if (child2.childCount <= 0) continue;
                    {
                        foreach (Transform child3 in child2)
                        {
                            var t3 = child3.GetComponentsInChildren<Renderer>();
                            foreach (var r in t3)
                            {
                                if (r.material.name.StartsWith("REPLACE_"))
                                {
                                    r.material = litpanel;
                                }
                            }

                            if (child3.childCount <= 0) continue;
                            {
                                foreach (Transform child4 in child3)
                                {
                                    var t4 = child4.GetComponentsInChildren<Renderer>();
                                    foreach (var r in t4)
                                    {
                                        if (r.material.name.StartsWith("REPLACE_"))
                                        {
                                            r.material = litpanel;
                                        }
                                    }
                                    
                                    if (child4.childCount <= 0) continue;
                                    {
                                        foreach (Transform child5 in child4)
                                        {
                                            var t5 = child5.GetComponentsInChildren<Renderer>();
                                            foreach (var r in t5)
                                            {
                                                if (r.material.name.StartsWith("REPLACE_"))
                                                {
                                                    r.material = litpanel;
                                                }
                                            }
                                            
                                            if (child5.childCount <= 0) continue;
                                            {
                                                foreach (Transform child6 in child5)
                                                {
                                                    var t6 = child6.GetComponentsInChildren<Renderer>();
                                                    foreach (var r in t6)
                                                    {
                                                        if (r.material.name.StartsWith("REPLACE_"))
                                                        {
                                                            r.material = litpanel;
                                                        }
                                                    }
                                                    
                                                    if (child6.childCount <= 0) continue;
                                                    {
                                                        foreach (Transform child7 in child6)
                                                        {
                                                            var t7 = child7.GetComponentsInChildren<Renderer>();
                                                            foreach (var r in t7)
                                                            {
                                                                if (r.material.name.StartsWith("REPLACE_"))
                                                                {
                                                                    r.material = litpanel;
                                                                }
                                                            }
                                                            if (child7.childCount <= 0) continue;
                                                            {
                                                                foreach (Transform child8 in child6)
                                                                {
                                                                    var t8 = child8.GetComponentsInChildren<Renderer>();
                                                                    foreach (var r in t8)
                                                                    {
                                                                        if (r.material.name.StartsWith("REPLACE_"))
                                                                        {
                                                                            r.material = litpanel;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                               
                            }
                        }
                    }

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
