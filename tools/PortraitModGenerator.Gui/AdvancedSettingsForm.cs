using PortraitModGenerator.Core.Services;
using PortraitModGenerator.Gui.Resources;

namespace PortraitModGenerator.Gui;

internal sealed class AdvancedSettingsForm : Form
{
    private readonly string _cardDisplayName;
    private readonly Dictionary<string, TextBox> _fieldInputs = new(StringComparer.Ordinal);
    private readonly Label _helpLabel;
    private readonly Button _okButton;
    private readonly Button _cancelButton;

    public AdvancedSettingsForm(string cardDisplayName, IReadOnlyDictionary<string, string>? initialValues)
    {
        _cardDisplayName = cardDisplayName;

        Text = string.Format(Strings.AdvancedSettingsForm_Title, cardDisplayName);
        Width = 720;
        Height = 620;
        MinimumSize = new Size(640, 480);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        TableLayoutPanel rootLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(rootLayout);

        _helpLabel = new Label
        {
            Text = Strings.AdvancedSettingsForm_Help,
            AutoSize = true,
            MaximumSize = new Size(680, 0),
            Padding = new Padding(0, 0, 0, 8)
        };
        rootLayout.Controls.Add(_helpLabel, 0, 0);

        Panel fieldsScrollHost = new()
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };
        rootLayout.Controls.Add(fieldsScrollHost, 0, 1);

        TableLayoutPanel fieldsLayout = new()
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        fieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        fieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fieldsScrollHost.Controls.Add(fieldsLayout);

        int row = 0;
        foreach (string key in MappingMaterializer.AdvancedFieldKeys)
        {
            Label label = new()
            {
                Text = Strings.GetAdvancedFieldLabel(key),
                AutoSize = true,
                Padding = new Padding(0, 8, 12, 0),
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };
            fieldsLayout.Controls.Add(label, 0, row);

            TextBox input = new()
            {
                Dock = DockStyle.Fill,
                Width = 480,
                Margin = new Padding(0, 4, 0, 4),
                PlaceholderText = "res://..."
            };
            if (initialValues is not null && initialValues.TryGetValue(key, out string? existing))
            {
                input.Text = existing;
            }
            fieldsLayout.Controls.Add(input, 1, row);
            _fieldInputs[key] = input;
            row++;
        }

        FlowLayoutPanel buttonPanel = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0)
        };
        rootLayout.Controls.Add(buttonPanel, 0, 2);

        _okButton = new Button
        {
            Text = Strings.AdvancedSettingsForm_OK,
            AutoSize = true,
            DialogResult = DialogResult.OK,
            Margin = new Padding(8, 0, 0, 0)
        };
        buttonPanel.Controls.Add(_okButton);

        _cancelButton = new Button
        {
            Text = Strings.AdvancedSettingsForm_Cancel,
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };
        buttonPanel.Controls.Add(_cancelButton);

        AcceptButton = _okButton;
        CancelButton = _cancelButton;
    }

    public Dictionary<string, string> GetResult()
    {
        Dictionary<string, string> result = new(StringComparer.Ordinal);
        foreach ((string key, TextBox input) in _fieldInputs)
        {
            string value = input.Text.Trim();
            if (!string.IsNullOrEmpty(value))
            {
                result[key] = value;
            }
        }

        return result;
    }
}
