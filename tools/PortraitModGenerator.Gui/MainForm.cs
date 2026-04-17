using System.Windows.Forms;
using PortraitModGenerator.Core.Abstractions;
using PortraitModGenerator.Core.Models;
using PortraitModGenerator.Core.Services;
using PortraitModGenerator.Gui.Resources;

namespace PortraitModGenerator.Gui;

public sealed class MainForm : Form
{
    private readonly Button _loadAnalysisButton;
    private readonly Button _openConflictsButton;
    private readonly Button _openBuildModButton;
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
    private readonly CheckBox _discardCheckBox;
    private readonly ComboBox _groupComboBox;
    private readonly ComboBox _cardComboBox;
    private readonly Label _summaryLabel;
    private readonly Button _updateMappingButton;
    private readonly Button _advancedSettingsButton;
    private readonly Panel _detailsPanel;
    private readonly Label _groupLabel;
    private readonly Label _manualCardLabel;
    private readonly Label _helpLabel;
    private readonly MenuStrip _menuStrip;
    private readonly ToolStripMenuItem _menuFile;
    private readonly ToolStripMenuItem _menuFileOpen;
    private readonly ToolStripMenuItem _menuFileExit;
    private readonly ToolStripMenuItem _menuBuild;
    private readonly ToolStripMenuItem _menuBuildMod;
    private readonly ToolStripMenuItem _menuView;
    private readonly ToolStripMenuItem _menuViewLanguage;
    private readonly ToolStripMenuItem _menuLangEnglish;
    private readonly ToolStripMenuItem _menuLangChinese;

    private string? _analysisPath;
    private string? _officialCardIndexPath;
    private MergedReviewSession? _session;
    private List<CardChoice> _allCardChoices = [];
    private readonly ConflictResolutionService _conflictResolutionService = new();
    private ConflictReviewForm? _conflictReviewForm;
    private BuildModForm? _buildModForm;
    private readonly BuildModDraft _buildModDraft = new();
    private bool _suppressEvents;

    public MainForm()
    {
        Text = Strings.MainForm_Title;
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
            Text = Strings.Button_ImportPck,
            AutoSize = true
        };
        _loadAnalysisButton.Click += async (_, _) => await ImportPckFromDialogAsync();
        toolbar.Controls.Add(_loadAnalysisButton);

        _openConflictsButton = new Button
        {
            Text = Strings.Button_OpenConflicts,
            AutoSize = true,
            Enabled = false
        };
        _openConflictsButton.Click += (_, _) => OpenConflictWindow();
        toolbar.Controls.Add(_openConflictsButton);

        _openBuildModButton = new Button
        {
            Text = Strings.Button_BuildMod,
            AutoSize = true,
            Enabled = false
        };
        _openBuildModButton.Click += (_, _) => OpenBuildModWindow();
        toolbar.Controls.Add(_openBuildModButton);

        _filterComboBox = new ComboBox
        {
            Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _filterComboBox.Items.AddRange([
            Strings.Filter_All,
            Strings.Filter_Included,
            Strings.Filter_Conflict,
            Strings.Filter_Unmatched,
            Strings.Filter_Discarded
        ]);
        _filterComboBox.SelectedIndex = 0;
        _filterComboBox.SelectedIndexChanged += HandleFilterChanged;
        toolbar.Controls.Add(_filterComboBox);

        _searchTextBox = new TextBox
        {
            Width = 260,
            PlaceholderText = Strings.Placeholder_SearchAssets
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

        _discardCheckBox = new CheckBox
        {
            Text = Strings.Checkbox_Discard,
            AutoSize = true
        };
        _discardCheckBox.CheckedChanged += (_, _) => ApplyDiscardState();
        statePanel.Controls.Add(_discardCheckBox);

        FlowLayoutPanel cardPanel = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            WrapContents = false
        };
        detailsContentLayout.Controls.Add(cardPanel, 0, 4);

        _groupLabel = new Label
        {
            Text = Strings.Label_Group,
            AutoSize = true,
            Padding = new Padding(0, 8, 8, 0)
        };
        cardPanel.Controls.Add(_groupLabel);

        _groupComboBox = new ComboBox
        {
            Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _groupComboBox.SelectedIndexChanged += (_, _) => ApplyGroupFilter();
        cardPanel.Controls.Add(_groupComboBox);

        _manualCardLabel = new Label
        {
            Text = Strings.Label_Card,
            AutoSize = true,
            Padding = new Padding(12, 8, 8, 0)
        };
        cardPanel.Controls.Add(_manualCardLabel);

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
            Text = Strings.Button_UpdateMapping,
            AutoSize = true,
            Enabled = false,
            Margin = new Padding(12, 3, 3, 3)
        };
        _updateMappingButton.Click += (_, _) => ApplyManualCardSelection();
        cardPanel.Controls.Add(_updateMappingButton);

        FlowLayoutPanel advancedPanel = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill
        };
        detailsContentLayout.Controls.Add(advancedPanel, 0, 5);

        _advancedSettingsButton = new Button
        {
            Text = Strings.Button_AdvancedSettings,
            AutoSize = true,
            Enabled = false,
            Margin = new Padding(0, 3, 3, 3)
        };
        _advancedSettingsButton.Click += (_, _) => OpenAdvancedSettings();
        advancedPanel.Controls.Add(_advancedSettingsButton);

        _helpLabel = CreateInfoLabel();
        _helpLabel.Text = Strings.Help_MappingReview;
        detailsContentLayout.Controls.Add(_helpLabel, 0, 6);

        _analysisPathLabel.Text = Strings.Help_ImportToBegin;
        _importStatusLabel.Text = string.Format(Strings.Info_GdreCache, AppPaths.GdreToolsPath, AppPaths.CacheRoot);

        _menuFileOpen = new ToolStripMenuItem(Strings.Menu_File_Open)
        {
            ShortcutKeys = Keys.Control | Keys.O
        };
        _menuFileOpen.Click += async (_, _) => await ImportPckFromDialogAsync();

        _menuFileExit = new ToolStripMenuItem(Strings.Menu_File_Exit);
        _menuFileExit.Click += (_, _) => Close();

        _menuFile = new ToolStripMenuItem(Strings.Menu_File);
        _menuFile.DropDownItems.Add(_menuFileOpen);
        _menuFile.DropDownItems.Add(new ToolStripSeparator());
        _menuFile.DropDownItems.Add(_menuFileExit);

        _menuBuildMod = new ToolStripMenuItem(Strings.Menu_Build_BuildMod);
        _menuBuildMod.Click += (_, _) => OpenBuildModWindow();

        _menuBuild = new ToolStripMenuItem(Strings.Menu_Build);
        _menuBuild.DropDownItems.Add(_menuBuildMod);

        _menuLangEnglish = new ToolStripMenuItem("English");
        _menuLangEnglish.Click += (_, _) => LocalizationManager.SetLanguage(LocalizationManager.English);

        _menuLangChinese = new ToolStripMenuItem("简体中文");
        _menuLangChinese.Click += (_, _) => LocalizationManager.SetLanguage(LocalizationManager.Chinese);

        _menuViewLanguage = new ToolStripMenuItem(Strings.Menu_View_Language);
        _menuViewLanguage.DropDownItems.Add(_menuLangEnglish);
        _menuViewLanguage.DropDownItems.Add(_menuLangChinese);

        _menuView = new ToolStripMenuItem(Strings.Menu_View);
        _menuView.DropDownItems.Add(_menuViewLanguage);

        _menuStrip = new MenuStrip
        {
            Dock = DockStyle.Top
        };
        _menuStrip.Items.Add(_menuFile);
        _menuStrip.Items.Add(_menuBuild);
        _menuStrip.Items.Add(_menuView);
        MainMenuStrip = _menuStrip;
        Controls.Add(_menuStrip);

        UpdateLanguageMenuChecks();

        LocalizationManager.LanguageChanged += HandleLanguageChanged;
        FormClosed += (_, _) => LocalizationManager.LanguageChanged -= HandleLanguageChanged;

        EnablePckDragDrop(rootLayout);
    }

    private void HandleLanguageChanged()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(ApplyLocalization));
            return;
        }

        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        Text = Strings.MainForm_Title;

        _loadAnalysisButton.Text = Strings.Button_ImportPck;
        _openConflictsButton.Text = Strings.Button_OpenConflicts;
        _openBuildModButton.Text = Strings.Button_BuildMod;
        _updateMappingButton.Text = Strings.Button_UpdateMapping;
        _advancedSettingsButton.Text = Strings.Button_AdvancedSettings;

        _searchTextBox.PlaceholderText = Strings.Placeholder_SearchAssets;

        _discardCheckBox.Text = Strings.Checkbox_Discard;
        _groupLabel.Text = Strings.Label_Group;
        _manualCardLabel.Text = Strings.Label_Card;
        _helpLabel.Text = Strings.Help_MappingReview;

        _menuFile.Text = Strings.Menu_File;
        _menuFileOpen.Text = Strings.Menu_File_Open;
        _menuFileExit.Text = Strings.Menu_File_Exit;
        _menuBuild.Text = Strings.Menu_Build;
        _menuBuildMod.Text = Strings.Menu_Build_BuildMod;
        _menuView.Text = Strings.Menu_View;
        _menuViewLanguage.Text = Strings.Menu_View_Language;

        UpdateLanguageMenuChecks();

        int filterIndex = _filterComboBox.SelectedIndex;
        _filterComboBox.SelectedIndexChanged -= HandleFilterChanged;
        _filterComboBox.Items.Clear();
        _filterComboBox.Items.AddRange([
            Strings.Filter_All,
            Strings.Filter_Included,
            Strings.Filter_Conflict,
            Strings.Filter_Unmatched,
            Strings.Filter_Discarded
        ]);
        _filterComboBox.SelectedIndex = filterIndex < 0 ? 0 : filterIndex;
        _filterComboBox.SelectedIndexChanged += HandleFilterChanged;

        if (_session is null)
        {
            _analysisPathLabel.Text = Strings.Help_ImportToBegin;
        }
        else if (!string.IsNullOrWhiteSpace(_analysisPath))
        {
            _analysisPathLabel.Text = string.Format(Strings.Info_SessionPath, _analysisPath);
        }

        _importStatusLabel.Text = string.Format(Strings.Info_GdreCache, AppPaths.GdreToolsPath, AppPaths.CacheRoot);

        RefreshAssetList();
        _assetListBox.Invalidate();
    }

    private void UpdateLanguageMenuChecks()
    {
        string current = LocalizationManager.CurrentCulture.Name;
        _menuLangEnglish.Checked = string.Equals(current, LocalizationManager.English.Name, StringComparison.OrdinalIgnoreCase);
        _menuLangChinese.Checked = string.Equals(current, LocalizationManager.Chinese.Name, StringComparison.OrdinalIgnoreCase);
    }

    private void HandleFilterChanged(object? sender, EventArgs e)
    {
        RefreshAssetList();
    }

    private void OpenAdvancedSettings()
    {
        if (_assetListBox.SelectedItem is not MergedMappingCandidate candidate)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(candidate.MatchedCardId))
        {
            return;
        }

        string cardDisplayName = candidate.CanonicalName ?? candidate.MatchedCardId!;
        using AdvancedSettingsForm dialog = new(cardDisplayName, candidate.AdvancedFields);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        Dictionary<string, string> values = dialog.GetResult();
        candidate.AdvancedFields = values.Count == 0 ? null : values;
        SynchronizeSession();
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

    private async Task ImportPckFromDialogAsync()
    {
        using OpenFileDialog dialog = new()
        {
            Title = Strings.Dialog_ImportPckFilePicker_Title,
            Filter = Strings.Dialog_PckFileFilter,
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
                string.Format(Strings.Error_GdreNotFound, gdreToolsPath),
                Strings.Dialog_ImportPck_Title,
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
            SetImportBusy(true, string.Format(Strings.Status_ImportingPcks, normalizedPckPaths.Length, sessionRoot));

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
                        progress.Report(string.Format(Strings.Status_SkippingAlreadyImported, Path.GetFileName(sourcePckPath)));
                        continue;
                    }

                    string packageId = CreatePackageId(nextImportOrder, sourcePckPath);
                    string packageRoot = Path.Combine(sessionRoot, "imports", packageId);
                    string recoverRoot = Path.Combine(packageRoot, "recover");
                    string scanJsonPath = Path.Combine(packageRoot, "asset_scan_result.json");
                    string mappingJsonPath = Path.Combine(packageRoot, "mapping_analysis_result.json");

                    progress.Report(string.Format(Strings.Status_Recovering, Path.GetFileName(sourcePckPath)));
                    GdrePckImporter importer = new();
                    importer.Import(new PckImportRequest
                    {
                        SourcePckPath = sourcePckPath,
                        OutputDirectory = recoverRoot,
                        GdreToolsPath = gdreToolsPath,
                        OverwriteOutput = true
                    });

                    progress.Report(string.Format(Strings.Status_Scanning, Path.GetFileName(sourcePckPath)));
                    AssetScanner scanner = new();
                    scanner.Scan(new AssetScanRequest
                    {
                        InputDirectory = recoverRoot,
                        OutputJsonPath = scanJsonPath
                    });

                    progress.Report(string.Format(Strings.Status_AnalyzingMappings, Path.GetFileName(sourcePckPath)));
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

                progress.Report(Strings.Status_Merging);
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

            _importStatusLabel.Text = string.Format(Strings.Status_ImportedCount, normalizedPckPaths.Length, sessionRoot);

            if (string.IsNullOrWhiteSpace(_buildModDraft.ModId) || string.Equals(_buildModDraft.ModId, "GeneratedPortraitMod", StringComparison.Ordinal))
            {
                _buildModDraft.ModId = suggestedModId;
                _buildModDraft.ModName = suggestedModId;
                if (_buildModForm is not null && !_buildModForm.IsDisposed)
                {
                    _buildModForm.ApplySuggestedModId(suggestedModId);
                }
            }

            if (_session is not null &&
                _session.Packages.Count > 1 &&
                _session.ConflictGroups.Count > 0)
            {
                OpenConflictWindow();
            }
        }
        catch (Exception ex)
        {
            _importStatusLabel.Text = string.Format(Strings.Status_ImportFailed, ex.Message);
            MessageBox.Show(this, ex.Message, Strings.Dialog_ImportPckFailed_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetImportBusy(false);
            TogglePrimaryActions(enabled: true);
            UseWaitCursor = false;
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
        _analysisPathLabel.Text = string.Format(Strings.Info_SessionPath, sessionPath);
        BindPackageList();
        RefreshAssetList();
        if (_conflictReviewForm is not null && !_conflictReviewForm.IsDisposed)
        {
            _conflictReviewForm.SetSession(session);
        }
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
        _openConflictsButton.Enabled = enabled && _session is not null && _session.ConflictGroups.Count > 0;
        _openBuildModButton.Enabled = enabled && _session is not null;
        _updateMappingButton.Enabled = enabled && _assetListBox.SelectedItem is MergedMappingCandidate && _cardComboBox.SelectedItem is CardChoice;
        _advancedSettingsButton.Enabled = enabled
            && _assetListBox.SelectedItem is MergedMappingCandidate selected
            && !string.IsNullOrWhiteSpace(selected.MatchedCardId);
    }

    private void SetImportBusy(bool busy, string? statusText = null)
    {
        _importProgressBar.Visible = busy;
        if (statusText is not null)
        {
            _importStatusLabel.Text = statusText;
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
        filtered = _filterComboBox.SelectedIndex switch
        {
            1 => filtered.Where(candidate => string.Equals(GetCandidateStatusName(candidate), "Included", StringComparison.Ordinal)),
            2 => filtered.Where(candidate => string.Equals(GetCandidateStatusName(candidate), "Conflict", StringComparison.Ordinal)),
            3 => filtered.Where(candidate => !candidate.Ignored && string.IsNullOrWhiteSpace(candidate.MatchedCardId)),
            4 => filtered.Where(candidate => string.Equals(GetCandidateStatusName(candidate), "Discarded", StringComparison.Ordinal)),
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

        int includedCount = _session.Candidates.Count(candidate => string.Equals(GetCandidateStatusName(candidate), "Included", StringComparison.Ordinal));
        int conflictCount = _session.Candidates.Count(candidate => string.Equals(GetCandidateStatusName(candidate), "Conflict", StringComparison.Ordinal));
        int unmatchedCount = _session.Candidates.Count(candidate => string.Equals(GetCandidateStatusName(candidate), "Unmatched", StringComparison.Ordinal));
        int discardedCount = _session.Candidates.Count(candidate => string.Equals(GetCandidateStatusName(candidate), "Discarded", StringComparison.Ordinal));
        int pendingConflictGroups = _session.ConflictGroups.Count(group => string.Equals(group.ResolutionState, "Pending", StringComparison.OrdinalIgnoreCase));

        _summaryLabel.Text = string.Format(
            Strings.Info_MainSummary,
            _session.Packages.Count,
            items.Count,
            includedCount,
            conflictCount,
            unmatchedCount,
            discardedCount,
            pendingConflictGroups);

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
        string candidateState = GetCandidateStatusName(candidate);

        Color prefixColor = GetCandidateStatusColor(candidate);
        Color remainderColor = (eventArgs.State & DrawItemState.Selected) == DrawItemState.Selected
            ? SystemColors.HighlightText
            : eventArgs.ForeColor;

        Rectangle bounds = eventArgs.Bounds;
        Rectangle contentBounds = Rectangle.Inflate(bounds, -4, 0);

        int statusWidth = Math.Max(
            118,
            TextRenderer.MeasureText(
                eventArgs.Graphics,
                GetCandidateStatusPrefix(candidate),
                badgeFont,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding).Width + 8);
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
            string.Equals(candidateState, "Conflict", StringComparison.Ordinal)
                ? string.Format(Strings.Info_ConflictCandidateCount, GetConflictCandidateCount(candidate))
                : $"{candidate.SourcePackageName} / {candidate.FileName}",
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

    private void DrawTargetCell(Graphics graphics, Font font, Font badgeFont, Rectangle targetRect, MergedMappingCandidate candidate, Color defaultTextColor)
    {
        string candidateState = GetCandidateStatusName(candidate);
        if (string.Equals(candidateState, "Discarded", StringComparison.Ordinal))
        {
            DrawBadge(graphics, targetRect, Strings.Status_Discarded, Color.DimGray, badgeFont);
            return;
        }

        if (string.Equals(candidateState, "Unmatched", StringComparison.Ordinal))
        {
            DrawBadge(graphics, targetRect, Strings.Status_Unmatched, Color.IndianRed, badgeFont);
            return;
        }

        string targetText = candidateState switch
        {
            "Conflict" => GetDisplayCardName(candidate),
            _ => GetDisplayCardName(candidate)
        };

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

    private string GetCandidateStatusPrefix(MergedMappingCandidate candidate)
    {
        return GetCandidateStatusName(candidate) switch
        {
            "Discarded" => Strings.Badge_Discarded,
            "Unmatched" => Strings.Badge_Unmatched,
            "Conflict" => Strings.Badge_Conflict,
            _ => Strings.Badge_Included
        };
    }

    private string GetCandidateStatusName(MergedMappingCandidate candidate)
    {
        if (candidate.Ignored)
        {
            return "Discarded";
        }

        if (string.IsNullOrWhiteSpace(candidate.MatchedCardId))
        {
            return "Unmatched";
        }

        if (!candidate.IsConflict)
        {
            return "Included";
        }

        CardConflictGroup? group = _session?.ConflictGroups.FirstOrDefault(conflictGroup =>
            string.Equals(conflictGroup.CardId, candidate.MatchedCardId, StringComparison.OrdinalIgnoreCase));
        if (group is null || string.Equals(group.ResolutionState, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            return "Conflict";
        }

        if (string.Equals(group.ResolutionState, "Discarded", StringComparison.OrdinalIgnoreCase))
        {
            return "Discarded";
        }

        return candidate.Selected ? "Included" : "Discarded";
    }

    private Color GetCandidateStatusColor(MergedMappingCandidate candidate)
    {
        return GetCandidateStatusName(candidate) switch
        {
            "Discarded" => Color.DimGray,
            "Unmatched" => Color.Firebrick,
            "Conflict" => Color.DarkOrange,
            _ => Color.ForestGreen
        };
    }

    private int GetConflictCandidateCount(MergedMappingCandidate candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate.MatchedCardId))
        {
            return 0;
        }

        return _session?.ConflictGroups.FirstOrDefault(group =>
            string.Equals(group.CardId, candidate.MatchedCardId, StringComparison.OrdinalIgnoreCase))?.CandidateIds.Count ?? 0;
    }

    private static string GetDisplayCardName(MergedMappingCandidate candidate)
    {
        return candidate.CanonicalName
               ?? candidate.MatchedCardId
               ?? Strings.Text_Unknown;
    }

    internal static string LocalizeStatusName(string canonical) => canonical switch
    {
        "Included" => Strings.Status_Included,
        "Discarded" => Strings.Status_Discarded,
        "Unmatched" => Strings.Status_Unmatched,
        "Conflict" => Strings.Status_Conflict,
        "Pending" => Strings.Status_Pending,
        "Resolved" => Strings.Status_Resolved,
        _ => canonical
    };

    internal static string? LocalizeReason(string? reason) => reason switch
    {
        null => null,
        "Discarded during manual review." => Strings.Reason_DiscardedManual,
        "Manually assigned in GUI." => Strings.Reason_ManuallyAssigned,
        "Discarded in conflicts review." => Strings.Reason_DiscardedInConflict,
        _ => reason
    };

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
            _discardCheckBox.Enabled = candidate is not null;
            _groupComboBox.Enabled = candidate is not null;
            _cardComboBox.Enabled = candidate is not null;
            _updateMappingButton.Enabled = false;
            _advancedSettingsButton.Enabled = candidate is not null && !string.IsNullOrWhiteSpace(candidate.MatchedCardId);

            if (candidate is null)
            {
                _previewBox.Image = null;
                _statusLabel.Text = string.Empty;
                _pathLabel.Text = string.Empty;
                _reasonLabel.Text = string.Empty;
                _discardCheckBox.Checked = false;
                _discardCheckBox.Enabled = false;
                _groupComboBox.SelectedIndex = -1;
                _cardComboBox.SelectedIndex = -1;
                _cardComboBox.Text = string.Empty;
                return;
            }

            string candidateStatus = LocalizeStatusName(GetCandidateStatusName(candidate));
            _statusLabel.Text = string.Format(
                Strings.Info_CandidateStatus,
                candidateStatus,
                candidate.CanonicalName ?? Strings.Text_None,
                candidate.Group ?? Strings.Text_None,
                candidate.SourcePackageName);
            _pathLabel.Text = string.Format(Strings.Info_CandidatePath, candidate.SourceRelativePath);
            _reasonLabel.Text = string.Format(
                Strings.Info_CandidateReason,
                LocalizeReason(candidate.MatchReason ?? candidate.IgnoredReason) ?? Strings.Text_None);
            _discardCheckBox.Checked = candidate.Ignored;
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

    private void ApplyDiscardState()
    {
        if (_suppressEvents)
        {
            return;
        }

        if (_assetListBox.SelectedItem is not MergedMappingCandidate candidate)
        {
            return;
        }

        candidate.Ignored = _discardCheckBox.Checked;
        if (candidate.Ignored)
        {
            candidate.Selected = false;
            candidate.IgnoredReason ??= "Discarded during manual review.";
        }
        else if (candidate.IgnoredReason == "Discarded during manual review.")
        {
            candidate.IgnoredReason = null;
            if (!candidate.IsConflict && !string.IsNullOrWhiteSpace(candidate.MatchedCardId))
            {
                candidate.Selected = true;
            }
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
        candidate.Ignored = false;
        candidate.IgnoredReason = null;
        candidate.Confidence = 1.0;
        candidate.MatchReason = "Manually assigned in GUI.";
        bool createsConflict = _session is not null && _session.Candidates.Any(other =>
            !string.Equals(other.CandidateId, candidate.CandidateId, StringComparison.OrdinalIgnoreCase) &&
            !other.Ignored &&
            string.Equals(other.MatchedCardId, candidate.MatchedCardId, StringComparison.OrdinalIgnoreCase));
        candidate.Selected = !createsConflict;
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

    private void SynchronizeSession()
    {
        if (_session is null)
        {
            return;
        }

        _conflictResolutionService.Refresh(_session);
        BindPackageList();
        _openConflictsButton.Enabled = _session.ConflictGroups.Count > 0;
        _openBuildModButton.Enabled = true;
        if (_conflictReviewForm is not null && !_conflictReviewForm.IsDisposed)
        {
            _conflictReviewForm.SetSession(_session);
        }
    }

    private void OpenConflictWindow()
    {
        if (_session is null)
        {
            MessageBox.Show(this, Strings.Error_ImportFirst, Strings.Dialog_Conflicts_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        SynchronizeSession();
        if (_conflictReviewForm is not null && !_conflictReviewForm.IsDisposed)
        {
            _conflictReviewForm.Focus();
            return;
        }

        _conflictReviewForm = new ConflictReviewForm(_session, HandleConflictSessionChanged);
        _conflictReviewForm.FormClosed += (_, _) => _conflictReviewForm = null;
        _conflictReviewForm.Show(this);
    }

    private void OpenBuildModWindow()
    {
        if (_session is null)
        {
            MessageBox.Show(this, Strings.Error_ImportAndReviewFirst, Strings.Dialog_BuildMod_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_buildModForm is not null && !_buildModForm.IsDisposed)
        {
            _buildModForm.Focus();
            return;
        }

        _buildModForm = new BuildModForm(
            sessionAccessor: () => _session,
            officialCardIndexPathAccessor: () => _officialCardIndexPath,
            sessionRootAccessor: ResolveSessionRoot,
            synchronizeSession: SynchronizeSession,
            draft: _buildModDraft);
        _buildModForm.FormClosed += (_, _) => _buildModForm = null;
        _buildModForm.ApplySuggestedModId(_buildModDraft.ModId);
        _buildModForm.Show(this);
    }

    private void HandleConflictSessionChanged()
    {
        if (_session is null)
        {
            return;
        }

        SynchronizeSession();
        RefreshCurrentBindings();
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
