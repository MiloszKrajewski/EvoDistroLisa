namespace fszmq

open System
open System.Threading
open System.Collections.Concurrent
open FSharp.Fx

type ZMQSync (context: Context, ?token: CancellationToken) =
    let cancel = Async.inheritToken token
    let token = cancel.Token
    let queue = ConcurrentQueue<unit -> unit>()
    let empty = ""B

    let sendSocket, recvSocket = 
        let addr = Guid.NewGuid().ToString("N") |> sprintf "inproc://%s"
        let recv = Context.pair context
        let send = Context.pair context
        Socket.bind recv addr
        Socket.connect send addr
        send, recv

    let rec sendLoop (inbox: MailboxProcessor<_>) = async {
        let! funcs = inbox |> Agent.recvMany token 1
        funcs |> Seq.iter queue.Enqueue
        empty |> Socket.send sendSocket
        do! sendLoop inbox
    }
    let sendAgent, sendDone = Async.startAgent token sendLoop

    let enqueue func = 
        token.ThrowIfCancellationRequested()
        sendAgent.Post(func)

    let rec enumerate () = seq {
        match queue.TryDequeue() with
        | false, _ -> ()
        | _, item -> yield item; yield! enumerate ()
    }

    let recvHandler socket =
        socket |> Socket.recvAll |> ignore
        enumerate () |> Seq.toArray |> Array.iter (fun func -> forgive func () |> ignore)

    let poll = recvSocket |> Polling.pollIn recvHandler

    let close () = 
        cancel.Cancel()
        sendDone |> Async.waitAndIgnore
        disposeMany [sendSocket; recvSocket]
        dispose cancel

    member x.Enqueue(func) = enqueue func
    member x.Poll = poll
    interface IDisposable with member x.Dispose() = close ()
