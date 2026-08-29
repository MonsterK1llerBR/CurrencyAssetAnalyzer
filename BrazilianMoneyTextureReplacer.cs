#nullable disable

using System;
using System.Collections.Generic;
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
    public class BrazilianMoneyTextureReplacer : BasePlugin
    {
        private const string GUID =
            "br.monsterk1llerbr.supermarketsimulator.brazilianmoneytexturereplacer";

        private const string NAME =
            "Brazilian Money Texture Replacer";

        private const string VERSION =
            "1.0.0";

        private const string RootFolder =
            "CurrencyAssetAnalyzer";

        private const string AtlasFolder =
            "BrazilianCoinUVComposer";

        private const string AtlasFileName =
            "BrazilianCoinAtlas_FINAL.png";

        private const string OutputFolder =
            "BrazilianMoneyTextureReplacer";

        private const string ReportFileName =
            "BrazilianMoneyTextureReplacerReport.txt";

        private static BrazilianMoneyTextureReplacer Instance;

        private Harmony HarmonyInstance;

        private Texture2D BrazilianAtlas;

        private string AtlasPath;

        private bool AtlasLoadAttempted;

        private readonly HashSet<int> ReplacedGameObjects =
            new HashSet<int>();

        private readonly HashSet<int> ReplacedMaterials =
            new HashSet<int>();

        private readonly HashSet<string> LoggedObjects =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

        private static readonly string[] CoinNameMarkers =
        {
            "SM_Coin_50_Cents",
            "SM_Coin_25_Cents",
            "SM_Coin_10_Cents",
            "SM_Coin_5_Cents",
            "SM_Coin_1_Cent"
        };

        private static readonly string[] TextureProperties =
        {
            "_BaseMap",
            "_MainTex"
        };

        public override void Load()
        {
            Instance =
                this;

            Log.LogInfo(
                "========================================"
            );

            Log.LogInfo(
                "Brazilian Money Texture Replacer v" +
                VERSION
            );

            Log.LogInfo(
                "========================================"
            );

            Log.LogInfo(
                "Objetivo: substituir o albedo das moedas em runtime."
            );

            Log.LogInfo(
                "Mesh: NAO ALTERADO."
            );

            Log.LogInfo(
                "UV: NAO ALTERADO."
            );

            Log.LogInfo(
                "M_Money original: NAO ALTERADO."
            );

            Log.LogInfo(
                "Normal: NAO ALTERADO."
            );

            Log.LogInfo(
                "Specular: NAO ALTERADO."
            );

            Log.LogInfo(
                "Atlas original: NAO ALTERADO."
            );

            InitializeReport();

            ResolveAtlasPath();

            PatchSpawnMoney();
        }

        private string GetPluginRoot()
        {
            return Path.Combine(
                Paths.PluginPath,
                RootFolder
            );
        }

        private string GetOutputDirectory()
        {
            return Path.Combine(
                GetPluginRoot(),
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

        private void InitializeReport()
        {
            try
            {
                Directory.CreateDirectory(
                    GetOutputDirectory()
                );

                using (
                    StreamWriter writer =
                        new StreamWriter(
                            GetReportPath(),
                            false
                        )
                )
                {
                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "BRAZILIAN MONEY TEXTURE REPLACER"
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
                        "Metodo:"
                    );

                    writer.WriteLine(
                        "SpawnMoney -> GameObject -> Renderer -> Material Instance"
                    );

                    writer.WriteLine(
                        "-> _BaseMap/_MainTex -> BrazilianCoinAtlas_FINAL.png"
                    );

                    writer.WriteLine();

                    writer.WriteLine(
                        "Mesh: NAO ALTERADO."
                    );

                    writer.WriteLine(
                        "UV: NAO ALTERADO."
                    );

                    writer.WriteLine(
                        "Material original: NAO ALTERADO."
                    );

                    writer.WriteLine(
                        "Textura original: NAO ALTERADA."
                    );

                    writer.WriteLine();
                }
            }
            catch (
                Exception ex
            )
            {
                Log.LogError(
                    "Erro criando relatorio:"
                );

                Log.LogError(
                    ex
                );
            }
        }

        private void ResolveAtlasPath()
        {
            try
            {
                string expected =
                    Path.Combine(
                        GetPluginRoot(),
                        AtlasFolder,
                        AtlasFileName
                    );

                AtlasPath =
                    expected;

                Log.LogInfo(
                    "Atlas alvo:"
                );

                Log.LogInfo(
                    AtlasPath
                );

                AppendLine();

                AppendLine(
                    "ATLAS PATH"
                );

                AppendLine(
                    "--------------------------------"
                );

                AppendLine(
                    AtlasPath
                );

                if (
                    File.Exists(
                        AtlasPath
                    )
                )
                {
                    Log.LogInfo(
                        "BrazilianCoinAtlas_FINAL.png encontrado."
                    );

                    AppendLine(
                        "Atlas encontrado: TRUE"
                    );
                }
                else
                {
                    Log.LogWarning(
                        "BrazilianCoinAtlas_FINAL.png ainda nao existe."
                    );

                    AppendLine(
                        "Atlas encontrado: FALSE"
                    );

                    AppendLine(
                        "O substituidor aguardara o arquivo."
                    );
                }
            }
            catch (
                Exception ex
            )
            {
                Log.LogError(
                    "Erro resolvendo caminho do atlas:"
                );

                Log.LogError(
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

                if (
                    managerType == null
                )
                {
                    Log.LogError(
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
                    Log.LogError(
                        "SpawnMoney nao encontrado."
                    );

                    return;
                }

                Log.LogInfo(
                    "SpawnMoney encontrado:"
                );

                Log.LogInfo(
                    spawnMoney.ToString()
                );

                HarmonyInstance =
                    new Harmony(
                        GUID
                    );

                MethodInfo postfix =
                    AccessTools.Method(
                        typeof(
                            BrazilianMoneyTextureReplacer
                        ),
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
            catch (
                Exception ex
            )
            {
                Log.LogError(
                    "Erro aplicando patch de SpawnMoney:"
                );

                Log.LogError(
                    ex
                );
            }
        }

        private Type FindType(
            string name
        )
        {
            Assembly[] assemblies;

            try
            {
                assemblies =
                    AppDomain.CurrentDomain.GetAssemblies();
            }
            catch
            {
                return null;
            }

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

                Type[] types;

                try
                {
                    types =
                        assembly.GetTypes();
                }
                catch
                {
                    continue;
                }

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

            return null;
        }

        private MethodInfo FindSpawnMoneyMethod(
            Type managerType
        )
        {
            MethodInfo[] methods;

            try
            {
                methods =
                    managerType.GetMethods(
                        BindingFlags.Instance |
                        BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );
            }
            catch
            {
                return null;
            }

            MethodInfo fallback =
                null;

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

                ParameterInfo[] parameters;

                try
                {
                    parameters =
                        method.GetParameters();
                }
                catch
                {
                    continue;
                }

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

                if (
                    parameters[0].ParameterType.Name ==
                    "MoneyPack"
                )
                {
                    return method;
                }

                if (
                    fallback == null
                )
                {
                    fallback =
                        method;
                }
            }

            return fallback;
        }

        private static void SpawnMoneyPostfix(
            object __0,
            bool __1
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

                if (
                    __0 == null
                )
                {
                    return;
                }

                GameObject gameObject =
                    Instance.ResolveGameObject(
                        __0
                    );

                if (
                    gameObject == null
                )
                {
                    Instance.Log.LogWarning(
                        "Nao foi possivel resolver GameObject do MoneyPack."
                    );

                    return;
                }

                if (
                    !Instance.IsTargetCoin(
                        gameObject
                    )
                )
                {
                    return;
                }

                Instance.ReplaceCoinTexture(
                    gameObject,
                    __1
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
                        "Erro no substituidor de textura:"
                    );

                    Instance.Log.LogError(
                        ex
                    );
                }
            }
        }

        private GameObject ResolveGameObject(
            object moneyPack
        )
        {
            if (
                moneyPack == null
            )
            {
                return null;
            }

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

            Type type =
                moneyPack.GetType();

            PropertyInfo[] properties;

            try
            {
                properties =
                    type.GetProperties(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );
            }
            catch
            {
                properties =
                    Array.Empty<PropertyInfo>();
            }

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

                GameObject result =
                    ResolveMemberValue(
                        value
                    );

                if (
                    result != null
                )
                {
                    return result;
                }
            }

            FieldInfo[] fields;

            try
            {
                fields =
                    type.GetFields(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );
            }
            catch
            {
                fields =
                    Array.Empty<FieldInfo>();
            }

            for (
                int i = 0;
                i < fields.Length;
                i++
            )
            {
                FieldInfo field =
                    fields[i];

                if (
                    field == null
                )
                {
                    continue;
                }

                object value;

                try
                {
                    value =
                        field.GetValue(
                            moneyPack
                        );
                }
                catch
                {
                    continue;
                }

                GameObject result =
                    ResolveMemberValue(
                        value
                    );

                if (
                    result != null
                )
                {
                    return result;
                }
            }

            return null;
        }

        private GameObject ResolveMemberValue(
            object value
        )
        {
            if (
                value == null
            )
            {
                return null;
            }

            try
            {
                GameObject gameObject =
                    value as GameObject;

                if (
                    gameObject != null
                )
                {
                    return gameObject;
                }
            }
            catch
            {
            }

            try
            {
                Component component =
                    value as Component;

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

            return null;
        }

        private bool IsTargetCoin(
    GameObject root
)
        {
            if (
                root == null
            )
            {
                return false;
            }

            try
            {
                string rootName =
                    root.name ??
                    string.Empty;

                for (
                    int i = 0;
                    i < CoinNameMarkers.Length;
                    i++
                )
                {
                    if (
                        rootName.IndexOf(
                            CoinNameMarkers[i],
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0
                    )
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }

            try
            {
                Transform rootTransform =
                    root.transform;

                if (
                    rootTransform == null
                )
                {
                    return false;
                }

                /*
                 * IMPORTANTE:
                 *
                 * A moeda real nao esta necessariamente no root
                 * nem no primeiro nivel de filhos.
                 *
                 * Exemplo descoberto pelo MoneyMaterialProbe:
                 *
                 * 1 Cent Pack
                 *   -> Visuals
                 *      -> 1 Cent Pack
                 *         -> Coin_1_Cent
                 *            -> SM_Coin_1_Cent
                 *
                 * Portanto procuramos recursivamente toda a hierarquia.
                 */
                Transform[] allTransforms =
                    root.GetComponentsInChildren<Transform>(
                        true
                    );

                if (
                    allTransforms == null
                )
                {
                    return false;
                }

                for (
                    int i = 0;
                    i < allTransforms.Length;
                    i++
                )
                {
                    Transform transform =
                        allTransforms[i];

                    if (
                        transform == null
                    )
                    {
                        continue;
                    }

                    string objectName =
                        transform.name ??
                        string.Empty;

                    for (
                        int c = 0;
                        c < CoinNameMarkers.Length;
                        c++
                    )
                    {
                        if (
                            objectName.IndexOf(
                                CoinNameMarkers[c],
                                StringComparison.OrdinalIgnoreCase
                            ) >= 0
                        )
                        {
                            return true;
                        }
                    }
                }
            }
            catch (
                Exception ex
            )
            {
                Log.LogWarning(
                    "Erro procurando moeda na hierarquia: " +
                    ex
                );
            }

            return false;
        }

        private bool EnsureAtlasLoaded()
        {
            if (
                BrazilianAtlas != null
            )
            {
                return true;
            }

            if (
                AtlasLoadAttempted
            )
            {
                return false;
            }

            AtlasLoadAttempted =
                true;

            try
            {
                if (
                    string.IsNullOrEmpty(
                        AtlasPath
                    )
                )
                {
                    ResolveAtlasPath();
                }

                if (
                    string.IsNullOrEmpty(
                        AtlasPath
                    )
                )
                {
                    Log.LogError(
                        "Caminho do atlas nao definido."
                    );

                    return false;
                }

                if (
                    !File.Exists(
                        AtlasPath
                    )
                )
                {
                    Log.LogError(
                        "Atlas nao encontrado:"
                    );

                    Log.LogError(
                        AtlasPath
                    );

                    AppendLine(
                        "ERRO: Atlas nao encontrado."
                    );

                    return false;
                }

                byte[] data =
                    File.ReadAllBytes(
                        AtlasPath
                    );

                Log.LogInfo(
                    "Carregando BrazilianCoinAtlas_FINAL.png"
                );

                Log.LogInfo(
                    "Bytes: " +
                    data.Length
                );

                Texture2D texture =
                    new Texture2D(
                        2,
                        2,
                        TextureFormat.RGBA32,
                        false,
                        false
                    );

                bool loaded =
                    ImageConversion.LoadImage(
                        texture,
                        data,
                        true
                    );

                if (
                    !loaded
                )
                {
                    Log.LogError(
                        "ImageConversion.LoadImage retornou FALSE."
                    );

                    UnityEngine.Object.Destroy(
                        texture
                    );

                    return false;
                }

                texture.name =
                    "BrazilianCoinAtlas_RUNTIME";

                texture.wrapMode =
                    TextureWrapMode.Clamp;

                texture.filterMode =
                    FilterMode.Bilinear;

                texture.anisoLevel =
                    1;

                BrazilianAtlas =
                    texture;

                Log.LogInfo(
                    "Atlas carregado com sucesso."
                );

                Log.LogInfo(
                    "Dimensions: " +
                    texture.width +
                    "x" +
                    texture.height
                );

                AppendLine();

                AppendLine(
                    "ATLAS CARREGADO"
                );

                AppendLine(
                    "Texture Name: " +
                    texture.name
                );

                AppendLine(
                    "Dimensions: " +
                    texture.width +
                    "x" +
                    texture.height
                );

                AppendLine(
                    "Source: " +
                    AtlasPath
                );

                return true;
            }
            catch (
                Exception ex
            )
            {
                Log.LogError(
                    "Erro carregando atlas:"
                );

                Log.LogError(
                    ex
                );

                AppendLine(
                    "ERRO CARREGANDO ATLAS: " +
                    ex
                );

                return false;
            }
        }

        private void ReplaceCoinTexture(
            GameObject root,
            bool isCoin
        )
        {
            if (
                root == null
            )
            {
                return;
            }

            if (
                !isCoin
            )
            {
                return;
            }

            if (
                !EnsureAtlasLoaded()
            )
            {
                return;
            }

            int rootId =
                root.GetInstanceID();

            if (
                ReplacedGameObjects.Contains(
                    rootId
                )
            )
            {
                return;
            }

            Renderer[] renderers;

            try
            {
                renderers =
                    root.GetComponentsInChildren<Renderer>(
                        true
                    );
            }
            catch (
                Exception ex
            )
            {
                Log.LogError(
                    "Erro procurando Renderers:"
                );

                Log.LogError(
                    ex
                );

                return;
            }

            if (
                renderers == null ||
                renderers.Length == 0
            )
            {
                Log.LogWarning(
                    "Nenhum Renderer encontrado em " +
                    root.name
                );

                return;
            }

            bool replaced =
                false;

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

                Material material =
                    null;

                try
                {
                    /*
                     * IMPORTANTE:
                     *
                     * renderer.material cria/fornece uma instancia
                     * do material para aquele Renderer.
                     *
                     * Assim evitamos modificar o asset compartilhado
                     * M_Money.
                     */
                    material =
                        renderer.material;
                }
                catch (
                    Exception ex
                )
                {
                    Log.LogWarning(
                        "Nao foi possivel obter material de " +
                        renderer.gameObject.name
                    );

                    Log.LogWarning(
                        ex
                    );

                    continue;
                }

                if (
                    material == null
                )
                {
                    continue;
                }

                int materialId =
                    material.GetInstanceID();

                string materialName =
                    material.name ??
                    string.Empty;

                bool interesting =
                    materialName.IndexOf(
                        "M_Money",
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0;

                bool propertyChanged =
                    false;

                for (
                    int p = 0;
                    p < TextureProperties.Length;
                    p++
                )
                {
                    string property =
                        TextureProperties[p];

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

                        Texture current =
                            material.GetTexture(
                                property
                            );

                        string currentName =
                            current != null
                                ? current.name
                                : "null";

                        /*
                         * Evita trocar materiais que nao aparentam
                         * pertencer ao sistema de dinheiro.
                         */
                        bool isMoneyTexture =
                            current != null &&
                            currentName.IndexOf(
                                "T_Money",
                                StringComparison.OrdinalIgnoreCase
                            ) >= 0;

                        if (
                            !interesting &&
                            !isMoneyTexture
                        )
                        {
                            continue;
                        }

                        material.SetTexture(
                            property,
                            BrazilianAtlas
                        );

                        propertyChanged =
                            true;

                        ReplacedMaterials.Add(
                            materialId
                        );

                        if (
                            LoggedObjects.Add(
                                root.name +
                                "|" +
                                renderer.gameObject.name +
                                "|" +
                                materialId +
                                "|" +
                                property
                            )
                        )
                        {
                            Log.LogInfo(
                                "########################################"
                            );

                            Log.LogInfo(
                                "TEXTURA SUBSTITUIDA"
                            );

                            Log.LogInfo(
                                "Coin Root: " +
                                root.name
                            );

                            Log.LogInfo(
                                "Renderer: " +
                                renderer.gameObject.name
                            );

                            Log.LogInfo(
                                "Material: " +
                                materialName
                            );

                            Log.LogInfo(
                                "Material InstanceID: " +
                                materialId
                            );

                            Log.LogInfo(
                                "Property: " +
                                property
                            );

                            Log.LogInfo(
                                "Texture anterior: " +
                                currentName
                            );

                            Log.LogInfo(
                                "Texture nova: " +
                                BrazilianAtlas.name
                            );

                            Log.LogInfo(
                                "########################################"
                            );
                        }

                        AppendLine();

                        AppendLine(
                            "TEXTURE REPLACEMENT"
                        );

                        AppendLine(
                            "Coin Root: " +
                            root.name
                        );

                        AppendLine(
                            "Renderer: " +
                            renderer.gameObject.name
                        );

                        AppendLine(
                            "Material: " +
                            materialName
                        );

                        AppendLine(
                            "Material InstanceID: " +
                            materialId
                        );

                        AppendLine(
                            "Property: " +
                            property
                        );

                        AppendLine(
                            "Previous Texture: " +
                            currentName
                        );

                        AppendLine(
                            "New Texture: " +
                            BrazilianAtlas.name
                        );

                        propertyChanged =
                            true;
                    }
                    catch (
                        Exception ex
                    )
                    {
                        Log.LogWarning(
                            "Erro definindo " +
                            property +
                            " em " +
                            materialName
                        );

                        Log.LogWarning(
                            ex
                        );
                    }
                }

                if (
                    propertyChanged
                )
                {
                    replaced =
                        true;
                }
            }

            if (
                replaced
            )
            {
                ReplacedGameObjects.Add(
                    rootId
                );

                AppendLine();

                AppendLine(
                    "COIN REPLACED: " +
                    root.name
                );

                AppendLine(
                    "Root InstanceID: " +
                    rootId
                );

                AppendLine(
                    "Renderer Count: " +
                    renderers.Length
                );
            }
        }

        private void AppendLine(
            string line = ""
        )
        {
            try
            {
                Directory.CreateDirectory(
                    GetOutputDirectory()
                );

                File.AppendAllText(
                    GetReportPath(),
                    line +
                    Environment.NewLine
                );
            }
            catch
            {
            }
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

            try
            {
                if (
                    BrazilianAtlas != null
                )
                {
                    UnityEngine.Object.Destroy(
                        BrazilianAtlas
                    );

                    BrazilianAtlas =
                        null;
                }
            }
            catch
            {
            }

            return true;
        }
    }
}