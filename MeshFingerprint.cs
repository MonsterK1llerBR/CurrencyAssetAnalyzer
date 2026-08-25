#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
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
    public class CurrencyMeshFingerprint : BasePlugin
    {
        private const string GUID =
            "br.monsterk1llerbr.supermarketsimulator.meshfingerprint";

        private const string NAME =
            "Currency Mesh Fingerprint";

        private const string VERSION =
            "1.1.0";

        private const string OutputDirectoryName =
            "MeshFingerprint";

        private static CurrencyMeshFingerprint Instance;

        private Harmony HarmonyInstance;

        private static string OutputDirectory;

        private static string ReportFile;

        private static readonly HashSet<string> AnalyzedMeshes =
            new HashSet<string>();

        public override void Load()
        {
            Instance = this;

            try
            {
                OutputDirectory =
                    Path.Combine(
                        Paths.PluginPath,
                        "CurrencyAssetAnalyzer",
                        OutputDirectoryName
                    );

                ReportFile =
                    Path.Combine(
                        OutputDirectory,
                        "MeshFingerprintReport.txt"
                    );

                Directory.CreateDirectory(
                    OutputDirectory
                );

                InitializeReport();

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "Currency Mesh Fingerprint v1.1.0"
                );

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "GetVertices(List): ATIVADO."
                );

                LogInfo(
                    "GetNormals(List): ATIVADO."
                );

                LogInfo(
                    "GetTangents(List): ATIVADO."
                );

                LogInfo(
                    "GetUVs(List): ATIVADO."
                );

                LogInfo(
                    "Arrays diretos mesh.vertices: DESATIVADOS."
                );

                LogInfo(
                    "Arrays diretos mesh.normals: DESATIVADOS."
                );

                LogInfo(
                    "Arrays diretos mesh.tangents: DESATIVADOS."
                );

                LogInfo(
                    "Arrays diretos mesh.uv: DESATIVADOS."
                );

                LogInfo(
                    "Hash normalizado: ATIVADO."
                );

                LogInfo(
                    "Saida: " +
                    OutputDirectory
                );

                PatchSpawnMoney();
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro inicializando Mesh Fingerprint: " +
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
                        "CURRENCY MESH FINGERPRINT"
                    );

                    writer.WriteLine(
                        "VERSION: " +
                        VERSION
                    );

                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "Metodo de leitura: List<T> IL2CPP."
                    );

                    writer.WriteLine(
                        "Objetivo: comparar geometria e UV dos meshes."
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
                        typeof(CurrencyMeshFingerprint),
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
                if (
                    Instance == null ||
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

                AnalyzeHierarchyForMesh(
                    root.transform
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

        private static void AnalyzeHierarchyForMesh(
            Transform root
        )
        {
            if (root == null)
                return;

            MeshFilter filter =
                root.GetComponent<MeshFilter>();

            if (
                filter != null &&
                filter.sharedMesh != null
            )
            {
                Mesh mesh =
                    filter.sharedMesh;

                string meshName =
                    mesh.name;

                if (
                    meshName.StartsWith(
                        "SM_Coin_",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    AnalyzeMesh(
                        mesh
                    );

                    return;
                }
            }

            for (
                int i = 0;
                i < root.childCount;
                i++
            )
            {
                AnalyzeHierarchyForMesh(
                    root.GetChild(i)
                );
            }
        }

        private static void AnalyzeMesh(
            Mesh mesh
        )
        {
            try
            {
                if (mesh == null)
                    return;

                string meshName =
                    mesh.name;

                if (
                    !AnalyzedMeshes.Add(
                        meshName
                    )
                )
                {
                    return;
                }

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "Analisando mesh: " +
                    meshName
                );

                LogInfo(
                    "VertexCount informado pelo Unity: " +
                    mesh.vertexCount
                );

                Il2CppSystem.Collections.Generic.List<Vector3> vertices =
                    ReadVertices(
                        mesh
                    );

                Il2CppSystem.Collections.Generic.List<Vector3> normals =
                    ReadNormals(
                        mesh
                    );

                Il2CppSystem.Collections.Generic.List<Vector4> tangents =
                    ReadTangents(
                        mesh
                    );

                Il2CppSystem.Collections.Generic.List<Vector2> uv0 =
                    ReadUV0(
                        mesh
                    );

                int vertexCount =
                    vertices != null
                        ? vertices.Count
                        : 0;

                int normalCount =
                    normals != null
                        ? normals.Count
                        : 0;

                int tangentCount =
                    tangents != null
                        ? tangents.Count
                        : 0;

                int uvCount =
                    uv0 != null
                        ? uv0.Count
                        : 0;

                LogInfo(
                    "Vertices lidos: " +
                    vertexCount
                );

                LogInfo(
                    "Normais lidas: " +
                    normalCount
                );

                LogInfo(
                    "Tangentes lidas: " +
                    tangentCount
                );

                LogInfo(
                    "UV0 lidos: " +
                    uvCount
                );

                string vertexHash =
                    ComputeVector3ListHash(
                        vertices
                    );

                string normalizedVertexHash =
                    ComputeNormalizedVertexListHash(
                        vertices
                    );

                string normalHash =
                    ComputeVector3ListHash(
                        normals
                    );

                string tangentHash =
                    ComputeVector4ListHash(
                        tangents
                    );

                string uvHash =
                    ComputeVector2ListHash(
                        uv0
                    );

                Bounds bounds =
                    mesh.bounds;

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
                        "Mesh Name: " +
                        meshName
                    );

                    writer.WriteLine(
                        "InstanceID: " +
                        mesh.GetInstanceID()
                    );

                    writer.WriteLine(
                        "VertexCount Unity: " +
                        mesh.vertexCount
                    );

                    writer.WriteLine(
                        "SubMeshCount: " +
                        mesh.subMeshCount
                    );

                    writer.WriteLine(
                        "Bounds Center: " +
                        bounds.center
                    );

                    writer.WriteLine(
                        "Bounds Extents: " +
                        bounds.extents
                    );

                    writer.WriteLine(
                        "Vertices Lidos: " +
                        vertexCount
                    );

                    writer.WriteLine(
                        "Normais Lidas: " +
                        normalCount
                    );

                    writer.WriteLine(
                        "Tangentes Lidas: " +
                        tangentCount
                    );

                    writer.WriteLine(
                        "UV0 Lidos: " +
                        uvCount
                    );

                    writer.WriteLine(
                        "Vertex Hash: " +
                        vertexHash
                    );

                    writer.WriteLine(
                        "Normalized Vertex Hash: " +
                        normalizedVertexHash
                    );

                    writer.WriteLine(
                        "Normal Hash: " +
                        normalHash
                    );

                    writer.WriteLine(
                        "Tangent Hash: " +
                        tangentHash
                    );

                    writer.WriteLine(
                        "UV Hash: " +
                        uvHash
                    );

                    WriteVertexStatistics(
                        writer,
                        vertices
                    );

                    WriteNormalStatistics(
                        writer,
                        normals
                    );

                    WriteTangentStatistics(
                        writer,
                        tangents
                    );

                    WriteUVStatistics(
                        writer,
                        uv0
                    );

                    writer.WriteLine();
                }
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro analisando mesh: " +
                    ex
                );
            }
        }

        private static Il2CppSystem.Collections.Generic.List<Vector3> ReadVertices(
            Mesh mesh
        )
        {
            try
            {
                Il2CppSystem.Collections.Generic.List<Vector3> list =
                    new Il2CppSystem.Collections.Generic.List<Vector3>();

                mesh.GetVertices(
                    list
                );

                return list;
            }
            catch (Exception ex)
            {
                LogError(
                    "GetVertices falhou: " +
                    ex.Message
                );

                return null;
            }
        }

        private static Il2CppSystem.Collections.Generic.List<Vector3> ReadNormals(
            Mesh mesh
        )
        {
            try
            {
                Il2CppSystem.Collections.Generic.List<Vector3> list =
                    new Il2CppSystem.Collections.Generic.List<Vector3>();

                mesh.GetNormals(
                    list
                );

                return list;
            }
            catch (Exception ex)
            {
                LogError(
                    "GetNormals falhou: " +
                    ex.Message
                );

                return null;
            }
        }

        private static Il2CppSystem.Collections.Generic.List<Vector4> ReadTangents(
            Mesh mesh
        )
        {
            try
            {
                Il2CppSystem.Collections.Generic.List<Vector4> list =
                    new Il2CppSystem.Collections.Generic.List<Vector4>();

                mesh.GetTangents(
                    list
                );

                return list;
            }
            catch (Exception ex)
            {
                LogError(
                    "GetTangents falhou: " +
                    ex.Message
                );

                return null;
            }
        }

        private static Il2CppSystem.Collections.Generic.List<Vector2> ReadUV0(
            Mesh mesh
        )
        {
            try
            {
                Il2CppSystem.Collections.Generic.List<Vector2> list =
                    new Il2CppSystem.Collections.Generic.List<Vector2>();

                mesh.GetUVs(
                    0,
                    list
                );

                return list;
            }
            catch (Exception ex)
            {
                LogError(
                    "GetUVs(0) falhou: " +
                    ex.Message
                );

                return null;
            }
        }

        private static void WriteVertexStatistics(
            StreamWriter writer,
            Il2CppSystem.Collections.Generic.List<Vector3> values
        )
        {
            if (
                values == null ||
                values.Count == 0
            )
            {
                writer.WriteLine(
                    "Vertex Statistics: UNAVAILABLE"
                );

                return;
            }

            Vector3 min =
                values[0];

            Vector3 max =
                values[0];

            for (
                int i = 1;
                i < values.Count;
                i++
            )
            {
                Vector3 value =
                    values[i];

                min.x =
                    Mathf.Min(
                        min.x,
                        value.x
                    );

                min.y =
                    Mathf.Min(
                        min.y,
                        value.y
                    );

                min.z =
                    Mathf.Min(
                        min.z,
                        value.z
                    );

                max.x =
                    Mathf.Max(
                        max.x,
                        value.x
                    );

                max.y =
                    Mathf.Max(
                        max.y,
                        value.y
                    );

                max.z =
                    Mathf.Max(
                        max.z,
                        value.z
                    );
            }

            writer.WriteLine(
                "Vertex Min: " +
                min
            );

            writer.WriteLine(
                "Vertex Max: " +
                max
            );

            writer.WriteLine(
                "Vertex Size: " +
                (
                    max -
                    min
                )
            );
        }

        private static void WriteNormalStatistics(
            StreamWriter writer,
            Il2CppSystem.Collections.Generic.List<Vector3> values
        )
        {
            if (
                values == null ||
                values.Count == 0
            )
            {
                writer.WriteLine(
                    "Normal Statistics: UNAVAILABLE"
                );

                return;
            }

            Vector3 min =
                values[0];

            Vector3 max =
                values[0];

            for (
                int i = 1;
                i < values.Count;
                i++
            )
            {
                Vector3 value =
                    values[i];

                min.x =
                    Mathf.Min(
                        min.x,
                        value.x
                    );

                min.y =
                    Mathf.Min(
                        min.y,
                        value.y
                    );

                min.z =
                    Mathf.Min(
                        min.z,
                        value.z
                    );

                max.x =
                    Mathf.Max(
                        max.x,
                        value.x
                    );

                max.y =
                    Mathf.Max(
                        max.y,
                        value.y
                    );

                max.z =
                    Mathf.Max(
                        max.z,
                        value.z
                    );
            }

            writer.WriteLine(
                "Normal Min: " +
                min
            );

            writer.WriteLine(
                "Normal Max: " +
                max
            );
        }

        private static void WriteTangentStatistics(
            StreamWriter writer,
            Il2CppSystem.Collections.Generic.List<Vector4> values
        )
        {
            if (
                values == null ||
                values.Count == 0
            )
            {
                writer.WriteLine(
                    "Tangent Statistics: UNAVAILABLE"
                );

                return;
            }

            Vector4 min =
                values[0];

            Vector4 max =
                values[0];

            for (
                int i = 1;
                i < values.Count;
                i++
            )
            {
                Vector4 value =
                    values[i];

                min.x =
                    Mathf.Min(
                        min.x,
                        value.x
                    );

                min.y =
                    Mathf.Min(
                        min.y,
                        value.y
                    );

                min.z =
                    Mathf.Min(
                        min.z,
                        value.z
                    );

                min.w =
                    Mathf.Min(
                        min.w,
                        value.w
                    );

                max.x =
                    Mathf.Max(
                        max.x,
                        value.x
                    );

                max.y =
                    Mathf.Max(
                        max.y,
                        value.y
                    );

                max.z =
                    Mathf.Max(
                        max.z,
                        value.z
                    );

                max.w =
                    Mathf.Max(
                        max.w,
                        value.w
                    );
            }

            writer.WriteLine(
                "Tangent Min: " +
                min
            );

            writer.WriteLine(
                "Tangent Max: " +
                max
            );
        }

        private static void WriteUVStatistics(
            StreamWriter writer,
            Il2CppSystem.Collections.Generic.List<Vector2> values
        )
        {
            if (
                values == null ||
                values.Count == 0
            )
            {
                writer.WriteLine(
                    "UV Statistics: UNAVAILABLE"
                );

                return;
            }

            Vector2 min =
                values[0];

            Vector2 max =
                values[0];

            for (
                int i = 1;
                i < values.Count;
                i++
            )
            {
                Vector2 value =
                    values[i];

                min.x =
                    Mathf.Min(
                        min.x,
                        value.x
                    );

                min.y =
                    Mathf.Min(
                        min.y,
                        value.y
                    );

                max.x =
                    Mathf.Max(
                        max.x,
                        value.x
                    );

                max.y =
                    Mathf.Max(
                        max.y,
                        value.y
                    );
            }

            writer.WriteLine(
                "UV Min: " +
                min
            );

            writer.WriteLine(
                "UV Max: " +
                max
            );

            writer.WriteLine(
                "UV Range: " +
                (
                    max -
                    min
                )
            );

            writer.WriteLine(
                "UV First: " +
                values[0]
            );

            int samples =
                Mathf.Min(
                    values.Count,
                    10
                );

            writer.WriteLine(
                "UV Samples:"
            );

            for (
                int i = 0;
                i < samples;
                i++
            )
            {
                writer.WriteLine(
                    "  [" +
                    i +
                    "] " +
                    values[i]
                );
            }
        }

        private static string ComputeVector3ListHash(
            Il2CppSystem.Collections.Generic.List<Vector3> values
        )
        {
            if (
                values == null ||
                values.Count == 0
            )
            {
                return "UNAVAILABLE";
            }

            using (
                SHA256 sha =
                    SHA256.Create()
            )
            using (
                MemoryStream stream =
                    new MemoryStream()
            )
            {
                for (
                    int i = 0;
                    i < values.Count;
                    i++
                )
                {
                    WriteFloat(
                        stream,
                        values[i].x
                    );

                    WriteFloat(
                        stream,
                        values[i].y
                    );

                    WriteFloat(
                        stream,
                        values[i].z
                    );
                }

                return BytesToHex(
                    sha.ComputeHash(
                        stream.ToArray()
                    )
                );
            }
        }

        private static string ComputeVector4ListHash(
            Il2CppSystem.Collections.Generic.List<Vector4> values
        )
        {
            if (
                values == null ||
                values.Count == 0
            )
            {
                return "UNAVAILABLE";
            }

            using (
                SHA256 sha =
                    SHA256.Create()
            )
            using (
                MemoryStream stream =
                    new MemoryStream()
            )
            {
                for (
                    int i = 0;
                    i < values.Count;
                    i++
                )
                {
                    WriteFloat(
                        stream,
                        values[i].x
                    );

                    WriteFloat(
                        stream,
                        values[i].y
                    );

                    WriteFloat(
                        stream,
                        values[i].z
                    );

                    WriteFloat(
                        stream,
                        values[i].w
                    );
                }

                return BytesToHex(
                    sha.ComputeHash(
                        stream.ToArray()
                    )
                );
            }
        }

        private static string ComputeVector2ListHash(
            Il2CppSystem.Collections.Generic.List<Vector2> values
        )
        {
            if (
                values == null ||
                values.Count == 0
            )
            {
                return "UNAVAILABLE";
            }

            using (
                SHA256 sha =
                    SHA256.Create()
            )
            using (
                MemoryStream stream =
                    new MemoryStream()
            )
            {
                for (
                    int i = 0;
                    i < values.Count;
                    i++
                )
                {
                    WriteFloat(
                        stream,
                        values[i].x
                    );

                    WriteFloat(
                        stream,
                        values[i].y
                    );
                }

                return BytesToHex(
                    sha.ComputeHash(
                        stream.ToArray()
                    )
                );
            }
        }

        private static string ComputeNormalizedVertexListHash(
            Il2CppSystem.Collections.Generic.List<Vector3> values
        )
        {
            if (
                values == null ||
                values.Count == 0
            )
            {
                return "UNAVAILABLE";
            }

            Vector3 min =
                values[0];

            Vector3 max =
                values[0];

            for (
                int i = 1;
                i < values.Count;
                i++
            )
            {
                Vector3 value =
                    values[i];

                min.x =
                    Mathf.Min(
                        min.x,
                        value.x
                    );

                min.y =
                    Mathf.Min(
                        min.y,
                        value.y
                    );

                min.z =
                    Mathf.Min(
                        min.z,
                        value.z
                    );

                max.x =
                    Mathf.Max(
                        max.x,
                        value.x
                    );

                max.y =
                    Mathf.Max(
                        max.y,
                        value.y
                    );

                max.z =
                    Mathf.Max(
                        max.z,
                        value.z
                    );
            }

            Vector3 size =
                max -
                min;

            if (
                Mathf.Abs(
                    size.x
                ) <
                0.0000001f
            )
            {
                size.x =
                    1f;
            }

            if (
                Mathf.Abs(
                    size.y
                ) <
                0.0000001f
            )
            {
                size.y =
                    1f;
            }

            if (
                Mathf.Abs(
                    size.z
                ) <
                0.0000001f
            )
            {
                size.z =
                    1f;
            }

            using (
                SHA256 sha =
                    SHA256.Create()
            )
            using (
                MemoryStream stream =
                    new MemoryStream()
            )
            {
                for (
                    int i = 0;
                    i < values.Count;
                    i++
                )
                {
                    Vector3 normalized =
                        new Vector3(
                            (
                                values[i].x -
                                min.x
                            ) /
                            size.x,

                            (
                                values[i].y -
                                min.y
                            ) /
                            size.y,

                            (
                                values[i].z -
                                min.z
                            ) /
                            size.z
                        );

                    WriteFloat(
                        stream,
                        normalized.x
                    );

                    WriteFloat(
                        stream,
                        normalized.y
                    );

                    WriteFloat(
                        stream,
                        normalized.z
                    );
                }

                return BytesToHex(
                    sha.ComputeHash(
                        stream.ToArray()
                    )
                );
            }
        }

        private static void WriteFloat(
            Stream stream,
            float value
        )
        {
            byte[] bytes =
                BitConverter.GetBytes(
                    value
                );

            stream.Write(
                bytes,
                0,
                bytes.Length
            );
        }

        private static string BytesToHex(
            byte[] bytes
        )
        {
            if (bytes == null)
                return "NULL";

            StringBuilder builder =
                new StringBuilder(
                    bytes.Length * 2
                );

            for (
                int i = 0;
                i < bytes.Length;
                i++
            )
            {
                builder.Append(
                    bytes[i].ToString(
                        "X2",
                        CultureInfo.InvariantCulture
                    )
                );
            }

            return builder.ToString();
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