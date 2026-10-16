using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;
using System.Text;

namespace MementoMoriCodeRedeemer;

internal static class TransparentControlChrome
{
    public static Color BorderColor => SystemColors.ControlDark;
}

internal sealed class TransparentImageCheckBox : CheckBox
{
    private const int TextGap = 6;
    private Image? _boxImage;
    private Image? _checkImage;

    public TransparentImageCheckBox()
    {
        SetStyle(
            ControlStyles.UserPaint
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.SupportsTransparentBackColor,
            true);

        BackColor = Color.Transparent;
        ForeColor = SystemColors.WindowText;
    }

    public Image? BoxImage
    {
        get => _boxImage;
        set
        {
            _boxImage = value;
            Invalidate();
        }
    }

    public Image? CheckImage
    {
        get => _checkImage;
        set
        {
            _checkImage = value;
            Invalidate();
        }
    }

    protected override void OnCheckedChanged(EventArgs e)
    {
        base.OnCheckedChanged(e);
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        PaintFormBackground(e.Graphics);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        PaintFormBackground(e.Graphics);

        if (_boxImage is null)
        {
            return;
        }

        var boxSize = Math.Min(_boxImage.Width, Math.Min(_boxImage.Height, ClientSize.Height));
        var boxBounds = new Rectangle(0, (ClientSize.Height - boxSize) / 2, boxSize, boxSize);
        e.Graphics.DrawImage(_boxImage, boxBounds);

        if (Checked && _checkImage is not null)
        {
            e.Graphics.DrawImage(_checkImage, boxBounds);
        }

        var textBounds = new Rectangle(
            boxBounds.Right + TextGap,
            0,
            Math.Max(0, ClientSize.Width - boxBounds.Right - TextGap),
            ClientSize.Height);
        using var textBrush = new SolidBrush(Enabled ? ForeColor : SystemColors.GrayText);
        using var textFormat = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap,
        };
        e.Graphics.DrawString(Text, Font, textBrush, textBounds, textFormat);
    }

    private void PaintFormBackground(Graphics graphics)
    {
        var form = FindForm();
        if (form?.BackgroundImage is null)
        {
            using var brush = new SolidBrush(Parent?.BackColor ?? SystemColors.Window);
            graphics.FillRectangle(brush, ClientRectangle);
            return;
        }

        var origin = form.PointToClient(PointToScreen(Point.Empty));
        graphics.DrawImage(
            form.BackgroundImage,
            new Rectangle(-origin.X, -origin.Y, form.ClientSize.Width, form.ClientSize.Height));
    }
}

internal sealed class TransparentDataGridView : DataGridView
{
    private const int CurrentRowBackgroundStartColumnIndex = 2;
    private const float CurrentRowBackgroundOffsetX = -6.5F;
    private const float CurrentRowBackgroundOffsetY = -4F;
    private const int WmMouseWheel = 0x020A;
    private const int WmVScroll = 0x0115;
    private const int WmHScroll = 0x0114;
    private Bitmap? _backgroundCache;
    private Size _backgroundCacheClientSize;
    private Size _backgroundCacheFormSize;
    private Point _backgroundCacheOrigin;
    private Image? _backgroundCacheSource;

    public Color BorderColor { get; set; } = TransparentControlChrome.BorderColor;
    public Image? CurrentRowBackgroundImage { get; set; }

    public TransparentDataGridView()
    {
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw,
            true);
    }

    protected override void SetSelectedCellCore(int columnIndex, int rowIndex, bool selected)
    {
        base.SetSelectedCellCore(columnIndex, rowIndex, false);
    }

    protected override void SetSelectedRowCore(int rowIndex, bool selected)
    {
        base.SetSelectedRowCore(rowIndex, false);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        PaintGridBackground(e.Graphics, ClientRectangle);
    }

    protected override void OnScroll(ScrollEventArgs e)
    {
        base.OnScroll(e);
        RedrawAfterScroll();
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        if (m.Msg is WmMouseWheel or WmVScroll or WmHScroll)
        {
            RedrawAfterScroll();
        }
    }

    protected override void OnLocationChanged(EventArgs e)
    {
        base.OnLocationChanged(e);
        ClearBackgroundCache();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        ClearBackgroundCache();
    }

    protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex >= 0 && CurrentCell?.RowIndex == e.RowIndex && CurrentRowBackgroundImage is not null)
        {
            PaintCurrentRowCell(e);
            return;
        }

        if (e.RowIndex >= 0 && e.CellStyle is not null)
        {
            var normalBackColor = e.CellStyle.BackColor.IsEmpty
                || e.CellStyle.BackColor == Color.Transparent
                ? DefaultCellStyle.BackColor
                : e.CellStyle.BackColor;
            var normalForeColor = e.CellStyle.ForeColor.IsEmpty
                ? ForeColor
                : e.CellStyle.ForeColor;

            e.CellStyle.SelectionBackColor = normalBackColor;
            e.CellStyle.SelectionForeColor = normalForeColor;
        }

        if (e.Graphics is not null)
        {
            PaintCellBackground(e.Graphics, e.CellBounds);
        }

        var paintParts = e.PaintParts
            & ~DataGridViewPaintParts.Background
            & ~DataGridViewPaintParts.SelectionBackground
            & ~DataGridViewPaintParts.Focus;

        e.Paint(e.CellBounds, paintParts);
        e.Handled = true;
    }

    private void PaintCurrentRowCell(DataGridViewCellPaintingEventArgs e)
    {
        var graphics = e.Graphics;
        var rowBackground = CurrentRowBackgroundImage;
        if (e.CellStyle is null || graphics is null || rowBackground is null)
        {
            return;
        }

        PaintCellBackground(graphics, e.CellBounds);

        var rowBounds = GetRowDisplayRectangle(e.RowIndex, false);
        if (!rowBounds.IsEmpty)
        {
            var startCellBounds = GetCellDisplayRectangle(CurrentRowBackgroundStartColumnIndex, e.RowIndex, false);
            var imageBounds = new RectangleF(
                (startCellBounds.IsEmpty ? rowBounds.Left : startCellBounds.Left) + CurrentRowBackgroundOffsetX,
                rowBounds.Top + ((rowBounds.Height - rowBackground.Height) / 2) + CurrentRowBackgroundOffsetY,
                rowBackground.Width,
                rowBackground.Height);
            var previousClip = graphics.Clip;
            var rowClipBounds = new RectangleF(rowBounds.X, rowBounds.Y, rowBounds.Width, rowBounds.Height);
            var cellClipBounds = new RectangleF(e.CellBounds.X, e.CellBounds.Y, e.CellBounds.Width, e.CellBounds.Height);
            var clipBounds = RectangleF.Intersect(
                rowClipBounds,
                RectangleF.Intersect(cellClipBounds, imageBounds));

            if (!clipBounds.IsEmpty)
            {
                graphics.SetClip(clipBounds);
                graphics.DrawImage(rowBackground, imageBounds);
            }

            graphics.Clip = previousClip;
            previousClip.Dispose();
        }

        e.CellStyle.SelectionBackColor = e.CellStyle.BackColor;
        e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor.IsEmpty ? ForeColor : e.CellStyle.ForeColor;

        var paintParts = e.PaintParts
            & ~DataGridViewPaintParts.Background
            & ~DataGridViewPaintParts.SelectionBackground
            & ~DataGridViewPaintParts.Focus;
        e.Paint(e.CellBounds, paintParts);
        e.Handled = true;
    }

    private void PaintCellBackground(Graphics graphics, Rectangle bounds)
    {
        PaintGridBackground(graphics, bounds);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        PaintGridBackground(e.Graphics, ClientRectangle);
        base.OnPaint(e);
        PaintUnusedRowsBackground(e.Graphics);
        ControlPaint.DrawBorder(e.Graphics, ClientRectangle, BorderColor, ButtonBorderStyle.Solid);
    }

    private void PaintUnusedRowsBackground(Graphics graphics)
    {
        var bounds = GetScrollableClientBounds();
        var bottom = ColumnHeadersVisible ? ColumnHeadersHeight : 0;

        foreach (DataGridViewRow row in Rows)
        {
            var rowBounds = GetRowDisplayRectangle(row.Index, true);
            if (rowBounds.IsEmpty || rowBounds.Bottom <= bottom || rowBounds.Top >= bounds.Bottom)
            {
                continue;
            }

            bottom = Math.Max(bottom, Math.Min(bounds.Bottom, rowBounds.Bottom));
        }

        if (bottom < bounds.Bottom)
        {
            PaintGridBackground(
                graphics,
                Rectangle.FromLTRB(bounds.Left, bottom, bounds.Right, bounds.Bottom));
        }
    }

    private Rectangle GetScrollableClientBounds()
    {
        var bounds = ClientRectangle;
        foreach (Control child in Controls)
        {
            if (!child.Visible)
            {
                continue;
            }

            if (child is VScrollBar)
            {
                bounds.Width = Math.Min(bounds.Width, Math.Max(0, child.Left - bounds.Left));
            }
            else if (child is HScrollBar)
            {
                bounds.Height = Math.Min(bounds.Height, Math.Max(0, child.Top - bounds.Top));
            }
        }

        return bounds;
    }

    private void PaintGridBackground(Graphics graphics, Rectangle clipBounds)
    {
        var previousClip = graphics.Clip;
        try
        {
            graphics.SetClip(clipBounds);
            PaintFixedBackground(graphics, clipBounds);
        }
        finally
        {
            graphics.Clip = previousClip;
            previousClip.Dispose();
        }
    }

    private void PaintFixedBackground(Graphics graphics, Rectangle clipBounds)
    {
        if (!EnsureBackgroundCache())
        {
            using var brush = new SolidBrush(Parent?.BackColor ?? SystemColors.Window);
            graphics.FillRectangle(brush, clipBounds);
            return;
        }

        var sourceBounds = Rectangle.Intersect(clipBounds, new Rectangle(Point.Empty, _backgroundCache!.Size));
        if (sourceBounds.IsEmpty)
        {
            return;
        }

        graphics.DrawImage(_backgroundCache, sourceBounds, sourceBounds, GraphicsUnit.Pixel);
    }

    private bool EnsureBackgroundCache()
    {
        var form = FindForm();
        if (ClientSize.Width <= 0 || ClientSize.Height <= 0 || form is null)
        {
            return false;
        }

        var origin = form.PointToClient(PointToScreen(Point.Empty));
        if (_backgroundCache is not null
            && _backgroundCacheClientSize == ClientSize
            && _backgroundCacheFormSize == form.ClientSize
            && _backgroundCacheOrigin == origin
            && ReferenceEquals(_backgroundCacheSource, form.BackgroundImage))
        {
            return true;
        }

        ClearBackgroundCache();
        _backgroundCache = new Bitmap(ClientSize.Width, ClientSize.Height);
        _backgroundCacheClientSize = ClientSize;
        _backgroundCacheFormSize = form.ClientSize;
        _backgroundCacheOrigin = origin;
        _backgroundCacheSource = form.BackgroundImage;

        using var cacheGraphics = Graphics.FromImage(_backgroundCache);
        if (form.BackgroundImage is null)
        {
            using var brush = new SolidBrush(Parent?.BackColor ?? SystemColors.Window);
            cacheGraphics.FillRectangle(brush, new Rectangle(Point.Empty, ClientSize));
            return true;
        }

        cacheGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        cacheGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        cacheGraphics.DrawImage(
            form.BackgroundImage,
            new Rectangle(-origin.X, -origin.Y, form.ClientSize.Width, form.ClientSize.Height));

        return true;
    }

    private void RedrawAfterScroll()
    {
        Invalidate();
        Update();
    }

    private void ClearBackgroundCache()
    {
        _backgroundCache?.Dispose();
        _backgroundCache = null;
        _backgroundCacheClientSize = Size.Empty;
        _backgroundCacheFormSize = Size.Empty;
        _backgroundCacheOrigin = Point.Empty;
        _backgroundCacheSource = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ClearBackgroundCache();
        }

        base.Dispose(disposing);
    }
}

internal sealed class TransparentImageProgressBar : Control
{
    private const int DefaultBarHeight = 26;
    private const int ProgressCapWidth = 14;
    private const int LeftCapSourceInset = 2;
    private const int LeftShadowSoftenWidth = 14;
    private const int LeftShadowSoftenHeight = 7;
    private int _minimum;
    private int _maximum = 100;
    private int _value;
    private Image? _trackImage;
    private Image? _fillImage;

    public TransparentImageProgressBar()
    {
        SetStyle(
            ControlStyles.UserPaint
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.SupportsTransparentBackColor,
            true);

        BackColor = Color.Transparent;
        MinimumSize = new Size(0, DefaultBarHeight);
    }

    public int Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_maximum < _minimum)
            {
                _maximum = _minimum;
            }

            Value = _value;
            Invalidate();
        }
    }

    public int Maximum
    {
        get => _maximum;
        set
        {
            _maximum = Math.Max(_minimum, value);
            Value = _value;
            Invalidate();
        }
    }

    public int Value
    {
        get => _value;
        set
        {
            var clamped = Math.Min(_maximum, Math.Max(_minimum, value));
            if (_value == clamped)
            {
                return;
            }

            _value = clamped;
            Invalidate();
        }
    }

    public Image? TrackImage
    {
        get => _trackImage;
        set
        {
            _trackImage = value;
            Invalidate();
        }
    }

    public Image? FillImage
    {
        get => _fillImage;
        set
        {
            _fillImage = value;
            Invalidate();
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        PaintFormBackground(e.Graphics);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        PaintFormBackground(e.Graphics);

        if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
        {
            return;
        }

        var barHeight = Math.Min(DefaultBarHeight, ClientSize.Height);
        var bounds = new Rectangle(0, (ClientSize.Height - barHeight) / 2, ClientSize.Width, barHeight);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var usesTrackImage = _trackImage is not null;
        if (_trackImage is { } trackImage)
        {
            DrawTrackWithMirroredLeftCap(e.Graphics, trackImage, bounds);
            SoftenLeftBottomShadow(e.Graphics, bounds);
        }
        else
        {
            using var trackBrush = new LinearGradientBrush(
                bounds,
                Color.FromArgb(226, 226, 220),
                Color.FromArgb(184, 184, 181),
                LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(trackBrush, bounds);
        }

        var range = _maximum - _minimum;
        var ratio = range <= 0 ? 0D : (double)(_value - _minimum) / range;
        var fillWidth = (int)Math.Round(bounds.Width * ratio);
        if (fillWidth > 0)
        {
            var fillBounds = new Rectangle(bounds.X, bounds.Y, fillWidth, bounds.Height);
            if (_fillImage is not null)
            {
                e.Graphics.DrawImage(_fillImage, fillBounds);
            }
            else
            {
                using var fillBrush = new LinearGradientBrush(
                    fillBounds,
                    Color.FromArgb(198, 221, 190),
                    Color.FromArgb(116, 177, 157),
                    LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(fillBrush, fillBounds);
            }
        }

        if (!usesTrackImage)
        {
            using var borderPen = new Pen(Color.FromArgb(165, 166, 158));
            e.Graphics.DrawRectangle(borderPen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        }
    }

    private static void DrawTrackWithMirroredLeftCap(Graphics graphics, Image image, Rectangle bounds)
    {
        var capWidth = Math.Min(ProgressCapWidth, Math.Min(image.Width / 2, bounds.Width / 2));
        if (capWidth <= 0)
        {
            graphics.DrawImage(image, bounds);
            return;
        }

        var rightCapSource = new Rectangle(image.Width - capWidth, 0, capWidth, image.Height);
        var leftCapSource = new Rectangle(
            Math.Max(0, image.Width - capWidth - LeftCapSourceInset),
            0,
            capWidth,
            image.Height);
        var middleSource = new Rectangle(capWidth, 0, image.Width - (capWidth * 2), image.Height);
        var leftCapTarget = new Rectangle(bounds.Left, bounds.Top, capWidth, bounds.Height);
        var middleTarget = new Rectangle(bounds.Left + capWidth, bounds.Top, bounds.Width - (capWidth * 2), bounds.Height);
        var rightCapTarget = new Rectangle(bounds.Right - capWidth, bounds.Top, capWidth, bounds.Height);

        if (leftCapTarget.Width > 0)
        {
            var flippedLeftCap = new[]
            {
                new Point(leftCapTarget.Right, leftCapTarget.Top),
                new Point(leftCapTarget.Left, leftCapTarget.Top),
                new Point(leftCapTarget.Right, leftCapTarget.Bottom),
            };

            graphics.DrawImage(image, flippedLeftCap, leftCapSource, GraphicsUnit.Pixel);
        }

        if (middleSource.Width > 0 && middleTarget.Width > 0)
        {
            graphics.DrawImage(image, middleTarget, middleSource, GraphicsUnit.Pixel);
        }

        graphics.DrawImage(image, rightCapTarget, rightCapSource, GraphicsUnit.Pixel);
    }

    private static void SoftenLeftBottomShadow(Graphics graphics, Rectangle bounds)
    {
        var softenWidth = Math.Min(LeftShadowSoftenWidth, bounds.Width);
        var softenHeight = Math.Min(LeftShadowSoftenHeight, bounds.Height);
        if (softenWidth <= 0 || softenHeight <= 0)
        {
            return;
        }

        var softenBounds = new Rectangle(
            bounds.Left,
            bounds.Bottom - softenHeight,
            softenWidth,
            softenHeight);

        using var path = new GraphicsPath();
        path.AddPolygon(new[]
        {
            new Point(softenBounds.Left, softenBounds.Top + 1),
            new Point(softenBounds.Right, softenBounds.Top + 3),
            new Point(softenBounds.Right, softenBounds.Bottom),
            new Point(softenBounds.Left, softenBounds.Bottom),
        });

        using var brush = new LinearGradientBrush(
            softenBounds,
            Color.FromArgb(160, 174, 168, 162),
            Color.FromArgb(25, 174, 168, 162),
            LinearGradientMode.Horizontal);
        graphics.FillPath(brush, path);
    }

    private void PaintFormBackground(Graphics graphics)
    {
        var form = FindForm();
        if (form?.BackgroundImage is null)
        {
            using var brush = new SolidBrush(Parent?.BackColor ?? SystemColors.Control);
            graphics.FillRectangle(brush, ClientRectangle);
            return;
        }

        var origin = form.PointToClient(PointToScreen(Point.Empty));
        graphics.DrawImage(
            form.BackgroundImage,
            new Rectangle(-origin.X, -origin.Y, form.ClientSize.Width, form.ClientSize.Height));
    }
}

internal sealed class TransparentLogBox : Control
{
    private readonly StringBuilder _buffer = new();

    public TransparentLogBox()
    {
        SetStyle(
            ControlStyles.UserPaint
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.SupportsTransparentBackColor,
            true);

        BackColor = Color.Transparent;
        ForeColor = SystemColors.WindowText;
    }

    public BorderStyle BorderStyle { get; set; } = BorderStyle.FixedSingle;
    public bool ReadOnly { get; set; } = true;
    public bool WordWrap { get; set; }
    public int SelectionStart { get; set; }
    public int TextLength => _buffer.Length;

    [AllowNull]
    public override string Text
    {
        get => _buffer.ToString();
        set
        {
            _buffer.Clear();
            _buffer.Append(value ?? string.Empty);
            Invalidate();
        }
    }

    public void AppendText(string text)
    {
        _buffer.Append(text);
        if (_buffer.Length > 200_000)
        {
            _buffer.Remove(0, _buffer.Length - 150_000);
        }

        Invalidate();
    }

    public void ScrollToCaret()
    {
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        PaintFormBackground(e.Graphics);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var bounds = ClientRectangle;
        if (BorderStyle != BorderStyle.None)
        {
            ControlPaint.DrawBorder(e.Graphics, bounds, TransparentControlChrome.BorderColor, ButtonBorderStyle.Solid);
            bounds.Inflate(-4, -4);
        }
        else
        {
            bounds.Inflate(-3, -3);
        }

        var lineHeight = Math.Max(1, TextRenderer.MeasureText(e.Graphics, "Ag", Font).Height);
        var visibleLines = Math.Max(1, bounds.Height / lineHeight);
        var lines = _buffer
            .ToString()
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
        var start = Math.Max(0, lines.Length - visibleLines - 1);
        var y = bounds.Top;

        for (var i = start; i < lines.Length && y + lineHeight <= bounds.Bottom; i++)
        {
            if (lines[i].Length > 0)
            {
                TextRenderer.DrawText(
                    e.Graphics,
                    lines[i],
                    Font,
                    new Rectangle(bounds.Left, y, bounds.Width, lineHeight),
                    ForeColor,
                    TextFormatFlags.NoPadding | TextFormatFlags.NoClipping);
            }

            y += lineHeight;
        }
    }

    private void PaintFormBackground(Graphics graphics)
    {
        var form = FindForm();
        if (form?.BackgroundImage is null)
        {
            using var brush = new SolidBrush(Parent?.BackColor ?? SystemColors.Window);
            graphics.FillRectangle(brush, ClientRectangle);
            return;
        }

        var origin = form.PointToClient(PointToScreen(Point.Empty));
        graphics.DrawImage(
            form.BackgroundImage,
            new Rectangle(-origin.X, -origin.Y, form.ClientSize.Width, form.ClientSize.Height));
    }
}
