using System.Globalization;
using System.Text;

namespace SchoolManagement.Service;

public static class MailPdf
{
    // Small dependency-free A4 PDF for transactional email attachments.
    public static byte[] Create(string title, IEnumerable<string> lines)
    {
        var entries = lines.SelectMany(Wrap).ToList();
        var chunks = entries.Chunk(38).ToList();
        if (chunks.Count == 0) chunks.Add(Array.Empty<string>());
        var pageIds = Enumerable.Range(0, chunks.Count).Select(index => 4 + index * 2).ToArray();
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            $"<< /Type /Pages /Kids [{string.Join(" ", pageIds.Select(id => $"{id} 0 R"))}] /Count {pageIds.Length} >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };
        foreach (var (chunk, index) in chunks.Select((value, index) => (value, index)))
        {
            var contentId = pageIds[index] + 1;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentId} 0 R >>");
            var content = new StringBuilder();
            content.AppendLine("0.1 0.2 0.35 rg 0 775 595 67 re f");
            content.AppendLine($"BT /F1 17 Tf 1 1 1 rg 38 800 Td ({Escape(title)}) Tj ET");
            content.AppendLine($"BT /F1 10 Tf 0.2 0.3 0.45 rg 38 766 Td ({Escape($"Page {index + 1} of {chunks.Count}")}) Tj ET");
            for (var row = 0; row < chunk.Length; row++)
            {
                var y = 740 - row * 18;
                var cells = chunk[row].Split(" | ", StringSplitOptions.None);
                if (cells.Length > 1)
                {
                    var xPositions = cells.Length == 3 ? new[] { 38, 300, 425 } : new[] { 38, 300, 390, 485 };
                    if (row % 2 == 0)
                        content.AppendLine($"0.94 0.96 0.98 rg 38 {y - 6} 519 18 re f");
                    content.AppendLine($"0.82 0.86 0.9 RG 38 {y - 8} m 557 {y - 8} l S");
                    for (var cell = 0; cell < Math.Min(cells.Length, xPositions.Length); cell++)
                    {
                        var maximum = cell == 0 ? 40 : cells.Length == 3 ? 17 : 13;
                        var label = cells[cell].Length > maximum ? cells[cell][..(maximum - 1)] + "..." : cells[cell];
                        content.AppendLine($"BT /F1 9 Tf 0.1 0.15 0.25 rg {xPositions[cell]} {y} Td ({Escape(label)}) Tj ET");
                    }
                }
                else
                    content.AppendLine($"BT /F1 10 Tf 0.1 0.15 0.25 rg 38 {y} Td ({Escape(chunk[row])}) Tj ET");
            }            content.AppendLine("0.75 0.8 0.86 RG 38 52 m 557 52 l S");
            var stream = content.ToString();
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}endstream");
        }
        using var output = new MemoryStream();
        void Write(string value) { var bytes = Encoding.ASCII.GetBytes(value); output.Write(bytes); }
        Write("%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(output.Position);
            Write($"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }
        var xref = output.Position;
        Write($"xref\n0 {offsets.Count}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            Write(offset.ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
        Write($"trailer\n<< /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return output.ToArray();
    }

    private static IEnumerable<string> Wrap(string value)
    {
        var safe = new string(value.Select(character => character is >= ' ' and <= '~' ? character : '?').ToArray());
        if (safe.Length == 0) { yield return " "; yield break; }
        for (var index = 0; index < safe.Length; index += 91)
            yield return safe.Substring(index, Math.Min(91, safe.Length - index));
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}