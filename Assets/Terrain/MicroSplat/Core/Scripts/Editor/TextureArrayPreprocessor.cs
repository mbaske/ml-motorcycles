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
   class TextureArrayPreProcessor : AssetPostprocessor
   {
      static int GetNewHash(TextureArrayConfig cfg)
      {
         unchecked
         {
            var settings = TextureArrayConfigEditor.GetSettingsGroup(cfg, UnityEditor.EditorUserBuildSettings.activeBuildTarget);
            int h = 17;

            h = h * TextureArrayConfigEditor.GetTextureFormat(cfg, settings.diffuseSettings.compression).GetHashCode() * 7;
            h = h * TextureArrayConfigEditor.GetTextureFormat(cfg, settings.normalSettings.compression).GetHashCode() * 13;
            h = h * TextureArrayConfigEditor.GetTextureFormat(cfg, settings.emissiveSettings.compression).GetHashCode() * 17;
            h = h * TextureArrayConfigEditor.GetTextureFormat(cfg, settings.antiTileSettings.compression).GetHashCode() * 31;
            h = h * TextureArrayConfigEditor.GetTextureFormat(cfg, settings.smoothSettings.compression).GetHashCode() * 37;
            h = h * Application.unityVersion.GetHashCode() * 43;
            return h;
         }
      }

      public static bool sIsPostProcessing = false;

      static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
      {
         var updates = new HashSet<TextureArrayConfig>();
         AddChangedConfigsToHashSet(updates, importedAssets);
         AddChangedConfigsToHashSet(updates, movedAssets);
         AddChangedConfigsToHashSet(updates, movedFromAssetPaths);

         foreach (var updatedConfig in updates)
         {
            CheckConfigForUpdates(updatedConfig);
         }
      }

      private static void AddChangedConfigsToHashSet(HashSet<TextureArrayConfig> hashSet, string[] paths)
      {
         for (int i = 0; i < paths.Length; i++)
         {
            var cfg = AssetDatabase.LoadAssetAtPath<TextureArrayConfig>(paths[i]);
            if (cfg != null)
            {
               hashSet.Add(cfg);
            }
         }
      }

      private static void CheckConfigForUpdates(TextureArrayConfig cfg)
      {
         int hash = GetNewHash(cfg);
         if (hash != cfg.hash)
         {
            cfg.hash = hash;
            EditorUtility.SetDirty(cfg);
            try 
            { 
               sIsPostProcessing = true;
               TextureArrayConfigEditor.CompileConfig(cfg);
            }
            finally
            {
               sIsPostProcessing = false;
               AssetDatabase.Refresh();
               AssetDatabase.SaveAssets();
               MicroSplatTerrain.SyncAll();
            }
         }
      }
   }
}
