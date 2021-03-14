﻿//////////////////////////////////////////////////////
// MicroSplat
// Copyright (c) Jason Booth
//////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using JBooth.MicroSplat;

#if __MICROSPLAT__
public partial class MicroSplatTerrainEditor : Editor 
{


   public static void GenerateTerrainNormalMap(MicroSplatTerrain bt)
   {
      Terrain t = bt.GetComponent<Terrain>();
      int w = t.terrainData.heightmapResolution;
      int h = t.terrainData.heightmapResolution;

      Texture2D data = new Texture2D(w, h, TextureFormat.RGBA32, true, true);
      for (int x = 0; x < w; ++x)
      {
         for (int y = 0; y < h; ++y)
         {
            Vector3 normal = t.terrainData.GetInterpolatedNormal((float)x / w, (float)y / h);
            data.SetPixel(x, y, new Color(normal.x * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, normal.y));
         }
      }
      data.Apply();

      var path = MicroSplatUtilities.RelativePathFromAsset(t.terrainData);
      path += "/" + t.name + "_normal.png";
      var bytes = data.EncodeToPNG();
      System.IO.File.WriteAllBytes(path, bytes);
      GameObject.DestroyImmediate(data);
      AssetDatabase.Refresh();
      bt.perPixelNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
      var ai = AssetImporter.GetAtPath(path);
      var ti = ai as TextureImporter;
      var ps = ti.GetDefaultPlatformTextureSettings();

      if (ti.isReadable == true ||
         ti.wrapMode != TextureWrapMode.Clamp ||
         ps.overridden != true ||
         ti.textureType != TextureImporterType.NormalMap)

      {
         ti.textureType = TextureImporterType.NormalMap;
         ti.mipmapEnabled = true;
         ti.wrapMode = TextureWrapMode.Clamp;
         ti.isReadable = false;
         ps.overridden = true;
         ti.SetPlatformTextureSettings(ps);
         ti.SaveAndReimport();
      }
      bt.sTerrainDirty = false;
      EditorUtility.SetDirty(bt);
      EditorUtility.SetDirty(bt.terrain);
      MicroSplatTerrain.SyncAll();
      AssetDatabase.SaveAssets();
   }

   public static void GenerateTerrainBlendData(MicroSplatTerrain bt)
   {
      Terrain t = bt.GetComponent<Terrain>();
      int w = t.terrainData.heightmapResolution;
      int h = t.terrainData.heightmapResolution;

      Texture2D data = null;
      if (bt.descriptorFormat == MicroSplatObject.DescriptorFormat.RGBAHalf)
      {
         data = new Texture2D (w, h, TextureFormat.RGBAHalf, true, true);
      }
      else
      {
         data = new Texture2D (w, h, TextureFormat.RGBAFloat, true, true);
      }



      for (int x = 0; x < w; ++x)
      {
         for (int y = 0; y < h; ++y)
         {
            float height = t.terrainData.GetHeight(x, y);
            Vector3 normal = t.terrainData.GetInterpolatedNormal((float)x / w, (float)y / h);
            // When you save a texture to EXR format, either in the saving or import stage,
            // some type of gamma curve is applied regardless of the fact that the textures is
            // set to linear. So we pow it here to counteract it, whis is total BS, but works..
            normal.x = (normal.x >= 0) ? Mathf.Pow(normal.x, 2.0f) : Mathf.Pow(normal.x, 2) * -1;
            normal.z = (normal.z >= 0) ? Mathf.Pow(normal.z, 2.0f) : Mathf.Pow(normal.z, 2) * -1;
            data.SetPixel(x, y, new Color(normal.x, normal.y, normal.z, height));
         }
      }
      data.Apply();

      var path = MicroSplatUtilities.RelativePathFromAsset(t.terrainData);
      path += "/" + t.name + ".exr";
      var bytes = data.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
      System.IO.File.WriteAllBytes(path, bytes);
      GameObject.DestroyImmediate(data);
      AssetDatabase.Refresh();
      bt.terrainDesc = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
      var ai = AssetImporter.GetAtPath(path);
      var ti = ai as TextureImporter;


      // default platform no longer supports RGBA half/float in newer unity, so we override all platforms
      bool changed = false;
      var platforms = System.Enum.GetNames (typeof(BuildTarget));
      for (int i = 0; i < platforms.Length; ++i)
      {
         string platform = platforms [i];
         var ps = ti.GetPlatformTextureSettings (platform);

         if (ti.isReadable == true ||
            ti.wrapMode != TextureWrapMode.Clamp ||
           (ps.format != TextureImporterFormat.RGBAHalf && ps.format != TextureImporterFormat.RGBAFloat) ||
            ps.textureCompression != TextureImporterCompression.Uncompressed ||
            ps.overridden != true ||
            ti.filterMode != FilterMode.Bilinear ||
            ti.sRGBTexture != false)
         {
            ti.sRGBTexture = false;
            ti.filterMode = FilterMode.Bilinear;
            ti.mipmapEnabled = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.isReadable = false;

            if (bt.descriptorFormat == MicroSplatObject.DescriptorFormat.RGBAHalf)
            {
               ps.format = TextureImporterFormat.RGBAHalf;
            }
            else
            {
               ps.format = TextureImporterFormat.RGBAFloat;
            }

            
            ps.textureCompression = TextureImporterCompression.Uncompressed;
            ps.overridden = true;
            try
            {
               ti.SetPlatformTextureSettings (ps);
               changed = true;
            }
            catch 
            {
            }
         }
      }
      if (changed)
      {
         ti.SaveAndReimport ();
      }
      bt.sTerrainDirty = false;
      EditorUtility.SetDirty(bt);
      EditorUtility.SetDirty(bt.terrain);
      MicroSplatTerrain.SyncAll();
      AssetDatabase.SaveAssets();
   }


#if __MICROSPLAT_PROCTEX__
   public static void ComputeCavityFlowMap(MicroSplatTerrain bt)
   {
      Terrain t = bt.terrain;
      Texture2D data = new Texture2D(t.terrainData.heightmapResolution, t.terrainData.heightmapResolution, TextureFormat.RGBA32, true, true);
      CurvatureMapGenerator.CreateMap(t.terrainData.GetHeights(0, 0, data.width, data.height), data);

      var path = MicroSplatUtilities.RelativePathFromAsset(t.terrainData);
      path += "/" + t.name + "_cavity.png";
      var bytes = data.EncodeToPNG();
      System.IO.File.WriteAllBytes(path, bytes);
      GameObject.DestroyImmediate(data);
      AssetDatabase.Refresh();
      bt.cavityMap = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
      var ai = AssetImporter.GetAtPath(path);
      var ti = ai as TextureImporter;

      if (ti.sRGBTexture != false)
      {
         ti.sRGBTexture = false;
         ti.SaveAndReimport();
      }
      bt.sTerrainDirty = false;
      EditorUtility.SetDirty(bt);
      EditorUtility.SetDirty(t);
      MicroSplatTerrain.SyncAll();
      AssetDatabase.SaveAssets();
   }
#endif


   static GUIContent CTerrainDesc = new GUIContent ("Terrain Descriptor", "Holds information about the terrain for dynamic streams or terrain blending");
   static GUIContent CPerPixelNormal = new GUIContent ("Per Pixel Normal", "Per Pixel normal map");
   public void DoTerrainDescGUI()
   {
      MicroSplatTerrain bt = target as MicroSplatTerrain;
      Terrain t = bt.GetComponent<Terrain>();
      if (t == null || t.terrainData == null)
      {
         return;
      }
      if (t.materialTemplate == null)
      {
         return;
      }
      if (bt.keywordSO == null)
      {
         return;
      }

      if (!bt.keywordSO.IsKeywordEnabled("_TERRAINBLENDING") && !bt.keywordSO.IsKeywordEnabled("_DYNAMICFLOWS"))
      {
         return;
      }
      EditorGUILayout.Space();
      

      if (bt.terrainDesc == null)
      {
         EditorGUILayout.HelpBox("Terrain Descriptor Data is not present, please generate", MessageType.Info);
      }
         
      if (bt.terrainDesc != null && bt.sTerrainDirty)
      {
         EditorGUILayout.HelpBox("Terrain Descriptor data is out of date, please update", MessageType.Info);
      }

      bt.descriptorFormat = (MicroSplatObject.DescriptorFormat)EditorGUILayout.EnumPopup ("Descriptor Format", bt.descriptorFormat);

      MicroSplatUtilities.DrawTextureField (bt, CTerrainDesc, ref bt.terrainDesc, "_TERRAINBLENDING", "_DYNAMICFLOWS");
      if (bt.terrainDesc != null)
      {
         EditorGUILayout.BeginHorizontal ();
         int mem = bt.terrainDesc.width * bt.terrainDesc.height;
         mem /= 128;
         EditorGUILayout.LabelField ("Terrain Descriptor Data Memory: " + mem.ToString () + "kb");
         EditorGUILayout.EndHorizontal ();
      }

      if (GUILayout.Button(bt.terrainDesc == null ? "Generate Terrain Descriptor Data" : "Update Terrain Descriptor Data"))
      {
         GenerateTerrainBlendData(bt);
      }

      if (bt.terrainDesc != null && GUILayout.Button("Clear Terrain Descriptor Data"))
      {
         bt.terrainDesc = null;
      }

      

      if (bt.blendMat == null && bt.templateMaterial != null && bt.keywordSO != null && bt.keywordSO.IsKeywordEnabled("_TERRAINBLENDING"))
      {
         var path = AssetDatabase.GetAssetPath(bt.templateMaterial);
         path = path.Replace(".mat", "_TerrainObjectBlend.mat");
         bt.blendMat = AssetDatabase.LoadAssetAtPath<Material>(path);
         if (bt.blendMat == null)
         {
            string shaderPath = path.Replace(".mat", ".shader");
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
			if (shader == null) 
			{
				shaderPath = AssetDatabase.GetAssetPath(bt.templateMaterial.shader);
				shaderPath = shaderPath.Replace(".shader", "_TerrainObjectBlend.shader");
				shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
			}
            if (shader != null)
            {
               Material mat = new Material(shader);
               AssetDatabase.CreateAsset(mat, path);
               AssetDatabase.SaveAssets();
               MicroSplatTerrain.SyncAll();
            }
         }
      }
      EditorUtility.SetDirty(bt);
      EditorUtility.SetDirty(bt.terrain);
   }


   public void DoPerPixelNormalGUI()
   {
      MicroSplatTerrain bt = target as MicroSplatTerrain;
      Terrain t = bt.GetComponent<Terrain>();
      if (t == null || t.terrainData == null)
      {
         EditorGUILayout.HelpBox("No Terrain data found", MessageType.Error);
         return;
      }
      if (t.materialTemplate == null)
      {
         return;
      }

      if (bt.keywordSO == null)
         return;

      if (!bt.keywordSO.IsKeywordEnabled("_PERPIXNORMAL"))
      {
         bt.perPixelNormal = null;
         return;
      }

      if (bt.terrain.drawInstanced && bt.perPixelNormal)
      {
         EditorGUILayout.HelpBox("Per Pixel Normal is assigned, but shader is using Instance rendering, which automatically provides per-pixel normal. You may turn off per pixel normal and clear the normal data to save memory.", MessageType.Warning);
      }

      MicroSplatUtilities.DrawTextureField (bt, CPerPixelNormal, ref bt.perPixelNormal, "_PERPIXNORMAL");
      if (bt.perPixelNormal == null)
      {
         EditorGUILayout.HelpBox("Terrain Normal Data is not present, please generate", MessageType.Warning);
      }

      if (bt.perPixelNormal != null && bt.sTerrainDirty)
      {
         EditorGUILayout.HelpBox("Terrain Normal data is out of date, please update", MessageType.Warning);
      }
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.PrefixLabel("Normal Data");
      if (GUILayout.Button(bt.perPixelNormal == null ? "Generate" : "Update"))
      {
         GenerateTerrainNormalMap(bt);
         EditorUtility.SetDirty(bt);
         EditorUtility.SetDirty(bt.terrain);
      }

      if (bt.perPixelNormal != null && GUILayout.Button("Clear"))
      {
         bt.perPixelNormal = null;
         EditorUtility.SetDirty(bt);
         EditorUtility.SetDirty(bt.terrain);
      }

      EditorGUILayout.EndHorizontal();



   }

#if __MICROSPLAT_PROCTEX__
   public void DoCavityMapGUI()
   {
      MicroSplatTerrain bt = target as MicroSplatTerrain;
      Terrain t = bt.GetComponent<Terrain>();
      if (t == null || t.terrainData == null)
      {
         EditorGUILayout.HelpBox("No Terrain data found", MessageType.Error);
         return;
      }
      if (t.materialTemplate == null)
      {
         return;
      }

      if (bt.keywordSO == null)
         return;

      if (bt.cavityMap == null)
      {
         EditorGUILayout.HelpBox("Cavity Map Data is not present, please generate or provide", MessageType.Warning);

      }
      bt.cavityMap = (Texture2D)EditorGUILayout.ObjectField("Cavity Map", bt.cavityMap, typeof(Texture2D), false);
      if (bt.cavityMap != null && bt.sTerrainDirty)
      {
         EditorGUILayout.HelpBox("Cavity data may be out of date as terrain has changed", MessageType.Warning);
      }
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.PrefixLabel("Cavity Data");
      if (GUILayout.Button(bt.cavityMap == null ? "Generate" : "Update"))
      {
         ComputeCavityFlowMap(bt);
      }

      if (bt.cavityMap != null && GUILayout.Button("Clear"))
      {
         bt.cavityMap = null;
      }
      EditorGUILayout.EndHorizontal();



   }
#endif
}
#endif
