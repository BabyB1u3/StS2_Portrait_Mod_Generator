using System.Text.Json;
using System.Windows.Forms;
using PortraitModGenerator.Core.Abstractions;
using PortraitModGenerator.Core.Models;
using PortraitModGenerator.Core.Services;

namespace PortraitModGenerator.Gui;

public sealed class MainForm : Form
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly Button _loadAnalysisButton;
    private readonly Button _importPckButton;
    private readonly Button _saveAnalysisButton;
    private readonly Label _analysisPathLabel;
    private readonly Label _importStatusLabel;
    private readonly ProgressBar _importProgressBar;
    private readonly ComboBox _filterComboBox;
    private readonly TextBox _searchTextBox;
    private readonly ListBox _packageListBox;
    private readonly ListBox _assetListBox;
    private readonly PictureBox _previewBox;
    private readonly Label _statusLabel;
    private readonly Label _pathLabel;
    private readonly Label _reasonLabel;
    private readonly CheckBox _selectedCheckBox;
    private readonly CheckBox _ignoredCheckBox;
    private readonly ComboBox _groupComboBox;
    private readonly ComboBox _cardComboBox;
    private readonly Label _summaryLabel;
    private readonly TextBox _modIdTextBox;
    private readonly TextBox _modNameTextBox;
    private readonly TextBox _authorTextBox;
    private readonly TextBox _descriptionTextBox;
    private readonly TextBox _outputDirectoryTextBox;
    private readonly Button _browseOutputButton;
    private readonly Button _updateMappingButton;
    private readonly Button _generateModButton;
    private readonly Label _generationStatusLabel;
    private readonly ProgressBar _buildProgressBar;
    private readonly Panel _detailsPanel;

    private string? _analysisPath;
    private string? _officialCardIndexPath;
    private MergedReviewSession? _session;
    private List<CardChoice> _allCardChoices = [];
    private readonly ConflictResolutionService _conflictResolutionService = new();
    private bool _suppressEvents;

    public MainForm()
    {
        Text = "Portrait Mod Generator - Mapping Review";
        Width = 1850;
        Height = 1070;
        StartPosition = FormStartPosition.CenterScreen;
        AllowDrop = true;
        DragEnter += HandlePckDragEnter;
        DragDrop += HandlePckDragDrop;

        TableLayoutPanel rootLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12)
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(rootLayout);

        FlowLayoutPanel toolbar = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true
        };
        rootLayout.Controls.Add(toolbar, 0, 0);

        _loadAnalysisButton = new Button
        {
            Text = "Import PCK",
            AutoSize = true
        };
        _loadAnalysisButton.Click += async (_, _) => await ImportPckFromDialogAsync();
        toolbar.Controls.Add(_loadAnalysisButton);

        _importPckButton = new Button
        {
            Text = "Load Session",
            AutoSize = true,
            Visible = true
        };
        _importPckButton.Click += (_, _) => LoadSessionFromDialog();
        toolbar.Controls.Add(_importPckButton);

        _saveAnalysisButton = new Button
        {
            Text = "Save Session As",
            AutoSize = true,
            Enabled = false,
            Visible = true
        };
        _saveAnalysisButton.Click += (_, _) => SaveSessionAs();
        toolbar.Controls.Add(_saveAnalysisButton);

        _filterComboBox = new ComboBox
        {
            Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _filterComboBox.Items.AddRange(["All", "Matched", "Unmatched", "Ignored", "Selected"]);
        _filterComboBox.SelectedIndex = 0;
        _filterComboBox.SelectedIndexChanged += (_, _) => RefreshAssetList();
        toolbar.Controls.Add(_filterComboBox);

        _searchTextBox = new TextBox
        {
            Width = 260,
            PlaceholderText = "Search file name or card id"
        };
        _searchTextBox.TextChanged += (_, _) => RefreshAssetList();
        toolbar.Controls.Add(_searchTextBox);

        _summaryLabel = new Label
        {
            AutoSize = true,
            Padding = new Padding(12, 8, 0, 0)
        };
        toolbar.Controls.Add(_summaryLabel);

        _analysisPathLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 0, 0, 8)
        };
        rootLayout.Controls.Add(_analysisPathLabel, 0, 1);

        _importStatusLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 0, 0, 8)
        };
        rootLayout.Controls.Add(_importStatusLabel, 0, 2);

        _importProgressBar = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = 18,
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 24,
            Visible = false,
            Margin = new Padding(0, 0, 0, 8)
        };
        rootLayout.Controls.Add(_importProgressBar, 0, 3);

        SplitContainer contentSplit = new()
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 700
        };
        rootLayout.Controls.Add(contentSplit, 0, 4);
        Shown += (_, _) =>
        {
            int availableWidth = contentSplit.ClientSize.Width;
            if (availableWidth > 0)
            {
                contentSplit.SplitterDistance = availableWidth / 2;
            }
        };

        TableLayoutPanel leftPanelLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        leftPanelLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        leftPanelLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        contentSplit.Panel1.Controls.Add(leftPanelLayout);

        _packageListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            HorizontalScrollbar = true
        };
        leftPanelLayout.Controls.Add(_packageListBox, 0, 0);

        _assetListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            HorizontalScrollbar = true,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 24
        };
        _assetListBox.SelectedIndexChanged += (_, _) => BindSelectedItem();
        _assetListBox.DrawItem += DrawAssetListItem;
        leftPanelLayout.Controls.Add(_assetListBox, 0, 1);

        TableLayoutPanel detailLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 9
        };
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        contentSplit.Panel2.Controls.Add(detailLayout);

        _previewBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        detailLayout.Controls.Add(_previewBox, 0, 0);

        _detailsPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };
        detailLayout.Controls.Add(_detailsPanel, 0, 1);

        TableLayoutPanel detailsContentLayout = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 7
        };
        _detailsPanel.Controls.Add(detailsContentLayout);

        _statusLabel = CreateInfoLabel();
        detailsContentLayout.Controls.Add(_statusLabel, 0, 0);

        _pathLabel = CreateInfoLabel();
        detailsContentLayout.Controls.Add(_pathLabel, 0, 1);

        _reasonLabel = CreateInfoLabel();
        detailsContentLayout.Controls.Add(_reasonLabel, 0, 2);

        FlowLayoutPanel statePanel = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill
        };
        detailsContentLayout.Controls.Add(statePanel, 0, 3);

        _selectedCheckBox = new CheckBox
        {
            Text = "Selected",
            AutoSize = true
        };
        _selectedCheckBox.CheckedChanged += (_, _) => ApplySelectionState();
        statePanel.Controls.Add(_selectedCheckBox);

        _ignoredCheckBox = new CheckBox
        {
            Text = "Ignored",
            AutoSize = true
        };
        _ignoredCheckBox.CheckedChanged += (_, _) => ApplyIgnoredState();
        statePanel.Controls.Add(_ignoredCheckBox);

        FlowLayoutPanel cardPanel = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            WrapContents = false
        };
        detailsContentLayout.Controls.Add(cardPanel, 0, 4);

        Label cardLabel = new()
        {
            Text = "Group:",
            AutoSize = true,
            Padding = new Padding(0, 8, 8, 0)
        };
        cardPanel.Controls.Add(cardLabel);

        _groupComboBox = new ComboBox
        {
            Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _groupComboBox.SelectedIndexChanged += (_, _) => ApplyGroupFilter();
        cardPanel.Controls.Add(_groupComboBox);

        Label manualCardLabel = new()
        {
            Text = "Card:",
            AutoSize = true,
            Padding = new Padding(12, 8, 8, 0)
        };
        cardPanel.Controls.Add(manualCardLabel);

        _cardComboBox = new ComboBox
        {
            Width = 360,
            DropDownStyle = ComboBoxStyle.DropDown,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems,
            DisplayMember = nameof(CardChoice.DisplayText)
        };
        _cardComboBox.SelectedIndexChanged += (_, _) => OnPendingCardSelectionChanged();
        _cardComboBox.TextUpdate += (_, _) => { };
        cardPanel.Controls.Add(_cardComboBox);

        _updateMappingButton = new Button
        {
            Text = "Update Mapping",
            AutoSize = true,
            Enabled = false,
            Margin = new Padding(12, 3, 3, 3)
        };
        _updateMappingButton.Click += (_, _) => ApplyManualCardSelection();
        cardPanel.Controls.Add(_updateMappingButton);

        Label helpLabel = CreateInfoLabel();
        helpLabel.Text = "Use the card dropdown to assign unmatched images, or mark them ignored.";
        detailsContentLayout.Controls.Add(helpLabel, 0, 5);

        GroupBox generationGroup = new()
        {
            Text = "Generate Mod",
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            AutoSize = true
        };
        detailsContentLayout.Controls.Add(generationGroup, 0, 6);

        TableLayoutPanel generationLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 6,
            AutoSize = true
        };
        generationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        generationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        generationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        generationGroup.Controls.Add(generationLayout);

        generationLayout.Controls.Add(CreateFieldLabel("Mod ID:"), 0, 0);
        _modIdTextBox = CreateFieldTextBox();
        generationLayout.Controls.Add(_modIdTextBox, 1, 0);

        generationLayout.Controls.Add(CreateFieldLabel("Mod Name:"), 0, 1);
        _modNameTextBox = CreateFieldTextBox();
        generationLayout.Controls.Add(_modNameTextBox, 1, 1);

        generationLayout.Controls.Add(CreateFieldLabel("Author:"), 0, 2);
        _authorTextBox = CreateFieldTextBox();
        generationLayout.Controls.Add(_authorTextBox, 1, 2);

        generationLayout.Controls.Add(CreateFieldLabel("Description:"), 0, 3);
        _descriptionTextBox = CreateFieldTextBox();
        generationLayout.Controls.Add(_descriptionTextBox, 1, 3);

        generationLayout.Controls.Add(CreateFieldLabel("Artifact Dir:"), 0, 4);
        _outputDirectoryTextBox = CreateFieldTextBox();
        generationLayout.Controls.Add(_outputDirectoryTextBox, 1, 4);

        _browseOutputButton = new Button
        {
            Text = "Browse",
            AutoSize = true
        };
        _browseOutputButton.Click += (_, _) => BrowseOutputDirectory();
        generationLayout.Controls.Add(_browseOutputButton, 2, 4);

        FlowLayoutPanel generatePanel = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill
        };
        generationLayout.Controls.Add(generatePanel, 1, 5);
        generationLayout.SetColumnSpan(generatePanel, 2);

        _generateModButton = new Button
        {
            Text = "Build Mod",
            AutoSize = true
        };
        _generateModButton.Click += async (_, _) => await GenerateModProjectAsync();
        generatePanel.Controls.Add(_generateModButton);

        _generationStatusLabel = CreateInfoLabel();
        _generationStatusLabel.Text = "Import a PCK, review mappings, then build the final mod output here.";
        generatePanel.Controls.Add(_generationStatusLabel);

        _buildProgressBar = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = 18,
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 24,
            Visible = false,
            Margin = new Padding(0, 8, 0, 0)
        };
        generationLayout.Controls.Add(_buildProgressBar, 0, 6);
        generationLayout.SetColumnSpan(_buildProgressBar, 3);

        _analysisPathLabel.Text = "Import one or more portrait PCK files to begin review.";
        _importStatusLabel.Text = $"GDRE: {AppPaths.GdreToolsPath} | Cache: {AppPaths.CacheRoot}";
        _authorTextBox.Text = "Unknown Author";
        _descriptionTextBox.Text = "Generated portrait replacement mod";
        _outputDirectoryTextBox.Text = AppPaths.ArtifactOutputRoot;

        EnablePckDragDrop(rootLayout);
    }

    private static Label CreateInfoLabel()
    {
        return new Label
        {
            AutoSize = true,
            MaximumSize = new Size(860, 0),
            Padding = new Padding(0, 4, 0, 4)
        };
    }

    private static Label CreateFieldLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(0, 8, 8, 0)
        };
    }

    private static TextBox CreateFieldTextBox()
    {
        return new TextBox
        {
            Dock = DockStyle.Fill,
            Width = 420
        };
    }

    private void LoadSessionFromDialog()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Open review session",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            LoadSession(dialog.FileName);
        }
    }

    private async Task ImportPckFromDialogAsync()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Import portrait PCK",
            Filter = "PCK files (*.pck)|*.pck|All files (*.*)|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await ImportPcksAsync(dialog.FileNames);
        }
    }

    private void HandlePckDragEnter(object? sender, DragEventArgs eventArgs)
    {
        eventArgs.Effect = HasDroppedPck(eventArgs.Data)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private async void HandlePckDragDrop(object? sender, DragEventArgs eventArgs)
    {
        string[] pckPaths = GetDroppedPckPaths(eventArgs.Data);
        if (pckPaths.Length == 0)
        {
            return;
        }

        await ImportPcksAsync(pckPaths);
    }

    private void EnablePckDragDrop(Control control)
    {
        control.AllowDrop = true;
        control.DragEnter += HandlePckDragEnter;
        control.DragDrop += HandlePckDragDrop;

        foreach (Control child in control.Controls)
        {
            EnablePckDragDrop(child);
        }
    }

    private static bool HasDroppedPck(IDataObject? dataObject)
    {
        return GetDroppedPckPaths(dataObject).Length > 0;
    }

    private static string[] GetDroppedPckPaths(IDataObject? dataObject)
    {
        if (dataObject?.GetData(DataFormats.FileDrop) is not string[] droppedPaths)
        {
            return [];
        }

        return droppedPaths
            .Where(path => string.Equals(Path.GetExtension(path), ".pck", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task ImportPcksAsync(IReadOnlyList<string> pckPaths)
    {
        string gdreToolsPath = AppPaths.GdreToolsPath;
        if (!File.Exists(gdreToolsPath))
        {
            MessageBox.Show(
                this,
                $"GDRETools was not found.\nExpected at:\n{gdreToolsPath}",
                "Import PCK",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        string[] normalizedPckPaths = pckPaths
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (normalizedPckPaths.Length == 0)
        {
            return;
        }

        string firstSourcePckPath = normalizedPckPaths[0];
        string sessionRoot = ResolveOrCreateSessionRoot(firstSourcePckPath);
        string sessionId = Path.GetFileName(sessionRoot);
        string mergedJsonPath = Path.Combine(sessionRoot, "merged", "merged_review_session.json");
        string suggestedModId = SanitizeModId(Path.GetFileNameWithoutExtension(firstSourcePckPath));

        try
        {
            UseWaitCursor = true;
            TogglePrimaryActions(enabled: false);
            SetImportBusy(true, $"Importing {normalizedPckPaths.Length} PCK file(s) into {sessionRoot}");

            IProgress<string> progress = new Progress<string>(status => _importStatusLabel.Text = status);
            await Task.Run(() =>
            {
                Directory.CreateDirectory(sessionRoot);

                List<ImportedPackage> packages = _session?.Packages
                    .OrderBy(package => package.ImportOrder)
                    .ToList() ?? [];
                int nextImportOrder = packages.Count + 1;

                foreach (string sourcePckPath in normalizedPckPaths)
                {
                    if (packages.Any(package => string.Equals(package.SourcePckPath, sourcePckPath, StringComparison.OrdinalIgnoreCase)))
                    {
                        progress.Report($"Skipping already imported package: {Path.GetFileName(sourcePckPath)}");
                        continue;
                    }

                    string packageId = CreatePackageId(nextImportOrder, sourcePckPath);
                    string packageRoot = Path.Combine(sessionRoot, "imports", packageId);
                    string recoverRoot = Path.Combine(packageRoot, "recover");
                    string scanJsonPath = Path.Combine(packageRoot, "asset_scan_result.json");
                    string mappingJsonPath = Path.Combine(packageRoot, "mapping_analysis_result.json");

                    progress.Report($"Recovering {Path.GetFileName(sourcePckPath)}");
                    GdrePckImporter importer = new();
                    importer.Import(new PckImportRequest
                    {
                        SourcePckPath = sourcePckPath,
                        OutputDirectory = recoverRoot,
                        GdreToolsPath = gdreToolsPath,
                        OverwriteOutput = true
                    });

                    progress.Report($"Scanning extracted images from {Path.GetFileName(sourcePckPath)}");
                    AssetScanner scanner = new();
                    scanner.Scan(new AssetScanRequest
                    {
                        InputDirectory = recoverRoot,
                        OutputJsonPath = scanJsonPath
                    });

                    progress.Report($"Analyzing mapping candidates for {Path.GetFileName(sourcePckPath)}");
                    MappingAnalyzer analyzer = new();
                    analyzer.Analyze(new MappingAnalysisRequest
                    {
                        ScanResultPath = scanJsonPath,
                        OfficialCardIndexPath = AppPaths.OfficialCardIndexPath,
                        OutputJsonPath = mappingJsonPath
                    });

                    packages.Add(new ImportedPackage
                    {
                        PackageId = packageId,
                        DisplayName = Path.GetFileNameWithoutExtension(sourcePckPath),
                        SourcePckPath = sourcePckPath,
                        RecoverRoot = recoverRoot,
                        ScanResultPath = scanJsonPath,
                        MappingAnalysisPath = mappingJsonPath,
                        ImportedAt = DateTimeOffset.UtcNow,
                        ImportOrder = nextImportOrder
                    });
                    nextImportOrder++;
                }

                progress.Report("Merging package analyses into a review session");
                MergeMappingsService mergeService = new();
                MergedReviewSession mergedSession = mergeService.Merge(new MergeMappingsRequest
                {
                    SessionId = sessionId,
                    SessionRoot = sessionRoot,
                    OfficialCardIndexPath = AppPaths.OfficialCardIndexPath,
                    OutputJsonPath = mergedJsonPath,
                    Packages = packages
                });

                _session = mergedSession;
            });

            if (_session is not null)
            {
                BindSession(_session, mergedJsonPath);
            }

            _importStatusLabel.Text = $"Imported {normalizedPckPaths.Length} package(s) | Session: {sessionRoot}";

            if (string.IsNullOrWhiteSpace(_modIdTextBox.Text) || string.Equals(_modIdTextBox.Text, "GeneratedPortraitMod", StringComparison.Ordinal))
            {
                _modIdTextBox.Text = suggestedModId;
                _modNameTextBox.Text = suggestedModId;
            }
        }
        catch (Exception ex)
        {
            _importStatusLabel.Text = $"Import failed: {ex.Message}";
            MessageBox.Show(this, ex.Message, "Import PCK failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetImportBusy(false);
            TogglePrimaryActions(enabled: true);
            UseWaitCursor = false;
        }
    }

    private void LoadSession(string path)
    {
        string fullPath = Path.GetFullPath(path);
        try
        {
            string json = File.ReadAllText(fullPath);
            MergedReviewSession? session;
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("packages", out _))
            {
                session = JsonSerializer.Deserialize<MergedReviewSession>(json, JsonOptions);
                if (session is null)
                {
                    throw new InvalidOperationException("Failed to deserialize merged review session.");
                }

                _conflictResolutionService.Refresh(session);
            }
            else
            {
                MappingAnalysisResult? analysis = JsonSerializer.Deserialize<MappingAnalysisResult>(json, JsonOptions);
                if (analysis is null)
                {
                    throw new InvalidOperationException("Failed to deserialize legacy mapping analysis file.");
                }

                session = ConvertLegacyAnalysis(fullPath, analysis);
            }

            BindSession(session, fullPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BindSession(MergedReviewSession session, string sessionPath)
    {
        _analysisPath = sessionPath;
        _officialCardIndexPath = ResolveOfficialCardIndexPath(sessionPath, session.OfficialCardIndexPath);
        LoadOfficialCards(_officialCardIndexPath);

        session.OfficialCardIndexPath = _officialCardIndexPath;
        session.SessionRoot = string.IsNullOrWhiteSpace(session.SessionRoot)
            ? Path.GetDirectoryName(sessionPath) ?? AppPaths.CacheRoot
            : session.SessionRoot;
        session.OutputJsonPath = sessionPath;

        _session = session;
        _analysisPathLabel.Text = $"Session: {sessionPath}";
        _saveAnalysisButton.Enabled = true;
        if (string.IsNullOrWhiteSpace(_modIdTextBox.Text))
        {
            _modIdTextBox.Text = "GeneratedPortraitMod";
        }

        if (string.IsNullOrWhiteSpace(_modNameTextBox.Text))
        {
            _modNameTextBox.Text = "Generated Portrait Mod";
        }

        BindPackageList();
        RefreshAssetList();
    }

    private void BindPackageList()
    {
        List<ImportedPackage> packages = _session?.Packages
            .OrderBy(package => package.ImportOrder)
            .ToList() ?? [];

        _packageListBox.DataSource = packages;
        _packageListBox.DisplayMember = nameof(ImportedPackage.ListDisplayText);
    }

    private void TogglePrimaryActions(bool enabled)
    {
        _loadAnalysisButton.Enabled = enabled;
        _importPckButton.Enabled = enabled;
        _saveAnalysisButton.Enabled = enabled && _session is not null;
        _generateModButton.Enabled = enabled;
        _browseOutputButton.Enabled = enabled;
        _updateMappingButton.Enabled = enabled && _assetListBox.SelectedItem is MergedMappingCandidate && _cardComboBox.SelectedItem is CardChoice;
    }

    private void SetImportBusy(bool busy, string? statusText = null)
    {
        _importProgressBar.Visible = busy;
        if (statusText is not null)
        {
            _importStatusLabel.Text = statusText;
        }
    }

    private void SetBuildBusy(bool busy, string? statusText = null)
    {
        _buildProgressBar.Visible = busy;
        if (statusText is not null)
        {
            _generationStatusLabel.Text = statusText;
        }
    }

    private void LoadOfficialCards(string officialCardIndexPath)
    {
        OfficialCardIndex index = new OfficialCardIndexLoader().Load(officialCardIndexPath);
        _allCardChoices = index.Cards
            .OrderBy(card => card.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(card => card.CanonicalName, StringComparer.OrdinalIgnoreCase)
            .Select(card => new CardChoice(card))
            .ToList();

        List<string> groups = index.Cards
            .Select(card => card.Group)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _suppressEvents = true;
        _groupComboBox.DataSource = groups;
        _groupComboBox.SelectedIndex = -1;
        ApplyGroupFilterCore(keepText: false);
        _cardComboBox.SelectedIndex = -1;
        _suppressEvents = false;
    }

    private static string ResolveOfficialCardIndexPath(string analysisPath, string officialCardIndexPath)
    {
        if (Path.IsPathRooted(officialCardIndexPath))
        {
            return officialCardIndexPath;
        }

        string analysisDirectory = Path.GetDirectoryName(analysisPath)!;
        string combined = Path.GetFullPath(Path.Combine(analysisDirectory, officialCardIndexPath));
        if (File.Exists(combined))
        {
            return combined;
        }

        if (File.Exists(AppPaths.OfficialCardIndexPath))
        {
            return AppPaths.OfficialCardIndexPath;
        }

        return Path.GetFullPath(officialCardIndexPath);
    }

    private void RefreshAssetList()
    {
        if (_session is null)
        {
            _assetListBox.DataSource = null;
            _summaryLabel.Text = string.Empty;
            return;
        }

        IEnumerable<MergedMappingCandidate> filtered = _session.Candidates;
        string filter = _filterComboBox.SelectedItem?.ToString() ?? "All";
        filtered = filter switch
        {
            "Matched" => filtered.Where(candidate => !string.IsNullOrWhiteSpace(candidate.MatchedCardId)),
            "Unmatched" => filtered.Where(candidate => !candidate.Ignored && string.IsNullOrWhiteSpace(candidate.MatchedCardId)),
            "Ignored" => filtered.Where(candidate => candidate.Ignored),
            "Selected" => filtered.Where(candidate => candidate.Selected),
            _ => filtered
        };

        string search = _searchTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(candidate =>
                candidate.FileName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                candidate.SourceRelativePath.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (candidate.MatchedCardId?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (candidate.CanonicalName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                candidate.SourcePackageName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        List<MergedMappingCandidate> items = filtered.ToList();
        _suppressEvents = true;
        _assetListBox.DataSource = items;
        _suppressEvents = false;

        _summaryLabel.Text =
            $"Packages {_session.Packages.Count} | Visible {items.Count} | Resolved {_session.ResolvedAssets} | Conflicts {_session.ConflictGroups.Count} | Pending {_session.PendingAssets} | Unmatched {_session.UnmatchedAssets} | Ignored {_session.IgnoredAssets}";

        if (items.Count > 0)
        {
            _assetListBox.SelectedIndex = 0;
        }
        else
        {
            BindSelectedItem();
        }
    }

    private void DrawAssetListItem(object? sender, DrawItemEventArgs eventArgs)
    {
        eventArgs.DrawBackground();

        if (eventArgs.Index < 0 || eventArgs.Index >= _assetListBox.Items.Count)
        {
            return;
        }

        if (_assetListBox.Items[eventArgs.Index] is not MergedMappingCandidate candidate)
        {
            return;
        }

        Font font = eventArgs.Font ?? _assetListBox.Font;
        Font badgeFont = new(font, FontStyle.Bold);

        Color prefixColor = GetCandidateStatusColor(candidate);
        Color remainderColor = (eventArgs.State & DrawItemState.Selected) == DrawItemState.Selected
            ? SystemColors.HighlightText
            : eventArgs.ForeColor;

        Rectangle bounds = eventArgs.Bounds;
        Rectangle contentBounds = Rectangle.Inflate(bounds, -4, 0);

        int statusWidth = 96;
        int arrowWidth = 26;
        int gap = 8;
        int availableWidth = Math.Max(120, contentBounds.Width - statusWidth - arrowWidth - (gap * 2));
        int sourceWidth = availableWidth / 2;
        int targetWidth = availableWidth - sourceWidth;

        Rectangle statusRect = new(contentBounds.Left, contentBounds.Top, statusWidth, contentBounds.Height);
        Rectangle sourceRect = new(statusRect.Right + gap, contentBounds.Top, sourceWidth, contentBounds.Height);
        Rectangle arrowRect = new(sourceRect.Right + gap, contentBounds.Top, arrowWidth, contentBounds.Height);
        Rectangle targetRect = new(arrowRect.Right + gap, contentBounds.Top, targetWidth, contentBounds.Height);

        TextRenderer.DrawText(
            eventArgs.Graphics,
            GetCandidateStatusPrefix(candidate),
            badgeFont,
            statusRect,
            prefixColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        TextRenderer.DrawText(
            eventArgs.Graphics,
            candidate.FileName,
            font,
            sourceRect,
            remainderColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        TextRenderer.DrawText(
            eventArgs.Graphics,
            "->",
            font,
            arrowRect,
            remainderColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        DrawTargetCell(eventArgs.Graphics, font, badgeFont, targetRect, candidate, remainderColor);

        badgeFont.Dispose();
        eventArgs.DrawFocusRectangle();
    }

    private static void DrawTargetCell(Graphics graphics, Font font, Font badgeFont, Rectangle targetRect, MergedMappingCandidate candidate, Color defaultTextColor)
    {
        if (candidate.Ignored)
        {
            DrawBadge(graphics, targetRect, "Ignored", Color.Goldenrod, badgeFont);
            return;
        }

        if (string.IsNullOrWhiteSpace(candidate.MatchedCardId))
        {
            DrawBadge(graphics, targetRect, "Needs Review", Color.IndianRed, badgeFont);
            return;
        }

        string targetText = candidate.IsConflict
            ? $"{candidate.CanonicalName ?? candidate.MatchedCardId!} [{candidate.SourcePackageName}]"
            : candidate.CanonicalName ?? candidate.MatchedCardId!;
        TextRenderer.DrawText(
            graphics,
            targetText,
            font,
            targetRect,
            defaultTextColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private static void DrawBadge(Graphics graphics, Rectangle bounds, string text, Color color, Font font)
    {
        Size textSize = TextRenderer.MeasureText(graphics, text, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
        Rectangle badgeRect = new(
            bounds.Left,
            bounds.Top + Math.Max(0, (bounds.Height - (textSize.Height + 6)) / 2),
            Math.Min(bounds.Width, textSize.Width + 16),
            textSize.Height + 6);

        using SolidBrush backgroundBrush = new(Color.FromArgb(32, color));
        using Pen borderPen = new(color);
        using SolidBrush textBrush = new(color);

        graphics.FillRectangle(backgroundBrush, badgeRect);
        graphics.DrawRectangle(borderPen, badgeRect);
        TextRenderer.DrawText(
            graphics,
            text,
            font,
            badgeRect,
            color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private static string GetCandidateStatusPrefix(MergedMappingCandidate candidate)
    {
        if (candidate.Ignored)
        {
            return "[Ignored]";
        }

        if (string.IsNullOrWhiteSpace(candidate.MatchedCardId))
        {
            return "[Unmatched]";
        }

        if (candidate.Selected)
        {
            return candidate.IsConflict ? "[Winner]" : "[Selected]";
        }

        return "[Pending]";
    }

    private static string GetCandidateStatusName(MergedMappingCandidate candidate)
    {
        if (candidate.Ignored)
        {
            return "Ignored";
        }

        if (string.IsNullOrWhiteSpace(candidate.MatchedCardId))
        {
            return "Unmatched";
        }

        if (candidate.Selected)
        {
            return candidate.IsConflict ? "Conflict winner" : "Selected";
        }

        return "Pending";
    }

    private static Color GetCandidateStatusColor(MergedMappingCandidate candidate)
    {
        if (candidate.Ignored)
        {
            return Color.Goldenrod;
        }

        if (string.IsNullOrWhiteSpace(candidate.MatchedCardId))
        {
            return Color.Firebrick;
        }

        if (candidate.Selected)
        {
            return candidate.IsConflict ? Color.MediumSeaGreen : Color.ForestGreen;
        }

        return candidate.IsConflict ? Color.DarkOrange : Color.SteelBlue;
    }

    private void BindSelectedItem()
    {
        if (_suppressEvents)
        {
            return;
        }

        MergedMappingCandidate? candidate = _assetListBox.SelectedItem as MergedMappingCandidate;
        _suppressEvents = true;

        try
        {
            _selectedCheckBox.Enabled = candidate is not null;
            _ignoredCheckBox.Enabled = candidate is not null;
            _groupComboBox.Enabled = candidate is not null;
            _cardComboBox.Enabled = candidate is not null;
            _updateMappingButton.Enabled = false;

            if (candidate is null)
            {
                _previewBox.Image = null;
                _statusLabel.Text = string.Empty;
                _pathLabel.Text = string.Empty;
                _reasonLabel.Text = string.Empty;
                _selectedCheckBox.Checked = false;
                _ignoredCheckBox.Checked = false;
                _groupComboBox.SelectedIndex = -1;
                _cardComboBox.SelectedIndex = -1;
                _cardComboBox.Text = string.Empty;
                return;
            }

            _selectedCheckBox.Checked = candidate.Selected;
            _ignoredCheckBox.Checked = candidate.Ignored;
            _statusLabel.Text = $"Status: {GetCandidateStatusName(candidate)} | Card: {candidate.CanonicalName ?? "(none)"} | Group: {candidate.Group ?? "(none)"} | Package: {candidate.SourcePackageName}";
            _pathLabel.Text = $"Path: {candidate.SourceRelativePath}";
            _reasonLabel.Text = $"Reason: {candidate.MatchReason ?? candidate.IgnoredReason ?? "(none)"}";
            BindCardSelection(candidate);
            LoadPreview(candidate.SourceAbsolutePath);
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private void BindCardSelection(MergedMappingCandidate candidate)
    {
        if (_groupComboBox.DataSource is not List<string> groups || groups.Count == 0)
        {
            _groupComboBox.SelectedIndex = -1;
            _cardComboBox.DataSource = null;
            _cardComboBox.Text = string.Empty;
            _updateMappingButton.Enabled = false;
            return;
        }

        int groupIndex = -1;
        if (!string.IsNullOrWhiteSpace(candidate.Group))
        {
            groupIndex = _groupComboBox.FindStringExact(candidate.Group);
        }

        _groupComboBox.SelectedIndex = groupIndex;
        ApplyGroupFilterCore(keepText: true);

        if (_cardComboBox.DataSource is not List<CardChoice> choices)
        {
            _cardComboBox.SelectedIndex = -1;
            _cardComboBox.Text = string.Empty;
            _updateMappingButton.Enabled = false;
            return;
        }

        CardChoice? selectedChoice = choices.FirstOrDefault(choice =>
            string.Equals(choice.Card.CardId, candidate.MatchedCardId, StringComparison.OrdinalIgnoreCase));

        if (selectedChoice is null)
        {
            _cardComboBox.SelectedIndex = -1;
            _cardComboBox.Text = candidate.CanonicalName ?? string.Empty;
            _updateMappingButton.Enabled = false;
            return;
        }

        _cardComboBox.SelectedItem = selectedChoice;
        _cardComboBox.Text = selectedChoice.DisplayText;
        _updateMappingButton.Enabled = false;
    }

    private void LoadPreview(string sourceAbsolutePath)
    {
        if (!File.Exists(sourceAbsolutePath))
        {
            _previewBox.Image = null;
            return;
        }

        using MemoryStream stream = new(File.ReadAllBytes(sourceAbsolutePath));
        using Image loadedImage = Image.FromStream(stream);
        _previewBox.Image?.Dispose();
        _previewBox.Image = new Bitmap(loadedImage);
    }

    private void ApplySelectionState()
    {
        if (_suppressEvents)
        {
            return;
        }

        if (_assetListBox.SelectedItem is not MergedMappingCandidate candidate)
        {
            return;
        }

        candidate.Selected = _selectedCheckBox.Checked;
        if (candidate.Selected)
        {
            candidate.Ignored = false;
            candidate.IgnoredReason = null;
            DeselectSiblingCandidates(candidate);
            _ignoredCheckBox.Checked = false;
        }

        SynchronizeSession();
        RefreshCurrentBindings();
    }

    private void ApplyIgnoredState()
    {
        if (_suppressEvents)
        {
            return;
        }

        if (_assetListBox.SelectedItem is not MergedMappingCandidate candidate)
        {
            return;
        }

        candidate.Ignored = _ignoredCheckBox.Checked;
        if (candidate.Ignored)
        {
            candidate.Selected = false;
            candidate.IgnoredReason ??= "Ignored during manual review.";
            _selectedCheckBox.Checked = false;
        }
        else if (candidate.IgnoredReason == "Ignored during manual review.")
        {
            candidate.IgnoredReason = null;
        }

        SynchronizeSession();
        RefreshCurrentBindings();
    }

    private void OnPendingCardSelectionChanged()
    {
        if (_suppressEvents)
        {
            return;
        }

        _updateMappingButton.Enabled =
            _assetListBox.SelectedItem is MergedMappingCandidate &&
            _cardComboBox.SelectedItem is CardChoice;
    }

    private void ApplyManualCardSelection()
    {
        if (_suppressEvents)
        {
            return;
        }

        if (_assetListBox.SelectedItem is not MergedMappingCandidate candidate)
        {
            return;
        }

        if (_cardComboBox.SelectedItem is not CardChoice choice)
        {
            return;
        }

        candidate.MatchedCardId = choice.Card.CardId;
        candidate.CanonicalName = choice.Card.CanonicalName;
        candidate.Group = choice.Card.Group;
        candidate.Selected = true;
        candidate.Ignored = false;
        candidate.IgnoredReason = null;
        candidate.Confidence = 1.0;
        candidate.MatchReason = "Manually assigned in GUI.";
        DeselectSiblingCandidates(candidate);
        SynchronizeSession();
        _updateMappingButton.Enabled = false;
        RefreshCurrentBindings();
    }

    private void ApplyGroupFilter()
    {
        if (_suppressEvents)
        {
            return;
        }

        ApplyGroupFilterCore(keepText: false);
    }

    private void ApplyGroupFilterCore(bool keepText)
    {
        string? selectedGroup = _groupComboBox.SelectedItem?.ToString();
        List<CardChoice> filtered = string.IsNullOrWhiteSpace(selectedGroup)
            ? []
            : _allCardChoices
                .Where(choice => string.Equals(choice.Card.Group, selectedGroup, StringComparison.OrdinalIgnoreCase))
                .ToList();

        string existingText = _cardComboBox.Text;
        _cardComboBox.DataSource = filtered;
        _cardComboBox.SelectedIndex = -1;
        _cardComboBox.Text = keepText ? existingText : string.Empty;
    }

    private void RefreshCurrentBindings()
    {
        MergedMappingCandidate? current = _assetListBox.SelectedItem as MergedMappingCandidate;
        RefreshAssetList();
        if (current is null)
        {
            return;
        }

        if (_assetListBox.DataSource is List<MergedMappingCandidate> items)
        {
            MergedMappingCandidate? updated = items.FirstOrDefault(item =>
                string.Equals(item.CandidateId, current.CandidateId, StringComparison.OrdinalIgnoreCase));
            if (updated is not null)
            {
                _assetListBox.SelectedItem = updated;
            }
        }
    }

    private void SaveSessionAs()
    {
        if (_session is null || string.IsNullOrWhiteSpace(_analysisPath))
        {
            return;
        }

        using SaveFileDialog dialog = new()
        {
            Title = "Save reviewed session",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = Path.GetFileName(_analysisPath)
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _session.OutputJsonPath = dialog.FileName;
        SynchronizeSession();
        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(_session, JsonOptions));
        _analysisPath = dialog.FileName;
        _analysisPathLabel.Text = $"Session: {dialog.FileName}";
        MessageBox.Show(this, $"Saved session file to:\n{dialog.FileName}", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void BrowseOutputDirectory()
    {
        using FolderBrowserDialog dialog = new();
        if (!string.IsNullOrWhiteSpace(_outputDirectoryTextBox.Text) && Directory.Exists(_outputDirectoryTextBox.Text))
        {
            dialog.InitialDirectory = _outputDirectoryTextBox.Text;
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _outputDirectoryTextBox.Text = dialog.SelectedPath;
        }
    }

    private async Task GenerateModProjectAsync()
    {
        if (_session is null || string.IsNullOrWhiteSpace(_officialCardIndexPath))
        {
            MessageBox.Show(this, "Import and review portrait PCK packages first.", "Build mod", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SynchronizeSession();
        int pendingCount = _session.PendingAssets;
        if (pendingCount > 0)
        {
            DialogResult pendingDecision = MessageBox.Show(
                this,
                $"There are {pendingCount} pending item(s) that will be skipped during generation.\n\nDo you want to continue anyway?",
                "Pending mappings",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (pendingDecision != DialogResult.Yes)
            {
                _generationStatusLabel.Text = $"Generation cancelled. {pendingCount} pending item(s) still need review.";
                return;
            }
        }

        string modId = _modIdTextBox.Text.Trim();
        string modName = _modNameTextBox.Text.Trim();
        string author = _authorTextBox.Text.Trim();
        string description = _descriptionTextBox.Text.Trim();
        string artifactOutputParent = _outputDirectoryTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(modId))
        {
            MessageBox.Show(this, "Mod ID is required.", "Build mod", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(artifactOutputParent))
        {
            MessageBox.Show(this, "Artifact output directory is required.", "Build mod", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string templateDirectory = AppPaths.PortraitTemplateDirectory;
        string sessionRoot = ResolveSessionRoot();
        string sourceGenerationRoot = Path.Combine(sessionRoot, "generated_src", modId);
        string artifactOutputDirectory = Path.Combine(Path.GetFullPath(artifactOutputParent), modId);
        string reviewPath = Path.Combine(sessionRoot, "merged", $"{modId}.merged_review_session.json");
        string buildLogPath = Path.Combine(sessionRoot, "build", modId, "publish.log");

        try
        {
            UseWaitCursor = true;
            TogglePrimaryActions(enabled: false);
            SetBuildBusy(true, "Preparing mod build...");

            Directory.CreateDirectory(Path.GetFullPath(artifactOutputParent));

            IProgress<string> progress = new Progress<string>(status => _generationStatusLabel.Text = status);
            ModBuildResult buildResult = await Task.Run(() =>
            {
                progress.Report("Generating cached source tree");
                TemplateProjectGenerator templateGenerator = new();
                TemplateGenerationResult generationResult = templateGenerator.Generate(new TemplateGenerationRequest
                {
                    TemplateDirectory = templateDirectory,
                    OutputDirectory = sourceGenerationRoot,
                    OverwriteExistingOutput = true,
                    TokenValues = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["__MOD_ID__"] = modId,
                        ["__MOD_NAME__"] = string.IsNullOrWhiteSpace(modName) ? modId : modName,
                        ["__AUTHOR__"] = string.IsNullOrWhiteSpace(author) ? "Unknown Author" : author,
                        ["__DESCRIPTION__"] = string.IsNullOrWhiteSpace(description) ? "Generated portrait replacement mod" : description,
                        ["__VERSION__"] = "v0.1.0"
                    }
                });

                progress.Report("Writing reviewed mapping data");
                _session.OfficialCardIndexPath = _officialCardIndexPath;
                _session.OutputJsonPath = reviewPath;
                SynchronizeSession();
                File.WriteAllText(reviewPath, JsonSerializer.Serialize(_session, JsonOptions));

                progress.Report("Materializing portraits and config");
                MappingMaterializer materializer = new();
                materializer.Materialize(new MaterializeMappingsRequest
                {
                    MappingAnalysisPath = reviewPath,
                    ModProjectRoot = sourceGenerationRoot,
                    ModId = modId
                });

                progress.Report("Building final mod artifacts");
                ModBuildService buildService = new();
                return buildService.Build(new ModBuildRequest
                {
                    ProjectFilePath = generationResult.EntryProjectPath,
                    ArtifactOutputDirectory = artifactOutputDirectory,
                    LogFilePath = buildLogPath,
                    DotnetCliHome = AppPaths.DotnetCliHome
                });
            });

            _generationStatusLabel.Text = $"Built mod to {artifactOutputDirectory}";
            MessageBox.Show(
                this,
                $"Built mod output:\n{artifactOutputDirectory}\n\nBuild log:\n{buildResult.LogFilePath}",
                "Build mod",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            string logMessage = File.Exists(buildLogPath)
                ? $"\n\nBuild log:\n{buildLogPath}"
                : string.Empty;
            _generationStatusLabel.Text = $"Build failed. Log: {buildLogPath}";
            MessageBox.Show(this, $"{ex.Message}{logMessage}", "Build mod failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBuildBusy(false);
            TogglePrimaryActions(enabled: true);
            UseWaitCursor = false;
        }
    }

    private string ResolveSessionRoot()
    {
        if (_session is not null && !string.IsNullOrWhiteSpace(_session.SessionRoot))
        {
            Directory.CreateDirectory(_session.SessionRoot);
            return _session.SessionRoot;
        }

        if (!string.IsNullOrWhiteSpace(_analysisPath))
        {
            string? analysisDirectory = Path.GetDirectoryName(_analysisPath);
            if (!string.IsNullOrWhiteSpace(analysisDirectory))
            {
                return analysisDirectory;
            }
        }

        string fallbackSessionName = $"manual_{DateTime.Now:yyyyMMdd_HHmmss}";
        string fallbackSessionRoot = Path.Combine(AppPaths.CacheRoot, "sessions", fallbackSessionName);
        Directory.CreateDirectory(fallbackSessionRoot);
        return fallbackSessionRoot;
    }

    private string ResolveOrCreateSessionRoot(string sourcePckPath)
    {
        if (_session is not null && !string.IsNullOrWhiteSpace(_session.SessionRoot))
        {
            Directory.CreateDirectory(_session.SessionRoot);
            return _session.SessionRoot;
        }

        string sessionName = $"{DateTime.Now:yyyyMMdd_HHmmss}_merge_{SanitizeSessionName(Path.GetFileNameWithoutExtension(sourcePckPath))}";
        string sessionRoot = Path.Combine(AppPaths.CacheRoot, "sessions", sessionName);
        Directory.CreateDirectory(sessionRoot);
        return sessionRoot;
    }

    private static string CreatePackageId(int importOrder, string sourcePckPath)
    {
        return $"{importOrder:000}_{SanitizeSessionName(Path.GetFileNameWithoutExtension(sourcePckPath))}";
    }

    private static string SanitizeSessionName(string value)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string sanitized = new(value
            .Select(character => invalid.Contains(character) ? '_' : character)
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "session" : sanitized;
    }

    private static string SanitizeModId(string value)
    {
        string cleaned = new(value.Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return "GeneratedPortraitMod";
        }

        return cleaned;
    }

    private void DeselectSiblingCandidates(MergedMappingCandidate selectedCandidate)
    {
        if (_session is null || string.IsNullOrWhiteSpace(selectedCandidate.MatchedCardId))
        {
            return;
        }

        foreach (MergedMappingCandidate sibling in _session.Candidates.Where(candidate =>
                     !string.Equals(candidate.CandidateId, selectedCandidate.CandidateId, StringComparison.OrdinalIgnoreCase) &&
                     string.Equals(candidate.MatchedCardId, selectedCandidate.MatchedCardId, StringComparison.OrdinalIgnoreCase)))
        {
            sibling.Selected = false;
        }
    }

    private void SynchronizeSession()
    {
        if (_session is null)
        {
            return;
        }

        _conflictResolutionService.Refresh(_session);
        BindPackageList();
    }

    private MergedReviewSession ConvertLegacyAnalysis(string analysisPath, MappingAnalysisResult analysis)
    {
        string sessionRoot = Path.GetDirectoryName(analysisPath) ?? AppPaths.CacheRoot;
        string sessionId = Path.GetFileName(sessionRoot);
        string packageName = Path.GetFileNameWithoutExtension(analysisPath);
        string mergedOutputPath = Path.Combine(sessionRoot, "merged", "merged_review_session.json");

        ImportedPackage package = new()
        {
            PackageId = CreatePackageId(1, packageName),
            DisplayName = packageName,
            SourcePckPath = packageName,
            RecoverRoot = ResolvePathRelativeToDocument(analysisPath, string.Empty, preferDirectory: true),
            ScanResultPath = ResolvePathRelativeToDocument(analysisPath, analysis.ScanResultPath),
            MappingAnalysisPath = analysisPath,
            ImportedAt = DateTimeOffset.UtcNow,
            ImportOrder = 1
        };

        MergeMappingsService mergeService = new();
        return mergeService.Merge(new MergeMappingsRequest
        {
            SessionId = sessionId,
            SessionRoot = sessionRoot,
            OfficialCardIndexPath = ResolveOfficialCardIndexPath(analysisPath, analysis.OfficialCardIndexPath),
            OutputJsonPath = mergedOutputPath,
            Packages = [package]
        });
    }

    private static string ResolvePathRelativeToDocument(string documentPath, string targetPath, bool preferDirectory = false)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return preferDirectory
                ? Path.GetDirectoryName(documentPath) ?? Path.GetFullPath(documentPath)
                : Path.GetFullPath(documentPath);
        }

        if (Path.IsPathRooted(targetPath))
        {
            return targetPath;
        }

        string documentDirectory = Path.GetDirectoryName(documentPath) ?? Directory.GetCurrentDirectory();
        return Path.GetFullPath(Path.Combine(documentDirectory, targetPath));
    }

    private sealed class CardChoice
    {
        public CardChoice(OfficialCardEntry card)
        {
            Card = card;
            DisplayText = $"{card.CanonicalName} ({card.Group} / {card.CardId})";
        }

        public OfficialCardEntry Card { get; }

        public string DisplayText { get; }
    }
}
