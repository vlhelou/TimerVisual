using System.Media;

namespace TimerVisual;

public sealed class MainForm : Form
{
    // ---- Cores do tema ----
    private static readonly Color Fundo = Color.FromArgb(28, 28, 34);
    private static readonly Color Texto = Color.FromArgb(235, 235, 240);
    private static readonly Color TextoSuave = Color.FromArgb(150, 150, 165);
    private static readonly Color Cor1 = Color.FromArgb(66, 165, 245);
    private static readonly Color Cor5 = Color.FromArgb(102, 187, 106);
    private static readonly Color Cor15 = Color.FromArgb(255, 167, 38);

    // ---- Estado ----
    private TimeSpan _diferencaNtp = TimeSpan.Zero;   // NTP - relógio local
    private bool _modoAgora;                          // referência = instante atual (como --agora)
    private DateTimeOffset _referencia;
    private long _ultimoMinuto;

    private readonly Dictionary<string, SoundPlayer> _sons = new();
    private readonly System.Windows.Forms.Timer _relogio = new() { Interval = 100 };
    private readonly ToolTip _dica = new();

    // ---- Controles ----
    private readonly Label _lblUltimo = new();
    private readonly BarraProgresso _barra1 = new() { Cor = Cor1 };
    private readonly BarraProgresso _barra5 = new() { Cor = Cor5 };
    private readonly BarraProgresso _barra15 = new() { Cor = Cor15 };
    private readonly CheckBox _chkSilenciar = new();
    private readonly CheckBox _chkSempreVisivel = new();
    private readonly Button _btnOpcoes = new();

    // ---- Menu de opções ----
    private readonly ContextMenuStrip _menu = new();
    private readonly ToolStripMenuItem _mnuStatus = new() { Enabled = false };
    private readonly ToolStripComboBox _cmbServidor = new();
    private readonly ToolStripMenuItem _mnuSincronizar = new("Sincronizar NTP");
    private readonly NumericUpDown _numOffset = new();

    public MainForm()
    {
        Text = "Timer";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        BackColor = Fundo;
        ForeColor = Texto;
        Font = new Font("Segoe UI", 9f);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(300, 120);
        KeyPreview = true;

        CarregarSons();
        MontarMenu();
        MontarTela();

        DefinirReferencia();
        _relogio.Tick += (_, _) => Atualizar();
        _relogio.Start();

        Shown += async (_, _) => await SincronizarAsync();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.M) { _chkSilenciar.Checked = !_chkSilenciar.Checked; e.Handled = true; }
            else if (e.KeyCode == Keys.T) { _chkSempreVisivel.Checked = !_chkSempreVisivel.Checked; e.Handled = true; }
        };
    }

    // =====================================================================
    // Montagem da interface
    // =====================================================================
    private void MontarTela()
    {
        var raiz = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 8, 8, 4),
            ColumnCount = 2,
            RowCount = 4,
        };
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 4; i++) raiz.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(raiz);

        Linha(raiz, 0, "1 min", Cor1, _barra1);
        Linha(raiz, 1, "5 min", Cor5, _barra5);
        Linha(raiz, 2, "15 min", Cor15, _barra15);

        // Rodapé: botões compactos + último toque
        var rodape = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, 2, 0, 0) };

        Alternador(_chkSilenciar, "🔊", Color.FromArgb(183, 28, 28));
        _chkSilenciar.CheckedChanged += (_, _) => AoSilenciar();
        _dica.SetToolTip(_chkSilenciar, "Silenciar / reativar (tecla M)");
        rodape.Controls.Add(_chkSilenciar);

        Alternador(_chkSempreVisivel, "📌", Color.FromArgb(21, 101, 192));
        _chkSempreVisivel.CheckedChanged += (_, _) => TopMost = _chkSempreVisivel.Checked;
        _dica.SetToolTip(_chkSempreVisivel, "Sempre visível, por cima das outras janelas (tecla T)");
        rodape.Controls.Add(_chkSempreVisivel);

        _btnOpcoes.Text = "⚙";
        _btnOpcoes.Size = new Size(30, 24);
        _btnOpcoes.FlatStyle = FlatStyle.Flat;
        _btnOpcoes.FlatAppearance.BorderColor = TextoSuave;
        _btnOpcoes.Margin = new Padding(0, 0, 6, 0);
        _btnOpcoes.Cursor = Cursors.Hand;
        _btnOpcoes.Click += (_, _) => _menu.Show(_btnOpcoes, new Point(0, _btnOpcoes.Height));
        _dica.SetToolTip(_btnOpcoes, "Opções: NTP, offset, reiniciar (ou clique direito na janela)");
        rodape.Controls.Add(_btnOpcoes);

        _lblUltimo.AutoSize = true;
        _lblUltimo.ForeColor = TextoSuave;
        _lblUltimo.Font = new Font("Segoe UI", 8f);
        _lblUltimo.Margin = new Padding(0, 5, 0, 0);
        _lblUltimo.Text = "";
        rodape.Controls.Add(_lblUltimo);

        raiz.Controls.Add(rodape, 0, 3);
        raiz.SetColumnSpan(rodape, 2);

        ContextMenuStrip = _menu;
        raiz.ContextMenuStrip = _menu;
    }

    private static void Linha(TableLayoutPanel t, int linha, string titulo, Color cor, BarraProgresso barra)
    {
        var lbl = new Label
        {
            Text = titulo,
            AutoSize = true,
            ForeColor = cor,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 6, 0),
        };
        barra.Dock = DockStyle.Fill;
        barra.Height = 22;
        barra.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        barra.Margin = new Padding(0, 2, 0, 2);
        t.Controls.Add(lbl, 0, linha);
        t.Controls.Add(barra, 1, linha);
    }

    private static void Alternador(CheckBox c, string texto, Color corLigado)
    {
        c.Text = texto;
        c.Font = new Font("Segoe UI Emoji", 9f);
        c.Appearance = Appearance.Button;
        c.AutoSize = false;
        c.Size = new Size(30, 24);
        c.TextAlign = ContentAlignment.MiddleCenter;
        c.FlatStyle = FlatStyle.Flat;
        c.FlatAppearance.BorderColor = TextoSuave;
        c.FlatAppearance.CheckedBackColor = corLigado;
        c.Margin = new Padding(0, 0, 4, 0);
        c.Cursor = Cursors.Hand;
    }

    private void MontarMenu()
    {
        _menu.Items.Add(_mnuStatus);
        _menu.Items.Add(new ToolStripSeparator());

        _menu.Items.Add(new ToolStripLabel("Servidor NTP:") { ForeColor = SystemColors.GrayText });
        _cmbServidor.DropDownStyle = ComboBoxStyle.DropDown;
        _cmbServidor.Size = new Size(160, 23);
        foreach (var s in Ntp.Servidores) _cmbServidor.Items.Add(s.Host);
        _cmbServidor.Text = "time.windows.com";
        _menu.Items.Add(_cmbServidor);
        _mnuSincronizar.Click += async (_, _) => await SincronizarAsync();
        _menu.Items.Add(_mnuSincronizar);
        _menu.Items.Add(new ToolStripSeparator());

        _menu.Items.Add(new ToolStripLabel("Offset (segundos):") { ForeColor = SystemColors.GrayText });
        _numOffset.Minimum = -3600;
        _numOffset.Maximum = 3600;
        _numOffset.Width = 80;
        _numOffset.ValueChanged += (_, _) => DefinirReferencia();
        _menu.Items.Add(new ToolStripControlHost(_numOffset));
        _menu.Items.Add(new ToolStripSeparator());

        _menu.Items.Add("Começar agora", null, (_, _) => { _modoAgora = true; DefinirReferencia(); })
            .ToolTipText = "Reinicia os 3 timers a partir deste instante";
        _menu.Items.Add("Alinhar ao relógio", null, (_, _) => { _modoAgora = false; DefinirReferencia(); })
            .ToolTipText = "Alinha os timers aos quartos de hora (:00, :15, :30, :45)";
    }

    // =====================================================================
    // Lógica dos timers (mesma regra do repositório original)
    // =====================================================================
    private DateTimeOffset Agora() => DateTimeOffset.Now + _diferencaNtp;

    private void DefinirReferencia()
    {
        var agora = Agora();
        DateTimeOffset baseRef;
        if (_modoAgora)
        {
            baseRef = agora;
        }
        else
        {
            // Arredonda para o último quarto de hora (:00, :15, :30, :45)
            var seg = agora.ToUnixTimeSeconds();
            baseRef = DateTimeOffset.FromUnixTimeSeconds(seg - Mod(seg, 60 * 15)).ToLocalTime();
        }
        _referencia = baseRef.AddSeconds((double)_numOffset.Value);
        _ultimoMinuto = MinutoAtual(agora);
        Atualizar();
    }

    private long MinutoAtual(DateTimeOffset agora) =>
        (long)Math.Floor((agora - _referencia).TotalSeconds / 60.0);

    private void Atualizar()
    {
        var agora = Agora();
        var decorrido = (agora - _referencia).TotalSeconds;

        AtualizarBarra(_barra1, decorrido, 60);
        AtualizarBarra(_barra5, decorrido, 5 * 60);
        AtualizarBarra(_barra15, decorrido, 15 * 60);

        var minuto = MinutoAtual(agora);
        if (minuto > _ultimoMinuto) Tocar(minuto, agora);
        _ultimoMinuto = minuto;

        var faltando = (int)Math.Ceiling(60 - ModD(decorrido, 60));
        Text = $"{(_chkSilenciar.Checked ? "🔇 " : "")}Faltando: {faltando}s";
    }

    private static void AtualizarBarra(BarraProgresso barra, double decorrido, int periodo)
    {
        var pos = ModD(decorrido, periodo);
        barra.Valor = pos / periodo;
        var resta = TimeSpan.FromSeconds(Math.Ceiling(periodo - pos));
        barra.Texto = $"{resta:mm\\:ss}";
    }

    private void Tocar(long minuto, DateTimeOffset agora)
    {
        string som, desc;
        if (Mod(minuto, 15) == 0) { som = "Vela15"; desc = "Vela 15"; }
        else if (Mod(minuto, 5) == 0) { som = "Vela5"; desc = "Vela 5"; }
        else { som = "Minuto" + Mod(minuto, 5); desc = $"Minuto {Mod(minuto, 5)}"; }

        var silenciado = _chkSilenciar.Checked;
        _lblUltimo.Text = $"{agora:HH:mm} · {desc}{(silenciado ? " (mudo)" : "")}";

        if (!silenciado && _sons.TryGetValue(som, out var player))
            player.Play();
    }

    private void AoSilenciar()
    {
        var mudo = _chkSilenciar.Checked;
        _chkSilenciar.Text = mudo ? "🔇" : "🔊";
        if (mudo) foreach (var p in _sons.Values) p.Stop();
        Atualizar();
    }

    private async Task SincronizarAsync()
    {
        var servidor = _cmbServidor.Text.Trim();
        if (servidor.Length == 0) return;
        _mnuSincronizar.Enabled = false;
        _mnuStatus.Text = $"Sincronizando com {servidor}…";
        try
        {
            _diferencaNtp = await Ntp.ObterDiferencaAsync(servidor);
            _mnuStatus.Text = $"NTP {servidor}: {_diferencaNtp.TotalSeconds:+0.000;-0.000}s";
        }
        catch (Exception ex)
        {
            _diferencaNtp = TimeSpan.Zero;
            _mnuStatus.Text = $"Falha no NTP ({ex.GetType().Name}) — relógio local";
        }
        finally
        {
            _mnuSincronizar.Enabled = true;
            _dica.SetToolTip(_btnOpcoes, _mnuStatus.Text);
            if (!_modoAgora) DefinirReferencia(); else _ultimoMinuto = MinutoAtual(Agora());
        }
    }

    private void CarregarSons()
    {
        var asm = typeof(MainForm).Assembly;
        foreach (var nome in new[] { "Minuto1", "Minuto2", "Minuto3", "Minuto4", "Vela5", "Vela15" })
        {
            var stream = asm.GetManifestResourceStream($"Sons.{nome}.wav");
            if (stream is null) continue;
            var p = new SoundPlayer(stream);
            p.Load();
            _sons[nome] = p;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _relogio.Stop();
        foreach (var p in _sons.Values) p.Dispose();
        base.OnFormClosed(e);
    }

    private static long Mod(long a, long m) => ((a % m) + m) % m;
    private static double ModD(double a, double m) => ((a % m) + m) % m;
}
