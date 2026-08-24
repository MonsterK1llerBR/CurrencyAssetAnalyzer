#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Globalization;
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
            "7.3.2";

        /*
         * ============================================================
         * CONFIGURAÇÃO DO REPOSITÓRIO
         * ============================================================
         *
         * Este é o diretório local do repositório GitHub.
         *
         * O Analyzer copiará automaticamente os relatórios gerados
         * pelo jogo para:
         *
         * Reports\MoneyPack
         *
         * dentro deste diretório.
         */
        private const string RepositoryRoot =
            @"C:\Users\natan\Documents\Mods\SupermarketSimulator\CurrencyAssetAnalyzer";

        private const string RepositoryMoneyPackDirectory =
            "Reports\\MoneyPack";

        /*
         * ============================================================
         * ESTADO DO PLUGIN
         * ============================================================
         */

        private static CurrencyAssetAnalyzer Instance;

        private Harmony HarmonyInstance;

        private static readonly HashSet<string> AnalyzedMoneyPacks =
            new HashSet<string>();

        private static string ReportDirectory;
        private static string MoneyPackDirectory;

        private static string RepositoryReportDirectory;

        /*
         * ============================================================
         * LOAD
         * ============================================================
         */

        public override void Load()
        {
            Instance = this;

            try
            {
                /*
                 * Diretório utilizado pelo Analyzer dentro do jogo.
                 */
                ReportDirectory = Path.Combine(
                    Paths.PluginPath,
                    "CurrencyAssetAnalyzer",
                    "AnalyzerV7"
                );

                MoneyPackDirectory = Path.Combine(
                    ReportDirectory,
                    "MoneyPack"
                );

                /*
                 * Diretório do repositório GitHub.
                 */
                RepositoryReportDirectory = Path.Combine(
                    RepositoryRoot,
                    RepositoryMoneyPackDirectory
                );

                /*
                 * Cria os diretórios necessários.
                 */
                Directory.CreateDirectory(
                    ReportDirectory
                );

                Directory.CreateDirectory(
                    MoneyPackDirectory
                );

                /*
                 * Tenta criar o diretório do repositório.
                 *
                 * Caso o projeto não esteja disponível neste computador,
                 * o Analyzer continua funcionando normalmente.
                 */
                try
                {
                    Directory.CreateDirectory(
                        RepositoryReportDirectory
                    );
                }
                catch (Exception repositoryException)
                {
                    Log.LogWarning(
                        "Não foi possível preparar o diretório do repositório: " +
                        repositoryException.Message
                    );
                }

                Log.LogInfo(
                    "========================================"
                );

                Log.LogInfo(
                    "Currency Asset Analyzer v7.3.2"
                );

                Log.LogInfo(
                    "========================================"
                );

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
                    "Relatórios: " +
                    ReportDirectory
                );

                Log.LogInfo(
                    "Sincronização GitHub: ATIVADA."
                );

                Log.LogInfo(
                    "Destino GitHub: " +
                    RepositoryReportDirectory
                );

                /*
                 * Verifica se o repositório existe.
                 */
                if (Directory.Exists(RepositoryRoot))
                {
                    Log.LogInfo(
                        "Repositório local encontrado."
                    );
                }
                else
                {
                    Log.LogWarning(
                        "Repositório local não encontrado: " +
                        RepositoryRoot
                    );

                    Log.LogWarning(
                        "O Analyzer continuará funcionando, mas os relatórios " +
                        "não serão sincronizados com o GitHub."
                    );
                }

                /*
                 * Aplica o patch.
                 */
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

        /*
         * ============================================================
         * PATCH SPAWN MONEY
         * ============================================================
         */

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

        /*
         * ============================================================
         * TYPE FINDER
         * ============================================================
         */

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

            for (int i = 0; i < assemblies.Length; i++)
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

        /*
         * ============================================================
         * FIND SPAWN MONEY
         * ============================================================
         */

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

                for (int i = 0; i < methods.Length; i++)
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

        /*
         * ============================================================
         * SPAWN MONEY POSTFIX
         * ============================================================
         */

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

        /*
         * ============================================================
         * MONEY PACK ANALYSIS
         * ============================================================
         */

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

        /*
         * ============================================================
         * READ VALUE
         * ============================================================
         */

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

        /*
         * ============================================================
         * READ GAMEOBJECT
         * ============================================================
         */

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

        /*
         * ============================================================
         * HIERARCHY ANALYSIS
         * ============================================================
         */

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

                /*
                 * ====================================================
                 * CRIAÇÃO DO RELATÓRIO
                 * ====================================================
                 */

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
                        "CURRENCY ASSET ANALYZER v7.3.2"
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

                /*
                 * ====================================================
                 * SINCRONIZAÇÃO AUTOMÁTICA
                 * ====================================================
                 *
                 * Depois que o relatório é salvo no diretório do jogo,
                 * copiamos automaticamente o arquivo para o repositório
                 * local do GitHub.
                 */

                SyncReportToRepository(
                    path,
                    fileName
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

        /*
         * ============================================================
         * SYNC REPORT TO GITHUB REPOSITORY
         * ============================================================
         */

        private static void SyncReportToRepository(
            string sourcePath,
            string fileName
        )
        {
            try
            {
                if (string.IsNullOrEmpty(
                    RepositoryReportDirectory
                ))
                {
                    Instance.Log.LogWarning(
                        "Diretório do repositório não configurado."
                    );

                    return;
                }

                if (!Directory.Exists(
                    RepositoryRoot
                ))
                {
                    Instance.Log.LogWarning(
                        "Repositório local não encontrado. " +
                        "Relatório não sincronizado."
                    );

                    Instance.Log.LogWarning(
                        "Esperado em: " +
                        RepositoryRoot
                    );

                    return;
                }

                if (!File.Exists(
                    sourcePath
                ))
                {
                    Instance.Log.LogWarning(
                        "Relatório de origem não encontrado: " +
                        sourcePath
                    );

                    return;
                }

                /*
                 * Garante que Reports\MoneyPack exista.
                 */
                Directory.CreateDirectory(
                    RepositoryReportDirectory
                );

                string destinationPath =
                    Path.Combine(
                        RepositoryReportDirectory,
                        fileName
                    );

                /*
                 * Sobrescreve automaticamente o relatório anterior.
                 */
                File.Copy(
                    sourcePath,
                    destinationPath,
                    true
                );

                FileInfo sourceInfo =
                    new FileInfo(
                        sourcePath
                    );

                FileInfo destinationInfo =
                    new FileInfo(
                        destinationPath
                    );

                Instance.Log.LogInfo(
                    "Relatório sincronizado com o repositório:"
                );

                Instance.Log.LogInfo(
                    "  Origem: " +
                    sourcePath
                );

                Instance.Log.LogInfo(
                    "  Destino: " +
                    destinationPath
                );

                Instance.Log.LogInfo(
                    "  Tamanho: " +
                    destinationInfo.Length +
                    " bytes"
                );
            }
            catch (Exception ex)
            {
                /*
                 * Falha na sincronização NÃO interrompe o Analyzer.
                 *
                 * O relatório original já foi salvo no diretório
                 * do jogo e continua disponível.
                 */
                Instance.Log.LogError(
                    "Erro sincronizando relatório com o repositório: " +
                    ex
                );
            }
        }

        /*
         * ============================================================
         * TRANSFORM ANALYSIS
         * ============================================================
         */

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

        /*
         * ============================================================
         * COMPONENT ANALYSIS
         * ============================================================
         */

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

        /*
         * ============================================================
         * GET COMPONENTS SAFELY
         * ============================================================
         */

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

        /*
         * ============================================================
         * RENDERER ANALYSIS
         * ============================================================
         */

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

        /*
         * ============================================================
         * TEXTURE PROPERTY
         * ============================================================
         */

        private static void AnalyzeTextureProperty(
            Material material,
            string property,
            string indent,
            StreamWriter writer
        )
        {
            try
            {
                if (!material.HasProperty(
                    property
                ))
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
            }
            catch
            {
            }
        }

        /*
         * ============================================================
         * MESH FILTER
         * ============================================================
         */

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

        /*
         * ============================================================
         * PHYSICS COMPONENTS
         * ============================================================
         */

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

        /*
         * ============================================================
         * COMPONENT PROPERTIES
         * ============================================================
         */

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

        /*
         * ============================================================
         * RELATIVE PATH
         * ============================================================
         */

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

        /*
         * ============================================================
         * SANITIZE FILE NAME
         * ============================================================
         */

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
    }
}