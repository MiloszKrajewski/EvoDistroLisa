namespace EvoDistroLisa.Engine.ZMQ

module ZmqClient = 
    open System.Threading
    open fszmq
    open FSharp.Fx
    open EvoDistroLisa.Domain.Scene
    open EvoDistroLisa.Engine
    open EvoDistroLisa.Engine.ZMQ.Message

    type private Message = EvoDistroLisa.Engine.ZMQ.Message.Message

    let createClient 
            addr port 
            (token: CancellationToken) = 

        let zmq = new ZMQLoop(token = token)
        let ctx = zmq.Context

        let reqSocket =
            let socket = Context.req ctx
            Socket.connect socket (sprintf "tcp://%s:%d" addr port)
            zmq.RegisterSocket(socket)
        let reqHandler = zmq.CreateRequester(reqSocket, decode, encode)

        let subSocket, pushSocket, bootstrap =
            let response = Handshake |> reqHandler |> Async.RunSynchronously
            let subPort, pushPort, bootstrap =
                match response with
                | Bootstrap (sub, push, bootstrap) -> (sub, push, bootstrap)
                | m -> m |> sprintf "Unexpected message: %A, cannot initialize client" |> trace |> failwith
            let subSocket = Context.sub ctx
            let pushSocket = Context.push ctx
            Socket.connect subSocket (sprintf "tcp://%s:%d" addr subPort)
            Socket.connect pushSocket (sprintf "tcp://%s:%d" addr pushPort)
            
            zmq.RegisterSocket(subSocket), zmq.RegisterSocket(pushSocket), bootstrap
        let { Pixels = pixels; Scene = best } = bootstrap

        let agent = Agent.createPassiveAgent pixels best token

        let subHandler message = 
            match message with
            | Improved scene -> agent.Push(scene)
            | m -> m |> sprintf "Unexpected message: %A, ignoring" |> trace |> ignore
        do zmq.CreateReceiver(subSocket, decode, subHandler)

        let pushHandler = zmq.CreateSender(pushSocket, encode)

        agent.Mutated |> Observable.subscribe (Mutated >> pushHandler) |> ignore
        agent.Improved |> Observable.subscribe (Improved >> pushHandler) |> ignore

        agent
