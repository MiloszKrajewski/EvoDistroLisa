namespace EvoDistroLisa.Engine.Tests

module Renderers =
    open EvoDistroLisa.Domain
    open EvoDistroLisa.Engine
    open System.Drawing
    open System.Drawing.Imaging
    open System.Windows.Media.Imaging
    open System.Windows.Media
    open System.IO
    open Xunit

    let red = { A = 0.4; R = 1.0; G = 0.0; B = 0.0 }
    let points = 
        [ (0.2, 0.1); (0.1, 0.9); (0.9, 0.4) ] 
        |> Seq.map (fun (x, y) -> { X = x; Y = y })
        |> Array.ofSeq
    let triangle = { Polygons = [| { Brush = red; Points = points } |] }

    let saveGDI filename (bitmap: Bitmap) = 
        bitmap.Save(filename)

    let renderGDI scene = 
        let bitmap = new Bitmap(200, 200, PixelFormat.Format32bppPArgb)
        Win32Renderer.render bitmap scene
        bitmap

    let saveWPF filename (bitmap: BitmapSource) =
        let encoder = PngBitmapEncoder()
        use stream = File.Create(filename)
        encoder.Frames.Add(BitmapFrame.Create(bitmap))
        encoder.Save(stream)

    let renderWPF scene = 
        let bitmap = BitmapFactory.New(200, 200)
        WBxRender.render bitmap scene

    [<Fact>]
    let sceneCanBeRenderedUsingGDI () =
        triangle |> renderGDI |> saveGDI "gdi.png"

    [<Fact>]
    let sceneCanBeRenderedUsingWBx () =
        triangle |> renderWPF |> saveWPF "wpf.png"
