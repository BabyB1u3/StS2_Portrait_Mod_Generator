using System.Windows.Forms;
using PortraitModGenerator.Core.Abstractions;
using PortraitModGenerator.Core.Services;

namespace PortraitModGenerator.Gui;

internal sealed class ConflictReviewForm : Form
{
    private readonly Action _sessionChanged;
    private readonly ConflictResolutionService _conflictResolutionService = new();
    private readonly ComboBox _filterComboBox;
    private readonly Button _nextPendingButton;
    private readonly Label _summaryLabel;
    private readonly ListBox _groupListBox;
    private readonly Label _groupHeaderLabel;
    private readonly Label _groupStateLabel;
    private readonly FlowLayoutPanel _candidatePanel;
    private readonly Button _toggleGroupIgnoreButton;

    private MergedReviewSession? _session;

    public ConflictReviewForm(MergedReviewSession session, Action sessionChanged)
    {
        _session = session;
        _sessionChanged = sessionChanged;

        Text = "Conflict Review";
        Width = 1500;
        Height = 920;
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
        _filterComboBox.Items.AddRange(["All", "Pending", "Resolved", "Ignored"]);
        _filterComboBox.SelectedIndex = 0;
        _filterComboBox.SelectedIndexChanged += (_, _) => RefreshConflictGroups(keepSelection: false);
        toolbar.Controls.Add(_filterComboBox);

        _nextPendingButton = new Button
        {
            Text = "Next Pending",
            AutoSize = true
        };
        _nextPendingButton.Click += (_, _) => SelectNextPendingConflict();
        toolbar.Controls.Add(_nextPendingButton);

        _toggleGroupIgnoreButton = new Button
        {
            Text = "Ignore Group",
            AutoSize = true
        };
        _toggleGroupIgnoreButton.Click += (_, _) => ToggleGroupIgnoredState();
        toolbar.Controls.Add(_toggleGroupIgnoreButton);

        _summaryLabel = new Label
        {
            AutoSize = true,
            Padding = new Padding(12, 8, 0, 0)
        };
        toolbar.Controls.Add(_summaryLabel);

        SplitContainer split = new()
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 360
        };
        rootLayout.Controls.Add(split, 0, 1);

        _groupListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 28,
            HorizontalScrollbar = true
        };
        _groupListBox.DrawItem += DrawConflictGroupItem;
        _groupListBox.SelectedIndexChanged += (_, _) => BindSelectedGroup();
        split.Panel1.Controls.Add(_groupListBox);

        TableLayoutPanel detailLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        split.Panel2.Controls.Add(detailLayout);

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

        RefreshConflictGroups(keepSelection: false);
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
            : $"Conflicts {_session.ConflictGroups.Count} | Pending {_session.ConflictGroups.Count(group => string.Equals(group.ResolutionState, "Pending", StringComparison.OrdinalIgnoreCase))} | Resolved {_session.ConflictGroups.Count(group => string.Equals(group.ResolutionState, "Resolved", StringComparison.OrdinalIgnoreCase))} | Ignored {_session.ConflictGroups.Count(group => string.Equals(group.ResolutionState, "Ignored", StringComparison.OrdinalIgnoreCase))}";

        if (groups.Count == 0)
        {
            _groupHeaderLabel.Text = "No conflicts";
            _groupStateLabel.Text = "This session currently has no cardId conflicts.";
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
        string filter = _filterComboBox.SelectedItem?.ToString() ?? "All";
        groups = filter switch
        {
            "Pending" => groups.Where(group => string.Equals(group.ResolutionState, "Pending", StringComparison.OrdinalIgnoreCase)),
            "Resolved" => groups.Where(group => string.Equals(group.ResolutionState, "Resolved", StringComparison.OrdinalIgnoreCase)),
            "Ignored" => groups.Where(group => string.Equals(group.ResolutionState, "Ignored", StringComparison.OrdinalIgnoreCase)),
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
            $"[{group.ResolutionState}]",
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
            _groupHeaderLabel.Text = "No conflict selected";
            _groupStateLabel.Text = string.Empty;
            ClearCandidateCards();
            return;
        }

        _groupHeaderLabel.Text = $"{group.CanonicalName} [{group.Group}]";
        _groupStateLabel.Text = $"cardId: {group.CardId} | candidates: {group.CandidateIds.Count} | state: {group.ResolutionState}";
        _toggleGroupIgnoreButton.Text = string.Equals(group.ResolutionState, "Ignored", StringComparison.OrdinalIgnoreCase)
            ? "Unignore Group"
            : "Ignore Group";

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
        Panel card = new()
        {
            Width = 320,
            Height = 520,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(8)
        };

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
                ? "Current Winner"
                : candidate.Ignored
                    ? "Ignored"
                    : candidate.IsAutoSelected
                        ? "Default Pick"
                        : "Available Candidate"
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

        layout.Controls.Add(CreateInfoLabel($"Package: {candidate.SourcePackageName}"), 0, 2);
        layout.Controls.Add(CreateInfoLabel($"File: {candidate.FileName}"), 0, 3);
        layout.Controls.Add(CreateInfoLabel($"Confidence: {candidate.Confidence:0.00}"), 0, 4);
        layout.Controls.Add(CreateInfoLabel($"Reason: {candidate.MatchReason ?? "(none)"}"), 0, 5);

        FlowLayoutPanel buttonPanel = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true
        };
        layout.Controls.Add(buttonPanel, 0, 6);

        Button selectButton = new()
        {
            Text = candidate.Selected ? "Selected" : "Select Winner",
            AutoSize = true,
            Enabled = !candidate.Selected
        };
        selectButton.Click += (_, _) => SelectCandidate(group, candidate);
        buttonPanel.Controls.Add(selectButton);

        Button ignoreButton = new()
        {
            Text = candidate.Ignored ? "Unignore" : "Ignore",
            AutoSize = true
        };
        ignoreButton.Click += (_, _) => ToggleCandidateIgnoredState(group, candidate);
        buttonPanel.Controls.Add(ignoreButton);

        return card;
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

    private void SelectCandidate(CardConflictGroup group, MergedMappingCandidate candidate)
    {
        if (_session is null)
        {
            return;
        }

        foreach (MergedMappingCandidate groupCandidate in _session.Candidates.Where(item =>
                     string.Equals(item.MatchedCardId, group.CardId, StringComparison.OrdinalIgnoreCase)))
        {
            groupCandidate.Selected = string.Equals(groupCandidate.CandidateId, candidate.CandidateId, StringComparison.OrdinalIgnoreCase);
            if (groupCandidate.Selected)
            {
                groupCandidate.Ignored = false;
                groupCandidate.IgnoredReason = null;
            }
        }

        AfterSessionMutation();
    }

    private void ToggleCandidateIgnoredState(CardConflictGroup group, MergedMappingCandidate candidate)
    {
        if (_session is null)
        {
            return;
        }

        MergedMappingCandidate? target = _session.Candidates.FirstOrDefault(item =>
            string.Equals(item.CandidateId, candidate.CandidateId, StringComparison.OrdinalIgnoreCase));
        if (target is null)
        {
            return;
        }

        target.Ignored = !target.Ignored;
        target.IgnoredReason = target.Ignored ? "Ignored in conflicts review." : null;
        if (target.Ignored)
        {
            target.Selected = false;
        }

        if (!target.Ignored && _session.Candidates.Where(item =>
                string.Equals(item.MatchedCardId, group.CardId, StringComparison.OrdinalIgnoreCase) &&
                !item.Ignored).All(item => !item.Selected))
        {
            target.Selected = true;
        }

        AfterSessionMutation();
    }

    private void ToggleGroupIgnoredState()
    {
        if (_session is null || _groupListBox.SelectedItem is not CardConflictGroup group)
        {
            return;
        }

        bool ignoreGroup = !string.Equals(group.ResolutionState, "Ignored", StringComparison.OrdinalIgnoreCase);
        foreach (MergedMappingCandidate candidate in _session.Candidates.Where(item =>
                     string.Equals(item.MatchedCardId, group.CardId, StringComparison.OrdinalIgnoreCase)))
        {
            candidate.Ignored = ignoreGroup;
            candidate.IgnoredReason = ignoreGroup ? "Ignored in conflicts review." : null;
            candidate.Selected = false;
        }

        if (!ignoreGroup)
        {
            MergedMappingCandidate? defaultCandidate = _session.Candidates
                .Where(item => string.Equals(item.MatchedCardId, group.CardId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.IsAutoSelected)
                .ThenByDescending(item => item.Confidence)
                .FirstOrDefault();
            if (defaultCandidate is not null)
            {
                defaultCandidate.Selected = true;
            }
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
            "Ignored" => Color.Goldenrod,
            _ => Color.IndianRed
        };
    }
}
