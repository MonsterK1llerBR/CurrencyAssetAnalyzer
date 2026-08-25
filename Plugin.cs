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
    public class CurrencyAssetAnalyzer : BasePlugin
    {
        private const string GUID =
            "br.monsterk1llerbr.supermarketsimulator.currencyassetanalyzer";

        private const string NAME =
            "Currency Asset Analyzer";

        private const string VERSION =
            "8.2.3";

        private const string RepositoryRoot =
            @"C:\Users\natan\Documents\Mods\SupermarketSimulator\CurrencyAssetAnalyzer";

        private const string RepositoryMoneyPackDirectory =
            @"C:\Users\natan\Documents\Mods\SupermarketSimulator\CurrencyAssetAnalyzer\Reports\MoneyPack";

        private const string RepositoryTextureDirectory =
            @"C:\Users\natan\Documents\Mods\SupermarketSimulator\CurrencyAssetAnalyzer\Reports\Textures";

        private static CurrencyAssetAnalyzer Instance;

        private Harmony HarmonyInstance;

        private static readonly HashSet<string> AnalyzedMoneyPacks =
            new HashSet<string>();

        private static readonly HashSet<int> ExtractedTextureInstanceIDs =
            new HashSet<int>();

        private static string ReportDirectory;

        private static string MoneyPackDirectory;

        private static string TextureDirectory;

        private static string TextureExtractionReport;

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
                ReportDirectory = Path.Combine(
                    Paths.PluginPath,
                    "CurrencyAssetAnalyzer",
                    "AnalyzerV8"
                );

                MoneyPackDirectory = Path.Combine(
                    ReportDirectory,
                    "MoneyPack"
                );

                TextureDirectory = Path.Combine(
                    ReportDirectory,
                    "Textures"
                );

                TextureExtractionReport = Path.Combine(
                    TextureDirectory,
                    "TextureExtraction.txt"
                );

                Directory.CreateDirectory(
                    ReportDirectory
                );

                Directory.CreateDirectory(
                    MoneyPackDirectory
                );

                Directory.CreateDirectory(
                    TextureDirectory
                );

                InitializeTextureExtractionReport();

                Log.LogInfo(
                    "========================================"
                );

                Log.LogInfo(
                    "Currency Asset Analyzer v8.2.3"
                );

                Log.LogInfo(
                    "========================================"
                );

                Log.LogInfo(
                    "Modo: engenharia reversa exclusiva de MoneyPack."
                );

                Log.LogInfo(
                    "Objetivo: identificar e extrair texturas reais."
                );

                Log.LogInfo(
                    "Scan global de GameObjects: DESATIVADO."
                );

                Log.LogInfo(
                    "Extracao via Graphics.Blit: ATIVADA."
                );

                Log.LogInfo(
                    "Codificacao PNG interna: ATIVADA."
                );

                Log.LogInfo(
                    "Texture2D obrigatoria como origem: DESATIVADA."
                );

                Log.LogInfo(
                    "Sincronizacao do repositorio: ATIVADA."
                );

                Log.LogInfo(
                    "Relatorios: " +
                    ReportDirectory
                );

                Log.LogInfo(
                    "Texturas: " +
                    TextureDirectory
                );

                Log.LogInfo(
                    "Destino: " +
                    RepositoryRoot
                );

                PatchSpawnMoney();
            }
            catch (Exception ex)
            {
                Log.LogError(
                    "Erro durante inicializacao: " +
                    ex
                );
            }
        }

        private static void InitializeTextureExtractionReport()
        {
            try
            {
                using (
                    StreamWriter writer =
                        new StreamWriter(
                            TextureExtractionReport,
                            false
                        )
                )
                {
                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "CURRENCY ASSET ANALYZER - TEXTURE EXTRACTION"
                    );

                    writer.WriteLine(
                        "VERSION: " +
                        VERSION
                    );

                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "A textura de origem e tratada como UnityEngine.Texture."
                    );

                    writer.WriteLine(
                        "Nenhuma textura, material, mesh ou UV do jogo e alterado."
                    );

                    writer.WriteLine();
                }
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

                if (managerType == null)
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

                if (spawnMoney == null)
                {
                    Log.LogError(
                        "SpawnMoney(MoneyPack, bool) nao encontrado."
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
                        "SpawnMoneyPostfix nao localizado."
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
                        "MoneyPack encontrado, mas o GameObject nao pode " +
                        "ser obtido. Value=" +
                        value
                    );

                    return;
                }

                string key =
                    (isCoin ? "COIN" : "BILL") +
                    "|" +
                    gameObject.name +
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
                    gameObject.name +
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
                        "CURRENCY ASSET ANALYZER v8.2.3"
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
                        "FIM DO RELATORIO"
                    );

                    writer.WriteLine(
                        "========================================"
                    );
                }

                Instance.Log.LogInfo(
                    "Relatorio MoneyPack salvo: " +
                    path
                );

                SyncReportToRepository(
                    path,
                    RepositoryMoneyPackDirectory
                );
            }
            catch (Exception ex)
            {
                Instance.Log.LogError(
                    "Erro criando relatorio: " +
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

                        writer.WriteLine();

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
                            writer,
                            gameObject
                        );

                        AnalyzeTextureProperty(
                            material,
                            "_MainTex",
                            indent,
                            writer,
                            gameObject
                        );

                        AnalyzeTextureProperty(
                            material,
                            "_BumpMap",
                            indent,
                            writer,
                            gameObject
                        );

                        AnalyzeTextureProperty(
                            material,
                            "_MetallicGlossMap",
                            indent,
                            writer,
                            gameObject
                        );

                        AnalyzeTextureProperty(
                            material,
                            "_SpecGlossMap",
                            indent,
                            writer,
                            gameObject
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

        private static void AnalyzeTextureProperty(
            Material material,
            string property,
            string indent,
            StreamWriter writer,
            GameObject owner
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

                writer.WriteLine(
                    indent +
                    "TEXTURE"
                );

                writer.WriteLine(
                    indent +
                    "Property: " +
                    property
                );

                if (texture == null)
                {
                    writer.WriteLine(
                        indent +
                        "Texture: null"
                    );

                    return;
                }

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

                writer.WriteLine(
                    indent +
                    "InstanceID: " +
                    texture.GetInstanceID()
                );

                AnalyzeTextureDetails(
                    texture,
                    indent,
                    writer
                );

                if (
                    IsInterestingTexture(
                        texture
                    )
                )
                {
                    LogInterestingTexture(
                        texture,
                        material,
                        property,
                        owner
                    );

                    ExtractTextureIfNeeded(
                        texture,
                        material,
                        property,
                        owner
                    );
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine(
                    indent +
                    "[ERRO TEXTURE] " +
                    ex
                );
            }
        }

        private static bool IsInterestingTexture(
            Texture texture
        )
        {
            if (texture == null)
                return false;

            string name =
                texture.name ??
                string.Empty;

            if (
                name.IndexOf(
                    "T_Money_AlbedoTransparency",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
            )
            {
                return true;
            }

            if (
                name.IndexOf(
                    "T_Money_Normal",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
            )
            {
                return true;
            }

            if (
                name.IndexOf(
                    "T_Money_SpecularSmoothness",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
            )
            {
                return true;
            }

            if (
                name.IndexOf(
                    "Paper",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
            )
            {
                return true;
            }

            if (
                name.IndexOf(
                    "Dollar",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
            )
            {
                return true;
            }

            if (
                name.IndexOf(
                    "USD",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
            )
            {
                return true;
            }

            return false;
        }

        private static void LogInterestingTexture(
            Texture texture,
            Material material,
            string property,
            GameObject owner
        )
        {
            try
            {
                Instance.Log.LogInfo(
                    "TEXTURA ALVO ENCONTRADA: " +
                    texture.name +
                    " | Type=" +
                    texture.GetType().FullName +
                    " | InstanceID=" +
                    texture.GetInstanceID() +
                    " | Material=" +
                    material.name +
                    " | Property=" +
                    property +
                    " | GameObject=" +
                    owner.name
                );
            }
            catch
            {
            }
        }

        private static void ExtractTextureIfNeeded(
            Texture source,
            Material material,
            string property,
            GameObject owner
        )
        {
            try
            {
                int instanceID =
                    source.GetInstanceID();

                if (
                    !ExtractedTextureInstanceIDs.Add(
                        instanceID
                    )
                )
                {
                    return;
                }

                Instance.Log.LogInfo(
                    "INICIANDO EXTRACAO: " +
                    source.name +
                    " | Type=" +
                    source.GetType().FullName +
                    " | InstanceID=" +
                    instanceID
                );

                string safeName =
                    SanitizeFileName(
                        source.name
                    );

                if (
                    string.IsNullOrWhiteSpace(
                        safeName
                    )
                )
                {
                    safeName =
                        "Texture_" +
                        instanceID;
                }

                string fileName =
                    safeName +
                    "_" +
                    instanceID +
                    ".png";

                string outputPath =
                    Path.Combine(
                        TextureDirectory,
                        fileName
                    );

                bool success =
                    TryExtractTextureToPNG(
                        source,
                        outputPath
                    );

                AppendTextureExtractionReport(
                    source,
                    material,
                    property,
                    owner,
                    outputPath,
                    success
                );

                if (success)
                {
                    Instance.Log.LogInfo(
                        "TEXTURA EXTRAIDA COM SUCESSO: " +
                        outputPath
                    );

                    SyncReportToRepository(
                        outputPath,
                        RepositoryTextureDirectory
                    );

                    SyncReportToRepository(
                        TextureExtractionReport,
                        RepositoryTextureDirectory
                    );
                }
                else
                {
                    Instance.Log.LogWarning(
                        "EXTRACAO FALHOU: " +
                        source.name +
                        " | InstanceID=" +
                        instanceID
                    );

                    SyncReportToRepository(
                        TextureExtractionReport,
                        RepositoryTextureDirectory
                    );
                }
            }
            catch (Exception ex)
            {
                Instance.Log.LogError(
                    "Erro extraindo textura: " +
                    ex
                );
            }
        }

        private static bool TryExtractTextureToPNG(
            Texture source,
            string outputPath
        )
        {
            RenderTexture temporary =
                null;

            RenderTexture previousActive =
                RenderTexture.active;

            Texture2D readable =
                null;

            try
            {
                if (source == null)
                    return false;

                int width =
                    source.width;

                int height =
                    source.height;

                if (
                    width <= 0 ||
                    height <= 0
                )
                {
                    Instance.Log.LogError(
                        "Dimensoes invalidas: " +
                        width +
                        "x" +
                        height
                    );

                    return false;
                }

                Instance.Log.LogInfo(
                    "Criando RenderTexture: " +
                    width +
                    "x" +
                    height
                );

                temporary =
                    new RenderTexture(
                        width,
                        height,
                        0,
                        RenderTextureFormat.ARGB32
                    );

                temporary.filterMode =
                    FilterMode.Point;

                temporary.wrapMode =
                    TextureWrapMode.Clamp;

                temporary.Create();

                if (
                    !temporary.IsCreated()
                )
                {
                    Instance.Log.LogError(
                        "RenderTexture nao foi criada."
                    );

                    return false;
                }

                Graphics.Blit(
                    source,
                    temporary
                );

                RenderTexture.active =
                    temporary;

                readable =
                    new Texture2D(
                        width,
                        height,
                        TextureFormat.RGBA32,
                        false,
                        false
                    );

                readable.ReadPixels(
                    new Rect(
                        0,
                        0,
                        width,
                        height
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

                if (
                    pixels == null ||
                    pixels.Length !=
                    width * height
                )
                {
                    Instance.Log.LogError(
                        "Quantidade de pixels inesperada: " +
                        (
                            pixels == null
                                ? -1
                                : pixels.Length
                        )
                    );

                    return false;
                }

                Instance.Log.LogInfo(
                    "Pixels capturados: " +
                    pixels.Length
                );

                byte[] png =
                    EncodePixelsToPng(
                        pixels,
                        width,
                        height
                    );

                if (
                    png == null ||
                    png.Length == 0
                )
                {
                    Instance.Log.LogError(
                        "PNG vazio."
                    );

                    return false;
                }

                string directory =
                    Path.GetDirectoryName(
                        outputPath
                    );

                if (
                    !string.IsNullOrEmpty(
                        directory
                    )
                )
                {
                    Directory.CreateDirectory(
                        directory
                    );
                }

                File.WriteAllBytes(
                    outputPath,
                    png
                );

                Instance.Log.LogInfo(
                    "PNG gravado: " +
                    outputPath +
                    " | Bytes=" +
                    png.Length
                );

                return File.Exists(
                    outputPath
                );
            }
            catch (Exception ex)
            {
                if (Instance != null)
                {
                    Instance.Log.LogError(
                        "Falha em TryExtractTextureToPNG: " +
                        ex
                    );
                }

                return false;
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
                    if (temporary != null)
                    {
                        temporary.Release();

                        UnityEngine.Object.Destroy(
                            temporary
                        );
                    }
                }
                catch
                {
                }
            }
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
                width * height
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

                ihdr[8] = 8;
                ihdr[9] = 6;
                ihdr[10] = 0;
                ihdr[11] = 0;
                ihdr[12] = 0;

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
                            y * width;

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

                    byte[] rawBytes =
                        raw.ToArray();

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
                    (current & 1u) != 0u
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

        private static void AppendTextureExtractionReport(
            Texture texture,
            Material material,
            string property,
            GameObject owner,
            string outputPath,
            bool success
        )
        {
            try
            {
                using (
                    StreamWriter writer =
                        new StreamWriter(
                            TextureExtractionReport,
                            true
                        )
                )
                {
                    writer.WriteLine(
                        "----------------------------------------"
                    );

                    writer.WriteLine(
                        "Texture: " +
                        (
                            texture != null
                                ? texture.name
                                : "null"
                        )
                    );

                    if (texture != null)
                    {
                        writer.WriteLine(
                            "Type: " +
                            texture.GetType().FullName
                        );

                        writer.WriteLine(
                            "InstanceID: " +
                            texture.GetInstanceID()
                        );

                        writer.WriteLine(
                            "Width: " +
                            texture.width
                        );

                        writer.WriteLine(
                            "Height: " +
                            texture.height
                        );

                        writer.WriteLine(
                            "FilterMode: " +
                            texture.filterMode
                        );

                        writer.WriteLine(
                            "WrapMode: " +
                            texture.wrapMode
                        );

                        writer.WriteLine(
                            "AnisoLevel: " +
                            texture.anisoLevel
                        );

                        Texture2D texture2D =
                            texture as Texture2D;

                        if (texture2D != null)
                        {
                            writer.WriteLine(
                                "Format: " +
                                texture2D.format
                            );

                            writer.WriteLine(
                                "MipmapCount: " +
                                texture2D.mipmapCount
                            );

                            writer.WriteLine(
                                "IsReadable: " +
                                texture2D.isReadable
                            );
                        }
                        else
                        {
                            writer.WriteLine(
                                "Format: N/A"
                            );

                            writer.WriteLine(
                                "MipmapCount: N/A"
                            );

                            writer.WriteLine(
                                "IsReadable: N/A"
                            );
                        }
                    }

                    writer.WriteLine(
                        "Material: " +
                        (
                            material != null
                                ? material.name
                                : "null"
                        )
                    );

                    writer.WriteLine(
                        "MaterialInstanceID: " +
                        (
                            material != null
                                ? material.GetInstanceID().ToString()
                                : "null"
                        )
                    );

                    writer.WriteLine(
                        "Property: " +
                        property
                    );

                    writer.WriteLine(
                        "GameObject: " +
                        (
                            owner != null
                                ? owner.name
                                : "null"
                        )
                    );

                    writer.WriteLine(
                        "Output: " +
                        outputPath
                    );

                    writer.WriteLine(
                        "Success: " +
                        success
                    );

                    writer.WriteLine();
                }
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
                if (
                    material.HasProperty(
                        "_BaseMap"
                    )
                )
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

                try
                {
                    mesh.GetUVs(
                        0,
                        uvList
                    );
                }
                catch (Exception ex)
                {
                    writer.WriteLine(
                        indent +
                        "GetUVs(0) falhou: " +
                        ex.Message
                    );

                    writer.WriteLine(
                        indent +
                        "UV0 Count: 0"
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

                if (
                    uvCount == 0
                )
                {
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

                int[] triangles =
                    null;

                try
                {
                    triangles =
                        mesh.triangles;
                }
                catch
                {
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

                writer.WriteLine(
                    indent +
                    "Triangle Count: " +
                    (
                        triangles.Length /
                        3
                    )
                );
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

        private static void SyncReportToRepository(
            string sourceFile,
            string destinationDirectory
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

                if (
                    string.IsNullOrWhiteSpace(
                        destinationDirectory
                    )
                )
                {
                    return;
                }

                if (
                    !Directory.Exists(
                        RepositoryRoot
                    )
                )
                {
                    Instance.Log.LogWarning(
                        "Repositorio local nao encontrado: " +
                        RepositoryRoot
                    );

                    return;
                }

                Directory.CreateDirectory(
                    destinationDirectory
                );

                string destinationFile =
                    Path.Combine(
                        destinationDirectory,
                        Path.GetFileName(
                            sourceFile
                        )
                    );

                File.Copy(
                    sourceFile,
                    destinationFile,
                    true
                );

                Instance.Log.LogInfo(
                    "Arquivo sincronizado: " +
                    destinationFile
                );
            }
            catch (Exception ex)
            {
                Instance.Log.LogError(
                    "Erro sincronizando arquivo: " +
                    ex
                );
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

                    node =
                        node.parent;
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