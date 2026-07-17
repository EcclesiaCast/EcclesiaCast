namespace EcclesiaCast.App.Services;

/// <summary>
/// Geometry for the draggable text box editor: resizing from any of the 8
/// handles (corners + edge midpoints), clamped inside the canvas. Works in
/// the canvas' own coordinate space (e.g. 960×540).
/// </summary>
public static class BoxGeometry
{
    public const double MinWidth = 60;
    public const double MinHeight = 40;

    public static (double X, double Y, double W, double H) Resize(
        string handle, double x, double y, double w, double h,
        double dx, double dy, double canvasW, double canvasH)
    {
        var left = handle is "NW" or "W" or "SW";
        var right = handle is "NE" or "E" or "SE";
        var top = handle is "NW" or "N" or "NE";
        var bottom = handle is "SW" or "S" or "SE";

        if (left)
        {
            var nx = x + dx;
            var nw = w - dx;
            if (nw < MinWidth) { nx = x + w - MinWidth; nw = MinWidth; }
            if (nx < 0) { nw += nx; nx = 0; }
            x = nx; w = nw;
        }
        else if (right)
        {
            w = Math.Max(MinWidth, w + dx);
            if (x + w > canvasW) w = canvasW - x;
        }

        if (top)
        {
            var ny = y + dy;
            var nh = h - dy;
            if (nh < MinHeight) { ny = y + h - MinHeight; nh = MinHeight; }
            if (ny < 0) { nh += ny; ny = 0; }
            y = ny; h = nh;
        }
        else if (bottom)
        {
            h = Math.Max(MinHeight, h + dy);
            if (y + h > canvasH) h = canvasH - y;
        }

        return (x, y, w, h);
    }
}
