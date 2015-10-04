namespace EvoDistroLisa.CLI

module Observable =
    open System
    open FSharp.Control.Reactive
    open System.Reactive
    open System.Reactive.Linq
    open System.Collections.Generic

    let create func =
        Observable.Create(Func<IObserver<_>, IDisposable>(func))

    let slidingWindow (interval: TimeSpan) observable =
        let observable = observable |> Observable.timestamp
        let rec cleanup limit (queue: Queue<Timestamped<_>>) =
            if queue.Count >= 1 && queue.Peek().Timestamp < limit then
                queue.Dequeue() |> ignore
                queue |> cleanup limit
        create (fun o ->
            let queue = Queue()
            let onNext (i: Timestamped<_>) = 
                queue.Enqueue(i)
                let limit = i.Timestamp.Subtract(interval)
                while queue.Count > 1 && queue.Peek().Timestamp < limit do
                    queue.Dequeue() |> ignore
                o.OnNext(queue.ToArray())
            observable |> Observable.subscribeWithCallbacks onNext o.OnError o.OnCompleted
        )
