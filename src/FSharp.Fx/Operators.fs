namespace FSharp.Fx

[<AutoOpen>]
module Operators =
    open System
    open System.Threading
    open System.Diagnostics
    open System.Reflection
    open System.Runtime.ExceptionServices

    let inline cap (lo, hi) v = v |> max lo |> min hi

    let inline apply func arg = func arg |> ignore; arg
    let inline trace obj = obj.ToString() |> Trace.WriteLine; obj
    let inline forgive func arg = try func arg |> Some with | _ -> None
    let inline trigger (event: Event<_>) message = event.Trigger(message)

    let weakref (value: 'a) = 
        let reference = WeakReference(value)
        fun () -> 
            match reference.Target :?> 'a with 
            | null -> None 
            | value -> Some value

    let weaklazyref (factory: unit -> 'a) = 
        let reference = WeakReference(null)
        fun () -> 
            match reference.Target :?> 'a with 
            | null -> 
                let result = factory ()
                reference.Target <- result
                result
            | result -> result

    let lazyref (factory: unit -> 'a) =
        let reference = ref None
        fun () ->
            match reference.Value with
            | None ->
                let result = factory ()
                reference.Value <- result |> Some
                result
            | Some result -> result

    let threadref (factory: unit -> 'a) =
        let generator = new ThreadLocal<'a>(Func<'a>(factory))
        fun () -> generator.Value

    let dispose (obj: obj) = 
        match obj with
        | :? IDisposable as d -> d.Dispose()
        | _ -> ()

    let disposeMany (objs: seq<obj>) =
        objs |> Seq.iter dispose

    let rec rethrow (e: Exception): 'a =
        match e with
        | null -> Unchecked.defaultof<'a>
        | :? AggregateException as e when e.InnerExceptions.Count = 1 -> e.InnerException |> rethrow
        | :? TargetInvocationException as e -> e.InnerException |> rethrow
        | _ -> ExceptionDispatchInfo.Capture(e).Throw(); Unchecked.defaultof<'a>
