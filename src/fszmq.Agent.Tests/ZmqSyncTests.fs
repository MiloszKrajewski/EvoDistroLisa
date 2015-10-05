namespace fszmq.Agent.Tests

module SinkTests =
    open Xunit
    open fszmq
    open System
    open System.Threading
    open FSharp.Fx

    let scope func = func ()

    [<Fact>]
    let ZmqSyncCanBeOpenedAndClosed () =
        scope (fun () -> 
            use cancel = new CancellationTokenSource()
            use context = new Context()
            use sync = new ZMQSync(context, cancel.Token)
            ()
        )

    [<Fact>]
    let ZmqSyncDoesNotBlockWhenMessageNotHandled () =
        scope (fun () -> 
            use cancel = new CancellationTokenSource()
            use context = new Context()
            use sync = new ZMQSync(context, cancel.Token)
            let received = ref false
            sync.Enqueue(fun () -> received.Value <- true)
            Assert.False(received.Value)
        )

    [<Fact>]
    let ZmqSyncExecutesActionsOnPoll () =
        scope (fun () -> 
            use cancel = new CancellationTokenSource()
            use context = new Context()
            use sync = new ZMQSync(context, cancel.Token)
            let received = ref false
            sync.Enqueue(fun () -> received.Value <- true)
            let success = Polling.poll 1000L [sync.Poll]
            Assert.True(success)
            Assert.True(received.Value)
            printf "xxx"
        )

    [<Fact>]
    let ZmqSyncCanBeCancelled () =
        scope (fun () -> 
            use cancel = new CancellationTokenSource()
            use context = new Context()
            use sync = new ZMQSync(context, cancel.Token)
            sync.Enqueue(fun () -> ())
            cancel.Cancel()
            Assert.Throws<OperationCanceledException>(fun () -> 
                sync.Enqueue(fun () -> ()))
        )
