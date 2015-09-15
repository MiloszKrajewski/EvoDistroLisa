namespace EvoDistroLisa.Engine

module Win32Renderer =
    open System.Drawing
    open EvoDistroLisa.Domain.Point
    open EvoDistroLisa.Domain.Polygon
    open EvoDistroLisa.Domain.Scene

    let inline private toByte v = (float v |> max 0.0 |> min 1.0) * 255.0 |> round |> int

    let inline private createPoint (x, y) = Point(int x, int y)
    let inline private createRect (x, y, w, h) = Rectangle(int x, int y, int w, int h)

    let inline private createColor (a, r, g, b) = Color.FromArgb(toByte a, toByte r, toByte g, toByte b)
    let inline private createBrush color = new SolidBrush(color)

    let private zeroColor = (1, 0.5, 0.5, 0.5) |> createColor

    let renderPolygon (extent: Rectangle) (ctx: Graphics) (polygon: Polygon) =
        use brush = 
            let b = polygon.Brush
            (b.A, b.R, b.G, b.B) |> createColor |> createBrush
        let w, h = extent.Width, extent.Height
        let inline toCoord m v = v * (float m) |> round |> int
        let inline toPoint { X = x; Y = y } = 
            (x |> toCoord w, y |> toCoord h) |> createPoint
        let points = polygon.Points |> Array.map toPoint
        ctx.FillPolygon(brush, points)

    let renderScene (extent: Rectangle) (ctx: Graphics) (scene: Scene) =
        use zeroBrush = zeroColor |> createBrush
        ctx.FillRectangle(zeroBrush, extent)
        scene.Polygons |> Seq.iter (renderPolygon extent ctx)

    let render (bitmap: Bitmap) (scene: Scene) =
        using (Graphics.FromImage(bitmap)) (fun ctx ->
            let extent = (0, 0, bitmap.Width, bitmap.Height) |> createRect
            scene |> renderScene extent ctx)
