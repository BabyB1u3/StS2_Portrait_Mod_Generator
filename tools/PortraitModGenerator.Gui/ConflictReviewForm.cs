using System.Windows.Forms;
using PortraitModGenerator.Core.Abstractions;
using PortraitModGenerator.Core.Services;
using PortraitModGenerator.Gui.Resources;

namespace PortraitModGenerator.Gui;

internal sealed class ConflictReviewForm : Form
{
    private const int PreferredLeftPaneRatioDivisor = 4;
    private const int MinimumLeftPaneWidth = 220;
    private const int MinimumRightPaneWidth = 520;

    private readonly Action _sessionChanged;
    private readonly ConflictResolutionService _conflictResolutionService = new();
    private readonly ComboBox _filterComboBox;
    private readonly Button _nextPendingButton;
    private readonly Label _summaryLabel;
    private readonly ListBox _groupListBox;
    private readonly Label _groupHeaderLabel;
    private readonly Label _groupStateLabel;
    private readonly FlowLayoutPanel _candidatePanel;
    private readonly CheckBox _discardGroupCheckBox;
    private readonly SplitContainer _contentSplit;

    private MergedReviewSession? _session;
    private bool _suppressEvents;

    public ConflictReviewForm(MergedReviewSession session, Action sessionChanged)
    {
        _session = session;
        _sessionChanged = sessionChanged;

        Text = Strings.ConflictReviewForm_Title;
        Width = 1494;
        Height = 910;
        MinimumSize = new Size(1480, 820);
        StartPosition = FormStartPosition.CenterParent;

        TableLayoutPanel rootLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12)
        };
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

        _filterComboBox = new ComboBox
        {
            Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _filterComboBox.Items.AddRange([
            Strings.Filter_All,
            Strings.Filter_Pending,
            Strings.Filter_Resolved,
            Strings.Filter_Discarded
        ]);
        _filterComboBox.SelectedIndex = 0;
        _filterComboBox.SelectedIndexChanged += HandleFilterChanged;
        toolbar.Controls.Add(_filterComboBox);

        _nextPendingButton = new Button
        {
            Text = Strings.Button_NextPending,
            AutoSize = true
        };
        _nextPendingButton.Click += (_, _) => SelectNextPendingConflict();
        toolbar.Controls.Add(_nextPendingButton);

        _summaryLabel = new Label
        {
            AutoSize = true,
            Padding = new Padding(12, 8, 0, 0)
        };
        toolbar.Controls.Add(_summaryLabel);

        _contentSplit = new SplitContainer
        {
            Dock = DockStyle.Fill
        };
        rootLayout.Controls.Add(_contentSplit, 0, 1);
        Shown += (_, _) => ApplyPreferredLayout();
        Resize += (_, _) => ApplyPreferredLayout();

        _groupListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 28,
            HorizontalScrollbar = true
        };
        _groupListBox.DrawItem += DrawConflictGroupItem;
        _groupListBox.SelectedIndexChanged += (_, _) => BindSelectedGroup();
        _contentSplit.Panel1.Controls.Add(_groupListBox);

        TableLayoutPanel detailLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _contentSplit.Panel2.Controls.Add(detailLayout);

        _groupHeaderLabel = new Label
        {
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Padding = new Padding(0, 0, 0, 6)
        };
        detailLayout.Controls.Add(_groupHeaderLabel, 0, 0);

        _groupStateLabel = new Label
        {
            AutoSize = true,
            Padding = new Padding(0, 0, 0, 8)
        };
        detailLayout.Controls.Add(_groupStateLabel, 0, 1);

        Panel candidateScrollHost = new()
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };
        detailLayout.Controls.Add(candidateScrollHost, 0, 2);

        _candidatePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = true
        };
        candidateScrollHost.Controls.Add(_candidatePanel);

        _discardGroupCheckBox = new CheckBox
        {
            Text = Strings.Checkbox_DiscardGroup,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0)
        };
        _discardGroupCheckBox.CheckedChanged += (_, _) => ToggleGroupDiscardedState();
        detailLayout.Controls.Add(_discardGroupCheckBox, 0, 3);

        RefreshConflictGroups(keepSelection: false);

        LocalizationManager.LanguageChanged += HandleLanguageChanged;
        FormClosed += (_, _) => LocalizationManager.LanguageChanged -= HandleLanguageChanged;
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
        Text = Strings.ConflictReviewForm_Title;
        _nextPendingButton.Text = Strings.Button_NextPending;
        _discardGroupCheckBox.Text = Strings.Checkbox_DiscardGroup;

        int filterIndex = _filterComboBox.SelectedIndex;
        _filterComboBox.SelectedIndexChanged -= HandleFilterChanged;
        _filterComboBox.Items.Clear();
        _filterComboBox.Items.AddRange([
            Strings.Filter_All,
            Strings.Filter_Pending,
            Strings.Filter_Resolved,
            Strings.Filter_Discarded
        ]);
        _filterComboBox.SelectedIndex = filterIndex < 0 ? 0 : filterIndex;
        _filterComboBox.SelectedIndexChanged += HandleFilterChanged;

        RefreshConflictGroups(keepSelection: true);
    }

    private void HandleFilterChanged(object? sender, EventArgs e)
    {
        RefreshConflictGroups(keepSelection: false);
    }

    private void ApplyPreferredLayout()
    {
        int totalWidth = _contentSplit.ClientSize.Width;
        if (totalWidth <= 0)
        {
            return;
        }

        int usableWidth = totalWidth - _contentSplit.SplitterWidth;
        if (usableWidth <= 0)
        {
            return;
        }

        int maxLeftWidth = Math.Max(1, usableWidth - MinimumRightPaneWidth);
        int preferredWidth = usableWidth / PreferredLeftPaneRatioDivisor;
        preferredWidth = Math.Max(MinimumLeftPaneWidth, preferredWidth);
        preferredWidth = Math.Min(preferredWidth, maxLeftWidth);
        preferredWidth = Math.Max(1, Math.Min(preferredWidth, usableWidth - 1));

        if (_contentSplit.SplitterDistance != preferredWidth)
        {
            _contentSplit.SplitterDistance = preferredWidth;
        }
    }

    public void SetSession(MergedReviewSession session)
    {
        _session = session;
        RefreshConflictGroups(keepSelection: true);
    }

    private void RefreshConflictGroups(bool keepSelection)
    {
        string? selectedGroupId = keepSelection && _groupListBox.SelectedItem is CardConflictGroup selectedGroup
            ? selectedGroup.CardId
            : null;

        List<CardConflictGroup> groups = GetFilteredGroups();
        _groupListBox.DataSource = null;
        _groupListBox.DataSource = groups;

        _summaryLabel.Text = _session is null
            ? string.Empty
            : string.Format(
                Strings.Info_ConflictSummary,
                _session.ConflictGroups.Count,
                _session.ConflictGroups.Count(group => string.Equals(group.ResolutionState, "Pending", StringComparison.OrdinalIgnoreCase)),
                _session.ConflictGroups.Count(group => string.Equals(group.ResolutionState, "Resolved", StringComparison.OrdinalIgnoreCase)),
                _session.ConflictGroups.Count(group => string.Equals(group.ResolutionState, "Discarded", StringComparison.OrdinalIgnoreCase)));

        if (groups.Count == 0)
        {
            _groupHeaderLabel.Text = Strings.Label_NoConflicts;
            _groupStateLabel.Text = Strings.Help_NoConflicts;
            _discardGroupCheckBox.Checked = false;
            _discardGroupCheckBox.Enabled = false;
            ClearCandidateCards();
            return;
        }

        if (!string.IsNullOrWhiteSpace(selectedGroupId))
        {
            CardConflictGroup? groupToRestore = groups.FirstOrDefault(group =>
                string.Equals(group.CardId, selectedGroupId, StringComparison.OrdinalIgnoreCase));
            if (groupToRestore is not null)
            {
                _groupListBox.SelectedItem = groupToRestore;
                return;
            }
        }

        CardConflictGroup? firstPending = groups.FirstOrDefault(group =>
            string.Equals(group.ResolutionState, "Pending", StringComparison.OrdinalIgnoreCase));
        _groupListBox.SelectedItem = firstPending ?? groups[0];
    }

    private List<CardConflictGroup> GetFilteredGroups()
    {
        if (_session is null)
        {
            return [];
        }

        IEnumerable<CardConflictGroup> groups = _session.ConflictGroups;
        groups = _filterComboBox.SelectedIndex switch
        {
            1 => groups.Where(group => string.Equals(group.ResolutionState, "Pending", StringComparison.OrdinalIgnoreCase)),
            2 => groups.Where(group => string.Equals(group.ResolutionState, "Resolved", StringComparison.OrdinalIgnoreCase)),
            3 => groups.Where(group => string.Equals(group.ResolutionState, "Discarded", StringComparison.OrdinalIgnoreCase)),
            _ => groups
        };

        return groups
            .OrderBy(group => group.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.CanonicalName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void DrawConflictGroupItem(object? sender, DrawItemEventArgs eventArgs)
    {
        eventArgs.DrawBackground();

        if (eventArgs.Index < 0 || eventArgs.Index >= _groupListBox.Items.Count)
        {
            return;
        }

        if (_groupListBox.Items[eventArgs.Index] is not CardConflictGroup group)
        {
            return;
        }

        Font font = eventArgs.Font ?? _groupListBox.Font;
        Color textColor = (eventArgs.State & DrawItemState.Selected) == DrawItemState.Selected
            ? SystemColors.HighlightText
            : eventArgs.ForeColor;
        Color stateColor = GetResolutionColor(group.ResolutionState);

        Rectangle bounds = Rectangle.Inflate(eventArgs.Bounds, -4, 0);
        Rectangle stateRect = new(bounds.Left, bounds.Top, 88, bounds.Height);
        Rectangle nameRect = new(stateRect.Right + 8, bounds.Top, bounds.Width - stateRect.Width - 8, bounds.Height);

        TextRenderer.DrawText(
            eventArgs.Graphics,
            $"[{MainForm.LocalizeStatusName(group.ResolutionState)}]",
            font,
            stateRect,
            stateColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(
            eventArgs.Graphics,
            $"{group.CanonicalName} ({group.CandidateIds.Count})",
            font,
            nameRect,
            textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        eventArgs.DrawFocusRectangle();
    }

    private void BindSelectedGroup()
    {
        if (_groupListBox.SelectedItem is not CardConflictGroup group || _session is null)
        {
            _groupHeaderLabel.Text = Strings.Label_NoConflictSelected;
            _groupStateLabel.Text = string.Empty;
            ClearCandidateCards();
            return;
        }

        _groupHeaderLabel.Text = $"{group.CanonicalName} [{group.Group}]";
        _groupStateLabel.Text = string.Format(
            Strings.Info_GroupDetails,
            group.CardId,
            group.CandidateIds.Count,
            MainForm.LocalizeStatusName(group.ResolutionState));
        _suppressEvents = true;
        _discardGroupCheckBox.Enabled = true;
        _discardGroupCheckBox.Checked = string.Equals(group.ResolutionState, "Discarded", StringComparison.OrdinalIgnoreCase);
        _suppressEvents = false;

        ClearCandidateCards();
        List<MergedMappingCandidate> candidates = _session.Candidates
            .Where(candidate => group.CandidateIds.Contains(candidate.CandidateId, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(candidate => candidate.Selected)
            .ThenBy(candidate => candidate.SourcePackageName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (MergedMappingCandidate candidate in candidates)
        {
            _candidatePanel.Controls.Add(CreateCandidateCard(group, candidate));
        }
    }

    private Control CreateCandidateCard(CardConflictGroup group, MergedMappingCandidate candidate)
    {
        Panel container = new()
        {
            Width = 320,
            Height = 556,
            Margin = new Padding(8)
        };

        Panel card = new()
        {
            Width = 320,
            Height = 520,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0)
        };
        container.Controls.Add(card);

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(8)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 260));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        card.Controls.Add(layout);

        Label statusLabel = new()
        {
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            ForeColor = candidate.Selected ? Color.ForestGreen : candidate.Ignored ? Color.Goldenrod : Color.SteelBlue,
            Text = candidate.Selected
                ? Strings.Status_Included
                : candidate.Ignored
                    ? Strings.Status_Discarded
                    : string.Equals(group.ResolutionState, "Resolved", StringComparison.OrdinalIgnoreCase)
                        ? Strings.Status_Resolved
                        : Strings.Status_Pending
        };
        layout.Controls.Add(statusLabel, 0, 0);

        PictureBox previewBox = new()
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(30, 30, 30)
        };
        previewBox.Image = LoadPreview(candidate.SourceAbsolutePath);
        layout.Controls.Add(previewBox, 0, 1);

        layout.Controls.Add(CreateInfoLabel(string.Format(Strings.Info_CandidatePackage, candidate.SourcePackageName)), 0, 2);
        layout.Controls.Add(CreateInfoLabel(string.Format(Strings.Info_CandidateFile, candidate.FileName)), 0, 3);
        layout.Controls.Add(CreateInfoLabel(string.Format(Strings.Info_CandidateConfidence, candidate.Confidence)), 0, 4);
        layout.Controls.Add(CreateInfoLabel(string.Format(
            Strings.Info_CandidateReason,
            MainForm.LocalizeReason(candidate.MatchReason) ?? Strings.Text_None)), 0, 5);

        FlowLayoutPanel buttonPanel = new()
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0, 8, 0, 0)
        };
        container.Controls.Add(buttonPanel);

        CheckBox chooseCheckBox = new()
        {
            Text = Strings.Checkbox_Choose,
            AutoSize = true,
            Checked = candidate.Selected
        };
        chooseCheckBox.CheckedChanged += (_, _) => ToggleCandidateSelection(group, candidate, chooseCheckBox.Checked);
        buttonPanel.Controls.Add(chooseCheckBox);

        return container;
    }

    private static Label CreateInfoLabel(string text)
    {
        return new Label
        {
            AutoSize = true,
            MaximumSize = new Size(300, 0),
            Text = text
        };
    }

    private static Image? LoadPreview(string sourceAbsolutePath)
    {
        if (!File.Exists(sourceAbsolutePath))
        {
            return null;
        }

        using MemoryStream stream = new(File.ReadAllBytes(sourceAbsolutePath));
        using Image loadedImage = Image.FromStream(stream);
        return new Bitmap(loadedImage);
    }

    private void ToggleCandidateSelection(CardConflictGroup group, MergedMappingCandidate candidate, bool isChecked)
    {
        if (_session is null)
        {
            return;
        }

        if (!isChecked && !candidate.Selected)
        {
            return;
        }

        foreach (MergedMappingCandidate groupCandidate in _session.Candidates.Where(item =>
                     string.Equals(item.MatchedCardId, group.CardId, StringComparison.OrdinalIgnoreCase)))
        {
            bool isWinner = isChecked &&
                            string.Equals(groupCandidate.CandidateId, candidate.CandidateId, StringComparison.OrdinalIgnoreCase);
            groupCandidate.Selected = isWinner;

            if (isWinner)
            {
                groupCandidate.Ignored = false;
                groupCandidate.IgnoredReason = null;
            }
            else if (isChecked)
            {
                groupCandidate.Ignored = true;
                groupCandidate.IgnoredReason = "Discarded in conflicts review.";
            }
            else if (string.Equals(groupCandidate.IgnoredReason, "Discarded in conflicts review.", StringComparison.Ordinal))
            {
                groupCandidate.Ignored = false;
                groupCandidate.IgnoredReason = null;
            }
        }

        AfterSessionMutation();
    }

    private void ToggleGroupDiscardedState()
    {
        if (_suppressEvents || _session is null || _groupListBox.SelectedItem is not CardConflictGroup group)
        {
            return;
        }

        bool discardGroup = _discardGroupCheckBox.Checked;
        foreach (MergedMappingCandidate candidate in _session.Candidates.Where(item =>
                     string.Equals(item.MatchedCardId, group.CardId, StringComparison.OrdinalIgnoreCase)))
        {
            candidate.Ignored = discardGroup;
            candidate.IgnoredReason = discardGroup ? "Discarded in conflicts review." : null;
            candidate.Selected = false;
        }

        AfterSessionMutation();
    }

    private void SelectNextPendingConflict()
    {
        List<CardConflictGroup> groups = GetFilteredGroups();
        if (groups.Count == 0)
        {
            return;
        }

        int currentIndex = _groupListBox.SelectedIndex;
        int startIndex = currentIndex < 0 ? 0 : currentIndex + 1;

        CardConflictGroup? nextPending = groups
            .Skip(startIndex)
            .Concat(groups.Take(startIndex))
            .FirstOrDefault(group => string.Equals(group.ResolutionState, "Pending", StringComparison.OrdinalIgnoreCase));

        if (nextPending is not null)
        {
            _groupListBox.SelectedItem = nextPending;
        }
    }

    private void AfterSessionMutation()
    {
        if (_session is null)
        {
            return;
        }

        _conflictResolutionService.Refresh(_session);
        _sessionChanged();
        RefreshConflictGroups(keepSelection: true);
    }

    private void ClearCandidateCards()
    {
        foreach (Control control in _candidatePanel.Controls)
        {
            if (control is PictureBox pictureBox)
            {
                pictureBox.Image?.Dispose();
            }
            else
            {
                DisposeImagesRecursive(control);
            }

            control.Dispose();
        }

        _candidatePanel.Controls.Clear();
    }

    private static void DisposeImagesRecursive(Control control)
    {
        if (control is PictureBox pictureBox)
        {
            pictureBox.Image?.Dispose();
        }

        foreach (Control child in control.Controls)
        {
            DisposeImagesRecursive(child);
        }
    }

    private static Color GetResolutionColor(string resolutionState)
    {
        return resolutionState switch
        {
            "Resolved" => Color.ForestGreen,
            "Discarded" => Color.Goldenrod,
            _ => Color.IndianRed
        };
    }
}
