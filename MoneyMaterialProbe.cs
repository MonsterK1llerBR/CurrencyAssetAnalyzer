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

namespace MonsterK1llerBR.CurrencyAssetAnalyzer
{
    [BepInPlugin(
        GUID,
        NAME,
        VERSION
    )]
    public class MoneyMaterialProbe : BasePlugin
    {
        private const string GUID =
            "br.monsterk1llerbr.supermarketsimulator.moneymaterialprobe";

        private const string NAME =
            "Money Material Probe";

        private const string VERSION =
            "1.0.0";

        private const string RootFolder =
            "CurrencyAssetAnalyzer";

        private const string OutputFolder =
            "MoneyMaterialProbe";

        private const string ReportFileName =
            "MoneyMaterialProbeReport.txt";

        private static MoneyMaterialProbe Instance;

        private Harmony HarmonyInstance;

        private static readonly HashSet<string> AnalyzedObjects =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        private static readonly string[] TextureProperties =
        {
            "_BaseMap",
            "_MainTex",
            "_BumpMap",
            "_MetallicGlossMap",
            "_SpecGlossMap"
        };

        public override void Load()
        {
            Instance =
                this;

            try
            {
                Log.LogInfo(
                    "========================================"
                );

                Log.LogInfo(
                    "Money Material Probe v" +
                    VERSION
                );

                Log.LogInfo(
                    "========================================"
                );

                Log.LogInfo(
                    "Objetivo: identificar Renderer, Material e textura usados pelos MoneyPack."
                );

                Log.LogInfo(
                    "Mesh: NAO ALTERADO."
                );

                Log.LogInfo(
                    "UV: NAO ALTERADO."
                );

                Log.LogInfo(
                    "Material: NAO ALTERADO."
                );

                Log.LogInfo(
                    "Textura: NAO ALTERADA."
                );

                InitializeReport();

                PatchSpawnMoney();
            }
            catch (Exception ex)
            {
                Log.LogError(
                    "Erro durante inicializacao do probe: " +
                    ex
                );
            }
        }

        private void InitializeReport()
        {
            try
            {
                string directory =
                    GetOutputDirectory();

                Directory.CreateDirectory(
                    directory
                );

                string path =
                    Path.Combine(
                        directory,
                        ReportFileName
                    );

                using (
                    StreamWriter writer =
                        new StreamWriter(
                            path,
                            false
                        )
                )
                {
                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "MONEY MATERIAL PROBE"
                    );

                    writer.WriteLine(
                        "VERSION: " +
                        VERSION
                    );

                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine();

                    writer.WriteLine(
                        "Objetivo:"
                    );

                    writer.WriteLine(
                        "Identificar exatamente quais Renderer,"
                    );

                    writer.WriteLine(
                        "Material e propriedades de textura"
                    );

                    writer.WriteLine(
                        "sao usados pelos MoneyPack."
                    );

                    writer.WriteLine();

                    writer.WriteLine(
                        "IMPORTANTE:"
                    );

                    writer.WriteLine(
                        "Nenhum asset do jogo sera alterado."
                    );

                    writer.WriteLine();

                    writer.WriteLine(
                        "Texture Properties analisadas:"
                    );

                    for (
                        int i = 0;
                        i < TextureProperties.Length;
                        i++
                    )
                    {
                        writer.WriteLine(
                            " - " +
                            TextureProperties[i]
                        );
                    }

                    writer.WriteLine();

                    writer.WriteLine(
                        "========================================"
                    );
                }

                Log.LogInfo(
                    "Relatorio inicializado:"
                );

                Log.LogInfo(
                    path
                );
            }
            catch (Exception ex)
            {
                Log.LogError(
                    "Erro criando relatorio: " +
                    ex
                );
            }
        }

        private string GetOutputDirectory()
        {
            return Path.Combine(
                Paths.PluginPath,
                RootFolder,
                OutputFolder
            );
        }

        private string GetReportPath()
        {
            return Path.Combine(
                GetOutputDirectory(),
                ReportFileName
            );
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
                    Log.LogError(
                        "CheckoutChangeManager nao encontrado."
                    );

                    return;
                }

                Log.LogInfo(
                    "CheckoutChangeManager encontrado: " +
                    managerType.FullName
                );

                MethodInfo spawnMoney =
                    FindSpawnMoneyMethod(
                        managerType
                    );

                if (
                    spawnMoney == null
                )
                {
                    Log.LogError(
                        "SpawnMoney(MoneyPack, Boolean) nao encontrado."
                    );

                    return;
                }

                Log.LogInfo(
                    "SpawnMoney encontrado: " +
                    spawnMoney
                );

                HarmonyInstance =
                    new Harmony(
                        GUID
                    );

                MethodInfo postfix =
                    AccessTools.Method(
                        typeof(MoneyMaterialProbe),
                        nameof(
                            SpawnMoneyPostfix
                        )
                    );

                if (
                    postfix == null
                )
                {
                    Log.LogError(
                        "SpawnMoneyPostfix nao encontrado."
                    );

                    return;
                }

                HarmonyInstance.Patch(
                    spawnMoney,
                    postfix:
                        new HarmonyMethod(
                            postfix
                        )
                );

                Log.LogInfo(
                    "Patch de SpawnMoney aplicado."
                );
            }
            catch (Exception ex)
            {
                Log.LogError(
                    "Erro aplicando patch: " +
                    ex
                );
            }
        }

        private Type FindType(
            string name
        )
        {
            try
            {
                Assembly[] assemblies =
                    AppDomain.CurrentDomain.GetAssemblies();

                for (
                    int i = 0;
                    i < assemblies.Length;
                    i++
                )
                {
                    Assembly assembly =
                        assemblies[i];

                    if (
                        assembly == null
                    )
                    {
                        continue;
                    }

                    try
                    {
                        Type[] types =
                            assembly.GetTypes();

                        for (
                            int t = 0;
                            t < types.Length;
                            t++
                        )
                        {
                            Type type =
                                types[t];

                            if (
                                type == null
                            )
                            {
                                continue;
                            }

                            if (
                                string.Equals(
                                    type.Name,
                                    name,
                                    StringComparison.Ordinal
                                )
                                ||
                                string.Equals(
                                    type.FullName,
                                    name,
                                    StringComparison.Ordinal
                                )
                            )
                            {
                                return type;
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError(
                    "Erro procurando tipo " +
                    name +
                    ": " +
                    ex
                );
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
                Log.LogError(
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
                if (
                    Instance == null ||
                    moneyPack == null
                )
                {
                    return;
                }

                GameObject gameObject =
                    ReadMoneyPackGameObject(
                        moneyPack
                    );

                if (
                    gameObject == null
                )
                {
                    Instance.Log.LogWarning(
                        "MoneyPack encontrado, mas GameObject nao foi obtido."
                    );

                    return;
                }

                float value =
                    ReadMoneyPackValue(
                        moneyPack
                    );

                string key =
                    (
                        isCoin
                            ? "COIN"
                            : "BILL"
                    ) +
                    "|" +
                    gameObject.name +
                    "|" +
                    value.ToString(
                        "0.00",
                        CultureInfo.InvariantCulture
                    );

                if (
                    !AnalyzedObjects.Add(
                        key
                    )
                )
                {
                    return;
                }

                Instance.Log.LogInfo(
                    "========================================"
                );

                Instance.Log.LogInfo(
                    "MONEY MATERIAL PROBE"
                );

                Instance.Log.LogInfo(
                    "Tipo: " +
                    (
                        isCoin
                            ? "COIN"
                            : "BILL"
                    )
                );

                Instance.Log.LogInfo(
                    "GameObject: " +
                    gameObject.name
                );

                Instance.Log.LogInfo(
                    "Value: " +
                    value.ToString(
                        CultureInfo.InvariantCulture
                    )
                );

                AnalyzeHierarchy(
                    gameObject
                );

                Instance.Log.LogInfo(
                    "Analise concluida: " +
                    gameObject.name
                );

                Instance.Log.LogInfo(
                    "========================================"
                );
            }
            catch (Exception ex)
            {
                if (
                    Instance != null
                )
                {
                    Instance.Log.LogError(
                        "Erro no Postfix: " +
                        ex
                    );
                }
            }
        }

        private static float ReadMoneyPackValue(
            object moneyPack
        )
        {
            try
            {
                Type type =
                    moneyPack.GetType();

                PropertyInfo property =
                    type.GetProperty(
                        "Value",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                if (
                    property != null
                )
                {
                    object result =
                        property.GetValue(
                            moneyPack,
                            null
                        );

                    if (
                        result is float
                    )
                    {
                        return (
                            float
                        )
                        result;
                    }
                }
            }
            catch
            {
            }

            try
            {
                Type type =
                    moneyPack.GetType();

                FieldInfo field =
                    type.GetField(
                        "m_Value",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                if (
                    field != null
                )
                {
                    object result =
                        field.GetValue(
                            moneyPack
                        );

                    if (
                        result is float
                    )
                    {
                        return (
                            float
                        )
                        result;
                    }
                }
            }
            catch
            {
            }

            return -1f;
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

                if (
                    property != null
                )
                {
                    object result =
                        property.GetValue(
                            moneyPack,
                            null
                        );

                    GameObject gameObject =
                        result as GameObject;

                    if (
                        gameObject != null
                    )
                    {
                        return gameObject;
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private static void AnalyzeHierarchy(
            GameObject root
        )
        {
            if (
                root == null
            )
            {
                return;
            }

            string path =
                root.name;

            AnalyzeGameObject(
                root,
                path,
                0
            );
        }

        private static void AnalyzeGameObject(
            GameObject gameObject,
            string hierarchyPath,
            int depth
        )
        {
            if (
                gameObject == null
            )
            {
                return;
            }

            try
            {
                string indent =
                    new string(
                        ' ',
                        depth * 2
                    );

                StreamWriter writer =
                    null;

                try
                {
                    writer =
                        new StreamWriter(
                            Instance.GetReportPath(),
                            true
                        );

                    writer.WriteLine(
                        indent +
                        "GAMEOBJECT"
                    );

                    writer.WriteLine(
                        indent +
                        "--------------------------------"
                    );

                    writer.WriteLine(
                        indent +
                        "Name: " +
                        gameObject.name
                    );

                    writer.WriteLine(
                        indent +
                        "Path: " +
                        hierarchyPath
                    );

                    writer.WriteLine(
                        indent +
                        "InstanceID: " +
                        gameObject.GetInstanceID()
                    );

                    writer.WriteLine();

                    AnalyzeRenderers(
                        gameObject,
                        hierarchyPath,
                        depth,
                        writer
                    );

                    writer.WriteLine();

                    AnalyzeMeshFilters(
                        gameObject,
                        depth,
                        writer
                    );

                    writer.WriteLine();

                    writer.Flush();
                }
                finally
                {
                    if (
                        writer != null
                    )
                    {
                        writer.Dispose();
                    }
                }

                Transform transform =
                    gameObject.transform;

                if (
                    transform == null
                )
                {
                    return;
                }

                int childCount =
                    transform.childCount;

                for (
                    int i = 0;
                    i < childCount;
                    i++
                )
                {
                    Transform child =
                        transform.GetChild(
                            i
                        );

                    if (
                        child == null
                    )
                    {
                        continue;
                    }

                    GameObject childObject =
                        child.gameObject;

                    if (
                        childObject == null
                    )
                    {
                        continue;
                    }

                    string childPath =
                        hierarchyPath +
                        "/" +
                        childObject.name;

                    AnalyzeGameObject(
                        childObject,
                        childPath,
                        depth + 1
                    );
                }
            }
            catch (Exception ex)
            {
                AppendLine(
                    "ERRO ANALISANDO GAMEOBJECT: " +
                    ex
                );
            }
        }

        private static void AnalyzeRenderers(
            GameObject gameObject,
            string hierarchyPath,
            int depth,
            StreamWriter writer
        )
        {
            try
            {
                Renderer[] renderers =
                    gameObject.GetComponents<Renderer>();

                if (
                    renderers == null ||
                    renderers.Length == 0
                )
                {
                    return;
                }

                string indent =
                    new string(
                        ' ',
                        depth * 2
                    );

                writer.WriteLine(
                    indent +
                    "RENDERERS"
                );

                writer.WriteLine(
                    indent +
                    "--------------------------------"
                );

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

                    writer.WriteLine(
                        indent +
                        "Renderer #" +
                        (
                            i + 1
                        )
                    );

                    writer.WriteLine(
                        indent +
                        "Type: " +
                        renderer.GetType().FullName
                    );

                    writer.WriteLine(
                        indent +
                        "Enabled: " +
                        renderer.enabled
                    );

                    Material[] materials =
                        renderer.sharedMaterials;

                    if (
                        materials == null
                    )
                    {
                        writer.WriteLine(
                            indent +
                            "Materials: null"
                        );

                        continue;
                    }

                    writer.WriteLine(
                        indent +
                        "Materials: " +
                        materials.Length
                    );

                    for (
                        int m = 0;
                        m < materials.Length;
                        m++
                    )
                    {
                        Material material =
                            materials[m];

                        if (
                            material == null
                        )
                        {
                            continue;
                        }

                        writer.WriteLine();

                        writer.WriteLine(
                            indent +
                            "MATERIAL #" +
                            (
                                m + 1
                            )
                        );

                        writer.WriteLine(
                            indent +
                            "Name: " +
                            material.name
                        );

                        writer.WriteLine(
                            indent +
                            "InstanceID: " +
                            material.GetInstanceID()
                        );

                        Shader shader =
                            material.shader;

                        if (
                            shader != null
                        )
                        {
                            writer.WriteLine(
                                indent +
                                "Shader: " +
                                shader.name
                            );
                        }
                        else
                        {
                            writer.WriteLine(
                                indent +
                                "Shader: null"
                            );
                        }

                        AnalyzeTextureProperties(
                            material,
                            gameObject,
                            hierarchyPath,
                            indent,
                            writer
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine(
                    "ERRO RENDERERS: " +
                    ex
                );
            }
        }

        private static void AnalyzeTextureProperties(
            Material material,
            GameObject owner,
            string hierarchyPath,
            string indent,
            StreamWriter writer
        )
        {
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
                    if (
                        !material.HasProperty(
                            property
                        )
                    )
                    {
                        writer.WriteLine(
                            indent +
                            "TextureProperty " +
                            property +
                            ": NAO EXISTE"
                        );

                        continue;
                    }

                    Texture texture =
                        material.GetTexture(
                            property
                        );

                    writer.WriteLine(
                        indent +
                        "TEXTURE PROPERTY"
                    );

                    writer.WriteLine(
                        indent +
                        "Property: " +
                        property
                    );

                    if (
                        texture == null
                    )
                    {
                        writer.WriteLine(
                            indent +
                            "Texture: null"
                        );

                        continue;
                    }

                    string textureName =
                        texture.name ??
                        string.Empty;

                    writer.WriteLine(
                        indent +
                        "Texture Name: " +
                        textureName
                    );

                    writer.WriteLine(
                        indent +
                        "Texture Type: " +
                        texture.GetType().FullName
                    );

                    writer.WriteLine(
                        indent +
                        "Texture InstanceID: " +
                        texture.GetInstanceID()
                    );

                    writer.WriteLine(
                        indent +
                        "Dimensions: " +
                        texture.width +
                        "x" +
                        texture.height
                    );

                    bool target =
                        textureName.IndexOf(
                            "T_Money_AlbedoTransparency",
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0;

                    writer.WriteLine(
                        indent +
                        "TARGET MONEY ALBEDO: " +
                        (
                            target
                                ? "TRUE"
                                : "FALSE"
                        )
                    );

                    if (
                        target
                    )
                    {
                        Instance.Log.LogInfo(
                            "TARGET TEXTURE ENCONTRADA:"
                        );

                        Instance.Log.LogInfo(
                            "GameObject=" +
                            owner.name
                        );

                        Instance.Log.LogInfo(
                            "Path=" +
                            hierarchyPath
                        );

                        Instance.Log.LogInfo(
                            "Material=" +
                            material.name
                        );

                        Instance.Log.LogInfo(
                            "Property=" +
                            property
                        );

                        Instance.Log.LogInfo(
                            "Texture=" +
                            textureName
                        );

                        Instance.Log.LogInfo(
                            "InstanceID=" +
                            texture.GetInstanceID()
                        );
                    }
                }
                catch (Exception ex)
                {
                    writer.WriteLine(
                        indent +
                        "ERRO PROPERTY " +
                        property +
                        ": " +
                        ex.Message
                    );
                }
            }
        }

        private static void AnalyzeMeshFilters(
            GameObject gameObject,
            int depth,
            StreamWriter writer
        )
        {
            try
            {
                MeshFilter[] filters =
                    gameObject.GetComponents<MeshFilter>();

                if (
                    filters == null ||
                    filters.Length == 0
                )
                {
                    return;
                }

                string indent =
                    new string(
                        ' ',
                        depth * 2
                    );

                writer.WriteLine(
                    indent +
                    "MESH FILTERS"
                );

                writer.WriteLine(
                    indent +
                    "--------------------------------"
                );

                for (
                    int i = 0;
                    i < filters.Length;
                    i++
                )
                {
                    MeshFilter filter =
                        filters[i];

                    if (
                        filter == null
                    )
                    {
                        continue;
                    }

                    Mesh mesh =
                        filter.sharedMesh;

                    writer.WriteLine(
                        indent +
                        "MeshFilter #" +
                        (
                            i + 1
                        )
                    );

                    if (
                        mesh == null
                    )
                    {
                        writer.WriteLine(
                            indent +
                            "Mesh: null"
                        );

                        continue;
                    }

                    writer.WriteLine(
                        indent +
                        "Mesh Name: " +
                        mesh.name
                    );

                    writer.WriteLine(
                        indent +
                        "Vertex Count: " +
                        mesh.vertexCount
                    );

                    writer.WriteLine(
                        indent +
                        "SubMesh Count: " +
                        mesh.subMeshCount
                    );
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine(
                    "ERRO MESH FILTERS: " +
                    ex
                );
            }
        }

        private static void AppendLine(
            string line
        )
        {
            try
            {
                if (
                    Instance == null
                )
                {
                    return;
                }

                Directory.CreateDirectory(
                    Instance.GetOutputDirectory()
                );

                File.AppendAllText(
                    Instance.GetReportPath(),
                    line +
                    Environment.NewLine
                );
            }
            catch
            {
            }
        }
    }
}
