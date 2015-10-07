namespace EvoDistroLisa.CLI

open Nessos.Argu
open System.IO
open System.Threading
open System.Drawing

module Web = 
    open Suave.Http
    open Suave.Http.Successful
    open Suave.Http.Applicatives
    open FSharp.Fx
    open Nessos.FsPickler.Json
    open EvoDistroLisa
    open EvoDistroLisa.Engine

    let pickler = FsPickler.CreateJsonSerializer()

    let savePng (agent: IAgent) = 
        let width, height = agent.Pixels.Width, agent.Pixels.Height
        let scene = agent.Best.Scene
        WpfRender.renderToPng width height scene
    
    let start token port (agent: IAgent) = 
        let imageProvider = warbler (fun _ -> agent |> savePng |> ok) >>= Writers.setMimeType "image/png"
        let jsonProvider = warbler (fun _ -> agent.Best |> pickler.Pickle |> ok) >>= Writers.setMimeType "text/json"
        let app = choose [ path "/image" >>= imageProvider; path "/json" >>= jsonProvider ]
        let loop () = Suave.Web.startWebServer Suave.Web.defaultConfig app
        Async.startThread token loop () |> ignore
        printfn "Suave listening at %d..." port

module Agent = 
    open System
    open System.Reactive.Concurrency
    open System.Windows.Media.Imaging
    open System.Windows.Media
    open FSharp.Control.Reactive
    open EvoDistroLisa
    open EvoDistroLisa.UI
    open EvoDistroLisa.Domain
    open EvoDistroLisa.Engine
    open EvoDistroLisa.Engine.ZMQ

    let defaultPort = 5801

    let attachAgents token agents (master: IAgent) = 
        let pixels = master.Pixels
        let best = master.Best
        { 1..agents } |> Seq.iter (fun index ->
            let mutator = Agent.createMutator ()
            let renderer = 
                match index with
                | 1 -> Win32Fitness.createRendererFactory (agents > 1)
                | _ -> WBxFitness.createRendererFactory ()
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
            let restart = arguments.TryGetResult(<@ Restart @>) |> Option.map Restart
            let resume = arguments.TryGetResult(<@ Resume @>) |> Option.map Resume
            match connect, resume, restart with
            | None, None, Some c -> c
            | None, Some r, None -> r
            | Some r, None, None -> r
            | _ -> failwith "One and only one of --restart, --resume or --connect is required"

        let agents = arguments.GetResult(<@ Agents @>, 1) |> max 0
        let gui = arguments.Contains(<@ Gui @>)
        let listen = arguments.TryGetResult(<@ Listen @>)
        let suave = arguments.TryGetResult(<@ Suave @>)
        let token = CancellationToken.None

        let clientAgent = 
            match mode with
            | Connect (h, p) -> 
                printfn "Connecting to %s:%d..." h p
                ZmqClient.createClient h p token |> Some
            | _ -> None

        let { Pixels = pixels; Scene = best } = 
            match mode, clientAgent with
            | Restart fn, _ -> { Pixels = Image.FromFile(fn) |> Win32Fitness.createPixels; Scene = RenderedScene.Zero }
            | Resume fn, _ -> File.ReadAllBytes(fn) |> Pickler.load<BootstrapScene>
            | Connect _, Some agent -> { Pixels = agent.Pixels; Scene = agent.Best }
            | Connect _, None -> failwith "Failed to create clientAgent"
            | _ -> failwithf "Unhandled mode: %A" mode

        let serverAgent =
            match listen with
            | Some p -> 
                printfn "Listening at %d..." p
                ZmqServer.createServer p pixels best token |> Some
            | _ -> None

        let agent =
            match clientAgent, serverAgent with
            | Some a, _ -> a
            | _, Some a -> a
            | _ -> Agent.createPassiveAgent pixels best token

        match mode with
        | Restart fn -> sprintf "%s.evoboot" fn |> Some
        | Resume fn -> fn |> Some
        | _ -> None
        |> Option.map (fun fn ->
            let bootstrap = { Pixels = agent.Pixels; Scene = agent.Best }
            agent.Improved
            |> Observable.sample (TimeSpan.FromSeconds(15.0))
            |> Observable.map (fun scene -> { bootstrap with Scene = scene })
            |> Observable.subscribe (fun scene -> 
                File.WriteAllBytes(fn, scene |> Pickler.save)))
        |> ignore

        let speed = 
            Observable.interval (TimeSpan.FromSeconds(1.0))
            |> Observable.map (fun _ -> agent.Mutations)
            |> Observable.slidingWindow (TimeSpan.FromSeconds(5.0)) 
            |> Observable.choose (fun tsm ->
                let length = tsm.Length
                if length <= 1 then None
                else
                    let first, last = tsm.[0], tsm.[length - 1]
                    let time = last.Timestamp.Subtract(first.Timestamp)
                    let diff = last.Value - first.Value
                    (float diff / time.TotalSeconds) |> Some)
            |> Observable.subscribe (fun s ->
                printfn "Speed: %d/s" (int s))

        // !!!

        agent |> attachAgents token agents |> ignore

        match gui with | true -> agent |> attachGui token | _ -> ()
        match suave with | Some p -> agent |> Web.start token p | _ -> ()

        while true do 
            printf "."
            Thread.Sleep(10000)

module Program = 
    open System
    open System.Reflection
    open System.IO

    let startHelp () =
        let exeName = Path.GetFileName(Assembly.GetExecutingAssembly().CodeBase)
        printfn "Syntax: %s [options...]" exeName
        printfn "See: '%s --help' for details" exeName

    [<EntryPoint>]
    let main (argv: string[]) = 
        try
            UI.startup ()
            try
                match argv |> List.ofArray with
                | [] -> startHelp ()
                | args -> Agent.start args
                0
            finally
                UI.shutdown ()
        with e -> e |> printfn "%A"; -1
