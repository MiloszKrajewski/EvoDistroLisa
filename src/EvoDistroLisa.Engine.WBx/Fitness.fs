#nowarn "9"

namespace EvoDistroLisa.Engine

module WBxFitness =
    open System
    open System.Windows.Threading
    open System.Windows.Media
    open System.Windows.Media.Imaging
    open FSharp.Fx
    open EvoDistroLisa.Domain
    open EvoDistroLisa.Engine.Unsafe
    open Microsoft.FSharp.NativeInterop

    let private format = PixelFormats.Pbgra32

    let private fitPbgraImage height width (original: uint32[]) (rendered: WriteableBitmap) =
        let length = width*height
        assert (rendered.PixelWidth * rendered.PixelHeight = length)
        assert (original.Length = length)
        use context = rendered.GetBitmapContext()
        let sumdev = Fitness.SumDev(original, 0, context.Pixels |> NativePtr.toNativeInt, 0, length)
        let maxdev = 255uL*255uL*3uL*(uint64 length)
        decimal sumdev / decimal maxdev

    let sync (image: #DispatcherObject) func =
        image.Dispatcher.Invoke(Func<_>(fun () -> func image))

    let private enforceFormat (image: BitmapSource) = 
        match image.Format with
        | f when f = format -> image
        | _ -> FormatConvertedBitmap(image, format, null, 0.0) :> BitmapSource

    let private bitmapToBytes (bytes: uint32[] option) (image: BitmapSource) =
        assert (image.Format = format)
        let height, width = image.PixelHeight, image.PixelWidth
        let bytes = 
            match bytes with
            | Some b -> assert (height*width = b.Length); b
            | None -> Array.zeroCreate<uint32> (height * width)
        let stride = width * sizeof<uint32>
        image.CopyPixels(bytes, stride, 0)
        bytes

    let createPixels (original: BitmapSource) = 
        let width, height = original.PixelWidth, original.PixelHeight
        let pixels = sync original (fun original ->
            original |> enforceFormat |> bitmapToBytes None)
        { Width = width; Height = height; Pixels = pixels }

    let createRenderer (original: Pixels) =
        let { Width = width; Height = height; Pixels = sourcePixels } = original
        let bitmapFactory = threadref (fun () -> BitmapFactory.New(width, height))
        let fitness (scene: Scene) =
            let targetBitmap = bitmapFactory ()
            let distance = 
                scene 
                |> WBxRender.render targetBitmap 
                |> fitPbgraImage height width sourcePixels
            1m - distance
        fitness

    let createRendererFactory () = createRenderer
