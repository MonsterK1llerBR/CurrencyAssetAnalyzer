#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace MonsterK1llerBR.CurrencyAssetAnalyzer
{
    [BepInPlugin(
        GUID,
        NAME,
        VERSION
    )]
    public class BrazilianBillTextureReplacer : BasePlugin
    {
        private const string GUID =
            "br.monsterk1llerbr.supermarketsimulator.brazilianbilltexturereplacer";

        private const string NAME =
            "Brazilian Bill Texture Replacer";

        private const string VERSION =
            "1.0.1";

        private const string RootFolder =
            "CurrencyAssetAnalyzer";

        private const string SourceFolder =
            "BrazilianBillTextureReplacer";

        private const string TextureFileName =
            "50_REAIS.png";

        private const string TargetPackName =
            "50 Dollar Pack";

        private const string TargetObjectName =
            "Paper Dollar Base";

        private const string TargetMaterialName =
            "50$ Paper Front";

        private const string BaseMapProperty =
            "_BaseMap";

        private const string MainTexProperty =
            "_MainTex";

        private static BrazilianBillTextureReplacer Instance;

        private Harmony HarmonyInstance;

        private Texture2D Brazilian50Texture;

        private string PluginDirectory;

        private string TexturePath;

        private UnityAction<Scene, LoadSceneMode> SceneLoadedAction;

        private readonly HashSet<int> AppliedRenderers =
            new HashSet<int>();

        private readonly HashSet<int> AppliedRoots =
            new HashSet<int>();

        public override void Load()
        {
            Instance =
                this;

            try
            {
                PluginDirectory =
                    Path.Combine(
                        Paths.PluginPath,
                        RootFolder,
                        SourceFolder
                    );

                TexturePath =
                    Path.Combine(
                        PluginDirectory,
                        TextureFileName
                    );

                Directory.CreateDirectory(
                    PluginDirectory
                );

                Log.LogInfo(
                    "========================================"
                );

                Log.LogInfo(
                    "Brazilian Bill Texture Replacer v" +
                    VERSION
                );

                Log.LogInfo(
                    "========================================"
                );

                Log.LogInfo(
                    "Alvo: " +
                    TargetPackName
                );

                Log.LogInfo(
                    "Objeto: " +
                    TargetObjectName
                );

                Log.LogInfo(
                    "Material esperado: " +
                    TargetMaterialName
                );

                Log.LogInfo(
                    "Textura: " +
                    TexturePath
                );

                LoadBrazilianTexture();

                RegisterSceneLoaded();

                PatchSpawnMoney();

                ScanLoadedScenes();

                Log.LogInfo(
                    "Inicializacao concluida."
                );
            }
            catch (
                Exception ex
            )
            {
                Log.LogError(
                    "Erro fatal na inicializacao:"
                );

                Log.LogError(
                    ex.ToString()
                );
            }
        }

        private void LoadBrazilianTexture()
        {
            try
            {
                if (
                    !File.Exists(
                        TexturePath
                    )
                )
                {
                    Log.LogWarning(
                        "Textura R$50 nao encontrada."
                    );

                    Log.LogWarning(
                        TexturePath
                    );

                    return;
                }

                byte[] bytes =
                    File.ReadAllBytes(
                        TexturePath
                    );

                if (
                    bytes == null ||
                    bytes.Length == 0
                )
                {
                    Log.LogError(
                        "Arquivo R$50 vazio."
                    );

                    return;
                }

                Texture2D texture =
                    new Texture2D(
                        2,
                        2,
                        TextureFormat.RGBA32,
                        false
                    );

                bool success =
                    UnityEngine.ImageConversion.LoadImage(
                        texture,
                        bytes,
                        false
                    );

                if (
                    !success
                )
                {
                    UnityEngine.Object.Destroy(
                        texture
                    );

                    Log.LogError(
                        "Falha ao decodificar 50_REAIS.png."
                    );

                    return;
                }

                texture.name =
                    "Brazilian50Runtime";

                texture.filterMode =
                    FilterMode.Bilinear;

                texture.wrapMode =
                    TextureWrapMode.Clamp;

                texture.anisoLevel =
                    1;

                Brazilian50Texture =
                    texture;

                Log.LogInfo(
                    "TEXTURA R$50 CARREGADA."
                );

                Log.LogInfo(
                    "Dimensoes: " +
                    texture.width +
                    "x" +
                    texture.height
                );

                Log.LogInfo(
                    "Texture InstanceID: " +
                    texture.GetInstanceID()
                );
            }
            catch (
                Exception ex
            )
            {
                Log.LogError(
                    "Erro carregando textura R$50:"
                );

                Log.LogError(
                    ex.ToString()
                );
            }
        }

        private void RegisterSceneLoaded()
        {
            try
            {
                SceneLoadedAction =
                    DelegateSupport.ConvertDelegate<
                        UnityAction<Scene, LoadSceneMode>
                    >(
                        new Action<Scene, LoadSceneMode>(
                            OnSceneLoaded
                        )
                    );

                SceneManager.sceneLoaded +=
                    SceneLoadedAction;

                Log.LogInfo(
                    "SceneLoaded registrado."
                );
            }
            catch (
                Exception ex
            )
            {
                Log.LogError(
                    "Erro registrando SceneLoaded:"
                );

                Log.LogError(
                    ex.ToString()
                );
            }
        }

        private void OnSceneLoaded(
            Scene scene,
            LoadSceneMode mode
        )
        {
            try
            {
                Log.LogInfo(
                    "SceneLoaded: " +
                    scene.name
                );

                ScanLoadedScenes();
            }
            catch (
                Exception ex
            )
            {
                Log.LogError(
                    "Erro durante SceneLoaded:"
                );

                Log.LogError(
                    ex.ToString()
                );
            }
        }

        private void ScanLoadedScenes()
        {
            if (
                Brazilian50Texture == null
            )
            {
                Log.LogWarning(
                    "Scan ignorado: textura R$50 nao carregada."
                );

                return;
            }

            try
            {
                Renderer[] renderers =
                    UnityEngine.Object.FindObjectsOfType<Renderer>(
                        true
                    );

                int candidates =
                    0;

                int applied =
                    0;

                if (
                    renderers == null
                )
                {
                    return;
                }

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

                    if (
                        !IsTargetRenderer(
                            renderer
                        )
                    )
                    {
                        continue;
                    }

                    candidates++;

                    if (
                        ApplyTexture(
                            renderer
                        )
                    )
                    {
                        applied++;
                    }
                }

                Log.LogInfo(
                    "Startup/Scene Scan:"
                );

                Log.LogInfo(
                    "Renderers candidatos: " +
                    candidates
                );

                Log.LogInfo(
                    "Renderers modificados: " +
                    applied
                );
            }
            catch (
                Exception ex
            )
            {
                Log.LogError(
                    "Erro no ScanLoadedScenes:"
                );

                Log.LogError(
                    ex.ToString()
                );
            }
        }

        private bool IsTargetRenderer(
            Renderer renderer
        )
        {
            try
            {
                GameObject gameObject =
                    renderer.gameObject;

                if (
                    gameObject == null
                )
                {
                    return false;
                }

                string objectName =
                    gameObject.name ??
                    string.Empty;

                if (
                    objectName.IndexOf(
                        TargetObjectName,
                        StringComparison.OrdinalIgnoreCase
                    ) < 0
                )
                {
                    return false;
                }

                Transform current =
                    gameObject.transform;

                while (
                    current != null
                )
                {
                    string parentName =
                        current.name ??
                        string.Empty;

                    if (
                        string.Equals(
                            parentName,
                            TargetPackName,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        return true;
                    }

                    current =
                        current.parent;
                }
            }
            catch
            {
            }

            return false;
        }

        private bool ApplyTexture(
            Renderer renderer
        )
        {
            if (
                renderer == null ||
                Brazilian50Texture == null
            )
            {
                return false;
            }

            try
            {
                int rendererId =
                    renderer.GetInstanceID();

                if (
                    AppliedRenderers.Contains(
                        rendererId
                    )
                )
                {
                    return false;
                }

                Material[] materials =
                    renderer.materials;

                if (
                    materials == null ||
                    materials.Length == 0
                )
                {
                    return false;
                }

                bool changed =
                    false;

                for (
                    int i = 0;
                    i < materials.Length;
                    i++
                )
                {
                    Material material =
                        materials[i];

                    if (
                        material == null
                    )
                    {
                        continue;
                    }

                    string materialName =
                        material.name ??
                        string.Empty;

                    Log.LogInfo(
                        "Material candidato: " +
                        renderer.gameObject.name +
                        " -> " +
                        materialName
                    );

                    if (
                        !IsTargetMaterial(
                            materialName
                        )
                    )
                    {
                        continue;
                    }

                    if (
                        material.HasProperty(
                            BaseMapProperty
                        )
                    )
                    {
                        material.SetTexture(
                            BaseMapProperty,
                            Brazilian50Texture
                        );

                        changed =
                            true;

                        Log.LogInfo(
                            "R$50 aplicada em _BaseMap: " +
                            GetHierarchyPath(
                                renderer.transform
                            )
                        );
                    }

                    if (
                        material.HasProperty(
                            MainTexProperty
                        )
                    )
                    {
                        material.SetTexture(
                            MainTexProperty,
                            Brazilian50Texture
                        );

                        changed =
                            true;

                        Log.LogInfo(
                            "R$50 aplicada em _MainTex: " +
                            GetHierarchyPath(
                                renderer.transform
                            )
                        );
                    }

                    if (
                        changed
                    )
                    {
                        AppliedRenderers.Add(
                            rendererId
                        );

                        Log.LogInfo(
                            "TEXTURA R$50 SUBSTITUIDA."
                        );
                    }
                }

                return changed;
            }
            catch (
                Exception ex
            )
            {
                Log.LogError(
                    "Erro aplicando textura R$50:"
                );

                Log.LogError(
                    ex.ToString()
                );

                return false;
            }
        }

        private bool IsTargetMaterial(
            string materialName
        )
        {
            if (
                string.IsNullOrEmpty(
                    materialName
                )
            )
            {
                return false;
            }

            return
                string.Equals(
                    materialName,
                    TargetMaterialName,
                    StringComparison.OrdinalIgnoreCase
                ) ||
                materialName.IndexOf(
                    TargetMaterialName,
                    StringComparison.OrdinalIgnoreCase
                ) >= 0;
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

                HarmonyInstance =
                    new Harmony(
                        GUID
                    );

                MethodInfo postfix =
                    AccessTools.Method(
                        typeof(
                            BrazilianBillTextureReplacer
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

                Log.LogInfo(
                    "Patch de SpawnMoney aplicado."
                );
            }
            catch (
                Exception ex
            )
            {
                Log.LogError(
                    "Erro aplicando SpawnMoney:"
                );

                Log.LogError(
                    ex.ToString()
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
                    isCoin ||
                    Instance.Brazilian50Texture == null
                )
                {
                    return;
                }

                GameObject root =
                    Instance.ResolveMoneyPackGameObject(
                        moneyPack
                    );

                if (
                    root == null
                )
                {
                    return;
                }

                string rootName =
                    root.name ??
                    string.Empty;

                if (
                    rootName.IndexOf(
                        TargetPackName,
                        StringComparison.OrdinalIgnoreCase
                    ) < 0
                )
                {
                    return;
                }

                int rootId =
                    root.GetInstanceID();

                if (
                    Instance.AppliedRoots.Contains(
                        rootId
                    )
                )
                {
                    return;
                }

                Instance.AppliedRoots.Add(
                    rootId
                );

                Instance.ApplyToMoneyPack(
                    root
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
                        "Erro no SpawnMoneyPostfix:"
                    );

                    Instance.Log.LogError(
                        ex.ToString()
                    );
                }
            }
        }

        private void ApplyToMoneyPack(
            GameObject root
        )
        {
            try
            {
                Renderer[] renderers =
                    root.GetComponentsInChildren<Renderer>(
                        true
                    );

                int found =
                    0;

                int applied =
                    0;

                if (
                    renderers == null
                )
                {
                    return;
                }

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

                    if (
                        !IsTargetRenderer(
                            renderer
                        )
                    )
                    {
                        continue;
                    }

                    found++;

                    if (
                        ApplyTexture(
                            renderer
                        )
                    )
                    {
                        applied++;
                    }
                }

                Log.LogInfo(
                    "SpawnMoney 50 Dollar Pack:"
                );

                Log.LogInfo(
                    "Renderers alvo: " +
                    found
                );

                Log.LogInfo(
                    "Renderers modificados: " +
                    applied
                );
            }
            catch (
                Exception ex
            )
            {
                Log.LogError(
                    "Erro aplicando ao MoneyPack:"
                );

                Log.LogError(
                    ex.ToString()
                );
            }
        }

        private GameObject ResolveMoneyPackGameObject(
            object moneyPack
        )
        {
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
                        parameters.Length != 2
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
            catch
            {
            }

            return null;
        }

        private static string GetHierarchyPath(
            Transform transform
        )
        {
            if (
                transform == null
            )
            {
                return string.Empty;
            }

            List<string> parts =
                new List<string>();

            Transform current =
                transform;

            while (
                current != null
            )
            {
                parts.Add(
                    current.name
                );

                current =
                    current.parent;
            }

            parts.Reverse();

            return string.Join(
                "/",
                parts.ToArray()
            );
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

                if (
                    SceneLoadedAction != null
                )
                {
                    SceneManager.sceneLoaded -=
                        SceneLoadedAction;

                    SceneLoadedAction =
                        null;
                }

                if (
                    Brazilian50Texture != null
                )
                {
                    UnityEngine.Object.Destroy(
                        Brazilian50Texture
                    );

                    Brazilian50Texture =
                        null;
                }

                AppliedRenderers.Clear();
                AppliedRoots.Clear();
            }
            catch
            {
            }

            return true;
        }
    }
}

