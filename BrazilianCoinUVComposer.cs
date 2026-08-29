#nullable disable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
    public class BrazilianCoinUVComposer : BasePlugin
    {
        private const string GUID =
            "br.monsterk1llerbr.supermarketsimulator.braziliancoinuvcomposer";

        private const string NAME =
            "Brazilian Coin UV Composer";

        private const string VERSION = "1.4.0";

        private const string RootFolder =
            "CurrencyAssetAnalyzer";

        private const string OutputFolder =
            "BrazilianCoinUVComposer";

        private const string SourceFolder =
            "Sources";

        private const string AtlasPattern =
            "T_Money_AlbedoTransparency_*.png";

        private static readonly string[] CoinNames =
        {
            "SM_Coin_50_Cents",
            "SM_Coin_25_Cents",
            "SM_Coin_10_Cents",
            "SM_Coin_5_Cents",
            "SM_Coin_1_Cent"
        };

        private static readonly Dictionary<string, string> CoinLabels =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase
            )
            {
                {
                    "SM_Coin_50_Cents",
                    "50_CENTAVOS"
                },
                {
                    "SM_Coin_25_Cents",
                    "25_CENTAVOS"
                },
                {
                    "SM_Coin_10_Cents",
                    "10_CENTAVOS"
                },
                {
                    "SM_Coin_5_Cents",
                    "5_CENTAVOS"
                },
                {
                    "SM_Coin_1_Cent",
                    "1_CENTAVO"
                }
            };

        private static readonly Dictionary<string, Color> CoinMetalColors =
            new Dictionary<string, Color>(
                StringComparer.OrdinalIgnoreCase
            )
            {
                {
                    "SM_Coin_50_Cents",
                    Color.FromArgb(
                        190,
                        193,
                        192
                    )
                },
                {
                    "SM_Coin_25_Cents",
                    Color.FromArgb(
                        188,
                        122,
                        60
                    )
                },
                {
                    "SM_Coin_10_Cents",
                    Color.FromArgb(
                        188,
                        122,
                        60
                    )
                },
                {
                    "SM_Coin_5_Cents",
                    Color.FromArgb(
                        156,
                        91,
                        43
                    )
                },
                {
                    "SM_Coin_1_Cent",
                    Color.FromArgb(
                        156,
                        91,
                        43
                    )
                }
            };

        private static int Running;

        private sealed class Component
        {
            public int Id;

            public int MinX =
                int.MaxValue;

            public int MinY =
                int.MaxValue;

            public int MaxX =
                int.MinValue;

            public int MaxY =
                int.MinValue;

            public long PixelCount;

            public Rectangle Bounds
            {
                get
                {
                    if (
                        PixelCount <= 0
                    )
                    {
                        return Rectangle.Empty;
                    }

                    return Rectangle.FromLTRB(
                        MinX,
                        MinY,
                        MaxX + 1,
                        MaxY + 1
                    );
                }
            }

            public double CenterX
            {
                get
                {
                    return (
                        MinX +
                        MaxX
                    ) *
                    0.5;
                }
            }

            public double CenterY
            {
                get
                {
                    return (
                        MinY +
                        MaxY
                    ) *
                    0.5;
                }
            }
        }

        private sealed class CoinComponents
        {
            public Component Side;

            public Component FaceA;

            public Component FaceB;

            public List<Component> All =
                new List<Component>();
        }

        public override void Load()
        {
            Log.LogInfo(
                "========================================"
            );

            Log.LogInfo(
                "Brazilian Coin UV Composer v" +
                VERSION
            );

            Log.LogInfo(
                "========================================"
            );

            Log.LogInfo(
                "Objetivo: compor frente, verso e lateral das moedas brasileiras."
            );

            Log.LogInfo(
                "Modo: PROCESSAMENTO PARCIAL AUTOMATICO."
            );

            Log.LogInfo(
                "Mesh original: NAO ALTERADO."
            );

            Log.LogInfo(
                "UV original: NAO ALTERADO."
            );

            Log.LogInfo(
                "Atlas original: NAO ALTERADO."
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
                    WaitAndCompose
                );

            worker.IsBackground =
                true;

            worker.Name =
                "BrazilianCoinUVComposer";

            worker.Start();
        }

        private void WaitAndCompose()
        {
            try
            {
                string root =
                    Path.Combine(
                        Paths.PluginPath,
                        RootFolder
                    );

                string output =
                    Path.Combine(
                        root,
                        OutputFolder
                    );

                string sources =
                    Path.Combine(
                        output,
                        SourceFolder
                    );

                Directory.CreateDirectory(
                    output
                );

                Directory.CreateDirectory(
                    sources
                );

                WriteSourceInstructions(
                    sources
                );

                string atlasPath =
                    FindAtlas(
                        root
                    );

                if (
                    string.IsNullOrEmpty(
                        atlasPath
                    )
                )
                {
                    Log.LogError(
                        "Atlas T_Money nao encontrado."
                    );

                    return;
                }

                Log.LogInfo(
                    "Atlas encontrado:"
                );

                Log.LogInfo(
                    atlasPath
                );

                Dictionary<string, string> masks =
                    FindMasks(
                        root
                    );

                Log.LogInfo(
                    "Mascaras validas: " +
                    masks.Count +
                    "/5"
                );

                if (
                    masks.Count != 5
                )
                {
                    Log.LogError(
                        "Nao foi possivel localizar todas as cinco mascaras."
                    );

                    return;
                }

                Dictionary<string, string> sourceFront =
                    FindSourceImages(
                        sources,
                        "_FRONT"
                    );

                Dictionary<string, string> sourceBack =
                    FindSourceImages(
                        sources,
                        "_BACK"
                    );

                Log.LogInfo(
                    "Artes FRONT encontradas: " +
                    sourceFront.Count +
                    "/5"
                );

                Log.LogInfo(
                    "Artes BACK encontradas: " +
                    sourceBack.Count +
                    "/5"
                );

                List<string> readyCoins =
                    new List<string>();

                foreach (
                    string coinName in CoinNames
                )
                {
                    bool hasFront =
                        sourceFront.ContainsKey(
                            coinName
                        );

                    bool hasBack =
                        sourceBack.ContainsKey(
                            coinName
                        );

                    if (
                        hasFront &&
                        hasBack
                    )
                    {
                        readyCoins.Add(
                            coinName
                        );

                        Log.LogInfo(
                            "Moeda pronta para composicao: " +
                            coinName
                        );
                    }
                    else
                    {
                        Log.LogInfo(
                            "Moeda aguardando arte: " +
                            coinName +
                            " | FRONT=" +
                            (
                                hasFront
                                    ? "OK"
                                    : "FALTA"
                            ) +
                            " | BACK=" +
                            (
                                hasBack
                                    ? "OK"
                                    : "FALTA"
                            )
                        );
                    }
                }

                Log.LogInfo(
                    "Moedas prontas: " +
                    readyCoins.Count +
                    "/5"
                );

                if (
                    readyCoins.Count == 0
                )
                {
                    Log.LogInfo(
                        "Nenhuma moeda possui FRONT + BACK."
                    );

                    Log.LogInfo(
                        "O compositor esta aguardando as artes."
                    );

                    Log.LogInfo(
                        "Pasta de entrada:"
                    );

                    Log.LogInfo(
                        sources
                    );

                    return;
                }

                Generate(
                    root,
                    output,
                    atlasPath,
                    masks,
                    sourceFront,
                    sourceBack,
                    readyCoins
                );
            }
            catch (
                Exception ex
            )
            {
                Log.LogError(
                    "Erro fatal no compositor:"
                );

                Log.LogError(
                    ex
                );
            }
        }

        private string FindAtlas(
            string root
        )
        {
            string[] files =
                Directory.GetFiles(
                    root,
                    AtlasPattern,
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
            string root
        )
        {
            Dictionary<string, string> result =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase
                );

            string[] files =
                Directory.GetFiles(
                    root,
                    "*_MASK.png",
                    SearchOption.AllDirectories
                );

            for (
                int i = 0;
                i < files.Length;
                i++
            )
            {
                string name =
                    Path.GetFileNameWithoutExtension(
                        files[i]
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
                        files[i]
                    );
                }
            }

            return result;
        }

        private Dictionary<string, string> FindSourceImages(
            string sources,
            string suffix
        )
        {
            Dictionary<string, string> result =
                new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase
                );

            string[] files =
                Directory.GetFiles(
                    sources,
                    "*",
                    SearchOption.TopDirectoryOnly
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
                    !name.EndsWith(
                        suffix,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    continue;
                }

                string baseName =
                    name.Substring(
                        0,
                        name.Length -
                        suffix.Length
                    );

                if (
                    baseName.StartsWith(
                        "_"
                    )
                )
                {
                    baseName =
                        baseName.Substring(
                            1
                        );
                }

                string coinName =
                    CoinNames.FirstOrDefault(
                        coin =>
                            CoinLabels[
                                coin
                            ].Equals(
                                baseName,
                                StringComparison.OrdinalIgnoreCase
                            )
                            ||
                            coin.Equals(
                                baseName,
                                StringComparison.OrdinalIgnoreCase
                            )
                    );

                if (
                    coinName == null
                )
                {
                    continue;
                }

                result[
                    coinName
                ] =
                    file;
            }

            return result;
        }

        private void Generate(
            string root,
            string output,
            string atlasPath,
            Dictionary<string, string> masks,
            Dictionary<string, string> sourceFront,
            Dictionary<string, string> sourceBack,
            List<string> readyCoins
        )
        {
            string finalAtlas =
                Path.Combine(
                    output,
                    "BrazilianCoinAtlas_FINAL.png"
                );

            string previewAtlas =
                Path.Combine(
                    output,
                    "BrazilianCoinAtlas_FINAL_PREVIEW.png"
                );

            string reportPath =
                Path.Combine(
                    output,
                    "BrazilianCoinUVComposerReport.txt"
                );

            Bitmap original =
                null;

            Bitmap result =
                null;

            Bitmap preview =
                null;

            try
            {
                original =
                    LoadBitmap(
                        atlasPath
                    );

                if (
                    original.Width != 2048 ||
                    original.Height != 2048
                )
                {
                    Log.LogWarning(
                        "Dimensoes inesperadas do atlas: " +
                        original.Width +
                        "x" +
                        original.Height
                    );
                }

                result =
                    CloneBitmap(
                        original
                    );

                preview =
                    new Bitmap(
                        original.Width,
                        original.Height,
                        PixelFormat.Format32bppArgb
                    );

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

                List<string> report =
                    new List<string>();

                report.Add(
                    "========================================"
                );

                report.Add(
                    "BRAZILIAN COIN UV COMPOSER"
                );

                report.Add(
                    "VERSION: " +
                    VERSION
                );

                report.Add(
                    "========================================"
                );

                report.Add(
                    "Metodo: mascaras UV reais + composicao pixel a pixel."
                );

                report.Add(
                    "Atlas original:"
                );

                report.Add(
                    atlasPath
                );

                report.Add("");

                report.Add(
                    "Mesh: NAO ALTERADO."
                );

                report.Add(
                    "UV: NAO ALTERADO."
                );

                report.Add(
                    "Atlas original: NAO ALTERADO."
                );

                report.Add("");

                report.Add(
                    "Moedas processadas nesta execucao: " +
                    readyCoins.Count +
                    "/5"
                );

                report.Add(
                    string.Join(
                        ", ",
                        readyCoins
                    )
                );

                report.Add("");

                foreach (
                    string coinName in readyCoins
                )
                {
                    Log.LogInfo(
                        "========================================"
                    );

                    Log.LogInfo(
                        "Compondo: " +
                        coinName
                    );

                    using (
                        Bitmap mask =
                            LoadBitmap(
                                masks[
                                    coinName
                                ]
                            ))
                    using (
                        Bitmap front =
                            LoadBitmap(
                                sourceFront[
                                    coinName
                                ]
                            ))
                    using (
                        Bitmap back =
                            LoadBitmap(
                                sourceBack[
                                    coinName
                                ]
                            ))
                    {
                        CoinComponents components =
                            DetectComponents(
                                mask
                            );

                        if (
                            components.All.Count < 3 ||
                            components.Side == null ||
                            components.FaceA == null ||
                            components.FaceB == null
                        )
                        {
                            throw new InvalidOperationException(
                                "A mascara de " +
                                coinName +
                                " nao produziu 3 componentes UV validos."
                            );
                        }

                        Log.LogInfo(
                            "Componentes detectados: " +
                            components.All.Count
                        );

                        Log.LogInfo(
                            "Side: " +
                            FormatComponent(
                                components.Side
                            )
                        );

                        Log.LogInfo(
                            "Face A: " +
                            FormatComponent(
                                components.FaceA
                            )
                        );

                        Log.LogInfo(
                            "Face B: " +
                            FormatComponent(
                                components.FaceB
                            )
                        );

                        DrawCoin(
                            result,
                            preview,
                            mask,
                            front,
                            back,
                            components,
                            CoinMetalColors[
                                coinName
                            ]
                        );

                        report.Add(
                            "----------------------------------------"
                        );

                        report.Add(
                            "Coin: " +
                            coinName
                        );

                        report.Add(
                            "Front Source: " +
                            sourceFront[
                                coinName
                            ]
                        );

                        report.Add(
                            "Back Source: " +
                            sourceBack[
                                coinName
                            ]
                        );

                        report.Add(
                            "Material: " +
                            CoinMetalColors[
                                coinName
                            ]
                        );

                        report.Add(
                            "Components: " +
                            components.All.Count
                        );

                        report.Add(
                            "Side:"
                        );

                        report.Add(
                            FormatComponent(
                                components.Side
                            )
                        );

                        report.Add(
                            "Face A:"
                        );

                        report.Add(
                            FormatComponent(
                                components.FaceA
                            )
                        );

                        report.Add(
                            "Face B:"
                        );

                        report.Add(
                            FormatComponent(
                                components.FaceB
                            )
                        );
                    }
                }

                result.Save(
                    finalAtlas,
                    ImageFormat.Png
                );

                preview.Save(
                    previewAtlas,
                    ImageFormat.Png
                );

                report.Add("");

                report.Add(
                    "========================================"
                );

                report.Add(
                    "COMPOSICAO CONCLUIDA."
                );

                report.Add(
                    "Moedas processadas: " +
                    readyCoins.Count +
                    "/5"
                );

                report.Add(
                    "========================================"
                );

                File.WriteAllLines(
                    reportPath,
                    report
                );

                Log.LogInfo(
                    "========================================"
                );

                Log.LogInfo(
                    "BRAZILIAN COIN UV COMPOSER CONCLUIDO."
                );

                Log.LogInfo(
                    "Moedas processadas: " +
                    readyCoins.Count +
                    "/5"
                );

                Log.LogInfo(
                    "FINAL:"
                );

                Log.LogInfo(
                    finalAtlas
                );

                Log.LogInfo(
                    "PREVIEW:"
                );

                Log.LogInfo(
                    previewAtlas
                );

                Log.LogInfo(
                    "REPORT:"
                );

                Log.LogInfo(
                    reportPath
                );

                Log.LogInfo(
                    "========================================"
                );
            }
            finally
            {
                if (
                    preview != null
                )
                {
                    preview.Dispose();
                }

                if (
                    result != null
                )
                {
                    result.Dispose();
                }

                if (
                    original != null
                )
                {
                    original.Dispose();
                }
            }
        }

        private void DrawCoin(
            Bitmap target,
            Bitmap preview,
            Bitmap mask,
            Bitmap front,
            Bitmap back,
            CoinComponents components,
            Color metalColor
        )
        {
            Rectangle side =
                components.Side.Bounds;

            Rectangle faceA =
                components.FaceA.Bounds;

            Rectangle faceB =
                components.FaceB.Bounds;

            using (
                Graphics graphics =
                    Graphics.FromImage(
                        target
                    )
            )
            {
                graphics.SmoothingMode =
                    SmoothingMode.HighQuality;

                graphics.InterpolationMode =
                    InterpolationMode.HighQualityBicubic;

                graphics.PixelOffsetMode =
                    PixelOffsetMode.HighQuality;

                using (
                    TextureBrush sideBrush =
                        CreateMetalBrush(
                            side,
                            metalColor
                        )
                )
                {
                    graphics.FillRectangle(
                        sideBrush,
                        side
                    );
                }

                using (
                    Bitmap preparedFront =
                        PrepareSource(
                            front,
                            faceA.Width,
                            faceA.Height
                        ))
                {
                    DrawMaskedImage(
                        graphics,
                        preparedFront,
                        mask,
                        components.FaceA
                    );
                }

                using (
                    Bitmap preparedBack =
                        PrepareSource(
                            back,
                            faceB.Width,
                            faceB.Height
                        ))
                {
                    DrawMaskedImage(
                        graphics,
                        preparedBack,
                        mask,
                        components.FaceB
                    );
                }
            }

            using (
                Graphics graphics =
                    Graphics.FromImage(
                        preview
                    )
            )
            {
                graphics.SmoothingMode =
                    SmoothingMode.HighQuality;

                using (
                    Brush brush =
                        new SolidBrush(
                            Color.FromArgb(
                                150,
                                metalColor
                            )
                        )
                )
                {
                    graphics.FillRectangle(
                        brush,
                        components.FaceA.Bounds
                    );
                }

                using (
                    Brush brush =
                        new SolidBrush(
                            Color.FromArgb(
                                100,
                                Color.White
                            )
                        )
                )
                {
                    graphics.FillRectangle(
                        brush,
                        components.FaceB.Bounds
                    );
                }

                using (
                    Brush brush =
                        new SolidBrush(
                            Color.FromArgb(
                                220,
                                metalColor
                            )
                        )
                )
                {
                    graphics.FillRectangle(
                        brush,
                        components.Side.Bounds
                    );
                }
            }
        }

        private TextureBrush CreateMetalBrush(
            Rectangle bounds,
            Color baseColor
        )
        {
            LinearGradientBrush gradient =
                new LinearGradientBrush(
                    bounds,
                    Lighten(
                        baseColor,
                        35
                    ),
                    Darken(
                        baseColor,
                        35
                    ),
                    LinearGradientMode.Horizontal
                );

            Bitmap bitmap =
                CreateBrushBitmap(
                    bounds.Width,
                    bounds.Height,
                    gradient
                );

            gradient.Dispose();

            TextureBrush brush =
                new TextureBrush(
                    bitmap
                );

            bitmap.Dispose();

            return brush;
        }

        private Bitmap CreateBrushBitmap(
            int width,
            int height,
            LinearGradientBrush gradient
        )
        {
            int safeWidth =
                Math.Max(
                    1,
                    width
                );

            int safeHeight =
                Math.Max(
                    1,
                    height
                );

            Bitmap bitmap =
                new Bitmap(
                    safeWidth,
                    safeHeight,
                    PixelFormat.Format32bppArgb
                );

            using (
                Graphics graphics =
                    Graphics.FromImage(
                        bitmap
                    )
            )
            {
                graphics.FillRectangle(
                    gradient,
                    0,
                    0,
                    safeWidth,
                    safeHeight
                );
            }

            return bitmap;
        }

        private Color Lighten(
            Color color,
            int amount
        )
        {
            return Color.FromArgb(
                color.A,
                Math.Min(
                    255,
                    color.R + amount
                ),
                Math.Min(
                    255,
                    color.G + amount
                ),
                Math.Min(
                    255,
                    color.B + amount
                )
            );
        }

        private Color Darken(
            Color color,
            int amount
        )
        {
            return Color.FromArgb(
                color.A,
                Math.Max(
                    0,
                    color.R - amount
                ),
                Math.Max(
                    0,
                    color.G - amount
                ),
                Math.Max(
                    0,
                    color.B - amount
                )
            );
        }

        private void DrawMaskedImage(
            Graphics graphics,
            Bitmap source,
            Bitmap mask,
            Component component
        )
        {
            Rectangle bounds =
                component.Bounds;

            using (
                Bitmap temporary =
                    new Bitmap(
                        mask.Width,
                        mask.Height,
                        PixelFormat.Format32bppArgb
                    )
            )
            {
                using (
                    Graphics tempGraphics =
                        Graphics.FromImage(
                            temporary
                        )
                )
                {
                    tempGraphics.Clear(
                        Color.Transparent
                    );

                    tempGraphics.DrawImage(
                        source,
                        bounds
                    );
                }

                ApplyAlphaMask(
                    temporary,
                    mask,
                    component
                );

                graphics.DrawImageUnscaled(
                    temporary,
                    0,
                    0
                );
            }
        }

        private void ApplyAlphaMask(
            Bitmap image,
            Bitmap mask,
            Component component
        )
        {
            BitmapData imageData =
                null;

            BitmapData maskData =
                null;

            try
            {
                imageData =
                    image.LockBits(
                        component.Bounds,
                        ImageLockMode.ReadWrite,
                        PixelFormat.Format32bppArgb
                    );

                maskData =
                    mask.LockBits(
                        component.Bounds,
                        ImageLockMode.ReadOnly,
                        PixelFormat.Format32bppArgb
                    );

                unsafe
                {
                    byte* imageBase =
                        (byte*)
                        imageData.Scan0;

                    byte* maskBase =
                        (byte*)
                        maskData.Scan0;

                    int width =
                        component.Bounds.Width;

                    int height =
                        component.Bounds.Height;

                    for (
                        int y = 0;
                        y < height;
                        y++
                    )
                    {
                        byte* imageRow =
                            imageBase +
                            (
                                y *
                                imageData.Stride
                            );

                        byte* maskRow =
                            maskBase +
                            (
                                y *
                                maskData.Stride
                            );

                        for (
                            int x = 0;
                            x < width;
                            x++
                        )
                        {
                            int offset =
                                x *
                                4;

                            byte alpha =
                                maskRow[
                                    offset + 3
                                ];

                            imageRow[
                                offset + 3
                            ] =
                                alpha;
                        }
                    }
                }
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
                }

                if (
                    imageData != null
                )
                {
                    image.UnlockBits(
                        imageData
                    );
                }
            }
        }

        private CoinComponents DetectComponents(
            Bitmap mask
        )
        {
            bool[,] visited =
                new bool[
                    mask.Width,
                    mask.Height
                ];

            List<Component> components =
                new List<Component>();

            BitmapData data =
                null;

            try
            {
                data =
                    mask.LockBits(
                        new Rectangle(
                            0,
                            0,
                            mask.Width,
                            mask.Height
                        ),
                        ImageLockMode.ReadOnly,
                        PixelFormat.Format32bppArgb
                    );

                unsafe
                {
                    byte* basePtr =
                        (byte*)
                        data.Scan0;

                    int componentId =
                        0;

                    for (
                        int y = 0;
                        y < mask.Height;
                        y++
                    )
                    {
                        byte* row =
                            basePtr +
                            (
                                y *
                                data.Stride
                            );

                        for (
                            int x = 0;
                            x < mask.Width;
                            x++
                        )
                        {
                            if (
                                visited[
                                    x,
                                    y
                                ]
                            )
                            {
                                continue;
                            }

                            int offset =
                                x *
                                4;

                            byte alpha =
                                row[
                                    offset + 3
                                ];

                            if (
                                alpha < 8
                            )
                            {
                                visited[
                                    x,
                                    y
                                ] =
                                    true;

                                continue;
                            }

                            Component component =
                                FloodFill(
                                    data,
                                    visited,
                                    x,
                                    y,
                                    componentId++
                                );

                            if (
                                component.PixelCount >=
                                20
                            )
                            {
                                components.Add(
                                    component
                                );
                            }
                        }
                    }
                }
            }
            finally
            {
                if (
                    data != null
                )
                {
                    mask.UnlockBits(
                        data
                    );
                }
            }

            components =
                components
                    .OrderByDescending(
                        c =>
                            c.PixelCount
                    )
                    .ToList();

            CoinComponents result =
                new CoinComponents();

            result.All =
                components;

            if (
                components.Count >= 3
            )
            {
                List<Component> candidates =
                    components
                        .OrderBy(
                            c =>
                                c.CenterX
                        )
                        .Take(
                            3
                        )
                        .OrderBy(
                            c =>
                                c.CenterX
                        )
                        .ToList();

                result.Side =
                    candidates[0];

                result.FaceA =
                    candidates[1];

                result.FaceB =
                    candidates[2];
            }

            return result;
        }

        private unsafe Component FloodFill(
            BitmapData data,
            bool[,] visited,
            int startX,
            int startY,
            int id
        )
        {
            Component component =
                new Component();

            component.Id =
                id;

            Queue<Point> queue =
                new Queue<Point>();

            queue.Enqueue(
                new Point(
                    startX,
                    startY
                )
            );

            visited[
                startX,
                startY
            ] =
                true;

            while (
                queue.Count > 0
            )
            {
                Point point =
                    queue.Dequeue();

                int x =
                    point.X;

                int y =
                    point.Y;

                component.PixelCount++;

                component.MinX =
                    Math.Min(
                        component.MinX,
                        x
                    );

                component.MaxX =
                    Math.Max(
                        component.MaxX,
                        x
                    );

                component.MinY =
                    Math.Min(
                        component.MinY,
                        y
                    );

                component.MaxY =
                    Math.Max(
                        component.MaxY,
                        y
                    );

                TryEnqueue(
                    data,
                    visited,
                    queue,
                    x + 1,
                    y
                );

                TryEnqueue(
                    data,
                    visited,
                    queue,
                    x - 1,
                    y
                );

                TryEnqueue(
                    data,
                    visited,
                    queue,
                    x,
                    y + 1
                );

                TryEnqueue(
                    data,
                    visited,
                    queue,
                    x,
                    y - 1
                );
            }

            return component;
        }

        private unsafe void TryEnqueue(
            BitmapData data,
            bool[,] visited,
            Queue<Point> queue,
            int x,
            int y
        )
        {
            if (
                x < 0 ||
                y < 0 ||
                x >= visited.GetLength(0) ||
                y >= visited.GetLength(1)
            )
            {
                return;
            }

            if (
                visited[
                    x,
                    y
                ]
            )
            {
                return;
            }

            byte* basePtr =
                (byte*)
                data.Scan0;

            byte* row =
                basePtr +
                (
                    y *
                    data.Stride
                );

            int offset =
                x *
                4;

            byte alpha =
                row[
                    offset + 3
                ];

            if (
                alpha < 8
            )
            {
                visited[
                    x,
                    y
                ] =
                    true;

                return;
            }

            visited[
                x,
                y
            ] =
                true;

            queue.Enqueue(
                new Point(
                    x,
                    y
                )
            );
        }
        private Bitmap PrepareSource(
            Bitmap source,
            int width,
            int height
        )
        {
            int safeWidth =
                Math.Max(
                    1,
                    width
                );

            int safeHeight =
                Math.Max(
                    1,
                    height
                );

            Rectangle contentBounds =
                FindVisibleContentBounds(
                    source
                );

            if (
                contentBounds == Rectangle.Empty
            )
            {
                contentBounds =
                    new Rectangle(
                        0,
                        0,
                        source.Width,
                        source.Height
                    );
            }

            /*
             * Remove somente uma fina faixa externa da
             * referencia para reduzir halo/borda artificial.
             *
             * Nao removemos pixels escuros individualmente,
             * pois eles podem fazer parte da moeda real.
             */
            const int haloTrim = 2;

            Rectangle trimmedBounds =
                TrimBounds(
                    contentBounds,
                    haloTrim,
                    source.Width,
                    source.Height
                );

            /*
             * Margem muito pequena dentro da ilha UV.
             * A escala anterior ficou praticamente perfeita,
             * portanto mantemos uma margem de apenas 2 pixels.
             */
            const int padding = 1;

            int availableWidth =
                Math.Max(
                    1,
                    safeWidth -
                    (
                        padding *
                        2
                    )
                );

            int availableHeight =
                Math.Max(
                    1,
                    safeHeight -
                    (
                        padding *
                        2
                    )
                );

            double scaleX =
                (double)availableWidth /
                trimmedBounds.Width;

            double scaleY =
                (double)availableHeight /
                trimmedBounds.Height;

            double scale =
                Math.Min(
                    scaleX,
                    scaleY
                );

            int drawWidth =
                Math.Max(
                    1,
                    (int)Math.Round(
                        trimmedBounds.Width *
                        scale
                    )
                );

            int drawHeight =
                Math.Max(
                    1,
                    (int)Math.Round(
                        trimmedBounds.Height *
                        scale
                    )
                );

            int drawX =
                (
                    safeWidth -
                    drawWidth
                ) /
                2;

            int drawY =
                (
                    safeHeight -
                    drawHeight
                ) /
                2;

            Bitmap result =
                new Bitmap(
                    safeWidth,
                    safeHeight,
                    PixelFormat.Format32bppArgb
                );

            using (
                Graphics graphics =
                    Graphics.FromImage(
                        result
                    )
            )
            {
                graphics.Clear(
                    Color.Transparent
                );

                graphics.SmoothingMode =
                    SmoothingMode.HighQuality;

                graphics.InterpolationMode =
                    InterpolationMode.HighQualityBicubic;

                graphics.PixelOffsetMode =
                    PixelOffsetMode.HighQuality;

                graphics.CompositingQuality =
                    CompositingQuality.HighQuality;

                graphics.DrawImage(
                    source,
                    new Rectangle(
                        drawX,
                        drawY,
                        drawWidth,
                        drawHeight
                    ),
                    trimmedBounds,
                    GraphicsUnit.Pixel
                );
            }

            Log.LogInfo(
                "PrepareSource: " +
                source.Width +
                "x" +
                source.Height +
                " | Content=" +
                contentBounds.Width +
                "x" +
                contentBounds.Height +
                " | Trimmed=" +
                trimmedBounds.Width +
                "x" +
                trimmedBounds.Height +
                " | Output=" +
                safeWidth +
                "x" +
                safeHeight +
                " | Draw=" +
                drawWidth +
                "x" +
                drawHeight
            );

            return result;
        }

        private Rectangle FindVisibleContentBounds(
            Bitmap source
        )
        {
            int minX =
                source.Width;

            int minY =
                source.Height;

            int maxX =
                -1;

            int maxY =
                -1;

            for (
                int y = 0;
                y < source.Height;
                y++
            )
            {
                for (
                    int x = 0;
                    x < source.Width;
                    x++
                )
                {
                    Color pixel =
                        source.GetPixel(
                            x,
                            y
                        );

                    if (
                        pixel.A < 8
                    )
                    {
                        continue;
                    }

                    if (
                        x < minX
                    )
                    {
                        minX =
                            x;
                    }

                    if (
                        y < minY
                    )
                    {
                        minY =
                            y;
                    }

                    if (
                        x > maxX
                    )
                    {
                        maxX =
                            x;
                    }

                    if (
                        y > maxY
                    )
                    {
                        maxY =
                            y;
                    }
                }
            }

            if (
                maxX < minX ||
                maxY < minY
            )
            {
                return Rectangle.Empty;
            }

            return Rectangle.FromLTRB(
                minX,
                minY,
                maxX + 1,
                maxY + 1
            );
        }

        private Rectangle TrimBounds(
            Rectangle bounds,
            int trim,
            int sourceWidth,
            int sourceHeight
        )
        {
            int x =
                bounds.X + trim;

            int y =
                bounds.Y + trim;

            int right =
                bounds.Right - trim;

            int bottom =
                bounds.Bottom - trim;

            if (
                right <= x
            )
            {
                x =
                    bounds.X;

                right =
                    bounds.Right;
            }

            if (
                bottom <= y
            )
            {
                y =
                    bounds.Y;

                bottom =
                    bounds.Bottom;
            }

            x =
                Math.Max(
                    0,
                    x
                );

            y =
                Math.Max(
                    0,
                    y
                );

            right =
                Math.Min(
                    sourceWidth,
                    right
                );

            bottom =
                Math.Min(
                    sourceHeight,
                    bottom
                );

            return Rectangle.FromLTRB(
                x,
                y,
                right,
                bottom
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

                return CloneBitmap(
                    temporary
                );
            }
            finally
            {
                if (
                    temporary != null
                )
                {
                    temporary.Dispose();
                }
            }
        }

        private Bitmap CloneBitmap(
            Bitmap source
        )
        {
            Bitmap result =
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

            return result;
        }

        private string FormatComponent(
            Component component
        )
        {
            if (
                component == null
            )
            {
                return "NONE";
            }

            Rectangle bounds =
                component.Bounds;

            return
                "Pixels=" +
                component.PixelCount +
                " | " +
                "X=" +
                bounds.X +
                ".." +
                (
                    bounds.Right -
                    1
                ) +
                " | " +
                "Y=" +
                bounds.Y +
                ".." +
                (
                    bounds.Bottom -
                    1
                );
        }

        private void WriteSourceInstructions(
            string sourceFolder
        )
        {
            string path =
                Path.Combine(
                    sourceFolder,
                    "README.txt"
                );

            List<string> lines =
                new List<string>();

            lines.Add(
                "BRAZILIAN COIN UV COMPOSER"
            );

            lines.Add(
                "========================================"
            );

            lines.Add("");

            lines.Add(
                "Coloque os PNGs correspondentes."
            );

            lines.Add(
                "O compositor processa automaticamente"
            );

            lines.Add(
                "somente moedas que tenham FRONT + BACK."
            );

            lines.Add("");

            lines.Add(
                "50_CENTAVOS_FRONT.png"
            );

            lines.Add(
                "50_CENTAVOS_BACK.png"
            );

            lines.Add(
                "25_CENTAVOS_FRONT.png"
            );

            lines.Add(
                "25_CENTAVOS_BACK.png"
            );

            lines.Add(
                "10_CENTAVOS_FRONT.png"
            );

            lines.Add(
                "10_CENTAVOS_BACK.png"
            );

            lines.Add(
                "5_CENTAVOS_FRONT.png"
            );

            lines.Add(
                "5_CENTAVOS_BACK.png"
            );

            lines.Add(
                "1_CENTAVO_FRONT.png"
            );

            lines.Add(
                "1_CENTAVO_BACK.png"
            );

            lines.Add("");

            lines.Add(
                "FRONT = anverso."
            );

            lines.Add(
                "BACK = reverso."
            );

            lines.Add("");

            lines.Add(
                "O atlas original nunca sera sobrescrito."
            );

            lines.Add(
                "Resultado:"
            );

            lines.Add(
                "BrazilianCoinAtlas_FINAL.png"
            );

            File.WriteAllLines(
                path,
                lines
            );
        }
    }
}



