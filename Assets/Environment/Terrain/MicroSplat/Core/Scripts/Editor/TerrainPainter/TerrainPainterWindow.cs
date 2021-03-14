//////////////////////////////////////////////////////
// MicroSplat
// Copyright (c) Jason Booth
//////////////////////////////////////////////////////


using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace JBooth.MicroSplat
{
#if __MICROSPLAT__ && (__MICROSPLAT_STREAMS__ || __MICROSPLAT_GLOBALTEXTURE__ || __MICROSPLAT_SNOW__ || __MICROSPLAT_SCATTER__)
   public partial class TerrainPainterWindow : EditorWindow 
   {
      [MenuItem("Window/MicroSplat/Terrain FX Painter")]
      public static void ShowWindow()
      {
         var window = GetWindow<JBooth.MicroSplat.TerrainPainterWindow>();
         window.InitTerrains();
         window.Show();
      }

      bool enabled = true;


      TerrainPaintJob[] terrains;
      bool[] jobEdits;

      TerrainPaintJob FindJob (Terrain t)
      {
         if (terrains == null || t == null)
            return null;

         for (int i = 0; i < terrains.Length; ++i)
         {
            if (terrains[i] != null && terrains[i].terrain == t)
               return terrains[i];
         }
         return null;
      }

      List<Terrain> rawTerrains = new List<Terrain>();

      void InitTerrains()
      {
         Object[] objs = Selection.GetFiltered(typeof(Terrain), SelectionMode.Editable | SelectionMode.OnlyUserModifiable | SelectionMode.Deep);
         List<TerrainPaintJob> ts = new List<TerrainPaintJob> ();
         rawTerrains.Clear();
         for (int i = 0; i < objs.Length; ++i)
         {
            Terrain t = objs[i] as Terrain;
            MicroSplatTerrain mst = t.GetComponent<MicroSplatTerrain>();
            if (mst == null)
               continue;
            rawTerrains.Add(t);
            if (t.materialTemplate != null)
            {
               bool hasStream = t.materialTemplate.HasProperty ("_StreamControl");
               bool hasSnow = t.materialTemplate.HasProperty ("_SnowMask");
               bool hasTint = t.materialTemplate.HasProperty ("_GlobalTintTex");
               bool hasScatter = t.materialTemplate.HasProperty ("_ScatterControl");

               if (!hasSnow && !hasStream && !hasTint && !hasScatter)
                  continue;
#if __MICROSPLAT_STREAMS__
               if (hasStream && mst.streamTexture == null)
               {
                  mst.streamTexture = CreateTexture(t, mst.streamTexture, "_stream_data", new Color(0,0,0,0));
               }
#endif
#if __MICROSPLAT_SNOW__
               if (hasSnow && mst.snowMaskOverride == null)
               {
                  mst.snowMaskOverride = CreateTexture (t, mst.snowMaskOverride, "_snowmask", new Color(1,0,0,1));
               }
#endif
#if __MICROSPLAT_GLOBALTEXTURE__
               if (hasTint && mst.tintMapOverride == null)
               {
                  mst.tintMapOverride = CreateTexture (t, mst.tintMapOverride, "_tint", Color.grey) ;
               }
#endif
#if __MICROSPLAT_SCATTER__
               if (hasScatter && mst.scatterMapOverride == null)
               {
                  mst.scatterMapOverride = CreateTexture (t, mst.scatterMapOverride, "_scatter", new Color (0, 0, 0, 1));
               }
#endif

               var tj = FindJob(t);
               if (tj != null)
               {
                  tj.collider = t.GetComponent<Collider>();
#if __MICROSPLAT_STREAMS__
                  tj.streamTex = mst.streamTexture;
#endif
#if __MICROSPLAT_GLOBALTEXTURE__
                  tj.tintTex = mst.tintMapOverride;
#endif
#if __MICROSPLAT_SNOW__
                  tj.snowTex = mst.snowMaskOverride;
#endif
#if __MICROSPLAT_SCATTER__
                  tj.scatterTex = mst.scatterMapOverride;
#endif

                  ts.Add(tj);
               }
               else
               {
                  tj = TerrainPaintJob.CreateInstance<TerrainPaintJob> ();
                  tj.terrain = t;
                  tj.collider = t.GetComponent<Collider>();
#if __MICROSPLAT_STREAMS__
                  tj.streamTex = mst.streamTexture;
#endif
#if __MICROSPLAT_GLOBALTEXTURE__
                  tj.tintTex = mst.tintMapOverride;
#endif
#if __MICROSPLAT_SNOW__
                  tj.snowTex = mst.snowMaskOverride;
#endif
                  ts.Add (tj);
               }
            }
         }
         if (terrains != null)
         {
            // clear out old terrains
            for (int i = 0; i < terrains.Length; ++i)
            {
               if (!ts.Contains(terrains[i]))
               {
                  DestroyImmediate(terrains[i]);
               }
            }
         }

         terrains = ts.ToArray();
         jobEdits = new bool[ts.Count];
      }

      void OnSelectionChange()
      {
         InitTerrains();
         this.Repaint();
      }

      void OnFocus() 
      {
#if UNITY_2019_1_OR_NEWER
         SceneView.duringSceneGui -= this.OnSceneGUI;
         SceneView.duringSceneGui += this.OnSceneGUI;
#else
         SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
         SceneView.onSceneGUIDelegate += this.OnSceneGUI;
#endif
         
         Undo.undoRedoPerformed -= this.OnUndo;
         Undo.undoRedoPerformed += this.OnUndo;

         UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= OnSceneSaving;
         UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += OnSceneSaving;


         this.titleContent = new GUIContent("MicroSplat Terrain FX Painter");
         InitTerrains();
         Repaint();
      }

      void SaveTexture (Texture2D tex)
      {
         if (tex != null)
         {
            string path = AssetDatabase.GetAssetPath (tex);
#if UNITY_2019_1_OR_NEWER
                  var bytes = tex.EncodeToTGA();
                  path = path.Replace (".png", ".tga");
#else
            var bytes = tex.EncodeToPNG ();
#endif

            System.IO.File.WriteAllBytes (path, bytes);
         }
      }


      void SaveAll ()
      {
         if (terrains == null)
            return;
         for (int i = 0; i < terrains.Length; ++i)
         {
            SaveTexture (terrains [i].streamTex);
            SaveTexture (terrains [i].tintTex);
            SaveTexture (terrains [i].snowTex);
            SaveTexture (terrains [i].scatterTex);
         }
         AssetDatabase.Refresh ();
      }

      void OnSceneSaving (UnityEngine.SceneManagement.Scene scene, string path)
      {
         SaveAll ();
      }

      void OnUndo()
      {
         if (terrains == null)
            return;
         for (int i = 0; i < terrains.Length; ++i)
         {
            if (terrains[i] != null)
            {
               terrains[i].RestoreUndo();
            }
         }
         Repaint();
      }

      void OnInspectorUpdate()
      {
         // unfortunate...
         Repaint ();
      }

      void OnDestroy() 
      {
#if UNITY_2019_1_OR_NEWER
         SceneView.duringSceneGui -= this.OnSceneGUI;
#else
         SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
#endif
         terrains = null;
      }


   }
#endif
               }

