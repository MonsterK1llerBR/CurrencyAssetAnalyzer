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
            "1.1.0";

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

        private static readonly string[] InterestingNames =
        {
            "M_Money",
            "T_Money",
            "Albedo",
            "Money"
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
                    "Metodo: SpawnMoney + analise IL2CPP do objeto recebido."
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
                    "Erro durante inicializacao do probe:"
                );

                Log.LogError(
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
                    GetReportPath();

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
                        "Identificar Renderer, Material, Shader"
                    );

                    writer.WriteLine(
                        "e propriedades de textura dos MoneyPack."
                    );

                    writer.WriteLine();

                    writer.WriteLine(
                        "IMPORTANTE:"
                    );

                    writer.WriteLine(
                        "Nenhum asset do jogo sera alterado."
                    );

                    writer.WriteLine(
                        "Nenhum Material sera modificado."
                    );

                    writer.WriteLine(
                        "Nenhuma Texture sera modificada."
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
                    "Erro criando relatorio:"
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

                ParameterInfo[] parameters =
                    spawnMoney.GetParameters();

                Log.LogInfo(
                    "Parametros de SpawnMoney: " +
                    parameters.Length
                );

                for (
                    int i = 0;
                    i < parameters.Length;
                    i++
                )
                {
                    Log.LogInfo(
                        "  [" +
                        i +
                        "] " +
                        parameters[i].ParameterType.FullName +
                        " " +
                        parameters[i].Name
                    );
                }

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
                    "Erro aplicando patch:"
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
            catch (Exception ex)
            {
                Log.LogError(
                    "Erro lendo metodos de CheckoutChangeManager:"
                );

                Log.LogError(
                    ex
                );

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
                    parameters[1].ParameterType ==
                    typeof(bool)
                )
                {
                    Type firstType =
                        parameters[0].ParameterType;

                    if (
                        IsMoneyPackType(
                            firstType
                        )
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
            }

            return fallback;
        }

        private bool IsMoneyPackType(
            Type type
        )
        {
            if (
                type == null
            )
            {
                return false;
            }

            if (
                string.Equals(
                    type.Name,
                    "MoneyPack",
                    StringComparison.Ordinal
                )
            )
            {
                return true;
            }

            if (
                type.FullName != null &&
                type.FullName.IndexOf(
                    "MoneyPack",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
            )
            {
                return true;
            }

            return false;
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
                    Instance.Log.LogWarning(
                        "SpawnMoney recebeu MoneyPack = null."
                    );

                    return;
                }

                GameObject gameObject =
                    Instance.ResolveMoneyPackGameObject(
                        __0
                    );

                Instance.Log.LogInfo(
                    "========================================"
                );

                Instance.Log.LogInfo(
                    "MONEY MATERIAL PROBE"
                );

                Instance.Log.LogInfo(
                    "MoneyPack Type: " +
                    __0.GetType().FullName
                );

                Instance.Log.LogInfo(
                    "Tipo informado pelo SpawnMoney: " +
                    (
                        __1
                            ? "COIN"
                            : "BILL"
                    )
                );

                Instance.Log.LogInfo(
                    "GameObject resolvido: " +
                    (
                        gameObject != null
                            ? gameObject.name
                            : "NULL"
                    )
                );

                if (
                    gameObject == null
                )
                {
                    Instance.Log.LogWarning(
                        "Nao foi possivel resolver o GameObject do MoneyPack."
                    );

                    Instance.Log.LogInfo(
                        "Dumpando membros do MoneyPack para investigacao."
                    );

                    Instance.DumpMoneyPackMembers(
                        __0
                    );

                    Instance.Log.LogInfo(
                        "========================================"
                    );

                    return;
                }

                float value =
                    Instance.ReadMoneyPackValue(
                        __0
                    );

                string key =
                    (
                        __1
                            ? "COIN"
                            : "BILL"
                    ) +
                    "|" +
                    gameObject.GetInstanceID() +
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
                    "Value: " +
                    value.ToString(
                        "0.00",
                        CultureInfo.InvariantCulture
                    )
                );

                Instance.Log.LogInfo(
                    "GameObject InstanceID: " +
                    gameObject.GetInstanceID()
                );

                Instance.AppendLine();

                Instance.AppendLine(
                    "========================================"
                );

                Instance.AppendLine(
                    "MONEY PACK ANALYSIS"
                );

                Instance.AppendLine(
                    "Type: " +
                    (
                        __1
                            ? "COIN"
                            : "BILL"
                    )
                );

                Instance.AppendLine(
                    "MoneyPack Type: " +
                    __0.GetType().FullName
                );

                Instance.AppendLine(
                    "GameObject: " +
                    gameObject.name
                );

                Instance.AppendLine(
                    "GameObject InstanceID: " +
                    gameObject.GetInstanceID()
                );

                Instance.AppendLine(
                    "Value: " +
                    value.ToString(
                        "0.00",
                        CultureInfo.InvariantCulture
                    )
                );

                Instance.AppendLine();

                Instance.AnalyzeHierarchy(
                    gameObject
                );

                Instance.AppendLine(
                    "========================================"
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
                        "Erro no Postfix:"
                    );

                    Instance.Log.LogError(
                        ex
                    );
                }
            }
        }

        private GameObject ResolveMoneyPackGameObject(
            object moneyPack
        )
        {
            if (
                moneyPack == null
            )
            {
                return null;
            }

            GameObject direct =
                TryResolveDirectly(
                    moneyPack
                );

            if (
                direct != null
            )
            {
                return direct;
            }

            direct =
                TryResolveUnityComponent(
                    moneyPack
                );

            if (
                direct != null
            )
            {
                return direct;
            }

            direct =
                TryResolveMembers(
                    moneyPack
                );

            if (
                direct != null
            )
            {
                return direct;
            }

            return null;
        }

        private GameObject TryResolveDirectly(
            object value
        )
        {
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

            return null;
        }

        private GameObject TryResolveUnityComponent(
            object value
        )
        {
            try
            {
                Component component =
                    value as Component;

                if (
                    component != null
                )
                {
                    GameObject gameObject =
                        component.gameObject;

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

        private GameObject TryResolveMembers(
            object moneyPack
        )
        {
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

                GameObject gameObject =
                    TryResolveMemberValue(
                        value
                    );

                if (
                    gameObject != null
                )
                {
                    return gameObject;
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

                GameObject gameObject =
                    TryResolveMemberValue(
                        value
                    );

                if (
                    gameObject != null
                )
                {
                    return gameObject;
                }
            }

            return null;
        }

        private GameObject TryResolveMemberValue(
            object value
        )
        {
            if (
                value == null
            )
            {
                return null;
            }

            GameObject gameObject =
                TryResolveDirectly(
                    value
                );

            if (
                gameObject != null
            )
            {
                return gameObject;
            }

            gameObject =
                TryResolveUnityComponent(
                    value
                );

            if (
                gameObject != null
            )
            {
                return gameObject;
            }

            return null;
        }

        private float ReadMoneyPackValue(
            object moneyPack
        )
        {
            Type type =
                moneyPack.GetType();

            string[] propertyNames =
            {
                "Value",
                "value",
                "MoneyValue"
            };

            for (
                int i = 0;
                i < propertyNames.Length;
                i++
            )
            {
                try
                {
                    PropertyInfo property =
                        type.GetProperty(
                            propertyNames[i],
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic
                        );

                    if (
                        property == null ||
                        property.GetIndexParameters().Length != 0
                    )
                    {
                        continue;
                    }

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

                    if (
                        result is double
                    )
                    {
                        return (
                            float
                        )
                        (
                            double
                        )
                        result;
                    }
                }
                catch
                {
                }
            }

            string[] fieldNames =
            {
                "m_Value",
                "value",
                "_value",
                "MoneyValue"
            };

            for (
                int i = 0;
                i < fieldNames.Length;
                i++
            )
            {
                try
                {
                    FieldInfo field =
                        type.GetField(
                            fieldNames[i],
                            BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic
                        );

                    if (
                        field == null
                    )
                    {
                        continue;
                    }

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

                    if (
                        result is double
                    )
                    {
                        return (
                            float
                        )
                        (
                            double
                        )
                        result;
                    }
                }
                catch
                {
                }
            }

            return -1f;
        }

        private void DumpMoneyPackMembers(
            object moneyPack
        )
        {
            try
            {
                AppendLine();

                AppendLine(
                    "MONEY PACK MEMBER DUMP"
                );

                AppendLine(
                    "--------------------------------"
                );

                Type type =
                    moneyPack.GetType();

                AppendLine(
                    "Type: " +
                    type.FullName
                );

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

                AppendLine(
                    "Properties: " +
                    properties.Length
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
                        property == null
                    )
                    {
                        continue;
                    }

                    string valueText =
                        "<unreadable>";

                    try
                    {
                        if (
                            property.GetIndexParameters().Length ==
                            0
                        )
                        {
                            object value =
                                property.GetValue(
                                    moneyPack,
                                    null
                                );

                            valueText =
                                DescribeObject(
                                    value
                                );
                        }
                    }
                    catch (
                        Exception ex
                    )
                    {
                        valueText =
                            "<error: " +
                            ex.Message +
                            ">";
                    }

                    AppendLine(
                        "PROPERTY: " +
                        property.Name +
                        " | TYPE=" +
                        (
                            property.PropertyType != null
                                ? property.PropertyType.FullName
                                : "null"
                        ) +
                        " | VALUE=" +
                        valueText
                    );
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

                AppendLine(
                    "Fields: " +
                    fields.Length
                );

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

                    string valueText =
                        "<unreadable>";

                    try
                    {
                        object value =
                            field.GetValue(
                                moneyPack
                            );

                        valueText =
                            DescribeObject(
                                value
                            );
                    }
                    catch (
                        Exception ex
                    )
                    {
                        valueText =
                            "<error: " +
                            ex.Message +
                            ">";
                    }

                    AppendLine(
                        "FIELD: " +
                        field.Name +
                        " | TYPE=" +
                        (
                            field.FieldType != null
                                ? field.FieldType.FullName
                                : "null"
                        ) +
                        " | VALUE=" +
                        valueText
                    );
                }
            }
            catch (Exception ex)
            {
                AppendLine(
                    "ERRO DUMP MONEY PACK: " +
                    ex
                );
            }
        }

        private string DescribeObject(
            object value
        )
        {
            if (
                value == null
            )
            {
                return "null";
            }

            try
            {
                GameObject gameObject =
                    value as GameObject;

                if (
                    gameObject != null
                )
                {
                    return
                        "GameObject(" +
                        gameObject.name +
                        ")";
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
                    component != null
                )
                {
                    return
                        "Component(" +
                        component.GetType().FullName +
                        ", GameObject=" +
                        (
                            component.gameObject != null
                                ? component.gameObject.name
                                : "null"
                        ) +
                        ")";
                }
            }
            catch
            {
            }

            try
            {
                return
                    value.GetType().FullName +
                    " | " +
                    value;
            }
            catch
            {
                return
                    "<object>";
            }
        }

        private void AnalyzeHierarchy(
            GameObject root
        )
        {
            if (
                root == null
            )
            {
                return;
            }

            AnalyzeGameObject(
                root,
                root.name,
                0
            );
        }

        private void AnalyzeGameObject(
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

            string indent =
                new string(
                    ' ',
                    depth * 2
                );

            AppendLine(
                indent +
                "GAMEOBJECT"
            );

            AppendLine(
                indent +
                "--------------------------------"
            );

            AppendLine(
                indent +
                "Name: " +
                gameObject.name
            );

            AppendLine(
                indent +
                "Path: " +
                hierarchyPath
            );

            AppendLine(
                indent +
                "InstanceID: " +
                gameObject.GetInstanceID()
            );

            AnalyzeRenderers(
                gameObject,
                hierarchyPath,
                depth
            );

            AnalyzeMeshFilters(
                gameObject,
                depth
            );

            AppendLine();

            try
            {
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
                    indent +
                    "ERRO CHILDREN: " +
                    ex
                );
            }
        }

        private void AnalyzeRenderers(
            GameObject gameObject,
            string hierarchyPath,
            int depth
        )
        {
            string indent =
                new string(
                    ' ',
                    depth * 2
                );

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

                AppendLine(
                    indent +
                    "RENDERERS"
                );

                AppendLine(
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

                    AppendLine(
                        indent +
                        "Renderer #" +
                        (
                            i + 1
                        )
                    );

                    AppendLine(
                        indent +
                        "Type: " +
                        renderer.GetType().FullName
                    );

                    AppendLine(
                        indent +
                        "Enabled: " +
                        renderer.enabled
                    );

                    Material[] materials =
                        null;

                    try
                    {
                        materials =
                            renderer.sharedMaterials;
                    }
                    catch
                    {
                    }

                    if (
                        materials == null
                    )
                    {
                        AppendLine(
                            indent +
                            "Materials: null"
                        );

                        continue;
                    }

                    AppendLine(
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
                            AppendLine(
                                indent +
                                "Material #" +
                                (
                                    m + 1
                                ) +
                                ": null"
                            );

                            continue;
                        }

                        string materialName =
                            material.name ??
                            string.Empty;

                        AppendLine(
                            indent +
                            "MATERIAL #" +
                            (
                                m + 1
                            )
                        );

                        AppendLine(
                            indent +
                            "Name: " +
                            materialName
                        );

                        AppendLine(
                            indent +
                            "InstanceID: " +
                            material.GetInstanceID()
                        );

                        try
                        {
                            Shader shader =
                                material.shader;

                            AppendLine(
                                indent +
                                "Shader: " +
                                (
                                    shader != null
                                        ? shader.name
                                        : "null"
                                )
                            );
                        }
                        catch
                        {
                            AppendLine(
                                indent +
                                "Shader: <error>"
                            );
                        }

                        bool interestingMaterial =
                            ContainsInterestingName(
                                materialName
                            );

                        AppendLine(
                            indent +
                            "Interesting Material: " +
                            (
                                interestingMaterial
                                    ? "TRUE"
                                    : "FALSE"
                            )
                        );

                        AnalyzeTextureProperties(
                            material,
                            gameObject,
                            hierarchyPath,
                            indent
                        );

                        AppendLine();
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLine(
                    indent +
                    "ERRO RENDERERS: " +
                    ex
                );
            }
        }

        private bool ContainsInterestingName(
            string name
        )
        {
            if (
                string.IsNullOrEmpty(
                    name
                )
            )
            {
                return false;
            }

            for (
                int i = 0;
                i < InterestingNames.Length;
                i++
            )
            {
                if (
                    name.IndexOf(
                        InterestingNames[i],
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0
                )
                {
                    return true;
                }
            }

            return false;
        }

        private void AnalyzeTextureProperties(
            Material material,
            GameObject owner,
            string hierarchyPath,
            string indent
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
                    bool hasProperty =
                        material.HasProperty(
                            property
                        );

                    if (
                        !hasProperty
                    )
                    {
                        AppendLine(
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

                    if (
                        texture == null
                    )
                    {
                        AppendLine(
                            indent +
                            "TextureProperty " +
                            property +
                            ": null"
                        );

                        continue;
                    }

                    string textureName =
                        texture.name ??
                        string.Empty;

                    AppendLine(
                        indent +
                        "TEXTURE PROPERTY"
                    );

                    AppendLine(
                        indent +
                        "Property: " +
                        property
                    );

                    AppendLine(
                        indent +
                        "Texture Name: " +
                        textureName
                    );

                    AppendLine(
                        indent +
                        "Texture Type: " +
                        texture.GetType().FullName
                    );

                    AppendLine(
                        indent +
                        "Texture InstanceID: " +
                        texture.GetInstanceID()
                    );

                    AppendLine(
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

                    bool interesting =
                        target ||
                        ContainsInterestingName(
                            textureName
                        );

                    AppendLine(
                        indent +
                        "Interesting Texture: " +
                        (
                            interesting
                                ? "TRUE"
                                : "FALSE"
                        )
                    );

                    AppendLine(
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
                        Log.LogInfo(
                            "########################################"
                        );

                        Log.LogInfo(
                            "TARGET TEXTURE ENCONTRADA"
                        );

                        Log.LogInfo(
                            "GameObject: " +
                            owner.name
                        );

                        Log.LogInfo(
                            "Path: " +
                            hierarchyPath
                        );

                        Log.LogInfo(
                            "Renderer Material: " +
                            material.name
                        );

                        Log.LogInfo(
                            "Property: " +
                            property
                        );

                        Log.LogInfo(
                            "Texture: " +
                            textureName
                        );

                        Log.LogInfo(
                            "InstanceID: " +
                            texture.GetInstanceID()
                        );

                        Log.LogInfo(
                            "########################################"
                        );
                    }
                }
                catch (Exception ex)
                {
                    AppendLine(
                        indent +
                        "ERRO PROPERTY " +
                        property +
                        ": " +
                        ex
                    );
                }
            }
        }

        private void AnalyzeMeshFilters(
            GameObject gameObject,
            int depth
        )
        {
            string indent =
                new string(
                    ' ',
                    depth * 2
                );

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

                AppendLine(
                    indent +
                    "MESH FILTERS"
                );

                AppendLine(
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

                    AppendLine(
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
                        AppendLine(
                            indent +
                            "Mesh: null"
                        );

                        continue;
                    }

                    AppendLine(
                        indent +
                        "Mesh Name: " +
                        mesh.name
                    );

                    AppendLine(
                        indent +
                        "Vertex Count: " +
                        mesh.vertexCount
                    );

                    AppendLine(
                        indent +
                        "SubMesh Count: " +
                        mesh.subMeshCount
                    );
                }
            }
            catch (Exception ex)
            {
                AppendLine(
                    indent +
                    "ERRO MESH FILTERS: " +
                    ex
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
    }
}