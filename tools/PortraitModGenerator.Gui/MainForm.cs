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
    private readonly Button _saveAnalysisButton;
    private readonly Label _analysisPathLabel;
    private readonly ComboBox _filterComboBox;
    private readonly TextBox _searchTextBox;
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
    private readonly Button _generateModButton;
    private readonly Label _generationStatusLabel;
    private readonly Panel _detailsPanel;

    private string? _analysisPath;
    private string? _officialCardIndexPath;
    private ReviewSession? _session;
    private List<CardChoice> _allCardChoices = [];
    private bool _suppressEvents;

    public MainForm()
    {
        Text = "Portrait Mod Generator - Mapping Review";
        Width = 1590;
        Height = 1070;
        StartPosition = FormStartPosition.CenterScreen;

        TableLayoutPanel rootLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
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
            Text = "Load Analysis",
            AutoSize = true
        };
        _loadAnalysisButton.Click += (_, _) => LoadAnalysisFromDialog();
        toolbar.Controls.Add(_loadAnalysisButton);

        _saveAnalysisButton = new Button
        {
            Text = "Save Review As",
            AutoSize = true,
            Enabled = false
        };
        _saveAnalysisButton.Click += (_, _) => SaveAnalysisAs();
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

        SplitContainer contentSplit = new()
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 700
        };
        rootLayout.Controls.Add(contentSplit, 0, 2);
        Shown += (_, _) =>
        {
            int availableWidth = contentSplit.ClientSize.Width;
            if (availableWidth > 0)
            {
                contentSplit.SplitterDistance = availableWidth / 2;
            }
        };

        _assetListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            HorizontalScrollbar = true
        };
        _assetListBox.SelectedIndexChanged += (_, _) => BindSelectedItem();
        contentSplit.Panel1.Controls.Add(_assetListBox);

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
        _cardComboBox.SelectedIndexChanged += (_, _) => ApplyManualCardSelection();
        _cardComboBox.TextUpdate += (_, _) => { };
        cardPanel.Controls.Add(_cardComboBox);

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

        generationLayout.Controls.Add(CreateFieldLabel("Output Dir:"), 0, 4);
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
            Text = "Generate Mod Project",
            AutoSize = true
        };
        _generateModButton.Click += (_, _) => GenerateModProject();
        generatePanel.Controls.Add(_generateModButton);

        _generationStatusLabel = CreateInfoLabel();
        _generationStatusLabel.Text = "Load an analysis, review it, then generate a mod project here.";
        generatePanel.Controls.Add(_generationStatusLabel);

        _analysisPathLabel.Text = "Load a mapping analysis JSON to begin review.";
        _authorTextBox.Text = "Unknown Author";
        _descriptionTextBox.Text = "Generated portrait replacement mod";
        _outputDirectoryTextBox.Text = Path.Combine(Environment.CurrentDirectory, "generated");
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

    private void LoadAnalysisFromDialog()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Open mapping analysis result",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            LoadAnalysis(dialog.FileName);
        }
    }

    private void LoadAnalysis(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string json = File.ReadAllText(fullPath);
        ReviewSession? session = JsonSerializer.Deserialize<ReviewSession>(json, JsonOptions);
        if (session is null)
        {
            MessageBox.Show(this, "Failed to deserialize mapping analysis file.", "Load failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _analysisPath = fullPath;
        _officialCardIndexPath = ResolveOfficialCardIndexPath(fullPath, session.OfficialCardIndexPath);
        LoadOfficialCards(_officialCardIndexPath);

        _session = session;
        _analysisPathLabel.Text = $"Analysis: {fullPath}";
        _saveAnalysisButton.Enabled = true;
        if (string.IsNullOrWhiteSpace(_modIdTextBox.Text))
        {
            _modIdTextBox.Text = "GeneratedPortraitMod";
        }

        if (string.IsNullOrWhiteSpace(_modNameTextBox.Text))
        {
            _modNameTextBox.Text = "Generated Portrait Mod";
        }

        RefreshAssetList();
    }

    private void LoadOfficialCards(string officialCardIndexPath)
    {
        OfficialCardIndex index = new OfficialCardIndexLoader().Load(officialCardIndexPath);
        _allCardChoices = index.Cards
            .OrderBy(card => card.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(card => card.CanonicalName, StringComparer.OrdinalIgnoreCase)
            .Select(card => new CardChoice(card))
            .ToList();

        List<string> groups = ["All"];
        groups.AddRange(index.Cards
            .Select(card => card.Group)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group, StringComparer.OrdinalIgnoreCase));

        _suppressEvents = true;
        _groupComboBox.DataSource = groups;
        _groupComboBox.SelectedIndex = 0;
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

        IEnumerable<ReviewCandidate> filtered = _session.Candidates;
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
                candidate.RelativePath.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (candidate.MatchedCardId?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (candidate.CanonicalName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        List<ReviewCandidate> items = filtered.ToList();
        _suppressEvents = true;
        _assetListBox.DataSource = items;
        _assetListBox.DisplayMember = nameof(ReviewCandidate.ListDisplayText);
        _suppressEvents = false;

        _summaryLabel.Text = $"Visible {items.Count} | Matched {_session.Candidates.Count(candidate => !string.IsNullOrWhiteSpace(candidate.MatchedCardId))} | Unmatched {_session.Candidates.Count(candidate => !candidate.Ignored && string.IsNullOrWhiteSpace(candidate.MatchedCardId))} | Ignored {_session.Candidates.Count(candidate => candidate.Ignored)}";

        if (items.Count > 0)
        {
            _assetListBox.SelectedIndex = 0;
        }
        else
        {
            BindSelectedItem();
        }
    }

    private void BindSelectedItem()
    {
        if (_suppressEvents)
        {
            return;
        }

        ReviewCandidate? candidate = _assetListBox.SelectedItem as ReviewCandidate;
        _suppressEvents = true;

        try
        {
            _selectedCheckBox.Enabled = candidate is not null;
            _ignoredCheckBox.Enabled = candidate is not null;
            _groupComboBox.Enabled = candidate is not null;
            _cardComboBox.Enabled = candidate is not null;

            if (candidate is null)
            {
                _previewBox.Image = null;
                _statusLabel.Text = string.Empty;
                _pathLabel.Text = string.Empty;
                _reasonLabel.Text = string.Empty;
                _selectedCheckBox.Checked = false;
                _ignoredCheckBox.Checked = false;
                _groupComboBox.SelectedIndex = 0;
                _cardComboBox.SelectedIndex = -1;
                _cardComboBox.Text = string.Empty;
                return;
            }

            _selectedCheckBox.Checked = candidate.Selected;
            _ignoredCheckBox.Checked = candidate.Ignored;
            _statusLabel.Text = $"Status: {(candidate.Ignored ? "Ignored" : string.IsNullOrWhiteSpace(candidate.MatchedCardId) ? "Unmatched" : "Matched")} | Card: {candidate.CanonicalName ?? "(none)"} | Group: {candidate.Group ?? "(none)"}";
            _pathLabel.Text = $"Path: {candidate.RelativePath}";
            _reasonLabel.Text = $"Reason: {candidate.MatchReason ?? candidate.IgnoredReason ?? "(none)"}";
            BindCardSelection(candidate);
            LoadPreview(candidate.SourceAbsolutePath);
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    private void BindCardSelection(ReviewCandidate candidate)
    {
        string targetGroup = candidate.Group ?? "All";
        if (string.IsNullOrWhiteSpace(candidate.MatchedCardId))
        {
            targetGroup = "All";
        }

        int groupIndex = _groupComboBox.FindStringExact(targetGroup);
        _groupComboBox.SelectedIndex = groupIndex >= 0 ? groupIndex : 0;
        ApplyGroupFilterCore(keepText: true);

        if (_cardComboBox.DataSource is not List<CardChoice> choices)
        {
            _cardComboBox.SelectedIndex = -1;
            _cardComboBox.Text = string.Empty;
            return;
        }

        CardChoice? selectedChoice = choices.FirstOrDefault(choice =>
            string.Equals(choice.Card.CardId, candidate.MatchedCardId, StringComparison.OrdinalIgnoreCase));

        if (selectedChoice is null)
        {
            _cardComboBox.SelectedIndex = -1;
            _cardComboBox.Text = candidate.CanonicalName ?? string.Empty;
            return;
        }

        _cardComboBox.SelectedItem = selectedChoice;
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

        if (_assetListBox.SelectedItem is not ReviewCandidate candidate)
        {
            return;
        }

        candidate.Selected = _selectedCheckBox.Checked;
        if (candidate.Selected)
        {
            candidate.Ignored = false;
            candidate.IgnoredReason = null;
            _ignoredCheckBox.Checked = false;
        }

        RefreshCurrentBindings();
    }

    private void ApplyIgnoredState()
    {
        if (_suppressEvents)
        {
            return;
        }

        if (_assetListBox.SelectedItem is not ReviewCandidate candidate)
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

        RefreshCurrentBindings();
    }

    private void ApplyManualCardSelection()
    {
        if (_suppressEvents)
        {
            return;
        }

        if (_assetListBox.SelectedItem is not ReviewCandidate candidate)
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
        string selectedGroup = _groupComboBox.SelectedItem?.ToString() ?? "All";
        List<CardChoice> filtered = string.Equals(selectedGroup, "All", StringComparison.OrdinalIgnoreCase)
            ? _allCardChoices.ToList()
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
        ReviewCandidate? current = _assetListBox.SelectedItem as ReviewCandidate;
        RefreshAssetList();
        if (current is null)
        {
            return;
        }

        if (_assetListBox.DataSource is List<ReviewCandidate> items)
        {
            ReviewCandidate? updated = items.FirstOrDefault(item =>
                string.Equals(item.SourceAbsolutePath, current.SourceAbsolutePath, StringComparison.OrdinalIgnoreCase));
            if (updated is not null)
            {
                _assetListBox.SelectedItem = updated;
            }
        }
    }

    private void SaveAnalysisAs()
    {
        if (_session is null || string.IsNullOrWhiteSpace(_analysisPath))
        {
            return;
        }

        using SaveFileDialog dialog = new()
        {
            Title = "Save reviewed mapping analysis",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = Path.GetFileName(_analysisPath)
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        ReviewSession output = _session with
        {
            OutputJsonPath = dialog.FileName,
            MatchedAssets = _session.Candidates.Count(candidate => !string.IsNullOrWhiteSpace(candidate.MatchedCardId)),
            IgnoredAssets = _session.Candidates.Count(candidate => candidate.Ignored),
            Candidates = _session.Candidates
        };

        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(output, JsonOptions));
        MessageBox.Show(this, $"Saved review file to:\n{dialog.FileName}", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

    private void GenerateModProject()
    {
        if (_session is null || string.IsNullOrWhiteSpace(_officialCardIndexPath))
        {
            MessageBox.Show(this, "Load and review a mapping analysis first.", "Generate mod", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string modId = _modIdTextBox.Text.Trim();
        string modName = _modNameTextBox.Text.Trim();
        string author = _authorTextBox.Text.Trim();
        string description = _descriptionTextBox.Text.Trim();
        string outputParent = _outputDirectoryTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(modId))
        {
            MessageBox.Show(this, "Mod ID is required.", "Generate mod", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(outputParent))
        {
            MessageBox.Show(this, "Output directory is required.", "Generate mod", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string templateDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "templates", "PortraitReplacementTemplate"));
        string generatedRoot = Path.Combine(Path.GetFullPath(outputParent), modId);
        string reviewPath = Path.Combine(generatedRoot, $"{modId}.mapping_review.json");

        try
        {
            Directory.CreateDirectory(Path.GetFullPath(outputParent));

            TemplateProjectGenerator templateGenerator = new();
            templateGenerator.Generate(new TemplateGenerationRequest
            {
                TemplateDirectory = templateDirectory,
                OutputDirectory = generatedRoot,
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

            ReviewSession review = _session with
            {
                OfficialCardIndexPath = _officialCardIndexPath,
                OutputJsonPath = reviewPath,
                MatchedAssets = _session.Candidates.Count(candidate => !string.IsNullOrWhiteSpace(candidate.MatchedCardId)),
                IgnoredAssets = _session.Candidates.Count(candidate => candidate.Ignored),
                Candidates = _session.Candidates
            };
            File.WriteAllText(reviewPath, JsonSerializer.Serialize(review, JsonOptions));

            MappingMaterializer materializer = new();
            MaterializeMappingsResult result = materializer.Materialize(new MaterializeMappingsRequest
            {
                MappingAnalysisPath = reviewPath,
                ModProjectRoot = generatedRoot,
                ModId = modId
            });

            _generationStatusLabel.Text = $"Generated {result.EntriesWritten} entries at {generatedRoot}";
            MessageBox.Show(
                this,
                $"Generated mod project:\n{generatedRoot}\n\nConfig:\n{result.ConfigPath}",
                "Generate mod",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _generationStatusLabel.Text = $"Generation failed: {ex.Message}";
            MessageBox.Show(this, ex.Message, "Generate mod failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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

    private sealed record ReviewSession
    {
        public required string ScanResultPath { get; init; }

        public required string OfficialCardIndexPath { get; init; }

        public required string OutputJsonPath { get; init; }

        public required int TotalAssets { get; init; }

        public required int MatchedAssets { get; init; }

        public required int IgnoredAssets { get; init; }

        public required List<ReviewCandidate> Candidates { get; init; }
    }

    private sealed class ReviewCandidate
    {
        public required string SourceAbsolutePath { get; set; }

        public required string RelativePath { get; set; }

        public required string FileName { get; set; }

        public required bool Selected { get; set; }

        public required bool Ignored { get; set; }

        public string? IgnoredReason { get; set; }

        public string? MatchedCardId { get; set; }

        public string? CanonicalName { get; set; }

        public string? Group { get; set; }

        public double Confidence { get; set; }

        public string? MatchReason { get; set; }

        public string ListDisplayText
        {
            get
            {
                string prefix = Ignored
                    ? "[Ignored]"
                    : string.IsNullOrWhiteSpace(MatchedCardId)
                        ? "[Unmatched]"
                        : Selected
                            ? "[Selected]"
                            : "[Matched]";

                string target = CanonicalName ?? MatchedCardId ?? "(manual review needed)";
                return $"{prefix} {FileName} -> {target}";
            }
        }
    }
}
