#nullable disable

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading;
using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace MonsterK1llerBR.CurrencyAssetAnalyzer
{
    [BepInPlugin(
        GUID,
        NAME,
        VERSION
    )]
    public class AtlasMaskVisualizer : BasePlugin
    {
        private const string GUID =
            "br.monsterk1llerbr.supermarketsimulator.atlasmaskvisualizer";

        private const string NAME =
            "Currency Atlas Mask Visualizer";

        private const string VERSION =
            "3.1.0";

        private const string GameRoot =
            @"B:\SteamLibrary\steamapps\common\Supermarket Simulator";

        private const string RepositoryRoot =
            @"C:\Users\natan\Documents\Mods\SupermarketSimulator\CurrencyAssetAnalyzer";

        private const string MaskDirectory =
            @"B:\SteamLibrary\steamapps\common\Supermarket Simulator\BepInEx\plugins\CurrencyAssetAnalyzer\AnalyzerV10\AtlasMasks";

        private const string OutputDirectory =
            @"B:\SteamLibrary\steamapps\common\Supermarket Simulator\BepInEx\plugins\CurrencyAssetAnalyzer\AnalyzerV11\AtlasVisualization";

        private const string RepositoryOutputDirectory =
            @"C:\Users\natan\Documents\Mods\SupermarketSimulator\CurrencyAssetAnalyzer\Reports\AtlasVisualization";

        private static AtlasMaskVisualizer Instance;

        private static Timer ScanTimer;

        private static bool Generated;

        private static readonly CoinVisualInfo[] Coins =
        {
            new CoinVisualInfo(
                "SM_Coin_50_Cents",
                0.50f,
                Color.Red
            ),

            new CoinVisualInfo(
                "SM_Coin_25_Cents",
                0.25f,
                Color.Lime
            ),

            new CoinVisualInfo(
                "SM_Coin_10_Cents",
                0.10f,
                Color.Blue
            ),

            new CoinVisualInfo(
                "SM_Coin_5_Cents",
                0.05f,
                Color.Yellow
            ),

            new CoinVisualInfo(
                "SM_Coin_1_Cent",
                0.01f,
                Color.Magenta
            )
        };

        public override void Load()
        {
            Instance = this;

            LogInfo(
                "========================================"
            );

            LogInfo(
                "Currency Atlas Mask Visualizer v" +
                VERSION
            );

            LogInfo(
                "========================================"
            );

            LogInfo(
                "Metodo: System.Drawing"
            );

            LogInfo(
                "Texture2D: DESATIVADO"
            );

            LogInfo(
                "ImageConversion: DESATIVADO"
            );

            try
            {
                Directory.CreateDirectory(
                    OutputDirectory
                );

                Directory.CreateDirectory(
                    RepositoryOutputDirectory
                );

                ScanTimer = new Timer(
                    ScanCallback,
                    null,
                    2000,
                    1000
                );

                LogInfo(
                    "Monitoramento das mascaras iniciado."
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro inicializando visualizador: " +
                    ex
                );
            }
        }

        private static void ScanCallback(
            object state
        )
        {
            if (Generated)
            {
                return;
            }

            try
            {
                string sourcePath =
                    FindSourceTexture();

                if (string.IsNullOrEmpty(sourcePath))
                {
                    return;
                }

                string[] maskPaths =
                    FindAllMasks();

                if (maskPaths == null)
                {
                    return;
                }

                LogInfo(
                    "Todas as mascaras encontradas."
                );

                GenerateVisualization(
                    sourcePath,
                    maskPaths
                );

                Generated = true;

                if (ScanTimer != null)
                {
                    ScanTimer.Dispose();
                    ScanTimer = null;
                }
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro durante visualizacao: " +
                    ex
                );
            }
        }

        private static string FindSourceTexture()
        {
            string analyzerRoot =
                Path.Combine(
                    GameRoot,
                    @"BepInEx\plugins\CurrencyAssetAnalyzer"
                );

            string result =
                SearchForMoneyTexture(
                    analyzerRoot
                );

            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }

            return SearchForMoneyTexture(
                RepositoryRoot
            );
        }

        private static string SearchForMoneyTexture(
            string root
        )
        {
            if (!Directory.Exists(root))
            {
                return null;
            }

            string[] files;

            try
            {
                files = Directory.GetFiles(
                    root,
                    "*.png",
                    SearchOption.AllDirectories
                );
            }
            catch
            {
                return null;
            }

            for (int i = 0; i < files.Length; i++)
            {
                string fileName =
                    Path.GetFileName(
                        files[i]
                    );

                if (
                    fileName.StartsWith(
                        "T_Money_AlbedoTransparency",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return files[i];
                }
            }

            return null;
        }

        private static string[] FindAllMasks()
        {
            if (!Directory.Exists(MaskDirectory))
            {
                return null;
            }

            string[] result =
                new string[Coins.Length];

            for (
                int i = 0;
                i < Coins.Length;
                i++
            )
            {
                string path =
                    Path.Combine(
                        MaskDirectory,
                        Coins[i].Name +
                        "_MASK.png"
                    );

                if (!File.Exists(path))
                {
                    return null;
                }

                result[i] = path;
            }

            return result;
        }

        private static void GenerateVisualization(
            string sourcePath,
            string[] maskPaths
        )
        {
            LogInfo(
                "Atlas encontrado: " +
                sourcePath
            );

            using var atlas =
                new Bitmap(
                    sourcePath
                );

            LogInfo(
                "Dimensoes do atlas: " +
                atlas.Width +
                "x" +
                atlas.Height
            );

            using var combined =
                new Bitmap(
                    atlas.Width,
                    atlas.Height,
                    PixelFormat.Format32bppArgb
                );

            CopyBitmap(
                atlas,
                combined
            );

            for (
                int i = 0;
                i < Coins.Length;
                i++
            )
            {
                using var mask =
                    new Bitmap(
                        maskPaths[i]
                    );

                ValidateMask(
                    atlas,
                    mask,
                    Coins[i].Name
                );

                ApplyOverlay(
                    combined,
                    mask,
                    Coins[i].Color,
                    0.55f
                );

                GenerateIndividual(
                    atlas,
                    mask,
                    Coins[i]
                );
            }

            string gameCombined =
                Path.Combine(
                    OutputDirectory,
                    "CurrencyAtlas_VisualMap.png"
                );

            string repositoryCombined =
                Path.Combine(
                    RepositoryOutputDirectory,
                    "CurrencyAtlas_VisualMap.png"
                );

            SavePng(
                combined,
                gameCombined
            );

            SavePng(
                combined,
                repositoryCombined
            );

            WriteReport();

            LogInfo(
                "Mapa combinado gerado."
            );

            LogInfo(
                "Saida: " +
                gameCombined
            );
        }

        private static void ValidateMask(
            Bitmap atlas,
            Bitmap mask,
            string coinName
        )
        {
            if (
                atlas.Width != mask.Width ||
                atlas.Height != mask.Height
            )
            {
                throw new Exception(
                    "Dimensoes invalidas em " +
                    coinName +
                    ": Atlas=" +
                    atlas.Width +
                    "x" +
                    atlas.Height +
                    " | Mask=" +
                    mask.Width +
                    "x" +
                    mask.Height
                );
            }
        }

        private static void CopyBitmap(
            Bitmap source,
            Bitmap destination
        )
        {
            using var graphics =
                Graphics.FromImage(
                    destination
                );

            graphics.DrawImageUnscaled(
                source,
                0,
                0
            );
        }

        private static void ApplyOverlay(
            Bitmap target,
            Bitmap mask,
            Color overlayColor,
            float opacity
        )
        {
            for (
                int y = 0;
                y < target.Height;
                y++
            )
            {
                for (
                    int x = 0;
                    x < target.Width;
                    x++
                )
                {
                    Color maskPixel =
                        mask.GetPixel(
                            x,
                            y
                        );

                    if (maskPixel.A == 0)
                    {
                        continue;
                    }

                    float amount =
                        opacity *
                        (
                            maskPixel.A /
                            255f
                        );

                    Color background =
                        target.GetPixel(
                            x,
                            y
                        );

                    Color result =
                        Blend(
                            background,
                            overlayColor,
                            amount
                        );

                    target.SetPixel(
                        x,
                        y,
                        result
                    );
                }
            }
        }

        private static Color Blend(
            Color background,
            Color foreground,
            float amount
        )
        {
            if (amount <= 0f)
            {
                return background;
            }

            if (amount >= 1f)
            {
                return Color.FromArgb(
                    background.A,
                    foreground.R,
                    foreground.G,
                    foreground.B
                );
            }

            int r =
                (int)(
                    background.R *
                    (1f - amount) +
                    foreground.R *
                    amount
                );

            int g =
                (int)(
                    background.G *
                    (1f - amount) +
                    foreground.G *
                    amount
                );

            int b =
                (int)(
                    background.B *
                    (1f - amount) +
                    foreground.B *
                    amount
                );

            return Color.FromArgb(
                background.A,
                ClampByte(r),
                ClampByte(g),
                ClampByte(b)
            );
        }

        private static int ClampByte(
            int value
        )
        {
            if (value < 0)
            {
                return 0;
            }

            if (value > 255)
            {
                return 255;
            }

            return value;
        }

        private static void GenerateIndividual(
            Bitmap atlas,
            Bitmap mask,
            CoinVisualInfo coin
        )
        {
            using var visual =
                new Bitmap(
                    atlas.Width,
                    atlas.Height,
                    PixelFormat.Format32bppArgb
                );

            CopyBitmap(
                atlas,
                visual
            );

            ApplyOverlay(
                visual,
                mask,
                coin.Color,
                0.75f
            );

            string fileName =
                coin.Name +
                "_VISUAL.png";

            SavePng(
                visual,
                Path.Combine(
                    OutputDirectory,
                    fileName
                )
            );

            SavePng(
                visual,
                Path.Combine(
                    RepositoryOutputDirectory,
                    fileName
                )
            );

            LogInfo(
                "Visualizacao gerada: " +
                coin.Name
            );
        }

        private static void SavePng(
            Bitmap bitmap,
            string path
        )
        {
            bitmap.Save(
                path,
                ImageFormat.Png
            );
        }

        private static void WriteReport()
        {
            string gamePath =
                Path.Combine(
                    OutputDirectory,
                    "AtlasVisualizationReport.txt"
                );

            string repositoryPath =
                Path.Combine(
                    RepositoryOutputDirectory,
                    "AtlasVisualizationReport.txt"
                );

            using var writer =
                new StreamWriter(
                    gamePath,
                    false
                );

            writer.WriteLine(
                "========================================"
            );

            writer.WriteLine(
                "CURRENCY ATLAS VISUALIZATION"
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
                "Metodo: System.Drawing"
            );

            writer.WriteLine(
                "Atlas: 2048x2048"
            );

            writer.WriteLine();

            for (
                int i = 0;
                i < Coins.Length;
                i++
            )
            {
                CoinVisualInfo coin =
                    Coins[i];

                writer.WriteLine(
                    "Coin: " +
                    coin.Name
                );

                writer.WriteLine(
                    "Value: " +
                    coin.Value.ToString(
                        "F2",
                        CultureInfo.InvariantCulture
                    )
                );

                writer.WriteLine(
                    "Color RGB: (" +
                    coin.Color.R +
                    "," +
                    coin.Color.G +
                    "," +
                    coin.Color.B +
                    ")"
                );

                writer.WriteLine(
                    "Mask: " +
                    coin.Name +
                    "_MASK.png"
                );

                writer.WriteLine(
                    "Visual: " +
                    coin.Name +
                    "_VISUAL.png"
                );

                writer.WriteLine();
            }

            writer.WriteLine(
                "Combined: CurrencyAtlas_VisualMap.png"
            );

            writer.Flush();

            File.Copy(
                gamePath,
                repositoryPath,
                true
            );
        }

        private static void LogInfo(
            string message
        )
        {
            try
            {
                if (Instance != null)
                {
                    Instance.Log.LogInfo(
                        message
                    );
                }
            }
            catch
            {
            }
        }

        private static void LogError(
            string message
        )
        {
            try
            {
                if (Instance != null)
                {
                    Instance.Log.LogError(
                        message
                    );
                }
            }
            catch
            {
            }
        }

        private sealed class CoinVisualInfo
        {
            public readonly string Name;

            public readonly float Value;

            public readonly Color Color;

            public CoinVisualInfo(
                string name,
                float value,
                Color color
            )
            {
                Name = name;
                Value = value;
                Color = color;
            }
        }
    }
}
