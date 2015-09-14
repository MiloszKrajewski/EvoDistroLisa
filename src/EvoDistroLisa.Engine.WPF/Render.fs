namespace EvoDistroLisa.Engine

module WpfRender = 
    open System.Windows
    open System.Windows.Media
    open System.Windows.Media.Imaging
    open EvoDistroLisa.Domain.Point
    open EvoDistroLisa.Domain.Polygon
    open EvoDistroLisa.Domain.Scene

    let inline private toByte v = (float v |> max 0.0 |> min 1.0) * 255.0 |> round |> byte

    let inline private createRect (x, y, w, h) = Rect(float x, float y, float w, float h)
    let inline private createPoint (x, y) = Point(float x, float y)
    let inline private createColor (a, r, g, b) = Color.FromArgb(toByte a, toByte r, toByte g, toByte b)
    let inline private createBrush color = SolidColorBrush(color)
    let inline private createPen thickness brush = Pen(brush, float thickness)

    let private renderPolygon (rect: Rect) (ctx: DrawingContext) (polygon: Polygon) =
        let brush = 
            let b = polygon.Brush
            (b.A, b.R, b.G, b.B) |> createColor |> createBrush
        let pen = brush |> createPen 0

        let inline toPoint { X = x; Y = y } = 
            (x * rect.Width, y * rect.Height) |> createPoint

        let start = polygon.Points.[0] |> toPoint
        let points = polygon.Points |> Seq.skip 1 |> Seq.map toPoint
        let segments = PolyLineSegment(points, false) :> PathSegment |> Seq.singleton
        let figures = PathFigure(start, segments, true) |> Seq.singleton
        let geometry = PathGeometry(figures)

        ctx.DrawGeometry(brush, pen, geometry)

    let private renderScene (extent: Rect) (ctx: DrawingContext) (scene: Scene) =
        // let zeroBrush = (1, 0.5, 0.5, 0.5) |> createColor |> createBrush
        let zeroBrush = (1, 0, 0, 0) |> createColor |> createBrush
        let zeroPen = zeroBrush |> createPen 0
        ctx.DrawRectangle(zeroBrush, zeroPen, extent)
        scene.Polygons |> Seq.iter (renderPolygon extent ctx)

    let render (bitmap: RenderTargetBitmap) (scene: Scene) = 
        let vis = new DrawingVisual()
        using (vis.RenderOpen()) (fun ctx -> 
            let extent = (0, 0, bitmap.Width, bitmap.Height) |> createRect
            scene |> renderScene extent ctx)
        bitmap.Render(vis)
        bitmap
