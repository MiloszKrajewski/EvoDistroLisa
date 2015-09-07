namespace fszmq.Collections

open System
open System.Threading
open System.Collections.Concurrent
open fszmq
open FSharp.Fx

module internal Shared =
    open FSharp.Fx

    type Shared<'a> = 
        | Owned of 'a
        | Borrowed of 'a
        member x.Value = match x with | Owned o -> o | Borrowed o -> o
        member x.Dispose() = match x with | Owned o -> dispose o | Borrowed _ -> ()
        interface IDisposable with member x.Dispose() = x.Dispose()

    let create f v = match v with | None -> f () |> Owned | Some v -> Borrowed v

type internal VersionedBag<'item> () =
    let bag = ConcurrentBag()
    let mutable generation = 1

    let read (v: int byref) = Interlocked.CompareExchange(&v, 0, 0)
    let incr (v: int byref) = Interlocked.Increment(&v)

    let register item = 
        bag.Add(item)
        incr &generation |> ignore

    let refresh version items =
        let generation = read &generation
        let snapshot = if generation <= version then items else bag.ToArray()
        generation, snapshot

    member x.Register(poll) = register poll
    member x.Refresh() = refresh 0 Array.empty
    member x.Refresh(snapshot: int * 'item[]) = 
        let version, polls = snapshot
        refresh version polls

type internal DisposableBag () =
    let objects = ConcurrentBag<IDisposable>()
    let mutable disposed = 0

    let disposeOne = 
        forgive dispose >> ignore

    let rec disposeAll () = 
        match objects.TryTake() with
        | false, _ -> ()
        | _, o -> disposeOne o; disposeAll ()

    let isDisposed () =
        Interlocked.CompareExchange(&disposed, -1, -1) <> 0

    let register subject = 
        if isDisposed () then
            disposeOne subject
        else
            objects.Add(subject :> IDisposable)
            if isDisposed () then 
                disposeAll ()
        subject

    let registerMany collection = 
        collection |> Seq.map (apply (register >> ignore)) |> Seq.toArray

    let close () =
        Interlocked.Exchange(&disposed, -1) |> ignore
        disposeAll ()

    member x.Add(subject) = register subject
    member x.AddMany(collection) = registerMany collection
    interface IDisposable with member x.Dispose () = close ()
