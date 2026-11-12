using System.Drawing.Drawing2D;

namespace TimerVisual;

/// <summary>Barra de progresso desenhada à mão: cor customizável, atualização suave e texto central.</summary>
public sealed class BarraProgresso : Control
{
    private double _valor;
    private Color _cor = Color.DodgerBlue;
    private string _texto = "";

    public BarraProgresso()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Height = 34;
        BackColor = Color.FromArgb(45, 45, 52);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 11f, FontStyle.Bold);
    }

    /// <summary>Valor entre 0 e 1.</summary>
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public double Valor
    {
        get => _valor;
        set { var v = Math.Clamp(value, 0, 1); if (v != _valor) { _valor = v; Invalidate(); } }
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color Cor
    {
        get => _cor;
        set { _cor = value; Invalidate(); }
    }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string Texto
    {
        get => _texto;
        set { if (value != _texto) { _texto = value; Invalidate(); } }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? BackColor);

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var fundo = Arredondado(rect, 8))
        using (var b = new SolidBrush(BackColor))
            g.FillPath(b, fundo);

        var largura = (int)Math.Round((Width - 1) * _valor);
        if (largura > 2)
        {
            var r = new Rectangle(0, 0, largura, Height - 1);
            using var path = Arredondado(r, 8);
            using var grad = new LinearGradientBrush(r, ControlPaint.Light(_cor, 0.3f), _cor, LinearGradientMode.Vertical);
            g.FillPath(grad, path);
        }

        TextRenderer.DrawText(g, _texto, Font, rect, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    private static GraphicsPath Arredondado(Rectangle r, int raio)
    {
        var d = Math.Min(raio * 2, Math.Min(r.Width, r.Height));
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}
