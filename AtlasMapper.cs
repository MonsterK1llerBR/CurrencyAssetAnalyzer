#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace MonsterK1llerBR.CurrencyAssetAnalyzer
{
    [BepInPlugin(
        GUID,
        NAME,
        VERSION
    )]
    public class CurrencyAtlasMapper : BasePlugin
    {
        private const string GUID =
            "br.monsterk1llerbr.supermarketsimulator.currencyatlasmapper";

        private const string NAME =
            "Currency Atlas Mapper";

        private const string VERSION =
            "2.0.0";

        private const string RepositoryRoot =
            @"C:\Users\natan\Documents\Mods\SupermarketSimulator\CurrencyAssetAnalyzer";

        private const string RepositoryAtlasDirectory =
            @"C:\Users\natan\Documents\Mods\SupermarketSimulator\CurrencyAssetAnalyzer\Reports\AtlasMapping";

        private static CurrencyAtlasMapper Instance;

        private Harmony HarmonyInstance;

        private static readonly HashSet<string> AnalyzedCoins =
            new HashSet<string>();

        private static string ReportDirectory;

        private static string OutputDirectory;

        private static string ReportFile;

        private static Texture2D DiagnosticTexture;

        private const int DiagnosticTextureSize = 1024;

        private const int CaptureSize = 1024;

        private const int DiagnosticLayer = 31;

        private static readonly byte[] PngSignature =
        {
            137,
            80,
            78,
            71,
            13,
            10,
            26,
            10
        };

        public override void Load()
        {
            Instance = this;

            try
            {
                ReportDirectory =
                    Path.Combine(
                        Paths.PluginPath,
                        "CurrencyAssetAnalyzer",
                        "AnalyzerV9"
                    );

                OutputDirectory =
                    Path.Combine(
                        ReportDirectory,
                        "AtlasMapping"
                    );

                ReportFile =
                    Path.Combine(
                        OutputDirectory,
                        "AtlasMapReport.txt"
                    );

                Directory.CreateDirectory(
                    ReportDirectory
                );

                Directory.CreateDirectory(
                    OutputDirectory
                );

                InitializeReport();

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "Currency Atlas Mapper v2.0.0"
                );

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "Metodo: Renderer real + Camera real."
                );

                LogInfo(
                    "Mesh.uv direto: NAO UTILIZADO."
                );

                LogInfo(
                    "Camera artificial: DESATIVADA."
                );

                LogInfo(
                    "Probe artificial: DESATIVADO."
                );

                LogInfo(
                    "Captura diagnostica U/V: ATIVADA."
                );

                LogInfo(
                    "Saida: " +
                    OutputDirectory
                );

                CreateDiagnosticTexture();

                PatchSpawnMoney();
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro inicializando Currency Atlas Mapper: " +
                    ex
                );
            }
        }

        private static void InitializeReport()
        {
            try
            {
                using (
                    StreamWriter writer =
                        new StreamWriter(
                            ReportFile,
                            false
                        )
                )
                {
                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "CURRENCY ATLAS MAPPER"
                    );

                    writer.WriteLine(
                        "VERSION: " +
                        VERSION
                    );

                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "Renderer real utilizado."
                    );

                    writer.WriteLine(
                        "Camera real utilizada."
                    );

                    writer.WriteLine(
                        "O material original e restaurado apos cada captura."
                    );

                    writer.WriteLine(
                        "A posicao original da moeda e restaurada."
                    );

                    writer.WriteLine();
                }
            }
            catch
            {
            }
        }

        private static void CreateDiagnosticTexture()
        {
            try
            {
                DiagnosticTexture =
                    new Texture2D(
                        DiagnosticTextureSize,
                        DiagnosticTextureSize,
                        TextureFormat.RGBA32,
                        false,
                        true
                    );

                DiagnosticTexture.name =
                    "CurrencyAtlasMapper_DiagnosticUV";

                DiagnosticTexture.filterMode =
                    FilterMode.Point;

                DiagnosticTexture.wrapMode =
                    TextureWrapMode.Clamp;

                Color32[] pixels =
                    new Color32[
                        DiagnosticTextureSize *
                        DiagnosticTextureSize
                    ];

                for (
                    int y = 0;
                    y < DiagnosticTextureSize;
                    y++
                )
                {
                    float v =
                        (float)y /
                        (float)(
                            DiagnosticTextureSize -
                            1
                        );

                    byte green =
                        (byte)(
                            Mathf.Clamp01(
                                v
                            ) *
                            255f
                        );

                    for (
                        int x = 0;
                        x < DiagnosticTextureSize;
                        x++
                    )
                    {
                        float u =
                            (float)x /
                            (float)(
                                DiagnosticTextureSize -
                                1
                            );

                        byte red =
                            (byte)(
                                Mathf.Clamp01(
                                    u
                                ) *
                                255f
                            );

                        pixels[
                            y *
                            DiagnosticTextureSize +
                            x
                        ] =
                            new Color32(
                                red,
                                green,
                                255,
                                255
                            );
                    }
                }

                DiagnosticTexture.SetPixels32(
                    pixels
                );

                DiagnosticTexture.Apply(
                    false,
                    false
                );

                string diagnosticPath =
                    Path.Combine(
                        OutputDirectory,
                        "UV_Diagnostic_1024.png"
                    );

                byte[] png =
                    EncodeTextureToPng(
                        DiagnosticTexture
                    );

                if (
                    png != null &&
                    png.Length > 0
                )
                {
                    File.WriteAllBytes(
                        diagnosticPath,
                        png
                    );

                    SyncToRepository(
                        diagnosticPath
                    );
                }

                LogInfo(
                    "Textura diagnostica criada."
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro criando textura diagnostica: " +
                    ex
                );
            }
        }

        private void PatchSpawnMoney()
        {
            try
            {
                Type managerType =
                    FindType(
                        "CheckoutChangeManager"
                    );

                if (managerType == null)
                {
                    LogError(
                        "CheckoutChangeManager nao encontrado."
                    );

                    return;
                }

                MethodInfo spawnMoney =
                    FindSpawnMoneyMethod(
                        managerType
                    );

                if (spawnMoney == null)
                {
                    LogError(
                        "SpawnMoney(MoneyPack, bool) nao encontrado."
                    );

                    return;
                }

                HarmonyInstance =
                    new Harmony(
                        GUID
                    );

                MethodInfo postfix =
                    AccessTools.Method(
                        typeof(CurrencyAtlasMapper),
                        nameof(SpawnMoneyPostfix)
                    );

                if (postfix == null)
                {
                    LogError(
                        "SpawnMoneyPostfix nao encontrado."
                    );

                    return;
                }

                HarmonyInstance.Patch(
                    spawnMoney,
                    null,
                    new HarmonyMethod(
                        postfix
                    ),
                    null,
                    null,
                    null
                );

                LogInfo(
                    "Patch de SpawnMoney aplicado."
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro aplicando patch: " +
                    ex
                );
            }
        }

        private static Type FindType(
            string typeName
        )
        {
            try
            {
                Type type =
                    AccessTools.TypeByName(
                        typeName
                    );

                if (type != null)
                    return type;
            }
            catch
            {
            }

            Assembly[] assemblies =
                AppDomain.CurrentDomain.GetAssemblies();

            for (
                int i = 0;
                i < assemblies.Length;
                i++
            )
            {
                try
                {
                    Type type =
                        assemblies[i].GetType(
                            typeName
                        );

                    if (type != null)
                        return type;
                }
                catch
                {
                }
            }

            return null;
        }

        private static MethodInfo FindSpawnMoneyMethod(
            Type managerType
        )
        {
            try
            {
                MethodInfo[] methods =
                    managerType.GetMethods(
                        BindingFlags.Instance |
                        BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                for (
                    int i = 0;
                    i < methods.Length;
                    i++
                )
                {
                    MethodInfo method =
                        methods[i];

                    if (
                        method.Name !=
                        "SpawnMoney"
                    )
                    {
                        continue;
                    }

                    ParameterInfo[] parameters =
                        method.GetParameters();

                    if (
                        parameters.Length !=
                        2
                    )
                    {
                        continue;
                    }

                    if (
                        parameters[1].ParameterType !=
                        typeof(bool)
                    )
                    {
                        continue;
                    }

                    return method;
                }
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro procurando SpawnMoney: " +
                    ex
                );
            }

            return null;
        }

        private static void SpawnMoneyPostfix(
            object moneyPack,
            bool isCoin
        )
        {
            try
            {
                if (Instance == null)
                    return;

                if (!isCoin)
                    return;

                if (moneyPack == null)
                    return;

                GameObject root =
                    ReadMoneyPackGameObject(
                        moneyPack
                    );

                if (root == null)
                    return;

                AnalyzeCoinPack(
                    root
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro no Postfix: " +
                    ex
                );
            }
        }

        private static GameObject ReadMoneyPackGameObject(
            object moneyPack
        )
        {
            try
            {
                Type type =
                    moneyPack.GetType();

                PropertyInfo property =
                    type.GetProperty(
                        "gameObject",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                if (property != null)
                {
                    object result =
                        property.GetValue(
                            moneyPack,
                            null
                        );

                    GameObject gameObject =
                        result as GameObject;

                    if (gameObject != null)
                        return gameObject;
                }
            }
            catch
            {
            }

            return null;
        }

        private static void AnalyzeCoinPack(
            GameObject root
        )
        {
            try
            {
                MeshFilter filter =
                    FindCoinMeshFilter(
                        root.transform
                    );

                if (filter == null)
                {
                    LogError(
                        "MeshFilter da moeda nao encontrado: " +
                        root.name
                    );

                    return;
                }

                Mesh mesh =
                    filter.sharedMesh;

                if (mesh == null)
                {
                    LogError(
                        "Mesh nulo em: " +
                        root.name
                    );

                    return;
                }

                string coinName =
                    mesh.name;

                if (
                    !coinName.StartsWith(
                        "SM_Coin_",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return;
                }

                if (
                    !AnalyzedCoins.Add(
                        coinName
                    )
                )
                {
                    return;
                }

                Renderer renderer =
                    FindRendererForMesh(
                        filter
                    );

                if (renderer == null)
                {
                    LogError(
                        "Renderer da moeda nao encontrado: " +
                        coinName
                    );

                    return;
                }

                Camera camera =
                    FindBestGameCamera();

                if (camera == null)
                {
                    LogError(
                        "Nenhuma Camera ativa encontrada."
                    );

                    return;
                }

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "Mapeando: " +
                    coinName
                );

                LogInfo(
                    "Mesh vertices: " +
                    mesh.vertexCount
                );

                LogInfo(
                    "Renderer: " +
                    renderer.GetType().FullName
                );

                LogInfo(
                    "Camera: " +
                    camera.name
                );

                CoinMappingResult result =
                    CaptureRealCoin(
                        coinName,
                        filter,
                        renderer,
                        camera
                    );

                WriteCoinReport(
                    result
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro analisando moeda: " +
                    ex
                );
            }
        }

        private static MeshFilter FindCoinMeshFilter(
            Transform node
        )
        {
            if (node == null)
                return null;

            MeshFilter filter =
                node.GetComponent<MeshFilter>();

            if (
                filter != null &&
                filter.sharedMesh != null
            )
            {
                string meshName =
                    filter.sharedMesh.name;

                if (
                    meshName.StartsWith(
                        "SM_Coin_",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return filter;
                }
            }

            for (
                int i = 0;
                i < node.childCount;
                i++
            )
            {
                MeshFilter result =
                    FindCoinMeshFilter(
                        node.GetChild(i)
                    );

                if (result != null)
                    return result;
            }

            return null;
        }

        private static Renderer FindRendererForMesh(
            MeshFilter filter
        )
        {
            if (filter == null)
                return null;

            Renderer renderer =
                filter.GetComponent<Renderer>();

            if (renderer != null)
                return renderer;

            try
            {
                Renderer[] renderers =
                    filter.gameObject.GetComponentsInChildren<Renderer>();

                if (
                    renderers != null &&
                    renderers.Length > 0
                )
                {
                    for (
                        int i = 0;
                        i < renderers.Length;
                        i++
                    )
                    {
                        if (renderers[i] != null)
                            return renderers[i];
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private static Camera FindBestGameCamera()
        {
            try
            {
                Camera main =
                    Camera.main;

                if (
                    main != null &&
                    main.isActiveAndEnabled
                )
                {
                    return main;
                }
            }
            catch
            {
            }

            try
            {
                Camera[] cameras =
                    UnityEngine.Object.FindObjectsOfType<Camera>();

                if (
                    cameras != null &&
                    cameras.Length > 0
                )
                {
                    for (
                        int i = 0;
                        i < cameras.Length;
                        i++
                    )
                    {
                        Camera camera =
                            cameras[i];

                        if (
                            camera != null &&
                            camera.isActiveAndEnabled
                        )
                        {
                            return camera;
                        }
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private static CoinMappingResult CaptureRealCoin(
            string coinName,
            MeshFilter filter,
            Renderer renderer,
            Camera camera
        )
        {
            CoinMappingResult result =
                new CoinMappingResult();

            result.CoinName =
                coinName;

            result.MeshName =
                (
                    filter.sharedMesh != null
                        ? filter.sharedMesh.name
                        : "null"
                );

            result.MinU =
                1f;

            result.MinV =
                1f;

            result.MaxU =
                0f;

            result.MaxV =
                0f;

            Transform coinTransform =
                filter.transform;

            Transform originalParent =
                coinTransform.parent;

            Vector3 originalPosition =
                coinTransform.position;

            Quaternion originalRotation =
                coinTransform.rotation;

            Vector3 originalScale =
                coinTransform.localScale;

            int originalLayer =
                renderer.gameObject.layer;

            Material originalMaterial =
                renderer.sharedMaterial;

            Material diagnosticMaterial =
                null;

            RenderTexture renderTexture =
                null;

            Texture2D readable =
                null;

            RenderTexture previousTarget =
                camera.targetTexture;

            RenderTexture previousActive =
                RenderTexture.active;

            CameraClearFlags previousClearFlags =
                camera.clearFlags;

            Color previousBackground =
                camera.backgroundColor;

            int previousCullingMask =
                camera.cullingMask;

            bool previousHDR =
                camera.allowHDR;

            bool previousMSAA =
                camera.allowMSAA;

            bool rendererWasEnabled =
                renderer.enabled;

            try
            {
                Shader diagnosticShader =
                    FindDiagnosticShader();

                if (
                    diagnosticShader == null
                )
                {
                    throw new Exception(
                        "Nenhum shader Unlit disponivel."
                    );
                }

                diagnosticMaterial =
                    new Material(
                        diagnosticShader
                    );

                diagnosticMaterial.name =
                    "CurrencyAtlasMapper_RuntimeDiagnostic";

                SetDiagnosticTexture(
                    diagnosticMaterial
                );

                renderer.gameObject.layer =
                    DiagnosticLayer;

                renderer.enabled =
                    true;

                renderer.sharedMaterial =
                    diagnosticMaterial;

                PositionCoinForCamera(
                    coinTransform,
                    camera,
                    filter.sharedMesh
                );

                renderTexture =
                    new RenderTexture(
                        CaptureSize,
                        CaptureSize,
                        24,
                        RenderTextureFormat.ARGB32
                    );

                renderTexture.name =
                    "CurrencyAtlasMapper_RT";

                renderTexture.Create();

                if (
                    !renderTexture.IsCreated()
                )
                {
                    throw new Exception(
                        "RenderTexture nao foi criada."
                    );
                }

                camera.targetTexture =
                    renderTexture;

                camera.clearFlags =
                    CameraClearFlags.SolidColor;

                camera.backgroundColor =
                    Color.black;

                camera.cullingMask =
                    1 <<
                    DiagnosticLayer;

                camera.allowHDR =
                    false;

                camera.allowMSAA =
                    false;

                CameraRenderWithoutPostProcessing(
                    camera
                );

                RenderTexture.active =
                    renderTexture;

                readable =
                    new Texture2D(
                        CaptureSize,
                        CaptureSize,
                        TextureFormat.RGBA32,
                        false,
                        true
                    );

                readable.ReadPixels(
                    new Rect(
                        0,
                        0,
                        CaptureSize,
                        CaptureSize
                    ),
                    0,
                    0,
                    false
                );

                readable.Apply(
                    false,
                    false
                );

                Color32[] pixels =
                    readable.GetPixels32();

                int valid =
                    AnalyzeDiagnosticPixels(
                        result,
                        pixels
                    );

                result.ValidPixelCount =
                    valid;

                result.MappingFound =
                    valid >
                    20;

                string safeCoin =
                    SanitizeFileName(
                        coinName
                    );

                string outputPath =
                    Path.Combine(
                        OutputDirectory,
                        safeCoin +
                        "_REAL_CAMERA.png"
                    );

                byte[] png =
                    EncodeTextureToPng(
                        readable
                    );

                if (
                    png != null &&
                    png.Length > 0
                )
                {
                    File.WriteAllBytes(
                        outputPath,
                        png
                    );

                    SyncToRepository(
                        outputPath
                    );
                }

                LogInfo(
                    "Captura real concluida: " +
                    coinName +
                    " | Pixels=" +
                    valid +
                    " | U=" +
                    FormatFloat(result.MinU) +
                    ".." +
                    FormatFloat(result.MaxU) +
                    " | V=" +
                    FormatFloat(result.MinV) +
                    ".." +
                    FormatFloat(result.MaxV)
                );

                return result;
            }
            catch (Exception ex)
            {
                result.Error =
                    ex.ToString();

                LogError(
                    "Erro capturando moeda real " +
                    coinName +
                    ": " +
                    ex
                );

                return result;
            }
            finally
            {
                try
                {
                    RenderTexture.active =
                        previousActive;
                }
                catch
                {
                }

                try
                {
                    camera.targetTexture =
                        previousTarget;

                    camera.clearFlags =
                        previousClearFlags;

                    camera.backgroundColor =
                        previousBackground;

                    camera.cullingMask =
                        previousCullingMask;

                    camera.allowHDR =
                        previousHDR;

                    camera.allowMSAA =
                        previousMSAA;
                }
                catch
                {
                }

                try
                {
                    renderer.sharedMaterial =
                        originalMaterial;
                }
                catch
                {
                }

                try
                {
                    renderer.enabled =
                        rendererWasEnabled;
                }
                catch
                {
                }

                try
                {
                    renderer.gameObject.layer =
                        originalLayer;
                }
                catch
                {
                }

                try
                {
                    coinTransform.SetParent(
                        originalParent,
                        true
                    );

                    coinTransform.position =
                        originalPosition;

                    coinTransform.rotation =
                        originalRotation;

                    coinTransform.localScale =
                        originalScale;
                }
                catch
                {
                }

                try
                {
                    if (readable != null)
                    {
                        UnityEngine.Object.Destroy(
                            readable
                        );
                    }
                }
                catch
                {
                }

                try
                {
                    if (renderTexture != null)
                    {
                        renderTexture.Release();

                        UnityEngine.Object.Destroy(
                            renderTexture
                        );
                    }
                }
                catch
                {
                }

                try
                {
                    if (diagnosticMaterial != null)
                    {
                        UnityEngine.Object.Destroy(
                            diagnosticMaterial
                        );
                    }
                }
                catch
                {
                }
            }
        }

        private static Shader FindDiagnosticShader()
        {
            try
            {
                Shader shader =
                    Shader.Find(
                        "Universal Render Pipeline/Unlit"
                    );

                if (shader != null)
                    return shader;
            }
            catch
            {
            }

            try
            {
                Shader shader =
                    Shader.Find(
                        "Unlit/Texture"
                    );

                if (shader != null)
                    return shader;
            }
            catch
            {
            }

            return null;
        }

        private static void SetDiagnosticTexture(
            Material material
        )
        {
            bool setBase =
                false;

            bool setMain =
                false;

            try
            {
                if (
                    material.HasProperty(
                        "_BaseMap"
                    )
                )
                {
                    material.SetTexture(
                        "_BaseMap",
                        DiagnosticTexture
                    );

                    setBase =
                        true;
                }
            }
            catch
            {
            }

            try
            {
                if (
                    material.HasProperty(
                        "_MainTex"
                    )
                )
                {
                    material.SetTexture(
                        "_MainTex",
                        DiagnosticTexture
                    );

                    setMain =
                        true;
                }
            }
            catch
            {
            }

            if (
                !setBase &&
                !setMain
            )
            {
                throw new Exception(
                    "Shader diagnostico nao possui _BaseMap nem _MainTex."
                );
            }

            try
            {
                if (
                    material.HasProperty(
                        "_Color"
                    )
                )
                {
                    material.SetColor(
                        "_Color",
                        Color.white
                    );
                }
            }
            catch
            {
            }

            try
            {
                if (
                    material.HasProperty(
                        "_BaseColor"
                    )
                )
                {
                    material.SetColor(
                        "_BaseColor",
                        Color.white
                    );
                }
            }
            catch
            {
            }
        }

        private static void PositionCoinForCamera(
            Transform coinTransform,
            Camera camera,
            Mesh mesh
        )
        {
            if (
                coinTransform == null ||
                camera == null
            )
            {
                return;
            }

            Transform originalParent =
                coinTransform.parent;

            coinTransform.SetParent(
                null,
                true
            );

            Bounds bounds =
                mesh != null
                    ? mesh.bounds
                    : new Bounds(
                        Vector3.zero,
                        Vector3.one
                    );

            float radius =
                Mathf.Max(
                    bounds.extents.x,
                    bounds.extents.y,
                    bounds.extents.z
                );

            if (
                radius <
                0.001f
            )
            {
                radius =
                    0.01f;
            }

            float distance =
                Mathf.Max(
                    radius * 8f,
                    0.5f
                );

            Vector3 target =
                camera.transform.position +
                camera.transform.forward *
                distance;

            coinTransform.position =
                target;

            coinTransform.rotation =
                camera.transform.rotation;

            coinTransform.localScale =
                Vector3.one;

            try
            {
                Renderer renderer =
                    coinTransform.GetComponent<Renderer>();

                if (renderer != null)
                {
                    Bounds worldBounds =
                        renderer.bounds;

                    float size =
                        Mathf.Max(
                            worldBounds.size.x,
                            worldBounds.size.y,
                            worldBounds.size.z
                        );

                    if (
                        size > 0.001f
                    )
                    {
                        float desired =
                            distance *
                            0.55f;

                        float multiplier =
                            desired /
                            size;

                        multiplier =
                            Mathf.Clamp(
                                multiplier,
                                0.25f,
                                8f
                            );

                        coinTransform.localScale =
                            Vector3.one *
                            multiplier;
                    }
                }
            }
            catch
            {
            }

            coinTransform.SetParent(
                originalParent,
                true
            );
        }

        private static void CameraRenderWithoutPostProcessing(
            Camera camera
        )
        {
            camera.Render();
        }

        private static int AnalyzeDiagnosticPixels(
            CoinMappingResult result,
            Color32[] pixels
        )
        {
            if (
                pixels == null
            )
            {
                return 0;
            }

            int valid =
                0;

            for (
                int i = 0;
                i < pixels.Length;
                i++
            )
            {
                Color32 pixel =
                    pixels[i];

                if (
                    pixel.b <
                    180
                )
                {
                    continue;
                }

                if (
                    pixel.r < 2 &&
                    pixel.g < 2
                )
                {
                    continue;
                }

                float u =
                    pixel.r /
                    255f;

                float v =
                    pixel.g /
                    255f;

                if (
                    u < result.MinU
                )
                {
                    result.MinU =
                        u;
                }

                if (
                    u > result.MaxU
                )
                {
                    result.MaxU =
                        u;
                }

                if (
                    v < result.MinV
                )
                {
                    result.MinV =
                        v;
                }

                if (
                    v > result.MaxV
                )
                {
                    result.MaxV =
                        v;
                }

                valid++;
            }

            return valid;
        }

        private static void WriteCoinReport(
            CoinMappingResult result
        )
        {
            try
            {
                using (
                    StreamWriter writer =
                        new StreamWriter(
                            ReportFile,
                            true
                        )
                )
                {
                    writer.WriteLine(
                        "----------------------------------------"
                    );

                    writer.WriteLine(
                        "Coin: " +
                        result.CoinName
                    );

                    writer.WriteLine(
                        "Mesh: " +
                        result.MeshName
                    );

                    writer.WriteLine(
                        "MappingFound: " +
                        result.MappingFound
                    );

                    writer.WriteLine(
                        "ValidPixelCount: " +
                        result.ValidPixelCount
                    );

                    writer.WriteLine(
                        "UV Min U: " +
                        FormatFloat(
                            result.MinU
                        )
                    );

                    writer.WriteLine(
                        "UV Max U: " +
                        FormatFloat(
                            result.MaxU
                        )
                    );

                    writer.WriteLine(
                        "UV Min V: " +
                        FormatFloat(
                            result.MinV
                        )
                    );

                    writer.WriteLine(
                        "UV Max V: " +
                        FormatFloat(
                            result.MaxV
                        )
                    );

                    writer.WriteLine(
                        "UV Width: " +
                        FormatFloat(
                            result.MaxU -
                            result.MinU
                        )
                    );

                    writer.WriteLine(
                        "UV Height: " +
                        FormatFloat(
                            result.MaxV -
                            result.MinV
                        )
                    );

                    if (
                        !string.IsNullOrWhiteSpace(
                            result.Error
                        )
                    )
                    {
                        writer.WriteLine(
                            "ERROR:"
                        );

                        writer.WriteLine(
                            result.Error
                        );
                    }

                    writer.WriteLine();
                }

                SyncToRepository(
                    ReportFile
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro escrevendo relatorio: " +
                    ex
                );
            }
        }

        private static byte[] EncodeTextureToPng(
            Texture2D texture
        )
        {
            if (texture == null)
                return null;

            Color32[] pixels =
                texture.GetPixels32();

            if (
                pixels == null ||
                pixels.Length !=
                texture.width *
                texture.height
            )
            {
                return null;
            }

            return EncodePixelsToPng(
                pixels,
                texture.width,
                texture.height
            );
        }

        private static byte[] EncodePixelsToPng(
            Color32[] pixels,
            int width,
            int height
        )
        {
            if (
                pixels == null ||
                pixels.Length !=
                width *
                height
            )
            {
                return null;
            }

            using (
                MemoryStream output =
                    new MemoryStream()
            )
            {
                output.Write(
                    PngSignature,
                    0,
                    PngSignature.Length
                );

                byte[] ihdr =
                    new byte[13];

                WriteUInt32BigEndian(
                    ihdr,
                    0,
                    (uint)width
                );

                WriteUInt32BigEndian(
                    ihdr,
                    4,
                    (uint)height
                );

                ihdr[8] =
                    8;

                ihdr[9] =
                    6;

                ihdr[10] =
                    0;

                ihdr[11] =
                    0;

                ihdr[12] =
                    0;

                WritePngChunk(
                    output,
                    "IHDR",
                    ihdr
                );

                using (
                    MemoryStream raw =
                        new MemoryStream()
                )
                {
                    for (
                        int y = height - 1;
                        y >= 0;
                        y--
                    )
                    {
                        raw.WriteByte(
                            0
                        );

                        int rowStart =
                            y *
                            width;

                        for (
                            int x = 0;
                            x < width;
                            x++
                        )
                        {
                            Color32 pixel =
                                pixels[
                                    rowStart +
                                    x
                                ];

                            raw.WriteByte(
                                pixel.r
                            );

                            raw.WriteByte(
                                pixel.g
                            );

                            raw.WriteByte(
                                pixel.b
                            );

                            raw.WriteByte(
                                pixel.a
                            );
                        }
                    }

                    using (
                        MemoryStream compressed =
                            new MemoryStream()
                    )
                    {
                        using (
                            ZLibStream zlib =
                                new ZLibStream(
                                    compressed,
                                    System.IO.Compression.CompressionLevel.Optimal,
                                    true
                                )
                        )
                        {
                            byte[] rawBytes =
                                raw.ToArray();

                            zlib.Write(
                                rawBytes,
                                0,
                                rawBytes.Length
                            );
                        }

                        WritePngChunk(
                            output,
                            "IDAT",
                            compressed.ToArray()
                        );
                    }
                }

                WritePngChunk(
                    output,
                    "IEND",
                    new byte[0]
                );

                return output.ToArray();
            }
        }

        private static void WritePngChunk(
            Stream stream,
            string type,
            byte[] data
        )
        {
            byte[] typeBytes =
                Encoding.ASCII.GetBytes(
                    type
                );

            WriteUInt32BigEndian(
                stream,
                (uint)data.Length
            );

            stream.Write(
                typeBytes,
                0,
                typeBytes.Length
            );

            if (
                data != null &&
                data.Length > 0
            )
            {
                stream.Write(
                    data,
                    0,
                    data.Length
                );
            }

            uint crc =
                ComputeCrc32(
                    typeBytes,
                    data
                );

            WriteUInt32BigEndian(
                stream,
                crc
            );
        }

        private static uint ComputeCrc32(
            byte[] type,
            byte[] data
        )
        {
            uint crc =
                0xFFFFFFFFu;

            if (type != null)
            {
                for (
                    int i = 0;
                    i < type.Length;
                    i++
                )
                {
                    crc =
                        UpdateCrc32(
                            crc,
                            type[i]
                        );
                }
            }

            if (data != null)
            {
                for (
                    int i = 0;
                    i < data.Length;
                    i++
                )
                {
                    crc =
                        UpdateCrc32(
                            crc,
                            data[i]
                        );
                }
            }

            return ~crc;
        }

        private static uint UpdateCrc32(
            uint crc,
            byte value
        )
        {
            uint current =
                crc ^
                value;

            for (
                int i = 0;
                i < 8;
                i++
            )
            {
                if (
                    (current & 1u) !=
                    0u
                )
                {
                    current =
                        (
                            current >>
                            1
                        ) ^
                        0xEDB88320u;
                }
                else
                {
                    current >>=
                        1;
                }
            }

            return current;
        }

        private static void WriteUInt32BigEndian(
            byte[] buffer,
            int offset,
            uint value
        )
        {
            buffer[offset] =
                (byte)(
                    (value >> 24) &
                    0xFF
                );

            buffer[offset + 1] =
                (byte)(
                    (value >> 16) &
                    0xFF
                );

            buffer[offset + 2] =
                (byte)(
                    (value >> 8) &
                    0xFF
                );

            buffer[offset + 3] =
                (byte)(
                    value &
                    0xFF
                );
        }

        private static void WriteUInt32BigEndian(
            Stream stream,
            uint value
        )
        {
            stream.WriteByte(
                (byte)(
                    (value >> 24) &
                    0xFF
                )
            );

            stream.WriteByte(
                (byte)(
                    (value >> 16) &
                    0xFF
                )
            );

            stream.WriteByte(
                (byte)(
                    (value >> 8) &
                    0xFF
                )
            );

            stream.WriteByte(
                (byte)(
                    value &
                    0xFF
                )
            );
        }

        private static void SyncToRepository(
            string sourceFile
        )
        {
            try
            {
                if (
                    string.IsNullOrWhiteSpace(
                        sourceFile
                    )
                )
                {
                    return;
                }

                if (
                    !File.Exists(
                        sourceFile
                    )
                )
                {
                    return;
                }

                Directory.CreateDirectory(
                    RepositoryAtlasDirectory
                );

                string destination =
                    Path.Combine(
                        RepositoryAtlasDirectory,
                        Path.GetFileName(
                            sourceFile
                        )
                    );

                File.Copy(
                    sourceFile,
                    destination,
                    true
                );

                LogInfo(
                    "Sincronizado: " +
                    destination
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro sincronizando: " +
                    ex
                );
            }
        }

        private static string SanitizeFileName(
            string value
        )
        {
            char[] invalidChars =
                Path.GetInvalidFileNameChars();

            for (
                int i = 0;
                i < invalidChars.Length;
                i++
            )
            {
                value =
                    value.Replace(
                        invalidChars[i],
                        '_'
                    );
            }

            return value;
        }

        private static string FormatFloat(
            float value
        )
        {
            return value.ToString(
                "0.000000",
                CultureInfo.InvariantCulture
            );
        }

        private static void LogInfo(
            string message
        )
        {
            if (Instance != null)
            {
                Instance.Log.LogInfo(
                    message
                );
            }
        }

        private static void LogError(
            string message
        )
        {
            if (Instance != null)
            {
                Instance.Log.LogError(
                    message
                );
            }
        }

        private sealed class CoinMappingResult
        {
            public string CoinName;

            public string MeshName;

            public bool MappingFound;

            public int ValidPixelCount;

            public float MinU;

            public float MaxU;

            public float MinV;

            public float MaxV;

            public string Error;
        }
    }
}