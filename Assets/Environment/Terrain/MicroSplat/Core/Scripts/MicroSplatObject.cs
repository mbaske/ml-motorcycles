//////////////////////////////////////////////////////
// MicroSplat
// Copyright (c) Jason Booth
//////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MicroSplatObject : MonoBehaviour 
{
   [HideInInspector]
   public Material templateMaterial;
   
   [HideInInspector]
   [System.NonSerialized]
   public Material matInstance;

   [HideInInspector]
   public Material blendMat;

   [HideInInspector]
   public Material blendMatInstance;

   [HideInInspector]
   public MicroSplatKeywords keywordSO;

   [HideInInspector]
   public Texture2D perPixelNormal;


   [HideInInspector]
   public Texture2D terrainDesc;

   public enum DescriptorFormat
   {
      RGBAHalf,
      RGBAFloat
   }

   public DescriptorFormat descriptorFormat = DescriptorFormat.RGBAHalf;

   [HideInInspector]
   public Texture2D streamTexture;

#if __MICROSPLAT_PROCTEX__
   [HideInInspector]
   public Texture2D cavityMap;
   [HideInInspector]
   public MicroSplatProceduralTextureConfig procTexCfg;
   [HideInInspector]
   public Texture2D procBiomeMask;
#endif

#if __MICROSPLAT_SNOW__
   [HideInInspector]
   public Texture2D snowMaskOverride;
#endif


#if __MICROSPLAT_GLOBALTEXTURE__
   [HideInInspector]
   public Texture2D tintMapOverride;
   [HideInInspector]
   public Texture2D globalNormalOverride;
   [HideInInspector]
   public Texture2D globalSAOMOverride;
   [HideInInspector]
   public Texture2D globalEmisOverride;
   [HideInInspector]
   public Texture2D geoTextureOverride;
#endif

#if __MICROSPLAT_SCATTER__
   public Texture2D scatterMapOverride;
#endif

#if (VEGETATION_STUDIO || VEGETATION_STUDIO_PRO)
   [HideInInspector]
   public Texture2D vsGrassMap;
   [HideInInspector]
   public Texture2D vsShadowMap;
#endif

   //#if __MICROSPLAT_ADVANCED_DETAIL__
   [HideInInspector]
   public Texture2D advDetailControl;
//#endif

#if __MICROSPLAT_ALPHAHOLE__
   [HideInInspector]
   public Texture2D clipMap;
#endif


   protected long GetOverrideHash()
   {
      long h = 3;
      unchecked
      {
         h *= (propData == null ? 3 : propData.GetHashCode ()) * 3;
         h *= (perPixelNormal == null ? 7 : perPixelNormal.GetNativeTexturePtr ().ToInt64 ()) * 7;
         h *= (keywordSO == null ? 11 : keywordSO.GetHashCode ()) * 11;

#if __MICROSPLAT_ALPHAHOLE__
         h *= (clipMap == null ? 5 : clipMap.GetNativeTexturePtr ().ToInt64 ()) * 5;
#endif     

#if __MICROSPLAT_PROCTEX__
         h *= (procBiomeMask == null ? 13 : procBiomeMask.GetNativeTexturePtr ().ToInt64 ()) * 13;
         h *= (cavityMap == null ? 17 : cavityMap.GetNativeTexturePtr ().ToInt64 ()) * 17;
         h *= (procTexCfg == null ? 19 : procTexCfg.GetHashCode ()) * 19;
#endif
#if __MICROSPLAT_ADVANCED_DETAIL__
         h *= (advDetailControl == null ? 23 : advDetailControl.GetNativeTexturePtr ().ToInt64 ()) * 23;
#endif
#if (VEGETATION_STUDIO || VEGETATION_STUDIO_PRO)
         h *= (vsShadowMap == null ? 31 : vsShadowMap.GetNativeTexturePtr ().ToInt64 ()) * 31;
         h *= (vsGrassMap == null ? 37 : vsGrassMap.GetNativeTexturePtr ().ToInt64 ()) * 37;
#endif
         h *= (streamTexture == null ? 41 : streamTexture.GetNativeTexturePtr ().ToInt64 ()) * 41;
         h *= (terrainDesc == null ? 43 : terrainDesc.GetNativeTexturePtr ().ToInt64 ()) * 43;
#if __MICROSPLAT_GLOBALTEXTURE__
         h *= (geoTextureOverride == null ? 47 : geoTextureOverride.GetNativeTexturePtr ().ToInt64 ()) * 47;
         h *= (globalNormalOverride == null ? 53 : globalNormalOverride.GetNativeTexturePtr ().ToInt64 ()) * 53;
         h *= (globalSAOMOverride == null ? 59 : globalSAOMOverride.GetNativeTexturePtr ().ToInt64 ()) * 59;
         h *= (globalEmisOverride == null ? 61 : globalEmisOverride.GetNativeTexturePtr ().ToInt64 ()) * 61;
         h *= (tintMapOverride == null ? 71 : tintMapOverride.GetNativeTexturePtr().ToInt64()) * 71;
#endif

#if __MICROSPLAT_SCATTER__
         h *= (scatterMapOverride == null ? 79 : scatterMapOverride.GetNativeTexturePtr ().ToInt64 ()) * 79;
#endif

#if __MICROSPLAT_SNOW__
         h *= (snowMaskOverride == null ? 73 : snowMaskOverride.GetNativeTexturePtr().ToInt64()) * 73;
#endif


         if (h == 0)
         {
            Debug.Log ("Override hash returned 0, this should not happen");
         }
      }
      return h;
   }




   [HideInInspector]
   public MicroSplatPropData propData;

   protected void SetMap(Material m, string name, Texture2D tex)
   {
      if (m.HasProperty(name) && tex != null)
      {
         m.SetTexture(name, tex);
      }
   }

   protected void ApplyMaps(Material m)
   {
      SetMap (m, "_PerPixelNormal", perPixelNormal);
      SetMap (m, "_StreamControl", streamTexture);

#if __MICROSPLAT_ALPHAHOLE__
      SetMap (m, "_AlphaHoleTexture", clipMap);
#endif
      
#if __MICROSPLAT_GLOBALTEXTURE__
      SetMap (m, "_GeoTex", geoTextureOverride);
      SetMap (m, "_GlobalTintTex", tintMapOverride);
      SetMap (m, "_GlobalNormalTex", globalNormalOverride);
      SetMap (m, "_GlobalSAOMTex", globalSAOMOverride);
      SetMap (m, "_GlobalEmisTex", globalEmisOverride);
      if (m.HasProperty ("_GeoCurve") && propData != null)
      {
         m.SetTexture ("_GeoCurve", propData.GetGeoCurve ());
      }
      if (m.HasProperty ("_GeoSlopeTex") && propData != null)
      {
         m.SetTexture ("_GeoSlopeTex", propData.GetGeoSlopeFilter ());
      }
      if (m.HasProperty ("_GlobalSlopeTex") && propData != null)
      {
         m.SetTexture ("_GlobalSlopeTex", propData.GetGlobalSlopeFilter ());
      }
#endif

#if __MICROSPLAT_SCATTER__
      SetMap (m, "_ScatterControl", scatterMapOverride);
#endif

#if __MICROSPLAT_SNOW__
      SetMap (m, "_SnowMask", snowMaskOverride);
#endif

#if (VEGETATION_STUDIO || VEGETATION_STUDIO_PRO)
      SetMap (m, "_VSGrassMap", vsGrassMap);
      SetMap (m, "_VSShadowMap", vsShadowMap);
#endif

#if __MICROSPLAT_ADVANCED_DETAIL__
      SetMap (m, "_AdvDetailControl", advDetailControl);
#endif


      if (propData != null)
      {
         m.SetTexture("_PerTexProps", propData.GetTexture());
      }

#if __MICROSPLAT_PROCTEX__
      if (procTexCfg != null)
      {
         if (m.HasProperty("_ProcTexCurves"))
         {
            m.SetTexture("_ProcTexCurves", procTexCfg.GetCurveTexture());
            m.SetTexture("_ProcTexParams", procTexCfg.GetParamTexture());
            m.SetInt("_PCLayerCount", procTexCfg.layers.Count);
            if (procBiomeMask != null && m.HasProperty("_ProcTexBiomeMask"))
            {
               m.SetTexture("_ProcTexBiomeMask", procBiomeMask);
            }
         }
         if (m.HasProperty("_PCHeightGradients"))
         {
            m.SetTexture("_PCHeightGradients", procTexCfg.GetHeightGradientTexture()); 
         }
         if (m.HasProperty("_PCHeightHSV"))
         {
            m.SetTexture("_PCHeightHSV", procTexCfg.GetHeightHSVTexture());
         }
         if (m.HasProperty("_CavityMap"))
         {
            m.SetTexture("_CavityMap", cavityMap);
         }

         if (m.HasProperty ("_PCSlopeGradients"))
         {
            m.SetTexture ("_PCSlopeGradients", procTexCfg.GetSlopeGradientTexture ());
         }
         if (m.HasProperty ("_PCSlopeHSV"))
         {
            m.SetTexture ("_PCSlopeHSV", procTexCfg.GetSlopeHSVTexture ());
         }
      }
#endif
}

protected void ApplyControlTextures(Texture2D[] controls, Material m)
   {
      m.SetTexture("_Control0", controls.Length > 0 ? controls[0] : Texture2D.blackTexture);
      m.SetTexture("_Control1", controls.Length > 1 ? controls[1] : Texture2D.blackTexture);
      m.SetTexture("_Control2", controls.Length > 2 ? controls[2] : Texture2D.blackTexture);
      m.SetTexture("_Control3", controls.Length > 3 ? controls[3] : Texture2D.blackTexture);
      m.SetTexture("_Control4", controls.Length > 4 ? controls[4] : Texture2D.blackTexture);
      m.SetTexture("_Control5", controls.Length > 5 ? controls[5] : Texture2D.blackTexture);
      m.SetTexture("_Control6", controls.Length > 6 ? controls[6] : Texture2D.blackTexture);
      m.SetTexture("_Control7", controls.Length > 7 ? controls[7] : Texture2D.blackTexture);

   }

   protected void SyncBlendMat(Vector3 size)
   {
      if (blendMatInstance != null && matInstance != null)
      {
         blendMatInstance.CopyPropertiesFromMaterial(matInstance);
         Vector4 bnds = new Vector4();
         bnds.z = size.x;
         bnds.w = size.z;
         bnds.x = transform.position.x;
         bnds.y = transform.position.z;
         blendMatInstance.SetVector("_TerrainBounds", bnds);
         blendMatInstance.SetTexture("_TerrainDesc", terrainDesc);
      }
   }

   public virtual Bounds GetBounds() { return new Bounds();  }


   public Material GetBlendMatInstance()
   {
      if (blendMat != null && terrainDesc != null)
      {
         if (blendMatInstance == null)
         {
            blendMatInstance = new Material(blendMat);
            SyncBlendMat(GetBounds().size);
         }
         if (blendMatInstance.shader != blendMat.shader)
         {
            blendMatInstance.shader = blendMat.shader;
            SyncBlendMat(GetBounds().size);
         }
      }
      return blendMatInstance;
   }

   protected void ApplyBlendMap()
   {
      if (blendMat != null && terrainDesc != null)
      {
         if (blendMatInstance == null)
         {
            blendMatInstance = new Material(blendMat);
         }

         SyncBlendMat(GetBounds().size); 
      }  
   }

   public void RevisionFromMat()
   {
#if UNITY_EDITOR
      if (keywordSO == null && templateMaterial != null)
      {
         var path = UnityEditor.AssetDatabase.GetAssetPath(templateMaterial);
         path = path.Replace(".mat", "_keywords.asset");
         keywordSO = UnityEditor.AssetDatabase.LoadAssetAtPath<MicroSplatKeywords>(path);
         if (keywordSO == null)
         {
            keywordSO = ScriptableObject.CreateInstance<MicroSplatKeywords>();
            keywordSO.keywords = new List<string>(templateMaterial.shaderKeywords);
            UnityEditor.AssetDatabase.CreateAsset(keywordSO, path);
            UnityEditor.AssetDatabase.SaveAssets();
            templateMaterial.shaderKeywords = null;
         }
         UnityEditor.EditorUtility.SetDirty(this);
      }
#endif
   }

   public static void SyncAll()
   {
      MicroSplatTerrain.SyncAll ();
#if __MICROSPLAT_MESHTERRAIN__
      MicroSplatMeshTerrain.SyncAll();
#endif
#if __MICROSPLAT_MESH__
      MicroSplatMesh.SyncAll();
      MicroSplatVertexMesh.SyncAll ();
#endif
#if __MICROSPLAT_POLARIS__
      MicroSplatPolarisMesh.SyncAll ();
#endif
   }
}
