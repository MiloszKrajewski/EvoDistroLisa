namespace EvoDistroLisa.Engine

module Win32Fitness =
    open System.Drawing
    open System.Drawing.Imaging
    open FSharp.Fx
    open EvoDistroLisa.Engine.Unsafe
    open EvoDistroLisa.Domain
    open EvoDistroLisa.Domain.Scene
    open FSharp.Collections.ParallelSeq

    let private format = PixelFormat.Format32bppPArgb

    let private enforceFormat (image: Image) =
        match image with
        | :? Bitmap as bitmap when bitmap.PixelFormat = format ->
            bitmap
        | _ ->
            let width, height = image.Width, image.Height
            let bitmap = new Bitmap(width, height, format)
            use graphics = Graphics.FromImage(bitmap)
            graphics.DrawImage(image, 0, 0, width, height)
            bitmap

    let private lockBits mode func (bitmap: Bitmap) =
        let width, height = bitmap.Width, bitmap.Height
        let rect = Rectangle(0, 0, width, height)
        let data = bitmap.LockBits(rect, mode, format)
        try 
            func data 
        finally 
            bitmap.UnlockBits(data)

    let private bitmapToBytes (bitmap: Bitmap) =
        let width, height = bitmap.Width, bitmap.Height
        let buffer = Array.zeroCreate<uint32> (width*height)
        bitmap |> lockBits ImageLockMode.ReadOnly (fun data -> 
            assert (data.Stride = width*sizeof<uint32>)
            let cloneRowBits y = Buffer.Copy(data.Scan0, y*width, buffer, y*width, width)
            { 0 .. height - 1 } |> Seq.piter cloneRowBits)
        buffer

    let private fitRgb32Row (original: uint32[]) (rendered: nativeint) width y =
        let sumdev = Fitness.SumDev(original, y * width, rendered, y * width, width)
        let maxdev = 255uL*255uL*3uL*(uint64 width)
        decimal sumdev / decimal maxdev

    let private parallelFitRgb32Image height width (original: uint32[]) (rendered: BitmapData) =
        { 0 .. height - 1 } 
        |> PSeq.averageBy (fitRgb32Row original rendered.Scan0 width)

    let private serialFitRgb32Image (original: uint32[]) (rendered: BitmapData) =
        fitRgb32Row original rendered.Scan0 original.Length 0

    let createPixels (original: Image) =
        let width, height = original.Width, original.Height
        let pixels = original |> enforceFormat |> bitmapToBytes
        { Width = width; Height = height; Pixels = pixels }

    let createRenderer serial (original: Pixels) =
        let { Width = width; Height = height; Pixels = pixels } = original
        let fitter = (if serial then serialFitRgb32Image else parallelFitRgb32Image height width) pixels
        let target = new Bitmap(width, height, format)
        let fitness (scene: Scene) = 
            Win32Renderer.render target scene
            target |> lockBits ImageLockMode.ReadOnly (fun data ->
                let distance = data |> fitter
                1m - distance)
        fitness

    let createRendererFactory serial = createRenderer serial
