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
            "8.1.1";

        private const string RepositoryMoneyPackDirectory =
            @"C:\Users\natan\Documents\Mods\SupermarketSimulator\CurrencyAssetAnalyzer\Reports\MoneyPack";

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
                    "AnalyzerV8"
                );

                MoneyPackDirectory = Path.Combine(
                    ReportDirectory,
                    "MoneyPack"
                );

                Directory.CreateDirectory(
                    ReportDirectory
                );

                Directory.CreateDirectory(
                    MoneyPackDirectory
                );

                Log.LogInfo(
                    "========================================"
                );

                Log.LogInfo(
                    "Currency Asset Analyzer v8.1.1"
                );

                Log.LogInfo(
                    "========================================"
                );

                Log.LogInfo(
                    "Modo: engenharia reversa exclusiva de MoneyPack."
                );

                Log.LogInfo(
                    "Objetivo: Mesh + Material + Texture + UV."
                );

                Log.LogInfo(
                    "Scan global de GameObjects: DESATIVADO."
                );

                Log.LogInfo(
                    "Leitura UV via Mesh.GetUVs: ATIVADA."
                );

                Log.LogInfo(
                    "Leitura de indices via Mesh.triangles: ATIVADA."
                );

                Log.LogInfo(
                    "Sincronização do repositório: ATIVADA."
                );

                Log.LogInfo(
                    "Relatórios: " +
                    ReportDirectory
                );

                Log.LogInfo(
                    "Destino: " +
                    RepositoryMoneyPackDirectory
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
                    FindType(
                        "CheckoutChangeManager"
                    );

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
                    FindSpawnMoneyMethod(
                        managerType
                    );

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
                    new Harmony(
                        GUID
                    );

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

                if (
                    !AnalyzedMoneyPacks.Add(
                        key
                    )
                )
                {
                    return;
                }

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
                    isCoin
                        ? "COIN"
                        : "BILL";

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
                        "CURRENCY ASSET ANALYZER v8.1.1"
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

                SyncReportToRepository(
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

        private static void SyncReportToRepository(
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
                    Instance.Log.LogWarning(
                        "Arquivo de relatório não encontrado para sincronização: " +
                        sourceFile
                    );

                    return;
                }

                Directory.CreateDirectory(
                    RepositoryMoneyPackDirectory
                );

                string fileName =
                    Path.GetFileName(
                        sourceFile
                    );

                string destinationFile =
                    Path.Combine(
                        RepositoryMoneyPackDirectory,
                        fileName
                    );

                File.Copy(
                    sourceFile,
                    destinationFile,
                    true
                );

                Instance.Log.LogInfo(
                    "Relatório sincronizado com o repositório: " +
                    destinationFile
                );
            }
            catch (Exception ex)
            {
                Instance.Log.LogError(
                    "Erro sincronizando relatório com o repositório: " +
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

                        writer.WriteLine(
                            indent +
                            "InstanceID: " +
                            material.GetInstanceID()
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

                        AnalyzeMaterialValues(
                            material,
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
                    ex
                );
            }
        }

        private static void AnalyzeMaterialValues(
            Material material,
            string indent,
            StreamWriter writer
        )
        {
            try
            {
                Vector2 offset =
                    material.GetTextureOffset(
                        "_BaseMap"
                    );

                Vector2 scale =
                    material.GetTextureScale(
                        "_BaseMap"
                    );

                writer.WriteLine(
                    indent +
                    "TEXTURE TRANSFORM _BaseMap"
                );

                writer.WriteLine(
                    indent +
                    "Offset: " +
                    offset
                );

                writer.WriteLine(
                    indent +
                    "Scale: " +
                    scale
                );
            }
            catch
            {
            }

            try
            {
                Vector2 offset =
                    material.GetTextureOffset(
                        "_MainTex"
                    );

                Vector2 scale =
                    material.GetTextureScale(
                        "_MainTex"
                    );

                writer.WriteLine(
                    indent +
                    "TEXTURE TRANSFORM _MainTex"
                );

                writer.WriteLine(
                    indent +
                    "Offset: " +
                    offset
                );

                writer.WriteLine(
                    indent +
                    "Scale: " +
                    scale
                );
            }
            catch
            {
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
                if (
                    !material.HasProperty(
                        property
                    )
                )
                {
                    return;
                }

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

                AnalyzeTextureDetails(
                    texture,
                    indent,
                    writer
                );
            }
            catch
            {
            }
        }

        private static void AnalyzeTextureDetails(
            Texture texture,
            string indent,
            StreamWriter writer
        )
        {
            try
            {
                writer.WriteLine(
                    indent +
                    "TextureInstanceID: " +
                    texture.GetInstanceID()
                );

                writer.WriteLine(
                    indent +
                    "Width: " +
                    texture.width
                );

                writer.WriteLine(
                    indent +
                    "Height: " +
                    texture.height
                );

                writer.WriteLine(
                    indent +
                    "AnisoLevel: " +
                    texture.anisoLevel
                );

                writer.WriteLine(
                    indent +
                    "FilterMode: " +
                    texture.filterMode
                );

                writer.WriteLine(
                    indent +
                    "WrapMode: " +
                    texture.wrapMode
                );

                Texture2D texture2D =
                    texture as Texture2D;

                if (texture2D != null)
                {
                    writer.WriteLine(
                        indent +
                        "Texture2D_Format: " +
                        texture2D.format
                    );

                    writer.WriteLine(
                        indent +
                        "Texture2D_MipmapCount: " +
                        texture2D.mipmapCount
                    );

                    writer.WriteLine(
                        indent +
                        "Texture2D_IsReadable: " +
                        texture2D.isReadable
                    );
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine(
                    indent +
                    "[ERRO TEXTURE DETAILS] " +
                    ex.Message
                );
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
                        "Mesh InstanceID: " +
                        mesh.GetInstanceID()
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

                    writer.WriteLine(
                        indent +
                        "Bounds: " +
                        mesh.bounds
                    );

                    AnalyzeMeshUV(
                        mesh,
                        indent,
                        writer
                    );

                    AnalyzeMeshIndices(
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
                    ex
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
                    "UV0 ANALYSIS"
                );

                writer.WriteLine(
                    indent +
                    "--------------------------------"
                );

                Il2CppSystem.Collections.Generic.List<Vector2> uvList =
                    new Il2CppSystem.Collections.Generic.List<Vector2>();

                bool success = false;

                try
                {
                    mesh.GetUVs(
                        0,
                        uvList
                    );

                    success = true;
                }
                catch (Exception ex)
                {
                    writer.WriteLine(
                        indent +
                        "GetUVs(0) falhou: " +
                        ex.Message
                    );
                }

                if (!success)
                {
                    writer.WriteLine(
                        indent +
                        "UV0: NAO FOI POSSIVEL LER"
                    );

                    return;
                }

                int uvCount =
                    uvList.Count;

                writer.WriteLine(
                    indent +
                    "UV0 Count: " +
                    uvCount
                );

                if (uvCount == 0)
                {
                    writer.WriteLine(
                        indent +
                        "UV0: LISTA VAZIA"
                    );

                    return;
                }

                float minX = float.MaxValue;
                float minY = float.MaxValue;
                float maxX = float.MinValue;
                float maxY = float.MinValue;

                for (
                    int i = 0;
                    i < uvCount;
                    i++
                )
                {
                    Vector2 uv =
                        uvList[i];

                    if (uv.x < minX)
                        minX = uv.x;

                    if (uv.x > maxX)
                        maxX = uv.x;

                    if (uv.y < minY)
                        minY = uv.y;

                    if (uv.y > maxY)
                        maxY = uv.y;
                }

                writer.WriteLine(
                    indent +
                    "UV0 Min: (" +
                    FormatFloat(minX) +
                    ", " +
                    FormatFloat(minY) +
                    ")"
                );

                writer.WriteLine(
                    indent +
                    "UV0 Max: (" +
                    FormatFloat(maxX) +
                    ", " +
                    FormatFloat(maxY) +
                    ")"
                );

                writer.WriteLine(
                    indent +
                    "UV0 Range: (" +
                    FormatFloat(maxX - minX) +
                    ", " +
                    FormatFloat(maxY - minY) +
                    ")"
                );

                writer.WriteLine();

                writer.WriteLine(
                    indent +
                    "UV0 DATA"
                );

                writer.WriteLine(
                    indent +
                    "--------------------------------"
                );

                for (
                    int i = 0;
                    i < uvCount;
                    i++
                )
                {
                    Vector2 uv =
                        uvList[i];

                    writer.WriteLine(
                        indent +
                        "[" +
                        i +
                        "] = (" +
                        FormatFloat(uv.x) +
                        ", " +
                        FormatFloat(uv.y) +
                        ")"
                    );
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine(
                    indent +
                    "[ERRO UV0] " +
                    ex
                );
            }
        }

        private static void AnalyzeMeshIndices(
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
                    "MESH INDEX ANALYSIS"
                );

                writer.WriteLine(
                    indent +
                    "--------------------------------"
                );

                int[] triangles = null;

                try
                {
                    triangles =
                        mesh.triangles;
                }
                catch (Exception ex)
                {
                    writer.WriteLine(
                        indent +
                        "mesh.triangles falhou: " +
                        ex.Message
                    );
                }

                if (
                    triangles == null ||
                    triangles.Length == 0
                )
                {
                    writer.WriteLine(
                        indent +
                        "Triangles: NAO FOI POSSIVEL LER"
                    );

                    return;
                }

                writer.WriteLine(
                    indent +
                    "Total Triangle Indices: " +
                    triangles.Length
                );

                int triangleCount =
                    triangles.Length / 3;

                writer.WriteLine(
                    indent +
                    "Triangle Count: " +
                    triangleCount
                );

                int limit =
                    Math.Min(
                        triangles.Length,
                        600
                    );

                for (
                    int i = 0;
                    i + 2 < limit;
                    i += 3
                )
                {
                    writer.WriteLine(
                        indent +
                        "Triangle[" +
                        (i / 3) +
                        "] = (" +
                        triangles[i] +
                        ", " +
                        triangles[i + 1] +
                        ", " +
                        triangles[i + 2] +
                        ")"
                    );
                }

                if (
                    triangles.Length >
                    limit
                )
                {
                    writer.WriteLine(
                        indent +
                        "[TRIANGLES LIMITADOS A 200 TRIANGULOS]"
                    );
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine(
                    indent +
                    "[ERRO INDICES] " +
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
                        type.FullName ??
                        type.Name;

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
                    ex
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
