using System;
using UnityEngine;

enum ShaderType
{
    Alpha,
    Blob,
    Bonemass,
    Clouds,
    Creature,
    Decal,
    Distortion,
    Flow,
    FlowOpaque,
    Grass,
    GuiScroll,
    HeightMap,
    Icon,
    InteriorSide,
    LitGui,
    LitParticles,
    MapShader,
    ParticleDetail,
    Piece,
    Player,
    Rug,
    ShadowBlob,
    SkyboxProcedural,
    SkyObject,
    StaticRock,
    Tar,
    TrilinearMap,
    BGBlur,
    Water,
    WaterBottom,
    WaterMask,
    Yggdrasil,
    YggdrasilRoot,
    ToonDeferredShading2017
}
public class ShaderReplacerNew : MonoBehaviour
{
    [Tooltip("Use this Field For Normal Renderers")] [SerializeField]
    internal Renderer[] _renderers;
    [SerializeField] internal ShaderType _shaderType;

    private void Awake()
    {
        if (IsHeadlessMode()) return;
        foreach (var renderer in _renderers)
        {
            foreach (var material in renderer.sharedMaterials)
            {
                material.shader = Shader.Find(ReturnEnumString(_shaderType));
            }
        }
    }

    internal string ReturnEnumString(ShaderType shaderchoice)
    {
        var s="";
        switch (shaderchoice)
        {
            case ShaderType.Alpha:
                s = "Custom/AlphaParticle";
                break;
            case ShaderType.Blob:
                s = "Custom/Blob";
                break;
            case ShaderType.Bonemass:
                s = "Custom/BoneMass";
                break;
            case ShaderType.Clouds:
                s = "Custom/Clouds";
                break;
            case ShaderType.Creature:
                s = "Custom/Creature";
                break;
            case ShaderType.Decal:
                s = "Custom/Decal";
                break;
            case ShaderType.Distortion:
                s = "Custom/Distortion";
                break;
            case ShaderType.Flow:
                s = "Custom/Flow";
                break;
            case ShaderType.FlowOpaque:
                s = "Custom/FlowOpaque";
                break;
            case ShaderType.Grass:
                s = "Custom/Grass";
                break;
            case ShaderType.GuiScroll:
                s = "Custom/GuiScroll";
                break;
            case ShaderType.HeightMap:
                s = "Custom/HeightMap";
                break;
            case ShaderType.Icon:
                s = "Custom/Icon";
                break;
            case ShaderType.InteriorSide:
                s = "Custom/InteriorSide";
                break;
            case ShaderType.LitGui:
                s = "Custom/LitGui";
                break;
            case ShaderType.LitParticles:
                s = "Custom/LitParticles";
                break;
            case ShaderType.MapShader:
                s = "Custom/mapshader";
                break;
            case ShaderType.ParticleDetail:
                s = "Custom/ParticleDecal";
                break;
            case ShaderType.Piece:
                break;
            case ShaderType.Player:
                break;
            case ShaderType.Rug:
                break;
            case ShaderType.ShadowBlob:
                break;
            case ShaderType.SkyboxProcedural:
                break;
            case ShaderType.SkyObject:
                break;
            case ShaderType.StaticRock:
                break;
            case ShaderType.Tar:
                break;
            case ShaderType.TrilinearMap:
                break;
            case ShaderType.BGBlur:
                break;
            case ShaderType.Water:
                break;
            case ShaderType.WaterBottom:
                break;
            case ShaderType.WaterMask:
                break;
            case ShaderType.Yggdrasil:
                break;
            case ShaderType.YggdrasilRoot:
                break;
            case ShaderType.ToonDeferredShading2017:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(shaderchoice), shaderchoice, null);
        }
        return s;
    }
    
    public static bool IsHeadlessMode()
    {
        return UnityEngine.SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
    }
}