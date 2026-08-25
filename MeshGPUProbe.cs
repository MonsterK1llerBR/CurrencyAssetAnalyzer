#nullable disable

using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace MonsterK1llerBR.CurrencyAssetAnalyzer
{
    [BepInPlugin(
        GUID,
        NAME,
        VERSION
    )]
    public class MeshGPUProbe : BasePlugin
    {
        private const string GUID =
            "br.monsterk1llerbr.supermarketsimulator.meshgpuprobe";

        private const string NAME =
            "Mesh GPU Probe";

        private const string VERSION =
            "1.0.0";

        private static MeshGPUProbe Instance;

        private Harmony HarmonyInstance;

        private static string OutputDirectory;

        private static string ReportFile;

        private static bool AlreadyExecuted;

        public override void Load()
        {
            Instance = this;

            try
            {
                OutputDirectory =
                    Path.Combine(
                        Paths.PluginPath,
                        "CurrencyAssetAnalyzer",
                        "MeshGPUProbe"
                    );

                ReportFile =
                    Path.Combine(
                        OutputDirectory,
                        "MeshGPUReport.txt"
                    );

                Directory.CreateDirectory(
                    OutputDirectory
                );

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
                        "MESH GPU PROBE"
                    );

                    writer.WriteLine(
                        "VERSION: " +
                        VERSION
                    );

                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "Objetivo: descobrir se o GPU Vertex Buffer pode ser lido."
                    );

                    writer.WriteLine();
                }

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "Mesh GPU Probe v1.0.0"
                );

                LogInfo(
                    "========================================"
                );

                PatchSpawnMoney();
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro inicializando Mesh GPU Probe: " +
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
                        typeof(MeshGPUProbe),
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

                LogInfo(
                    "Patch aplicado."
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

                AlreadyExecuted = true;

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

        private static void ProbeMesh(
            Mesh mesh
        )
        {
            try
            {
                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "Mesh: " +
                    mesh.name
                );

                bool isReadable =
                    false;

                bool canAccess =
                    false;

                int vertexBufferCount =
                    0;

                int vertexAttributeCount =
                    0;

                try
                {
                    isReadable =
                        mesh.isReadable;
                }
                catch
                {
                }

                try
                {
                    canAccess =
                        mesh.canAccess;
                }
                catch
                {
                }

                try
                {
                    vertexBufferCount =
                        mesh.vertexBufferCount;
                }
                catch
                {
                }

                try
                {
                    vertexAttributeCount =
                        mesh.vertexAttributeCount;
                }
                catch
                {
                }

                Write(
                    "Mesh Name: " +
                    mesh.name
                );

                Write(
                    "InstanceID: " +
                    mesh.GetInstanceID()
                );

                Write(
                    "VertexCount: " +
                    mesh.vertexCount
                );

                Write(
                    "SubMeshCount: " +
                    mesh.subMeshCount
                );

                Write(
                    "isReadable: " +
                    isReadable
                );

                Write(
                    "canAccess: " +
                    canAccess
                );

                Write(
                    "VertexBufferCount: " +
                    vertexBufferCount
                );

                Write(
                    "VertexAttributeCount: " +
                    vertexAttributeCount
                );

                ProbeAttributes(
                    mesh
                );

                for (
                    int stream = 0;
                    stream < vertexBufferCount;
                    stream++
                )
                {
                    ProbeVertexBuffer(
                        mesh,
                        stream
                    );
                }

                ProbeIndexBuffer(
                    mesh
                );

                Write(
                    "END"
                );

                LogInfo(
                    "Relatorio GPU gerado."
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro analisando GPU buffer: " +
                    ex
                );
            }
        }

        private static void ProbeAttributes(
            Mesh mesh
        )
        {
            try
            {
                Write(
                    ""
                );

                Write(
                    "VERTEX ATTRIBUTES"
                );

                Write(
                    "--------------------------------"
                );

                VertexAttribute[] attributes =
                {
                    VertexAttribute.Position,
                    VertexAttribute.Normal,
                    VertexAttribute.Tangent,
                    VertexAttribute.Color,
                    VertexAttribute.TexCoord0,
                    VertexAttribute.TexCoord1,
                    VertexAttribute.TexCoord2,
                    VertexAttribute.TexCoord3
                };

                for (
                    int i = 0;
                    i < attributes.Length;
                    i++
                )
                {
                    VertexAttribute attribute =
                        attributes[i];

                    bool exists =
                        false;

                    try
                    {
                        exists =
                            mesh.HasVertexAttribute(
                                attribute
                            );
                    }
                    catch
                    {
                    }

                    Write(
                        attribute +
                        ": " +
                        exists
                    );

                    if (!exists)
                        continue;

                    try
                    {
                        Write(
                            "  Stream: " +
                            mesh.GetVertexAttributeStream(
                                attribute
                            )
                        );
                    }
                    catch
                    {
                    }

                    try
                    {
                        Write(
                            "  Offset: " +
                            mesh.GetVertexAttributeOffset(
                                attribute
                            )
                        );
                    }
                    catch
                    {
                    }

                    try
                    {
                        Write(
                            "  Dimension: " +
                            mesh.GetVertexAttributeDimension(
                                attribute
                            )
                        );
                    }
                    catch
                    {
                    }

                    try
                    {
                        Write(
                            "  Format: " +
                            mesh.GetVertexAttributeFormat(
                                attribute
                            )
                        );
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                Write(
                    "Attribute probe error: " +
                    ex.Message
                );
            }
        }

        private static void ProbeVertexBuffer(
            Mesh mesh,
            int stream
        )
        {
            GraphicsBuffer buffer =
                null;

            try
            {
                Write(
                    ""
                );

                Write(
                    "VERTEX BUFFER #" +
                    stream
                );

                Write(
                    "--------------------------------"
                );

                int stride =
                    0;

                try
                {
                    stride =
                        mesh.GetVertexBufferStride(
                            stream
                        );
                }
                catch
                {
                }

                Write(
                    "Stride: " +
                    stride
                );

                buffer =
                    mesh.GetVertexBuffer(
                        stream
                    );

                if (buffer == null)
                {
                    Write(
                        "Buffer: NULL"
                    );

                    return;
                }

                Write(
                    "Buffer: OK"
                );

                Write(
                    "Buffer Count: " +
                    buffer.count
                );

                Write(
                    "Buffer Stride: " +
                    buffer.stride
                );

                Write(
                    "Buffer Target: " +
                    buffer.target
                );

                bool valid =
                    false;

                try
                {
                    valid =
                        buffer.IsValid();
                }
                catch
                {
                }

                Write(
                    "Buffer IsValid: " +
                    valid
                );

                try
                {
                    IntPtr nativePtr =
                        buffer.GetNativeBufferPtr();

                    Write(
                        "NativeBufferPtr: " +
                        nativePtr
                    );
                }
                catch (Exception ex)
                {
                    Write(
                        "NativeBufferPtr ERROR: " +
                        ex.Message
                    );
                }
}
            catch (Exception ex)
            {
                Write(
                    "VertexBuffer ERROR: " +
                    ex
                );
            }
            finally
            {
                try
                {
                    if (buffer != null)
                    {
                        buffer.Release();
                    }
                }
                catch
                {
                }
            }
        }


        private static void ProbeIndexBuffer(
            Mesh mesh
        )
        {
            GraphicsBuffer buffer =
                null;

            try
            {
                Write(
                    ""
                );

                Write(
                    "INDEX BUFFER"
                );

                Write(
                    "--------------------------------"
                );

                buffer =
                    mesh.GetIndexBuffer();

                if (buffer == null)
                {
                    Write(
                        "Index Buffer: NULL"
                    );

                    return;
                }

                Write(
                    "Index Buffer: OK"
                );

                Write(
                    "Count: " +
                    buffer.count
                );

                Write(
                    "Stride: " +
                    buffer.stride
                );

                Write(
                    "Target: " +
                    buffer.target
                );

                try
                {
                    bool valid =
                        buffer.IsValid();

                    Write(
                        "IsValid: " +
                        valid
                    );
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                Write(
                    "Index Buffer ERROR: " +
                    ex
                );
            }
            finally
            {
                try
                {
                    if (buffer != null)
                    {
                        buffer.Release();
                    }
                }
                catch
                {
                }
            }
        }

        private static void Write(
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




