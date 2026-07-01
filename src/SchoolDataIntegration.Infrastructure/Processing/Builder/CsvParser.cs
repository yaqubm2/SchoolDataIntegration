using System.Text;

namespace SchoolDataIntegration.Infrastructure;

public static class CsvParser
{
    public static List<Dictionary<string, string>> Parse(string csvContent)
    {
        var rows = ParseRows(csvContent);
        var result = new List<Dictionary<string, string>>();

        if (rows.Count == 0)
        {
            return result;
        }

        var header = rows[0];

        for (var i = 1; i < rows.Count; i++)
        {
            var row = rows[i];

            // Skip fully blank trailing lines.
            if (row.Count == 1 && string.IsNullOrWhiteSpace(row[0]))
            {
                continue;
            }

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var col = 0; col < header.Count; col++)
            {
                dict[header[col]] = col < row.Count ? row[col] : string.Empty;
            }
            result.Add(dict);
        }

        return result;
    }

    private static List<List<string>> ParseRows(string content)
    {
        var rows = new List<List<string>>();
        var currentRow = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(c);
                }
                continue;
            }

            switch (c)
            {
                case '"':
                    inQuotes = true;
                    break;
                case ',':
                    currentRow.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    // ignore; \n (or end of content) terminates the row
                    break;
                case '\n':
                    currentRow.Add(field.ToString());
                    field.Clear();
                    rows.Add(currentRow);
                    currentRow = new List<string>();
                    break;
                default:
                    field.Append(c);
                    break;
            }
        }

        // Flush the final field/row if the content didn't end with a newline.
        if (field.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(field.ToString());
            rows.Add(currentRow);
        }

        return rows;
    }
}
