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
    public class AtlasTriangleMapper : BasePlugin
    {
        private const string GUID =
            "br.monsterk1llerbr.supermarketsimulator.atlastrianglemapper";

        private const string NAME =
            "Currency Atlas Triangle Mapper";

        private const string VERSION =
            "1.0.0";

        private const int AtlasWidth =
            2048;

        private const int AtlasHeight =
            2048;

        private static AtlasTriangleMapper Instance;

        private Harmony HarmonyInstance;

        private static string OutputDirectory;

        private static string ReportFile;

        private static readonly HashSet<string> AnalyzedMeshes =
            new HashSet<string>();

        private static readonly Queue<MeshRequest> PendingMeshes =
            new Queue<MeshRequest>();

        private static MeshRequest CurrentRequest;

        private static bool ReadbackPending;

        private static bool Processing;

        private enum ReadbackStage
        {
            None,
            Vertices,
            Indices
        }

        private static ReadbackStage CurrentStage =
            ReadbackStage.None;

        private class MeshRequest
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

            public int IndexCount;

            public int IndexStride;

            public GraphicsBuffer VertexBuffer;

            public GraphicsBuffer IndexBuffer;

            public float[] UVs;

            public int[] Indices;
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
                        "AtlasTriangleMapper"
                    );

                ReportFile =
                    Path.Combine(
                        OutputDirectory,
                        "AtlasTriangleReport.txt"
                    );

                Directory.CreateDirectory(
                    OutputDirectory
                );

                InitializeReport();

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "Currency Atlas Triangle Mapper v1.0.0"
                );

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "Metodo: GPU Vertex Buffer + Index Buffer."
                );

                LogInfo(
                    "Metodo de leitura: AsyncGPUReadback."
                );

                LogInfo(
                    "Objetivo: reconstruir os triangulos UV reais."
                );

                LogInfo(
                    "Atlas: " +
                    AtlasWidth +
                    "x" +
                    AtlasHeight
                );

                PatchSpawnMoney();
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro inicializando Atlas Triangle Mapper: " +
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
                        "CURRENCY ATLAS TRIANGLE MAPPER"
                    );

                    writer.WriteLine(
                        "VERSION: " +
                        VERSION
                    );

                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "Objetivo: reconstruir os triangulos UV."
                    );

                    writer.WriteLine(
                        "Metodo: Vertex Buffer + Index Buffer via GPU."
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
                        typeof(AtlasTriangleMapper),
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
                    moneyPack == null ||
                    !isCoin
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
                    !AnalyzedMeshes.Add(
                        meshName
                    )
                )
                {
                    return;
                }

                Material material =
                    FindCoinMaterial(
                        root
                    );

                Texture texture =
                    FindCoinTexture(
                        material
                    );

                MeshRequest request =
                    new MeshRequest();

                request.CoinName =
                    meshName;

                request.Value =
                    ReadMoneyPackValue(
                        moneyPack
                    );

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

                request.IndexCount =
                    (int)mesh.GetIndexCount(
                        0
                    );

                request.IndexStride =
                    GetIndexStride(
                        mesh
                    );

                PendingMeshes.Enqueue(
                    request
                );

                LogInfo(
                    "Moeda adicionada a fila: " +
                    meshName
                );

                StartNextMesh();
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro registrando moeda: " +
                    ex
                );
            }
        }

        private static void StartNextMesh()
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

                    StartNextMesh();

                    return;
                }

                Processing =
                    true;

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "Analise: " +
                    CurrentRequest.CoinName
                );

                LogInfo(
                    "Value: " +
                    FormatFloat(
                        CurrentRequest.Value
                    )
                );

                LogInfo(
                    "VertexCount: " +
                    CurrentRequest.VertexCount
                );

                LogInfo(
                    "VertexStride: " +
                    CurrentRequest.VertexStride
                );

                LogInfo(
                    "UVOffset: " +
                    CurrentRequest.UVOffset
                );

                LogInfo(
                    "IndexCount: " +
                    CurrentRequest.IndexCount
                );

                LogInfo(
                    "IndexStride: " +
                    CurrentRequest.IndexStride
                );

                RequestVertexBuffer();
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro iniciando analise: " +
                    ex
                );

                FinishCurrentMesh();
            }
        }

        private static void RequestVertexBuffer()
        {
            try
            {
                MeshRequest request =
                    CurrentRequest;

                request.VertexBuffer =
                    request.Mesh.GetVertexBuffer(
                        request.UVStream
                    );

                if (
                    request.VertexBuffer == null
                )
                {
                    LogError(
                        "VertexBuffer NULL."
                    );

                    FinishCurrentMesh();

                    return;
                }

                if (
                    !request.VertexBuffer.IsValid()
                )
                {
                    LogError(
                        "VertexBuffer invalido."
                    );

                    FinishCurrentMesh();

                    return;
                }

                CurrentStage =
                    ReadbackStage.Vertices;

                ReadbackPending =
                    true;

                AsyncGPUReadback.Request(
                    request.VertexBuffer,
                    DelegateSupport.ConvertDelegate<Il2CppSystem.Action<AsyncGPUReadbackRequest>>(
                        new Action<AsyncGPUReadbackRequest>(
                            VertexReadbackCompleted
                        )
                    )
                );

                LogInfo(
                    "Vertex Readback solicitado."
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro solicitando Vertex Readback: " +
                    ex
                );

                FinishCurrentMesh();
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
                    return;
                }

                if (
                    gpuRequest.hasError
                )
                {
                    LogError(
                        "Vertex Readback falhou."
                    );

                    FinishCurrentMesh();

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

                    FinishCurrentMesh();

                    return;
                }

                CurrentRequest.UVs =
                    ReadUVArray(
                        data,
                        CurrentRequest.VertexCount,
                        CurrentRequest.VertexStride,
                        CurrentRequest.UVOffset
                    );

                ReleaseVertexBuffer();

                RequestIndexBuffer();
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro processando Vertex Readback: " +
                    ex
                );

                FinishCurrentMesh();
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

                result[
                    i *
                    2
                ] =
                    ReadFloat(
                        data,
                        baseOffset +
                        offset
                    );

                result[
                    i *
                    2 +
                    1
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
                MeshRequest request =
                    CurrentRequest;

                request.IndexBuffer =
                    request.Mesh.GetIndexBuffer();

                if (
                    request.IndexBuffer == null
                )
                {
                    LogError(
                        "IndexBuffer NULL."
                    );

                    FinishCurrentMesh();

                    return;
                }

                if (
                    !request.IndexBuffer.IsValid()
                )
                {
                    LogError(
                        "IndexBuffer invalido."
                    );

                    FinishCurrentMesh();

                    return;
                }

                CurrentStage =
                    ReadbackStage.Indices;

                ReadbackPending =
                    true;

                AsyncGPUReadback.Request(
                    request.IndexBuffer,
                    DelegateSupport.ConvertDelegate<Il2CppSystem.Action<AsyncGPUReadbackRequest>>(
                        new Action<AsyncGPUReadbackRequest>(
                            IndexReadbackCompleted
                        )
                    )
                );

                LogInfo(
                    "Index Readback solicitado."
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro solicitando Index Readback: " +
                    ex
                );

                FinishCurrentMesh();
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
                    return;
                }

                if (
                    gpuRequest.hasError
                )
                {
                    LogError(
                        "Index Readback falhou."
                    );

                    FinishCurrentMesh();

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

                    FinishCurrentMesh();

                    return;
                }

                CurrentRequest.Indices =
                    ReadIndexArray(
                        data,
                        CurrentRequest.IndexStride,
                        CurrentRequest.IndexCount
                    );

                ReleaseIndexBuffer();

                WriteTriangleReport(
                    CurrentRequest
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro processando Index Readback: " +
                    ex
                );

                FinishCurrentMesh();
            }
        }

        private static int[] ReadIndexArray(
            NativeArray<byte> data,
            int indexStride,
            int indexCount
        )
        {
            int[] result =
                new int[
                    indexCount
                ];

            if (
                indexStride == 2
            )
            {
                int available =
                    data.Length /
                    2;

                int count =
                    Math.Min(
                        indexCount,
                        available
                    );

                for (
                    int i = 0;
                    i < count;
                    i++
                )
                {
                    result[i] =
                        data[
                            i *
                            2
                        ] |
                        (
                            data[
                                i *
                                2 +
                                1
                            ]
                            <<
                            8
                        );
                }

                return result;
            }

            if (
                indexStride == 4
            )
            {
                int available =
                    data.Length /
                    4;

                int count =
                    Math.Min(
                        indexCount,
                        available
                    );

                for (
                    int i = 0;
                    i < count;
                    i++
                )
                {
                    int offset =
                        i *
                        4;

                    result[i] =
                        data[
                            offset
                        ] |
                        (
                            data[
                                offset +
                                1
                            ]
                            <<
                            8
                        ) |
                        (
                            data[
                                offset +
                                2
                            ]
                            <<
                            16
                        ) |
                        (
                            data[
                                offset +
                                3
                            ]
                            <<
                            24
                        );
                }

                return result;
            }

            return result;
        }

        private static void WriteTriangleReport(
            MeshRequest request
        )
        {
            try
            {
                float[] uvs =
                    request.UVs;

                int[] indices =
                    request.Indices;

                if (
                    uvs == null ||
                    indices == null
                )
                {
                    WriteReport(
                        "ERROR: UV ou indices ausentes."
                    );

                    FinishCurrentMesh();

                    return;
                }

                int triangleCount =
                    indices.Length /
                    3;

                int validTriangleCount =
                    0;

                float totalUVArea =
                    0f;

                float minU =
                    float.MaxValue;

                float maxU =
                    float.MinValue;

                float minV =
                    float.MaxValue;

                float maxV =
                    float.MinValue;

                WriteReport(
                    ""
                );

                WriteReport(
                    "========================================"
                );

                WriteReport(
                    "COIN: " +
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
                    "VertexCount: " +
                    request.VertexCount
                );

                WriteReport(
                    "UV Array Count: " +
                    (
                        uvs.Length /
                        2
                    )
                );

                WriteReport(
                    "IndexCount: " +
                    indices.Length
                );

                WriteReport(
                    "TriangleCount: " +
                    triangleCount
                );

                WriteReport(
                    "IndexStride: " +
                    request.IndexStride
                );

                WriteReport(
                    ""
                );

                WriteReport(
                    "TRIANGLES"
                );

                WriteReport(
                    "----------------------------------------"
                );

                for (
                    int triangle = 0;
                    triangle < triangleCount;
                    triangle++
                )
                {
                    int indexBase =
                        triangle *
                        3;

                    int i0 =
                        indices[
                            indexBase
                        ];

                    int i1 =
                        indices[
                            indexBase +
                            1
                        ];

                    int i2 =
                        indices[
                            indexBase +
                            2
                        ];

                    if (
                        !IsValidVertexIndex(
                            i0,
                            uvs
                        ) ||
                        !IsValidVertexIndex(
                            i1,
                            uvs
                        ) ||
                        !IsValidVertexIndex(
                            i2,
                            uvs
                        )
                    )
                    {
                        WriteReport(
                            "Triangle " +
                            triangle +
                            ": INVALID INDEX"
                        );

                        continue;
                    }

                    float u0 =
                        uvs[
                            i0 *
                            2
                        ];

                    float v0 =
                        uvs[
                            i0 *
                            2 +
                            1
                        ];

                    float u1 =
                        uvs[
                            i1 *
                            2
                        ];

                    float v1 =
                        uvs[
                            i1 *
                            2 +
                            1
                        ];

                    float u2 =
                        uvs[
                            i2 *
                            2
                        ];

                    float v2 =
                        uvs[
                            i2 *
                            2 +
                            1
                        ];

                    if (
                        !IsValidFloat(
                            u0
                        ) ||
                        !IsValidFloat(
                            v0
                        ) ||
                        !IsValidFloat(
                            u1
                        ) ||
                        !IsValidFloat(
                            v1
                        ) ||
                        !IsValidFloat(
                            u2
                        ) ||
                        !IsValidFloat(
                            v2
                        )
                    )
                    {
                        WriteReport(
                            "Triangle " +
                            triangle +
                            ": INVALID UV"
                        );

                        continue;
                    }

                    validTriangleCount++;

                    float triangleArea =
                        Math.Abs(
                            (
                                u1 -
                                u0
                            ) *
                            (
                                v2 -
                                v0
                            ) -
                            (
                                u2 -
                                u0
                            ) *
                            (
                                v1 -
                                v0
                            )
                        )
                        *
                        0.5f;

                    totalUVArea +=
                        triangleArea;

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

                    WriteReport(
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
                        FormatFloat(
                            u0
                        ) +
                        "," +
                        FormatFloat(
                            v0
                        ) +
                        ") " +
                        "UV1=(" +
                        FormatFloat(
                            u1
                        ) +
                        "," +
                        FormatFloat(
                            v1
                        ) +
                        ") " +
                        "UV2=(" +
                        FormatFloat(
                            u2
                        ) +
                        "," +
                        FormatFloat(
                            v2
                        ) +
                        ") " +
                        "Area=" +
                        FormatFloat(
                            triangleArea
                        )
                    );
                }

                WriteReport(
                    ""
                );

                WriteReport(
                    "TRIANGLE SUMMARY"
                );

                WriteReport(
                    "----------------------------------------"
                );

                WriteReport(
                    "Valid Triangle Count: " +
                    validTriangleCount
                );

                WriteReport(
                    "Expected Triangle Count: " +
                    triangleCount
                );

                WriteReport(
                    "Total UV Area: " +
                    FormatFloat(
                        totalUVArea
                    )
                );

                WriteReport(
                    "UV Min: (" +
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
                    "UV Max: (" +
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
                    "Atlas Pixel Bounds:"
                );

                WriteReport(
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

                WriteReport(
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

                WriteReport(
                    "========================================"
                );

                WriteReport(
                    ""
                );

                LogInfo(
                    "Triangulos reconstruidos: " +
                    request.CoinName +
                    " | " +
                    validTriangleCount +
                    "/" +
                    triangleCount
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro gerando relatorio de triangulos: " +
                    ex
                );
            }
            finally
            {
                FinishCurrentMesh();
            }
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
            if (u < minU)
                minU = u;

            if (u > maxU)
                maxU = u;

            if (v < minV)
                minV = v;

            if (v > maxV)
                maxV = v;
        }

        private static bool IsValidVertexIndex(
            int index,
            float[] uvs
        )
        {
            if (
                index < 0
            )
            {
                return false;
            }

            return (
                index *
                2 +
                1
            ) < uvs.Length;
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

        private static void FinishCurrentMesh()
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

            Processing =
                false;

            CurrentRequest =
                null;

            StartNextMesh();
        }

        private static float ReadFloat(
            NativeArray<byte> data,
            int offset
        )
        {
            int bits =
                data[
                    offset
                ] |
                (
                    data[
                        offset +
                        1
                    ]
                    <<
                    8
                ) |
                (
                    data[
                        offset +
                        2
                    ]
                    <<
                    16
                ) |
                (
                    data[
                        offset +
                        3
                    ]
                    <<
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

                    if (
                        result is float
                    )
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

                    if (
                        result is float
                    )
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

                if (
                    renderer == null
                )
                {
                    return null;
                }

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
            if (
                material == null
            )
            {
                return null;
            }

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

                    if (
                        texture != null
                    )
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
            if (
                root == null
            )
            {
                return null;
            }

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

                if (
                    result != null
                )
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

                if (
                    property != null
                )
                {
                    object result =
                        property.GetValue(
                            moneyPack,
                            null
                        );

                    GameObject gameObject =
                        result as GameObject;

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
    }
}