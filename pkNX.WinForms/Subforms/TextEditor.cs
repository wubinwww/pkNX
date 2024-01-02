using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using pkNX.Randomization;

namespace pkNX.WinForms;

public partial class TextEditor : Form
{
    public enum TextEditorMode
    {
        Common,
        Script,
    }

    private readonly TextContainer TextData;

    public TextEditor(TextContainer c, TextEditorMode mode)
    {
        InitializeComponent();
        TextData = c;
        Mode = mode;
        for (int i = 0; i < TextData.Length; i++)
            CB_Entry.Items.Add(c.GetFileName(i));
        CB_Entry.SelectedIndex = 0;
        dgv.EditMode = DataGridViewEditMode.EditOnEnter;
    }

    private readonly TextEditorMode Mode;
    private int entry = -1;

    // IO
    private void B_Export_Click(object sender, EventArgs e)
    {
        if (TextData.Length <= 0) return;
        using var dump = new SaveFileDialog { Filter = "Text File|*.txt" };
        if (dump.ShowDialog() != DialogResult.OK)
            return;

        var result = WinFormsUtil.Prompt(MessageBoxButton.YesNo,
            "Remove newline formatting codes? (\\n,\\r,\\c)",
            "Removing newline formatting will make it more readable but will prevent any importing of that dump.");
        bool newline = result == MessageBoxResult.Yes;
        string path = dump.FileName;
        ExportTextFile(path, newline, TextData);
    }

    private void B_Import_Click(object sender, EventArgs e)
    {
        if (TextData.Length <= 0) return;
        using var dump = new OpenFileDialog { Filter = "Text File|*.txt" };
        if (dump.ShowDialog() != DialogResult.OK)
            return;

        string path = dump.FileName;
        if (!ImportTextFiles(path))
            return;

        // Reload the form with the new data.
        ChangeEntry(this, e);
        WinFormsUtil.Alert("Imported Text from Input Path:", path);
    }

    public static void ExportTextFile(string fileName, bool newline, TextContainer lineData)
    {
        using var ms = new MemoryStream();
        ms.Write(new byte[] { 0xFF, 0xFE }, 0, 2); // Write Unicode BOM
        using (TextWriter tw = new StreamWriter(ms, new UnicodeEncoding()))
        {
            for (int i = 0; i < lineData.Length; i++)
            {
                // Get Strings for the File
                string[] data = lineData[i];
                string fn = lineData.GetFileName(i);
                WriteTextFile(tw, fn, data, newline);
            }
        }
        File.WriteAllBytes(fileName, ms.ToArray());
    }

    private static void WriteTextFile(TextWriter tw, string fn, string[] data, bool newline = false)
    {
        // Append the File Header
        tw.WriteLine("~~~~~~~~~~~~~~~");
        tw.WriteLine("Text File : " + fn);
        tw.WriteLine("~~~~~~~~~~~~~~~");
        // Write the String to the File
        foreach (string line in data)
        {
            tw.WriteLine(newline
                ? line.Replace("\\n\\n", " ")
                    .Replace("\\n", " ")
                    .Replace("\\c", "")
                    .Replace("\\r", "")
                    .Replace("\\\\", "\\")
                    .Replace("\\[", "[")
                : line);
        }
    }

    private bool ImportTextFiles(string fileName)
    {
        string[] fileText = File.ReadAllLines(fileName, Encoding.Unicode);
        string[][] textLines = new string[TextData.Length][];
        int ctr = 0;
        bool newlineFormatting = false;
        // Loop through all files
        for (int i = 0; i < fileText.Length; i++)
        {
            string line = fileText[i];
            if (line != "~~~~~~~~~~~~~~~")
                continue;
            string[] brokenLine = fileText[i++ + 1].Split(new[] { " : " }, StringSplitOptions.None);
            if (brokenLine.Length != 2)
            { WinFormsUtil.Error($"Invalid Line @ {i}, expected Text File : {ctr}"); return false; }

            var file = brokenLine[1];
            if (int.TryParse(file, out var fnum))
            {
                if (fnum != ctr)
                {
                    WinFormsUtil.Error($"Invalid Line @ {i}, expected Text File : {ctr}");
                    return false;
                }
            }
            // else pray that the filename index lines up

            i += 2; // Skip over the other header line
            List<string> Lines = [];
            while (i < fileText.Length && fileText[i] != "~~~~~~~~~~~~~~~")
            {
                Lines.Add(fileText[i]);
                newlineFormatting |= fileText[i].Contains("\\n"); // Check if any line wasn't stripped of ingame formatting codes for human readability.
                i++;
            }
            i--;
            textLines[ctr++] = Lines.ToArray();
        }

        // Error Check
        if (ctr != TextData.Length)
        {
            WinFormsUtil.Error("The amount of Text Files in the input file does not match the required for the text file.",
                $"Received: {ctr}, Expected: {TextData.Length}"); return false;
        }
        if (!newlineFormatting)
        {
            WinFormsUtil.Error("The input Text Files do not have the in-game newline formatting codes (\\n,\\r,\\c).",
                "When exporting text, do not remove newline formatting."); return false;
        }

        // All Text Lines received. Store all back.
        for (int i = 0; i < TextData.Length; i++)
        {
            try { TextData[i] = textLines[i]; }
            catch (Exception e) { WinFormsUtil.Error($"The input Text File (# {i}) failed to convert:", e.ToString()); return false; }
        }

        return true;
    }

    private void ChangeEntry(object sender, EventArgs e)
    {
        // Save All the old text
        if (entry > -1 && sender != this)
        {
            try
            {
                TextData[entry] = GetCurrentDGLines();
            }
            catch (Exception ex) { WinFormsUtil.Error(ex.ToString()); }
        }

        // Reset
        entry = CB_Entry.SelectedIndex;
        SetStringsDataGridView(TextData[entry]);
    }

    // Main Handling
    private void SetStringsDataGridView(string[] textArray)
    {
        // Clear the datagrid row content to remove all text lines.
        dgv.Rows.Clear();
        // Clear the header columns, these are repopulated every time.
        dgv.Columns.Clear();
        if (textArray.Length == 0)
            return;
        // Reset settings and columns.
        dgv.AllowUserToResizeColumns = false;
        DataGridViewColumn dgvLine = new DataGridViewTextBoxColumn
        {
            HeaderText = "Line",
            DisplayIndex = 0,
            Width = 32,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        };
        dgvLine.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

        DataGridViewTextBoxColumn dgvText = new()
        {
            HeaderText = "Text",
            DisplayIndex = 1,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
        };

        dgv.Columns.Add(dgvLine);
        dgv.Columns.Add(dgvText);
        dgv.Rows.Add(textArray.Length);

        // Add the text lines into their cells.
        for (int i = 0; i < textArray.Length; i++)
        {
            dgv.Rows[i].Cells[0].Value = i;
            dgv.Rows[i].Cells[1].Value = textArray[i];
        }
    }

    private string[] GetCurrentDGLines()
    {
        // Get Line Count
        string[] lines = new string[dgv.RowCount];
        for (int i = 0; i < dgv.RowCount; i++)
            lines[i] = (string)dgv.Rows[i].Cells[1].Value;
        return lines;
    }
    // Meta Usage
    private void B_AddLine_Click(object sender, EventArgs e)
    {
        int currentRow = 0;
        try { currentRow = dgv.CurrentRow!.Index; }
        catch { dgv.Rows.Add(); }
        if (dgv.Rows.Count != 1 && (currentRow < dgv.Rows.Count - 1 || currentRow == 0))
        {
            if (ModifierKeys != Keys.Control && currentRow != 0)
            {
                if (WinFormsUtil.Prompt(MessageBoxButton.YesNo, "Inserting in between rows will shift all subsequent lines.", "Continue?") != MessageBoxResult.Yes)
                    return;
            }
            // Insert new Row after current row.
            dgv.Rows.Insert(currentRow + 1);
        }

        for (int i = 0; i < dgv.Rows.Count; i++)
            dgv.Rows[i].Cells[0].Value = i.ToString();
    }

    private void B_RemoveLine_Click(object sender, EventArgs e)
    {
        int currentRow = dgv.CurrentRow!.Index;
        if (currentRow < dgv.Rows.Count - 1)
        {
            if (ModifierKeys != Keys.Control && MessageBoxResult.Yes != WinFormsUtil.Prompt(MessageBoxButton.YesNo, "Deleting a row above other lines will shift all subsequent lines.", "Continue?"))
                return;
        }
        dgv.Rows.RemoveAt(currentRow);

        // Resequence the Index Value column
        for (int i = 0; i < dgv.Rows.Count; i++)
            dgv.Rows[i].Cells[0].Value = i.ToString();
    }

    private void SaveCurrentFile()
    {
        // Save any pending edits
        dgv.EndEdit();
        // Save All the old text
        if (entry > -1)
            TextData[entry] = GetCurrentDGLines();
    }

    private void B_Randomize_Click(object sender, EventArgs e)
    {
        // gametext can be horribly broken if randomized
        if (Mode == TextEditorMode.Common && MessageBoxResult.Yes != WinFormsUtil.Prompt(MessageBoxButton.YesNo, "Randomizing Game Text is dangerous!", "Continue?"))
            return;

        // get if the user wants to randomize current text file or all files
        var dr = WinFormsUtil.Prompt(MessageBoxButton.YesNoCancel,
            $"Yes: Randomize ALL{Environment.NewLine}No: Randomize current Text File{Environment.NewLine}Cancel: Abort");

        if (dr == MessageBoxResult.Cancel)
            return;

        // get if pure shuffle or smart shuffle (no shuffle if variable present)
        var drs = WinFormsUtil.Prompt(MessageBoxButton.YesNo,
            $"Smart shuffle:{Environment.NewLine}Yes: Shuffle if no Variable present{Environment.NewLine}No: Pure random!");

        if (drs == MessageBoxResult.Cancel)
            return;

        bool all = dr == MessageBoxResult.Yes;
        bool smart = drs == MessageBoxResult.Yes;

        // save current
        if (entry > -1)
            TextData[entry] = GetCurrentDGLines();

        // single-entire looping
        int start = all ? 0 : entry;
        int end = all ? TextData.Length - 1 : entry;

        // Gather strings
        List<string> strings = [];
        for (int i = start; i <= end; i++)
        {
            string[] data = TextData[i];
            strings.AddRange(smart
                ? data.Where(line => !line.Contains('['))
                : data);
        }

        // Shuffle up
        string[] pool = strings.ToArray();
        Util.Shuffle(pool);

        // Apply Text
        int ctr = 0;
        for (int i = start; i <= end; i++)
        {
            string[] data = TextData[i];

            for (int j = 0; j < data.Length; j++) // apply lines
            {
                if (!smart || !data[j].Contains("["))
                    data[j] = pool[ctr++];
            }

            TextData[i] = data;
        }

        // Load current text file
        SetStringsDataGridView(TextData[entry]);

        WinFormsUtil.Alert("Strings randomized!");
    }

    private void B_Save_Click(object sender, EventArgs e)
    {
        Modified = true;
        SaveCurrentFile();
        TextData.Save();
        Close();
    }

    public bool Modified { get; set; }
}
