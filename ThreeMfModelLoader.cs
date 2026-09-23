using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Xml.Linq;

namespace ThreeMFExplorer
{
    public sealed class ThreeMfSceneObject
    {
        public required int DisplayIndex { get; init; }
        public required int SourceObjectId { get; init; }
        public int PlateId { get; init; } = 1;
        public required string Name { get; init; }
        public required Model3DGroup Model { get; init; }

        public Rect3D Bounds => Model.Bounds;

        public required int TriangleCount { get; init; }
    }

    public sealed class ThreeMfPlate
    {
        public required int PlateId { get; init; }
        public required string Name { get; init; }
        public required IReadOnlyList<int> ObjectIds { get; init; }
        public string? ThumbnailPath { get; init; }
        public string? TopViewPath { get; init; }
        public string? PickViewPath { get; init; }
        public ImageSource? PreviewImage { get; init; }
    }

    public sealed class ThreeMfScene
    {
        public required Model3DGroup Model { get; init; }
        public required IReadOnlyList<ThreeMfSceneObject> Objects { get; init; }

        public IReadOnlyList<ThreeMfPlate> Plates { get; init; } =
            Array.Empty<ThreeMfPlate>();

        public string SourcePrinterName { get; init; } = string.Empty;
        public double SourceBedWidth { get; init; } = 256.0;
        public double SourceBedDepth { get; init; } = 256.0;
        public double SourceBedHeight { get; init; } = 256.0;
    }

    public static class ThreeMfModelLoader
    {
        // WPF Media3D can become unstable with extremely dense meshes.
        // The source triangle count is still retained exactly for display,
        // while only the render mesh is reduced when necessary.
        private const int MaxRenderedTrianglesPerMesh = 180000;

        private sealed class ModelDocument
        {
            public required string Path { get; init; }
            public required XDocument Xml { get; init; }
            public required XNamespace Ns { get; init; }
            public required double UnitScaleToMm { get; init; }
        }

        public static Model3DGroup Load(string filePath)
        {
            return LoadScene(filePath).Model;
        }

        public static ThreeMfScene LoadScene(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("No 3MF file was specified.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("The 3MF file was not found.", filePath);

            using ZipArchive archive = ZipFile.OpenRead(filePath);

            ZipArchiveEntry? rootEntry =
                archive.GetEntry("3D/3dmodel.model")
                ?? archive.Entries.FirstOrDefault(
                    e => e.FullName.EndsWith(
                        ".model",
                        StringComparison.OrdinalIgnoreCase));

            if (rootEntry is null)
                throw new InvalidDataException(
                    "The 3MF file contains no model data.");

            Dictionary<string, ModelDocument> cache =
                new(StringComparer.OrdinalIgnoreCase);

            ModelDocument root =
                LoadDocument(
                    archive,
                    rootEntry.FullName,
                    cache);

            Model3DGroup sceneModel = new();
            List<ThreeMfSceneObject> sceneObjects = new();
            Dictionary<int, string> bambuObjectNames =
                LoadBambuObjectNames(archive);

            List<ThreeMfPlate> bambuPlates =
                LoadBambuPlates(archive);

            Dictionary<int, int> objectPlateMap =
                bambuPlates
                    .SelectMany(
                        plate =>
                            plate.ObjectIds.Select(
                                objectId =>
                                    new
                                    {
                                        objectId,
                                        plate.PlateId
                                    }))
                    .GroupBy(item => item.objectId)
                    .ToDictionary(
                        group => group.Key,
                        group => group.First().PlateId);

            XElement? build =
                root.Xml.Root?.Element(
                    root.Ns + "build");

            (string sourcePrinterName,
             double sourceBedWidth,
             double sourceBedDepth,
             double sourceBedHeight) =
                LoadSourcePrinterInfo(archive);

            Dictionary<int, Vector3D> plateOffsets =
                build is null
                    ? new Dictionary<int, Vector3D>()
                    : CalculatePlateOffsets(
                        build,
                        root,
                        objectPlateMap,
                        sourceBedWidth,
                        sourceBedDepth);

            if (build != null &&
                build.Elements(root.Ns + "item").Any())
            {
                int displayIndex = 1;

                foreach (XElement item
                         in build.Elements(root.Ns + "item"))
                {
                    int objectId =
                        ParseInt(
                            item.Attribute("objectid")?.Value);

                    if (objectId < 0)
                        continue;

                    Matrix3D transform =
                        ParseTransform(
                            item.Attribute("transform")?.Value,
                            root.UnitScaleToMm);

                    int currentPlateId =
                        objectPlateMap.TryGetValue(
                            objectId,
                            out int mappedPlateId)
                            ? mappedPlateId
                            : 1;

                    if (plateOffsets.TryGetValue(
                            currentPlateId,
                            out Vector3D plateOffset))
                    {
                        transform.OffsetX -=
                            plateOffset.X;

                        transform.OffsetY -=
                            plateOffset.Y;
                    }

                    Model3DGroup objectGroup = new();

                    int sourceTriangleCount = 0;

                    AppendObject(
                        archive,
                        root,
                        objectId,
                        transform,
                        objectGroup,
                        cache,
                        new HashSet<string>(),
                        ref sourceTriangleCount);

                    if (objectGroup.Children.Count == 0)
                        continue;

                    sceneModel.Children.Add(objectGroup);

                    sceneObjects.Add(
                        new ThreeMfSceneObject
                        {
                            DisplayIndex = displayIndex,
                            SourceObjectId = objectId,
                            PlateId =
                                currentPlateId,
                            Name = ResolveObjectName(
                                root,
                                objectId,
                                item,
                                bambuObjectNames),
                            Model = objectGroup,
                            TriangleCount =
                                sourceTriangleCount
                        });

                    displayIndex++;
                }
            }
            else
            {
                XElement? resources =
                    root.Xml.Root?.Element(
                        root.Ns + "resources");

                if (resources != null)
                {
                    int displayIndex = 1;

                    foreach (XElement obj
                             in resources.Elements(
                                 root.Ns + "object"))
                    {
                        int objectId =
                            ParseInt(
                                obj.Attribute("id")?.Value);

                        if (objectId < 0)
                            continue;

                        Model3DGroup objectGroup = new();

                        int sourceTriangleCount = 0;

                        AppendObject(
                            archive,
                            root,
                            objectId,
                            Matrix3D.Identity,
                            objectGroup,
                            cache,
                            new HashSet<string>(),
                            ref sourceTriangleCount);

                        if (objectGroup.Children.Count == 0)
                            continue;

                        sceneModel.Children.Add(objectGroup);

                        sceneObjects.Add(
                            new ThreeMfSceneObject
                            {
                                DisplayIndex = displayIndex,
                                SourceObjectId = objectId,
                            PlateId =
                                objectPlateMap.TryGetValue(
                                    objectId,
                                    out int plateId)
                                    ? plateId
                                    : 1,
                                Name = ResolveObjectName(
                                    root,
                                    objectId,
                                    null,
                                    bambuObjectNames),
                                Model = objectGroup,
                                TriangleCount =
                                    sourceTriangleCount
                            });

                        displayIndex++;
                    }
                }
            }

            if (sceneObjects.Count == 0 ||
                sceneModel.Children.Count == 0)
            {
                throw new InvalidDataException(
                    "No usable 3D geometry was found in the 3MF file.");
            }

            return new ThreeMfScene
            {
                Model = sceneModel,
                Objects = sceneObjects,
                SourcePrinterName = sourcePrinterName,
                SourceBedWidth = sourceBedWidth,
                SourceBedDepth = sourceBedDepth,
                SourceBedHeight = sourceBedHeight,
                Plates =
                    bambuPlates.Count > 0
                        ? bambuPlates
                        : new[]
                        {
                            new ThreeMfPlate
                            {
                                PlateId = 1,
                                Name = "Plate 1",
                                ObjectIds =
                                    sceneObjects
                                        .Select(
                                            item =>
                                                item.SourceObjectId)
                                        .ToList()
                            }
                        }
            };
        }

        private static (
            string PrinterName,
            double Width,
            double Depth,
            double Height)
            LoadSourcePrinterInfo(
                ZipArchive archive)
        {
            const double fallbackWidth = 256.0;
            const double fallbackDepth = 256.0;
            const double fallbackHeight = 256.0;

            ZipArchiveEntry? entry =
                archive.Entries.FirstOrDefault(
                    item =>
                        item.FullName.Equals(
                            "Metadata/project_settings.config",
                            StringComparison.OrdinalIgnoreCase));

            if (entry is null)
            {
                return (
                    string.Empty,
                    fallbackWidth,
                    fallbackDepth,
                    fallbackHeight);
            }

            try
            {
                using StreamReader reader =
                    new StreamReader(
                        entry.Open());

                string text =
                    reader.ReadToEnd();

                string printerName =
                    string.Empty;

                Match printerMatch =
                    Regex.Match(
                        text,
                        @"""printer_model""\s*:\s*""(?<name>(?:\\.|[^""])*)""",
                        RegexOptions.IgnoreCase);

                if (printerMatch.Success)
                {
                    printerName =
                        printerMatch
                            .Groups["name"]
                            .Value
                            .Replace("\\\"", "\"")
                            .Replace("\\\\", "\\")
                            .Trim();
                }

                double width =
                    fallbackWidth;

                double depth =
                    fallbackDepth;

                Match areaMatch =
                    Regex.Match(
                        text,
                        @"""printable_area""\s*:\s*\[\s*""0x0""\s*,\s*""(?<w>[-+0-9.,]+)x0""\s*,\s*""[-+0-9.,]+x(?<h>[-+0-9.,]+)""",
                        RegexOptions.IgnoreCase);

                if (areaMatch.Success)
                {
                    if (double.TryParse(
                            areaMatch.Groups["w"].Value.Replace(',', '.'),
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out double parsedWidth) &&
                        parsedWidth > 0)
                    {
                        width = parsedWidth;
                    }

                    if (double.TryParse(
                            areaMatch.Groups["h"].Value.Replace(',', '.'),
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out double parsedDepth) &&
                        parsedDepth > 0)
                    {
                        depth = parsedDepth;
                    }
                }

                double height =
                    fallbackHeight;

                Match heightMatch =
                    Regex.Match(
                        text,
                        @"""printable_height""\s*:\s*(?:""(?<quoted>[-+0-9.,]+)""|(?<number>[-+0-9.,]+))",
                        RegexOptions.IgnoreCase);

                if (heightMatch.Success)
                {
                    string heightText =
                        heightMatch.Groups["quoted"].Success
                            ? heightMatch.Groups["quoted"].Value
                            : heightMatch.Groups["number"].Value;

                    if (double.TryParse(
                            heightText.Replace(',', '.'),
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out double parsedHeight) &&
                        parsedHeight > 0)
                    {
                        height = parsedHeight;
                    }
                }

                return (
                    printerName,
                    width,
                    depth,
                    height);
            }
            catch
            {
                return (
                    string.Empty,
                    fallbackWidth,
                    fallbackDepth,
                    fallbackHeight);
            }
        }

        private static Dictionary<int, Vector3D>
            CalculatePlateOffsets(
                XElement build,
                ModelDocument root,
                IReadOnlyDictionary<int, int> objectPlateMap,
                double sourceBedWidth,
                double sourceBedDepth)
        {
            Dictionary<int, List<Point3D>> translations =
                new();

            foreach (XElement item
                     in build.Elements(root.Ns + "item"))
            {
                int objectId =
                    ParseInt(
                        item.Attribute("objectid")?.Value);

                if (!objectPlateMap.TryGetValue(
                        objectId,
                        out int plateId))
                {
                    continue;
                }

                Matrix3D transform =
                    ParseTransform(
                        item.Attribute("transform")?.Value,
                        root.UnitScaleToMm);

                if (!translations.TryGetValue(
                        plateId,
                        out List<Point3D>? points))
                {
                    points = new List<Point3D>();
                    translations[plateId] = points;
                }

                points.Add(
                    new Point3D(
                        transform.OffsetX,
                        transform.OffsetY,
                        transform.OffsetZ));
            }

            // Bambu Studio lays multiple virtual plates out in a grid.
            // The gap between neighbouring source plates is 36 mm.
            // Example from the supplied Skink project:
            // A1 mini 180 mm bed -> 216 mm virtual grid spacing.
            double strideX =
                sourceBedWidth + 36.0;

            double strideY =
                sourceBedDepth + 36.0;

            Dictionary<int, Vector3D> result =
                new();

            foreach (KeyValuePair<int, List<Point3D>> pair
                     in translations)
            {
                if (pair.Value.Count == 0)
                    continue;

                double averageX =
                    pair.Value.Average(
                        point => point.X);

                double averageY =
                    pair.Value.Average(
                        point => point.Y);

                // Plate membership is already known from Bambu's metadata.
                // Use the centre of that complete object group to identify
                // the virtual tile, then remove only the tile displacement.
                double offsetX =
                    Math.Floor(
                        averageX / strideX)
                    * strideX;

                double offsetY =
                    Math.Floor(
                        averageY / strideY)
                    * strideY;

                // The 36 mm value belongs only to Bambu Studio's
                // virtual spacing between plates. It must not be split and
                // added to the local printable-area origin. The build-item
                // transforms already contain the saved position inside the
                // source plate. We therefore remove only the complete virtual
                // tile displacement and preserve the original local X/Y.
                result[pair.Key] =
                    new Vector3D(
                        offsetX,
                        offsetY,
                        0);
            }

            return result;
        }

        private static List<ThreeMfPlate>
            LoadBambuPlates(
                ZipArchive archive)
        {
            List<ThreeMfPlate> result = new();

            ZipArchiveEntry? entry =
                archive.Entries.FirstOrDefault(
                    e =>
                        e.FullName.Equals(
                            "Metadata/model_settings.config",
                            StringComparison.OrdinalIgnoreCase));

            if (entry is null)
                return result;

            try
            {
                using Stream stream = entry.Open();
                XDocument config = XDocument.Load(stream);

                if (config.Root is null)
                    return result;

                foreach (XElement plate
                         in config.Root.Elements("plate"))
                {
                    Dictionary<string, string> metadata =
                        plate.Elements("metadata")
                            .Where(
                                item =>
                                    item.Attribute("key") != null)
                            .GroupBy(
                                item =>
                                    item.Attribute("key")!.Value,
                                StringComparer.OrdinalIgnoreCase)
                            .ToDictionary(
                                group => group.Key,
                                group =>
                                    group.First()
                                        .Attribute("value")?.Value
                                    ?? string.Empty,
                                StringComparer.OrdinalIgnoreCase);

                    if (!metadata.TryGetValue(
                            "plater_id",
                            out string? idText) ||
                        !int.TryParse(
                            idText,
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out int plateId))
                    {
                        continue;
                    }

                    List<int> objectIds = new();

                    foreach (XElement instance
                             in plate.Elements(
                                 "model_instance"))
                    {
                        XElement? idMetadata =
                            instance.Elements("metadata")
                                .FirstOrDefault(
                                    item =>
                                        string.Equals(
                                            item.Attribute("key")?.Value,
                                            "object_id",
                                            StringComparison.OrdinalIgnoreCase));

                        if (int.TryParse(
                                idMetadata?
                                    .Attribute("value")?.Value,
                                NumberStyles.Integer,
                                CultureInfo.InvariantCulture,
                                out int objectId))
                        {
                            objectIds.Add(objectId);
                        }
                    }

                    metadata.TryGetValue(
                        "plater_name",
                        out string? name);

                    metadata.TryGetValue(
                        "thumbnail_file",
                        out string? thumbnail);

                    metadata.TryGetValue(
                        "top_file",
                        out string? top);

                    metadata.TryGetValue(
                        "pick_file",
                        out string? pick);

                    string? thumbnailPath =
                        NormalizeMetadataPath(
                            thumbnail);

                    string? topPath =
                        NormalizeMetadataPath(
                            top);

                    string? pickPath =
                        NormalizeMetadataPath(
                            pick);

                    result.Add(
                        new ThreeMfPlate
                        {
                            PlateId = plateId,
                            Name =
                                string.IsNullOrWhiteSpace(name)
                                    ? $"Plate {plateId}"
                                    : name.Trim(),
                            ObjectIds = objectIds,
                            ThumbnailPath =
                                thumbnailPath,
                            TopViewPath =
                                topPath,
                            PickViewPath =
                                pickPath,
                            PreviewImage =
                                LoadPlatePreviewImage(
                                    archive,
                                    plateId,
                                    thumbnailPath,
                                    topPath,
                                    pickPath)
                        });
                }
            }
            catch
            {
                // Plate metadata is optional.
            }

            return result
                .OrderBy(item => item.PlateId)
                .ToList();
        }

        private static ImageSource?
            LoadPlatePreviewImage(
                ZipArchive archive,
                int plateId,
                params string?[] metadataPaths)
        {
            List<string> candidates =
                metadataPaths
                    .Where(
                        path =>
                            !string.IsNullOrWhiteSpace(
                                path))
                    .Select(
                        path =>
                            NormalizeArchivePath(
                                path!))
                    .ToList();

            candidates.Add(
                $"Metadata/plate_{plateId}.png");

            candidates.Add(
                $"Metadata/top_{plateId}.png");

            candidates.Add(
                $"Metadata/pick_{plateId}.png");

            foreach (string candidate
                     in candidates.Distinct(
                         StringComparer.OrdinalIgnoreCase))
            {
                ZipArchiveEntry? imageEntry =
                    archive.Entries.FirstOrDefault(
                        entry =>
                            entry.FullName.Equals(
                                candidate,
                                StringComparison.OrdinalIgnoreCase));

                if (imageEntry is null)
                    continue;

                try
                {
                    // Detach the image completely from the ZipArchiveEntry.
                    // WPF can continue bitmap decoding on a background thread.
                    // A stream backed by the ZIP/DeflateStream must therefore
                    // never remain as BitmapImage.StreamSource.
                    byte[] imageBytes;

                    using (Stream zipStream = imageEntry.Open())
                    using (MemoryStream copy = new MemoryStream())
                    {
                        zipStream.CopyTo(copy);
                        imageBytes = copy.ToArray();
                    }

                    if (imageBytes.Length == 0)
                        continue;

                    using MemoryStream imageStream =
                        new MemoryStream(
                            imageBytes,
                            writable: false);

                    BitmapImage bitmap =
                        new BitmapImage();

                    bitmap.BeginInit();
                    bitmap.CacheOption =
                        BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions =
                        BitmapCreateOptions.PreservePixelFormat;
                    bitmap.StreamSource =
                        imageStream;
                    bitmap.EndInit();

                    if (bitmap.CanFreeze)
                        bitmap.Freeze();

                    return bitmap;
                }
                catch
                {
                    // Plate preview is optional. Try the next candidate.
                }
            }

            return null;
        }

        private static string?
            NormalizeMetadataPath(
                string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value
                .Replace('\\', '/')
                .TrimStart('/');
        }

        private static Dictionary<int, string>
            LoadBambuObjectNames(
                ZipArchive archive)
        {
            Dictionary<int, string> result =
                new();

            ZipArchiveEntry? entry =
                archive.Entries.FirstOrDefault(
                    e =>
                        e.FullName.Equals(
                            "Metadata/model_settings.config",
                            StringComparison.OrdinalIgnoreCase));

            if (entry is null)
                return result;

            try
            {
                using Stream stream =
                    entry.Open();

                XDocument config =
                    XDocument.Load(stream);

                XElement? root =
                    config.Root;

                if (root is null)
                    return result;

                foreach (XElement objectElement
                         in root.Elements("object"))
                {
                    int objectId =
                        ParseInt(
                            objectElement.Attribute("id")?.Value);

                    if (objectId < 0)
                        continue;

                    XElement? nameElement =
                        objectElement
                            .Elements("metadata")
                            .FirstOrDefault(
                                metadata =>
                                    string.Equals(
                                        metadata.Attribute("key")?.Value,
                                        "name",
                                        StringComparison.OrdinalIgnoreCase));

                    string? name =
                        nameElement?.Attribute("value")?.Value;

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        result[objectId] =
                            name.Trim();
                    }
                }
            }
            catch
            {
                // The Bambu-specific metadata is optional.
                // Geometry loading must still work without it.
            }

            return result;
        }

        private static string ResolveObjectName(
            ModelDocument doc,
            int objectId,
            XElement? buildItem,
            IReadOnlyDictionary<int, string> bambuObjectNames)
        {
            if (bambuObjectNames.TryGetValue(
                    objectId,
                    out string? bambuName) &&
                !string.IsNullOrWhiteSpace(
                    bambuName))
            {
                return bambuName.Trim();
            }

            XElement? resources =
                doc.Xml.Root?.Element(
                    doc.Ns + "resources");

            XElement? obj =
                resources?
                    .Elements(doc.Ns + "object")
                    .FirstOrDefault(
                        x =>
                            ParseInt(
                                x.Attribute("id")?.Value)
                            == objectId);

            string? name =
                obj?.Attribute("name")?.Value;

            if (string.IsNullOrWhiteSpace(name))
            {
                XElement? nameMetadata =
                    obj?
                        .Elements(doc.Ns + "metadata")
                        .FirstOrDefault(
                            x =>
                            {
                                string metadataName =
                                    x.Attribute("name")?.Value
                                    ?? string.Empty;

                                return
                                    metadataName.Equals(
                                        "name",
                                        StringComparison.OrdinalIgnoreCase)
                                    || metadataName.Equals(
                                        "title",
                                        StringComparison.OrdinalIgnoreCase);
                            });

                name = nameMetadata?.Value;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                name =
                    buildItem?
                        .Attributes()
                        .FirstOrDefault(
                            a =>
                                a.Name.LocalName.Equals(
                                    "partnumber",
                                    StringComparison.OrdinalIgnoreCase))
                        ?.Value;
            }

            return name?.Trim() ?? string.Empty;
        }

        private static void AppendObject(
            ZipArchive archive,
            ModelDocument doc,
            int objectId,
            Matrix3D parentTransform,
            Model3DGroup target,
            Dictionary<string, ModelDocument> cache,
            HashSet<string> recursionGuard,
            ref int sourceTriangleCount)
        {
            string guardKey =
                $"{doc.Path}|{objectId}";

            if (!recursionGuard.Add(guardKey))
                return;

            try
            {
                XElement? resources =
                    doc.Xml.Root?.Element(
                        doc.Ns + "resources");

                XElement? obj =
                    resources?
                        .Elements(doc.Ns + "object")
                        .FirstOrDefault(
                            x =>
                                ParseInt(
                                    x.Attribute("id")?.Value)
                                == objectId);

                if (obj is null)
                    return;

                XElement? mesh =
                    obj.Element(
                        doc.Ns + "mesh");

                if (mesh != null)
                {
                    XElement? trianglesElement =
                        mesh.Element(
                            doc.Ns + "triangles");

                    if (trianglesElement is not null)
                    {
                        sourceTriangleCount +=
                            trianglesElement
                                .Elements(
                                    doc.Ns + "triangle")
                                .Count();
                    }

                    GeometryModel3D? geometry =
                        CreateGeometry(
                            mesh,
                            doc.Ns,
                            doc.UnitScaleToMm);

                    if (geometry != null)
                    {
                        geometry.Transform =
                            new MatrixTransform3D(
                                parentTransform);

                        target.Children.Add(
                            geometry);
                    }
                }

                XElement? components =
                    obj.Element(
                        doc.Ns + "components");

                if (components == null)
                    return;

                foreach (XElement component
                         in components.Elements(
                             doc.Ns + "component"))
                {
                    int childId =
                        ParseInt(
                            component.Attribute(
                                "objectid")?.Value);

                    if (childId < 0)
                        continue;

                    string? externalPath =
                        component
                            .Attributes()
                            .FirstOrDefault(
                                a =>
                                    a.Name.LocalName.Equals(
                                        "path",
                                        StringComparison.OrdinalIgnoreCase))
                            ?.Value;

                    ModelDocument childDoc = doc;

                    if (!string.IsNullOrWhiteSpace(
                            externalPath))
                    {
                        string normalized =
                            NormalizeArchivePath(
                                externalPath);

                        childDoc =
                            LoadDocument(
                                archive,
                                normalized,
                                cache);
                    }

                    Matrix3D local =
                        ParseTransform(
                            component.Attribute(
                                "transform")?.Value,
                            childDoc.UnitScaleToMm);

                    Matrix3D combined = local;
                    combined.Append(parentTransform);

                    AppendObject(
                        archive,
                        childDoc,
                        childId,
                        combined,
                        target,
                        cache,
                        recursionGuard,
                        ref sourceTriangleCount);
                }
            }
            finally
            {
                recursionGuard.Remove(guardKey);
            }
        }

        private static ModelDocument LoadDocument(
            ZipArchive archive,
            string path,
            Dictionary<string, ModelDocument> cache)
        {
            path = NormalizeArchivePath(path);

            if (cache.TryGetValue(
                    path,
                    out ModelDocument? cached))
            {
                return cached;
            }

            ZipArchiveEntry? entry =
                archive.GetEntry(path);

            if (entry is null)
            {
                throw new InvalidDataException(
                    $"Referenced 3MF model part was not found: {path}");
            }

            using Stream stream =
                entry.Open();

            XDocument xml =
                XDocument.Load(stream);

            XElement root =
                xml.Root
                ?? throw new InvalidDataException(
                    $"Invalid 3MF model XML: {path}");

            ModelDocument doc =
                new()
                {
                    Path = path,
                    Xml = xml,
                    Ns = root.Name.Namespace,
                    UnitScaleToMm =
                        UnitScale(
                            root.Attribute("unit")?.Value)
                };

            cache[path] = doc;

            return doc;
        }

        private static GeometryModel3D? CreateGeometry(
            XElement mesh,
            XNamespace ns,
            double scale)
        {
            XElement? verticesElement =
                mesh.Element(
                    ns + "vertices");

            XElement? trianglesElement =
                mesh.Element(
                    ns + "triangles");

            if (verticesElement == null ||
                trianglesElement == null)
            {
                return null;
            }

            MeshGeometry3D geometry = new();

            foreach (XElement vertex
                     in verticesElement.Elements(
                         ns + "vertex"))
            {
                geometry.Positions.Add(
                    new Point3D(
                        ParseDouble(
                            vertex.Attribute("x")?.Value)
                        * scale,
                        ParseDouble(
                            vertex.Attribute("y")?.Value)
                        * scale,
                        ParseDouble(
                            vertex.Attribute("z")?.Value)
                        * scale));
            }

            int sourceTriangleCount =
                trianglesElement
                    .Elements(
                        ns + "triangle")
                    .Count();

            int renderStep =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        sourceTriangleCount /
                        (double)MaxRenderedTrianglesPerMesh));

            int triangleIndex = 0;

            foreach (XElement triangle
                     in trianglesElement.Elements(
                         ns + "triangle"))
            {
                bool renderTriangle =
                    triangleIndex % renderStep == 0;

                triangleIndex++;

                if (!renderTriangle)
                    continue;

                int a =
                    ParseInt(
                        triangle.Attribute("v1")?.Value);

                int b =
                    ParseInt(
                        triangle.Attribute("v2")?.Value);

                int c =
                    ParseInt(
                        triangle.Attribute("v3")?.Value);

                if (a < 0 ||
                    b < 0 ||
                    c < 0 ||
                    a >= geometry.Positions.Count ||
                    b >= geometry.Positions.Count ||
                    c >= geometry.Positions.Count)
                {
                    continue;
                }

                geometry.TriangleIndices.Add(a);
                geometry.TriangleIndices.Add(b);
                geometry.TriangleIndices.Add(c);
            }

            if (geometry.TriangleIndices.Count == 0)
                return null;

            // Geometry itself never changes after loading. Freezing it greatly
            // reduces WPF change-notification overhead and makes rendering of
            // large 3MF files considerably more stable.
            if (geometry.CanFreeze)
                geometry.Freeze();

            MaterialGroup material = new();

            material.Children.Add(
                new DiffuseMaterial(
                    new SolidColorBrush(
                        Color.FromRgb(
                            190,
                            195,
                            205))));

            material.Children.Add(
                new SpecularMaterial(
                    new SolidColorBrush(
                        Color.FromRgb(
                            90,
                            95,
                            105)),
                    35));

            return new GeometryModel3D
            {
                Geometry = geometry,
                Material = material,
                BackMaterial = material
            };
        }

        private static Matrix3D ParseTransform(
            string? text,
            double translationScale)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Matrix3D.Identity;

            string[] parts =
                text.Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 12)
                return Matrix3D.Identity;

            double[] values =
                parts
                    .Select(ParseDouble)
                    .ToArray();

            return new Matrix3D(
                values[0],
                values[1],
                values[2],
                0,
                values[3],
                values[4],
                values[5],
                0,
                values[6],
                values[7],
                values[8],
                0,
                values[9] * translationScale,
                values[10] * translationScale,
                values[11] * translationScale,
                1);
        }

        private static double UnitScale(
            string? unit)
        {
            return unit?
                .ToLowerInvariant()
                switch
                {
                    "micron" => 0.001,
                    "millimeter" or null or "" => 1.0,
                    "centimeter" => 10.0,
                    "meter" => 1000.0,
                    "inch" => 25.4,
                    "foot" => 304.8,
                    _ => 1.0
                };
        }

        private static string NormalizeArchivePath(
            string path)
        {
            return path
                .Replace('\\', '/')
                .TrimStart('/');
        }

        private static double ParseDouble(
            string? value)
        {
            return double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double parsed)
                    ? parsed
                    : 0;
        }

        private static int ParseInt(
            string? value)
        {
            return int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int parsed)
                    ? parsed
                    : -1;
        }
    }
}
