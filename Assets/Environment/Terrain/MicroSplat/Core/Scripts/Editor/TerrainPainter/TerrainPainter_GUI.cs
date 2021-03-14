﻿//////////////////////////////////////////////////////
// MicroSplat
// Copyright (c) Jason Booth
//////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;


namespace JBooth.MicroSplat
{
#if __MICROSPLAT__ && (__MICROSPLAT_STREAMS__ || __MICROSPLAT_GLOBALTEXTURE__ || __MICROSPLAT_SNOW__ || __MICROSPLAT_SCATTER__)
   public partial class TerrainPainterWindow : EditorWindow 
   {

      enum Tab
      {
         TintMap,
         SnowMin,
         SnowMax,
         Wetness,
         Puddles,
         Streams,
         Lava,
         Scatter,
      }

      string[] tabNames =
      {
         "Tint Map",
         "Snow Min",
         "Snow Max",
         "Wetness",
         "Puddles",
         "Streams",
         "Lava",
         "Scatter",
      };

      Tab tab = Tab.Wetness;



      Texture2D SaveTexture(string path, Texture2D tex, bool overwrite = false)
      {
         if (overwrite || !System.IO.File.Exists(path))
         {

#if UNITY_2019_1_OR_NEWER
            var bytes = tex.EncodeToTGA();
            path = path.Replace (".png", ".tga");
#else
            var bytes = tex.EncodeToPNG();
#endif

            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            AssetImporter ai = AssetImporter.GetAtPath(path);
            TextureImporter ti = ai as TextureImporter;
            ti.sRGBTexture = false;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            var ftm = ti.GetDefaultPlatformTextureSettings();
            ftm.format = TextureImporterFormat.RGBA32;
            ti.SetPlatformTextureSettings(ftm);

            ti.mipmapEnabled = true;
            ti.isReadable = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();
         }
         return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
      }

      Texture2D CreateTexture(Terrain t, Texture2D tex, string texNamePostfix, Color defaultColor)
      {
         // find/create manager
         var mgr = t.GetComponent<MicroSplatTerrain>();
         if (mgr == null)
         {
            return null;
         }

         // if we still don't have a texture, create one
         if (tex == null)
         {
            tex = new Texture2D(t.terrainData.alphamapWidth, t.terrainData.alphamapHeight, TextureFormat.RGBA32, false, true);
            
            for (int x = 0; x < tex.width; ++x)
            {
               for (int y = 0; y < tex.height; ++y)
               {
                  tex.SetPixel(x, y, defaultColor);
               }
            }
            tex.Apply();
            tex.wrapMode = TextureWrapMode.Clamp;
            var path = MicroSplatUtilities.RelativePathFromAsset(t.terrainData);
            path += "/" + t.name + texNamePostfix + ".png";
            tex = SaveTexture(path, tex);
            mgr.Sync ();
         }

         
         return tex;
      }

      bool VerifyStreams ()
      {
#if __MICROSPLAT_STREAMS__
         for (int i = 0; i < rawTerrains.Count; ++i)
         {
            Terrain t = rawTerrains [i];
            MicroSplatTerrain mst = t.GetComponent<MicroSplatTerrain> ();

            if (mst != null)
            {
               var streamTex = mst.streamTexture;
               if (streamTex != null)
               {
                  AssetImporter ai = AssetImporter.GetAtPath (AssetDatabase.GetAssetPath (streamTex));
                  TextureImporter ti = ai as TextureImporter;
                  if (ti == null || !ti.isReadable)
                  {
                     EditorGUILayout.HelpBox ("Stream control texture is not read/write", MessageType.Error);
                     if (GUILayout.Button ("Fix it!"))
                     {
                        ti.isReadable = true;
                        ti.SaveAndReimport ();
                     }
                     return false;
                  }

                  bool isLinear = ti.sRGBTexture == false;
                  bool isRGB32 = ti.textureCompression == TextureImporterCompression.Uncompressed && ti.GetDefaultPlatformTextureSettings ().format == TextureImporterFormat.RGBA32;

                  if (isRGB32 == false || isLinear == false || ti.wrapMode == TextureWrapMode.Repeat)
                  {
                     EditorGUILayout.HelpBox ("Stream control texture is not in the correct format (Uncompressed, linear, clamp, RGBA32)", MessageType.Error);
                     if (GUILayout.Button ("Fix it!"))
                     {

                        ti.sRGBTexture = false;
                        ti.textureCompression = TextureImporterCompression.Uncompressed;
                        var ftm = ti.GetDefaultPlatformTextureSettings ();
                        ftm.format = TextureImporterFormat.RGBA32;
                        ti.SetPlatformTextureSettings (ftm);

                        ti.mipmapEnabled = true;
                        ti.wrapMode = TextureWrapMode.Clamp;
                        ti.SaveAndReimport ();
                     }
                     return false;
                  }
               }
            }
         }
#endif

         return true;
      }

      bool VerifyTint ()
      {
#if __MICROSPLAT_GLOBALTEXTURE__
         for (int i = 0; i < rawTerrains.Count; ++i)
         {
            Terrain t = rawTerrains [i];
            MicroSplatTerrain mst = t.GetComponent<MicroSplatTerrain> ();

            if (mst != null)
            {
               var tex = mst.tintMapOverride;
               if (tex != null)
               {
                  AssetImporter ai = AssetImporter.GetAtPath (AssetDatabase.GetAssetPath (tex));
                  TextureImporter ti = ai as TextureImporter;
                  if (ti == null || !ti.isReadable)
                  {
                     EditorGUILayout.HelpBox ("Tint texture is not read/write", MessageType.Error);
                     if (GUILayout.Button ("Fix it!"))
                     {
                        ti.isReadable = true;
                        ti.SaveAndReimport ();
                     }
                     return false;
                  }

                  bool isLinear = ti.sRGBTexture == false;
                  bool isRGB32 = ti.textureCompression == TextureImporterCompression.Uncompressed && ti.GetDefaultPlatformTextureSettings ().format == TextureImporterFormat.RGBA32;

                  if (isRGB32 == false || isLinear == true || ti.wrapMode == TextureWrapMode.Repeat)
                  {
                     EditorGUILayout.HelpBox ("Tint texture is not in the correct format (Uncompressed, sRGB, clamp, RGBA32)", MessageType.Error);
                     if (GUILayout.Button ("Fix it!"))
                     {

                        ti.sRGBTexture = true;
                        ti.textureCompression = TextureImporterCompression.Uncompressed;
                        var ftm = ti.GetDefaultPlatformTextureSettings ();
                        ftm.format = TextureImporterFormat.RGBA32;
                        ti.SetPlatformTextureSettings (ftm);

                        ti.mipmapEnabled = true;
                        ti.wrapMode = TextureWrapMode.Clamp;
                        ti.SaveAndReimport ();
                     }
                     return false;
                  }
               }
            }
         }
#endif

         return true;
      }

      bool VerifySnow ()
      {
#if __MICROSPLAT_SNOW__
         for (int i = 0; i < rawTerrains.Count; ++i)
         {
            Terrain t = rawTerrains [i];
            MicroSplatTerrain mst = t.GetComponent<MicroSplatTerrain> ();

            if (mst != null)
            {
               var tex = mst.snowMaskOverride;
               if (tex != null)
               {
                  AssetImporter ai = AssetImporter.GetAtPath (AssetDatabase.GetAssetPath (tex));
                  TextureImporter ti = ai as TextureImporter;
                  if (ti == null || !ti.isReadable)
                  {
                     EditorGUILayout.HelpBox ("Snow Mask texture is not read/write", MessageType.Error);
                     if (GUILayout.Button ("Fix it!"))
                     {
                        ti.isReadable = true;
                        ti.SaveAndReimport ();
                     }
                     return false;
                  }

                  bool isLinear = ti.sRGBTexture == false;
                  bool isRGB32 = ti.textureCompression == TextureImporterCompression.Uncompressed && ti.GetDefaultPlatformTextureSettings ().format == TextureImporterFormat.RGBA32;

                  if (isRGB32 == false || isLinear == false || ti.wrapMode == TextureWrapMode.Repeat)
                  {
                     EditorGUILayout.HelpBox ("Snow Mask is not in the correct format (Uncompressed, linear, clamp, RGBA32)", MessageType.Error);
                     if (GUILayout.Button ("Fix it!"))
                     {

                        ti.sRGBTexture = false;
                        ti.textureCompression = TextureImporterCompression.Uncompressed;
                        var ftm = ti.GetDefaultPlatformTextureSettings ();
                        ftm.format = TextureImporterFormat.RGBA32;
                        ti.SetPlatformTextureSettings (ftm);

                        ti.mipmapEnabled = true;
                        ti.wrapMode = TextureWrapMode.Clamp;
                        ti.SaveAndReimport ();
                     }
                     return false;
                  }
               }
            }
         }
#endif


         return true;
      }


      bool VerifyScatter ()
      {
#if __MICROSPLAT_SCATTER__
         for (int i = 0; i < rawTerrains.Count; ++i)
         {
            Terrain t = rawTerrains [i];
            MicroSplatTerrain mst = t.GetComponent<MicroSplatTerrain> ();

            if (mst != null)
            {
               var tex = mst.scatterMapOverride;
               if (tex != null)
               {
                  AssetImporter ai = AssetImporter.GetAtPath (AssetDatabase.GetAssetPath (tex));
                  TextureImporter ti = ai as TextureImporter;
                  if (ti == null || !ti.isReadable)
                  {
                     EditorGUILayout.HelpBox ("Scatter Control texture is not read/write", MessageType.Error);
                     if (GUILayout.Button ("Fix it!"))
                     {
                        ti.isReadable = true;
                        ti.SaveAndReimport ();
                     }
                     return false;
                  }

                  bool isLinear = ti.sRGBTexture == false;
                  bool isRGB32 = ti.textureCompression == TextureImporterCompression.Uncompressed && ti.GetDefaultPlatformTextureSettings ().format == TextureImporterFormat.RGBA32;

                  if (isRGB32 == false || isLinear == false || ti.wrapMode == TextureWrapMode.Repeat)
                  {
                     EditorGUILayout.HelpBox ("Scatter Control is not in the correct format (Uncompressed, linear, clamp, RGBA32)", MessageType.Error);
                     if (GUILayout.Button ("Fix it!"))
                     {

                        ti.sRGBTexture = false;
                        ti.textureCompression = TextureImporterCompression.Uncompressed;
                        var ftm = ti.GetDefaultPlatformTextureSettings ();
                        ftm.format = TextureImporterFormat.RGBA32;
                        ti.SetPlatformTextureSettings (ftm);

                        ti.mipmapEnabled = true;
                        ti.wrapMode = TextureWrapMode.Clamp;
                        ti.SaveAndReimport ();
                     }
                     return false;
                  }
               }
            }
         }
#endif


         return true;
      }

      bool VerifyData ()
      {
         if (rawTerrains == null || rawTerrains.Count == 0)
            return false;

         for (int i = 0; i < rawTerrains.Count; ++i)
         {
            Terrain t = rawTerrains [i];
            MicroSplatObject mso = t.GetComponent<MicroSplatObject> ();
            if (t.materialTemplate == null || mso == null || !mso.keywordSO.IsKeywordEnabled ("_MICROSPLAT"))
            {
               EditorGUILayout.HelpBox ("Terrain(s) are not setup for MicroSplat, please set them up", MessageType.Error);
               return false;
            }
         }
         return true;
      }
         

      void DrawFillGUI()
      {
         EditorGUILayout.BeginHorizontal();
         if (GUILayout.Button("Fill"))
         {
            if (OnBeginStroke != null)
            {
               OnBeginStroke(terrains);
            }
            for (int i = 0; i < terrains.Length; ++i)
            {
               FillTerrain(terrains[i], paintValue);
               if (OnStokeModified != null)
               {
                  OnStokeModified(terrains[i], true);
               }
            }
            if (OnEndStroke != null)
            {
               OnEndStroke();
            }
         }
         if (GUILayout.Button("Clear"))
         {
            if (OnBeginStroke != null)
            {
               OnBeginStroke(terrains);
            }
            for (int i = 0; i < terrains.Length; ++i)
            {
               FillTerrain(terrains[i], 0);
               if (OnStokeModified != null)
               {
                  OnStokeModified(terrains[i], true);
               }
            }
            if (OnEndStroke != null)
            {
               OnEndStroke();
            }
         }
         EditorGUILayout.EndHorizontal();
      }

      int scatterIndex = 0;

#if __MICROSPLAT_SCATTER__
      enum ScatterLayer
      {
         First,
         Second
      }
      ScatterLayer scatterLayer = ScatterLayer.First;
      static GUIContent CScatterLayer = new GUIContent ("Layer", "Scatter supports painting on two layers");
#endif

      void DrawScatterGUI ()
      {
         if (MicroSplatUtilities.DrawRollup ("Brush Settings"))
         {

            brushSize = EditorGUILayout.Slider ("Brush Size", brushSize, 0.01f, 30.0f);
            brushFlow = EditorGUILayout.Slider ("Brush Flow", brushFlow, 0.1f, 128.0f);
            brushFalloff = EditorGUILayout.Slider ("Brush Falloff", brushFalloff, 0.1f, 3.5f);

            
            Material tempMat = null;
            for (int i = 0; i < rawTerrains.Count; ++i)
            {
               Terrain t = rawTerrains [i];
               MicroSplatTerrain mst = t.GetComponent<MicroSplatTerrain> ();
               if (mst != null)
               {
                  if (mst.templateMaterial != null && mst.templateMaterial.HasProperty("_ScatterDiffuse"))
                  {
                     Texture2DArray diff = mst.templateMaterial.GetTexture ("_ScatterDiffuse") as Texture2DArray;
                     scatterIndex = MicroSplatUtilities.DrawTextureSelector (scatterIndex, diff, false);
                     tempMat = mst.templateMaterial;
                  }
                  else
                  {
                     scatterIndex = EditorGUILayout.IntField ("Scatter Index", scatterIndex);
                  }
               }
               else
               {
                  scatterIndex = EditorGUILayout.IntField ("Scatter Index", scatterIndex);
               }
            }

               
            //EditorGUILayout.MinMaxSlider (CSlopeRange, ref slopeRange.x, ref slopeRange.y, 0.0f, 1.0f);

            paintValue = EditorGUILayout.Slider ("Target Opacity", paintValue, 0.0f, 1.0f);
            
            

#if __MICROSPLAT_SCATTER__
            if (tempMat != null)
            {
               scatterLayer = (ScatterLayer)EditorGUILayout.EnumPopup (CScatterLayer, scatterLayer);
               EditorGUILayout.Separator ();

               using (new GUILayout.VerticalScope (GUI.skin.box))
               {
                  EditorGUI.BeginChangeCheck ();
                  EditorGUILayout.LabelField ("Per Texture Properties");
                  bool changed = MicroSplatScatter.DrawPerTexExternal (tempMat, scatterIndex);
                  EditorGUILayout.Separator ();
                  // sync compile changes
                  if (changed)
                  {
                     MicroSplatShaderGUI.MicroSplatCompiler comp = new MicroSplatShaderGUI.MicroSplatCompiler ();
                     comp.Init ();
                     comp.Compile (tempMat);
                  }
                  // sync property changes
                  if (EditorGUI.EndChangeCheck())
                  {
                     MicroSplatObject.SyncAll ();
                  }
               }
               
            }

#endif
            GUILayout.Box ("", new GUILayoutOption [] { GUILayout.ExpandWidth (true), GUILayout.Height (1) });
            EditorGUILayout.Separator ();
         }
         DrawFillGUI ();
      }

      void DrawWetnessGUI()
      {
         if (MicroSplatUtilities.DrawRollup("Brush Settings"))
         {
            DrawBrushSettingsGUI();
         }
         DrawFillGUI();
         if (MicroSplatUtilities.DrawRollup("Raycast Wetness", true, true))
         {
            EditorGUILayout.HelpBox("This utility will raycast against your terrain, generating a wetness map which will wet uncovered terrain. You can then use the maximum wetess value to remove the effect, raising it when it rains", MessageType.Info);
            if (GUILayout.Button("Calculate"))
            {
               DoWetnessRaycast();
            }
         }
      }

      Vector2 slopeRange = new Vector2(0, 1);
      static GUIContent CSlopeRange = new GUIContent("Slope Range", "Filter strokes to only affect certain angles");

      void DoWetnessRaycast()
      {
         for (int i = 0; i < terrains.Length; ++i)
         {
            var terrain = terrains[i];
            var tex = terrain.streamTex;
            RaycastHit hit;
            for (int x = 0; x < tex.width; ++x)
            {
               for (int y = 0; y < tex.height; ++y)
               {
                  Vector3 tp = TerrainToWorld(terrain.terrain, x, y, tex);
                  tp += Vector3.up * 500;
                  Ray ray = new Ray(tp, Vector3.down);
                  bool val = false;
                  if  (Physics.Raycast(ray, out hit))
                  {
                     if (hit.collider == terrain.collider || hit.collider.GetComponent<Terrain>() != null)
                     {
                        val = true;
                     }
                  }
                  Color c = tex.GetPixel(x, y);
                  c.r = val ? 1 : 0;
                  tex.SetPixel(x, y, c);
               }
            }
            tex.Apply();
         }
      }

      void DoSnowRaycast ()
      {
         for (int i = 0; i < terrains.Length; ++i)
         {
            var terrain = terrains [i];
            var tex = terrain.snowTex;
            RaycastHit hit;
            for (int x = 0; x < tex.width; ++x)
            {
               for (int y = 0; y < tex.height; ++y)
               {
                  Vector3 tp = TerrainToWorld (terrain.terrain, x, y, tex);
                  tp += Vector3.up * 500;
                  Ray ray = new Ray (tp, Vector3.down);
                  bool val = false;
                  if (Physics.Raycast (ray, out hit))
                  {
                     if (hit.collider == terrain.collider || hit.collider.GetComponent<Terrain> () != null)
                     {
                        val = true;
                     }
                  }
                  Color c = tex.GetPixel (x, y);
                  c.r = val ? 1 : 0;
                  tex.SetPixel (x, y, c);
               }
            }
            tex.Apply ();
         }
      }


      void DrawPuddlesGUI()
      {
         if (MicroSplatUtilities.DrawRollup("Brush Settings"))
         {
            DrawBrushSettingsGUI();
         }
         DrawFillGUI();

      }
         
      void DrawStreamGUI()
      {
         if (MicroSplatUtilities.DrawRollup("Brush Settings"))
         {
            DrawBrushSettingsGUI();
         }
         DrawFillGUI();
      }
         
      void DrawLavaGUI()
      {
         if (MicroSplatUtilities.DrawRollup("Brush Settings"))
         {
            DrawBrushSettingsGUI();
         }
         DrawFillGUI();
      }

      void DrawTintGUI ()
      {
         if (MicroSplatUtilities.DrawRollup ("Brush Settings"))
         {
            DrawBrushSettingsGUI (true);
         }
         DrawFillGUI ();
      }

      void DrawSnowMaskGUI ()
      {
         if (MicroSplatUtilities.DrawRollup ("Brush Settings"))
         {
            DrawBrushSettingsGUI ();
         }
         DrawFillGUI ();

         if (MicroSplatUtilities.DrawRollup ("Raycast Snow", true, true))
         {
            EditorGUILayout.HelpBox ("This utility will raycast against your terrain, generating a snow mask which will prevent snow on covered terrain.", MessageType.Info);
            if (GUILayout.Button ("Calculate"))
            {
               DoSnowRaycast ();
            }
         }
      }

      void ShowGlobalTexAd()
      {
         EditorGUILayout.HelpBox ("Global Texturing Module is not installed", MessageType.Info);
         if (GUILayout.Button ("Get"))
         {
            Application.OpenURL (MicroSplatDefines.link_globalTexture);
         }
      }

      void ShowSnowAd ()
      {
         EditorGUILayout.HelpBox ("Snow Module is not installed", MessageType.Info);
         if (GUILayout.Button ("Get"))
         {
            Application.OpenURL (MicroSplatDefines.link_snow);
         }
      }

      void ShowStreamsAd()
      {
         EditorGUILayout.HelpBox ("Wetness, Puddles, Streams, Lava module is not installed", MessageType.Info);
         if (GUILayout.Button ("Get"))
         {
            Application.OpenURL (MicroSplatDefines.link_streams);
         }

      }

      void ShowScatterAd ()
      {
         EditorGUILayout.HelpBox ("Scatter module is not installed", MessageType.Info);
         if (GUILayout.Button ("Get"))
         {
            Application.OpenURL (MicroSplatDefines.link_streams);
         }

      }


      Vector2 scroll;
      void OnGUI()
      {
         if (VerifyData() == false)
         {
            EditorGUILayout.HelpBox("Please select a terrain to begin", MessageType.Info);
            return;
         }

         DrawSettingsGUI();
         DrawSaveGUI();
         tab = (Tab)GUILayout.Toolbar((int)tab, tabNames);

         bool hasWetness = false;
         bool hasPuddles = false;
         bool hasStreams = false;
         bool hasLava = false;
         bool hasTint = false;
         bool hasSnowMask = false;
         bool hasScatter = false;

         for (int i = 0; i < terrains.Length; ++i)
         {
            var t = terrains[i];
            if (t == null || t.terrain == null)
               continue;

            var mso = t.terrain.GetComponent<MicroSplatObject>();
            if (t.terrain.terrainData != null && t.terrain.materialTemplate != null && mso != null && mso.keywordSO != null)
            {
               if (!hasWetness)
                  hasWetness = mso.keywordSO.IsKeywordEnabled("_WETNESS");
               if (!hasPuddles)
                  hasPuddles = mso.keywordSO.IsKeywordEnabled("_PUDDLES");
               if (!hasStreams)
                  hasStreams = mso.keywordSO.IsKeywordEnabled("_STREAMS");
               if (!hasLava)
                  hasLava = mso.keywordSO.IsKeywordEnabled("_LAVA");
               if (!hasScatter)
                  hasScatter = mso.keywordSO.IsKeywordEnabled ("_SCATTER");
               if (!hasTint)
                  hasTint = mso.keywordSO.IsKeywordEnabled ("_GLOBALTINT");
               if (!hasSnowMask)
                  hasSnowMask = mso.keywordSO.IsKeywordEnabled ("_SNOWMASK");

            }
         }

         if (tab == Tab.TintMap)
         {
#if __MICROSPLAT_GLOBALTEXTURE__
            if (hasTint && VerifyTint())
            {
               DrawTintGUI ();
            }
            else
            {
               EditorGUILayout.HelpBox ("Global Tint Map is not enabled on your terrain, please enable in the shader options if you want to paint it", MessageType.Warning);
            }

#else
            ShowGlobalTexAd ();
#endif
         }
         else if (tab == Tab.SnowMax)
         {
#if __MICROSPLAT_SNOW__
            if (hasSnowMask && VerifySnow())
            {
               DrawSnowMaskGUI ();
            }
            else
            {
               EditorGUILayout.HelpBox ("Snow Mask is not enabled on your terrain, please enable in the shader options if you want to paint a snow mask", MessageType.Warning);
            }

#else
            ShowSnowAd();
#endif
         }
         else if (tab == Tab.SnowMin)
         {
#if __MICROSPLAT_SNOW__
            if (hasSnowMask && VerifySnow())
            {
               DrawSnowMaskGUI ();
            }
            else
            {
               EditorGUILayout.HelpBox ("Snow Mask is not enabled on your terrain, please enable in the shader options if you want to paint a snow mask", MessageType.Warning);
            }
#else
            ShowSnowAd();
#endif
         }

         else if (tab == Tab.Wetness)
         {
#if __MICROSPLAT_STREAMS__
            if (hasWetness && VerifyStreams())
            {
               DrawWetnessGUI();
            }
            else
            {
               EditorGUILayout.HelpBox("Wetness is not enabled on your terrain, please enable in the shader options if you want to paint wetness", MessageType.Warning);
            }
#else
            ShowStreamsAd ();
#endif
         }
         else if (tab == Tab.Puddles)
         {
#if __MICROSPLAT_STREAMS__
            if (hasPuddles && VerifyStreams ())
            {
               DrawPuddlesGUI();
            }
            else
            {
               EditorGUILayout.HelpBox("Puddles is not enabled on your terrain, please enable in the shader options if you want to paint puddles", MessageType.Warning);
            }
#else
            ShowStreamsAd ();
#endif
         }
         else if (tab == Tab.Streams)
         {
#if __MICROSPLAT_STREAMS__
            if (hasStreams && VerifyStreams ())
            {
               DrawStreamGUI();
            }
            else
            {
               EditorGUILayout.HelpBox("Streams are not enabled on your terrain, please enable in the shader options if you want to paint streams", MessageType.Warning);
            }
#else
            ShowStreamsAd ();
#endif
         }
         else if (tab == Tab.Lava)
         {
#if __MICROSPLAT_STREAMS__
            if (hasLava && VerifyStreams ())
            {
               DrawLavaGUI();
            }
            else
            {
               EditorGUILayout.HelpBox("Lava is not enabled on your terrain, please enable in the shader options if you want to paint lava", MessageType.Warning);
            }
#else
            ShowStreamsAd ();
#endif
         }
         else if (tab == Tab.Scatter)
         {
#if __MICROSPLAT_SCATTER__
            if (hasScatter && VerifyScatter ())
            {
               DrawScatterGUI ();
            }
            else
            {
               EditorGUILayout.HelpBox ("Scatter is not enabled on your terrain, please enable in the shader options if you want to paint scatter", MessageType.Warning);
            }
#else
            ShowScatterAd ();
#endif
         }

      }

      
      void DrawSaveGUI()
      {
         EditorGUILayout.Space();
         EditorGUILayout.BeginHorizontal();
         if (GUILayout.Button("Save"))
         {
            SaveAll ();
         }

         EditorGUILayout.EndHorizontal();
         EditorGUILayout.Space();
      }

      void DrawSettingsGUI()
      {
         EditorGUILayout.Separator();
         GUI.skin.box.normal.textColor = Color.white;
         if (MicroSplatUtilities.DrawRollup("MicroSplat Terrain Painter"))
         {
            bool oldEnabled = enabled;
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape && Event.current.type == EventType.KeyUp)
            {
               enabled = !enabled;
            }
            enabled = GUILayout.Toggle(enabled, "Active (ESC)");
            if (enabled != oldEnabled)
            {
               InitTerrains();
            }

            brushVisualization = (BrushVisualization)EditorGUILayout.EnumPopup("Brush Visualization", brushVisualization);
            EditorGUILayout.Separator();
            GUILayout.Box("", new GUILayoutOption[]{GUILayout.ExpandWidth(true), GUILayout.Height(1)});
            EditorGUILayout.Separator();
         }
      }

      void DrawBrushSettingsGUI(bool showColor = false)
      {
         brushSize      = EditorGUILayout.Slider("Brush Size", brushSize, 0.01f, 30.0f);
         brushFlow      = EditorGUILayout.Slider("Brush Flow", brushFlow, 0.1f, 128.0f);
         brushFalloff   = EditorGUILayout.Slider("Brush Falloff", brushFalloff, 0.1f, 3.5f);
         EditorGUILayout.MinMaxSlider(CSlopeRange, ref slopeRange.x, ref slopeRange.y, 0.0f, 1.0f);
         if (showColor)
         {
            paintColor = EditorGUILayout.ColorField ("Color", paintColor);
         }
         else
         {
            paintValue = EditorGUILayout.Slider ("Target Value", paintValue, 0.0f, 1.0f);
         }
         EditorGUILayout.Separator();
         GUILayout.Box("", new GUILayoutOption[]{GUILayout.ExpandWidth(true), GUILayout.Height(1)});
         EditorGUILayout.Separator();

      }

      void GetTexAndChannel(TerrainPaintJob t, out Texture2D tex, out int channel)
      {
         channel = -1;
         tex = null;
         switch (tab)
         {
         case Tab.TintMap:
            {
               tex = t.tintTex;
               break;
            }
         case Tab.SnowMin:
            {
               tex = t.snowTex;
               channel = 1;
               break;
            }
         case Tab.SnowMax:
            {
               tex = t.snowTex;
               channel = 0;
               break;
            }
         case Tab.Wetness:
            {
               tex = t.streamTex;
               channel = 0;
               break;
            }
         case Tab.Puddles:
            {
               tex = t.streamTex;
               channel = 1;
               break;
            }
         case Tab.Streams:
            {
               tex = t.streamTex;
               channel = 2;
               break;
            }
         case Tab.Lava:
            {
               tex = t.streamTex;
               channel = 3;
               break;
            }
         case Tab.Scatter:
            {
               tex = t.scatterTex;
               channel = -2;
               break;
            }
         }
      }

      void FillTerrain(TerrainPaintJob t, float val)
      {
         InitTerrains();
         t.RegisterUndo();

         Texture2D tex = null;
         int channel = -1; // this means paint in RGB

         GetTexAndChannel (t, out tex, out channel);
         if (tex == null)
            return;

         int width = tex.width;
         int height = tex.height;
         for (int x = 0; x < width; ++x)
         {
            for (int y = 0; y < height; ++y)
            {
               var c = tex.GetPixel(x, y);

               Vector3 normal = t.terrain.terrainData.GetInterpolatedNormal((float)x / tex.width, (float)y / tex.height);
               float dt = Vector3.Dot(normal, Vector3.up);
               dt = 1 - Mathf.Clamp01(dt);
               bool filtered = dt < slopeRange.x || dt > slopeRange.y;
               if (tab == Tab.Scatter)
               {
#if __MICROSPLAT_SCATTER__
                  if (scatterLayer == ScatterLayer.First)
                  {
                     c.r = (float)(scatterIndex + 1) / 64.0f;
                     c.g = val;
                     if (c.g <= 0)
                     {
                        c.r = 0;
                     }
                  }
                  else
                  {
                     c.a = (float)(scatterIndex + 1) / 64.0f;
                     c.b = val;
                     if (c.b <= 0)
                     {
                        c.a = 0;
                     }
                  }
#endif
                  
                  tex.SetPixel (x, y, c);
               }
               else if (!filtered)
               {
                  if (channel == -1)
                  {
                     if (val < float.Epsilon)
                     {
                        c.r = Color.grey.r;
                        c.g = Color.grey.g;
                        c.b = Color.grey.b;
                     }
                     else
                     {
                        c.r = paintColor.r;
                        c.g = paintColor.g;
                        c.b = paintColor.b;
                     }
                     tex.SetPixel (x, y, c);
                  }
                  else
                  {
                     c [channel] = val;

                     tex.SetPixel (x, y, c);
                  }
               }
            }
         }
         tex.Apply();
      }

   }
#endif
}
