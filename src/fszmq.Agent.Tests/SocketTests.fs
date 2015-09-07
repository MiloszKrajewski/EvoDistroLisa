namespace fszmq.Agent.Tests

module SocketTests = 
    open Xunit
    open fszmq
    open System
    open System.Net.NetworkInformation

    [<Fact>]
    let BindToSamePortTwiceFails () =
        (fun () ->
            use context = new Context()
            use socket1 = Context.rep context
            use socket2 = Context.rep context

            Socket.bind socket1 "tcp://*:56789"

            Assert.Throws<ZMQError>(fun () -> Socket.bind socket2 "tcp://*:56789")
        ) ()

    [<Fact>]
    let BindToSamePortTwiceUsingBitToAnyFailsAsWell () =
        (fun () ->
            use context = new Context()
            use socket1 = Context.rep context
            use socket2 = Context.rep context

            Socket.bind socket1 "tcp://*:56789"

            let ports = 
                IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners()
                |> Seq.map (fun ep -> ep.Port)
                |> Seq.toList

            Assert.Contains(56789, ports)

            Assert.Throws<AggregateException>(fun () -> 
                Socket.bindToAny socket2 (sprintf "tcp://*:%d") [80; 56789] |> ignore
            )
        ) ()

    [<Fact>]
    let Create100RandomSockets () =
        (fun () -> 
            use context = new Context()

            let sockets = 
                { 1..100 } |> Seq.map (fun _ ->
                    let socket = Context.rep context
                    let port = 
                        Socket.randomTcpPort 50000 51000
                        |> Socket.bindToAny socket (sprintf "tcp://*:%d")
                    socket, port)
                |> List.ofSeq

            sockets
            |> Seq.map snd
            |> Seq.forall (fun p -> p >= 50000 && p <= 51000)
            |> Assert.True

            sockets
            |> Seq.map fst
            |> Seq.cast<IDisposable>
            |> Seq.iter (fun s -> s.Dispose())
        ) ()

