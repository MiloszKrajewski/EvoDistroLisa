﻿namespace EvoDistroLisa.CLI

open Nessos.Argu
open System.IO
open System.Threading
open System.Drawing

module Agent = 
    open EvoDistroLisa.Domain
    open EvoDistroLisa.Engine

    let attachAgents token agents (master: IAgent) = 
        let pixels = master.Pixels
        let best = master.Best
        { 1..agents } |> Seq.iter (fun index ->
            let mutator = Agent.createMutator ()
            let renderer = 
                match index with
                | 1 -> Win32Fitness.createRendererFactory (agents > 1)
                | _ -> WpfFitness.createRendererFactory ()
            let agent = Agent.createAgent pixels mutator renderer best token
            master |> Agent.attachAgent agent 
            agent.Push(best)
            printfn "Agent(%d) started..." index
        )
        master
    
    let attachGui token agent = ()

module Server = 
    open EvoDistroLisa
    open EvoDistroLisa.Engine
    open EvoDistroLisa.Engine.ZMQ
    open EvoDistroLisa.Domain.Scene

    type Arguments = 
        | Listen of port: int
        | Restart of string
        | Resume of string
        | Agents of int
        | Gui
        interface IArgParserTemplate with
            member x.Usage = 
                match x with
                | Listen _ -> "the port to listen (as server)"
                | Agents _ -> "number of agents"
                | Restart _ -> "restart processing (requires image file)"
                | Resume _ -> "resume processing (requires bootstrap file)"
                | Gui -> "open GUI window"

    let start argv =
        let parser = ArgumentParser.Create<Arguments>()
        let arguments = parser.Parse(argv |> Array.ofSeq)
        let port = arguments.GetResult(<@ Listen @>, 5801)
        
        let mode = 
            match arguments.Contains(<@ Restart @>), arguments.Contains(<@ Resume @>) with
            | true, false -> arguments.GetResult(<@ Restart @>) |> Restart
            | false, true -> arguments.GetResult(<@ Resume @>) |> Resume
            | _ -> failwith "Server requires either --restart or --resume specified, but not both"

        let { Pixels = pixels; Scene = best } = 
            match mode with
            | Restart fn -> { Pixels = Image.FromFile(fn) |> Win32Fitness.createPixels; Scene = initialScene }
            | Resume fn -> File.ReadAllBytes(fn) |> Pickler.load<BootstrapScene>
            | _ -> mode |> sprintf "Unexpected mode: %A" |> failwith

        let agents = arguments.GetResult(<@ Agents @>, 0)
        let gui = arguments.Contains(<@ Gui @>)

        let token = CancellationToken.None

        let agent = ZmqServer.createServer port pixels best token 
        agent |> Agent.attachAgents token agents |> ignore
        if gui then agent |> Agent.attachGui token 

        printfn "Listening at %d..." port

        while true do 
            printf "."
            Thread.Sleep(1000)

module Client = 
    open EvoDistroLisa.Engine.ZMQ

    type Arguments = 
        | Connect of host: string * port: int
        | Agents of int
        | Gui
        interface IArgParserTemplate with
            member x.Usage = 
                match x with
                | Connect _ -> "the host and port to connect to (as client)"
                | Agents _ -> "number of agents"
                | Gui -> "open GUI window"

    let start argv =
        let parser = ArgumentParser.Create<Arguments>()
        let arguments = parser.Parse(argv |> Array.ofSeq)
        let host, port = arguments.GetResult(<@ Connect @>, ("127.0.0.1", 5801))
        let agents = arguments.GetResult(<@ Agents @>, 0)
        let gui = arguments.Contains(<@ Gui @>)
        let token = CancellationToken.None

        let agent = ZmqClient.createClient host port token
        agent |> Agent.attachAgents token agents |> ignore
        if gui then agent |> Agent.attachGui token
        printfn "Connected to %s:%d..." host port

        while true do 
            printf "."
            Thread.Sleep(1000)

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
            match argv |> List.ofSeq with
            | "server" :: t -> Server.start t
            | "client" :: t -> Client.start t
            | l -> startHelp l
            0
        with 
        | e -> e |> printfn "%A"; -1