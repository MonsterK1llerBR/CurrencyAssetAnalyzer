#nullable disable

using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime;
using HarmonyLib;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterK1llerBR.CurrencyAssetAnalyzer
{
    [BepInPlugin(
        GUID,
        NAME,
        VERSION
    )]
    public class MeshGPUReadbackProbe : BasePlugin
    {
        private const string GUID =
            "br.monsterk1llerbr.supermarketsimulator.meshgpureadbackprobe";

        private const string NAME =
            "Mesh GPU Readback Probe";

        private const string VERSION =
            "1.0.0";

        private static MeshGPUReadbackProbe Instance;

        private Harmony HarmonyInstance;

        private static string OutputDirectory;

        private static string ReportFile;

        private static GraphicsBuffer PendingBuffer;

        private static AsyncGPUReadbackRequest PendingRequest;

        private static bool RequestPending;

        private static bool AlreadyRequested;

        private static bool Finished;

        public override void Load()
        {
            Instance = this;

            try
            {
                OutputDirectory =
                    Path.Combine(
                        Paths.PluginPath,
                        "CurrencyAssetAnalyzer",
                        "MeshGPUReadbackProbe"
                    );

                ReportFile =
                    Path.Combine(
                        OutputDirectory,
                        "MeshGPUReadbackReport.txt"
                    );

                Directory.CreateDirectory(
                    OutputDirectory
                );

                InitializeReport();

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "Mesh GPU Readback Probe v1.0.0"
                );

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "AsyncGPUReadback: ATIVADO."
                );

                LogInfo(
                    "Objetivo: ler Vertex Buffer diretamente da GPU."
                );

                LogInfo(
                    "UV0 esperado no Offset 40."
                );

                LogInfo(
                    "Stride esperado: 48."
                );

                PatchSpawnMoney();
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro inicializando Mesh GPU Readback Probe: " +
                    ex
                );
            }
        }

        public override bool Unload()
        {
            try
            {
                ReleaseBuffer();
            }
            catch
            {
            }

            return base.Unload();
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
                        "MESH GPU READBACK PROBE"
                    );

                    writer.WriteLine(
                        "VERSION: " +
                        VERSION
                    );

                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "Objetivo: ler diretamente o Vertex Buffer da GPU."
                    );

                    writer.WriteLine(
                        "Metodo: AsyncGPUReadback.Request(GraphicsBuffer)."
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
                        typeof(MeshGPUReadbackProbe),
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
                    moneyPack == null ||
                    AlreadyRequested
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

                AlreadyRequested =
                    true;

                RequestVertexBufferReadback(
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

        private static void RequestVertexBufferReadback(
            Mesh mesh
        )
        {
            GraphicsBuffer buffer =
                null;

            try
            {
                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "Solicitando GPU Readback: " +
                    mesh.name
                );

                int stream =
                    mesh.GetVertexAttributeStream(
                        VertexAttribute.TexCoord0
                    );

                int offset =
                    mesh.GetVertexAttributeOffset(
                        VertexAttribute.TexCoord0
                    );

                int stride =
                    mesh.GetVertexBufferStride(
                        stream
                    );

                int vertexCount =
                    mesh.vertexCount;

                LogInfo(
                    "Mesh: " +
                    mesh.name
                );

                LogInfo(
                    "VertexCount: " +
                    vertexCount
                );

                LogInfo(
                    "UV Stream: " +
                    stream
                );

                LogInfo(
                    "UV Offset: " +
                    offset
                );

                LogInfo(
                    "Vertex Stride: " +
                    stride
                );

                buffer =
                    mesh.GetVertexBuffer(
                        stream
                    );

                if (buffer == null)
                {
                    LogError(
                        "GetVertexBuffer retornou NULL."
                    );

                    return;
                }

                LogInfo(
                    "GraphicsBuffer obtido."
                );

                LogInfo(
                    "Buffer Count: " +
                    buffer.count
                );

                LogInfo(
                    "Buffer Stride: " +
                    buffer.stride
                );

                LogInfo(
                    "Buffer Target: " +
                    buffer.target
                );

                LogInfo(
                    "Buffer IsValid: " +
                    buffer.IsValid()
                );

                int totalBytes =
                    checked(
                        vertexCount *
                        stride
                    );

                LogInfo(
                    "Total Bytes solicitados: " +
                    totalBytes
                );

                PendingBuffer =
                    buffer;

                PendingRequest =
                    AsyncGPUReadback.Request(
                        buffer,
                        DelegateSupport.ConvertDelegate<Il2CppSystem.Action<AsyncGPUReadbackRequest>>(
                            new Action<AsyncGPUReadbackRequest>(
                                ReadbackCompleted
                            )
                        )
                    );

                RequestPending =
                    true;

                LogInfo(
                    "AsyncGPUReadback solicitado com sucesso."
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro solicitando AsyncGPUReadback: " +
                    ex
                );

                try
                {
                    if (
                        buffer != null &&
                        buffer != PendingBuffer
                    )
                    {
                        buffer.Release();
                    }
                }
                catch
                {
                }
            }
        }

        private static void ReadbackCompleted(
            AsyncGPUReadbackRequest request
        )
        {
            try
            {
                ProcessReadback(
                    request
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro no callback de GPU Readback: " +
                    ex
                );

                ReleaseBuffer();
            }
        }
        private static void ProcessReadback(
            AsyncGPUReadbackRequest request
        )
        {
            try
            {
                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "Processando GPU Readback."
                );

                if (
                    request.hasError
                )
                {
                    WriteReport(
                        "READBACK RESULT: ERROR"
                    );

                    WriteReport(
                        "AsyncGPUReadbackRequest.hasError = true"
                    );

                    LogError(
                        "GPU Readback retornou erro."
                    );

                    ReleaseBuffer();

                    return;
                }

                LogInfo(
                    "GPU Readback: SUCCESS."
                );

                NativeArray<byte> data =
                    request.GetData<byte>(0);

                if (
                    !data.IsCreated
                )
                {
                    WriteReport(
                        "NativeArray nao foi criado."
                    );

                    ReleaseBuffer();

                    return;
                }

                int byteCount =
                    data.Length;

                LogInfo(
                    "Bytes recebidos: " +
                    byteCount
                );

                WriteReport(
                    "READBACK RESULT: SUCCESS"
                );

                WriteReport(
                    "Bytes recebidos: " +
                    byteCount
                );

                AnalyzeVertexBufferBytes(
                    data
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro processando GPU Readback: " +
                    ex
                );

                WriteReport(
                    "READBACK PROCESS ERROR:"
                );

                WriteReport(
                    ex.ToString()
                );
            }
            finally
            {
                ReleaseBuffer();
            }
        }

        private static void AnalyzeVertexBufferBytes(
            NativeArray<byte> data
        )
        {
            try
            {
                const int VertexStride =
                    48;

                const int UVOffset =
                    40;

                int vertexCount =
                    data.Length /
                    VertexStride;

                WriteReport(
                    ""
                );

                WriteReport(
                    "VERTEX BUFFER ANALYSIS"
                );

                WriteReport(
                    "--------------------------------"
                );

                WriteReport(
                    "Stride: " +
                    VertexStride
                );

                WriteReport(
                    "UV Offset: " +
                    UVOffset
                );

                WriteReport(
                    "Vertex Count Calculated: " +
                    vertexCount
                );

                if (
                    vertexCount <= 0
                )
                {
                    WriteReport(
                        "Nenhum vertice encontrado."
                    );

                    return;
                }

                float minU =
                    float.MaxValue;

                float maxU =
                    float.MinValue;

                float minV =
                    float.MaxValue;

                float maxV =
                    float.MinValue;

                int validUVCount =
                    0;

                int sampleCount =
                    Math.Min(
                        vertexCount,
                        30
                    );

                WriteReport(
                    ""
                );

                WriteReport(
                    "UV SAMPLE"
                );

                WriteReport(
                    "--------------------------------"
                );

                for (
                    int i = 0;
                    i < vertexCount;
                    i++
                )
                {
                    int baseOffset =
                        i *
                        VertexStride;

                    if (
                        baseOffset +
                        UVOffset +
                        8 >
                        data.Length
                    )
                    {
                        break;
                    }

                    float u =
                        BitConverter.ToSingle(
                            GetBytes(
                                data,
                                baseOffset +
                                UVOffset,
                                4
                            ),
                            0
                        );

                    float v =
                        BitConverter.ToSingle(
                            GetBytes(
                                data,
                                baseOffset +
                                UVOffset +
                                4,
                                4
                            ),
                            0
                        );

                    if (
                        !float.IsNaN(u) &&
                        !float.IsNaN(v) &&
                        !float.IsInfinity(u) &&
                        !float.IsInfinity(v)
                    )
                    {
                        validUVCount++;

                        if (u < minU)
                            minU = u;

                        if (u > maxU)
                            maxU = u;

                        if (v < minV)
                            minV = v;

                        if (v > maxV)
                            maxV = v;
                    }

                    if (
                        i < sampleCount
                    )
                    {
                        WriteReport(
                            "[" +
                            i +
                            "] U=" +
                            FormatFloat(u) +
                            " V=" +
                            FormatFloat(v)
                        );
                    }
                }

                WriteReport(
                    ""
                );

                WriteReport(
                    "UV STATISTICS"
                );

                WriteReport(
                    "--------------------------------"
                );

                WriteReport(
                    "Valid UV Count: " +
                    validUVCount
                );

                if (
                    validUVCount > 0
                )
                {
                    WriteReport(
                        "UV Min: (" +
                        FormatFloat(minU) +
                        ", " +
                        FormatFloat(minV) +
                        ")"
                    );

                    WriteReport(
                        "UV Max: (" +
                        FormatFloat(maxU) +
                        ", " +
                        FormatFloat(maxV) +
                        ")"
                    );

                    WriteReport(
                        "UV Width: " +
                        FormatFloat(
                            maxU -
                            minU
                        )
                    );

                    WriteReport(
                        "UV Height: " +
                        FormatFloat(
                            maxV -
                            minV
                        )
                    );

                    WriteReport(
                        "MappingFound: TRUE"
                    );
                }
                else
                {
                    WriteReport(
                        "MappingFound: FALSE"
                    );
                }

                WriteVertexSample(
                    data
                );
            }
            catch (Exception ex)
            {
                WriteReport(
                    "Vertex buffer analysis error:"
                );

                WriteReport(
                    ex.ToString()
                );
            }
        }

        private static void WriteVertexSample(
            NativeArray<byte> data
        )
        {
            try
            {
                const int VertexStride =
                    48;

                const int PositionOffset =
                    0;

                const int UVOffset =
                    40;

                WriteReport(
                    ""
                );

                WriteReport(
                    "POSITION + UV SAMPLE"
                );

                WriteReport(
                    "--------------------------------"
                );

                int vertexCount =
                    Math.Min(
                        data.Length /
                        VertexStride,
                        10
                    );

                for (
                    int i = 0;
                    i < vertexCount;
                    i++
                )
                {
                    int baseOffset =
                        i *
                        VertexStride;

                    float x =
                        BitConverter.ToSingle(
                            GetBytes(
                                data,
                                baseOffset +
                                PositionOffset,
                                4
                            ),
                            0
                        );

                    float y =
                        BitConverter.ToSingle(
                            GetBytes(
                                data,
                                baseOffset +
                                PositionOffset +
                                4,
                                4
                            ),
                            0
                        );

                    float z =
                        BitConverter.ToSingle(
                            GetBytes(
                                data,
                                baseOffset +
                                PositionOffset +
                                8,
                                4
                            ),
                            0
                        );

                    float u =
                        BitConverter.ToSingle(
                            GetBytes(
                                data,
                                baseOffset +
                                UVOffset,
                                4
                            ),
                            0
                        );

                    float v =
                        BitConverter.ToSingle(
                            GetBytes(
                                data,
                                baseOffset +
                                UVOffset +
                                4,
                                4
                            ),
                            0
                        );

                    WriteReport(
                        "[" +
                        i +
                        "] " +
                        "P=(" +
                        FormatFloat(x) +
                        ", " +
                        FormatFloat(y) +
                        ", " +
                        FormatFloat(z) +
                        ") " +
                        "UV=(" +
                        FormatFloat(u) +
                        ", " +
                        FormatFloat(v) +
                        ")"
                    );
                }
            }
            catch (Exception ex)
            {
                WriteReport(
                    "Sample error:"
                );

                WriteReport(
                    ex.ToString()
                );
            }
        }

        private static byte[] GetBytes(
            NativeArray<byte> source,
            int offset,
            int count
        )
        {
            byte[] result =
                new byte[
                    count
                ];

            for (
                int i = 0;
                i < count;
                i++
            )
            {
                result[i] =
                    source[
                        offset +
                        i
                    ];
            }

            return result;
        }

        private static void ReleaseBuffer()
        {
            try
            {
                if (
                    PendingBuffer != null
                )
                {
                    PendingBuffer.Release();
                }
            }
            catch
            {
            }

            PendingBuffer =
                null;
        }

        private static void WriteReport(
            string text
        )
        {
            try
            {
                using (
                    StreamWriter writer =
                        new StreamWriter(
                            ReportFile,
                            true
                        )
                )
                {
                    writer.WriteLine(
                        text
                    );
                }

                LogInfo(
                    text
                );
            }
            catch
            {
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
                Mesh mesh =
                    filter.sharedMesh;

                if (
                    mesh.name.StartsWith(
                        "SM_Coin_",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return mesh;
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

        private static string FormatFloat(
            float value
        )
        {
            return value.ToString(
                "0.000000",
                System.Globalization.CultureInfo.InvariantCulture
            );
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







