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
    public class CurrencyAssetAnalyzer : BasePlugin
    {
        private const string GUID =
            "br.monsterk1llerbr.supermarketsimulator.currencyassetanalyzer";

        private const string NAME =
            "Currency Asset Analyzer";

        private const string VERSION =
            "7.4.0";

        private static CurrencyAssetAnalyzer Instance;

        private Harmony HarmonyInstance;

        private static readonly HashSet<string> AnalyzedMoneyPacks =
            new HashSet<string>();

        private static string ReportDirectory;
        private static string MoneyPackDirectory;

        public override void Load()
        {
            Instance = this;

            try
            {
                ReportDirectory = Path.Combine(
                    Paths.PluginPath,
                    "CurrencyAssetAnalyzer",
                    "AnalyzerV7"
                );

                MoneyPackDirectory = Path.Combine(
                    ReportDirectory,
                    "MoneyPack"
                );

                Directory.CreateDirectory(ReportDirectory);
                Directory.CreateDirectory(MoneyPackDirectory);

                Log.LogInfo("========================================");
                Log.LogInfo("Currency Asset Analyzer v7.4.0");
                Log.LogInfo("========================================");
                Log.LogInfo(
                    "Modo: engenharia reversa exclusiva de MoneyPack."
                );
                Log.LogInfo(
                    "Scan global de GameObjects: DESATIVADO."
                );
                Log.LogInfo(
                    "Análise de componentes físicos: via reflexão."
                );
                Log.LogInfo(
                    "Análise UV/Mesh: ATIVADA."
                );
                Log.LogInfo(
                    "Objetivo: identificar regiões UV das texturas."
                );
                Log.LogInfo(
                    "Relatórios: " + ReportDirectory
                );

                PatchSpawnMoney();
            }
            catch (Exception ex)
            {
                Log.LogError(
                    "Erro durante inicialização: " +
                    ex
                );
            }
        }

        private void PatchSpawnMoney()
        {
            try
            {
                Type managerType =
                    FindType("CheckoutChangeManager");

                if (managerType == null)
                {
                    Log.LogError(
                        "CheckoutChangeManager não encontrado."
                    );

                    return;
                }

                Log.LogInfo(
                    "CheckoutChangeManager encontrado: " +
                    managerType.FullName
                );

                MethodInfo spawnMoney =
                    FindSpawnMoneyMethod(managerType);

                if (spawnMoney == null)
                {
                    Log.LogError(
                        "SpawnMoney(MoneyPack, bool) não encontrado."
                    );

                    return;
                }

                Log.LogInfo(
                    "SpawnMoney selecionado: " +
                    spawnMoney
                );

                HarmonyInstance =
                    new Harmony(GUID);

                MethodInfo postfix =
                    AccessTools.Method(
                        typeof(CurrencyAssetAnalyzer),
                        nameof(SpawnMoneyPostfix)
                    );

                if (postfix == null)
                {
                    Log.LogError(
                        "Não foi possível localizar SpawnMoneyPostfix."
                    );

                    return;
                }

                HarmonyInstance.Patch(
                    spawnMoney,
                    null,
                    new HarmonyMethod(postfix),
                    null,
                    null,
                    null
                );

                Log.LogInfo(
                    "Patch de SpawnMoney aplicado com sucesso."
                );
            }
            catch (Exception ex)
            {
                Log.LogError(
                    "Erro aplicando patch de SpawnMoney: " +
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
                    AccessTools.TypeByName(typeName);

                if (type != null)
                    return type;
            }
            catch
            {
            }

            Assembly[] assemblies =
                AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++)
            {
                try
                {
                    Type type =
                        assemblies[i].GetType(typeName);

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

                    if (method.Name != "SpawnMoney")
                        continue;

                    ParameterInfo[] parameters =
                        method.GetParameters();

                    if (parameters.Length != 2)
                        continue;

                    if (parameters[1].ParameterType != typeof(bool))
                        continue;

                    return method;
                }
            }
            catch (Exception ex)
            {
                if (Instance != null)
                {
                    Instance.Log.LogError(
                        "Erro procurando SpawnMoney: " +
                        ex
                    );
                }
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

                if (moneyPack == null)
                    return;

                AnalyzeMoneyPack(
                    moneyPack,
                    isCoin
                );
            }
            catch (Exception ex)
            {
                if (Instance != null)
                {
                    Instance.Log.LogError(
                        "Erro no Postfix de SpawnMoney: " +
                        ex
                    );
                }
            }
        }

        private static void AnalyzeMoneyPack(
            object moneyPack,
            bool isCoin
        )
        {
            try
            {
                float value =
                    ReadMoneyPackValue(
                        moneyPack
                    );

                GameObject gameObject =
                    ReadMoneyPackGameObject(
                        moneyPack
                    );

                if (gameObject == null)
                {
                    Instance.Log.LogWarning(
                        "MoneyPack encontrado, mas o GameObject não pôde " +
                        "ser obtido. Value=" +
                        value
                    );

                    return;
                }

                string packName =
                    gameObject.name;

                string key =
                    (isCoin ? "COIN" : "BILL") +
                    "|" +
                    packName +
                    "|" +
                    value.ToString(
                        CultureInfo.InvariantCulture
                    );

                if (!AnalyzedMoneyPacks.Add(key))
                    return;

                Instance.Log.LogInfo(
                    "Novo MoneyPack descoberto: " +
                    (isCoin ? "COIN" : "BILL") +
                    " | Name=" +
                    packName +
                    " | Value=" +
                    value
                );

                AnalyzeHierarchy(
                    gameObject,
                    isCoin,
                    value
                );
            }
            catch (Exception ex)
            {
                Instance.Log.LogError(
                    "Erro analisando MoneyPack: " +
                    ex
                );
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

                if (property != null)
                {
                    object result =
                        property.GetValue(
                            moneyPack,
                            null
                        );

                    if (result is float)
                        return (float)result;
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

                if (field != null)
                {
                    object result =
                        field.GetValue(
                            moneyPack
                        );

                    if (result is float)
                        return (float)result;
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

        private static void AnalyzeHierarchy(
            GameObject root,
            bool isCoin,
            float value
        )
        {
            try
            {
                string safeName =
                    SanitizeFileName(
                        root.name
                    );

                string typeName =
                    isCoin ? "COIN" : "BILL";

                string valueString =
                    value.ToString(
                        "0.00",
                        CultureInfo.InvariantCulture
                    );

                string fileName =
                    typeName +
                    "_" +
                    safeName +
                    "_" +
                    valueString +
                    ".txt";

                string path =
                    Path.Combine(
                        MoneyPackDirectory,
                        fileName
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
                        "CURRENCY ASSET ANALYZER v7.4.0"
                    );

                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "Trigger: SpawnMoney"
                    );

                    writer.WriteLine(
                        "Type: " +
                        typeName
                    );

                    writer.WriteLine(
                        "MoneyPack: " +
                        root.name
                    );

                    writer.WriteLine(
                        "Value: " +
                        value
                    );

                    writer.WriteLine(
                        "InstanceID: " +
                        root.GetInstanceID()
                    );

                    writer.WriteLine(
                        "ActiveSelf: " +
                        root.activeSelf
                    );

                    writer.WriteLine(
                        "ActiveInHierarchy: " +
                        root.activeInHierarchy
                    );

                    writer.WriteLine();

                    writer.WriteLine(
                        "HIERARQUIA"
                    );

                    writer.WriteLine(
                        "----------------------------------------"
                    );

                    AnalyzeTransform(
                        root.transform,
                        0,
                        root.transform,
                        writer
                    );

                    writer.WriteLine();

                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "FIM DO RELATÓRIO"
                    );

                    writer.WriteLine(
                        "========================================"
                    );
                }

                Instance.Log.LogInfo(
                    "Relatório MoneyPack salvo: " +
                    path
                );
            }
            catch (Exception ex)
            {
                Instance.Log.LogError(
                    "Erro criando relatório: " +
                    ex
                );
            }
        }

        private static void AnalyzeTransform(
            Transform transform,
            int depth,
            Transform root,
            StreamWriter writer
        )
        {
            try
            {
                string indent =
                    new string(
                        ' ',
                        depth * 2
                    );

                GameObject gameObject =
                    transform.gameObject;

                writer.WriteLine(
                    indent +
                    "GameObject: " +
                    gameObject.name
                );

                writer.WriteLine(
                    indent +
                    "Path: " +
                    GetRelativePath(
                        transform,
                        root
                    )
                );

                writer.WriteLine(
                    indent +
                    "LocalPosition: " +
                    transform.localPosition
                );

                writer.WriteLine(
                    indent +
                    "LocalRotation: " +
                    transform.localEulerAngles
                );

                writer.WriteLine(
                    indent +
                    "LocalScale: " +
                    transform.localScale
                );

                AnalyzeComponentsByReflection(
                    gameObject,
                    indent,
                    writer
                );

                writer.WriteLine();

                for (
                    int i = 0;
                    i < transform.childCount;
                    i++
                )
                {
                    AnalyzeTransform(
                        transform.GetChild(i),
                        depth + 1,
                        root,
                        writer
                    );
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine(
                    "[ERRO TRANSFORM] " +
                    ex.Message
                );
            }
        }

        private static void AnalyzeComponentsByReflection(
            GameObject gameObject,
            string indent,
            StreamWriter writer
        )
        {
            try
            {
                writer.WriteLine(
                    indent +
                    "COMPONENTES"
                );

                writer.WriteLine(
                    indent +
                    "--------------------------------"
                );

                Component[] components =
                    GetAllComponentsSafely(
                        gameObject
                    );

                writer.WriteLine(
                    indent +
                    "Count: " +
                    components.Length
                );

                for (
                    int i = 0;
                    i < components.Length;
                    i++
                )
                {
                    Component component =
                        components[i];

                    if (component == null)
                        continue;

                    Type type =
                        component.GetType();

                    writer.WriteLine(
                        indent +
                        "- " +
                        (
                            type.FullName ??
                            type.Name
                        )
                    );
                }

                AnalyzeRenderer(
                    gameObject,
                    indent,
                    writer
                );

                AnalyzeMeshFilter(
                    gameObject,
                    indent,
                    writer
                );

                AnalyzePhysicsComponents(
                    gameObject,
                    indent,
                    writer
                );
            }
            catch (Exception ex)
            {
                writer.WriteLine(
                    indent +
                    "[ERRO COMPONENTES] " +
                    ex.Message
                );
            }
        }

        private static Component[] GetAllComponentsSafely(
            GameObject gameObject
        )
        {
            try
            {
                Component[] components =
                    gameObject.GetComponents<Component>();

                if (components != null)
                    return components;
            }
            catch
            {
            }

            return new Component[0];
        }

        private static void AnalyzeRenderer(
            GameObject gameObject,
            string indent,
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

                writer.WriteLine();

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

                    if (renderer == null)
                        continue;

                    writer.WriteLine(
                        indent +
                        "RENDERER #" +
                        (i + 1)
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

                    if (materials == null)
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

                        if (material == null)
                            continue;

                        writer.WriteLine(
                            indent +
                            "MATERIAL #" +
                            (m + 1)
                        );

                        writer.WriteLine(
                            indent +
                            "Name: " +
                            material.name
                        );

                        Shader shader =
                            material.shader;

                        if (shader != null)
                        {
                            writer.WriteLine(
                                indent +
                                "Shader: " +
                                shader.name
                            );
                        }

                        AnalyzeTextureProperty(
                            material,
                            "_BaseMap",
                            indent,
                            writer
                        );

                        AnalyzeTextureProperty(
                            material,
                            "_MainTex",
                            indent,
                            writer
                        );

                        AnalyzeTextureProperty(
                            material,
                            "_BumpMap",
                            indent,
                            writer
                        );

                        AnalyzeTextureProperty(
                            material,
                            "_MetallicGlossMap",
                            indent,
                            writer
                        );

                        AnalyzeTextureProperty(
                            material,
                            "_SpecGlossMap",
                            indent,
                            writer
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine(
                    indent +
                    "[ERRO RENDERER] " +
                    ex.Message
                );
            }
        }

        private static void AnalyzeTextureProperty(
            Material material,
            string property,
            string indent,
            StreamWriter writer
        )
        {
            try
            {
                if (!material.HasProperty(property))
                    return;

                Texture texture =
                    material.GetTexture(
                        property
                    );

                if (texture == null)
                    return;

                writer.WriteLine(
                    indent +
                    "TEXTURE"
                );

                writer.WriteLine(
                    indent +
                    "Property: " +
                    property
                );

                writer.WriteLine(
                    indent +
                    "Name: " +
                    texture.name
                );

                writer.WriteLine(
                    indent +
                    "Type: " +
                    texture.GetType().FullName
                );
            }
            catch
            {
            }
        }

        private static void AnalyzeMeshFilter(
            GameObject gameObject,
            string indent,
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

                writer.WriteLine();

                writer.WriteLine(
                    indent +
                    "MESH FILTER"
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

                    if (filter == null)
                        continue;

                    writer.WriteLine(
                        indent +
                        "MeshFilter #" +
                        (i + 1)
                    );

                    writer.WriteLine(
                        indent +
                        "Type: " +
                        filter.GetType().FullName
                    );

                    Mesh mesh =
                        filter.sharedMesh;

                    if (mesh == null)
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
                        "Vertices: " +
                        mesh.vertexCount
                    );

                    writer.WriteLine(
                        indent +
                        "SubMeshes: " +
                        mesh.subMeshCount
                    );

                    AnalyzeMeshUV(
                        mesh,
                        indent,
                        writer
                    );
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine(
                    indent +
                    "[ERRO MESHFILTER] " +
                    ex.Message
                );
            }
        }

        private static void AnalyzeMeshUV(
            Mesh mesh,
            string indent,
            StreamWriter writer
        )
        {
            try
            {
                writer.WriteLine();

                writer.WriteLine(
                    indent +
                    "UV ANALYSIS"
                );

                writer.WriteLine(
                    indent +
                    "--------------------------------"
                );

                Vector2[] uv =
                    mesh.uv;

                if (
                    uv == null ||
                    uv.Length == 0
                )
                {
                    writer.WriteLine(
                        indent +
                        "UV0: NONE"
                    );

                    return;
                }

                writer.WriteLine(
                    indent +
                    "UV0 Count: " +
                    uv.Length
                );

                float minX =
                    float.MaxValue;

                float maxX =
                    float.MinValue;

                float minY =
                    float.MaxValue;

                float maxY =
                    float.MinValue;

                for (
                    int i = 0;
                    i < uv.Length;
                    i++
                )
                {
                    Vector2 value =
                        uv[i];

                    if (value.x < minX)
                        minX = value.x;

                    if (value.x > maxX)
                        maxX = value.x;

                    if (value.y < minY)
                        minY = value.y;

                    if (value.y > maxY)
                        maxY = value.y;
                }

                writer.WriteLine(
                    indent +
                    "UV0 Bounds:"
                );

                writer.WriteLine(
                    indent +
                    "  MinX: " +
                    FormatFloat(minX)
                );

                writer.WriteLine(
                    indent +
                    "  MaxX: " +
                    FormatFloat(maxX)
                );

                writer.WriteLine(
                    indent +
                    "  MinY: " +
                    FormatFloat(minY)
                );

                writer.WriteLine(
                    indent +
                    "  MaxY: " +
                    FormatFloat(maxY)
                );

                writer.WriteLine();

                writer.WriteLine(
                    indent +
                    "UV0 VERTICES"
                );

                writer.WriteLine(
                    indent +
                    "--------------------------------"
                );

                for (
                    int i = 0;
                    i < uv.Length;
                    i++
                )
                {
                    writer.WriteLine(
                        indent +
                        "Vertex[" +
                        i +
                        "] = (" +
                        FormatFloat(uv[i].x) +
                        ", " +
                        FormatFloat(uv[i].y) +
                        ")"
                    );
                }

                writer.WriteLine();

                AnalyzeSubMeshes(
                    mesh,
                    uv,
                    indent,
                    writer
                );
            }
            catch (Exception ex)
            {
                writer.WriteLine(
                    indent +
                    "[ERRO UV] " +
                    ex
                );
            }
        }

        private static void AnalyzeSubMeshes(
            Mesh mesh,
            Vector2[] uv,
            string indent,
            StreamWriter writer
        )
        {
            try
            {
                writer.WriteLine(
                    indent +
                    "SUBMESH UV ANALYSIS"
                );

                writer.WriteLine(
                    indent +
                    "--------------------------------"
                );

                for (
                    int subMeshIndex = 0;
                    subMeshIndex < mesh.subMeshCount;
                    subMeshIndex++
                )
                {
                    int[] indices;

                    try
                    {
                        indices =
                            mesh.GetIndices(
                                subMeshIndex
                            );
                    }
                    catch (Exception ex)
                    {
                        writer.WriteLine(
                            indent +
                            "SubMesh #" +
                            (subMeshIndex + 1) +
                            ": ERRO AO LER INDICES: " +
                            ex.Message
                        );

                        continue;
                    }

                    writer.WriteLine(
                        indent +
                        "SubMesh #" +
                        (subMeshIndex + 1)
                    );

                    writer.WriteLine(
                        indent +
                        "Index Count: " +
                        (
                            indices == null
                                ? 0
                                : indices.Length
                        )
                    );

                    if (
                        indices == null ||
                        indices.Length == 0
                    )
                    {
                        writer.WriteLine();
                        continue;
                    }

                    float minX =
                        float.MaxValue;

                    float maxX =
                        float.MinValue;

                    float minY =
                        float.MaxValue;

                    float maxY =
                        float.MinValue;

                    HashSet<int> uniqueVertices =
                        new HashSet<int>();

                    for (
                        int i = 0;
                        i < indices.Length;
                        i++
                    )
                    {
                        int vertexIndex =
                            indices[i];

                        if (
                            vertexIndex < 0 ||
                            vertexIndex >= uv.Length
                        )
                        {
                            continue;
                        }

                        uniqueVertices.Add(
                            vertexIndex
                        );

                        Vector2 value =
                            uv[vertexIndex];

                        if (value.x < minX)
                            minX = value.x;

                        if (value.x > maxX)
                            maxX = value.x;

                        if (value.y < minY)
                            minY = value.y;

                        if (value.y > maxY)
                            maxY = value.y;
                    }

                    writer.WriteLine(
                        indent +
                        "Unique Vertices: " +
                        uniqueVertices.Count
                    );

                    writer.WriteLine(
                        indent +
                        "UV Bounds:"
                    );

                    writer.WriteLine(
                        indent +
                        "  MinX: " +
                        FormatFloat(minX)
                    );

                    writer.WriteLine(
                        indent +
                        "  MaxX: " +
                        FormatFloat(maxX)
                    );

                    writer.WriteLine(
                        indent +
                        "  MinY: " +
                        FormatFloat(minY)
                    );

                    writer.WriteLine(
                        indent +
                        "  MaxY: " +
                        FormatFloat(maxY)
                    );

                    writer.WriteLine();

                    writer.WriteLine(
                        indent +
                        "Vertices usados:"
                    );

                    int counter = 0;

                    foreach (
                        int vertexIndex
                        in uniqueVertices
                    )
                    {
                        writer.WriteLine(
                            indent +
                            "  Vertex[" +
                            vertexIndex +
                            "] = (" +
                            FormatFloat(
                                uv[vertexIndex].x
                            ) +
                            ", " +
                            FormatFloat(
                                uv[vertexIndex].y
                            ) +
                            ")"
                        );

                        counter++;

                        if (counter >= 500)
                        {
                            writer.WriteLine(
                                indent +
                                "  [LISTA LIMITADA A 500 VERTICES]"
                            );

                            break;
                        }
                    }

                    writer.WriteLine();

                    writer.WriteLine(
                        indent +
                        "Indices:"
                    );

                    int indexLimit =
                        Math.Min(
                            indices.Length,
                            1000
                        );

                    for (
                        int i = 0;
                        i < indexLimit;
                        i++
                    )
                    {
                        writer.WriteLine(
                            indent +
                            "  [" +
                            i +
                            "] = " +
                            indices[i]
                        );
                    }

                    if (
                        indices.Length >
                        indexLimit
                    )
                    {
                        writer.WriteLine(
                            indent +
                            "  [LISTA LIMITADA A 1000 INDICES]"
                        );
                    }

                    writer.WriteLine();
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine(
                    indent +
                    "[ERRO SUBMESH] " +
                    ex
                );
            }
        }

        private static void AnalyzePhysicsComponents(
            GameObject gameObject,
            string indent,
            StreamWriter writer
        )
        {
            try
            {
                Component[] components =
                    GetAllComponentsSafely(
                        gameObject
                    );

                if (
                    components == null ||
                    components.Length == 0
                )
                {
                    return;
                }

                bool headerWritten = false;

                for (
                    int i = 0;
                    i < components.Length;
                    i++
                )
                {
                    Component component =
                        components[i];

                    if (component == null)
                        continue;

                    Type type =
                        component.GetType();

                    string fullName =
                        type.FullName ?? type.Name;

                    bool isCollider =
                        fullName.IndexOf(
                            "Collider",
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0;

                    bool isRigidbody =
                        fullName.IndexOf(
                            "Rigidbody",
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0;

                    if (
                        !isCollider &&
                        !isRigidbody
                    )
                    {
                        continue;
                    }

                    if (!headerWritten)
                    {
                        writer.WriteLine();

                        writer.WriteLine(
                            indent +
                            "PHYSICS COMPONENTS"
                        );

                        writer.WriteLine(
                            indent +
                            "--------------------------------"
                        );

                        headerWritten = true;
                    }

                    writer.WriteLine(
                        indent +
                        "Component: " +
                        fullName
                    );

                    DumpComponentProperties(
                        component,
                        indent,
                        writer
                    );
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine(
                    indent +
                    "[ERRO PHYSICS] " +
                    ex.Message
                );
            }
        }

        private static void DumpComponentProperties(
            Component component,
            string indent,
            StreamWriter writer
        )
        {
            try
            {
                Type type =
                    component.GetType();

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

                    if (!property.CanRead)
                        continue;

                    string name =
                        property.Name;

                    if (
                        name != "enabled" &&
                        name != "isTrigger" &&
                        name != "isKinematic" &&
                        name != "useGravity" &&
                        name != "mass"
                    )
                    {
                        continue;
                    }

                    try
                    {
                        object value =
                            property.GetValue(
                                component,
                                null
                            );

                        writer.WriteLine(
                            indent +
                            "  " +
                            name +
                            ": " +
                            value
                        );
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }

        private static string GetRelativePath(
            Transform current,
            Transform root
        )
        {
            try
            {
                List<string> names =
                    new List<string>();

                Transform node =
                    current;

                while (node != null)
                {
                    names.Add(
                        node.name
                    );

                    if (node == root)
                        break;

                    node = node.parent;
                }

                names.Reverse();

                return string.Join(
                    "/",
                    names.ToArray()
                );
            }
            catch
            {
                return current.name;
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
    }
}