namespace EvoDistroLisa.Engine

module WBxRender = 
    open System.Windows
    open System.Windows.Media
    open System.Windows.Media.Imaging
    open EvoDistroLisa.Domain.Point
    open EvoDistroLisa.Domain.Polygon
    open EvoDistroLisa.Domain.Scene

    let inline private toByte v = (float v |> max 0.0 |> min 1.0) * 255.0 |> round |> byte
    let inline private createColor (a, r, g, b) = Color.FromArgb(toByte a, toByte r, toByte g, toByte b)
    let inline private copyPoint width height (points: int[]) index { X = x; Y = y } = 
        let index = index >>> 2
        points.[index + 0] <- x * (float (width - 1)) |> round |> int
        points.[index + 1] <- y * (float (height - 1)) |> round |> int

    let private renderPolygon width height (bmp: WriteableBitmap) (polygon: Polygon) =
        let brush = 
            let b = polygon.Brush
            (b.A, b.R, b.G, b.B) |> createColor
        let points = Array.zeroCreate (polygon.Points.Length * 2)
        polygon.Points |> Array.iteri (copyPoint width height points)
        bmp.FillPolygon(points, brush)

    let private renderScene width height (bmp: WriteableBitmap) (scene: Scene) =
        let zeroBrush = (1, 0.5, 0.5, 0.5) |> createColor
        bmp.FillRectangle(0, 0, width, height, zeroBrush)
        scene.Polygons |> Seq.iter (renderPolygon width height bmp)

    let render (bitmap: WriteableBitmap) (scene: Scene) = 
        let width, height = bitmap.PixelWidth, bitmap.PixelHeight
        scene |> renderScene width height bitmap
        bitmap
