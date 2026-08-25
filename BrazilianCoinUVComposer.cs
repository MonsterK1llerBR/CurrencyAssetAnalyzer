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

        private const string VERSION =
            "1.0.0";

        private const int AtlasWidth =
            2048;

        private const int AtlasHeight =
            2048;

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
                    Color.FromArgb(190, 193, 192)
                },
                {
                    "SM_Coin_25_Cents",
                    Color.FromArgb(188, 122, 60)
                },
                {
                    "SM_Coin_10_Cents",
                    Color.FromArgb(188, 122, 60)
                },
                {
                    "SM_Coin_5_Cents",
                    Color.FromArgb(156, 91, 43)
                },
                {
                    "SM_Coin_1_Cent",
                    Color.FromArgb(156, 91, 43)
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

                if (
                    sourceFront.Count != 5 ||
                    sourceBack.Count != 5
                )
                {
                    Log.LogInfo(
                        "As dez artes de referencia ainda nao estao presentes."
                    );

                    Log.LogInfo(
                        "O compositor gerou as instrucoes em:"
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
                    sourceBack
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
            Dictionary<string, string> sourceBack
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

                foreach (
                    string coinName in CoinNames
                )
                {
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
                            )
                    )
                    {
                        CoinComponents components =
                            DetectComponents(
                                mask
                            );

                        if (
                            components.All.Count < 3
                        )
                        {
                            throw new InvalidOperationException(
                                "A mascara de " +
                                coinName +
                                " possui menos de 3 ilhas UV detectaveis."
                            );
                        }

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
                    "FINAL GERADO COM SUCESSO."
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
                    "FINAL: " +
                    finalAtlas
                );

                Log.LogInfo(
                    "PREVIEW: " +
                    previewAtlas
                );

                Log.LogInfo(
                    "REPORT: " +
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

            return new TextureBrush(
                CreateBrushBitmap(
                    bounds.Width,
                    bounds.Height,
                    gradient
                )
            );
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

            gradient.Dispose();

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
                                    mask,
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
                List<Component> firstThree =
                    components
                        .OrderBy(
                            c =>
                                c.CenterX
                        )
                        .Take(
                            3
                        )
                        .ToList();

                firstThree =
                    firstThree
                        .OrderBy(
                            c =>
                                c.CenterX
                        )
                        .ToList();

                result.Side =
                    firstThree[0];

                result.FaceA =
                    firstThree[1];

                result.FaceB =
                    firstThree[2];
            }

            return result;
        }

        private unsafe Component FloodFill(
            Bitmap mask,
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

                if (
                    x < component.MinX
                )
                {
                    component.MinX =
                        x;
                }

                if (
                    x > component.MaxX
                )
                {
                    component.MaxX =
                        x;
                }

                if (
                    y < component.MinY
                )
                {
                    component.MinY =
                        y;
                }

                if (
                    y > component.MaxY
                )
                {
                    component.MaxY =
                        y;
                }

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

                graphics.DrawImage(
                    source,
                    new Rectangle(
                        0,
                        0,
                        safeWidth,
                        safeHeight
                    )
                );
            }

            return result;
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

            if (
                File.Exists(
                    path
                )
            )
            {
                return;
            }

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
                "Coloque 10 PNGs nesta pasta:"
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
                "Use imagens de referencia sem fundo,"
            );

            lines.Add(
                "preferencialmente vistas de frente,"
            );

            lines.Add(
                "para evitar deformacao durante o mapeamento."
            );

            lines.Add("");

            lines.Add(
                "O atlas original nunca sera sobrescrito."
            );

            lines.Add(
                "O resultado sera:"
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