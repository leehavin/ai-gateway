using DataChat.Application;
using DataChat.Core.Configuration;
using DataChat.Core.Entities;
using DataChat.WinForms.UI;

namespace DataChat.WinForms;

public sealed class MainForm : Form
{
    private static readonly Color BgMain = Color.FromArgb(241, 245, 249);
    private static readonly Color SidebarBg = Color.FromArgb(15, 23, 42);
    private static readonly Color SidebarHover = Color.FromArgb(30, 41, 59);
    private static readonly Color Accent = Color.FromArgb(99, 102, 241);
    private static readonly Color AccentHover = Color.FromArgb(79, 70, 229);
    private static readonly Color TextMuted = Color.FromArgb(100, 116, 139);

    private readonly ChatService _chatService;
    private readonly DomainsConfiguration _domains;
    private readonly ChatWebViewHost _webView = new();
    private readonly ListBox _sessionList = new();
    private readonly ComboBox _domainCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly TextBox _input = new() { Multiline = true, Dock = DockStyle.Fill, BorderStyle = BorderStyle.None };
    private readonly Button _sendBtn = new() { Text = "发送  ⏎", Width = 100, Height = 40 };
    private readonly Button _stopBtn = new() { Text = "停止", Width = 100, Height = 36 };
    private readonly Button _newSessionBtn = new() { Text = "＋  新会话", Height = 40 };
    private readonly Label _statusLabel = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };

    private ChatSession? _currentSession;
    private string? _streamingAssistantId;
    private bool _sending;

    public MainForm(ChatService chatService, DomainsConfiguration domains)
    {
        _chatService = chatService;
        _domains = domains;
        Text = "DataChat · 智能助手";
        Width = 1180;
        Height = 760;
        MinimumSize = new Size(960, 640);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);
        BackColor = BgMain;
        BuildLayout();
        Load += OnLoadAsync;
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = BgMain,
            Padding = new Padding(0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 248));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildSidebar(), 0, 0);
        root.Controls.Add(BuildMainPanel(), 1, 0);
        Controls.Add(root);

        _sessionList.SelectedIndexChanged += async (_, _) => await LoadSelectedSessionAsync();
        _input.KeyDown += OnInputKeyDown;
    }

    private Panel BuildSidebar()
    {
        var sidebar = new Panel { Dock = DockStyle.Fill, BackColor = SidebarBg, Padding = new Padding(12, 16, 12, 12) };

        var title = new Label
        {
            Text = "DataChat",
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold),
            Dock = DockStyle.Top,
            Height = 32,
            Padding = new Padding(4, 0, 0, 0)
        };
        var subtitle = new Label
        {
            Text = "会话历史",
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Segoe UI", 8.5f),
            Dock = DockStyle.Top,
            Height = 22,
            Padding = new Padding(4, 0, 0, 8)
        };

        StylePrimaryButton(_newSessionBtn);
        _newSessionBtn.Dock = DockStyle.Top;
        _newSessionBtn.Margin = new Padding(0, 0, 0, 12);
        _newSessionBtn.Click += async (_, _) => await CreateSessionAsync();

        _sessionList.Dock = DockStyle.Fill;
        _sessionList.BorderStyle = BorderStyle.None;
        _sessionList.BackColor = SidebarBg;
        _sessionList.ForeColor = Color.FromArgb(226, 232, 240);
        _sessionList.Font = new Font("Segoe UI", 9.5f);
        _sessionList.IntegralHeight = false;
        _sessionList.DrawMode = DrawMode.OwnerDrawFixed;
        _sessionList.ItemHeight = 44;
        _sessionList.DrawItem += DrawSessionItem;

        sidebar.Controls.Add(_sessionList);
        sidebar.Controls.Add(_newSessionBtn);
        sidebar.Controls.Add(subtitle);
        sidebar.Controls.Add(title);
        return sidebar;
    }

    private Panel BuildMainPanel()
    {
        var main = new Panel { Dock = DockStyle.Fill, BackColor = BgMain, Padding = new Padding(12, 12, 12, 8) };

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(0)
        };
        card.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(226, 232, 240), 1);
            var r = card.ClientRectangle;
            r.Width -= 1; r.Height -= 1;
            g.DrawRectangle(pen, r);
        };

        var inner = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1, BackColor = Color.White };
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

        var toolbar = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16, 10, 16, 10) };
        var toolbarFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        var domainLabel = new Label
        {
            Text = "智能体",
            AutoSize = true,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
            Padding = new Padding(0, 8, 8, 0)
        };
        foreach (var d in _domains.Domains)
            _domainCombo.Items.Add(d);
        _domainCombo.DisplayMember = "DisplayName";
        _domainCombo.Font = new Font("Segoe UI", 9.5f);
        _domainCombo.FlatStyle = FlatStyle.Flat;
        _domainCombo.BackColor = Color.FromArgb(248, 250, 252);
        toolbarFlow.Controls.Add(domainLabel);
        toolbarFlow.Controls.Add(_domainCombo);
        toolbar.Controls.Add(toolbarFlow);
        inner.Controls.Add(toolbar, 0, 0);

        _webView.Dock = DockStyle.Fill;
        inner.Controls.Add(_webView, 0, 1);

        var inputWrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(12) };
        var inputBorder = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(12, 10, 12, 10)
        };
        inputBorder.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 240));
            var r = inputBorder.ClientRectangle;
            r.Width -= 1; r.Height -= 1;
            e.Graphics.DrawRectangle(pen, r);
        };

        var inputGrid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));

        _input.Font = new Font("Segoe UI", 10f);
        _input.BackColor = Color.White;
        _input.ForeColor = Color.FromArgb(15, 23, 42);
        inputGrid.Controls.Add(_input, 0, 0);

        var btnCol = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(8, 0, 0, 0) };
        StylePrimaryButton(_sendBtn);
        StyleSecondaryButton(_stopBtn);
        _sendBtn.Click += async (_, _) => await SendAsync();
        _stopBtn.Click += (_, _) => _chatService.CancelCurrent();
        btnCol.Controls.Add(_sendBtn);
        btnCol.Controls.Add(_stopBtn);
        inputGrid.Controls.Add(btnCol, 1, 0);

        inputBorder.Controls.Add(inputGrid);
        inputWrap.Controls.Add(inputBorder);
        inner.Controls.Add(inputWrap, 0, 2);

        _statusLabel.Text = $"  DB-GPT {_domains.Defaults.DbgptBaseUrl}  ·  Coze {_domains.Defaults.CozeEndpoint}  ·  本地 history";
        _statusLabel.ForeColor = TextMuted;
        _statusLabel.Font = new Font("Segoe UI", 8.25f);
        inner.Controls.Add(_statusLabel, 0, 3);

        card.Controls.Add(inner);
        main.Controls.Add(card);
        return main;
    }

    private void DrawSessionItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var bg = selected ? SidebarHover : SidebarBg;
        using var brush = new SolidBrush(bg);
        e.Graphics.FillRectangle(brush, e.Bounds);

        var session = _sessionList.Items[e.Index] as ChatSession;
        var title = session?.Title ?? "会话";
        if (title.Length > 18) title = title[..17] + "…";

        using var titleBrush = new SolidBrush(selected ? Color.White : Color.FromArgb(226, 232, 240));
        using var titleFont = new Font("Segoe UI", 9.5f, selected ? FontStyle.Bold : FontStyle.Regular);
        e.Graphics.DrawString(title, titleFont, titleBrush, e.Bounds.Left + 10, e.Bounds.Top + 8);

        if (session?.UpdatedAt is > 0)
        {
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(session.UpdatedAt).LocalDateTime;
            using var subBrush = new SolidBrush(Color.FromArgb(100, 116, 139));
            using var subFont = new Font("Segoe UI", 8f);
            e.Graphics.DrawString(dt.ToString("MM-dd HH:mm"), subFont, subBrush, e.Bounds.Left + 10, e.Bounds.Top + 26);
        }
    }

    private void StylePrimaryButton(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = Accent;
        btn.ForeColor = Color.White;
        btn.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
        btn.Cursor = Cursors.Hand;
        btn.MouseEnter += (_, _) => btn.BackColor = AccentHover;
        btn.MouseLeave += (_, _) => btn.BackColor = Accent;
    }

    private void StyleSecondaryButton(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderColor = Color.FromArgb(226, 232, 240);
        btn.FlatAppearance.BorderSize = 1;
        btn.BackColor = Color.White;
        btn.ForeColor = TextMuted;
        btn.Font = new Font("Segoe UI", 9f);
        btn.Cursor = Cursors.Hand;
    }

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && !e.Shift && !e.Control)
        {
            e.SuppressKeyPress = true;
            _ = SendAsync();
        }
    }

    private async void OnLoadAsync(object? sender, EventArgs e)
    {
        Load -= OnLoadAsync;
        var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        await _webView.InitializeAsync(wwwroot);
        await RefreshSessionsAsync();
        if (_domainCombo.Items.Count > 0)
            _domainCombo.SelectedIndex = 0;
    }

    private async Task RefreshSessionsAsync()
    {
        var selectedId = (_sessionList.SelectedItem as ChatSession)?.Id;
        _sessionList.Items.Clear();
        var sessions = await _chatService.ListSessionsAsync();
        foreach (var s in sessions)
            _sessionList.Items.Add(s);
        if (selectedId is not null)
        {
            for (var i = 0; i < _sessionList.Items.Count; i++)
            {
                if ((_sessionList.Items[i] as ChatSession)?.Id == selectedId)
                {
                    _sessionList.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private async Task CreateSessionAsync()
    {
        if (_domainCombo.SelectedItem is not DomainProfile domain) return;
        _currentSession = await _chatService.CreateSessionAsync(domain.Id);
        await RefreshSessionsAsync();
        _sessionList.SelectedItem = _currentSession;
        await _webView.RenderMessagesAsync([]);
    }

    private async Task LoadSelectedSessionAsync()
    {
        if (_sessionList.SelectedItem is not ChatSession session) return;
        _currentSession = session;
        var messages = await _chatService.GetMessagesAsync(session.Id);
        await _webView.RenderMessagesAsync(messages);
    }

    private async Task SendAsync()
    {
        if (_sending || _currentSession is null || string.IsNullOrWhiteSpace(_input.Text)) return;
        _sending = true;
        _sendBtn.Enabled = false;
        var text = _input.Text.Trim();
        _input.Clear();

        _streamingAssistantId = "streaming";
        await _webView.AppendMessageAsync("streaming-user", "user", text, false);
        await _webView.AppendMessageAsync(_streamingAssistantId, "assistant", "", true);

        var buffer = "";
        try
        {
            await foreach (var chunk in _chatService.SendMessageAsync(_currentSession.Id, text))
            {
                if (chunk.Error is not null)
                {
                    buffer += "\n[错误] " + chunk.Error;
                    break;
                }
                if (chunk.TextDelta is not null)
                {
                    buffer += chunk.TextDelta;
                    await _webView.UpdateMessageAsync(_streamingAssistantId, buffer, !chunk.IsCompleted);
                }
            }
            await _webView.UpdateMessageAsync(_streamingAssistantId, buffer, false);
            var messages = await _chatService.GetMessagesAsync(_currentSession.Id);
            await _webView.RenderMessagesAsync(messages);
            await RefreshSessionsAsync();
        }
        finally
        {
            _sending = false;
            _sendBtn.Enabled = true;
        }
    }
}
