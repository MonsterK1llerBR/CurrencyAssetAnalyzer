#nullable disable

using System.Text;

using System;
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
    public class MeshApiProbe : BasePlugin
    {
        private const string GUID =
            "br.monsterk1llerbr.supermarketsimulator.meshapiprobe";

        private const string NAME =
            "Mesh API Probe";

        private const string VERSION =
            "1.0.0";

        private static MeshApiProbe Instance;

        private Harmony HarmonyInstance;

        private static string ReportDirectory;

        private static string ReportFile;

        private static bool AlreadyExecuted;

        public override void Load()
        {
            Instance = this;

            try
            {
                ReportDirectory =
                    Path.Combine(
                        Paths.PluginPath,
                        "CurrencyAssetAnalyzer",
                        "MeshApiProbe"
                    );

                ReportFile =
                    Path.Combine(
                        ReportDirectory,
                        "MeshApiReport.txt"
                    );

                Directory.CreateDirectory(
                    ReportDirectory
                );

                InitializeReport();

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "Mesh API Probe v1.0.0"
                );

                LogInfo(
                    "========================================"
                );

                PatchSpawnMoney();

                LogInfo(
                    "Patch aplicado."
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro inicializando Mesh API Probe: " +
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
                        "MESH API PROBE"
                    );

                    writer.WriteLine(
                        "VERSION: " +
                        VERSION
                    );

                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "Objetivo: descobrir a API real de UnityEngine.Mesh no runtime."
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
                        typeof(MeshApiProbe),
                        nameof(SpawnMoneyPostfix)
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
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro aplicando patch: " +
                    ex
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
                    AlreadyExecuted ||
                    !isCoin ||
                    moneyPack == null
                )
                {
                    return;
                }

                GameObject root =
                    ReadMoneyPackGameObject(
                        moneyPack
                    );

                if (root == null)
                    return;

                Mesh mesh =
                    FindCoinMesh(
                        root.transform
                    );

                if (mesh == null)
                    return;

                AlreadyExecuted =
                    true;

                ProbeMesh(
                    mesh
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

        private static Mesh FindCoinMesh(
            Transform root
        )
        {
            if (root == null)
                return null;

            MeshFilter filter =
                root.GetComponent<MeshFilter>();

            if (
                filter != null &&
                filter.sharedMesh != null
            )
            {
                if (
                    filter.sharedMesh.name.StartsWith(
                        "SM_Coin_",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return filter.sharedMesh;
                }
            }

            for (
                int i = 0;
                i < root.childCount;
                i++
            )
            {
                Mesh result =
                    FindCoinMesh(
                        root.GetChild(i)
                    );

                if (result != null)
                    return result;
            }

            return null;
        }

        private static void ProbeMesh(
            Mesh mesh
        )
        {
            LogInfo(
                "========================================"
            );

            LogInfo(
                "Mesh encontrado: " +
                mesh.name
            );

            Type meshType =
                mesh.GetType();

            LogInfo(
                "Runtime Type: " +
                meshType.FullName
            );

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
                    "MESH"
                );

                writer.WriteLine(
                    "Name: " +
                    mesh.name
                );

                writer.WriteLine(
                    "RuntimeType: " +
                    meshType.FullName
                );

                writer.WriteLine(
                    "Assembly: " +
                    (
                        meshType.Assembly != null
                            ? meshType.Assembly.FullName
                            : "null"
                    )
                );

                writer.WriteLine(
                    "VertexCount: " +
                    SafeVertexCount(
                        mesh
                    )
                );

                writer.WriteLine(
                    "SubMeshCount: " +
                    SafeSubMeshCount(
                        mesh
                    )
                );

                writer.WriteLine();

                writer.WriteLine(
                    "PROPERTIES"
                );

                writer.WriteLine(
                    "--------------------------------"
                );

                PropertyInfo[] properties =
                    meshType.GetProperties(
                        BindingFlags.Instance |
                        BindingFlags.Static |
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

                    string name =
                        property.Name;

                    string typeName =
                        property.PropertyType != null
                            ? property.PropertyType.FullName
                            : "null";

                    writer.WriteLine(
                        name +
                        " : " +
                        typeName
                    );
                }

                writer.WriteLine();

                writer.WriteLine(
                    "METHODS RELACIONADOS A DADOS"
                );

                writer.WriteLine(
                    "--------------------------------"
                );

                MethodInfo[] methods =
                    meshType.GetMethods(
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

                    string name =
                        method.Name;

                    string lower =
                        name.ToLowerInvariant();

                    if (
                        lower.Contains("vert") ||
                        lower.Contains("normal") ||
                        lower.Contains("tangent") ||
                        lower.Contains("uv") ||
                        lower.Contains("vertex") ||
                        lower.Contains("index") ||
                        lower.Contains("data") ||
                        lower.Contains("buffer") ||
                        lower.Contains("mesh")
                    )
                    {
                        writer.WriteLine(
                            FormatMethod(
                                method
                            )
                        );
                    }
                }

                writer.WriteLine();

                writer.WriteLine(
                    "NESTED TYPES"
                );

                writer.WriteLine(
                    "--------------------------------"
                );

                Type[] nestedTypes =
                    meshType.GetNestedTypes(
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                for (
                    int i = 0;
                    i < nestedTypes.Length;
                    i++
                )
                {
                    Type nested =
                        nestedTypes[i];

                    writer.WriteLine(
                        nested.FullName
                    );
                }

                writer.WriteLine();

                writer.WriteLine(
                    "STATIC MEMBERS RELATED TO MESH DATA"
                );

                writer.WriteLine(
                    "--------------------------------"
                );

                FieldInfo[] fields =
                    meshType.GetFields(
                        BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.NonPublic
                    );

                for (
                    int i = 0;
                    i < fields.Length;
                    i++
                )
                {
                    FieldInfo field =
                        fields[i];

                    string lower =
                        field.Name.ToLowerInvariant();

                    if (
                        lower.Contains("data") ||
                        lower.Contains("vertex") ||
                        lower.Contains("mesh") ||
                        lower.Contains("index")
                    )
                    {
                        writer.WriteLine(
                            field.Name +
                            " : " +
                            field.FieldType.FullName
                        );
                    }
                }

                writer.WriteLine();

                writer.WriteLine(
                    "END"
                );
            }

            SyncReportToRepository();

            LogInfo(
                "Relatorio Mesh API gerado."
            );
        }

        private static string FormatMethod(
            MethodInfo method
        )
        {
            try
            {
                StringBuilder builder =
                    new StringBuilder();

                builder.Append(
                    method.ReturnType != null
                        ? method.ReturnType.FullName
                        : "void"
                );

                builder.Append(
                    " "
                );

                builder.Append(
                    method.Name
                );

                builder.Append(
                    "("
                );

                ParameterInfo[] parameters =
                    method.GetParameters();

                for (
                    int i = 0;
                    i < parameters.Length;
                    i++
                )
                {
                    if (i > 0)
                        builder.Append(
                            ", "
                        );

                    ParameterInfo parameter =
                        parameters[i];

                    builder.Append(
                        parameter.ParameterType != null
                            ? parameter.ParameterType.FullName
                            : "null"
                    );

                    builder.Append(
                        " "
                    );

                    builder.Append(
                        parameter.Name
                    );
                }

                builder.Append(
                    ")"
                );

                return builder.ToString();
            }
            catch
            {
                return method.Name;
            }
        }

        private static int SafeVertexCount(
            Mesh mesh
        )
        {
            try
            {
                return mesh.vertexCount;
            }
            catch
            {
                return -1;
            }
        }

        private static int SafeSubMeshCount(
            Mesh mesh
        )
        {
            try
            {
                return mesh.subMeshCount;
            }
            catch
            {
                return -1;
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

        private static void SyncReportToRepository()
        {
            try
            {
                string repository =
                    @"C:\Users\natan\Documents\Mods\SupermarketSimulator\CurrencyAssetAnalyzer\Reports\MeshApiProbe";

                Directory.CreateDirectory(
                    repository
                );

                string destination =
                    Path.Combine(
                        repository,
                        "MeshApiReport.txt"
                    );

                File.Copy(
                    ReportFile,
                    destination,
                    true
                );

                LogInfo(
                    "Relatorio sincronizado: " +
                    destination
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro sincronizando relatorio: " +
                    ex
                );
            }
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
    }
}

