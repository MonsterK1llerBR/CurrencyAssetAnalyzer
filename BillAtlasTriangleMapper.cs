#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
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
    public class BillAtlasTriangleMapper : BasePlugin
    {
        private const string GUID =
            "br.monsterk1llerbr.supermarketsimulator.billatlastrianglemapper";

        private const string NAME =
            "Bill Atlas Triangle Mapper";

        private const string VERSION =
            "1.0.0";

        private const int AtlasWidth =
            2048;

        private const int AtlasHeight =
            2048;

        private const string TargetMeshName =
            "1_Dollar_Plane";

        private static BillAtlasTriangleMapper Instance;

        private Harmony HarmonyInstance;

        private string OutputDirectory;

        private string ReportFile;

        private static bool AnalysisQueued;

        private static bool ReadbackPending;

        private static BillRequest CurrentRequest;

        private enum ReadbackStage
        {
            None,
            Vertices,
            Indices
        }

        private static ReadbackStage CurrentStage =
            ReadbackStage.None;

        private sealed class SubMeshData
        {
            public int SubMesh;

            public int IndexStart;

            public int IndexCount;

            public int BaseVertex;

            public int TriangleCount;

            public int[] Indices;
        }

        private sealed class BillRequest
        {
            public GameObject Root;

            public Mesh Mesh;

            public Material Material;

            public Texture Texture;

            public string RendererName;

            public int VertexCount;

            public int VertexStream;

            public int VertexOffset;

            public int VertexStride;

            public int IndexStride;

            public int SubMeshCount;

            public GraphicsBuffer VertexBuffer;

            public GraphicsBuffer IndexBuffer;

            public float[] UVs;

            public List<SubMeshData> SubMeshes =
                new List<SubMeshData>();
        }

        public override void Load()
        {
            Instance =
                this;

            OutputDirectory =
                Path.Combine(
                    Paths.PluginPath,
                    "CurrencyAssetAnalyzer",
                    "BillAtlasTriangleMapper"
                );

            ReportFile =
                Path.Combine(
                    OutputDirectory,
                    "BillAtlasTriangleReport.txt"
                );

            Directory.CreateDirectory(
                OutputDirectory
            );

            InitializeReport();

            Log.LogInfo(
                "========================================"
            );

            Log.LogInfo(
                "Bill Atlas Triangle Mapper v" +
                VERSION
            );

            Log.LogInfo(
                "========================================"
            );

            Log.LogInfo(
                "Alvo: nota de 50 Dollar Pack."
            );

            Log.LogInfo(
                "Metodo: Vertex Buffer + Index Buffer via GPU."
            );

            Log.LogInfo(
                "Mesh alvo: " +
                TargetMeshName
            );

            Log.LogInfo(
                "Atlas: " +
                AtlasWidth +
                "x" +
                AtlasHeight
            );

            PatchSpawnMoney();
        }

        private static void InitializeReport()
        {
            try
            {
                using (
                    StreamWriter writer =
                        new StreamWriter(
                            Instance.ReportFile,
                            false
                        )
                )
                {
                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "BILL ATLAS TRIANGLE MAPPER"
                    );

                    writer.WriteLine(
                        "VERSION: " +
                        VERSION
                    );

                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "Target: 50 Dollar Pack"
                    );

                    writer.WriteLine(
                        "Mesh: " +
                        TargetMeshName
                    );

                    writer.WriteLine(
                        "Metodo: Vertex Buffer + Index Buffer"
                    );

                    writer.WriteLine(
                        "Atlas: " +
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

                if (
                    managerType == null
                )
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

                if (
                    spawnMoney == null
                )
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
                        typeof(
                            BillAtlasTriangleMapper
                        ),
                        nameof(
                            SpawnMoneyPostfix
                        )
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

                LogInfo(
                    "Patch de SpawnMoney aplicado."
                );
            }
            catch (
                Exception ex
            )
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
                    moneyPack == null ||
                    isCoin
                )
                {
                    return;
                }

                if (
                    AnalysisQueued
                )
                {
                    return;
                }

                GameObject root =
                    ReadMoneyPackGameObject(
                        moneyPack
                    );

                if (
                    root == null
                )
                {
                    LogError(
                        "GameObject do MoneyPack nao resolvido."
                    );

                    return;
                }

                MeshFilter[] filters =
                    root.GetComponentsInChildren<MeshFilter>(
                        true
                    );

                if (
                    filters == null
                )
                {
                    return;
                }

                Mesh selectedMesh =
                    null;

                Renderer selectedRenderer =
                    null;

                for (
                    int i = 0;
                    i < filters.Length;
                    i++
                )
                {
                    MeshFilter filter =
                        filters[i];

                    if (
                        filter == null ||
                        filter.sharedMesh == null
                    )
                    {
                        continue;
                    }

                    Mesh mesh =
                        filter.sharedMesh;

                    if (
                        !string.Equals(
                            mesh.name,
                            TargetMeshName,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        continue;
                    }

                    selectedMesh =
                        mesh;

                    try
                    {
                        selectedRenderer =
                            filter.GetComponent<Renderer>();
                    }
                    catch
                    {
                        selectedRenderer =
                            null;
                    }

                    break;
                }

                if (
                    selectedMesh == null
                )
                {
                    LogInfo(
                        "50 Dollar Pack encontrado, mas " +
                        TargetMeshName +
                        " ainda nao foi localizado."
                    );

                    return;
                }

                AnalysisQueued =
                    true;

                BillRequest request =
                    new BillRequest();

                request.Root =
                    root;

                request.Mesh =
                    selectedMesh;

                request.RendererName =
                    selectedRenderer != null
                        ? selectedRenderer.gameObject.name
                        : "unknown";

                if (
                    selectedRenderer != null
                )
                {
                    try
                    {
                        Material[] materials =
                            selectedRenderer.sharedMaterials;

                        if (
                            materials != null &&
                            materials.Length > 0
                        )
                        {
                            request.Material =
                                materials[0];

                            if (
                                request.Material != null
                            )
                            {
                                if (
                                    request.Material.HasProperty(
                                        "_BaseMap"
                                    )
                                )
                                {
                                    request.Texture =
                                        request.Material.GetTexture(
                                            "_BaseMap"
                                        );
                                }
                                else if (
                                    request.Material.HasProperty(
                                        "_MainTex"
                                    )
                                )
                                {
                                    request.Texture =
                                        request.Material.GetTexture(
                                            "_MainTex"
                                        );
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                request.VertexCount =
                    selectedMesh.vertexCount;

                request.VertexStream =
                    selectedMesh.GetVertexAttributeStream(
                        VertexAttribute.TexCoord0
                    );

                request.VertexOffset =
                    selectedMesh.GetVertexAttributeOffset(
                        VertexAttribute.TexCoord0
                    );

                request.VertexStride =
                    selectedMesh.GetVertexBufferStride(
                        request.VertexStream
                    );

                request.IndexStride =
                    GetIndexStride(
                        selectedMesh
                    );

                request.SubMeshCount =
                    selectedMesh.subMeshCount;

                for (
                    int subMesh = 0;
                    subMesh < request.SubMeshCount;
                    subMesh++
                )
                {
                    SubMeshData data =
                        new SubMeshData();

                    data.SubMesh =
                        subMesh;

                    data.IndexStart =
                        checked(
                            (int)
                            selectedMesh.GetIndexStart(
                                subMesh
                            )
                        );

                    data.IndexCount =
                        checked(
                            (int)
                            selectedMesh.GetIndexCount(
                                subMesh
                            )
                        );

                    data.BaseVertex =
                        checked(
                            (int)
                            selectedMesh.GetBaseVertex(
                                subMesh
                            )
                        );

                    data.TriangleCount =
                        data.IndexCount /
                        3;

                    request.SubMeshes.Add(
                        data
                    );
                }

                CurrentRequest =
                    request;

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "50 Dollar Pack detectado."
                );

                LogInfo(
                    "Renderer: " +
                    request.RendererName
                );

                LogInfo(
                    "Mesh: " +
                    selectedMesh.name
                );

                LogInfo(
                    "VertexCount: " +
                    request.VertexCount
                );

                LogInfo(
                    "VertexStride: " +
                    request.VertexStride
                );

                LogInfo(
                    "UVOffset: " +
                    request.VertexOffset
                );

                LogInfo(
                    "IndexStride: " +
                    request.IndexStride
                );

                LogInfo(
                    "SubMeshes: " +
                    request.SubMeshCount
                );

                RequestVertexBuffer();
            }
            catch (
                Exception ex
            )
            {
                AnalysisQueued =
                    false;

                LogError(
                    "Erro registrando nota: " +
                    ex
                );
            }
        }

        private static void RequestVertexBuffer()
        {
            try
            {
                BillRequest request =
                    CurrentRequest;

                if (
                    request == null
                )
                {
                    Finish();
                    return;
                }

                request.VertexBuffer =
                    request.Mesh.GetVertexBuffer(
                        request.VertexStream
                    );

                if (
                    request.VertexBuffer == null ||
                    !request.VertexBuffer.IsValid()
                )
                {
                    LogError(
                        "VertexBuffer invalido."
                    );

                    Finish();

                    return;
                }

                CurrentStage =
                    ReadbackStage.Vertices;

                ReadbackPending =
                    true;

                AsyncGPUReadback.Request(
                    request.VertexBuffer,
                    DelegateSupport.ConvertDelegate<
                        Il2CppSystem.Action<AsyncGPUReadbackRequest>
                    >(
                        new Action<AsyncGPUReadbackRequest>(
                            VertexReadbackCompleted
                        )
                    )
                );

                LogInfo(
                    "Vertex Readback solicitado."
                );
            }
            catch (
                Exception ex
            )
            {
                LogError(
                    "Erro solicitando Vertex Readback: " +
                    ex
                );

                Finish();
            }
        }

        private static void VertexReadbackCompleted(
            AsyncGPUReadbackRequest gpuRequest
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
                    Finish();
                    return;
                }

                if (
                    gpuRequest.hasError
                )
                {
                    LogError(
                        "Vertex Readback falhou."
                    );

                    Finish();

                    return;
                }

                NativeArray<byte> data =
                    gpuRequest.GetData<byte>(
                        0
                    );

                if (
                    !data.IsCreated ||
                    data.Length <= 0
                )
                {
                    LogError(
                        "Vertex Readback vazio."
                    );

                    Finish();

                    return;
                }

                CurrentRequest.UVs =
                    ReadUVArray(
                        data,
                        CurrentRequest.VertexCount,
                        CurrentRequest.VertexStride,
                        CurrentRequest.VertexOffset
                    );

                ReleaseVertexBuffer();

                RequestIndexBuffer();
            }
            catch (
                Exception ex
            )
            {
                LogError(
                    "Erro processando Vertex Readback: " +
                    ex
                );

                Finish();
            }
        }

        private static float[] ReadUVArray(
            NativeArray<byte> data,
            int vertexCount,
            int stride,
            int offset
        )
        {
            float[] result =
                new float[
                    vertexCount *
                    2
                ];

            if (
                stride <= 0 ||
                offset < 0
            )
            {
                return result;
            }

            int availableVertices =
                data.Length /
                stride;

            int count =
                Math.Min(
                    vertexCount,
                    availableVertices
                );

            for (
                int i = 0;
                i < count;
                i++
            )
            {
                int baseOffset =
                    i *
                    stride;

                if (
                    baseOffset +
                    offset +
                    7 >=
                    data.Length
                )
                {
                    break;
                }

                result[
                    i * 2
                ] =
                    ReadFloat(
                        data,
                        baseOffset +
                        offset
                    );

                result[
                    i * 2 + 1
                ] =
                    ReadFloat(
                        data,
                        baseOffset +
                        offset +
                        4
                    );
            }

            return result;
        }

        private static void RequestIndexBuffer()
        {
            try
            {
                BillRequest request =
                    CurrentRequest;

                request.IndexBuffer =
                    request.Mesh.GetIndexBuffer();

                if (
                    request.IndexBuffer == null ||
                    !request.IndexBuffer.IsValid()
                )
                {
                    LogError(
                        "IndexBuffer invalido."
                    );

                    Finish();

                    return;
                }

                CurrentStage =
                    ReadbackStage.Indices;

                ReadbackPending =
                    true;

                AsyncGPUReadback.Request(
                    request.IndexBuffer,
                    DelegateSupport.ConvertDelegate<
                        Il2CppSystem.Action<AsyncGPUReadbackRequest>
                    >(
                        new Action<AsyncGPUReadbackRequest>(
                            IndexReadbackCompleted
                        )
                    )
                );

                LogInfo(
                    "Index Readback solicitado."
                );
            }
            catch (
                Exception ex
            )
            {
                LogError(
                    "Erro solicitando Index Readback: " +
                    ex
                );

                Finish();
            }
        }

        private static void IndexReadbackCompleted(
            AsyncGPUReadbackRequest gpuRequest
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
                    Finish();
                    return;
                }

                if (
                    gpuRequest.hasError
                )
                {
                    LogError(
                        "Index Readback falhou."
                    );

                    Finish();

                    return;
                }

                NativeArray<byte> data =
                    gpuRequest.GetData<byte>(
                        0
                    );

                if (
                    !data.IsCreated ||
                    data.Length <= 0
                )
                {
                    LogError(
                        "Index Readback vazio."
                    );

                    Finish();

                    return;
                }

                for (
                    int s = 0;
                    s < CurrentRequest.SubMeshes.Count;
                    s++
                )
                {
                    SubMeshData subMesh =
                        CurrentRequest.SubMeshes[s];

                    subMesh.Indices =
                        ReadSubMeshIndices(
                            data,
                            CurrentRequest.IndexStride,
                            subMesh.IndexStart,
                            subMesh.IndexCount,
                            subMesh.BaseVertex
                        );
                }

                WriteBillReport(
                    CurrentRequest
                );

                GenerateMask(
                    CurrentRequest
                );
            }
            catch (
                Exception ex
            )
            {
                LogError(
                    "Erro processando Index Readback: " +
                    ex
                );

                Finish();
            }
        }

        private static int[] ReadSubMeshIndices(
            NativeArray<byte> data,
            int indexStride,
            int indexStart,
            int indexCount,
            int baseVertex
        )
        {
            int[] result =
                new int[
                    indexCount
                ];

            for (
                int i = 0;
                i < indexCount;
                i++
            )
            {
                int absoluteIndex =
                    indexStart +
                    i;

                int byteOffset =
                    absoluteIndex *
                    indexStride;

                if (
                    byteOffset +
                    indexStride >
                    data.Length
                )
                {
                    result[i] =
                        -1;

                    continue;
                }

                int rawIndex;

                if (
                    indexStride ==
                    2
                )
                {
                    rawIndex =
                        data[
                            byteOffset
                        ] |
                        (
                            data[
                                byteOffset +
                                1
                            ]
                            <<
                            8
                        );
                }
                else
                {
                    rawIndex =
                        data[
                            byteOffset
                        ] |
                        (
                            data[
                                byteOffset +
                                1
                            ]
                            <<
                            8
                        ) |
                        (
                            data[
                                byteOffset +
                                2
                            ]
                            <<
                            16
                        ) |
                        (
                            data[
                                byteOffset +
                                3
                            ]
                            <<
                            24
                        );
                }

                result[i] =
                    rawIndex +
                    baseVertex;
            }

            return result;
        }

        private static void WriteBillReport(
            BillRequest request
        )
        {
            try
            {
                float[] uvs =
                    request.UVs;

                using (
                    StreamWriter writer =
                        new StreamWriter(
                            Instance.ReportFile,
                            true
                        )
                )
                {
                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "BILL: 50 DOLLAR"
                    );

                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "Root: " +
                        (
                            request.Root != null
                                ? request.Root.name
                                : "null"
                        )
                    );

                    writer.WriteLine(
                        "Renderer: " +
                        request.RendererName
                    );

                    writer.WriteLine(
                        "Mesh: " +
                        request.Mesh.name
                    );

                    writer.WriteLine(
                        "VertexCount: " +
                        request.VertexCount
                    );

                    writer.WriteLine(
                        "UV Array Count: " +
                        (
                            uvs != null
                                ? uvs.Length / 2
                                : 0
                        )
                    );

                    writer.WriteLine(
                        "SubMeshCount: " +
                        request.SubMeshCount
                    );

                    writer.WriteLine();

                    float globalMinU =
                        float.MaxValue;

                    float globalMaxU =
                        float.MinValue;

                    float globalMinV =
                        float.MaxValue;

                    float globalMaxV =
                        float.MinValue;

                    int globalTriangles =
                        0;

                    for (
                        int s = 0;
                        s < request.SubMeshes.Count;
                        s++
                    )
                    {
                        SubMeshData subMesh =
                            request.SubMeshes[s];

                        writer.WriteLine(
                            "----------------------------------------"
                        );

                        writer.WriteLine(
                            "SUBMESH: " +
                            subMesh.SubMesh
                        );

                        writer.WriteLine(
                            "IndexStart: " +
                            subMesh.IndexStart
                        );

                        writer.WriteLine(
                            "IndexCount: " +
                            subMesh.IndexCount
                        );

                        writer.WriteLine(
                            "BaseVertex: " +
                            subMesh.BaseVertex
                        );

                        writer.WriteLine(
                            "TriangleCount: " +
                            subMesh.TriangleCount
                        );

                        float minU =
                            float.MaxValue;

                        float maxU =
                            float.MinValue;

                        float minV =
                            float.MaxValue;

                        float maxV =
                            float.MinValue;

                        int validTriangles =
                            0;

                        if (
                            subMesh.Indices != null
                        )
                        {
                            int triangleCount =
                                subMesh.Indices.Length /
                                3;

                            for (
                                int triangle = 0;
                                triangle < triangleCount;
                                triangle++
                            )
                            {
                                int i0 =
                                    subMesh.Indices[
                                        triangle * 3
                                    ];

                                int i1 =
                                    subMesh.Indices[
                                        triangle * 3 + 1
                                    ];

                                int i2 =
                                    subMesh.Indices[
                                        triangle * 3 + 2
                                    ];

                                if (
                                    !ValidVertex(
                                        i0,
                                        uvs
                                    ) ||
                                    !ValidVertex(
                                        i1,
                                        uvs
                                    ) ||
                                    !ValidVertex(
                                        i2,
                                        uvs
                                    )
                                )
                                {
                                    continue;
                                }

                                float u0 =
                                    uvs[i0 * 2];

                                float v0 =
                                    uvs[i0 * 2 + 1];

                                float u1 =
                                    uvs[i1 * 2];

                                float v1 =
                                    uvs[i1 * 2 + 1];

                                float u2 =
                                    uvs[i2 * 2];

                                float v2 =
                                    uvs[i2 * 2 + 1];

                                if (
                                    !ValidFloat(u0) ||
                                    !ValidFloat(v0) ||
                                    !ValidFloat(u1) ||
                                    !ValidFloat(v1) ||
                                    !ValidFloat(u2) ||
                                    !ValidFloat(v2)
                                )
                                {
                                    continue;
                                }

                                validTriangles++;

                                UpdateMinMax(
                                    u0,
                                    v0,
                                    ref minU,
                                    ref maxU,
                                    ref minV,
                                    ref maxV
                                );

                                UpdateMinMax(
                                    u1,
                                    v1,
                                    ref minU,
                                    ref maxU,
                                    ref minV,
                                    ref maxV
                                );

                                UpdateMinMax(
                                    u2,
                                    v2,
                                    ref minU,
                                    ref maxU,
                                    ref minV,
                                    ref maxV
                                );

                                writer.WriteLine(
                                    "T" +
                                    triangle.ToString(
                                        "000"
                                    ) +
                                    " | I=" +
                                    i0 +
                                    "," +
                                    i1 +
                                    "," +
                                    i2 +
                                    " | " +
                                    "UV0=(" +
                                    FormatFloat(u0) +
                                    "," +
                                    FormatFloat(v0) +
                                    ") " +
                                    "UV1=(" +
                                    FormatFloat(u1) +
                                    "," +
                                    FormatFloat(v1) +
                                    ") " +
                                    "UV2=(" +
                                    FormatFloat(u2) +
                                    "," +
                                    FormatFloat(v2) +
                                    ")"
                                );
                            }
                        }

                        writer.WriteLine();

                        writer.WriteLine(
                            "ValidTriangles: " +
                            validTriangles
                        );

                        writer.WriteLine(
                            "UV Min: (" +
                            FormatFloat(minU) +
                            ", " +
                            FormatFloat(minV) +
                            ")"
                        );

                        writer.WriteLine(
                            "UV Max: (" +
                            FormatFloat(maxU) +
                            ", " +
                            FormatFloat(maxV) +
                            ")"
                        );

                        writer.WriteLine(
                            "Pixel Bounds:"
                        );

                        writer.WriteLine(
                            "X: " +
                            FormatFloat(
                                minU *
                                AtlasWidth
                            ) +
                            " -> " +
                            FormatFloat(
                                maxU *
                                AtlasWidth
                            )
                        );

                        writer.WriteLine(
                            "Y: " +
                            FormatFloat(
                                (
                                    1f -
                                    maxV
                                ) *
                                AtlasHeight
                            ) +
                            " -> " +
                            FormatFloat(
                                (
                                    1f -
                                    minV
                                ) *
                                AtlasHeight
                            )
                        );

                        if (
                            validTriangles >
                            0
                        )
                        {
                            globalTriangles +=
                                validTriangles;

                            UpdateMinMax(
                                minU,
                                minV,
                                ref globalMinU,
                                ref globalMaxU,
                                ref globalMinV,
                                ref globalMaxV
                            );

                            UpdateMinMax(
                                maxU,
                                maxV,
                                ref globalMinU,
                                ref globalMaxU,
                                ref globalMinV,
                                ref globalMaxV
                            );
                        }
                    }

                    writer.WriteLine();

                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "GLOBAL SUMMARY"
                    );

                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "ValidTriangles: " +
                        globalTriangles
                    );

                    writer.WriteLine(
                        "Global UV Min: (" +
                        FormatFloat(globalMinU) +
                        ", " +
                        FormatFloat(globalMinV) +
                        ")"
                    );

                    writer.WriteLine(
                        "Global UV Max: (" +
                        FormatFloat(globalMaxU) +
                        ", " +
                        FormatFloat(globalMaxV) +
                        ")"
                    );

                    writer.WriteLine(
                        "Global Pixel Bounds:"
                    );

                    writer.WriteLine(
                        "X: " +
                        FormatFloat(
                            globalMinU *
                            AtlasWidth
                        ) +
                        " -> " +
                        FormatFloat(
                            globalMaxU *
                            AtlasWidth
                        )
                    );

                    writer.WriteLine(
                        "Y: " +
                        FormatFloat(
                            (
                                1f -
                                globalMaxV
                            ) *
                            AtlasHeight
                        ) +
                        " -> " +
                        FormatFloat(
                            (
                                1f -
                                globalMinV
                            ) *
                            AtlasHeight
                        )
                    );

                    writer.WriteLine();
                }

                LogInfo(
                    "Relatorio da nota gerado."
                );
            }
            catch (
                Exception ex
            )
            {
                LogError(
                    "Erro escrevendo relatorio da nota: " +
                    ex
                );
            }
        }

        private static void GenerateMask(
            BillRequest request
        )
        {
            try
            {
                string maskPath =
                    Path.Combine(
                        Instance.OutputDirectory,
                        "BILL_50_Dollar_MASK.png"
                    );

                System.Drawing.Bitmap bitmap =
                    new System.Drawing.Bitmap(
                        AtlasWidth,
                        AtlasHeight,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb
                    );

                using (
                    System.Drawing.Graphics graphics =
                        System.Drawing.Graphics.FromImage(
                            bitmap
                        )
                )
                {
                    graphics.Clear(
                        System.Drawing.Color.Transparent
                    );
                }

                int markedPixels =
                    0;

                for (
                    int s = 0;
                    s < request.SubMeshes.Count;
                    s++
                )
                {
                    SubMeshData subMesh =
                        request.SubMeshes[s];

                    if (
                        subMesh.Indices == null ||
                        request.UVs == null
                    )
                    {
                        continue;
                    }

                    int triangleCount =
                        subMesh.Indices.Length /
                        3;

                    for (
                        int triangle = 0;
                        triangle < triangleCount;
                        triangle++
                    )
                    {
                        int i0 =
                            subMesh.Indices[
                                triangle * 3
                            ];

                        int i1 =
                            subMesh.Indices[
                                triangle * 3 + 1
                            ];

                        int i2 =
                            subMesh.Indices[
                                triangle * 3 + 2
                            ];

                        if (
                            !ValidVertex(
                                i0,
                                request.UVs
                            ) ||
                            !ValidVertex(
                                i1,
                                request.UVs
                            ) ||
                            !ValidVertex(
                                i2,
                                request.UVs
                            )
                        )
                        {
                            continue;
                        }

                        float x0 =
                            request.UVs[i0 * 2] *
                            (AtlasWidth - 1);

                        float y0 =
                            (
                                1f -
                                request.UVs[i0 * 2 + 1]
                            ) *
                            (AtlasHeight - 1);

                        float x1 =
                            request.UVs[i1 * 2] *
                            (AtlasWidth - 1);

                        float y1 =
                            (
                                1f -
                                request.UVs[i1 * 2 + 1]
                            ) *
                            (AtlasHeight - 1);

                        float x2 =
                            request.UVs[i2 * 2] *
                            (AtlasWidth - 1);

                        float y2 =
                            (
                                1f -
                                request.UVs[i2 * 2 + 1]
                            ) *
                            (AtlasHeight - 1);

                        float minX =
                            MathF.Min(
                                x0,
                                MathF.Min(
                                    x1,
                                    x2
                                )
                            );

                        float maxX =
                            MathF.Max(
                                x0,
                                MathF.Max(
                                    x1,
                                    x2
                                )
                            );

                        float minY =
                            MathF.Min(
                                y0,
                                MathF.Min(
                                    y1,
                                    y2
                                )
                            );

                        float maxY =
                            MathF.Max(
                                y0,
                                MathF.Max(
                                    y1,
                                    y2
                                )
                            );

                        int startX =
                            Math.Max(
                                0,
                                (int)Math.Floor(
                                    minX
                                )
                            );

                        int endX =
                            Math.Min(
                                AtlasWidth - 1,
                                (int)Math.Ceiling(
                                    maxX
                                )
                            );

                        int startY =
                            Math.Max(
                                0,
                                (int)Math.Floor(
                                    minY
                                )
                            );

                        int endY =
                            Math.Min(
                                AtlasHeight - 1,
                                (int)Math.Ceiling(
                                    maxY
                                )
                            );

                        float denominator =
                            (
                                y1 -
                                y2
                            ) *
                            (
                                x0 -
                                x2
                            ) +
                            (
                                x2 -
                                x1
                            ) *
                            (
                                y0 -
                                y2
                            );

                        if (
                            Math.Abs(
                                denominator
                            ) <
                            0.000001f
                        )
                        {
                            continue;
                        }

                        for (
                            int y = startY;
                            y <= endY;
                            y++
                        )
                        {
                            for (
                                int x = startX;
                                x <= endX;
                                x++
                            )
                            {
                                float px =
                                    x +
                                    0.5f;

                                float py =
                                    y +
                                    0.5f;

                                float a =
                                    (
                                        (
                                            y1 -
                                            y2
                                        ) *
                                        (
                                            px -
                                            x2
                                        ) +
                                        (
                                            x2 -
                                            x1
                                        ) *
                                        (
                                            py -
                                            y2
                                        )
                                    ) /
                                    denominator;

                                float b =
                                    (
                                        (
                                            y2 -
                                            y0
                                        ) *
                                        (
                                            px -
                                            x2
                                        ) +
                                        (
                                            x0 -
                                            x2
                                        ) *
                                        (
                                            py -
                                            y2
                                        )
                                    ) /
                                    denominator;

                                float c =
                                    1f -
                                    a -
                                    b;

                                if (
                                    a >= -0.0001f &&
                                    b >= -0.0001f &&
                                    c >= -0.0001f
                                )
                                {
                                    System.Drawing.Color current =
                                        bitmap.GetPixel(
                                            x,
                                            y
                                        );

                                    if (
                                        current.A <
                                        255
                                    )
                                    {
                                        bitmap.SetPixel(
                                            x,
                                            y,
                                            System.Drawing.Color.White
                                        );

                                        markedPixels++;
                                    }
                                }
                            }
                        }
                    }
                }

                bitmap.Save(
                    maskPath,
                    System.Drawing.Imaging.ImageFormat.Png
                );

                bitmap.Dispose();

                LogInfo(
                    "Mascara da nota gerada:"
                );

                LogInfo(
                    maskPath
                );

                LogInfo(
                    "Pixels marcados: " +
                    markedPixels
                );

                WriteMaskReport(
                    request,
                    maskPath,
                    markedPixels
                );
            }
            catch (
                Exception ex
            )
            {
                LogError(
                    "Erro gerando mascara da nota: " +
                    ex
                );
            }
            finally
            {
                Finish();
            }
        }

        private static void WriteMaskReport(
            BillRequest request,
            string maskPath,
            int markedPixels
        )
        {
            try
            {
                using (
                    StreamWriter writer =
                        new StreamWriter(
                            Instance.ReportFile,
                            true
                        )
                )
                {
                    writer.WriteLine(
                        "MASK"
                    );

                    writer.WriteLine(
                        "----------------------------------------"
                    );

                    writer.WriteLine(
                        "File: " +
                        maskPath
                    );

                    writer.WriteLine(
                        "Dimensions: " +
                        AtlasWidth +
                        "x" +
                        AtlasHeight
                    );

                    writer.WriteLine(
                        "MarkedPixels: " +
                        markedPixels
                    );

                    writer.WriteLine();

                    writer.WriteLine(
                        "ANALYSIS COMPLETE"
                    );
                }
            }
            catch
            {
            }
        }

        private static bool ValidVertex(
            int index,
            float[] uvs
        )
        {
            if (
                index < 0 ||
                uvs == null
            )
            {
                return false;
            }

            return
                (
                    index *
                    2 +
                    1
                ) <
                uvs.Length;
        }

        private static bool ValidFloat(
            float value
        )
        {
            return
                !float.IsNaN(
                    value
                ) &&
                !float.IsInfinity(
                    value
                );
        }

        private static void UpdateMinMax(
            float u,
            float v,
            ref float minU,
            ref float maxU,
            ref float minV,
            ref float maxV
        )
        {
            if (
                u < minU
            )
            {
                minU =
                    u;
            }

            if (
                u > maxU
            )
            {
                maxU =
                    u;
            }

            if (
                v < minV
            )
            {
                minV =
                    v;
            }

            if (
                v > maxV
            )
            {
                maxV =
                    v;
            }
        }

        private static int GetIndexStride(
            Mesh mesh
        )
        {
            try
            {
                return mesh.indexFormat ==
                    IndexFormat.UInt32
                    ? 4
                    : 2;
            }
            catch
            {
                return 2;
            }
        }

        private static void ReleaseVertexBuffer()
        {
            try
            {
                if (
                    CurrentRequest != null &&
                    CurrentRequest.VertexBuffer != null
                )
                {
                    CurrentRequest.VertexBuffer.Release();
                }
            }
            catch
            {
            }

            if (
                CurrentRequest != null
            )
            {
                CurrentRequest.VertexBuffer =
                    null;
            }
        }

        private static void ReleaseIndexBuffer()
        {
            try
            {
                if (
                    CurrentRequest != null &&
                    CurrentRequest.IndexBuffer != null
                )
                {
                    CurrentRequest.IndexBuffer.Release();
                }
            }
            catch
            {
            }

            if (
                CurrentRequest != null
            )
            {
                CurrentRequest.IndexBuffer =
                    null;
            }
        }

        private static void Finish()
        {
            try
            {
                ReleaseVertexBuffer();

                ReleaseIndexBuffer();
            }
            catch
            {
            }

            CurrentStage =
                ReadbackStage.None;

            ReadbackPending =
                false;

            CurrentRequest =
                null;

            AnalysisQueued =
                false;
        }

        private static float ReadFloat(
            NativeArray<byte> data,
            int offset
        )
        {
            int bits =
                data[offset] |
                (
                    data[offset + 1] <<
                    8
                ) |
                (
                    data[offset + 2] <<
                    16
                ) |
                (
                    data[offset + 3] <<
                    24
                );

            return BitConverter.Int32BitsToSingle(
                bits
            );
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

                if (
                    property != null
                )
                {
                    object value =
                        property.GetValue(
                            moneyPack,
                            null
                        );

                    GameObject result =
                        value as GameObject;

                    if (
                        result != null
                    )
                    {
                        return result;
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

                if (
                    type != null
                )
                {
                    return type;
                }
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

                    if (
                        type != null
                    )
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
                        method == null ||
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
            catch (
                Exception ex
            )
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
                CultureInfo.InvariantCulture
            );
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

            return true;
        }
    }
}




