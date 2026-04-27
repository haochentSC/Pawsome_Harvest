/*
 * Copyright (c) 2024-2025 SIOCODE LLC.
 * 
 * All rights reserved. This script and its source code are the property of 
 * SIOCODE LLC., Hungary, 8600 Si�fok, Barackfa utca 18.
 * 
 * Redistribution and modification of this source code, in whole or in part, 
 * with or without alterations, are strictly prohibited.
 * 
 * This script is licensed for use as a compiled asset only. You may use it in 
 * your projects, including commercial ones, as long as you obtained it through 
 * an official distribution channel and comply with its licensing terms.
 * 
 * Unauthorized sharing, selling, or distributing of this script or its source 
 * code is prohibited. For inquiries regarding licensing, please contact 
 * SIOCODE LLC.
 */

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

namespace SIOCODE
{
    namespace FernAssetCreator_Free
    {

        public enum FernMeshGenerator_Free_FernPlantType
        {
            Cross,
            Cross3,
            Cross4,
            Box,
            Single,
            Tent,
            WideTent,
            V,
            WideV
        }

        static class FernMeshGenerator_Free
        {
            public static Mesh GenerateFernMesh(FernMeshGenerator_Free_FernPlantType type)
            {
                List<Vector3> vertices = new List<Vector3>();
                List<Vector2> uvs = new List<Vector2>();
                List<int> indices = new List<int>();

                Vector3[] baseVerts = new Vector3[]
                {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
            new Vector3( 0.5f, 0.5f, 0f),
            new Vector3( 0.5f, -0.5f, 0f),
                };
                Vector2[] baseUVs = new Vector2[]
                {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f),
                };

                void AddPlane(Matrix4x4 transform)
                {
                    int startIndex = vertices.Count;
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 pos = transform.MultiplyPoint3x4(baseVerts[i]);
                        vertices.Add(pos);
                        uvs.Add(baseUVs[i]);
                    }
                    indices.Add(startIndex + 0);
                    indices.Add(startIndex + 1);
                    indices.Add(startIndex + 2);
                    indices.Add(startIndex + 2);
                    indices.Add(startIndex + 3);
                    indices.Add(startIndex + 0);
                }

                switch (type)
                {
                    case FernMeshGenerator_Free_FernPlantType.Cross:
                        AddPlane(Matrix4x4.identity);
                        AddPlane(Matrix4x4.Rotate(Quaternion.Euler(0, 90, 0)));
                        AddPlane(Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0)));
                        AddPlane(Matrix4x4.Rotate(Quaternion.Euler(0, 270, 0)));
                        break;

                    case FernMeshGenerator_Free_FernPlantType.Cross3:
                        AddPlane(Matrix4x4.identity);
                        AddPlane(Matrix4x4.Rotate(Quaternion.Euler(0, 60, 0)));
                        AddPlane(Matrix4x4.Rotate(Quaternion.Euler(0, 120, 0)));
                        AddPlane(Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0)));
                        AddPlane(Matrix4x4.Rotate(Quaternion.Euler(0, 240, 0)));
                        AddPlane(Matrix4x4.Rotate(Quaternion.Euler(0, 300, 0)));
                        break;

                    case FernMeshGenerator_Free_FernPlantType.Cross4:
                        AddPlane(Matrix4x4.Rotate(Quaternion.Euler(0, 0, 0)));
                        AddPlane(Matrix4x4.Rotate(Quaternion.Euler(0, 45, 0)));
                        AddPlane(Matrix4x4.Rotate(Quaternion.Euler(0, 90, 0)));
                        AddPlane(Matrix4x4.Rotate(Quaternion.Euler(0, 135, 0)));
                        AddPlane(Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0)));
                        AddPlane(Matrix4x4.Rotate(Quaternion.Euler(0, 225, 0)));
                        AddPlane(Matrix4x4.Rotate(Quaternion.Euler(0, 270, 0)));
                        AddPlane(Matrix4x4.Rotate(Quaternion.Euler(0, 315, 0)));
                        break;

                    case FernMeshGenerator_Free_FernPlantType.Box:
                        AddPlane(Matrix4x4.Translate(new Vector3(0, 0, 0.1f)));
                        AddPlane(Matrix4x4.Translate(new Vector3(0, 0, 0.1f)) * Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0)));
                        AddPlane(
                            Matrix4x4.Translate(new Vector3(0.1f, 0, 0)) *
                            Matrix4x4.Rotate(Quaternion.Euler(0, 90, 0))
                        );
                        AddPlane(
                            Matrix4x4.Translate(new Vector3(0.1f, 0, 0)) *
                            Matrix4x4.Rotate(Quaternion.Euler(0, 90, 0)) *
                            Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0))
                        );
                        AddPlane(
                            Matrix4x4.Translate(new Vector3(0, 0, -0.1f)) *
                            Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0))
                        );
                        AddPlane(
                            Matrix4x4.Translate(new Vector3(0, 0, -0.1f)) *
                            Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0)) *
                            Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0))
                        );
                        AddPlane(
                            Matrix4x4.Translate(new Vector3(-0.1f, 0, 0)) *
                            Matrix4x4.Rotate(Quaternion.Euler(0, -90, 0))
                        );
                        AddPlane(
                            Matrix4x4.Translate(new Vector3(-0.1f, 0, 0)) *
                            Matrix4x4.Rotate(Quaternion.Euler(0, -90, 0)) *
                            Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0))
                        );
                        break;

                    case FernMeshGenerator_Free_FernPlantType.Single:
                        AddPlane(Matrix4x4.identity);
                        AddPlane(Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0)));
                        break;

                    case FernMeshGenerator_Free_FernPlantType.Tent:
                        for (int i = 0; i < 4; i++)
                        {
                            float yRot = 90f * i;
                            float xRot = 20f;
                            Matrix4x4 transform = Matrix4x4.Rotate(Quaternion.Euler(-xRot, yRot, 0));
                            AddPlane(transform);
                            AddPlane(transform * Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0)));
                        }
                        break;

                    case FernMeshGenerator_Free_FernPlantType.WideTent:
                        for (int i = 0; i < 4; i++)
                        {
                            float yRot = 90f * i;
                            float xRot = 30f;
                            Matrix4x4 transform = Matrix4x4.Rotate(Quaternion.Euler(-xRot, yRot, 0));
                            AddPlane(transform);
                            AddPlane(transform * Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0)));
                        }
                        break;

                    case FernMeshGenerator_Free_FernPlantType.V:
                        // tilt with pivot shift
                        for (int i = 0; i < 4; i++)
                        {
                            float yRot = 90f * i;
                            float tiltAngle = 20f;

                            Matrix4x4 pivotDown = Matrix4x4.Translate(new Vector3(0, -0.5f, 0));
                            Matrix4x4 pivotRestore = Matrix4x4.Translate(new Vector3(0, 0.5f, 0));

                            Quaternion tilt = Quaternion.identity;
                            if (i % 2 == 0)
                            {
                                float sign = (i == 0) ? 1 : -1;
                                tilt = Quaternion.Euler(tiltAngle * sign, 0, 0);
                            }
                            else
                            {
                                float sign = (i == 1) ? -1 : 1;
                                tilt = Quaternion.Euler(0, 0, tiltAngle * sign);
                            }
                            Quaternion yRotQ = Quaternion.Euler(0, yRot, 0);

                            Matrix4x4 transform =
                                pivotRestore * Matrix4x4.Rotate(tilt) * Matrix4x4.Rotate(yRotQ) * pivotDown;
                            AddPlane(transform);
                            AddPlane(transform * Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0)));
                        }
                        break;

                    case FernMeshGenerator_Free_FernPlantType.WideV:
                        for (int i = 0; i < 4; i++)
                        {
                            float yRot = 90f * i;
                            float tiltAngle = 30f;

                            Matrix4x4 pivotDown = Matrix4x4.Translate(new Vector3(0, -0.5f, 0));
                            Matrix4x4 pivotRestore = Matrix4x4.Translate(new Vector3(0, 0.5f, 0));

                            Quaternion tilt = Quaternion.identity;
                            if (i % 2 == 0)
                            {
                                float sign = (i == 0) ? 1 : -1;
                                tilt = Quaternion.Euler(tiltAngle * sign, 0, 0);
                            }
                            else
                            {
                                float sign = (i == 1) ? -1 : 1;
                                tilt = Quaternion.Euler(0, 0, tiltAngle * sign);
                            }
                            Quaternion yRotQ = Quaternion.Euler(0, yRot, 0);

                            Matrix4x4 transform =
                                pivotRestore * Matrix4x4.Rotate(tilt) * Matrix4x4.Rotate(yRotQ) * pivotDown;
                            AddPlane(transform);
                            AddPlane(transform * Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0)));
                        }
                        break;
                }

                float minY = vertices.Min(v => v.y);
                for (int i = 0; i < vertices.Count; i++)
                {
                    Vector3 v = vertices[i];
                    v.y -= minY;
                    vertices[i] = v;
                }

                Mesh mesh = new Mesh();
                mesh.name = $"FernMesh_{type}";
                mesh.SetVertices(vertices);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(indices, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();

                return mesh;
            }

            public static Material CreateFernMaterial(
                Material baseMaterial,
                Texture2D fernTexture
            )
            {

                Material mat = new Material(
                    baseMaterial
                );
                mat.name = "FernMaterial";

                mat.mainTexture = fernTexture;

                if (mat.HasProperty("_Cull"))
                    mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Back);
                if (mat.HasProperty("_ReceiveShadows"))
                    mat.SetFloat("_ReceiveShadows", 0f);


                Debug.Log(baseMaterial.shader.name);

                if (baseMaterial.shader.name.StartsWith("HDRP/Lit"))
                {
                    if (mat.HasProperty("_AlphaCutoffEnable"))
                        mat.SetFloat("_AlphaCutoffEnable", 1f);
                    if (mat.HasProperty("_AlphaCutoff"))
                        mat.SetFloat("_AlphaCutoff", 0.5f);

                }
                else if (baseMaterial.shader.name.StartsWith("Universal Render Pipeline/Lit"))
                {

                    if (mat.HasProperty("_Metallic"))
                        mat.SetFloat("_Metallic", 0f);
                    if (mat.HasProperty("_Smoothness"))
                        mat.SetFloat("_Smoothness", 0f);
                    if (mat.HasProperty("_WorkflowMode"))
                        mat.SetFloat("_WorkflowMode", 1f);
                    if (mat.HasProperty("_Glossiness"))
                        mat.SetFloat("_Glossiness", 0f);
                    if (mat.HasProperty("_Cutoff"))
                        mat.SetFloat("_Cutoff", 0.5f);
                    if (mat.HasProperty("_AlphaClip"))
                        mat.SetFloat("_AlphaClip", 1f);

                }
                else
                {

                    if (mat.HasProperty("_Surface"))
                        mat.SetFloat("_Surface", 0f);
                    if (mat.HasProperty("_Mode"))
                        mat.SetFloat("_Mode", 1f);
                    if (mat.HasProperty("_AlphaClip"))
                        mat.SetFloat("_AlphaClip", 1f);

                }

                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                mat.EnableKeyword("_ALPHATEST_ON");

                return mat;
            }
        }

        public class FernAssetCreator_Free : EditorWindow
        {
            private Texture2D fernTexture;
            private FernMeshGenerator_Free_FernPlantType fernType = FernMeshGenerator_Free_FernPlantType.Cross;

            private PreviewRenderUtility previewUtility;
            private Mesh previewMesh;
            private Material previewMaterial;
            private Material baseMaterial;
            private string baseMaterialInfo = "No information about the base material.";
            private float previewRotation;
            private bool needsPreviewUpdate;
            private float prevAppTime = 0.0f;

            [MenuItem("Tools/Fern Asset Creator (Free)")]
            public static void ShowWindow()
            {
                GetWindow(typeof(FernAssetCreator_Free), false, "Fern Asset Creator (Free)");
            }

            private void UpdatePreview()
            {
                if (previewUtility != null)
                {
                    float now = Time.time;
                    float deltaTime = now - prevAppTime;
                    prevAppTime = now;
                    previewRotation += 30.0f * deltaTime;
                    Repaint();
                }
            }

            private static string DetectRenderPipeline()
            {
                RenderPipelineAsset currentPipeline = GraphicsSettings.defaultRenderPipeline;

                if (currentPipeline == null)
                    return "STANDARD";

                string pipelineType = currentPipeline.GetType().ToString();
                if (pipelineType.Contains("UniversalRenderPipelineAsset"))
                    return "URP";
                if (pipelineType.Contains("HDRenderPipelineAsset"))
                    return "HDRP";

                return "UNKNOWN";
            }

            private void OnEnable()
            {
                // Create our helper for rendering a preview.
                previewUtility = new PreviewRenderUtility();
                previewUtility.cameraFieldOfView = 30f;
                previewUtility.camera.farClipPlane = 100f;
                previewUtility.camera.nearClipPlane = 0.01f;

                // We�ll mark that we need an initial preview build.
                needsPreviewUpdate = true;

                // Find base material
                // Are we using Universal Render Pipeline?
                var detectedPipeline = DetectRenderPipeline();
                var baseShaderName = "Standard";
                var baseShaderDescription = "Using Standard shader for the generated ferns.";
                if (detectedPipeline == "URP")
                {
                    baseShaderName = "Universal Render Pipeline/Lit";
                    baseShaderDescription = "Using URP Lit shader for the generated ferns.";
                }
                else if (detectedPipeline == "HDRP")
                {
                    baseShaderName = "HDRP/Lit";
                    baseShaderDescription = "Using HDRP Lit shader for the generated ferns.";
                }
                else if (detectedPipeline == "UNKNOWN")
                {
                    baseShaderDescription = $"Detected custom render pipeline, this may cause issues. Using Standard shader.";
                }
                var litShader = Shader.Find(baseShaderName);
                if (litShader != null)
                {
                    baseMaterial = new Material(litShader);
                    baseMaterialInfo = baseShaderDescription;
                }
                else
                {
                    baseMaterial = new Material(
                        Shader.Find("Standard")
                    );
                    baseMaterialInfo = "Cannot find the base shader for the current render pipeline. Using Standard shader.";
                }

                EditorApplication.update += UpdatePreview;
            }

            private void OnDisable()
            {
                if (previewUtility != null)
                {
                    previewUtility.Cleanup();
                    previewUtility = null;
                }

                // Clean up
                if (previewMesh != null)
                {
                    DestroyImmediate(previewMesh);
                    previewMesh = null;
                }
                if (previewMaterial != null)
                {
                    DestroyImmediate(previewMaterial);
                    previewMaterial = null;
                }

                EditorApplication.update -= UpdatePreview;
            }

            void OnGUI()
            {
                GUILayout.Label("Rendering: " + baseMaterialInfo, EditorStyles.helpBox);

                EditorGUI.BeginChangeCheck();
                fernTexture = (Texture2D)EditorGUILayout.ObjectField(
                    "Fern Texture (PNG)",
                    fernTexture,
                    typeof(Texture2D),
                    false
                );
                if (EditorGUI.EndChangeCheck())
                {
                    needsPreviewUpdate = true;
                }

                EditorGUI.BeginChangeCheck();
                fernType = (FernMeshGenerator_Free_FernPlantType)EditorGUILayout.EnumPopup("Fern Type", fernType);
                if (EditorGUI.EndChangeCheck())
                {
                    needsPreviewUpdate = true;
                }

                DrawPreviewArea();

                if (GUILayout.Button("Generate Fern"))
                {
                    GenerateFernAssets();
                }
            }

            private void DrawPreviewArea()
            {
                Rect previewRect = GUILayoutUtility.GetRect(200, 200, GUILayout.ExpandWidth(true));

                if (needsPreviewUpdate && fernTexture != null)
                {
                    RegeneratePreviewFern();
                    needsPreviewUpdate = false;
                }

                if (previewMesh == null || previewMaterial == null)
                {
                    GUI.Box(previewRect, "No preview available");
                    return;
                }

                previewUtility.BeginPreview(previewRect, GUIStyle.none);

                previewUtility.lights[0].intensity = 1f;
                previewUtility.lights[0].transform.rotation = Quaternion.Euler(-50f, 50f, 0f);
                previewUtility.lights[0].shadows = LightShadows.None;
                previewUtility.lights[1].intensity = 1f;
                previewUtility.lights[1].transform.rotation = Quaternion.Euler(-30f, 0, 0f);
                previewUtility.lights[1].shadows = LightShadows.None;

                Quaternion rot = Quaternion.Euler(0, previewRotation, 0);
                Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one);

                previewUtility.DrawMesh(
                    previewMesh,
                    matrix,
                    previewMaterial,
                    0
                );

                var cam = previewUtility.camera;
                cam.transform.position = new Vector3(0, 1f, -2f);
                cam.transform.LookAt(new Vector3(0.0f, 0.5f, 0.0f));
                cam.backgroundColor = Color.gray;
                cam.clearFlags = CameraClearFlags.Color;

                // cam.Render();
                previewUtility.Render(allowScriptableRenderPipeline: true);

                Texture resultTex = previewUtility.EndPreview();
                GUI.DrawTexture(previewRect, resultTex, ScaleMode.StretchToFill, false);
            }

            private void RegeneratePreviewFern()
            {
                if (previewMesh != null) DestroyImmediate(previewMesh);
                if (previewMaterial != null) DestroyImmediate(previewMaterial);

                previewMesh = FernMeshGenerator_Free.GenerateFernMesh(fernType);
                previewMaterial = FernMeshGenerator_Free.CreateFernMaterial(baseMaterial, fernTexture);
            }

            private void GenerateFernAssets()
            {
                if (fernTexture == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please assign a fern texture first!", "OK");
                    return;
                }

                string savePrefabPath = EditorUtility.SaveFilePanelInProject(
                    "Save Fern Prefab",
                    "Fern",
                    "prefab",
                    "Select where to save the generated prefab."
                );

                if (string.IsNullOrEmpty(savePrefabPath))
                {
                    return;
                }

                Mesh fernMesh = FernMeshGenerator_Free.GenerateFernMesh(fernType);
                Material fernMat = FernMeshGenerator_Free.CreateFernMaterial(baseMaterial, fernTexture);

                string directory = Path.GetDirectoryName(savePrefabPath);
                string baseName = Path.GetFileNameWithoutExtension(savePrefabPath);
                string meshPath = Path.Combine("Assets/FernAssetCreator", baseName + "_Mesh.asset");
                string matPath = Path.Combine("Assets/FernAssetCreator", baseName + "_Material.mat");

                if (!AssetDatabase.IsValidFolder("Assets/FernAssetCreator"))
                {
                    AssetDatabase.CreateFolder("Assets", "FernAssetCreator");
                }

                AssetDatabase.CreateAsset(fernMesh, meshPath);
                AssetDatabase.CreateAsset(fernMat, matPath);

                GameObject fernGO = new GameObject(baseName);
                var mf = fernGO.AddComponent<MeshFilter>();
                mf.sharedMesh = fernMesh;

                var mr = fernGO.AddComponent<MeshRenderer>();
                mr.sharedMaterial = fernMat;

                PrefabUtility.SaveAsPrefabAsset(fernGO, savePrefabPath);

                DestroyImmediate(fernGO);

                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(
                    "Success",
                    $"Fern assets generated:\n\n{meshPath}\n{matPath}\n{savePrefabPath}",
                    "OK"
                );
            }
        }

    }
}
