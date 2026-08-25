#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
    public class AtlasMaskGenerator : BasePlugin
    {
        private const string GUID =
            "br.monsterk1llerbr.supermarketsimulator.currencyatlasmaskgenerator";

        private const string NAME =
            "Currency Atlas Mask Generator";

        private const string VERSION =
            "1.0.0";

        private const string GameRoot =
            @"B:\SteamLibrary\steamapps\common\Supermarket Simulator";

        private const string RepositoryRoot =
            @"C:\Users\natan\Documents\Mods\SupermarketSimulator\CurrencyAssetAnalyzer";

        private const int AtlasWidth = 2048;
        private const int AtlasHeight = 2048;

        private const int ExpectedTriangleCount = 188;

        private const string TriangleReportRelativePath =
            @"BepInEx\plugins\CurrencyAssetAnalyzer\AtlasTriangleMapper\AtlasTriangleReport.txt";

        private const string GameOutputRelativePath =
            @"BepInEx\plugins\CurrencyAssetAnalyzer\AnalyzerV10\AtlasMasks";

        private const string RepositoryOutputRelativePath =
            @"Reports\AtlasMasks";

        private static Timer WatchTimer;

        private static readonly object GenerateLock =
            new object();

        private static string LastProcessedHash =
            string.Empty;

        private static readonly string[] ExpectedCoins =
        {
            "SM_Coin_50_Cents",
            "SM_Coin_25_Cents",
            "SM_Coin_10_Cents",
            "SM_Coin_5_Cents",
            "SM_Coin_1_Cent"
        };

        private static readonly Regex CoinRegex =
            new Regex(
                @"^COIN:\s*(.+?)\s*$",
                RegexOptions.Compiled
            );

        private static readonly Regex TriangleRegex =
            new Regex(
                @"^\s*T\d+\s*\|\s*I=(\d+),(\d+),(\d+)\s*\|\s*UV0=\(([-+0-9.eE]+),([-+0-9.eE]+)\)\s*UV1=\(([-+0-9.eE]+),([-+0-9.eE]+)\)\s*UV2=\(([-+0-9.eE]+),([-+0-9.eE]+)\)\s*Area=([-+0-9.eE]+)",
                RegexOptions.Compiled
            );

        private static string TriangleReportPath
        {
            get
            {
                return Path.Combine(
                    GameRoot,
                    TriangleReportRelativePath
                );
            }
        }

        private static string GameOutputDirectory
        {
            get
            {
                return Path.Combine(
                    GameRoot,
                    GameOutputRelativePath
                );
            }
        }

        private static string RepositoryOutputDirectory
        {
            get
            {
                return Path.Combine(
                    RepositoryRoot,
                    RepositoryOutputRelativePath
                );
            }
        }

        public override void Load()
        {
            Instance = this;

            try
            {
                Directory.CreateDirectory(
                    GameOutputDirectory
                );

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "Currency Atlas Mask Generator v" +
                    VERSION
                );

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "Objetivo: rasterizar os triangulos UV reais."
                );

                LogInfo(
                    "Atlas: " +
                    AtlasWidth +
                    "x" +
                    AtlasHeight
                );

                LogInfo(
                    "Fonte: AtlasTriangleMapper."
                );

                LogInfo(
                    "Metodo: rasterizacao CPU dos triangulos UV."
                );

                LogInfo(
                    "Mesh original: NAO ALTERADO."
                );

                LogInfo(
                    "UV original: NAO ALTERADO."
                );

                LogInfo(
                    "Material original: NAO ALTERADO."
                );

                LogInfo(
                    "Textura original: NAO ALTERADA."
                );

                LogInfo(
                    "Saida: " +
                    GameOutputDirectory
                );

                InitializeReport();

                WatchTimer =
                    new Timer(
                        WatchReport,
                        null,
                        3000,
                        3000
                    );

                LogInfo(
                    "Monitoramento do TriangleReport iniciado."
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro inicializando Atlas Mask Generator: " +
                    ex
                );
            }
        }

        private static void WatchReport(
            object state
        )
        {
            try
            {
                lock (GenerateLock)
                {
                    if (!File.Exists(
                        TriangleReportPath
                    ))
                    {
                        return;
                    }

                    string[] lines =
                        File.ReadAllLines(
                            TriangleReportPath,
                            Encoding.UTF8
                        );

                    Dictionary<string, List<TriangleData>>
                        trianglesByCoin =
                            ParseReport(
                                lines
                            );

                    if (!IsCompleteReport(
                        trianglesByCoin
                    ))
                    {
                        return;
                    }

                    string reportHash =
                        ComputeReportHash(
                            lines
                        );

                    if (
                        !string.IsNullOrEmpty(
                            LastProcessedHash
                        ) &&
                        LastProcessedHash ==
                        reportHash
                    )
                    {
                        return;
                    }

                    GenerateAllMasks(
                        trianglesByCoin
                    );

                    LastProcessedHash =
                        reportHash;
                }
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro monitorando TriangleReport: " +
                    ex
                );
            }
        }

        private static Dictionary<string, List<TriangleData>>
            ParseReport(
                string[] lines
            )
        {
            Dictionary<string, List<TriangleData>>
                result =
                    new Dictionary<string, List<TriangleData>>(
                        StringComparer.OrdinalIgnoreCase
                    );

            string currentCoin =
                null;

            for (
                int i = 0;
                i < ExpectedCoins.Length;
                i++
            )
            {
                result[
                    ExpectedCoins[i]
                ] =
                    new List<TriangleData>();
            }

            for (
                int i = 0;
                i < lines.Length;
                i++
            )
            {
                string line =
                    lines[i];

                Match coinMatch =
                    CoinRegex.Match(
                        line
                    );

                if (coinMatch.Success)
                {
                    currentCoin =
                        coinMatch.Groups[1].Value.Trim();

                    if (
                        !result.ContainsKey(
                            currentCoin
                        )
                    )
                    {
                        currentCoin = null;
                    }

                    continue;
                }

                if (
                    string.IsNullOrEmpty(
                        currentCoin
                    )
                )
                {
                    continue;
                }

                Match triangleMatch =
                    TriangleRegex.Match(
                        line
                    );

                if (!triangleMatch.Success)
                {
                    continue;
                }

                TriangleData triangle =
                    new TriangleData();

                triangle.Index0 =
                    int.Parse(
                        triangleMatch.Groups[1].Value,
                        CultureInfo.InvariantCulture
                    );

                triangle.Index1 =
                    int.Parse(
                        triangleMatch.Groups[2].Value,
                        CultureInfo.InvariantCulture
                    );

                triangle.Index2 =
                    int.Parse(
                        triangleMatch.Groups[3].Value,
                        CultureInfo.InvariantCulture
                    );

                triangle.U0 =
                    ParseFloat(
                        triangleMatch.Groups[4].Value
                    );

                triangle.V0 =
                    ParseFloat(
                        triangleMatch.Groups[5].Value
                    );

                triangle.U1 =
                    ParseFloat(
                        triangleMatch.Groups[6].Value
                    );

                triangle.V1 =
                    ParseFloat(
                        triangleMatch.Groups[7].Value
                    );

                triangle.U2 =
                    ParseFloat(
                        triangleMatch.Groups[8].Value
                    );

                triangle.V2 =
                    ParseFloat(
                        triangleMatch.Groups[9].Value
                    );

                triangle.Area =
                    ParseFloat(
                        triangleMatch.Groups[10].Value
                    );

                result[
                    currentCoin
                ].Add(
                    triangle
                );
            }

            return result;
        }

        private static float ParseFloat(
            string value
        )
        {
            return float.Parse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture
            );
        }

        private static bool IsCompleteReport(
            Dictionary<string, List<TriangleData>>
                trianglesByCoin
        )
        {
            for (
                int i = 0;
                i < ExpectedCoins.Length;
                i++
            )
            {
                string coin =
                    ExpectedCoins[i];

                List<TriangleData>
                    triangles;

                if (
                    !trianglesByCoin.TryGetValue(
                        coin,
                        out triangles
                    )
                )
                {
                    return false;
                }

                if (
                    triangles.Count !=
                    ExpectedTriangleCount
                )
                {
                    return false;
                }
            }

            return true;
        }

        private static void GenerateAllMasks(
            Dictionary<string, List<TriangleData>>
                trianglesByCoin
        )
        {
            try
            {
                Directory.CreateDirectory(
                    GameOutputDirectory
                );

                Directory.CreateDirectory(
                    RepositoryOutputDirectory
                );

                LogInfo(
                    "========================================"
                );

                LogInfo(
                    "TriangleReport completo."
                );

                for (
                    int i = 0;
                    i < ExpectedCoins.Length;
                    i++
                )
                {
                    string coin =
                        ExpectedCoins[i];

                    List<TriangleData>
                        triangles =
                            trianglesByCoin[
                                coin
                            ];

                    GenerateCoinMask(
                        coin,
                        triangles
                    );
                }

                GenerateCombinedMask(
                    trianglesByCoin
                );

                WriteSummary(
                    trianglesByCoin
                );

                LogInfo(
                    "Todas as mascaras foram geradas."
                );
            }
            catch (Exception ex)
            {
                LogError(
                    "Erro gerando mascaras: " +
                    ex
                );
            }
        }

        private static void GenerateCoinMask(
            string coinName,
            List<TriangleData> triangles
        )
        {
            byte[] pixels =
                new byte[
                    AtlasWidth *
                    AtlasHeight *
                    4
                ];

            RasterizeTriangles(
                triangles,
                pixels,
                255,
                255,
                255,
                255,
                false
            );

            string safeName =
                SanitizeFileName(
                    coinName
                );

            string filename =
                safeName +
                "_MASK.png";

            string gamePath =
                Path.Combine(
                    GameOutputDirectory,
                    filename
                );

            string repositoryPath =
                Path.Combine(
                    RepositoryOutputDirectory,
                    filename
                );

            byte[] png =
                EncodeRgbaPng(
                    AtlasWidth,
                    AtlasHeight,
                    pixels
                );

            File.WriteAllBytes(
                gamePath,
                png
            );

            File.WriteAllBytes(
                repositoryPath,
                png
            );

            LogInfo(
                "Mascara gerada: " +
                coinName +
                " | Triangulos=" +
                triangles.Count +
                " | Pixels=" +
                CountOpaquePixels(
                    pixels
                )
            );
        }

        private static void GenerateCombinedMask(
            Dictionary<string, List<TriangleData>>
                trianglesByCoin
        )
        {
            byte[] pixels =
                new byte[
                    AtlasWidth *
                    AtlasHeight *
                    4
                ];

            byte[][] colors =
            {
                new byte[] { 255, 60, 60, 255 },
                new byte[] { 60, 255, 60, 255 },
                new byte[] { 60, 120, 255, 255 },
                new byte[] { 255, 220, 60, 255 },
                new byte[] { 220, 60, 255, 255 }
            };

            for (
                int i = 0;
                i < ExpectedCoins.Length;
                i++
            )
            {
                string coin =
                    ExpectedCoins[i];

                RasterizeTriangles(
                    trianglesByCoin[
                        coin
                    ],
                    pixels,
                    colors[i][0],
                    colors[i][1],
                    colors[i][2],
                    colors[i][3],
                    true
                );
            }

            byte[] png =
                EncodeRgbaPng(
                    AtlasWidth,
                    AtlasHeight,
                    pixels
                );

            string gamePath =
                Path.Combine(
                    GameOutputDirectory,
                    "CurrencyAtlas_ALL_MASKS.png"
                );

            string repositoryPath =
                Path.Combine(
                    RepositoryOutputDirectory,
                    "CurrencyAtlas_ALL_MASKS.png"
                );

            File.WriteAllBytes(
                gamePath,
                png
            );

            File.WriteAllBytes(
                repositoryPath,
                png
            );

            LogInfo(
                "Mascara combinada gerada."
            );
        }

        private static void RasterizeTriangles(
            List<TriangleData> triangles,
            byte[] pixels,
            byte red,
            byte green,
            byte blue,
            byte alpha,
            bool overwrite
        )
        {
            for (
                int i = 0;
                i < triangles.Count;
                i++
            )
            {
                TriangleData triangle =
                    triangles[i];

                RasterizeTriangle(
                    triangle,
                    pixels,
                    red,
                    green,
                    blue,
                    alpha,
                    overwrite
                );
            }
        }

        private static void RasterizeTriangle(
            TriangleData triangle,
            byte[] pixels,
            byte red,
            byte green,
            byte blue,
            byte alpha,
            bool overwrite
        )
        {
            float x0 =
                triangle.U0 *
                (
                    AtlasWidth -
                    1
                );

            float y0 =
                (
                    1f -
                    triangle.V0
                ) *
                (
                    AtlasHeight -
                    1
                );

            float x1 =
                triangle.U1 *
                (
                    AtlasWidth -
                    1
                );

            float y1 =
                (
                    1f -
                    triangle.V1
                ) *
                (
                    AtlasHeight -
                    1
                );

            float x2 =
                triangle.U2 *
                (
                    AtlasWidth -
                    1
                );

            float y2 =
                (
                    1f -
                    triangle.V2
                ) *
                (
                    AtlasHeight -
                    1
                );

            float minX =
                Math.Min(
                    x0,
                    Math.Min(
                        x1,
                        x2
                    )
                );

            float maxX =
                Math.Max(
                    x0,
                    Math.Max(
                        x1,
                        x2
                    )
                );

            float minY =
                Math.Min(
                    y0,
                    Math.Min(
                        y1,
                        y2
                    )
                );

            float maxY =
                Math.Max(
                    y0,
                    Math.Max(
                        y1,
                        y2
                    )
                );

            int pixelMinX =
                ClampInt(
                    (int)Math.Floor(
                        minX
                    ),
                    0,
                    AtlasWidth - 1
                );

            int pixelMaxX =
                ClampInt(
                    (int)Math.Ceiling(
                        maxX
                    ),
                    0,
                    AtlasWidth - 1
                );

            int pixelMinY =
                ClampInt(
                    (int)Math.Floor(
                        minY
                    ),
                    0,
                    AtlasHeight - 1
                );

            int pixelMaxY =
                ClampInt(
                    (int)Math.Ceiling(
                        maxY
                    ),
                    0,
                    AtlasHeight - 1
                );

            float area =
                EdgeFunction(
                    x0,
                    y0,
                    x1,
                    y1,
                    x2,
                    y2
                );

            if (
                Math.Abs(
                    area
                ) <
                0.000001f
            )
            {
                return;
            }

            for (
                int y = pixelMinY;
                y <= pixelMaxY;
                y++
            )
            {
                float py =
                    y +
                    0.5f;

                for (
                    int x = pixelMinX;
                    x <= pixelMaxX;
                    x++
                )
                {
                    float px =
                        x +
                        0.5f;

                    float w0 =
                        EdgeFunction(
                            x1,
                            y1,
                            x2,
                            y2,
                            px,
                            py
                        );

                    float w1 =
                        EdgeFunction(
                            x2,
                            y2,
                            x0,
                            y0,
                            px,
                            py
                        );

                    float w2 =
                        EdgeFunction(
                            x0,
                            y0,
                            x1,
                            y1,
                            px,
                            py
                        );

                    if (
                        (
                            w0 >= -0.0001f &&
                            w1 >= -0.0001f &&
                            w2 >= -0.0001f
                        ) ||
                        (
                            w0 <= 0.0001f &&
                            w1 <= 0.0001f &&
                            w2 <= 0.0001f
                        )
                    )
                    {
                        int offset =
                            (
                                y *
                                AtlasWidth +
                                x
                            ) *
                            4;

                        if (
                            overwrite ||
                            pixels[offset + 3] == 0
                        )
                        {
                            pixels[offset] =
                                red;

                            pixels[offset + 1] =
                                green;

                            pixels[offset + 2] =
                                blue;

                            pixels[offset + 3] =
                                alpha;
                        }
                    }
                }
            }
        }

        private static float EdgeFunction(
            float ax,
            float ay,
            float bx,
            float by,
            float px,
            float py
        )
        {
            return
                (
                    px - ax
                ) *
                (
                    by - ay
                ) -
                (
                    py - ay
                ) *
                (
                    bx - ax
                );
        }

        private static int ClampInt(
            int value,
            int min,
            int max
        )
        {
            if (value < min)
                return min;

            if (value > max)
                return max;

            return value;
        }

        private static long CountOpaquePixels(
            byte[] pixels
        )
        {
            long count =
                0;

            for (
                int i = 3;
                i < pixels.Length;
                i += 4
            )
            {
                if (
                    pixels[i] != 0
                )
                {
                    count++;
                }
            }

            return count;
        }

        private static void WriteSummary(
            Dictionary<string, List<TriangleData>>
                trianglesByCoin
        )
        {
            string gamePath =
                Path.Combine(
                    GameOutputDirectory,
                    "AtlasMaskReport.txt"
                );

            string repositoryPath =
                Path.Combine(
                    RepositoryOutputDirectory,
                    "AtlasMaskReport.txt"
                );

            using (
                StreamWriter writer =
                    new StreamWriter(
                        gamePath,
                        false,
                        Encoding.UTF8
                    )
            )
            {
                writer.WriteLine(
                    "========================================"
                );

                writer.WriteLine(
                    "CURRENCY ATLAS MASK GENERATOR"
                );

                writer.WriteLine(
                    "VERSION: " +
                    VERSION
                );

                writer.WriteLine(
                    "========================================"
                );

                writer.WriteLine(
                    "Atlas Width: " +
                    AtlasWidth
                );

                writer.WriteLine(
                    "Atlas Height: " +
                    AtlasHeight
                );

                writer.WriteLine(
                    "Expected Triangles Per Coin: " +
                    ExpectedTriangleCount
                );

                writer.WriteLine();

                for (
                    int i = 0;
                    i < ExpectedCoins.Length;
                    i++
                )
                {
                    string coin =
                        ExpectedCoins[i];

                    List<TriangleData>
                        triangles =
                            trianglesByCoin[
                                coin
                            ];

                    float minU =
                        1f;

                    float maxU =
                        0f;

                    float minV =
                        1f;

                    float maxV =
                        0f;

                    for (
                        int t = 0;
                        t < triangles.Count;
                        t++
                    )
                    {
                        TriangleData triangle =
                            triangles[t];

                        minU =
                            Math.Min(
                                minU,
                                Math.Min(
                                    triangle.U0,
                                    Math.Min(
                                        triangle.U1,
                                        triangle.U2
                                    )
                                )
                            );

                        maxU =
                            Math.Max(
                                maxU,
                                Math.Max(
                                    triangle.U0,
                                    Math.Max(
                                        triangle.U1,
                                        triangle.U2
                                    )
                                )
                            );

                        minV =
                            Math.Min(
                                minV,
                                Math.Min(
                                    triangle.V0,
                                    Math.Min(
                                        triangle.V1,
                                        triangle.V2
                                    )
                                )
                            );

                        maxV =
                            Math.Max(
                                maxV,
                                Math.Max(
                                    triangle.V0,
                                    Math.Max(
                                        triangle.V1,
                                        triangle.V2
                                    )
                                )
                            );
                    }

                    writer.WriteLine(
                        "----------------------------------------"
                    );

                    writer.WriteLine(
                        "Coin: " +
                        coin
                    );

                    writer.WriteLine(
                        "Triangle Count: " +
                        triangles.Count
                    );

                    writer.WriteLine(
                        "UV Min: (" +
                        minU.ToString(
                            "F6",
                            CultureInfo.InvariantCulture
                        ) +
                        ", " +
                        minV.ToString(
                            "F6",
                            CultureInfo.InvariantCulture
                        ) +
                        ")"
                    );

                    writer.WriteLine(
                        "UV Max: (" +
                        maxU.ToString(
                            "F6",
                            CultureInfo.InvariantCulture
                        ) +
                        ", " +
                        maxV.ToString(
                            "F6",
                            CultureInfo.InvariantCulture
                        ) +
                        ")"
                    );

                    writer.WriteLine(
                        "Pixel X: " +
                        (
                            minU *
                            AtlasWidth
                        ).ToString(
                            "F3",
                            CultureInfo.InvariantCulture
                        ) +
                        " -> " +
                        (
                            maxU *
                            AtlasWidth
                        ).ToString(
                            "F3",
                            CultureInfo.InvariantCulture
                        )
                    );

                    writer.WriteLine(
                        "Pixel Y: " +
                        (
                            (
                                1f -
                                maxV
                            ) *
                            AtlasHeight
                        ).ToString(
                            "F3",
                            CultureInfo.InvariantCulture
                        ) +
                        " -> " +
                        (
                            (
                                1f -
                                minV
                            ) *
                            AtlasHeight
                        ).ToString(
                            "F3",
                            CultureInfo.InvariantCulture
                        )
                    );

                    writer.WriteLine();
                }
            }

            File.Copy(
                gamePath,
                repositoryPath,
                true
            );
        }

        private static string SanitizeFileName(
            string value
        )
        {
            foreach (
                char invalid
                in Path.GetInvalidFileNameChars()
            )
            {
                value =
                    value.Replace(
                        invalid,
                        '_'
                    );
            }

            return value;
        }

        private static string ComputeReportHash(
            string[] lines
        )
        {
            string joined =
                string.Join(
                    "\n",
                    lines
                );

            using (
                SHA256 sha =
                    SHA256.Create()
            )
            {
                byte[] bytes =
                    Encoding.UTF8.GetBytes(
                        joined
                    );

                byte[] hash =
                    sha.ComputeHash(
                        bytes
                    );

                StringBuilder builder =
                    new StringBuilder(
                        hash.Length *
                        2
                    );

                for (
                    int i = 0;
                    i < hash.Length;
                    i++
                )
                {
                    builder.Append(
                        hash[i].ToString(
                            "x2",
                            CultureInfo.InvariantCulture
                        )
                    );
                }

                return builder.ToString();
            }
        }

        private static byte[] EncodeRgbaPng(
            int width,
            int height,
            byte[] rgba
        )
        {
            using (
                MemoryStream output =
                    new MemoryStream()
            )
            {
                WritePngSignature(
                    output
                );

                byte[] ihdr =
                    new byte[13];

                WriteUInt32(
                    ihdr,
                    0,
                    (uint)width
                );

                WriteUInt32(
                    ihdr,
                    4,
                    (uint)height
                );

                ihdr[8] =
                    8;

                ihdr[9] =
                    6;

                ihdr[10] =
                    0;

                ihdr[11] =
                    0;

                ihdr[12] =
                    0;

                WriteChunk(
                    output,
                    "IHDR",
                    ihdr
                );

                byte[] raw =
                    new byte[
                        (
                            width *
                            4 +
                            1
                        ) *
                        height
                    ];

                int sourceOffset =
                    0;

                int destinationOffset =
                    0;

                for (
                    int y = 0;
                    y < height;
                    y++
                )
                {
                    raw[
                        destinationOffset++
                    ] =
                        0;

                    Buffer.BlockCopy(
                        rgba,
                        sourceOffset,
                        raw,
                        destinationOffset,
                        width *
                        4
                    );

                    sourceOffset +=
                        width *
                        4;

                    destinationOffset +=
                        width *
                        4;
                }

                byte[] zlib =
                    CompressZlib(
                        raw
                    );

                WriteChunk(
                    output,
                    "IDAT",
                    zlib
                );

                WriteChunk(
                    output,
                    "IEND",
                    Array.Empty<byte>()
                );

                return output.ToArray();
            }
        }

        private static void WritePngSignature(
            Stream stream
        )
        {
            byte[] signature =
            {
                137,
                80,
                78,
                71,
                13,
                10,
                26,
                10
            };

            stream.Write(
                signature,
                0,
                signature.Length
            );
        }

        private static byte[] CompressZlib(
            byte[] data
        )
        {
            using (
                MemoryStream compressed =
                    new MemoryStream()
            )
            {
                compressed.WriteByte(
                    0x78
                );

                compressed.WriteByte(
                    0x9C
                );

                using (
                    MemoryStream deflateTarget =
                        new MemoryStream()
                )
                {
                    using (
                        DeflateStream deflate =
                            new DeflateStream(
                                deflateTarget,
                                CompressionLevel.Fastest,
                                true
                            )
                    )
                    {
                        deflate.Write(
                            data,
                            0,
                            data.Length
                        );
                    }

                    byte[] deflateBytes =
                        deflateTarget.ToArray();

                    compressed.Write(
                        deflateBytes,
                        0,
                        deflateBytes.Length
                    );
                }

                uint adler =
                    Adler32(
                        data
                    );

                compressed.WriteByte(
                    (byte)(
                        adler >>
                        24
                    )
                );

                compressed.WriteByte(
                    (byte)(
                        adler >>
                        16
                    )
                );

                compressed.WriteByte(
                    (byte)(
                        adler >>
                        8
                    )
                );

                compressed.WriteByte(
                    (byte)adler
                );

                return compressed.ToArray();
            }
        }

        private static uint Adler32(
            byte[] data
        )
        {
            const uint Mod =
                65521;

            uint a =
                1;

            uint b =
                0;

            for (
                int i = 0;
                i < data.Length;
                i++
            )
            {
                a =
                    (
                        a +
                        data[i]
                    ) %
                    Mod;

                b =
                    (
                        b +
                        a
                    ) %
                    Mod;
            }

            return (
                b <<
                16
            ) |
            a;
        }

        private static void WriteChunk(
            Stream stream,
            string type,
            byte[] data
        )
        {
            byte[] typeBytes =
                Encoding.ASCII.GetBytes(
                    type
                );

            WriteUInt32(
                stream,
                (uint)data.Length
            );

            stream.Write(
                typeBytes,
                0,
                4
            );

            if (
                data.Length > 0
            )
            {
                stream.Write(
                    data,
                    0,
                    data.Length
                );
            }

            uint crc =
                Crc32(
                    typeBytes,
                    data
                );

            WriteUInt32(
                stream,
                crc
            );
        }

        private static void WriteUInt32(
            Stream stream,
            uint value
        )
        {
            stream.WriteByte(
                (byte)(
                    value >>
                    24
                )
            );

            stream.WriteByte(
                (byte)(
                    value >>
                    16
                )
            );

            stream.WriteByte(
                (byte)(
                    value >>
                    8
                )
            );

            stream.WriteByte(
                (byte)value
            );
        }

        private static void WriteUInt32(
            byte[] buffer,
            int offset,
            uint value
        )
        {
            buffer[
                offset
            ] =
                (byte)(
                    value >>
                    24
                );

            buffer[
                offset + 1
            ] =
                (byte)(
                    value >>
                    16
                );

            buffer[
                offset + 2
            ] =
                (byte)(
                    value >>
                    8
                );

            buffer[
                offset + 3
            ] =
                (byte)value;
        }

        private static uint Crc32(
            byte[] type,
            byte[] data
        )
        {
            uint crc =
                0xFFFFFFFF;

            for (
                int i = 0;
                i < type.Length;
                i++
            )
            {
                crc =
                    UpdateCrc(
                        crc,
                        type[i]
                    );
            }

            for (
                int i = 0;
                i < data.Length;
                i++
            )
            {
                crc =
                    UpdateCrc(
                        crc,
                        data[i]
                    );
            }

            return
                crc ^
                0xFFFFFFFF;
        }

        private static uint UpdateCrc(
            uint crc,
            byte value
        )
        {
            uint c =
                crc ^
                value;

            for (
                int k = 0;
                k < 8;
                k++
            )
            {
                if (
                    (
                        c &
                        1
                    ) != 0
                )
                {
                    c =
                        0xEDB88320U ^
                        (
                            c >>
                            1
                        );
                }
                else
                {
                    c >>=
                        1;
                }
            }

            return c;
        }

        private static void InitializeReport()
        {
            try
            {
                string path =
                    Path.Combine(
                        GameOutputDirectory,
                        "AtlasMaskGenerator.txt"
                    );

                using (
                    StreamWriter writer =
                        new StreamWriter(
                            path,
                            false,
                            Encoding.UTF8
                        )
                )
                {
                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "CURRENCY ATLAS MASK GENERATOR"
                    );

                    writer.WriteLine(
                        "VERSION: " +
                        VERSION
                    );

                    writer.WriteLine(
                        "========================================"
                    );

                    writer.WriteLine(
                        "Objetivo: rasterizar os triangulos UV."
                    );

                    writer.WriteLine(
                        "Atlas: 2048x2048"
                    );

                    writer.WriteLine(
                        "Mesh original: NAO ALTERADO."
                    );

                    writer.WriteLine(
                        "Material original: NAO ALTERADO."
                    );

                    writer.WriteLine(
                        "UV original: NAO ALTERADO."
                    );

                    writer.WriteLine();
                }
            }
            catch
            {
            }
        }

        private static void LogInfo(
            string message
        )
        {
            try
            {
                Instance.Log.LogInfo(
                    message
                );
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
                Instance.Log.LogError(
                    message
                );
            }
            catch
            {
            }
        }

        private static AtlasMaskGenerator Instance;

        private class TriangleData
        {
            public int Index0;
            public int Index1;
            public int Index2;

            public float U0;
            public float V0;

            public float U1;
            public float V1;

            public float U2;
            public float V2;

            public float Area;
        }
    }
}
