#nullable disable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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
    public class BrazilianCoinAtlasGenerator : BasePlugin
    {
        private const string GUID =
            "br.monsterk1llerbr.supermarketsimulator.braziliancoinatlasgenerator";

        private const string NAME =
            "Brazilian Coin Atlas Generator";

        private const string VERSION =
            "1.0.1";

        private const int AtlasWidth =
            2048;

        private const int AtlasHeight =
            2048;

        private const string OutputFolderName =
            "BrazilianCoinAtlas";

        private static readonly string[] CoinNames =
        {
            "SM_Coin_50_Cents",
            "SM_Coin_25_Cents",
            "SM_Coin_10_Cents",
            "SM_Coin_5_Cents",
            "SM_Coin_1_Cent"
        };

        private static readonly Dictionary<string, Color> CoinColors =
            new Dictionary<string, Color>(
                StringComparer.OrdinalIgnoreCase
            )
            {
                {
                    "SM_Coin_50_Cents",
                    Color.FromArgb(230, 196, 112)
                },
                {
                    "SM_Coin_25_Cents",
                    Color.FromArgb(230, 196, 112)
                },
                {
                    "SM_Coin_10_Cents",
                    Color.FromArgb(190, 190, 190)
                },
                {
                    "SM_Coin_5_Cents",
                    Color.FromArgb(190, 190, 190)
                },
                {
                    "SM_Coin_1_Cent",
                    Color.FromArgb(190, 190, 190)
                }
            };

        private static int Running;

        public override void Load()
        {
            Log.LogInfo(
                "========================================"
            );

            Log.LogInfo(
                "Brazilian Coin Atlas Generator v" +
                VERSION
            );

            Log.LogInfo(
                "========================================"
            );

            Log.LogInfo(
                "Objetivo: testar substituicao das regioes das moedas."
            );

            Log.LogInfo(
                "Mesh: NAO ALTERADO."
            );

            Log.LogInfo(
                "UV: NAO ALTERADO."
            );

            Log.LogInfo(
                "Textura original: NAO ALTERADA."
            );

            Log.LogInfo(
                "Metodo: mascara pixel a pixel."
            );

            if (
                Interlocked.Exchange(
                    ref Running,
                    1
                ) != 0
            )
            {
                return;
            }

            Thread worker =
                new Thread(
                    WaitAndGenerate
                );

            worker.IsBackground =
                true;

            worker.Name =
                "BrazilianCoinAtlasGenerator";

            worker.Start();
        }

        private void WaitAndGenerate()
        {
            try
            {
                string pluginRoot =
                    Path.Combine(
                        Paths.PluginPath,
                        "CurrencyAssetAnalyzer"
                    );

                Log.LogInfo(
                    "Pasta de trabalho: " +
                    pluginRoot
                );

                for (
                    int attempt = 0;
                    attempt < 120;
                    attempt++
                )
                {
                    try
                    {
                        string atlasPath =
                            FindAtlas(
                                pluginRoot
                            );

                        Dictionary<string, string> masks =
                            FindMasks(
                                pluginRoot
                            );

                        if (
                            !string.IsNullOrEmpty(
                                atlasPath
                            ) &&
                            masks.Count == 5
                        )
                        {
                            Log.LogInfo(
                                "Atlas e cinco mascaras encontrados."
                            );

                            Generate(
                                pluginRoot,
                                atlasPath,
                                masks
                            );

                            return;
                        }
                    }
                    catch (
                        Exception ex
                    )
                    {
                        Log.LogError(
                            "Erro durante tentativa de localizacao: " +
                            ex.Message
                        );
                    }

                    Thread.Sleep(
                        1000
                    );
                }

                Log.LogError(
                    "Tempo limite aguardando atlas e mascaras."
                );
            }
            catch (
                Exception ex
            )
            {
                Log.LogError(
                    "Erro fatal no gerador: " +
                    ex
                );
            }
        }

        private string FindAtlas(
            string pluginRoot
        )
        {
            if (
                !Directory.Exists(
                    pluginRoot
                )
            )
            {
                return null;
            }

            string[] files =
                Directory.GetFiles(
                    pluginRoot,
                    "T_Money_AlbedoTransparency_*.png",
                    SearchOption.AllDirectories
                );

            if (
                files.Length == 0
            )
            {
                return null;
            }

            return files
                .OrderByDescending(
                    File.GetLastWriteTimeUtc
                )
                .First();
        }

        private Dictionary<string, string> FindMasks(
    string pluginRoot
)
        {
            Dictionary<string, string> result =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase
                );

            if (
                !Directory.Exists(
                    pluginRoot
                )
            )
            {
                return result;
            }

            string[] files =
                Directory.GetFiles(
                    pluginRoot,
                    "*_MASK.png",
                    SearchOption.AllDirectories
                );

            Log.LogInfo(
                "Mascaras encontradas no disco: " +
                files.Length
            );

            for (
                int i = 0;
                i < files.Length;
                i++
            )
            {
                string file =
                    files[i];

                string name =
                    Path.GetFileNameWithoutExtension(
                        file
                    );

                if (
                    name.EndsWith(
                        "_MASK",
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    name =
                        name.Substring(
                            0,
                            name.Length - 5
                        );
                }

                Log.LogInfo(
                    "Mascara analisada: " +
                    Path.GetFileName(file) +
                    " -> " +
                    name
                );

                if (
                    !CoinNames.Contains(
                        name,
                        StringComparer.OrdinalIgnoreCase
                    )
                )
                {
                    continue;
                }

                if (
                    !result.ContainsKey(
                        name
                    )
                )
                {
                    result.Add(
                        name,
                        file
                    );

                    Log.LogInfo(
                        "Mascara aceita: " +
                        name
                    );
                }
            }

            Log.LogInfo(
                "Mascaras validas: " +
                result.Count +
                "/5"
            );

            return result;
        }

        private void Generate(
            string pluginRoot,
            string atlasPath,
            Dictionary<string, string> masks
        )
        {
            string outputDirectory =
                Path.Combine(
                    pluginRoot,
                    OutputFolderName
                );

            Directory.CreateDirectory(
                outputDirectory
            );

            string outputAtlas =
                Path.Combine(
                    outputDirectory,
                    "BrazilianCoinAtlas_TEST.png"
                );

            string outputPreview =
                Path.Combine(
                    outputDirectory,
                    "BrazilianCoinAtlas_MASK_PREVIEW.png"
                );

            string outputReport =
                Path.Combine(
                    outputDirectory,
                    "BrazilianCoinAtlasReport.txt"
                );

            Log.LogInfo(
                "========================================"
            );

            Log.LogInfo(
                "INICIANDO SUBSTITUICAO DE TESTE"
            );

            Log.LogInfo(
                "Atlas origem: " +
                atlasPath
            );

            Log.LogInfo(
                "Destino: " +
                outputAtlas
            );

            Bitmap source =
                null;

            Bitmap result =
                null;

            Bitmap preview =
                null;

            BitmapData sourceData =
                null;

            BitmapData resultData =
                null;

            BitmapData previewData =
                null;

            long totalChangedPixels =
                0;

            try
            {
                source =
                    LoadBitmap(
                        atlasPath
                    );

                if (
                    source.Width != AtlasWidth ||
                    source.Height != AtlasHeight
                )
                {
                    Log.LogInfo(
                        "Aviso: dimensoes encontradas: " +
                        source.Width +
                        "x" +
                        source.Height
                    );
                }

                result =
                    new Bitmap(
                        source.Width,
                        source.Height,
                        PixelFormat.Format32bppArgb
                    );

                preview =
                    new Bitmap(
                        source.Width,
                        source.Height,
                        PixelFormat.Format32bppArgb
                    );

                using (
                    Graphics graphics =
                        Graphics.FromImage(
                            result
                        )
                )
                {
                    graphics.DrawImageUnscaled(
                        source,
                        0,
                        0
                    );
                }

                using (
                    Graphics graphics =
                        Graphics.FromImage(
                            preview
                        )
                )
                {
                    graphics.Clear(
                        Color.Transparent
                    );
                }

                sourceData =
                    LockBitmap(
                        source,
                        ImageLockMode.ReadOnly
                    );

                resultData =
                    LockBitmap(
                        result,
                        ImageLockMode.ReadWrite
                    );

                previewData =
                    LockBitmap(
                        preview,
                        ImageLockMode.ReadWrite
                    );

                unsafe
                {
                    byte* sourceBase =
                        (byte*)
                        sourceData.Scan0;

                    byte* resultBase =
                        (byte*)
                        resultData.Scan0;

                    byte* previewBase =
                        (byte*)
                        previewData.Scan0;

                    for (
                        int coinIndex = 0;
                        coinIndex < CoinNames.Length;
                        coinIndex++
                    )
                    {
                        string coinName =
                            CoinNames[
                                coinIndex
                            ];

                        string maskPath;

                        if (
                            !masks.TryGetValue(
                                coinName,
                                out maskPath
                            )
                        )
                        {
                            Log.LogError(
                                "Mascara ausente: " +
                                coinName
                            );

                            continue;
                        }

                        Color replacementColor =
                            CoinColors[
                                coinName
                            ];

                        Log.LogInfo(
                            "Processando: " +
                            coinName
                        );

                        Bitmap mask =
                            null;

                        BitmapData maskData =
                            null;

                        try
                        {
                            mask =
                                LoadBitmap(
                                    maskPath
                                );

                            if (
                                mask.Width != source.Width ||
                                mask.Height != source.Height
                            )
                            {
                                Log.LogError(
                                    "Dimensoes da mascara incompatíveis: " +
                                    coinName +
                                    " | " +
                                    mask.Width +
                                    "x" +
                                    mask.Height
                                );

                                continue;
                            }

                            maskData =
                                LockBitmap(
                                    mask,
                                    ImageLockMode.ReadOnly
                                );

                            int changed =
                                ApplyMask(
                                    sourceBase,
                                    resultBase,
                                    previewBase,
                                    sourceData.Stride,
                                    maskData.Scan0,
                                    maskData.Stride,
                                    source.Width,
                                    source.Height,
                                    replacementColor
                                );

                            totalChangedPixels +=
                                changed;

                            Log.LogInfo(
                                coinName +
                                " | Pixels substituidos: " +
                                changed
                            );
                        }
                        finally
                        {
                            if (
                                maskData != null
                            )
                            {
                                mask.UnlockBits(
                                    maskData
                                );

                                maskData =
                                    null;
                            }

                            if (
                                mask != null
                            )
                            {
                                mask.Dispose();

                                mask =
                                    null;
                            }
                        }
                    }
                }

                result.Save(
                    outputAtlas,
                    ImageFormat.Png
                );

                preview.Save(
                    outputPreview,
                    ImageFormat.Png
                );

                WriteReport(
                    outputReport,
                    atlasPath,
                    outputAtlas,
                    outputPreview,
                    masks,
                    totalChangedPixels
                );

                Log.LogInfo(
                    "========================================"
                );

                Log.LogInfo(
                    "SUBSTITUICAO DE TESTE CONCLUIDA."
                );

                Log.LogInfo(
                    "Pixels totais substituidos: " +
                    totalChangedPixels
                );

                Log.LogInfo(
                    "Atlas: " +
                    outputAtlas
                );

                Log.LogInfo(
                    "Preview: " +
                    outputPreview
                );

                Log.LogInfo(
                    "========================================"
                );
            }
            finally
            {
                if (
                    sourceData != null
                )
                {
                    source.UnlockBits(
                        sourceData
                    );

                    sourceData =
                        null;
                }

                if (
                    resultData != null
                )
                {
                    result.UnlockBits(
                        resultData
                    );

                    resultData =
                        null;
                }

                if (
                    previewData != null
                )
                {
                    preview.UnlockBits(
                        previewData
                    );

                    previewData =
                        null;
                }

                if (
                    preview != null
                )
                {
                    preview.Dispose();

                    preview =
                        null;
                }

                if (
                    result != null
                )
                {
                    result.Dispose();

                    result =
                        null;
                }

                if (
                    source != null
                )
                {
                    source.Dispose();

                    source =
                        null;
                }
            }
        }

        private unsafe int ApplyMask(
            byte* sourceBase,
            byte* resultBase,
            byte* previewBase,
            int sourceStride,
            IntPtr maskScan0,
            int maskStride,
            int width,
            int height,
            Color replacementColor
        )
        {
            byte* maskBase =
                (byte*)
                maskScan0;

            const int bytesPerPixel =
                4;

            int changedPixels =
                0;

            for (
                int y = 0;
                y < height;
                y++
            )
            {
                byte* sourceRow =
                    sourceBase +
                    (
                        y *
                        sourceStride
                    );

                byte* resultRow =
                    resultBase +
                    (
                        y *
                        sourceStride
                    );

                byte* previewRow =
                    previewBase +
                    (
                        y *
                        sourceStride
                    );

                byte* maskRow =
                    maskBase +
                    (
                        y *
                        maskStride
                    );

                for (
                    int x = 0;
                    x < width;
                    x++
                )
                {
                    int offset =
                        x *
                        bytesPerPixel;

                    byte maskB =
                        maskRow[
                            offset
                        ];

                    byte maskG =
                        maskRow[
                            offset + 1
                        ];

                    byte maskR =
                        maskRow[
                            offset + 2
                        ];

                    byte maskA =
                        maskRow[
                            offset + 3
                        ];

                    byte maskValue =
                        GetMaskValue(
                            maskR,
                            maskG,
                            maskB,
                            maskA
                        );

                    if (
                        maskValue == 0
                    )
                    {
                        continue;
                    }

                    int alpha =
                        maskValue;

                    int originalB =
                        sourceRow[
                            offset
                        ];

                    int originalG =
                        sourceRow[
                            offset + 1
                        ];

                    int originalR =
                        sourceRow[
                            offset + 2
                        ];

                    int blendedB =
                        (
                            (
                                originalB *
                                (
                                    255 -
                                    alpha
                                )
                            )
                            +
                            (
                                replacementColor.B *
                                alpha
                            )
                        )
                        /
                        255;

                    int blendedG =
                        (
                            (
                                originalG *
                                (
                                    255 -
                                    alpha
                                )
                            )
                            +
                            (
                                replacementColor.G *
                                alpha
                            )
                        )
                        /
                        255;

                    int blendedR =
                        (
                            (
                                originalR *
                                (
                                    255 -
                                    alpha
                                )
                            )
                            +
                            (
                                replacementColor.R *
                                alpha
                            )
                        )
                        /
                        255;

                    resultRow[
                        offset
                    ] =
                        (byte)
                        blendedB;

                    resultRow[
                        offset + 1
                    ] =
                        (byte)
                        blendedG;

                    resultRow[
                        offset + 2
                    ] =
                        (byte)
                        blendedR;

                    previewRow[
                        offset
                    ] =
                        replacementColor.B;

                    previewRow[
                        offset + 1
                    ] =
                        replacementColor.G;

                    previewRow[
                        offset + 2
                    ] =
                        replacementColor.R;

                    previewRow[
                        offset + 3
                    ] =
                        maskValue;

                    changedPixels++;
                }
            }

            return changedPixels;
        }

        private byte GetMaskValue(
            byte r,
            byte g,
            byte b,
            byte a
        )
        {
            if (
                a > 0
            )
            {
                return a;
            }

            int luminance =
                (
                    (
                        r *
                        299
                    )
                    +
                    (
                        g *
                        587
                    )
                    +
                    (
                        b *
                        114
                    )
                )
                /
                1000;

            if (
                luminance < 8
            )
            {
                return 0;
            }

            return (
                byte
            )
            Math.Min(
                255,
                luminance
            );
        }

        private Bitmap LoadBitmap(
            string path
        )
        {
            Bitmap temporary =
                null;

            try
            {
                temporary =
                    new Bitmap(
                        path
                    );

                Bitmap result =
                    new Bitmap(
                        temporary.Width,
                        temporary.Height,
                        PixelFormat.Format32bppArgb
                    );

                using (
                    Graphics graphics =
                        Graphics.FromImage(
                            result
                        )
                )
                {
                    graphics.DrawImageUnscaled(
                        temporary,
                        0,
                        0
                    );
                }

                return result;
            }
            finally
            {
                if (
                    temporary != null
                )
                {
                    temporary.Dispose();

                    temporary =
                        null;
                }
            }
        }

        private BitmapData LockBitmap(
            Bitmap bitmap,
            ImageLockMode mode
        )
        {
            Rectangle rectangle =
                new Rectangle(
                    0,
                    0,
                    bitmap.Width,
                    bitmap.Height
                );

            return bitmap.LockBits(
                rectangle,
                mode,
                PixelFormat.Format32bppArgb
            );
        }

        private void WriteReport(
            string path,
            string atlasPath,
            string outputAtlas,
            string outputPreview,
            Dictionary<string, string> masks,
            long totalChangedPixels
        )
        {
            using (
                StreamWriter writer =
                    new StreamWriter(
                        path,
                        false
                    )
            )
            {
                writer.WriteLine(
                    "========================================"
                );

                writer.WriteLine(
                    "BRAZILIAN COIN ATLAS GENERATOR"
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
                    "Metodo: substituicao pixel a pixel usando mascaras UV reais."
                );

                writer.WriteLine(
                    "Mesh original: NAO ALTERADO."
                );

                writer.WriteLine(
                    "UV original: NAO ALTERADO."
                );

                writer.WriteLine(
                    "Atlas original: NAO ALTERADO."
                );

                writer.WriteLine();

                writer.WriteLine(
                    "Atlas origem:"
                );

                writer.WriteLine(
                    atlasPath
                );

                writer.WriteLine();

                writer.WriteLine(
                    "Atlas gerado:"
                );

                writer.WriteLine(
                    outputAtlas
                );

                writer.WriteLine();

                writer.WriteLine(
                    "Preview:"
                );

                writer.WriteLine(
                    outputPreview
                );

                writer.WriteLine();

                writer.WriteLine(
                    "Dimensoes esperadas: " +
                    AtlasWidth +
                    "x" +
                    AtlasHeight
                );

                writer.WriteLine(
                    "Pixels totais substituidos: " +
                    totalChangedPixels
                );

                writer.WriteLine();

                for (
                    int i = 0;
                    i < CoinNames.Length;
                    i++
                )
                {
                    string coin =
                        CoinNames[
                            i
                        ];

                    writer.WriteLine(
                        "----------------------------------------"
                    );

                    writer.WriteLine(
                        "Coin: " +
                        coin
                    );

                    writer.WriteLine(
                        "Mask: " +
                        (
                            masks.ContainsKey(
                                coin
                            )
                                ? masks[
                                    coin
                                ]
                                : "NOT FOUND"
                        )
                    );

                    writer.WriteLine(
                        "Teste visual: " +
                        CoinColors[
                            coin
                        ]
                    );

                    writer.WriteLine();
                }

                writer.WriteLine(
                    "========================================"
                );
            }
        }
    }
}