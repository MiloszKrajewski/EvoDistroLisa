﻿namespace EvoDistroLisa.CLI

open Nessos.Argu
open System.IO
open System.Threading
open System.Drawing

module Agent = 
    open EvoDistroLisa.UI
    open EvoDistroLisa.Domain
    open EvoDistroLisa.Engine
    open FSharp.Control.Reactive
    open System
    open System.Reactive.Concurrency
    open System.Windows.Media.Imaging
    open System.Windows.Media
    open EvoDistroLisa.Domain.Scene

    let attachAgents token agents (master: IAgent) = 
        let pixels = master.Pixels
        let best = master.Best
        { 1..agents } |> Seq.iter (fun index ->
            let mutator = Agent.createMutator ()
            let renderer = 
                match index with
                // | 1 -> Win32Fitness.createRendererFactory (agents > 1)
                | _ -> WpfFitness.createRendererFactory ()
            let agent = Agent.createAgent pixels mutator renderer best token
            master |> Agent.attachAgent agent 
            agent.Push(best)
            printfn "Agent(%d) started..." index
        )
        master
    
    let attachGui (token: CancellationToken) (agent: IAgent) = 
        UI.invoke (fun () -> 
            let pixels = agent.Pixels
            let width, height = pixels.Width, pixels.Height
            let dispatcher = DispatcherScheduler.Current
            let fps = 10.0
            let sampling = TimeSpan.FromSeconds(1.0/fps)
            let window = ImageViewer()
            let target = RenderTargetBitmap(width, height, 96.0, 96.0, PixelFormats.Pbgra32)
            window.DataContext <- window
            window.Show()

            agent.Best
            |> Observable.single
            |> Observable.merge (agent.Improved |> Observable.sample sampling)
            |> Observable.observeOn dispatcher
            |> Observable.subscribe (fun scene ->
                let fitness = scene.Fitness
                let scene = scene.Scene
                window.Image <- WpfRender.render target scene
                window.Title <- sprintf "%g" fitness)
            |> ignore
        )

        agent.Improved
        |> Observable.sample (TimeSpan.FromSeconds(1.0))
        |> Observable.subscribe (fun scene ->
            let polygons = scene.Scene.Polygons.Length
            let mutations = agent.Mutations
            printfn "Polygons: %d; Mutations: %d" polygons mutations)
        |> ignore

    let saveJpegToBuffer (agent: IAgent) = 
        let target = 
            RenderTargetBitmap(agent.Pixels.Width, agent.Pixels.Height, 96.0, 96.0, PixelFormats.Pbgra32)
        WpfRender.render target agent.Best.Scene |> ignore
        let encoder = JpegBitmapEncoder()
        use stream = new MemoryStream()
        encoder.Frames.Add(BitmapFrame.Create(target))
        encoder.Save(stream)
        stream.ToArray()

module Web = 
    open Suave.Http
    open Suave.Http.Successful
    open Suave.Http.Applicatives
    open FSharp.Fx
    open EvoDistroLisa.Domain
    open Nessos.FsPickler.Json

    let pickler = FsPickler.CreateJsonSerializer()
    
    let start token port (agent: IAgent) = 
        let imageProvider = warbler (fun _ -> agent |> Agent.saveJpegToBuffer |> ok) >>= Writers.setMimeType "image/jpeg"
        let jsonProvider = warbler (fun _ -> agent.Best |> pickler.Pickle |> ok) >>= Writers.setMimeType "text/json"
        let app = choose [ path "/image" >>= imageProvider; path "/json" >>= jsonProvider ]
        let loop () = Suave.Web.startWebServer Suave.Web.defaultConfig app
        Async.startThread token loop () |> ignore
        printfn "Suave listening at %d..." port

module Server = 
    let start argv =
        let parser = ArgumentParser.Create<Arguments>()
        let arguments = parser.Parse(argv |> Array.ofSeq)
        
        let { Pixels = pixels; Scene = best } = 
            match mode with
            | Restart fn -> { Pixels = Image.FromFile(fn) |> Win32Fitness.createPixels; Scene = RenderedScene.Zero }
            | Resume fn -> File.ReadAllBytes(fn) |> Pickler.load<BootstrapScene>
            | _ -> mode |> sprintf "Unexpected mode: %A" |> failwith

        let agents = arguments.GetResult(<@ Agents @>, 0)
        let gui = arguments.Contains(<@ Gui @>)
        let suave = 
            match arguments.Contains(<@ Suave @>) with 
            | true -> arguments.GetResult(<@ Suave @>) |> Some 
            | _ -> None

        let token = CancellationToken.None

        let agent = ZmqServer.createServer port pixels best token
        agent |> Agent.attachAgents token agents |> ignore

        printfn "Agent listening at %d..." port

        if gui then 
            agent |> Agent.attachGui token

        if suave.IsSome then
            agent |> Web.start token suave.Value

        while true do 
            printf "."
            Thread.Sleep(10000)

module Client = 
    open EvoDistroLisa.Engine.ZMQ
    open EvoDistroLisa.Domain.Scene

    type Arguments = 
        | Connect of host: string * port: int
        | Restart of string
        | Resume of string
        | Agents of int
        | Gui
        | Listen of port: int
        | Suave of port: int
        interface IArgParserTemplate with
            member x.Usage = 
                match x with
                | Connect _ -> "the host and port to connect to (as client)"
                | Restart _ -> "restart processing (requires image file)"
                | Resume _ -> "resume processing (requires bootstrap file)"
                | Agents _ -> "number of agents"
                | Gui -> "open GUI window"
                | Listen _ -> "the port to listen (as server)"
                | Suave _ -> "the port to listen for HTTP requests"

    let start argv =
        let parser = ArgumentParser.Create<Arguments>()
        let arguments = parser.Parse(argv |> Array.ofSeq)

        let mode = 
            let connect = arguments.TryGetResult(<@ Connect @>) |> Option.map Connect
            let resume = arguments.TryGetResult(<@ Resume @>) |> Option.map Resume
            let restart = arguments.TryGetResult(<@ Restart @>) |> Option.map Restart
            match connect, resume, restart with
            | None, None, Some c -> c
            | None, Some r, None -> r
            | Some r, None, None -> r
            | _ -> failwith "One and only one of --restart, --resume or --connect is required"

        let listen = arguments.TryGetResult(<@ Listen @>)
        let agents = arguments.GetResult(<@ Agents @>, 0)
        let gui = arguments.Contains(<@ Gui @>)
        let token = CancellationToken.None

        let agent = ZmqClient.createClient connectHost connectPort token
        agent |> Agent.attachAgents token agents |> ignore
        if gui then agent |> Agent.attachGui token
        printfn "Connected to %s:%d..." connectHost connectPort

        while true do 
            printf "."
            Thread.Sleep(10000)

module Program = 
    open System
    open System.Reflection
    open System.IO

    let startHelp argv =
        let exeName = Path.GetFileName(Assembly.GetExecutingAssembly().CodeBase)
        printfn "Syntax: %s <command> [options...]" exeName
        printfn "Commands:"
        printfn "  server: create server"
        printfn "  client: create client"
        printfn "See: '%s <command> --help' for details" exeName

    [<EntryPoint>]
    let main (argv: string[]) = 
        try
            UI.startup ()
            try
                match argv |> List.ofSeq with
                | "server" :: t -> Server.start t
                | "client" :: t -> Client.start t
                | l -> startHelp l
                0
            finally
                UI.shutdown ()
        with e -> e |> printfn "%A"; -1
