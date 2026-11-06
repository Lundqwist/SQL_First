namespace SQL_First;

/// <summary>Console drawing helpers: banner, arrow-key menu, tables, cards and prompts.</summary>
static class Ui
{
    const ConsoleColor Accent = ConsoleColor.Cyan;
    const ConsoleColor Dim = ConsoleColor.DarkGray;
    const int MaxCellWidth = 45;

    // Simple 5x5 block font for the banner
    static readonly Dictionary<char, string[]> Font = new()
    {
        ['S'] = ["█████", "█    ", "█████", "    █", "█████"],
        ['C'] = ["█████", "█    ", "█    ", "█    ", "█████"],
        ['H'] = ["█   █", "█   █", "█████", "█   █", "█   █"],
        ['O'] = ["█████", "█   █", "█   █", "█   █", "█████"],
        ['L'] = ["█    ", "█    ", "█    ", "█    ", "█████"],
    };
    static readonly ConsoleColor[] BannerColors =
        [ConsoleColor.Cyan, ConsoleColor.Cyan, ConsoleColor.DarkCyan, ConsoleColor.Blue, ConsoleColor.DarkBlue];

    // ------------------------------------------------------------------ layout

    public static void Header(string section)
    {
        if (!Console.IsOutputRedirected) Console.Clear();
        Console.WriteLine();
        for (int row = 0; row < 5; row++)
        {
            string line = string.Join(" ", "SCHOOL".Select(ch => Font[ch][row]));
            Write("    " + line + "\n", BannerColors[row]);
        }
        Write("    Programs · Courses · Students\n", Dim);
        Write("    " + new string('─', 35) + "\n\n", Dim);
        Write("  ▌ ", Accent);
        Write(section.ToUpper() + "\n\n", ConsoleColor.White);
    }

    /// <summary>Arrow-key menu. Returns the selected index, or -1 if Esc was pressed.</summary>
    public static int Menu(string title, string[] options)
    {
        Header(title);
        if (Console.IsInputRedirected || Console.IsOutputRedirected) return SimpleMenu(options);

        int selected = 0;
        int top = Console.CursorTop;
        int width = options.Max(o => o.Length) + 8;
        Console.CursorVisible = false;
        try
        {
            while (true)
            {
                Console.SetCursorPosition(0, top);
                for (int i = 0; i < options.Length; i++)
                {
                    if (i == selected)
                    {
                        Write("   ▶ ", Accent);
                        Console.BackgroundColor = ConsoleColor.DarkCyan;
                        Write($" {i + 1}  {options[i]}".PadRight(width), ConsoleColor.White);
                        Console.ResetColor();
                    }
                    else
                    {
                        Write("     ");
                        Write($" {i + 1}  ", Dim);
                        Write(options[i].PadRight(width - 4), ConsoleColor.Gray);
                    }
                    Console.WriteLine();
                }
                Write("\n   ↑/↓ move   Enter select   1-9 shortcut   Esc back", Dim);

                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:   selected = (selected - 1 + options.Length) % options.Length; break;
                    case ConsoleKey.DownArrow: selected = (selected + 1) % options.Length; break;
                    case ConsoleKey.Enter:     return selected;
                    case ConsoleKey.Escape:    return -1;
                    default:
                        if (char.IsDigit(key.KeyChar) && key.KeyChar - '1' is var n && n >= 0 && n < options.Length)
                            return n;
                        break;
                }
            }
        }
        finally
        {
            Console.CursorVisible = true;
        }
    }

    /// <summary>Fallback when input is redirected (no arrow keys available).</summary>
    static int SimpleMenu(string[] options)
    {
        for (int i = 0; i < options.Length; i++)
            Write($"   {i + 1}  {options[i]}\n");
        while (true)
        {
            Write("\n  › Choose: ", Accent);
            string? input = Console.ReadLine();
            if (input is null) return options.Length - 1;   // end of input → last option (back/exit)
            if (int.TryParse(input, out int n) && n >= 1 && n <= options.Length) return n - 1;
        }
    }

    // ------------------------------------------------------------------ output

    public static void Table(string[] headers, List<string[]> rows)
    {
        if (rows.Count == 0)
        {
            Info("No rows found.");
            return;
        }

        rows = rows.Select(r => r.Select(Truncate).ToArray()).ToList();
        int[] widths = headers.Select((h, i) => Math.Max(h.Length, rows.Max(r => r[i].Length))).ToArray();

        string Border(char left, char mid, char right) =>
            "  " + left + string.Join(mid, widths.Select(w => new string('─', w + 2))) + right + "\n";

        void Row(string[] cells, ConsoleColor color)
        {
            Write("  │", Dim);
            for (int i = 0; i < cells.Length; i++)
            {
                Write(" " + cells[i].PadRight(widths[i]) + " ", color);
                Write("│", Dim);
            }
            Console.WriteLine();
        }

        Write(Border('┌', '┬', '┐'), Dim);
        Row(headers, Accent);
        Write(Border('├', '┼', '┤'), Dim);
        foreach (var r in rows) Row(r, ConsoleColor.Gray);
        Write(Border('└', '┴', '┘'), Dim);
        Write($"  {rows.Count} row(s)\n", Dim);
    }

    /// <summary>A boxed card with label/value pairs.</summary>
    public static void Card(string title, (string Label, string Value)[] fields)
    {
        int labelWidth = fields.Max(f => f.Label.Length);
        int inner = Math.Max(title.Length + 4, fields.Max(f => labelWidth + 3 + f.Value.Length)) + 2;

        Write("  ╭─ ", Accent);
        Write(title, ConsoleColor.White);
        Write(" " + new string('─', inner - title.Length - 3) + "╮\n", Accent);
        foreach (var (label, value) in fields)
        {
            Write("  │ ", Accent);
            Write(label.PadRight(labelWidth), Dim);
            Write(" : ", Dim);
            Write(value.PadRight(inner - labelWidth - 4), ConsoleColor.White);
            Write("│\n", Accent);
        }
        Write("  ╰" + new string('─', inner) + "╯\n", Accent);
    }

    public static void Success(string message) => Write($"\n  ✔ {message}\n", ConsoleColor.Green);
    public static void Error(string message)   => Write($"\n  ✖ {message}\n", ConsoleColor.Red);
    public static void Warn(string message)    => Write($"\n  ! {message}\n", ConsoleColor.Yellow);
    public static void Info(string message)    => Write($"  · {message}\n", Dim);

    public static void Write(string text, ConsoleColor color = ConsoleColor.Gray)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = ConsoleColor.Gray;
    }

    static string Truncate(string s) => s.Length <= MaxCellWidth ? s : s[..(MaxCellWidth - 1)] + "…";

    // ------------------------------------------------------------------ input

    public static string Prompt(string label)
    {
        Write($"  › {label}: ", Accent);
        Console.ForegroundColor = ConsoleColor.White;
        string? input = Console.ReadLine();
        Console.ResetColor();
        return input?.Trim() ?? "";
    }

    /// <summary>Asks for a number. Empty input returns null (= cancel / skip).</summary>
    public static int? PromptInt(string label)
    {
        while (true)
        {
            string input = Prompt(label);
            if (input == "") return null;
            if (int.TryParse(input, out int n)) return n;
            Error("Please enter a number.");
        }
    }

    public static bool Confirm(string question)
    {
        string answer = Prompt(question + " (y/n)").ToLower();
        return answer is "y" or "yes" or "j" or "ja";
    }

    public static void Pause()
    {
        Write("\n  Press any key to return…", Dim);
        if (Console.IsInputRedirected) Console.ReadLine();
        else Console.ReadKey(true);
    }
}
