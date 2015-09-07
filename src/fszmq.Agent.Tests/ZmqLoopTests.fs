namespace fszmq.Agent.Tests

module ZmqLoopTests =
    open Xunit
    open fszmq
    open System
    open System.Threading
    open System.Text

    let scope func = func ()
    let encode (text: string) = [| Encoding.UTF8.GetBytes(text) |]
    let decode (msg: byte[][]) = 
        match msg with
        | [| bytes |] -> Encoding.UTF8.GetString(bytes)
        | _ -> invalidArg "msg" "Unrecognized message type"

    [<Fact>]
    let CanBeCreated () =
        scope (fun () ->
            use hub = new ZMQLoop()
            ()
        )

    [<Fact>]
    let TwoLoopsCanTalk () = 
        scope (fun () ->
            let addr = Guid.NewGuid().ToString("N") |> sprintf "inproc://%s"

            use context = new Context()
            use hub1 = new ZMQLoop(context = context)
            use hub2 = new ZMQLoop(context = context)

            let socket1 = hub1.CreateSocket(fun ctx ->
                let sckt = Context.pair ctx
                Socket.bind sckt addr
                sckt)

            let socket2 = hub2.CreateSocket(fun ctx ->
                let sckt = Context.pair ctx
                Socket.connect sckt addr
                sckt)

            let signal = new ManualResetEventSlim(false)
            let received = ResizeArray()
            let receiver m =
                received.Add(m)
                signal.Set()

            let sender = hub1.CreateSender(socket1, encode)
            hub2.CreateReceiver(socket2, decode, receiver)

            sender "hello!"

            Assert.True(signal.Wait(1000))
            Assert.Contains("hello!", received)
        )

    [<Fact>]
    let SendingLotOfMessages () = 
        scope (fun () ->
            let port = 54375
            let addr = port |> sprintf "tcp://127.0.0.1:%d"

            use hub1 = new ZMQLoop()
            use hub2 = new ZMQLoop()

            let socket1 = hub1.CreateSocket(fun ctx ->
                let sckt = Context.pair ctx
                Socket.bind sckt addr
                sckt)

            let socket2 = hub2.CreateSocket(fun ctx ->
                let sckt = Context.pair ctx
                Socket.connect sckt addr
                sckt)

            let signal = new ManualResetEventSlim(false)
            let received = ResizeArray()
            let receiver m =
                received.Add(m)
                if received.Count >= 1000 then signal.Set()

            let sender = hub1.CreateSender(socket1, encode)
            hub2.CreateReceiver(socket2, decode, receiver)

            { 1..1000 } |> Seq.iter (sprintf "hello: %d" >> sender)

            Assert.True(signal.Wait(1000))
            Assert.True((received.Count = 1000))
        )
