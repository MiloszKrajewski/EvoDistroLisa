namespace EvoDistroLisa.Server

open Nessos.Argu

type Arguments = 
    | Port of int
    | Restart of string
    | Resume of string
    interface IArgParserTemplate with
        member x.Usage = 
            match x with
            | Port _ -> "server port"
            | Restart _ -> "image filename to restart evolution"
            | Resume _ -> "bootstrap filename to resume evolution"

module Program = 
    open System.Threading
    open EvoDistroLisa.Engine.ZMQ
    open EvoDistroLisa.Engine
    open System.Drawing
    open EvoDistroLisa
    open EvoDistroLisa.Domain.Scene
    open System.IO

    [<EntryPoint>]
    let main argv = 
        let parser = ArgumentParser.Create<Arguments>()
        let arguments = parser.Parse(argv)
        let port = arguments.GetResult(<@ Port @>, defaultValue = 5801)
        
        let mode = 
            match arguments.Contains(<@ Restart @>), arguments.Contains(<@ Resume @>) with
            | true, false -> arguments.GetResult(<@ Restart @>) |> Restart
            | false, true -> arguments.GetResult(<@ Resume @>) |> Resume
            | _ -> failwith "Hub requires either --restart or --resume specified, but not both"

        let { Pixels = pixels; Scene = best } = 
            match mode with
            | Restart fn -> { Pixels = Image.FromFile(fn) |> Win32Fitness.createPixels; Scene = initialScene }
            | Resume fn -> File.ReadAllBytes(fn) |> Pickler.load<BootstrapScene>
            | _ -> mode |> sprintf "Unexpected mode: %A" |> failwith

        let token = CancellationToken.None

        ZmqServer.createServer port pixels best token |> ignore
        printfn "EvoDistoLisa serving @ %d..." port
        while true do Thread.Sleep(1000)

        0 // return an integer exit code
