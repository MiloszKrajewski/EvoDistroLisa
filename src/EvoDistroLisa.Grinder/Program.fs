namespace EvoDistroLisa.Grinder

open Nessos.Argu

type Arguments = 
    | Server of host:string * port:int
    | Agents of int
    interface IArgParserTemplate with
        member x.Usage = 
            match x with
            | Server _ -> "specify server address"
            | Agents _ -> "specify number of agents"

module Program = 
    open System.Threading
    open EvoDistroLisa.Engine.ZMQ
    open EvoDistroLisa.Engine

    [<EntryPoint>]
    let main argv = 
        let parser = ArgumentParser.Create<Arguments>()
        let arguments = parser.Parse(argv)
        let agents = arguments.GetResult(<@ Agents @>, defaultValue = 1)
        let host, port = arguments.GetResult(<@ Server @>, defaultValue = ("127.0.0.1", 5801))
        let token = CancellationToken.None

        printfn "Grinder connecting to %s:%d..." host port

        let agentZ = ZmqClient.createClient host port token
        let pixels = agentZ.Pixels
        let best = agentZ.Best

        { 1..agents } |> Seq.iter (fun index ->
            let mutator = Agent.createMutator ()
            let renderer = 
                match index with
                | 1 -> Win32Fitness.createRendererFactory (agents > 1)
                | _ -> WpfFitness.createRendererFactory ()
            let agent = Agent.createAgent pixels mutator renderer best token
            Agent.attachAgent agent agentZ
            agent.Push(best)
            printfn "Agent(%d) started..." index
        )

        while true do Thread.Sleep(1000)

        0 // return an integer exit code
