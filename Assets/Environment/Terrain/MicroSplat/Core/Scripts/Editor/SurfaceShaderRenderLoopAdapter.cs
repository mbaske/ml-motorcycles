﻿//////////////////////////////////////////////////////
// MicroSplat
// Copyright (c) Jason Booth
//////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Text;
using JBooth.MicroSplat;
using System.Collections.Generic;
using System.Linq;

namespace JBooth.MicroSplat
{
   public class SurfaceShaderRenderLoopAdapter : IRenderLoopAdapter 
   {
      const string declareTerrain        = "      #pragma surface surf Standard vertex:vert fullforwardshadows addshadow";
      const string declareTerrainDebug   = "      #pragma surface surf Unlit vertex:vert nofog";
      const string declareTerrainTess    = "      #pragma surface surf Standard vertex:disp tessellate:TessDistance fullforwardshadows addshadow";
      const string declareBlend          = "      #pragma surface blendSurf TerrainBlendable fullforwardshadows addshadow decal:blend";
      const string declareMeshBlend      = "      #pragma surface surf Standard fullforwardshadows addshadow decal:blend";

      static TextAsset vertexFunc;
      static TextAsset fragmentFunc;
      static TextAsset terrainBlendBody;
      static TextAsset terrainBlendCBuffer;
      static TextAsset terrainBody;
      static TextAsset sharedInc;
      static TextAsset meshBlendBody;

      public string GetDisplayName() 
      { 
         return "Surface Shader"; 
      }

      public string GetRenderLoopKeyword() 
      {
         return "_MSRENDERLOOP_SURFACESHADER";
      }

      public bool UseReplaceMethods()
      {
         return false;
      }

      public void WriteProperties(string[] features, StringBuilder sb, bool blend)
      {

      }

      public void WritePerMaterialCBuffer(string[] features, StringBuilder sb, bool blend)
      {
         if (blend && !(features.Contains("_MICRODIGGERMESH") || features.Contains("_OUTPUTDIGGER")))
         {
            sb.AppendLine (terrainBlendCBuffer.text);
         }
      }

      public int GetNumPasses() { return 1; }

      public void WriteShaderHeader(string[] features, StringBuilder sb, MicroSplatShaderGUI.MicroSplatCompiler compiler, bool blend)
      {

         sb.AppendLine();
         sb.AppendLine("   CGINCLUDE");
         
         if (!features.Contains<string>("_DEBUG_USE_TOPOLOGY") && ( features.Contains<string>("_BDRF1") || features.Contains<string>("_BDRF2") || features.Contains<string>("_BDRF3")))
         {
            if (features.Contains<string>("_BDRF1"))
            {
               sb.AppendLine("      #define UNITY_BRDF_PBS BRDF1_Unity_PBS");
            }
            else if (features.Contains<string>("_BDRF2"))
            {
               sb.AppendLine("      #define UNITY_BRDF_PBS BRDF2_Unity_PBS");
            }
            else if (features.Contains<string>("_BDRF3"))
            {
               sb.AppendLine("      #define UNITY_BRDF_PBS BRDF3_Unity_PBS");
            }
         }
         sb.AppendLine("   ENDCG");
         sb.AppendLine();

         sb.AppendLine("   SubShader {");

         sb.Append("      Tags{ \"RenderType\" = \"Opaque\"  \"Queue\" = \"Geometry+100\" "); 
         // on 2018+, this removes the multipass..
         if (features.Contains("_MAX4TEXTURES"))
         {
            sb.Append("\"SplatCount\" = \"4\"");
         }
         else if (features.Contains("_MAX8TEXTURES"))
         {
            sb.Append("\"SplatCount\" = \"8\"");
         }
         else if (features.Contains("_MAX12TEXTURES"))
         {
            sb.Append("\"SplatCount\" = \"12\"");
         }
         else if (features.Contains("_MAX20TEXTURES"))
         {
            sb.Append("\"SplatCount\" = \"20\"");
         }
         else if (features.Contains("_MAX24TEXTURES"))
         {
            sb.Append("\"SplatCount\" = \"24\"");
         }
         else if (features.Contains("_MAX28TEXTURES"))
         {
            sb.Append("\"SplatCount\" = \"28\"");
         }
         else if (features.Contains("_MAX32TEXTURES"))
         {
            sb.Append("\"SplatCount\" = \"32\"");
         }
         else
         {
            sb.Append ("\"SplatCount\" = \"16\"");
         }
         sb.AppendLine("}");
         sb.AppendLine("      Cull Back");
         sb.AppendLine("      ZTest LEqual");
         if (blend && !(features.Contains ("_MICRODIGGERMESH") || features.Contains ("_OUTPUTDIGGER")))
         {
            sb.AppendLine("      BLEND ONE ONE");
         }
#if UNITY_2018_3_OR_NEWER
         sb.AppendLine("      UsePass \"Hidden/Nature/Terrain/Utilities/PICKING\"");
         sb.AppendLine("      UsePass \"Hidden/Nature/Terrain/Utilities/SELECTION\"");
#endif
         sb.AppendLine("      CGPROGRAM");
      }

      public void WritePassHeader(string[] features, StringBuilder sb, MicroSplatShaderGUI.MicroSplatCompiler compiler, int pass, bool blend)
      {

         sb.AppendLine("      #pragma exclude_renderers d3d9");

         sb.AppendLine("      #include \"UnityCG.cginc\"");
         sb.AppendLine("      #include \"AutoLight.cginc\"");
         sb.AppendLine("      #include \"Lighting.cginc\"");
         if (features.Contains<string>("_BDRFNOSPECMIN"))
         {
            sb.AppendLine("      #include \"Assets/MicroSplat/Core/Scripts/MicroSplatPBSLighting.cginc\"");
         }
         else
         {
            sb.AppendLine("      #include \"UnityPBSLighting.cginc\"");
         }


         sb.AppendLine("      #include \"UnityStandardBRDF.cginc\"");
         sb.AppendLine();

         string pragma = "";
         if ((blend || features.Contains("_MESHOVERLAYSPLATS")) && !(features.Contains ("_MICRODIGGERMESH") || features.Contains ("_OUTPUTDIGGER")))
         {
            if (features.Contains("_MESHOVERLAYSPLATS"))
            {
               pragma = declareMeshBlend;
            }
            else
            {
               pragma = declareBlend;
            }
         }
         else if (!features.Contains<string>("_TESSDISTANCE"))
         {
            if (MicroSplatShaderGUI.MicroSplatCompiler.HasDebugFeature(features))
            {
               pragma = declareTerrainDebug;
               if (features.Contains("_ALPHAHOLE") || features.Contains("_ALPHABELOWHEIGHT"))
               {
                  // generate a shadow pass so we clip that too..
                  pragma += (" addshadow");
               }
            }
            else
            {
               pragma = declareTerrain;
            }
         }
         else
         {
            pragma = declareTerrainTess;
         }

         if (!MicroSplatShaderGUI.MicroSplatCompiler.HasDebugFeature (features) && features.Contains ("_BDRFLAMBERT"))
         {
            pragma = pragma.Replace ("Standard", "Lambert");
         }
         else if (features.Contains ("_USESPECULARWORKFLOW") || features.Contains("_SPECULARFROMMETALLIC"))
         {
            pragma = pragma.Replace("Standard", "StandardSpecular");
         }

         sb.Append(pragma);

         if (!blend || features.Contains ("_MICRODIGGERMESH") || features.Contains("_OUTPUTDIGGER"))
         {
            if (features.Contains<string>("_BDRF1") || features.Contains<string>("_BDRF2") || features.Contains<string>("_BDRF3"))
            {
               sb.Append(" exclude_path:deferred");
            }
         }

         // allow for custom functions in lighting when this keyword is enabled
         if (!blend && features.Contains("_MSCUSTOMLIGHTING"))
         {
            sb.Replace("surf Standard", "customLightingSurf CustomLighting");
         }


         // don't remove
         sb.AppendLine();
         sb.AppendLine();
         sb.AppendLine();
         // for 2018..
#if UNITY_2018_3_OR_NEWER
         sb.AppendLine("     #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap forwardadd");
#endif

         sb.AppendLine("      #pragma target " + compiler.GetShaderModel(features));
         // 2019.3 holes
         sb.AppendLine("      #pragma multi_compile_local __ _ALPHATEST_ON");

      }


      public void WriteVertexFunction(string[] features, StringBuilder sb, MicroSplatShaderGUI.MicroSplatCompiler compiler, int pass, bool blend)
      {
         sb.AppendLine(vertexFunc.text);
      }

      public void WriteFragmentFunction(string[] features, StringBuilder sb, MicroSplatShaderGUI.MicroSplatCompiler compiler, int pass, bool blend)
      {
         sb.AppendLine(fragmentFunc.text);
         if (blend)
         {
            if (features.Contains("_MESHOVERLAYSPLATS") && meshBlendBody != null)
            {
               sb.AppendLine(meshBlendBody.text);
            }
            else if (features.Contains("_TERRAINBLENDING") && terrainBlendBody != null)
            {
               sb.AppendLine(terrainBlendBody.text);
            }
         }

         sb.AppendLine("ENDCG\n\n   }");
      }


      public void WriteShaderFooter(string[] features, StringBuilder sb, MicroSplatShaderGUI.MicroSplatCompiler compiler, bool blend, string baseName)
      {
         if (blend && !features.Contains("_MICROMESH"))
         {
            sb.AppendLine("   CustomEditor \"MicroSplatBlendableMaterialEditor\"");
         }
         else if (blend)
         {
            sb.AppendLine ("   CustomEditor \"MicroSplatShaderGUI\"");
         }
         else if (baseName != null)
         {
            if (!features.Contains ("_MICROMESH"))
            {
               sb.AppendLine ("   Dependency \"AddPassShader\" = \"Hidden/MicroSplat/AddPass\"");
               // disable until unity fixes the spew bug..
               // for 2018 basemap gen
               //string depName = baseName;
               //if (depName.Contains ("Hidden/MicroSplat"))
               //{
               //   depName = depName.Substring (depName.IndexOf ("Hidden/MicroSplat") + 17);
               //}

               //sb.AppendLine ("   Dependency \"BaseMapGenShader\" = \"" + depName + "\"");
               sb.AppendLine ("   Dependency \"BaseMapShader\" = \"" + baseName + "\"");
            }
            sb.AppendLine ("   CustomEditor \"MicroSplatShaderGUI\"");
         }
         sb.AppendLine("   Fallback \"Nature/Terrain/Diffuse\"");
         sb.Append("}");
      }

      public void Init(string[] paths)
      {
         for (int i = 0; i < paths.Length; ++i)
         {
            string p = paths[i];
            if (p.EndsWith("microsplat_terrain_surface_vertex.txt"))
            {
               vertexFunc = AssetDatabase.LoadAssetAtPath<TextAsset>(p);
            }
            if (p.EndsWith("microsplat_terrain_surface_fragment.txt"))
            {
               fragmentFunc = AssetDatabase.LoadAssetAtPath<TextAsset>(p);
            }
            if (p.EndsWith("microsplat_terrainblend_body.txt"))
            {
               terrainBlendBody = AssetDatabase.LoadAssetAtPath<TextAsset>(p);
            }
            if (p.EndsWith ("microsplat_terrainblend_cbuffer.txt"))
            {
               terrainBlendCBuffer = AssetDatabase.LoadAssetAtPath<TextAsset> (p);
            }
            if (p.EndsWith("microsplat_meshoverlay_body.txt"))
            {
               meshBlendBody = AssetDatabase.LoadAssetAtPath<TextAsset>(p);
            }
            if (p.EndsWith("microsplat_terrain_body.txt"))
            {
               terrainBody = AssetDatabase.LoadAssetAtPath<TextAsset>(p);
            }
            if (p.EndsWith("microsplat_shared.txt"))
            {
               sharedInc = AssetDatabase.LoadAssetAtPath<TextAsset>(p);
            }
         }
      }

      public void PostProcessShader(string[] features, StringBuilder sb, MicroSplatShaderGUI.MicroSplatCompiler compiler, bool blend)
      {
      }

      public void WriteSharedCode(string[] features, StringBuilder sb, MicroSplatShaderGUI.MicroSplatCompiler compiler, int pass, bool blend)
      {
         sb.AppendLine(sharedInc.text);

      }

      public void WriteTerrainBody(string[] features, StringBuilder sb, MicroSplatShaderGUI.MicroSplatCompiler compiler, int pass, bool blend)
      {
         sb.AppendLine(terrainBody.text);
      }

      public MicroSplatShaderGUI.PassType GetPassType(int i) { return MicroSplatShaderGUI.PassType.Surface; }

      public string GetVersion()
      {
         return MicroSplatShaderGUI.MicroSplatVersion;
      }
   }
}
