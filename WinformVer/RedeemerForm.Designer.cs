#nullable enable

using System.Drawing;
using System.Windows.Forms;

namespace MementoMoriCodeRedeemer;

partial class RedeemerForm
{
    private System.ComponentModel.IContainer? components = null;

    private TableLayoutPanel _rootLayout = null!;
    private GroupBox _settingsGroup = null!;
    private TableLayoutPanel _settingsLayout = null!;
    private TableLayoutPanel _settingsFieldShell = null!;
    private TableLayoutPanel _settingsFieldsLayout = null!;
    private Label _serverLabel = null!;
    private Label _serialCodeLabel = null!;
    private Panel _logoPanel = null!;
    private PictureBox _logoLink = null!;
    private GroupBox _playersGroup = null!;
    private TableLayoutPanel _playersLayout = null!;
    private TableLayoutPanel _playerEditorPanel = null!;
    private Label _editorPlayerIdLabel = null!;
    private Label _editorNoteLabel = null!;
    private FlowLayoutPanel _editButtonsPanel = null!;
    private FlowLayoutPanel _selectionButtonsPanel = null!;
    private FlowLayoutPanel _clipboardButtonsPanel = null!;
    private TableLayoutPanel _actionLayout = null!;
    private GroupBox _logGroup = null!;

    private ComboBox _serverComboBox = null!;
    private Button _refreshServersButton = null!;
    private TextBox _serialCodeTextBox = null!;
    private TransparentImageCheckBox _confirmOnlyCheckBox = null!;
    private TransparentDataGridView _playersGrid = null!;
    private TextBox _playerIdTextBox = null!;
    private TextBox _noteTextBox = null!;
    private Button _addPlayerButton = null!;
    private Button _updatePlayerButton = null!;
    private Button _deletePlayerButton = null!;
    private Button _selectAllButton = null!;
    private Button _clearSelectionButton = null!;
    private Button _importClipboardButton = null!;
    private Button _exportClipboardButton = null!;
    private Button _redeemSelectedButton = null!;
    private Button _redeemAllButton = null!;
    private Button _cancelButton = null!;
    private TransparentImageProgressBar _progressBar = null!;
    private Label _statusLabel = null!;
    private TransparentLogBox _logBox = null!;
    private ToolTip _toolTip = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        _rootLayout = new TableLayoutPanel();
        _settingsGroup = new GroupBox();
        _settingsLayout = new TableLayoutPanel();
        _settingsFieldShell = new TableLayoutPanel();
        _settingsFieldsLayout = new TableLayoutPanel();
        _serverLabel = new Label();
        _serverComboBox = new ComboBox();
        _refreshServersButton = new Button();
        _serialCodeLabel = new Label();
        _serialCodeTextBox = new TextBox();
        _confirmOnlyCheckBox = new TransparentImageCheckBox();
        _logoPanel = new Panel();
        _logoLink = new PictureBox();
        _playersGroup = new GroupBox();
        _playersLayout = new TableLayoutPanel();
        _playersGrid = new TransparentDataGridView();
        _playerEditorPanel = new TableLayoutPanel();
        _editorPlayerIdLabel = new Label();
        _playerIdTextBox = new TextBox();
        _editorNoteLabel = new Label();
        _noteTextBox = new TextBox();
        _editButtonsPanel = new FlowLayoutPanel();
        _addPlayerButton = new Button();
        _updatePlayerButton = new Button();
        _deletePlayerButton = new Button();
        _selectionButtonsPanel = new FlowLayoutPanel();
        _selectAllButton = new Button();
        _clearSelectionButton = new Button();
        _clipboardButtonsPanel = new FlowLayoutPanel();
        _importClipboardButton = new Button();
        _exportClipboardButton = new Button();
        _actionLayout = new TableLayoutPanel();
        _redeemSelectedButton = new Button();
        _redeemAllButton = new Button();
        _cancelButton = new Button();
        _progressBar = new TransparentImageProgressBar();
        _statusLabel = new Label();
        _logGroup = new GroupBox();
        _logBox = new TransparentLogBox();
        _toolTip = new ToolTip(components);

        SuspendLayout();
        _rootLayout.SuspendLayout();
        _settingsGroup.SuspendLayout();
        _settingsLayout.SuspendLayout();
        _settingsFieldShell.SuspendLayout();
        _settingsFieldsLayout.SuspendLayout();
        _logoPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_logoLink).BeginInit();
        _playersGroup.SuspendLayout();
        _playersLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_playersGrid).BeginInit();
        _playerEditorPanel.SuspendLayout();
        _editButtonsPanel.SuspendLayout();
        _selectionButtonsPanel.SuspendLayout();
        _clipboardButtonsPanel.SuspendLayout();
        _actionLayout.SuspendLayout();
        _logGroup.SuspendLayout();

        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(248, 247, 242);
        ClientSize = new Size(984, 741);
        Font = new Font("Microsoft YaHei UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        MinimumSize = new Size(1000, 780);
        MaximumSize = new Size(1000, 780);
        Name = "RedeemerForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "MementoMori 虛寶碼自動領取";

        _rootLayout.BackColor = Color.Transparent;
        _rootLayout.ColumnCount = 1;
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _rootLayout.Controls.Add(_settingsGroup, 0, 0);
        _rootLayout.Controls.Add(_playersGroup, 0, 1);
        _rootLayout.Controls.Add(_actionLayout, 0, 2);
        _rootLayout.Controls.Add(_logGroup, 0, 3);
        _rootLayout.Dock = DockStyle.Fill;
        _rootLayout.Location = new Point(0, 0);
        _rootLayout.Name = "_rootLayout";
        _rootLayout.Padding = new Padding(14);
        _rootLayout.RowCount = 4;
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
        _rootLayout.Size = new Size(984, 741);
        Controls.Add(_rootLayout);

        _settingsGroup.BackColor = Color.Transparent;
        _settingsGroup.Dock = DockStyle.Top;
        _settingsGroup.Height = 90;
        _settingsGroup.Margin = new Padding(0, 0, 0, 8);
        _settingsGroup.MinimumSize = new Size(0, 90);
        _settingsGroup.Name = "_settingsGroup";
        _settingsGroup.Padding = new Padding(14, 10, 14, 8);
        _settingsGroup.Text = "兌換設定";
        _settingsGroup.Controls.Add(_settingsLayout);

        _settingsLayout.BackColor = Color.Transparent;
        _settingsLayout.ColumnCount = 2;
        _settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210F));
        _settingsLayout.Controls.Add(_settingsFieldShell, 0, 0);
        _settingsLayout.Controls.Add(_logoPanel, 1, 0);
        _settingsLayout.Dock = DockStyle.Fill;
        _settingsLayout.Margin = new Padding(0);
        _settingsLayout.Name = "_settingsLayout";
        _settingsLayout.RowCount = 1;
        _settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _settingsFieldShell.BackColor = Color.Transparent;
        _settingsFieldShell.ColumnCount = 1;
        _settingsFieldShell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _settingsFieldShell.Controls.Add(_settingsFieldsLayout, 0, 1);
        _settingsFieldShell.Dock = DockStyle.Fill;
        _settingsFieldShell.Margin = new Padding(0);
        _settingsFieldShell.Name = "_settingsFieldShell";
        _settingsFieldShell.RowCount = 3;
        _settingsFieldShell.RowStyles.Add(new RowStyle(SizeType.Absolute, 8F));
        _settingsFieldShell.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        _settingsFieldShell.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _settingsFieldsLayout.BackColor = Color.Transparent;
        _settingsFieldsLayout.ColumnCount = 11;
        _settingsFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52F));
        _settingsFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190F));
        _settingsFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 6F));
        _settingsFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38F));
        _settingsFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16F));
        _settingsFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52F));
        _settingsFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 4F));
        _settingsFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
        _settingsFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12F));
        _settingsFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 158F));
        _settingsFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _settingsFieldsLayout.Controls.Add(_serverLabel, 0, 0);
        _settingsFieldsLayout.Controls.Add(_serverComboBox, 1, 0);
        _settingsFieldsLayout.Controls.Add(_refreshServersButton, 3, 0);
        _settingsFieldsLayout.Controls.Add(_serialCodeLabel, 5, 0);
        _settingsFieldsLayout.Controls.Add(_serialCodeTextBox, 7, 0);
        _settingsFieldsLayout.Controls.Add(_confirmOnlyCheckBox, 9, 0);
        _settingsFieldsLayout.Dock = DockStyle.Fill;
        _settingsFieldsLayout.Margin = new Padding(0);
        _settingsFieldsLayout.Name = "_settingsFieldsLayout";
        _settingsFieldsLayout.RowCount = 1;
        _settingsFieldsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _serverLabel.BackColor = Color.Transparent;
        _serverLabel.Dock = DockStyle.Fill;
        _serverLabel.Margin = new Padding(0, 2, 0, 2);
        _serverLabel.Name = "_serverLabel";
        _serverLabel.Text = "伺服器";
        _serverLabel.TextAlign = ContentAlignment.MiddleLeft;

        _serverComboBox.Dock = DockStyle.Fill;
        _serverComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _serverComboBox.Margin = new Padding(0, 3, 0, 3);
        _serverComboBox.Name = "_serverComboBox";

        _refreshServersButton.Dock = DockStyle.Fill;
        _refreshServersButton.Margin = new Padding(0);
        _refreshServersButton.Name = "_refreshServersButton";
        _refreshServersButton.Text = "刷新";
        _refreshServersButton.UseVisualStyleBackColor = true;
        _refreshServersButton.Click += RefreshServersButton_Click;

        _serialCodeLabel.BackColor = Color.Transparent;
        _serialCodeLabel.Dock = DockStyle.Fill;
        _serialCodeLabel.Margin = new Padding(0, 2, 0, 2);
        _serialCodeLabel.Name = "_serialCodeLabel";
        _serialCodeLabel.Text = "虛寶碼";
        _serialCodeLabel.TextAlign = ContentAlignment.MiddleLeft;

        _serialCodeTextBox.Dock = DockStyle.Fill;
        _serialCodeTextBox.Margin = new Padding(0, 3, 0, 3);
        _serialCodeTextBox.MaxLength = 64;
        _serialCodeTextBox.MinimumSize = new Size(140, 0);
        _serialCodeTextBox.Name = "_serialCodeTextBox";

        _confirmOnlyCheckBox.AutoSize = false;
        _confirmOnlyCheckBox.BackColor = Color.Transparent;
        _confirmOnlyCheckBox.Dock = DockStyle.Fill;
        _confirmOnlyCheckBox.Margin = new Padding(0);
        _confirmOnlyCheckBox.MinimumSize = new Size(158, 30);
        _confirmOnlyCheckBox.Name = "_confirmOnlyCheckBox";
        _confirmOnlyCheckBox.Text = "僅確認不兌換";
        _confirmOnlyCheckBox.TextAlign = ContentAlignment.MiddleLeft;
        _confirmOnlyCheckBox.UseVisualStyleBackColor = false;

        _logoPanel.BackColor = Color.Transparent;
        _logoPanel.Controls.Add(_logoLink);
        _logoPanel.Dock = DockStyle.Fill;
        _logoPanel.Margin = new Padding(0);
        _logoPanel.Name = "_logoPanel";

        _logoLink.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _logoLink.BackColor = Color.Transparent;
        _logoLink.Cursor = Cursors.Hand;
        _logoLink.Location = new Point(0, 0);
        _logoLink.Margin = new Padding(0);
        _logoLink.MinimumSize = new Size(198, 60);
        _logoLink.Name = "_logoLink";
        _logoLink.Size = new Size(198, 60);
        _logoLink.SizeMode = PictureBoxSizeMode.Zoom;
        _logoLink.Click += LogoLink_Click;

        _playersGroup.BackColor = Color.Transparent;
        _playersGroup.Controls.Add(_playersLayout);
        _playersGroup.Dock = DockStyle.Fill;
        _playersGroup.Margin = new Padding(0, 0, 0, 10);
        _playersGroup.MinimumSize = new Size(0, 300);
        _playersGroup.Name = "_playersGroup";
        _playersGroup.Padding = new Padding(12);
        _playersGroup.Text = "玩家ID內容庫";

        _playersLayout.BackColor = Color.Transparent;
        _playersLayout.ColumnCount = 2;
        _playersLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _playersLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 176F));
        _playersLayout.Controls.Add(_playersGrid, 0, 0);
        _playersLayout.Controls.Add(_playerEditorPanel, 1, 0);
        _playersLayout.Dock = DockStyle.Fill;
        _playersLayout.Name = "_playersLayout";
        _playersLayout.RowCount = 1;
        _playersLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _playersGrid.AllowUserToAddRows = false;
        _playersGrid.AllowUserToDeleteRows = false;
        _playersGrid.AllowUserToResizeColumns = true;
        _playersGrid.AllowUserToResizeRows = false;
        _playersGrid.AutoGenerateColumns = false;
        _playersGrid.BackgroundColor = Color.FromArgb(248, 247, 242);
        _playersGrid.BorderStyle = BorderStyle.None;
        _playersGrid.CellBorderStyle = DataGridViewCellBorderStyle.Single;
        _playersGrid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        _playersGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _playersGrid.DefaultCellStyle.BackColor = Color.FromArgb(248, 247, 242);
        _playersGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 247, 242);
        _playersGrid.DefaultCellStyle.SelectionForeColor = SystemColors.WindowText;
        _playersGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 247, 242);
        _playersGrid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 247, 242);
        _playersGrid.AlternatingRowsDefaultCellStyle.SelectionForeColor = SystemColors.WindowText;
        _playersGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 247, 242);
        _playersGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 247, 242);
        _playersGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = SystemColors.WindowText;
        _playersGrid.EnableHeadersVisualStyles = false;
        _playersGrid.GridColor = SystemColors.ControlDark;
        _playersGrid.Columns.Add(new DataGridViewImageColumn
        {
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter },
            HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            ImageLayout = DataGridViewImageCellLayout.Zoom,
            MinimumWidth = 44,
            Name = "_lockedColumn",
            ReadOnly = true,
            Resizable = DataGridViewTriState.False,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            HeaderText = "鎖定",
            Width = 44,
        });
        _playersGrid.Columns.Add(new DataGridViewImageColumn
        {
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter },
            HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
            ImageLayout = DataGridViewImageCellLayout.Normal,
            MinimumWidth = 44,
            Name = "_selectedColumn",
            ReadOnly = true,
            Resizable = DataGridViewTriState.False,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            HeaderText = "選擇",
            Width = 44,
        });
        _playersGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(PlayerRow.PlayerId),
            DefaultCellStyle = new DataGridViewCellStyle { Padding = new Padding(7, 0, 0, 0) },
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            HeaderText = "玩家ID",
            Width = 150,
        });
        _playersGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(PlayerRow.Note),
            DefaultCellStyle = new DataGridViewCellStyle { Padding = new Padding(7, 0, 0, 0) },
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            HeaderText = "備註",
            Width = 150,
        });
        _playersGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(PlayerRow.LastResult),
            DefaultCellStyle = new DataGridViewCellStyle { Padding = new Padding(7, 0, 0, 0) },
            SortMode = DataGridViewColumnSortMode.NotSortable,
            HeaderText = "最近結果",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            ReadOnly = true,
        });
        _playersGrid.DataSource = _players;
        _playersGrid.Dock = DockStyle.Fill;
        _playersGrid.MultiSelect = false;
        _playersGrid.Name = "_playersGrid";
        _playersGrid.RowHeadersVisible = false;
        _playersGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _playersGrid.SelectionChanged += PlayersGrid_SelectionChanged;
        _playersGrid.CellClick += PlayersGrid_CellClick;
        _playersGrid.CellFormatting += PlayersGrid_CellFormatting;
        _playersGrid.CellValidating += PlayersGrid_CellValidating;
        _playersGrid.CellEndEdit += PlayersGrid_CellEndEdit;
        _playersGrid.ColumnHeaderMouseClick += PlayersGrid_ColumnHeaderMouseClick;
        _playersGrid.CurrentCellChanged += PlayersGrid_CurrentCellChanged;
        _playersGrid.CurrentCellDirtyStateChanged += PlayersGrid_CurrentCellDirtyStateChanged;

        _playerEditorPanel.BackColor = Color.Transparent;
        _playerEditorPanel.ColumnCount = 1;
        _playerEditorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _playerEditorPanel.Controls.Add(_editorPlayerIdLabel, 0, 0);
        _playerEditorPanel.Controls.Add(_playerIdTextBox, 0, 1);
        _playerEditorPanel.Controls.Add(_editorNoteLabel, 0, 2);
        _playerEditorPanel.Controls.Add(_noteTextBox, 0, 3);
        _playerEditorPanel.Controls.Add(_editButtonsPanel, 0, 4);
        _playerEditorPanel.Controls.Add(_selectionButtonsPanel, 0, 5);
        _playerEditorPanel.Controls.Add(_clipboardButtonsPanel, 0, 6);
        _playerEditorPanel.Dock = DockStyle.Fill;
        _playerEditorPanel.MinimumSize = new Size(0, 228);
        _playerEditorPanel.Name = "_playerEditorPanel";
        _playerEditorPanel.Padding = new Padding(10, 0, 0, 0);
        _playerEditorPanel.RowCount = 8;
        _playerEditorPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _playerEditorPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _playerEditorPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _playerEditorPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _playerEditorPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _playerEditorPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _playerEditorPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _playerEditorPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _editorPlayerIdLabel.AutoSize = true;
        _editorPlayerIdLabel.BackColor = Color.Transparent;
        _editorPlayerIdLabel.Name = "_editorPlayerIdLabel";
        _editorPlayerIdLabel.Text = "玩家ID";

        _playerIdTextBox.Dock = DockStyle.None;
        _playerIdTextBox.BackColor = SystemColors.Window;
        _playerIdTextBox.MaxLength = 32;
        _playerIdTextBox.Name = "_playerIdTextBox";
        _playerIdTextBox.Width = 156;

        _editorNoteLabel.AutoSize = true;
        _editorNoteLabel.BackColor = Color.Transparent;
        _editorNoteLabel.Margin = new Padding(0, 10, 0, 0);
        _editorNoteLabel.Name = "_editorNoteLabel";
        _editorNoteLabel.Text = "備註";

        _noteTextBox.Dock = DockStyle.None;
        _noteTextBox.BackColor = SystemColors.Window;
        _noteTextBox.MaxLength = 80;
        _noteTextBox.Name = "_noteTextBox";
        _noteTextBox.Width = 156;

        _editButtonsPanel.BackColor = Color.Transparent;
        _editButtonsPanel.AutoSize = true;
        _editButtonsPanel.Dock = DockStyle.Top;
        _editButtonsPanel.Margin = new Padding(0, 8, 0, 0);
        _editButtonsPanel.Name = "_editButtonsPanel";
        _editButtonsPanel.WrapContents = false;
        _editButtonsPanel.Controls.Add(_addPlayerButton);
        _editButtonsPanel.Controls.Add(_updatePlayerButton);
        _editButtonsPanel.Controls.Add(_deletePlayerButton);

        _addPlayerButton.Name = "_addPlayerButton";
        _addPlayerButton.Text = "新增";
        _addPlayerButton.UseVisualStyleBackColor = true;
        _addPlayerButton.Width = 78;
        _addPlayerButton.Click += AddPlayerButton_Click;

        _updatePlayerButton.Name = "_updatePlayerButton";
        _updatePlayerButton.Text = "修改";
        _updatePlayerButton.UseVisualStyleBackColor = true;
        _updatePlayerButton.Width = 78;
        _updatePlayerButton.Click += UpdatePlayerButton_Click;

        _deletePlayerButton.Name = "_deletePlayerButton";
        _deletePlayerButton.Text = "刪除";
        _deletePlayerButton.UseVisualStyleBackColor = true;
        _deletePlayerButton.Width = 78;
        _deletePlayerButton.Click += DeletePlayerButton_Click;

        _selectionButtonsPanel.BackColor = Color.Transparent;
        _selectionButtonsPanel.AutoSize = true;
        _selectionButtonsPanel.Dock = DockStyle.Top;
        _selectionButtonsPanel.Margin = new Padding(0, 8, 0, 0);
        _selectionButtonsPanel.Name = "_selectionButtonsPanel";
        _selectionButtonsPanel.WrapContents = false;
        _selectionButtonsPanel.Controls.Add(_selectAllButton);
        _selectionButtonsPanel.Controls.Add(_clearSelectionButton);

        _selectAllButton.Name = "_selectAllButton";
        _selectAllButton.Text = "全選";
        _selectAllButton.UseVisualStyleBackColor = true;
        _selectAllButton.Width = 78;
        _selectAllButton.Click += SelectAllButton_Click;

        _clearSelectionButton.Name = "_clearSelectionButton";
        _clearSelectionButton.Text = "全不選";
        _clearSelectionButton.UseVisualStyleBackColor = true;
        _clearSelectionButton.Width = 78;
        _clearSelectionButton.Click += ClearSelectionButton_Click;

        _clipboardButtonsPanel.BackColor = Color.Transparent;
        _clipboardButtonsPanel.AutoSize = true;
        _clipboardButtonsPanel.Dock = DockStyle.Top;
        _clipboardButtonsPanel.Margin = new Padding(0, 8, 0, 0);
        _clipboardButtonsPanel.Name = "_clipboardButtonsPanel";
        _clipboardButtonsPanel.WrapContents = false;
        _clipboardButtonsPanel.Controls.Add(_importClipboardButton);
        _clipboardButtonsPanel.Controls.Add(_exportClipboardButton);

        _importClipboardButton.Dock = DockStyle.None;
        _importClipboardButton.Size = new Size(44, 44);
        _importClipboardButton.Margin = new Padding(0);
        _importClipboardButton.Name = "_importClipboardButton";
        _importClipboardButton.Text = "從剪貼簿匯入ID";
        _importClipboardButton.UseVisualStyleBackColor = true;
        _importClipboardButton.Click += ImportClipboardButton_Click;

        _exportClipboardButton.Dock = DockStyle.None;
        _exportClipboardButton.Size = new Size(44, 44);
        _exportClipboardButton.Margin = new Padding(0);
        _exportClipboardButton.Name = "_exportClipboardButton";
        _exportClipboardButton.Text = "複製ID庫";
        _exportClipboardButton.UseVisualStyleBackColor = true;
        _exportClipboardButton.Click += ExportClipboardButton_Click;

        _actionLayout.BackColor = Color.Transparent;
        _actionLayout.ColumnCount = 6;
        _actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        _actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        _actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
        _actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260F));
        _actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 4F));
        _actionLayout.Controls.Add(_redeemSelectedButton, 0, 0);
        _actionLayout.Controls.Add(_redeemAllButton, 1, 0);
        _actionLayout.Controls.Add(_cancelButton, 2, 0);
        _actionLayout.Controls.Add(_progressBar, 3, 0);
        _actionLayout.Controls.Add(_statusLabel, 4, 0);
        _actionLayout.Dock = DockStyle.Top;
        _actionLayout.Height = 44;
        _actionLayout.Margin = new Padding(0, 0, 0, 5);
        _actionLayout.MinimumSize = new Size(0, 44);
        _actionLayout.Name = "_actionLayout";
        _actionLayout.RowCount = 1;
        _actionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

        _redeemSelectedButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        _redeemSelectedButton.Height = 36;
        _redeemSelectedButton.Margin = new Padding(0, 4, 0, 4);
        _redeemSelectedButton.MinimumSize = new Size(0, 36);
        _redeemSelectedButton.Name = "_redeemSelectedButton";
        _redeemSelectedButton.Size = new Size(122, 36);
        _redeemSelectedButton.Text = "領取勾選";
        _redeemSelectedButton.UseVisualStyleBackColor = true;
        _redeemSelectedButton.Click += RedeemSelectedButton_Click;

        _redeemAllButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        _redeemAllButton.Height = 36;
        _redeemAllButton.Margin = new Padding(0, 4, 0, 4);
        _redeemAllButton.MinimumSize = new Size(0, 36);
        _redeemAllButton.Name = "_redeemAllButton";
        _redeemAllButton.Size = new Size(122, 36);
        _redeemAllButton.Text = "領取全部";
        _redeemAllButton.UseVisualStyleBackColor = true;
        _redeemAllButton.Click += RedeemAllButton_Click;

        _cancelButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        _cancelButton.Enabled = false;
        _cancelButton.Height = 36;
        _cancelButton.Margin = new Padding(0, 4, 0, 4);
        _cancelButton.MinimumSize = new Size(0, 36);
        _cancelButton.Name = "_cancelButton";
        _cancelButton.Size = new Size(92, 36);
        _cancelButton.Text = "取消";
        _cancelButton.UseVisualStyleBackColor = true;
        _cancelButton.Click += CancelButton_Click;

        _progressBar.Dock = DockStyle.Fill;
        _progressBar.Height = 28;
        _progressBar.Margin = new Padding(0, 6, 0, 6);
        _progressBar.Name = "_progressBar";

        _statusLabel.BackColor = Color.Transparent;
        _statusLabel.AutoEllipsis = true;
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.Name = "_statusLabel";
        _statusLabel.Text = "等待開始";
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;

        _logGroup.BackColor = Color.Transparent;
        _logGroup.Controls.Add(_logBox);
        _logGroup.Dock = DockStyle.Fill;
        _logGroup.Name = "_logGroup";
        _logGroup.Padding = new Padding(12);
        _logGroup.Text = "日誌";

        _logBox.BackColor = Color.Transparent;
        _logBox.BorderStyle = BorderStyle.FixedSingle;
        _logBox.Dock = DockStyle.Fill;
        _logBox.Font = new Font("Consolas", 9F);
        _logBox.Name = "_logBox";
        _logBox.ReadOnly = true;
        _logBox.WordWrap = false;

        _logGroup.ResumeLayout(false);
        _actionLayout.ResumeLayout(false);
        _clipboardButtonsPanel.ResumeLayout(false);
        _selectionButtonsPanel.ResumeLayout(false);
        _editButtonsPanel.ResumeLayout(false);
        _playerEditorPanel.ResumeLayout(false);
        _playerEditorPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_playersGrid).EndInit();
        _playersLayout.ResumeLayout(false);
        _playersGroup.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_logoLink).EndInit();
        _logoPanel.ResumeLayout(false);
        _settingsFieldsLayout.ResumeLayout(false);
        _settingsFieldsLayout.PerformLayout();
        _settingsFieldShell.ResumeLayout(false);
        _settingsLayout.ResumeLayout(false);
        _settingsGroup.ResumeLayout(false);
        _rootLayout.ResumeLayout(false);
        ResumeLayout(false);
    }
}
