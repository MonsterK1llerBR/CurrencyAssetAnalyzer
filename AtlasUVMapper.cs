#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
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
    public class AtlasUVMapper : BasePlugin
    {
        private const string GUID =
            "br.monsterk1llerbr.supermarketsimulator.atlasuvmapper";

        private const string NAME =
            "Currency Atlas UV Mapper";

        private const string VERSION =
            "1.0.0";

        private const int AtlasWidth =
            2048;

        private const int AtlasHeight =
            2048;

        private static AtlasUVMapper Instance;

        private Harmony HarmonyInstance;

        private static string OutputDirectory;

        private static string ReportFile;

        private static readonly HashSet<string> QueuedMeshes =
            new HashSet<string>();

        private static readonly Queue<CoinMapRequest> PendingMeshes =
            new Queue<CoinMapRequest>();

        private static CoinMapRequest CurrentRequest;

        private static bool ReadbackPending;

        private static bool Processing;

        private class CoinMapRequest
        {
            public string CoinName;

            public float Value;

            public Mesh Mesh;

            public Material Material;

            public Texture Texture;

            public int VertexCount;

            public int VertexStride;

            public int UVOffset;

            public int UVStream;

            public GraphicsBuffer Buffer;
        }

        public override void Load()
        {
            Instance = this;

            try
            {
                OutputDirectory =
                    Path.Combine(
                        Paths.PluginPath,
                        "CurrencyAssetAnalyzer",
                        "AtlasUVMapper"
                    );

                ReportFile =
                    Path.Combine(
                        OutputDirectory,
                        "AtlasUVMapReport.txt"
                    );

                Directory.CreateDirectory(
                    OutputDirectory
                );

                InitializeReport();

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "Currency Atlas UV Mapper v1.0.0"
                );

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "Metodo: GPU Vertex Buffer + AsyncGPUReadback."
                );

                LogInfo(
                    "Objetivo: mapear todas as moedas no atlas."
                );

                LogInfo(
                    "Atlas esperado: " +
                    AtlasWidth +
                    "x" +
                    AtlasHeight
                );

                PatchSpawnMoney();
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro inicializando Atlas UV Mapper: " +
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
                        "CURRENCY ATLAS UV MAPPER"
                    );

                    writer.WriteLine(
                        "VERSION: " +
                        VERSION
                    );

                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "Metodo: GPU Vertex Buffer + AsyncGPUReadback."
                    );

                    writer.WriteLine(
                        "Objetivo: identificar a regiao exata de cada moeda."
                    );

                    writer.WriteLine(
                        "Atlas esperado: " +
                        AtlasWidth +
                        "x" +
                        AtlasHeight
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
                        typeof(AtlasUVMapper),
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
                    moneyPack == null ||
                    !isCoin ||
                    Instance == null
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

                string meshName =
                    mesh.name;

                if (
                    string.IsNullOrWhiteSpace(
                        meshName
                    )
                )
                {
                    return;
                }

                if (
                    !QueuedMeshes.Add(
                        meshName
                    )
                )
                {
                    return;
                }

                float value =
                    ReadMoneyPackValue(
                        moneyPack
                    );

                Material material =
                    FindCoinMaterial(
                        root
                    );

                Texture texture =
                    FindCoinTexture(
                        material
                    );

                CoinMapRequest request =
                    new CoinMapRequest();

                request.CoinName =
                    meshName;

                request.Value =
                    value;

                request.Mesh =
                    mesh;

                request.Material =
                    material;

                request.Texture =
                    texture;

                request.VertexCount =
                    mesh.vertexCount;

                request.UVStream =
                    mesh.GetVertexAttributeStream(
                        VertexAttribute.TexCoord0
                    );

                request.UVOffset =
                    mesh.GetVertexAttributeOffset(
                        VertexAttribute.TexCoord0
                    );

                request.VertexStride =
                    mesh.GetVertexBufferStride(
                        request.UVStream
                    );

                PendingMeshes.Enqueue(
                    request
                );

                LogInfo(
                    "Moeda adicionada a fila: " +
                    meshName +
                    " | Value=" +
                    FormatFloat(
                        value
                    )
                );

                StartNextReadback();
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro registrando moeda: " +
                    ex
                );
            }
        }

        private static void StartNextReadback()
        {
            try
            {
                if (
                    ReadbackPending ||
                    Processing ||
                    PendingMeshes.Count == 0
                )
                {
                    return;
                }

                CurrentRequest =
                    PendingMeshes.Dequeue();

                if (
                    CurrentRequest.Mesh == null
                )
                {
                    CurrentRequest =
                        null;

                    StartNextReadback();

                    return;
                }

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "Mapeando moeda: " +
                    CurrentRequest.CoinName
                );

                LogInfo(
                    "Value: " +
                    FormatFloat(
                        CurrentRequest.Value
                    )
                );

                LogInfo(
                    "Mesh: " +
                    CurrentRequest.Mesh.name
                );

                LogInfo(
                    "VertexCount: " +
                    CurrentRequest.VertexCount
                );

                LogInfo(
                    "UV Stream: " +
                    CurrentRequest.UVStream
                );

                LogInfo(
                    "UV Offset: " +
                    CurrentRequest.UVOffset
                );

                LogInfo(
                    "Vertex Stride: " +
                    CurrentRequest.VertexStride
                );

                if (
                    CurrentRequest.VertexStride <=
                    0
                )
                {
                    LogError(
                        "Stride invalido."
                    );

                    FinishCurrentRequest();

                    return;
                }

                CurrentRequest.Buffer =
                    CurrentRequest.Mesh.GetVertexBuffer(
                        CurrentRequest.UVStream
                    );

                if (
                    CurrentRequest.Buffer == null
                )
                {
                    LogError(
                        "VertexBuffer retornou NULL."
                    );

                    FinishCurrentRequest();

                    return;
                }

                if (
                    !CurrentRequest.Buffer.IsValid()
                )
                {
                    LogError(
                        "VertexBuffer invalido."
                    );

                    FinishCurrentRequest();

                    return;
                }

                LogInfo(
                    "Buffer Count: " +
                    CurrentRequest.Buffer.count
                );

                LogInfo(
                    "Buffer Stride: " +
                    CurrentRequest.Buffer.stride
                );

                LogInfo(
                    "Buffer Target: " +
                    CurrentRequest.Buffer.target
                );

                ReadbackPending =
                    true;

                AsyncGPUReadbackRequest request =
                    AsyncGPUReadback.Request(
                        CurrentRequest.Buffer,
                        DelegateSupport.ConvertDelegate<Il2CppSystem.Action<AsyncGPUReadbackRequest>>(
                            new Action<AsyncGPUReadbackRequest>(
                                ReadbackCompleted
                            )
                        )
                    );

                WriteReport(
                    "Readback solicitado: " +
                    CurrentRequest.CoinName
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro iniciando readback: " +
                    ex
                );

                FinishCurrentRequest();
            }
        }

        private static void ReadbackCompleted(
            AsyncGPUReadbackRequest request
        )
        {
            try
            {
                ReadbackPending =
                    false;

                if (
                    CurrentRequest == null
                )
                {
                    return;
                }

                if (
                    request.hasError
                )
                {
                    WriteReport(
                        "READBACK ERROR: " +
                        CurrentRequest.CoinName
                    );

                    LogError(
                        "Readback falhou: " +
                        CurrentRequest.CoinName
                    );

                    FinishCurrentRequest();

                    return;
                }

                NativeArray<byte> data =
                    request.GetData<byte>(
                        0
                    );

                if (
                    !data.IsCreated ||
                    data.Length <= 0
                )
                {
                    WriteReport(
                        "READBACK EMPTY: " +
                        CurrentRequest.CoinName
                    );

                    FinishCurrentRequest();

                    return;
                }

                AnalyzeCurrentMesh(
                    data
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro no callback de readback: " +
                    ex
                );

                FinishCurrentRequest();
            }
        }

        private static void AnalyzeCurrentMesh(
            NativeArray<byte> data
        )
        {
            try
            {
                CoinMapRequest request =
                    CurrentRequest;

                int stride =
                    request.VertexStride;

                int offset =
                    request.UVOffset;

                int expectedVertexCount =
                    request.VertexCount;

                int calculatedVertexCount =
                    data.Length /
                    stride;

                int vertexCount =
                    Math.Min(
                        expectedVertexCount,
                        calculatedVertexCount
                    );

                float minU =
                    float.MaxValue;

                float maxU =
                    float.MinValue;

                float minV =
                    float.MaxValue;

                float maxV =
                    float.MinValue;

                int validCount =
                    0;

                for (
                    int i = 0;
                    i < vertexCount;
                    i++
                )
                {
                    int baseOffset =
                        i *
                        stride;

                    if (
                        baseOffset +
                        offset +
                        8 >
                        data.Length
                    )
                    {
                        break;
                    }

                    float u =
                        ReadFloat(
                            data,
                            baseOffset +
                            offset
                        );

                    float v =
                        ReadFloat(
                            data,
                            baseOffset +
                            offset +
                            4
                        );

                    if (
                        !IsValidFloat(
                            u
                        ) ||
                        !IsValidFloat(
                            v
                        )
                    )
                    {
                        continue;
                    }

                    validCount++;

                    if (u < minU)
                        minU = u;

                    if (u > maxU)
                        maxU = u;

                    if (v < minV)
                        minV = v;

                    if (v > maxV)
                        maxV = v;
                }

                WriteReport(
                    ""
                );

                WriteReport(
                    "----------------------------------------"
                );

                WriteReport(
                    "Coin: " +
                    request.CoinName
                );

                WriteReport(
                    "Value: " +
                    FormatFloat(
                        request.Value
                    )
                );

                WriteReport(
                    "Mesh: " +
                    request.Mesh.name
                );

                WriteReport(
                    "Material: " +
                    (
                        request.Material != null
                            ? request.Material.name
                            : "null"
                    )
                );

                WriteReport(
                    "Texture: " +
                    (
                        request.Texture != null
                            ? request.Texture.name
                            : "null"
                    )
                );

                WriteReport(
                    "Texture Type: " +
                    (
                        request.Texture != null
                            ? request.Texture.GetType().FullName
                            : "null"
                    )
                );

                WriteReport(
                    "Vertex Count Expected: " +
                    expectedVertexCount
                );

                WriteReport(
                    "Vertex Count Buffer: " +
                    calculatedVertexCount
                );

                WriteReport(
                    "UV Valid Count: " +
                    validCount
                );

                if (
                    validCount <= 0
                )
                {
                    WriteReport(
                        "MappingFound: FALSE"
                    );

                    FinishCurrentRequest();

                    return;
                }

                float uvWidth =
                    maxU -
                    minU;

                float uvHeight =
                    maxV -
                    minV;

                float pixelMinX =
                    minU *
                    AtlasWidth;

                float pixelMaxX =
                    maxU *
                    AtlasWidth;

                float pixelMinY =
                    minV *
                    AtlasHeight;

                float pixelMaxY =
                    maxV *
                    AtlasHeight;

                float imageTop =
                    (
                        1f -
                        maxV
                    ) *
                    AtlasHeight;

                float imageBottom =
                    (
                        1f -
                        minV
                    ) *
                    AtlasHeight;

                WriteReport(
                    "UV Min U: " +
                    FormatFloat(
                        minU
                    )
                );

                WriteReport(
                    "UV Max U: " +
                    FormatFloat(
                        maxU
                    )
                );

                WriteReport(
                    "UV Min V: " +
                    FormatFloat(
                        minV
                    )
                );

                WriteReport(
                    "UV Max V: " +
                    FormatFloat(
                        maxV
                    )
                );

                WriteReport(
                    "UV Width: " +
                    FormatFloat(
                        uvWidth
                    )
                );

                WriteReport(
                    "UV Height: " +
                    FormatFloat(
                        uvHeight
                    )
                );

                WriteReport(
                    "Atlas Pixel X Min: " +
                    FormatFloat(
                        pixelMinX
                    )
                );

                WriteReport(
                    "Atlas Pixel X Max: " +
                    FormatFloat(
                        pixelMaxX
                    )
                );

                WriteReport(
                    "Atlas Pixel Y Min UV: " +
                    FormatFloat(
                        pixelMinY
                    )
                );

                WriteReport(
                    "Atlas Pixel Y Max UV: " +
                    FormatFloat(
                        pixelMaxY
                    )
                );

                WriteReport(
                    "PNG X: " +
                    FormatFloat(
                        pixelMinX
                    ) +
                    " -> " +
                    FormatFloat(
                        pixelMaxX
                    )
                );

                WriteReport(
                    "PNG Y: " +
                    FormatFloat(
                        imageTop
                    ) +
                    " -> " +
                    FormatFloat(
                        imageBottom
                    )
                );

                WriteReport(
                    "MappingFound: TRUE"
                );

                WriteReport(
                    ""
                );

                WriteReport(
                    "UV CORNERS"
                );

                WriteReport(
                    "--------------------------------"
                );

                WriteReport(
                    "Bottom Left: (" +
                    FormatFloat(
                        minU
                    ) +
                    ", " +
                    FormatFloat(
                        minV
                    ) +
                    ")"
                );

                WriteReport(
                    "Bottom Right: (" +
                    FormatFloat(
                        maxU
                    ) +
                    ", " +
                    FormatFloat(
                        minV
                    ) +
                    ")"
                );

                WriteReport(
                    "Top Left: (" +
                    FormatFloat(
                        minU
                    ) +
                    ", " +
                    FormatFloat(
                        maxV
                    ) +
                    ")"
                );

                WriteReport(
                    "Top Right: (" +
                    FormatFloat(
                        maxU
                    ) +
                    ", " +
                    FormatFloat(
                        maxV
                    ) +
                    ")"
                );

                WriteReport(
                    ""
                );

                WriteReport(
                    "UV SAMPLES"
                );

                WriteReport(
                    "--------------------------------"
                );

                int sampleCount =
                    Math.Min(
                        vertexCount,
                        20
                    );

                for (
                    int i = 0;
                    i < sampleCount;
                    i++
                )
                {
                    int baseOffset =
                        i *
                        stride;

                    float u =
                        ReadFloat(
                            data,
                            baseOffset +
                            offset
                        );

                    float v =
                        ReadFloat(
                            data,
                            baseOffset +
                            offset +
                            4
                        );

                    WriteReport(
                        "[" +
                        i +
                        "] U=" +
                        FormatFloat(
                            u
                        ) +
                        " V=" +
                        FormatFloat(
                            v
                        )
                    );
                }

                LogInfo(
                    "Mapeamento concluido: " +
                    request.CoinName +
                    " | U=" +
                    FormatFloat(
                        minU
                    ) +
                    ".." +
                    FormatFloat(
                        maxU
                    ) +
                    " | V=" +
                    FormatFloat(
                        minV
                    ) +
                    ".." +
                    FormatFloat(
                        maxV
                    )
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro analisando mesh atual: " +
                    ex
                );
            }
            finally
            {
                FinishCurrentRequest();
            }
        }

        private static void FinishCurrentRequest()
        {
            try
            {
                if (
                    CurrentRequest != null &&
                    CurrentRequest.Buffer != null
                )
                {
                    CurrentRequest.Buffer.Release();
                }
            }
            catch
            {
            }

            if (
                CurrentRequest != null
            )
            {
                LogInfo(
                    "Finalizado: " +
                    CurrentRequest.CoinName
                );
            }

            CurrentRequest =
                null;

            ReadbackPending =
                false;

            Processing =
                false;

            StartNextReadback();
        }

        private static float ReadFloat(
            NativeArray<byte> data,
            int offset
        )
        {
            byte b0 =
                data[
                    offset
                ];

            byte b1 =
                data[
                    offset +
                    1
                ];

            byte b2 =
                data[
                    offset +
                    2
                ];

            byte b3 =
                data[
                    offset +
                    3
                ];

            int bits =
                b0 |
                (
                    b1 <<
                    8
                ) |
                (
                    b2 <<
                    16
                ) |
                (
                    b3 <<
                    24
                );

            return BitConverter.Int32BitsToSingle(
                bits
            );
        }

        private static bool IsValidFloat(
            float value
        )
        {
            return !float.IsNaN(
                value
            ) &&
            !float.IsInfinity(
                value
            );
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
            }
            catch
            {
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
                    {
                        return (float)result;
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

                if (field != null)
                {
                    object result =
                        field.GetValue(
                            moneyPack
                        );

                    if (result is float)
                    {
                        return (float)result;
                    }
                }
            }
            catch
            {
            }

            return -1f;
        }

        private static Material FindCoinMaterial(
            GameObject root
        )
        {
            try
            {
                Renderer renderer =
                    root.GetComponentInChildren<Renderer>();

                if (renderer == null)
                    return null;

                Material[] materials =
                    renderer.sharedMaterials;

                if (
                    materials == null ||
                    materials.Length == 0
                )
                {
                    return null;
                }

                for (
                    int i = 0;
                    i < materials.Length;
                    i++
                )
                {
                    if (
                        materials[i] != null
                    )
                    {
                        return materials[i];
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private static Texture FindCoinTexture(
            Material material
        )
        {
            if (material == null)
                return null;

            try
            {
                string[] properties =
                {
                    "_BaseMap",
                    "_MainTex"
                };

                for (
                    int i = 0;
                    i < properties.Length;
                    i++
                )
                {
                    string property =
                        properties[i];

                    if (
                        !material.HasProperty(
                            property
                        )
                    )
                    {
                        continue;
                    }

                    Texture texture =
                        material.GetTexture(
                            property
                        );

                    if (texture != null)
                    {
                        return texture;
                    }
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
                {
                    return result;
                }
            }

            return null;
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
                    {
                        return type;
                    }
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

        private static void LogInfo(
            string message
        )
        {
            if (
                Instance != null
            )
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
            if (
                Instance != null
            )
            {
                Instance.Log.LogError(
                    message
                );
            }
        }
    }
}