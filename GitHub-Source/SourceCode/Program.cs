using System.Drawing;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AddonUploader;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class ProjectConfig
{
    public string Name { get; set; } = "My Addon";
    public string Folder { get; set; } = "";
    public string Version { get; set; } = "1.0.0";
    public string CurseForgeId { get; set; } = "";
    public string WagoId { get; set; } = "";
    public string CurseForgeToken { get; set; } = "";
    public string WagoToken { get; set; } = "";
    public string WagoRetailPatch { get; set; } = "";
    public string WagoMopPatch { get; set; } = "";
    public string WagoCataclysmPatch { get; set; } = "";
    public string WagoWotlkPatch { get; set; } = "";
    public string WagoBcPatch { get; set; } = "";
    public string WagoForeverPatch { get; set; } = "";
    public string WagoClassicPatch { get; set; } = "";
    public string ReleaseType { get; set; } = "release";
}

internal sealed class GlobalConfig
{
    public string CurseForgeToken { get; set; } = "";
    public string WagoToken { get; set; } = "";
}

internal sealed class MainForm : Form
{
    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase) { ".git", ".svn", ".vs", "bin", "obj" };
    private readonly string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AddonUploader");
    private readonly string configPath;
    private readonly string globalConfigPath;
    private readonly List<ProjectConfig> projects = [];
    private string globalCurseForgeToken = "";
    private string globalWagoToken = "";
    private readonly ComboBox projectPicker = new();
    private bool loadingProjects;
    private readonly TextBox nameBox = new();
    private readonly TextBox folderBox = new();
    private readonly TextBox versionBox = new();
    private readonly TextBox cfIdBox = new();
    private readonly TextBox wagoIdBox = new();
    private readonly TextBox cfTokenBox = new();
    private readonly TextBox wagoTokenBox = new();
    private readonly TextBox retailBox = new();
    private readonly TextBox mopBox = new();
    private readonly TextBox cataclysmBox = new();
    private readonly TextBox wotlkBox = new();
    private readonly TextBox burningCrusadeBox = new();
    private readonly TextBox foreverBox = new();
    private readonly TextBox classicBox = new();
    private readonly TextBox changelogBox = new();
    private readonly ComboBox releasePicker = new();
    private readonly CheckBox uploadCf = new() { Text = "CurseForge", Checked = true, AutoSize = true };
    private readonly CheckBox uploadWago = new() { Text = "Wago", Checked = true, AutoSize = true };
    private readonly TextBox logBox = new();
    private readonly Button uploadButton = new() { Text = "Create ZIP and Upload", Height = 42, BackColor = Color.FromArgb(98, 76, 210), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(5) };

    public MainForm()
    {
        configPath = Path.Combine(dataDir, "projects.json");
        globalConfigPath = Path.Combine(dataDir, "global.json");
        Directory.CreateDirectory(dataDir);
        Text = "Addon Uploader | CurseForge + Wago";
        MinimumSize = new Size(930, 760);
        Size = new Size(1050, 850);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 246, 250);
        Font = new Font("Segoe UI", 9);
        BuildUi();
        LoadProjects();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12, 8, 12, 10) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
        Controls.Add(root);
        var title = new Label { Text = "Addon Uploader", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = Color.FromArgb(40, 42, 55), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        root.Controls.Add(title, 0, 0);

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White, Padding = new Padding(12) };
        root.Controls.Add(scroll, 0, 1);
        var form = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(3), RowCount = 0 };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 185));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        scroll.Controls.Add(form);
        void Section(string s)
        {
            var row = form.RowCount;
            var label = new Label { Text = s, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.FromArgb(98, 76, 210), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = false, Margin = new Padding(0, 0, 0, 0) };
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            form.Controls.Add(label, 0, row);
            form.SetColumnSpan(label, 2);
            form.RowCount++;
        }
        void Row(string caption, Control field, int height = 31, string? tip = null)
        {
            var row = form.RowCount;
            var l = new Label { Text = caption, AutoSize = false, Dock = DockStyle.Fill, ForeColor = Color.FromArgb(65, 68, 80), Margin = new Padding(0, 0, 8, 0), TextAlign = ContentAlignment.MiddleLeft };
            field.Dock = DockStyle.Fill;
            field.Margin = new Padding(0, 3, 0, 3);
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            form.Controls.Add(l, 0, row);
            form.Controls.Add(field, 1, row);
            form.RowCount++;
            if (tip != null) field.AccessibleDescription = tip;
        }
        projectPicker.DropDownStyle = ComboBoxStyle.DropDownList;
        projectPicker.SelectedIndexChanged += (_, _) => SelectProject();
        var projectControls = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = Padding.Empty };
        projectPicker.Width = 300;
        projectPicker.Margin = Padding.Empty;
        var newBtn = SmallButton("New Project"); newBtn.Click += (_, _) => NewProject();
        var saveBtn = SmallButton("Save"); saveBtn.Click += (_, _) => SaveCurrent();
        projectControls.Controls.Add(projectPicker); projectControls.Controls.Add(newBtn); projectControls.Controls.Add(saveBtn);
        Section("Project"); Row("Saved Project", projectControls);
        nameBox.PlaceholderText = "e.g. MyAddon"; Row("Addon Name", nameBox);
        var folderPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = Padding.Empty };
        folderBox.Width = 570; folderBox.Anchor = AnchorStyles.Left | AnchorStyles.Right; folderBox.Margin = Padding.Empty;
        var browse = SmallButton("Choose Folder…"); browse.Click += (_, _) => BrowseFolder(); folderPanel.Controls.Add(folderBox); folderPanel.Controls.Add(browse);
        Row("Addon Source Folder", folderPanel, tip: "Folder containing the .toc file and addon files");
        var versionPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = Padding.Empty };
        versionBox.Width = 160; versionBox.Margin = Padding.Empty;
        var bump = SmallButton("Bump Version"); bump.Click += (_, _) => BumpVersion();
        versionPanel.Controls.Add(versionBox); versionPanel.Controls.Add(bump); Row("Current Version", versionPanel);
        releasePicker.DropDownStyle = ComboBoxStyle.DropDownList; releasePicker.Items.AddRange(["release", "beta", "alpha"]); releasePicker.SelectedIndex = 0; Row("Release Type", releasePicker);

        Section("Platforms and API Credentials");
        cfIdBox.PlaceholderText = "CurseForge project ID (number)"; Row("CurseForge Project ID", cfIdBox);
        cfTokenBox.UseSystemPasswordChar = true; cfTokenBox.PlaceholderText = "Global token for all addons"; Row("CurseForge API Token (global)", cfTokenBox);
        wagoIdBox.PlaceholderText = "Wago project ID from dashboard"; Row("Wago Project ID", wagoIdBox);
        wagoTokenBox.UseSystemPasswordChar = true; wagoTokenBox.PlaceholderText = "Global token for all addons"; Row("Wago API Token (global)", wagoTokenBox);
        var wagoPatches = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 7, RowCount = 2, Margin = new Padding(0, 3, 0, 3), Padding = Padding.Empty };
        for (int i = 0; i < 7; i++) wagoPatches.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95));
        wagoPatches.RowStyles.Add(new RowStyle(SizeType.Absolute, 20)); wagoPatches.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        TextBox[] patchBoxes = [retailBox, mopBox, cataclysmBox, wotlkBox, burningCrusadeBox, foreverBox, classicBox];
        string[] patchLabels = ["Retail", "MoP", "Cata", "Wrath", "BCC", "Forever", "Classic"];
        string[] patchDescriptions = ["Supported Retail patch", "Supported Mists of Pandaria patch", "Supported Cataclysm patch", "Supported Wrath of the Lich King patch", "Supported Burning Crusade Classic patch", "Supported Classic Forever patch", "Supported Classic Era patch"];
        for (int i = 0; i < patchBoxes.Length; i++)
        {
            var patchLabel = new Label { Text = patchLabels[i], Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Margin = new Padding(2, 0, 2, 0) };
            patchBoxes[i].Dock = DockStyle.None; patchBoxes[i].Anchor = AnchorStyles.Left; patchBoxes[i].Width = 89; patchBoxes[i].Margin = new Padding(2, 0, 2, 0); patchBoxes[i].AccessibleDescription = $"{patchDescriptions[i]}, e.g. 12.1.0";
            wagoPatches.Controls.Add(patchLabel, i, 0); wagoPatches.Controls.Add(patchBoxes[i], i, 1);
        }
        Row("Wago Game Versions", wagoPatches, 55);
        changelogBox.Multiline = true; changelogBox.ScrollBars = ScrollBars.Vertical; changelogBox.Height = 137; Row("Changelog", changelogBox, 145);
        var targets = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 5, 0, 0) }; targets.Controls.Add(uploadCf); targets.Controls.Add(uploadWago); Row("Upload to", targets);
        var hint = new Label { Text = "API tokens are encrypted and stored for your Windows account only. TOC version numbers are updated when packaging.", ForeColor = Color.DimGray, AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
        form.Controls.Add(hint, 0, form.RowCount); form.SetColumnSpan(hint, 2); form.RowCount++;

        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(0, 8, 0, 0) };
        bottom.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); bottom.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.Controls.Add(bottom, 0, 2);
        uploadButton.Dock = DockStyle.Fill; uploadButton.FlatAppearance.BorderSize = 0; uploadButton.Click += async (_, _) => await UploadAsync(); bottom.Controls.Add(uploadButton, 0, 0);
        logBox.Multiline = true; logBox.ReadOnly = true; logBox.ScrollBars = ScrollBars.Vertical; logBox.BackColor = Color.FromArgb(30, 32, 40); logBox.ForeColor = Color.FromArgb(222, 225, 235); logBox.BorderStyle = BorderStyle.None; logBox.Dock = DockStyle.Fill; logBox.Font = new Font("Consolas", 9); bottom.Controls.Add(logBox, 0, 1);
    }

    private static Button SmallButton(string text)
    {
        int width = TextRenderer.MeasureText(text, SystemFonts.MessageBoxFont).Width + 20;
        return new Button { Text = text, AutoSize = false, Width = width, Height = 24, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(238, 237, 249), ForeColor = Color.FromArgb(65, 53, 145), Margin = new Padding(5, 0, 0, 0) };
    }
    private void Log(string message) { if (InvokeRequired) { BeginInvoke(() => Log(message)); return; } logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}"); }
    private void LoadProjects()
    {
        loadingProjects = true;
        try { if (File.Exists(configPath)) projects.AddRange(JsonSerializer.Deserialize<List<ProjectConfig>>(File.ReadAllText(configPath)) ?? []); } catch (Exception e) { Log("Could not read project settings: " + e.Message); }
        try
        {
            if (File.Exists(globalConfigPath))
            {
                var global = JsonSerializer.Deserialize<GlobalConfig>(File.ReadAllText(globalConfigPath));
                globalCurseForgeToken = global?.CurseForgeToken ?? "";
                globalWagoToken = global?.WagoToken ?? "";
            }
        }
        catch (Exception e) { Log("Could not read global credentials: " + e.Message); }
        if (string.IsNullOrEmpty(Unprotect(globalCurseForgeToken))) globalCurseForgeToken = projects.Select(x => x.CurseForgeToken).FirstOrDefault(x => !string.IsNullOrEmpty(Unprotect(x))) ?? "";
        if (string.IsNullOrEmpty(Unprotect(globalWagoToken))) globalWagoToken = projects.Select(x => x.WagoToken).FirstOrDefault(x => !string.IsNullOrEmpty(Unprotect(x))) ?? "";
        foreach (var project in projects) { project.CurseForgeToken = ""; project.WagoToken = ""; }
        RefreshPicker(); loadingProjects = false; if (projects.Count == 0) NewProject(); else SelectProject();
    }
    private void RefreshPicker() { loadingProjects = true; projectPicker.DataSource = null; projectPicker.DataSource = projects; projectPicker.DisplayMember = nameof(ProjectConfig.Name); loadingProjects = false; }
    private ProjectConfig? Current => projectPicker.SelectedItem as ProjectConfig;
    private void SelectProject() { if (loadingProjects) return; var p = Current; if (p == null) return; nameBox.Text = p.Name; folderBox.Text = p.Folder; versionBox.Text = p.Version; cfIdBox.Text = p.CurseForgeId; wagoIdBox.Text = p.WagoId; cfTokenBox.Text = Unprotect(globalCurseForgeToken); wagoTokenBox.Text = Unprotect(globalWagoToken); retailBox.Text = p.WagoRetailPatch; mopBox.Text = p.WagoMopPatch; cataclysmBox.Text = p.WagoCataclysmPatch; wotlkBox.Text = p.WagoWotlkPatch; burningCrusadeBox.Text = p.WagoBcPatch; foreverBox.Text = p.WagoForeverPatch; classicBox.Text = p.WagoClassicPatch; releasePicker.SelectedItem = p.ReleaseType; if (releasePicker.SelectedIndex < 0) releasePicker.SelectedIndex = 0; }
    private void NewProject() { var p = new ProjectConfig(); projects.Add(p); RefreshPicker(); projectPicker.SelectedItem = p; SelectProject(); }
    private void SaveCurrent(bool show = true)
    {
        var p = Current; if (p == null) return;
        p.Name = string.IsNullOrWhiteSpace(nameBox.Text) ? "My Addon" : nameBox.Text.Trim(); p.Folder = folderBox.Text.Trim(); p.Version = string.IsNullOrWhiteSpace(versionBox.Text) ? "1.0.0" : versionBox.Text.Trim(); p.CurseForgeId = cfIdBox.Text.Trim(); p.WagoId = wagoIdBox.Text.Trim(); p.CurseForgeToken = ""; p.WagoToken = ""; p.WagoRetailPatch = retailBox.Text.Trim(); p.WagoMopPatch = mopBox.Text.Trim(); p.WagoCataclysmPatch = cataclysmBox.Text.Trim(); p.WagoWotlkPatch = wotlkBox.Text.Trim(); p.WagoBcPatch = burningCrusadeBox.Text.Trim(); p.WagoForeverPatch = foreverBox.Text.Trim(); p.WagoClassicPatch = classicBox.Text.Trim(); p.ReleaseType = releasePicker.SelectedItem?.ToString() ?? "release";
        try
        {
            globalCurseForgeToken = Protect(cfTokenBox.Text.Trim()); globalWagoToken = Protect(wagoTokenBox.Text.Trim());
            File.WriteAllText(globalConfigPath, JsonSerializer.Serialize(new GlobalConfig { CurseForgeToken = globalCurseForgeToken, WagoToken = globalWagoToken }, new JsonSerializerOptions { WriteIndented = true }));
            File.WriteAllText(configPath, JsonSerializer.Serialize(projects, new JsonSerializerOptions { WriteIndented = true }));
            RefreshPicker(); projectPicker.SelectedItem = p; if (show) Log("Project and global API tokens saved.");
        }
        catch (Exception e) { Log("Save failed: " + e.Message); }
    }
    private static string Protect(string value) { if (string.IsNullOrEmpty(value)) return ""; return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser)); }
    private static string Unprotect(string value) { try { return string.IsNullOrEmpty(value) ? "" : Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(value), null, DataProtectionScope.CurrentUser)); } catch { return ""; } }
    private void BrowseFolder() { using var d = new FolderBrowserDialog { Description = "Select the addon source folder" }; if (Directory.Exists(folderBox.Text)) d.SelectedPath = folderBox.Text; if (d.ShowDialog(this) == DialogResult.OK) { folderBox.Text = d.SelectedPath; var toc = Directory.EnumerateFiles(d.SelectedPath, "*.toc", SearchOption.AllDirectories).FirstOrDefault(); var match = toc is null ? null : Regex.Match(File.ReadAllText(toc), @"(?im)^##\s*Version\s*:\s*([^\r\n]+)"); if (match?.Success == true && Regex.IsMatch(match.Groups[1].Value.Trim(), @"^\d+(?:\.\d+)*$")) { versionBox.Text = match.Groups[1].Value.Trim(); BumpVersion(); } } }
    private void BumpVersion()
    {
        var v = versionBox.Text.Trim();
        var m = Regex.Match(v, @"^(\d+(?:\.\d+)*)(.*)$");
        if (!m.Success) { versionBox.Text = "1.0.1"; return; }
        var parts = m.Groups[1].Value.Split('.');
        var last = parts[^1];
        if (!ulong.TryParse(last, out var number) || number == ulong.MaxValue) { versionBox.Text = "1.0.1"; return; }
        var incremented = (number + 1).ToString().PadLeft(last.Length, '0');
        parts[^1] = incremented;
        versionBox.Text = string.Join('.', parts) + m.Groups[2].Value;
    }

    private async Task UploadAsync()
    {
        if (uploadButton.Enabled == false) return;
        SaveCurrent(false); var p = Current;
        if (p == null || !Directory.Exists(folderBox.Text)) { MessageBox.Show(this, "Select a valid addon source folder.", "Folder required", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (!uploadCf.Checked && !uploadWago.Checked) { MessageBox.Show(this, "Select at least one upload platform."); return; }
        if (uploadCf.Checked && (string.IsNullOrWhiteSpace(cfIdBox.Text) || string.IsNullOrWhiteSpace(cfTokenBox.Text))) { MessageBox.Show(this, "CurseForge project ID and API token are required."); return; }
        if (uploadWago.Checked && (string.IsNullOrWhiteSpace(wagoIdBox.Text) || string.IsNullOrWhiteSpace(wagoTokenBox.Text) || !HasWagoPatch())) { MessageBox.Show(this, "Wago project ID, API token, and at least one supported game version are required."); return; }
        uploadButton.Enabled = false;
        string releaseName = $"{p.Name.Trim()} {versionBox.Text.Trim()}";
        string zip = Path.Combine(dataDir, SafeName(releaseName) + ".zip");
        string sourceFolder = folderBox.Text;
        string version = versionBox.Text.Trim();
        try
        {
            Log("Creating ZIP package …");
            await Task.Run(() => CreatePackage(sourceFolder, zip, version));
            Log("Package created: " + zip);
            if (uploadCf.Checked) await UploadCurseForge(p, zip);
            if (uploadWago.Checked) await UploadWago(p, zip);
            int updatedTocFiles = await Task.Run(() => UpdateSourceTocFiles(sourceFolder, version));
            Log($"Updated addon source folder: set {updatedTocFiles} TOC file(s) to version {version}.");
            p.Version = version; SaveCurrent(false);
            try { File.Delete(zip); Log("Deleted the temporary ZIP after successful uploads."); }
            catch (Exception deleteError) { Log("Uploads succeeded, but the temporary ZIP could not be deleted: " + deleteError.Message); }
            Log("All done."); MessageBox.Show(this, "Package created and selected uploads completed.", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) { Log("ERROR: " + ex.Message); MessageBox.Show(this, ex.Message, "Upload failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { uploadButton.Enabled = true; }
    }

    private static string SafeName(string value) { foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_'); return string.IsNullOrWhiteSpace(value) ? "Addon" : value; }
    private bool HasWagoPatch() => new[] { retailBox, mopBox, cataclysmBox, wotlkBox, burningCrusadeBox, foreverBox, classicBox }.Any(box => !string.IsNullOrWhiteSpace(box.Text));
    private static int UpdateSourceTocFiles(string source, string version)
    {
        int updated = 0;
        foreach (string path in EnumerateAddonFiles(source).Where(path => Path.GetExtension(path).Equals(".toc", StringComparison.OrdinalIgnoreCase)))
        {
            byte[] bytes = File.ReadAllBytes(path);
            using var input = new MemoryStream(bytes);
            using var reader = new StreamReader(input, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            string text = reader.ReadToEnd();
            Encoding encoding = reader.CurrentEncoding;
            string changed = UpdateTocVersion(text, version);
            if (changed == text) continue;
            File.WriteAllText(path, changed, encoding);
            updated++;
        }
        return updated;
    }
    private static IEnumerable<string> EnumerateAddonFiles(string sourceRoot)
    {
        var pending = new Stack<string>();
        pending.Push(sourceRoot);
        while (pending.Count > 0)
        {
            string directory = pending.Pop();
            foreach (string path in Directory.EnumerateFileSystemEntries(directory))
            {
                var attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0) continue;
                if ((attributes & FileAttributes.Directory) != 0)
                {
                    if (!ExcludedDirectories.Contains(Path.GetFileName(path)))
                        pending.Push(path);
                    continue;
                }
                yield return path;
            }
        }
    }
    private static string UpdateTocVersion(string text, string version)
    {
        const string pattern = @"(?im)^##\s*Version\s*:.*$";
        if (Regex.IsMatch(text, pattern)) return Regex.Replace(text, pattern, _ => $"## Version: {version}");
        return $"## Version: {version}{Environment.NewLine}" + text;
    }
    private static void CreatePackage(string source, string zip, string version)
    {
        if (File.Exists(zip)) File.Delete(zip);
        using var archive = ZipFile.Open(zip, ZipArchiveMode.Create);
        string sourceRoot = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string rootPrefix = new DirectoryInfo(sourceRoot).Name;
        foreach (string file in EnumerateAddonFiles(sourceRoot))
        {
            var rel = Path.GetRelativePath(sourceRoot, file);
            var entry = archive.CreateEntry($"{rootPrefix}/{rel.Replace('\\', '/')}", CompressionLevel.Fastest);
            using var input = File.OpenRead(file); using var output = entry.Open();
            if (Path.GetExtension(file).Equals(".toc", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = new StreamReader(input, Encoding.UTF8, true); var text = reader.ReadToEnd();
                text = UpdateTocVersion(text, version);
                using var writer = new StreamWriter(output, new UTF8Encoding(false)); writer.Write(text);
            }
            else input.CopyTo(output);
        }
    }
    private async Task UploadCurseForge(ProjectConfig p, string zip)
    {
        Log("Uploading to CurseForge …"); using var form = new MultipartFormDataContent();
        var metadata = new { changelog = changelogBox.Text, changelogType = "markdown", displayName = $"{p.Name} {versionBox.Text}", releaseType = releasePicker.SelectedItem?.ToString() ?? "release" };
        form.Add(new StringContent(JsonSerializer.Serialize(metadata), Encoding.UTF8, "application/json"), "metadata");
        using var stream = File.OpenRead(zip); using var file = new StreamContent(stream); file.Headers.ContentType = new MediaTypeHeaderValue("application/zip"); form.Add(file, "file", Path.GetFileName(zip));
        using var req = new HttpRequestMessage(HttpMethod.Post, $"https://wow.curseforge.com/api/projects/{Uri.EscapeDataString(cfIdBox.Text.Trim())}/upload-file") { Content = form }; req.Headers.Add("X-Api-Token", cfTokenBox.Text.Trim());
        using var response = await http.SendAsync(req); string body = await response.Content.ReadAsStringAsync(); if (!response.IsSuccessStatusCode) throw new Exception($"CurseForge ({(int)response.StatusCode}): {body}");
        string fileId = ""; try { fileId = JsonNode.Parse(body)?["id"]?.ToString() ?? ""; } catch { }
        Log(fileId.Length > 0 ? $"CurseForge upload accepted. File ID: {fileId}. The file may be pending moderation." : "CurseForge upload accepted: " + body);
    }
    private async Task UploadWago(ProjectConfig p, string zip)
    {
        Log("Uploading to Wago …"); var metadata = new Dictionary<string, object?> { ["label"] = $"{p.Name.Trim()} {versionBox.Text.Trim()}", ["stability"] = releasePicker.SelectedItem?.ToString() == "release" ? "stable" : releasePicker.SelectedItem?.ToString(), ["changelog"] = changelogBox.Text };
        AddWagoPatch(metadata, "supported_retail_patch", retailBox);
        AddWagoPatch(metadata, "supported_mop_patch", mopBox);
        AddWagoPatch(metadata, "supported_cata_patch", cataclysmBox);
        AddWagoPatch(metadata, "supported_wotlk_patch", wotlkBox);
        AddWagoPatch(metadata, "supported_bc_patch", burningCrusadeBox);
        AddWagoPatch(metadata, "supported_forever_patch", foreverBox);
        AddWagoPatch(metadata, "supported_classic_patch", classicBox);
        using var form = new MultipartFormDataContent(); form.Add(new StringContent(JsonSerializer.Serialize(metadata), Encoding.UTF8), "metadata");
        using var stream = File.OpenRead(zip); using var file = new StreamContent(stream); file.Headers.ContentType = new MediaTypeHeaderValue("application/zip"); form.Add(file, "file", Path.GetFileName(zip));
        using var req = new HttpRequestMessage(HttpMethod.Post, $"https://addons.wago.io/api/projects/{Uri.EscapeDataString(wagoIdBox.Text.Trim())}/version") { Content = form }; req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", wagoTokenBox.Text.Trim()); req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await http.SendAsync(req); string body = await response.Content.ReadAsStringAsync(); if (!response.IsSuccessStatusCode) throw new Exception($"Wago ({(int)response.StatusCode}): {body}"); Log("Wago upload successful: " + body);
    }
    private static void AddWagoPatch(Dictionary<string, object?> metadata, string key, TextBox field)
    {
        if (!string.IsNullOrWhiteSpace(field.Text)) metadata[key] = field.Text.Trim();
    }
}
