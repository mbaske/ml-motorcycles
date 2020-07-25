//////////////////////////////////////////////////////
// MicroSplat
// Copyright (c) Jason Booth
//////////////////////////////////////////////////////

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using JBooth.MicroSplat;


public partial class MicroSplatTerrainEditor : Editor
{
   class MissingModule
	{
		public string name = null;
		public string link = null;
	}

   class IntegrationConfig
   {
      public string name;
      public bool assetInstalled = false;
		public string assetLink;
      public List<MissingModule> missingModules = new List<MissingModule> ();
      public bool include = false;
      public string [] keywords;
   }


   List<IntegrationConfig> integrationConfigs = null;
   Texture2D convertSelectionImg;

   void RequireTriplanar(List<MissingModule> m)
   {
#if !__MICROSPLAT_TRIPLANAR__
				m.Add(new MissingModule() { name = "Triplanar", link = MicroSplatDefines.link_triplanar });
#endif
   }

   void RequireAntiTile (List<MissingModule> m)
   {
#if !__MICROSPLAT_DETAILRESAMPLE__
				m.Add(new MissingModule() { name = "Anti-Tiling", link = MicroSplatDefines.link_antitile });
#endif
   }

   void RequireTessellation(List<MissingModule> m)
   {
#if !__MICROSPLAT_TESSELLATION__
				m.Add(new MissingModule() { name = "Tessellation", link = MicroSplatDefines.link_tessellation });
#endif
   }

   void RequireTextureClusters(List<MissingModule> m)
   {
#if !__MICROSPLAT_TEXTURECLUSTERS__
				m.Add(new MissingModule() { name = "Texture Clusters", link = MicroSplatDefines.link_textureclusters });
#endif
   }

   void RequireStreams(List<MissingModule> m)
   {
#if !__MICROSPLAT_STREAMS__
				m.Add (new MissingModule () { name = "Wetness/Streams", link = MicroSplatDefines.link_streams });
#endif
   }

   void RequireSnow(List<MissingModule> m)
   {
#if !__MICROSPLAT_SNOW__
				m.Add (new MissingModule () { name = "Snow", link = MicroSplatDefines.link_snow });
#endif
   }

   void RequireTerrainHoles(List<MissingModule> m)
   {
#if !__MICROSPLAT_ALPHAHOLE__
      m.Add (new MissingModule () { name = "Terrain Holes", link = MicroSplatDefines.link_alphahole });
#endif
   }

   string [] defaultKeywords = null;

   void InitConvertConfigs()
   {
      if (defaultKeywords == null)
      {
         integrationConfigs = new List<IntegrationConfig> ();

         defaultKeywords = new string [] { "_MICROSPLAT", "_BRANCHSAMPLES", "_BRANCHSAMPLESAGR", "_USEGRADMIP" };

         // INTERGRATIONS
			{
            var c = new IntegrationConfig ();
            c.name = "Enviro";
            c.assetLink = "https://assetstore.unity.com/packages/tools/particles-effects/enviro-sky-and-weather-33963?aid=25047";

            c.keywords = new string [] {
#if __MICROSPLAT_SNOW__
               "_SNOW",
               "_USEGLOBALSNOWLEVEL",
#endif
#if __MICROSPLAT_STREAMS__
               "_WETNESS",
               "_GLOBALWETNESS",
#endif
               };




#if ENVIRO_HD || ENVIRO_LW || ENVIRO_PRO
            c.assetInstalled = true;
#endif
            RequireStreams (c.missingModules);
            RequireSnow (c.missingModules);
            integrationConfigs.Add(c);
         }

         {
            var c = new IntegrationConfig ();
            c.name = "Weather Maker";
            c.keywords = new string [] {
#if __MICROSPLAT_SNOW__
               "_SNOW",
               "_USEGLOBALSNOWLEVEL",
#endif
#if __MICROSPLAT_STREAMS__
               "_WETNESS",
               "_GLOBALWETNESS",
#endif

            };
            c.assetLink = "https://assetstore.unity.com/packages/tools/particles-effects/weather-maker-unity-weather-system-sky-water-volumetric-clouds-a-60955?aid=25047";
            c.keywords = new string [] { };
            RequireStreams (c.missingModules);
            RequireSnow (c.missingModules);

#if WEATHER_MAKER_PRESENT
            c.assetInstalled = true;
#endif
            integrationConfigs.Add(c);
			}

         

      }
      
   }

   void DrawMissingModule(MissingModule m)
   {
      EditorGUILayout.BeginHorizontal ();
      EditorGUILayout.PrefixLabel (m.name);
      if (GUILayout.Button ("Link"))
      {
         Application.OpenURL (m.link);
      }
      EditorGUILayout.EndHorizontal ();
   }


   bool DoConvertGUI(MicroSplatTerrain t)
   {
      if (t.templateMaterial == null)
      {
         InitConvertConfigs ();
         using (new GUILayout.VerticalScope (GUI.skin.box))
         {
            EditorGUILayout.LabelField ("Select any integrations you want to add:");
            // integrations
            for (int i = 0; i < integrationConfigs.Count; ++i)
            {
               
               var ic = integrationConfigs [i];
               if (!ic.assetInstalled)
               {
                  EditorGUILayout.BeginHorizontal ();
                  EditorGUILayout.LabelField (ic.name, GUILayout.Width (120));
                  EditorGUILayout.LabelField ("Not Installed", GUILayout.Width (120));
                  if (GUILayout.Button ("Link", GUILayout.Width (120)))
                  {
                     Application.OpenURL (ic.assetLink);
                  }
                  EditorGUILayout.EndHorizontal ();
               }
               else
               {
                  EditorGUILayout.BeginHorizontal ();
                  ic.include = EditorGUILayout.Toggle (ic.include, GUILayout.Width (20));
                  EditorGUILayout.LabelField (ic.name);
                  EditorGUILayout.EndHorizontal ();
                  if (ic.include && ic.missingModules.Count > 0)
                  {
                     using (new GUILayout.VerticalScope (GUI.skin.box))
                     {
                        EditorGUILayout.HelpBox ("Some MicroSplat modules requested by this module are not installed. Some or all features of the integration will not be active.", MessageType.Warning);
                        for (int j = 0; j < ic.missingModules.Count; ++j)
                        {
                           var m = ic.missingModules [j];
                           DrawMissingModule (m);
                        }
                     }
                  }
               }
            }

            if (GUILayout.Button ("Convert to MicroSplat"))
            {
               // get all terrains in selection, not just this one, and treat as one giant terrain
               var objs = Selection.gameObjects;
               List<Terrain> terrains = new List<Terrain> ();
               for (int i = 0; i < objs.Length; ++i)
               {
                  Terrain ter = objs [i].GetComponent<Terrain> ();
                  if (ter != null)
                  {
                     terrains.Add (ter);
                  }
                  Terrain [] trs = objs [i].GetComponentsInChildren<Terrain> ();
                  for (int x = 0; x < trs.Length; ++x)
                  {
                     if (!terrains.Contains (trs [x]))
                     {
                        terrains.Add (trs [x]);
                     }
                  }
               }

               Terrain terrain = t.GetComponent<Terrain> ();
               int texcount = terrain.terrainData.terrainLayers.Length;
               List<string> keywords = new List<string> (defaultKeywords);
               if (texcount <= 4)
               {
                  keywords.Add ("_MAX4TEXTURES");
               }
               else if (texcount <= 8)
               {
                  keywords.Add ("_MAX8TEXTURES");
               }
               else if (texcount <= 12)
               {
                  keywords.Add ("_MAX12TEXTURES");
               }
               else if (texcount <= 20)
               {
                  keywords.Add ("_MAX20TEXTURES");
               }
               else if (texcount <= 24)
               {
                  keywords.Add ("_MAX24TEXTURES");
               }
               else if (texcount <= 28)
               {
                  keywords.Add ("_MAX28TEXTURES");
               }
               else if (texcount > 28)
               {
                  keywords.Add ("_MAX32TEXTURES");
               }

               for (int i = 0; i < integrationConfigs.Count; ++i)
               {
                  var ic = integrationConfigs [i];
                  if (ic.include)
                  {
                     keywords.AddRange (ic.keywords);
                  }
               }

               // setup this terrain
               t.templateMaterial = MicroSplatShaderGUI.NewShaderAndMaterial (terrain, keywords.ToArray ());

               var config = TextureArrayConfigEditor.CreateConfig (terrain);
               t.templateMaterial.SetTexture ("_Diffuse", config.diffuseArray);
               t.templateMaterial.SetTexture ("_NormalSAO", config.normalSAOArray);

               t.propData = MicroSplatShaderGUI.FindOrCreatePropTex (t.templateMaterial);

               if (terrain.terrainData.terrainLayers != null)
               {
                  if (terrain.terrainData.terrainLayers.Length > 0)
                  {
                     Vector2 min = new Vector2 (99999, 99999);
                     Vector2 max = Vector2.zero;
                     

                     for (int x = 0; x < terrain.terrainData.terrainLayers.Length; ++x)
                     {
                        var uv = terrain.terrainData.terrainLayers [x].tileSize;
                        if (min.x > uv.x)
                           min.x = uv.x;
                        if (min.y > uv.y)
                           min.y = uv.y;
                        if (max.x < uv.x)
                           max.x = uv.x;
                        if (max.y < uv.y)
                           max.y = uv.y;
                     }
                     Vector2 average = Vector2.Lerp (min, max, 0.5f);
                     // use per texture UVs instead..
                     float diff = Vector2.Distance (min, max);
                     if (diff > 0.1)
                     {
                        keywords.Add ("_PERTEXUVSCALEOFFSET");

                        // if the user has widely different UVs, use the LOD sampler. This is because the gradient mode blends between mip levels,
                        // which looks bad with hugely different UVs. I still don't understand why people do this kind of crap though, ideally
                        // your UVs should not differ per texture, and if so, not by much..
                        if (diff > 10)
                        {
                           Debug.LogWarning ("Terrain has wildly varing UV scales, it's best to keep consistent texture resolution. ");
                        }
                        if (!keywords.Contains("_USEGRADMIP"))
                        {
                           keywords.Add ("_USEGRADMIP");
                        }
                        Vector4 scaleOffset = new Vector4 (1, 1, 0, 0);
                        t.templateMaterial.SetVector ("_UVScale", scaleOffset);
                        var propData = MicroSplatShaderGUI.FindOrCreatePropTex (t.templateMaterial);
                        
                        for (int x = 0; x < terrain.terrainData.terrainLayers.Length; ++x)
                        {
                           var uvScale = terrain.terrainData.terrainLayers [x].tileSize;
                           var uvOffset = terrain.terrainData.terrainLayers [x].tileOffset;
                           uvScale = MicroSplatRuntimeUtil.UnityUVScaleToUVScale (uvScale, terrain);
                           uvScale.x = Mathf.RoundToInt (uvScale.x);
                           uvScale.y = Mathf.RoundToInt (uvScale.y);
                           propData.SetValue (x, MicroSplatPropData.PerTexVector2.SplatUVScale, uvScale);
                           propData.SetValue (x, MicroSplatPropData.PerTexVector2.SplatUVOffset, Vector2.zero);
                        }
                        for (int x = terrain.terrainData.terrainLayers.Length; x < 32; ++x)
                        {
                           propData.SetValue (x, MicroSplatPropData.PerTexVector2.SplatUVScale, average);
                           propData.SetValue (x, MicroSplatPropData.PerTexVector2.SplatUVOffset, Vector2.zero);
                        }
                        // must init the data, or the editor will write over it.
                        propData.SetValue (0, 15, Color.white);

                     }
                     else
                     {
                        var uvScale = terrain.terrainData.terrainLayers [0].tileSize;
                        var uvOffset = terrain.terrainData.terrainLayers [0].tileOffset;

                        uvScale = MicroSplatRuntimeUtil.UnityUVScaleToUVScale (uvScale, terrain);
                        uvOffset.x = uvScale.x / terrain.terrainData.size.x * 0.5f * uvOffset.x;
                        uvOffset.y = uvScale.y / terrain.terrainData.size.x * 0.5f * uvOffset.y;
                        Vector4 scaleOffset = new Vector4 (uvScale.x, uvScale.y, uvOffset.x, uvOffset.y);
                        t.templateMaterial.SetVector ("_UVScale", scaleOffset);
                     }
                  }
               }


               // now make sure others all have the same settings as well.
               for (int i = 0; i < terrains.Count; ++i)
               {
                  var nt = terrains [i];
                  var mgr = nt.GetComponent<MicroSplatTerrain> ();
                  if (mgr == null)
                  {
                     mgr = nt.gameObject.AddComponent<MicroSplatTerrain> ();
                  }
                  mgr.templateMaterial = t.templateMaterial;

                  if (mgr.propData == null)
                  {
                     mgr.propData = MicroSplatShaderGUI.FindOrCreatePropTex (mgr.templateMaterial);
                  }
               }
               

               Selection.SetActiveObjectWithContext (config, config);
               t.keywordSO = MicroSplatUtilities.FindOrCreateKeywords (t.templateMaterial);

               t.keywordSO.keywords.Clear ();
               t.keywordSO.keywords = new List<string> (keywords);

               // force recompile, so that basemap shader name gets reset correctly..
               MicroSplatShaderGUI.MicroSplatCompiler comp = new MicroSplatShaderGUI.MicroSplatCompiler ();
               comp.Compile (t.templateMaterial);

               MicroSplatTerrain.SyncAll ();
               /*
               // turn on draw instanced if enabled and tessellation is disabled, unless render loop is LWRP/URP in which case it does work..
               if (t.keywordSO != null && (!t.keywordSO.IsKeywordEnabled("_TESSDISTANCE") || t.keywordSO.IsKeywordEnabled("_MSRENDERLOOP_UNITYLD")))
               {
                  for (int i = 0; i < terrains.Count; ++i)
                  {
                     var nt = terrains [i];
                     var mgr = nt.GetComponent<MicroSplatTerrain> ();
                     if (mgr != null && mgr.keywordSO != null && !mgr.keywordSO.IsKeywordEnabled("_MSRENDERLOOP_UNITYLD"))
                     {
                        nt.drawInstanced = true;
                     }
                  }
               }
*/
               return true;
            }
         }
      }
      return false;
   }
}
