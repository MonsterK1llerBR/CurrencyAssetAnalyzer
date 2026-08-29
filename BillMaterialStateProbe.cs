#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterK1llerBR.CurrencyAssetAnalyzer
{
    [BepInPlugin(
        GUID,
        NAME,
        VERSION
    )]
    public class BillMaterialStateProbe : BasePlugin
    {
        private const string GUID =
            "br.monsterk1llerbr.supermarketsimulator.billmaterialstateprobe";

        private const string NAME =
            "Bill Material State Probe";

        private const string VERSION =
            "1.1.0";

        private const string OutputFolder =
            "BillMaterialStateProbe";

        private const string ReportFileName =
            "BillMaterialStateReport.txt";

        private static BillMaterialStateProbe Instance;

        private Harmony HarmonyInstance;

        private string OutputDirectory;

        private string ReportPath;

        private static readonly HashSet<int> AnalyzedRoots =
            new HashSet<int>();

        private static readonly string[] TextureProperties =
        {
            "_BaseMap",
            "_MainTex",
            "_BumpMap",
            "_MetallicGlossMap",
            "_SpecGlossMap"
        };

        private static readonly string[] VectorProperties =
        {
            "_BaseMap_ST",
            "_MainTex_ST",
            "_Color",
            "_BaseColor"
        };

        private static readonly string[] FloatProperties =
        {
            "_Metallic",
            "_Smoothness",
            "_Glossiness",
            "_Cutoff"
        };

        public override void Load()
        {
            Instance =
                this;

            OutputDirectory =
                Path.Combine(
                    Paths.PluginPath,
                    "CurrencyAssetAnalyzer",
                    OutputFolder
                );

            ReportPath =
                Path.Combine(
                    OutputDirectory,
                    ReportFileName
                );

            Directory.CreateDirectory(
                OutputDirectory
            );

            InitializeReport();

            Log.LogInfo(
                "========================================"
            );

            Log.LogInfo(
                "Bill Material State Probe v" +
                VERSION
            );

            Log.LogInfo(
                "========================================"
            );

            Log.LogInfo(
                "Objetivo: descobrir como as notas"
            );

            Log.LogInfo(
                "selecionam regioes do atlas."
            );

            Log.LogInfo(
                "Somente diagnostico."
            );

            Log.LogInfo(
                "Nenhum asset sera alterado."
            );

            PatchSpawnMoney();
        }

        private void InitializeReport()
        {
            try
            {
                File.WriteAllText(
                    ReportPath,
                    "========================================" +
                    Environment.NewLine +
                    "BILL MATERIAL STATE PROBE" +
                    Environment.NewLine +
                    "VERSION: " +
                    VERSION +
                    Environment.NewLine +
                    "========================================" +
                    Environment.NewLine +
                    Environment.NewLine +
                    "Somente leitura." +
                    Environment.NewLine +
                    "Nenhum asset sera alterado." +
                    Environment.NewLine
                );
            }
            catch
            {
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

                if (
                    managerType == null
                )
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

                if (
                    spawnMoney == null
                )
                {
                    LogError(
                        "SpawnMoney nao encontrado."
                    );

                    return;
                }

                HarmonyInstance =
                    new Harmony(
                        GUID
                    );

                MethodInfo postfix =
                    AccessTools.Method(
                        typeof(
                            BillMaterialStateProbe
                        ),
                        nameof(
                            SpawnMoneyPostfix
                        )
                    );

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
            catch (
                Exception ex
            )
            {
                LogError(
                    "Erro aplicando patch:"
                );

                LogError(
                    ex.ToString()
                );
            }
        }

        private static void SpawnMoneyPostfix(
            object moneyPack,
            bool isCoin
        )
        {
            try
            {
                if (
                    Instance == null ||
                    moneyPack == null ||
                    isCoin
                )
                {
                    return;
                }

                GameObject root =
                    Instance.ResolveMoneyPackGameObject(
                        moneyPack
                    );

                if (
                    root == null
                )
                {
                    return;
                }

                if (
                    string.IsNullOrEmpty(
                        root.name
                    )
                )
                {
                    return;
                }

                if (
                    root.name.IndexOf(
                        "50 Dollar Pack",
                        StringComparison.OrdinalIgnoreCase
                    ) < 0
                )
                {
                    return;
                }

                int rootId =
                    root.GetInstanceID();

                if (
                    !AnalyzedRoots.Add(
                        rootId
                    )
                )
                {
                    return;
                }

                Instance.AnalyzeBill(
                    root
                );
            }
            catch (
                Exception ex
            )
            {
                if (
                    Instance != null
                )
                {
                    Instance.Log.LogError(
                        "Erro no Postfix:"
                    );

                    Instance.Log.LogError(
                        ex.ToString()
                    );
                }
            }
        }

        private void AnalyzeBill(
            GameObject root
        )
        {
            try
            {
                AppendLine();
                AppendLine(
                    "========================================"
                );
                AppendLine(
                    "50 DOLLAR PACK"
                );
                AppendLine(
                    "========================================"
                );

                AppendLine(
                    "Root: " +
                    root.name
                );

                AppendLine(
                    "Root InstanceID: " +
                    root.GetInstanceID()
                );

                AppendLine();

                Transform[] transforms =
                    root.GetComponentsInChildren<Transform>(
                        true
                    );

                AppendLine(
                    "Transforms encontrados: " +
                    (
                        transforms != null
                            ? transforms.Length
                            : 0
                    )
                );

                Renderer[] renderers =
                    root.GetComponentsInChildren<Renderer>(
                        true
                    );

                AppendLine(
                    "Renderers encontrados: " +
                    (
                        renderers != null
                            ? renderers.Length
                            : 0
                    )
                );

                AppendLine();

                AnalyzePaperDollarRenderers(
                    renderers
                );

                AppendLine();

                AppendLine(
                    "========================================"
                );

                AppendLine(
                    "ANALYSIS COMPLETE"
                );

                AppendLine(
                    "========================================"
                );

                LogInfo(
                    "Bill Material State Probe concluido."
                );
            }
            catch (
                Exception ex
            )
            {
                LogError(
                    "Erro analisando nota:"
                );

                LogError(
                    ex.ToString()
                );
            }
        }

        private void AnalyzePaperDollarRenderers(
            Renderer[] renderers
        )
        {
            if (
                renderers == null
            )
            {
                return;
            }

            int count =
                0;

            for (
                int i = 0;
                i < renderers.Length;
                i++
            )
            {
                Renderer renderer =
                    renderers[i];

                if (
                    renderer == null
                )
                {
                    continue;
                }

                GameObject go =
                    renderer.gameObject;

                if (
                    go == null
                )
                {
                    continue;
                }

                string name =
                    go.name ??
                    string.Empty;

                if (
                    name.IndexOf(
                        "Paper Dollar Base",
                        StringComparison.OrdinalIgnoreCase
                    ) < 0
                )
                {
                    continue;
                }

                count++;

                AnalyzeRenderer(
                    renderer,
                    count
                );
            }

            AppendLine();

            AppendLine(
                "Paper Dollar Base analisados: " +
                count
            );
        }

        private void AnalyzeRenderer(
            Renderer renderer,
            int number
        )
        {
            GameObject go =
                renderer.gameObject;

            AppendLine();

            AppendLine(
                "----------------------------------------"
            );

            AppendLine(
                "PAPER DOLLAR BASE #" +
                number
            );

            AppendLine(
                "GameObject: " +
                go.name
            );

            AppendLine(
                "Path: " +
                GetHierarchyPath(
                    go.transform
                )
            );

            AppendLine(
                "Renderer Type: " +
                renderer.GetType().FullName
            );

            AppendLine(
                "Renderer InstanceID: " +
                renderer.GetInstanceID()
            );

            AppendLine(
                "Enabled: " +
                renderer.enabled
            );

            AppendLine();

            AppendTransform(
                go.transform
            );

            MeshFilter filter =
                null;

            try
            {
                filter =
                    go.GetComponent<MeshFilter>();
            }
            catch
            {
            }

            if (
                filter != null
            )
            {
                AnalyzeMesh(
                    filter
                );
            }

            AnalyzeSharedMaterials(
                renderer
            );

            AnalyzeInstanceMaterial(
                renderer
            );

            AnalyzePropertyBlock(
                renderer
            );
        }

        private void AnalyzeTransform(
            Transform transform
        )
        {
            if (
                transform == null
            )
            {
                return;
            }

            Vector3 localPosition =
                transform.localPosition;

            Vector3 localEulerAngles =
                transform.localEulerAngles;

            Vector3 localScale =
                transform.localScale;

            Vector3 worldPosition =
                transform.position;

            Vector3 lossyScale =
                transform.lossyScale;

            AppendLine(
                "LocalPosition: " +
                FormatVector3(
                    localPosition
                )
            );

            AppendLine(
                "LocalRotation: " +
                FormatVector3(
                    localEulerAngles
                )
            );

            AppendLine(
                "LocalScale: " +
                FormatVector3(
                    localScale
                )
            );

            AppendLine(
                "WorldPosition: " +
                FormatVector3(
                    worldPosition
                )
            );

            AppendLine(
                "LossyScale: " +
                FormatVector3(
                    lossyScale
                )
            );
        }

        private void AppendTransform(
            Transform transform
        )
        {
            AnalyzeTransform(
                transform
            );
        }

        private void AnalyzeMesh(
            MeshFilter filter
        )
        {
            try
            {
                Mesh mesh =
                    filter.sharedMesh;

                if (
                    mesh == null
                )
                {
                    AppendLine(
                        "Mesh: null"
                    );

                    return;
                }

                AppendLine();

                AppendLine(
                    "MESH"
                );

                AppendLine(
                    "Mesh Name: " +
                    mesh.name
                );

                AppendLine(
                    "Mesh InstanceID: " +
                    mesh.GetInstanceID()
                );

                AppendLine(
                    "VertexCount: " +
                    mesh.vertexCount
                );

                AppendLine(
                    "SubMeshCount: " +
                    mesh.subMeshCount
                );

                AppendLine(
                    "IndexFormat: " +
                    mesh.indexFormat
                );

                AppendLine(
                    "BlendShapeCount: " +
                    mesh.blendShapeCount
                );

                AppendLine(
                    "Bounds: " +
                    mesh.bounds
                );

                try
                {
                    Vector2[] uv =
                        mesh.uv;

                    AppendLine(
                        "mesh.uv Length: " +
                        (
                            uv != null
                                ? uv.Length
                                : 0
                        )
                    );

                    if (
                        uv != null &&
                        uv.Length > 0
                    )
                    {
                        float minU =
                            float.MaxValue;

                        float maxU =
                            float.MinValue;

                        float minV =
                            float.MaxValue;

                        float maxV =
                            float.MinValue;

                        int sampleCount =
                            Math.Min(
                                uv.Length,
                                24
                            );

                        for (
                            int i = 0;
                            i < uv.Length;
                            i++
                        )
                        {
                            Vector2 value =
                                uv[i];

                            minU =
                                Math.Min(
                                    minU,
                                    value.x
                                );

                            maxU =
                                Math.Max(
                                    maxU,
                                    value.x
                                );

                            minV =
                                Math.Min(
                                    minV,
                                    value.y
                                );

                            maxV =
                                Math.Max(
                                    maxV,
                                    value.y
                                );
                        }

                        AppendLine(
                            "UV Bounds: (" +
                            FormatFloat(
                                minU
                            ) +
                            ", " +
                            FormatFloat(
                                minV
                            ) +
                            ") -> (" +
                            FormatFloat(
                                maxU
                            ) +
                            ", " +
                            FormatFloat(
                                maxV
                            ) +
                            ")"
                        );

                        AppendLine(
                            "UV Sample:"
                        );

                        for (
                            int i = 0;
                            i < sampleCount;
                            i++
                        )
                        {
                            AppendLine(
                                "[" +
                                i +
                                "] " +
                                FormatVector2(
                                    uv[i]
                                )
                            );
                        }
                    }
                }
                catch (
                    Exception ex
                )
                {
                    AppendLine(
                        "mesh.uv ERROR: " +
                        ex.Message
                    );
                }

                try
                {
                    Vector3[] vertices =
                        mesh.vertices;

                    AppendLine(
                        "mesh.vertices Length: " +
                        (
                            vertices != null
                                ? vertices.Length
                                : 0
                        )
                    );

                    if (
                        vertices != null &&
                        vertices.Length > 0
                    )
                    {
                        int sampleCount =
                            Math.Min(
                                vertices.Length,
                                24
                            );

                        AppendLine(
                            "Vertex Sample:"
                        );

                        for (
                            int i = 0;
                            i < sampleCount;
                            i++
                        )
                        {
                            AppendLine(
                                "[" +
                                i +
                                "] " +
                                FormatVector3(
                                    vertices[i]
                                )
                            );
                        }
                    }
                }
                catch (
                    Exception ex
                )
                {
                    AppendLine(
                        "mesh.vertices ERROR: " +
                        ex.Message
                    );
                }

                for (
                    int subMesh = 0;
                    subMesh < mesh.subMeshCount;
                    subMesh++
                )
                {
                    try
                    {
                        SubMeshDescriptor descriptor =
                            mesh.GetSubMesh(
                                subMesh
                            );

                        AppendLine();

                        AppendLine(
                            "SUBMESH " +
                            subMesh
                        );

                        AppendLine(
                            "IndexStart: " +
                            descriptor.indexStart
                        );

                        AppendLine(
                            "IndexCount: " +
                            descriptor.indexCount
                        );

                        AppendLine(
                            "BaseVertex: " +
                            descriptor.baseVertex
                        );

                        AppendLine(
                            "FirstVertex: " +
                            descriptor.firstVertex
                        );

                        AppendLine(
                            "VertexCount: " +
                            descriptor.vertexCount
                        );

                        AppendLine(
                            "Topology: " +
                            descriptor.topology
                        );
                    }
                    catch (
                        Exception ex
                    )
                    {
                        AppendLine(
                            "SubMesh " +
                            subMesh +
                            " ERROR: " +
                            ex.Message
                        );
                    }
                }
            }
            catch (
                Exception ex
            )
            {
                AppendLine(
                    "Mesh ERROR: " +
                    ex.Message
                );
            }
        }

        private void AnalyzeSharedMaterials(
            Renderer renderer
        )
        {
            try
            {
                Material[] materials =
                    renderer.sharedMaterials;

                AppendLine();

                AppendLine(
                    "SHARED MATERIALS: " +
                    (
                        materials != null
                            ? materials.Length
                            : 0
                    )
                );

                if (
                    materials == null
                )
                {
                    return;
                }

                for (
                    int i = 0;
                    i < materials.Length;
                    i++
                )
                {
                    AnalyzeMaterial(
                        materials[i],
                        "sharedMaterials[" +
                        i +
                        "]"
                    );
                }
            }
            catch (
                Exception ex
            )
            {
                AppendLine(
                    "SharedMaterials ERROR: " +
                    ex.Message
                );
            }
        }

        private void AnalyzeInstanceMaterial(
            Renderer renderer
        )
        {
            try
            {
                Material material =
                    renderer.material;

                AnalyzeMaterial(
                    material,
                    "renderer.material"
                );
            }
            catch (
                Exception ex
            )
            {
                AppendLine(
                    "renderer.material ERROR: " +
                    ex.Message
                );
            }
        }

        private void AnalyzeMaterial(
            Material material,
            string source
        )
        {
            if (
                material == null
            )
            {
                AppendLine(
                    "MATERIAL: null | Source=" +
                    source
                );

                return;
            }

            AppendLine();

            AppendLine(
                "MATERIAL"
            );

            AppendLine(
                "Source: " +
                source
            );

            AppendLine(
                "Name: " +
                material.name
            );

            AppendLine(
                "InstanceID: " +
                material.GetInstanceID()
            );

            Shader shader =
                null;

            try
            {
                shader =
                    material.shader;
            }
            catch
            {
            }

            AppendLine(
                "Shader: " +
                (
                    shader != null
                        ? shader.name
                        : "null"
                )
            );

            if (
                shader == null
            )
            {
                return;
            }

            AnalyzeTextureProperties(
                material
            );

            AnalyzeNumericProperties(
                material
            );
        }

        private void AnalyzeTextureProperties(
            Material material
        )
        {
            AppendLine();

            AppendLine(
                "TEXTURE PROPERTIES"
            );

            for (
                int i = 0;
                i < TextureProperties.Length;
                i++
            )
            {
                string property =
                    TextureProperties[i];

                try
                {
                    bool exists =
                        material.HasProperty(
                            property
                        );

                    AppendLine(
                        property +
                        " HasProperty=" +
                        exists
                    );

                    if (
                        !exists
                    )
                    {
                        continue;
                    }

                    Texture texture =
                        material.GetTexture(
                            property
                        );

                    AppendLine(
                        property +
                        " Texture=" +
                        (
                            texture != null
                                ? texture.name
                                : "null"
                        )
                    );

                    AppendLine(
                        property +
                        " TextureType=" +
                        (
                            texture != null
                                ? texture.GetType().FullName
                                : "null"
                        )
                    );

                    AppendLine(
                        property +
                        " InstanceID=" +
                        (
                            texture != null
                                ? texture.GetInstanceID().ToString()
                                : "null"
                        )
                    );

                    Vector2 scale =
                        material.GetTextureScale(
                            property
                        );

                    Vector2 offset =
                        material.GetTextureOffset(
                            property
                        );

                    AppendLine(
                        property +
                        " Scale=" +
                        FormatVector2(
                            scale
                        )
                    );

                    AppendLine(
                        property +
                        " Offset=" +
                        FormatVector2(
                            offset
                        )
                    );
                }
                catch (
                    Exception ex
                )
                {
                    AppendLine(
                        property +
                        " ERROR=" +
                        ex.Message
                    );
                }
            }
        }

        private void AnalyzeNumericProperties(
            Material material
        )
        {
            AppendLine();

            AppendLine(
                "NUMERIC / VECTOR PROPERTIES"
            );

            for (
                int i = 0;
                i < VectorProperties.Length;
                i++
            )
            {
                string property =
                    VectorProperties[i];

                try
                {
                    if (
                        !material.HasProperty(
                            property
                        )
                    )
                    {
                        continue;
                    }

                    Vector4 value =
                        material.GetVector(
                            property
                        );

                    AppendLine(
                        property +
                        " Vector=" +
                        FormatVector4(
                            value
                        )
                    );
                }
                catch (
                    Exception ex
                )
                {
                    AppendLine(
                        property +
                        " ERROR=" +
                        ex.Message
                    );
                }
            }

            for (
                int i = 0;
                i < FloatProperties.Length;
                i++
            )
            {
                string property =
                    FloatProperties[i];

                try
                {
                    if (
                        !material.HasProperty(
                            property
                        )
                    )
                    {
                        continue;
                    }

                    float value =
                        material.GetFloat(
                            property
                        );

                    AppendLine(
                        property +
                        " Float=" +
                        FormatFloat(
                            value
                        )
                    );
                }
                catch (
                    Exception ex
                )
                {
                    AppendLine(
                        property +
                        " ERROR=" +
                        ex.Message
                    );
                }
            }
        }

        private void AnalyzePropertyBlock(
            Renderer renderer
        )
        {
            try
            {
                MaterialPropertyBlock block =
                    new MaterialPropertyBlock();

                renderer.GetPropertyBlock(
                    block
                );

                AppendLine();

                AppendLine(
                    "MATERIAL PROPERTY BLOCK"
                );

                bool foundAny =
                    false;

                for (
                    int i = 0;
                    i < TextureProperties.Length;
                    i++
                )
                {
                    string property =
                        TextureProperties[i];

                    try
                    {
                        int id =
                            Shader.PropertyToID(
                                property
                            );

                        Texture texture =
                            block.GetTexture(
                                id
                            );

                        if (
                            texture != null
                        )
                        {
                            foundAny =
                                true;

                            AppendLine(
                                property +
                                " Texture=" +
                                texture.name +
                                " | InstanceID=" +
                                texture.GetInstanceID()
                            );
                        }
                    }
                    catch
                    {
                    }
                }

                for (
                    int i = 0;
                    i < VectorProperties.Length;
                    i++
                )
                {
                    string property =
                        VectorProperties[i];

                    try
                    {
                        int id =
                            Shader.PropertyToID(
                                property
                            );

                        Vector4 value =
                            block.GetVector(
                                id
                            );

                        if (
                            value != Vector4.zero
                        )
                        {
                            foundAny =
                                true;

                            AppendLine(
                                property +
                                " Vector=" +
                                FormatVector4(
                                    value
                                )
                            );
                        }
                    }
                    catch
                    {
                    }
                }

                if (
                    !foundAny
                )
                {
                    AppendLine(
                        "Nenhum override relevante encontrado."
                    );
                }
            }
            catch (
                Exception ex
            )
            {
                AppendLine(
                    "MaterialPropertyBlock ERROR: " +
                    ex.Message
                );
            }
        }

        private GameObject ResolveMoneyPackGameObject(
            object moneyPack
        )
        {
            try
            {
                GameObject direct =
                    moneyPack as GameObject;

                if (
                    direct != null
                )
                {
                    return direct;
                }
            }
            catch
            {
            }

            try
            {
                Component component =
                    moneyPack as Component;

                if (
                    component != null &&
                    component.gameObject != null
                )
                {
                    return component.gameObject;
                }
            }
            catch
            {
            }

            try
            {
                Type type =
                    moneyPack.GetType();

                PropertyInfo[] properties =
                    type.GetProperties(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                for (
                    int i = 0;
                    i < properties.Length;
                    i++
                )
                {
                    PropertyInfo property =
                        properties[i];

                    if (
                        property == null ||
                        property.GetIndexParameters().Length != 0
                    )
                    {
                        continue;
                    }

                    object value;

                    try
                    {
                        value =
                            property.GetValue(
                                moneyPack,
                                null
                            );
                    }
                    catch
                    {
                        continue;
                    }

                    GameObject gameObject =
                        value as GameObject;

                    if (
                        gameObject != null
                    )
                    {
                        return gameObject;
                    }

                    Component childComponent =
                        value as Component;

                    if (
                        childComponent != null &&
                        childComponent.gameObject != null
                    )
                    {
                        return childComponent.gameObject;
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private Type FindType(
            string name
        )
        {
            try
            {
                Type type =
                    AccessTools.TypeByName(
                        name
                    );

                if (
                    type != null
                )
                {
                    return type;
                }
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
                            name
                        );

                    if (
                        type != null
                    )
                    {
                        return type;
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        private MethodInfo FindSpawnMoneyMethod(
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
                        method == null ||
                        method.Name !=
                        "SpawnMoney"
                    )
                    {
                        continue;
                    }

                    ParameterInfo[] parameters =
                        method.GetParameters();

                    if (
                        parameters.Length != 2
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
            catch
            {
            }

            return null;
        }

        private string GetHierarchyPath(
            Transform transform
        )
        {
            if (
                transform == null
            )
            {
                return string.Empty;
            }

            List<string> parts =
                new List<string>();

            Transform current =
                transform;

            while (
                current != null
            )
            {
                parts.Add(
                    current.name
                );

                current =
                    current.parent;
            }

            parts.Reverse();

            return string.Join(
                "/",
                parts.ToArray()
            );
        }

        private static string FormatVector2(
            Vector2 value
        )
        {
            return
                "(" +
                FormatFloat(
                    value.x
                ) +
                ", " +
                FormatFloat(
                    value.y
                ) +
                ")";
        }

        private static string FormatVector3(
            Vector3 value
        )
        {
            return
                "(" +
                FormatFloat(
                    value.x
                ) +
                ", " +
                FormatFloat(
                    value.y
                ) +
                ", " +
                FormatFloat(
                    value.z
                ) +
                ")";
        }

        private static string FormatVector4(
            Vector4 value
        )
        {
            return
                "(" +
                FormatFloat(
                    value.x
                ) +
                ", " +
                FormatFloat(
                    value.y
                ) +
                ", " +
                FormatFloat(
                    value.z
                ) +
                ", " +
                FormatFloat(
                    value.w
                ) +
                ")";
        }

        private static string FormatFloat(
            float value
        )
        {
            return value.ToString(
                "0.######",
                CultureInfo.InvariantCulture
            );
        }

        private void AppendLine(
            string line = ""
        )
        {
            try
            {
                File.AppendAllText(
                    ReportPath,
                    line +
                    Environment.NewLine
                );
            }
            catch
            {
            }
        }

        private void LogInfo(
            string message
        )
        {
            Log.LogInfo(
                message
            );
        }

        private void LogError(
            string message
        )
        {
            Log.LogError(
                message
            );
        }

        public override bool Unload()
        {
            try
            {
                if (
                    HarmonyInstance != null
                )
                {
                    HarmonyInstance.UnpatchSelf();
                }
            }
            catch
            {
            }

            return true;
        }
    }
}

