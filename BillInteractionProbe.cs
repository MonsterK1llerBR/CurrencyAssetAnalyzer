#nullable disable

using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using UnityEngine;

namespace MonsterK1llerBR.CurrencyAssetAnalyzer
{
    [BepInPlugin(
        GUID,
        NAME,
        VERSION
    )]
    public class BillInteractionProbe : BasePlugin
    {
        private const string GUID =
            "br.monsterk1llerbr.supermarketsimulator.billinteractionprobe";

        private const string NAME =
            "Bill Interaction Probe";

        private const string VERSION =
            "1.0.0";

        private string ReportPath;

        public override void Load()
        {
            ReportPath =
                Path.Combine(
                    Paths.PluginPath,
                    "CurrencyAssetAnalyzer",
                    "BillInteractionProbe",
                    "BillInteractionProbeReport.txt"
                );

            Directory.CreateDirectory(
                Path.GetDirectoryName(
                    ReportPath
                )
            );

            File.WriteAllText(
                ReportPath,
                "========================================" +
                Environment.NewLine +
                "BILL INTERACTION PROBE" +
                Environment.NewLine +
                "VERSION: " +
                VERSION +
                Environment.NewLine +
                "========================================" +
                Environment.NewLine
            );

            Log.LogInfo(
                "Bill Interaction Probe carregado."
            );

            AnalyzeCheckoutChangeManager();
        }

        private void AnalyzeCheckoutChangeManager()
        {
            Type type =
                FindType(
                    "CheckoutChangeManager"
                );

            if (
                type == null
            )
            {
                Write(
                    "CheckoutChangeManager NAO ENCONTRADO."
                );

                return;
            }

            Write(
                "TYPE: " +
                type.FullName
            );

            Write("");

            MethodInfo[] methods =
                type.GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic
                );

            foreach (
                MethodInfo method in methods
            )
            {
                if (
                    method == null
                )
                {
                    continue;
                }

                string name =
                    method.Name ??
                    string.Empty;

                string lower =
                    name.ToLowerInvariant();

                if (
                    lower.Contains("money") ||
                    lower.Contains("bill") ||
                    lower.Contains("cash") ||
                    lower.Contains("change") ||
                    lower.Contains("drop") ||
                    lower.Contains("throw") ||
                    lower.Contains("give") ||
                    lower.Contains("spawn") ||
                    lower.Contains("pick") ||
                    lower.Contains("select") ||
                    lower.Contains("grab")
                )
                {
                    Write(
                        "METHOD: " +
                        method.Name
                    );

                    Write(
                        "  Return: " +
                        method.ReturnType
                    );

                    ParameterInfo[] parameters =
                        method.GetParameters();

                    Write(
                        "  Parameters: " +
                        parameters.Length
                    );

                    foreach (
                        ParameterInfo parameter in parameters
                    )
                    {
                        Write(
                            "    " +
                            parameter.Name +
                            " : " +
                            parameter.ParameterType
                        );
                    }

                    Write("");
                }
            }

            Write(
                "========================================"
            );

            Write(
                "ANALYSIS COMPLETE"
            );

            Write(
                "========================================"
            );

            Log.LogInfo(
                "Relatorio do Bill Interaction Probe gerado."
            );

            Log.LogInfo(
                ReportPath
            );
        }

        private Type FindType(
            string name
        )
        {
            try
            {
                Type type =
                    HarmonyLib.AccessTools.TypeByName(
                        name
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
                            name
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

        private void Write(
            string line
        )
        {
            File.AppendAllText(
                ReportPath,
                line +
                Environment.NewLine
            );
        }
    }
}
