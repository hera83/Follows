using System.Text;
using DocumentFormat.OpenXml.Packaging;
using PDFtoImage;
using SkiaSharp;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;
using S = DocumentFormat.OpenXml.Spreadsheet;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace web.Infrastructure
{
    /// <summary>
    /// Pulls a loose, Markdown-ish text representation out of an uploaded document, for the Documents
    /// preview modal's "Oversæt"-button (see DocumentsService.TranslateDocumentAsync). Tables in
    /// Word/Excel files are rendered as real Markdown pipe-tables here, in code, since that structure is
    /// readily available from the file format; everything else (headings, lists, PDF/plain text, slide
    /// text) is handed to the translation model as loosely-structured text and reconstructed there instead
    /// — see LanguageTools.TranslateDocumentToMarkdownAsync.
    ///
    /// Only modern (OOXML, zip-based) Office formats are supported — .docx/.xlsx/.pptx, via
    /// DocumentFormat.OpenXml (Microsoft's own SDK, MIT-licensed). Legacy binary formats (.doc, .xls,
    /// .ppt) aren't handled — see web.Constants.DocumentLimits.CanExtractText.
    /// </summary>
    public static class DocumentMarkdownExtractor
    {
        /// <summary>Reads <paramref name="fullPath"/> and returns its text content. Throws on read/parse failure — callers should catch and turn that into a user-facing error.</summary>
        public static string Extract(string fullPath, string contentType)
        {
            using var stream = File.OpenRead(fullPath);
            return contentType switch
            {
                "application/pdf" => ExtractPdf(stream),
                "text/plain" => new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true).ReadToEnd(),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ExtractDocx(stream),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ExtractXlsx(stream),
                "application/vnd.openxmlformats-officedocument.presentationml.presentation" => ExtractPptx(stream),
                _ => throw new NotSupportedException($"Kan ikke udtrække tekst fra indholdstypen '{contentType}'.")
            };
        }

        private static string ExtractPdf(Stream stream)
        {
            using var pdf = PdfDocument.Open(stream);
            var sb = new StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                // page.Text is PdfPig's simple built-in reconstruction and normally fine, but for some
                // fonts/encodings it comes back empty even though the page demonstrably has glyphs on
                // it (page.Letters is non-empty) — i.e. the text is selectable/readable in a real PDF
                // viewer, just not recoverable by that particular heuristic. ContentOrderTextExtractor
                // rebuilds the text directly from the page's letters in reading order and recovers
                // content in exactly that case, so it's used as a fallback rather than the default —
                // it's slower and less forgiving of layout, so only worth it when page.Text fails.
                // Each page is isolated so one page's parser trouble (corrupt content stream, unusual
                // Type3/embedded font, ...) doesn't blank out the whole (otherwise-fine) document.
                string text;
                try
                {
                    text = page.Text;
                    if (string.IsNullOrWhiteSpace(text) && page.Letters.Count > 0)
                        text = ContentOrderTextExtractor.GetText(page);
                }
                catch (Exception)
                {
                    text = string.Empty;
                }

                sb.AppendLine(text);
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>
        /// Rasterizes each page of a PDF to a PNG image — the OCR fallback used by
        /// DocumentsService.TranslateDocumentAsync when <see cref="ExtractPdf"/> finds no letters at all on
        /// any page. That happens for "born vector" PDFs: ones where the text was flattened to outline
        /// paths (ordinary line/curve/fill drawing operations) instead of being kept as real text-showing
        /// operations — seen in practice from Windows' "Microsoft: Print To PDF" driver applied to some
        /// browser/mail-client print pipelines. The characters themselves genuinely don't exist anywhere in
        /// such a file, only their visual shapes do, so no text-extraction library (this one included) can
        /// recover them — reading the rendered pixels via a vision model is the only way. Stops after
        /// <paramref name="maxPages"/> pages as a cost/time cap, same spirit as DocumentLimits.MaxTranslatableChars.
        /// </summary>
        public static List<byte[]> RenderPdfPagesToPng(string fullPath, int maxPages)
        {
            var bytes = File.ReadAllBytes(fullPath);
            var images = new List<byte[]>();
            foreach (var bitmap in Conversion.ToImages(bytes))
            {
                using (bitmap)
                {
                    using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 90);
                    images.Add(encoded.ToArray());
                }
                if (images.Count >= maxPages) break;
            }
            return images;
        }

        private static string ExtractDocx(Stream stream)
        {
            using var doc = WordprocessingDocument.Open(stream, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            var sb = new StringBuilder();
            if (body is null) return string.Empty;

            foreach (var element in body.Elements())
            {
                switch (element)
                {
                    case W.Paragraph paragraph:
                        AppendParagraph(sb, paragraph);
                        break;
                    case W.Table table:
                        AppendMarkdownTable(sb, table.Elements<W.TableRow>()
                            .Select(r => r.Elements<W.TableCell>().Select(GetCellText).ToList())
                            .ToList());
                        break;
                }
            }

            return sb.ToString();
        }

        private static string GetCellText(W.TableCell cell) =>
            string.Concat(cell.Descendants<W.Text>().Select(t => t.Text));

        private static void AppendParagraph(StringBuilder sb, W.Paragraph paragraph)
        {
            var text = string.Concat(paragraph.Descendants<W.Text>().Select(t => t.Text));
            if (string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine();
                return;
            }

            var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
            var headingLevel = styleId is not null && styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(styleId.AsSpan("Heading".Length), out var level) ? Math.Clamp(level, 1, 6) : 0;

            if (headingLevel > 0)
            {
                sb.AppendLine($"{new string('#', headingLevel)} {text}");
            }
            else if (paragraph.ParagraphProperties?.NumberingProperties is not null)
            {
                sb.AppendLine($"- {text}");
            }
            else
            {
                sb.AppendLine(text);
            }
            sb.AppendLine();
        }

        private static string ExtractXlsx(Stream stream)
        {
            using var doc = SpreadsheetDocument.Open(stream, false);
            var workbookPart = doc.WorkbookPart;
            if (workbookPart is null) return string.Empty;

            var sharedStrings = workbookPart.GetPartsOfType<SharedStringTablePart>().FirstOrDefault()?
                .SharedStringTable?.Elements<S.SharedStringItem>().Select(i => i.InnerText).ToList() ?? new List<string>();

            var sb = new StringBuilder();
            foreach (var sheetInfo in workbookPart.Workbook?.Descendants<S.Sheet>() ?? Enumerable.Empty<S.Sheet>())
            {
                var relId = sheetInfo.Id?.Value;
                if (relId is null || workbookPart.GetPartById(relId) is not WorksheetPart worksheetPart) continue;

                var sheetData = worksheetPart.Worksheet?.Elements<S.SheetData>().FirstOrDefault();
                if (sheetData is null) continue;

                var rows = new List<List<string>>();
                foreach (var row in sheetData.Elements<S.Row>())
                {
                    var cellMap = new Dictionary<int, string>();
                    var seq = 0;
                    foreach (var cell in row.Elements<S.Cell>())
                    {
                        var colIndex = cell.CellReference?.Value is string cellRef ? GetColumnIndex(cellRef) : seq;
                        cellMap[colIndex] = GetCellText(cell, sharedStrings);
                        seq++;
                    }
                    if (cellMap.Count == 0) continue;

                    var maxCol = cellMap.Keys.Max();
                    var rowValues = Enumerable.Range(0, maxCol + 1).Select(c => cellMap.GetValueOrDefault(c, string.Empty)).ToList();
                    if (rowValues.Any(v => !string.IsNullOrWhiteSpace(v)))
                        rows.Add(rowValues);
                }

                if (rows.Count == 0) continue;

                sb.AppendLine($"## {sheetInfo.Name}");
                sb.AppendLine();
                AppendMarkdownTable(sb, rows);
            }

            return sb.ToString();
        }

        private static string GetCellText(S.Cell cell, List<string> sharedStrings)
        {
            var raw = cell.CellValue?.InnerText;
            var dataType = cell.DataType?.Value;

            if (dataType == S.CellValues.SharedString)
                return int.TryParse(raw, out var idx) && idx >= 0 && idx < sharedStrings.Count ? sharedStrings[idx] : string.Empty;

            if (dataType == S.CellValues.InlineString)
                return cell.InlineString?.InnerText ?? string.Empty;

            if (dataType == S.CellValues.Boolean)
                return raw == "1" ? "TRUE" : "FALSE";

            // Number, Date, Error, or no explicit type (defaults to Number)
            return raw ?? string.Empty;
        }

        /// <summary>Maps a cell reference like "C5" to its 0-based column index (A=0, B=1, ...).</summary>
        private static int GetColumnIndex(string cellReference)
        {
            var index = 0;
            foreach (var ch in cellReference)
            {
                if (!char.IsLetter(ch)) break;
                index = index * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
            }
            return index - 1;
        }

        private static string ExtractPptx(Stream stream)
        {
            using var doc = PresentationDocument.Open(stream, false);
            var presentationPart = doc.PresentationPart;
            var slideIds = presentationPart?.Presentation?.SlideIdList?.Elements<P.SlideId>();
            if (presentationPart is null || slideIds is null) return string.Empty;

            var sb = new StringBuilder();
            var slideNumber = 0;
            foreach (var slideId in slideIds)
            {
                slideNumber++;
                var relId = slideId.RelationshipId?.Value;
                if (relId is null || presentationPart.GetPartById(relId) is not SlidePart slidePart) continue;

                sb.AppendLine($"## Slide {slideNumber}");
                sb.AppendLine();

                foreach (var paragraph in slidePart.Slide?.Descendants<A.Paragraph>() ?? Enumerable.Empty<A.Paragraph>())
                {
                    var text = string.Concat(paragraph.Descendants<A.Text>().Select(t => t.Text));
                    if (!string.IsNullOrWhiteSpace(text))
                        sb.AppendLine(text);
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>Renders <paramref name="rows"/> (first row = header) as a GFM Markdown pipe-table. No-op if there are fewer than 2 rows.</summary>
        private static void AppendMarkdownTable(StringBuilder sb, List<List<string>> rows)
        {
            if (rows.Count == 0) return;

            var columnCount = rows.Max(r => r.Count);
            if (columnCount == 0) return;

            string FormatRow(List<string> row)
            {
                var cells = Enumerable.Range(0, columnCount)
                    .Select(i => i < row.Count ? CleanCell(row[i]) : string.Empty);
                return "| " + string.Join(" | ", cells) + " |";
            }

            sb.AppendLine(FormatRow(rows[0]));
            sb.AppendLine("| " + string.Join(" | ", Enumerable.Repeat("---", columnCount)) + " |");
            foreach (var row in rows.Skip(1))
            {
                sb.AppendLine(FormatRow(row));
            }
            sb.AppendLine();
        }

        private static string CleanCell(string? text) =>
            string.IsNullOrEmpty(text) ? string.Empty : text.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ").Trim();
    }
}
