namespace FSharp.Fx

open System
open System.Threading
open System.Threading.Tasks

module Async = 
    open System.Runtime.ExceptionServices

    type private Token = CancellationToken
    type private Result<'a> = TaskCompletionSource<'a>

    let private wrapAsync (token: Token) (result: Result<_>) func arg = async {
        try 
            token.ThrowIfCancellationRequested()
            let! value = func arg
            result.TrySetResult(value) |> ignore
        with
        | :? OperationCanceledException when token.IsCancellationRequested -> result.TrySetCanceled() |> ignore
        | e -> result.TrySetException(e |> trace) |> ignore
    }

    let private wrapFunc (token: Token) (result: Result<_>) func arg =
        try 
            token.ThrowIfCancellationRequested()
            let value = func arg
            result.TrySetResult(value) |> ignore
        with
        | :? OperationCanceledException when token.IsCancellationRequested -> result.TrySetCanceled() |> ignore
        | e -> result.TrySetException(e |> trace) |> ignore

    let startThread token func arg = 
        let result = Result<_>()
        let wrapper () = wrapFunc token result func arg
        let thread = Thread(wrapper, IsBackground = true)
        thread.Start()
        thread, result.Task |> Async.AwaitTask

    let startAgent token func =
        let result = Result<_>()
        let wrapper = wrapAsync token result func
        let agent = MailboxProcessor.Start(wrapper)
        agent, result.Task |> Async.AwaitTask

    let inheritToken token = 
        let token = token |> Option.def CancellationToken.None
        CancellationTokenSource.CreateLinkedTokenSource(token)

    let waitAndIgnore action =
        try action |> Async.RunSynchronously |> ignore with | e -> ()

module Agent = 
    type private Token = CancellationToken
    type private Agent<'a> = MailboxProcessor<'a>

    let rec recv (token: Token) (inbox: Agent<_>) = async {
        let! item = inbox.TryReceive(100)
        token.ThrowIfCancellationRequested()
        match item with
        | None -> return! recv token inbox
        | Some item -> return item
    }

    let recvMany (token: Token) min (inbox: Agent<_>) = 
        let rec loop list = async {
            let required = List.length list < min
            let! item = inbox.TryReceive(if required then 100 else 0)
            token.ThrowIfCancellationRequested()
            match item, required with
            | Some item, _ -> return! loop (item :: list)
            | None, true -> return! loop list
            | None, false -> return list
        }
        loop []

    let send (inbox: Agent<_>) message = 
        inbox.Post(message)
