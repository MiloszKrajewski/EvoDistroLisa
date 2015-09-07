//namespace FSharp.Collections
//
//module BlockingQueue =
//    open System
//    open System.Threading
//    open System.Collections.Concurrent
//
//    module private Option =
//        let inline def d v = defaultArg v d
//        let inline alt d v = match v with | None -> d | _ -> v
//
//    type BlockingQueue<'m> (?maximumSize: int) =
//        let maximumSize = maximumSize |> Option.def Int32.MaxValue
//        let queue = ConcurrentQueue<'m>()
//        let reader = new SemaphoreSlim(0)
//        let writer = new SemaphoreSlim(maximumSize)
//
//        let dispose () = 
//            reader.Dispose()
//            writer.Dispose()
//
//        let asyncWait (timeout: TimeSpan) (token: CancellationToken) (semaphore: SemaphoreSlim) = 
//            async {
//                let! success = semaphore.WaitAsync(timeout, token) |> Async.AwaitTask
//                if success then semaphore.Release() |> ignore
//                return success
//            }
//
//        let asyncPut (timeout: TimeSpan) (token: CancellationToken) value = async {
//            let! success = writer.WaitAsync(timeout, token) |> Async.AwaitTask
//            if success then
//                queue.Enqueue(value)
//                reader.Release() |> ignore
//            return success
//        }
//
//        let asyncGet (timeout: TimeSpan) (token: CancellationToken) = async {
//            let! success = reader.WaitAsync(timeout, token) |> Async.AwaitTask
//            if success then
//                let success, value = queue.TryDequeue()
//                assert success
//                writer.Release() |> ignore
//                return Some value
//            else
//                return None
//        }
//
//        let asyncPutWait (timeout: TimeSpan) (token: CancellationToken) = 
//            writer |> asyncWait timeout token
//
//        let asyncGetWait (timeout: TimeSpan) (token: CancellationToken) = 
//            reader |> asyncWait timeout token
//
//        member x.PutAsync(value, ?timeout: TimeSpan, ?token: CancellationToken) =
//            let timeout = timeout |> Option.def Timeout.InfiniteTimeSpan
//            let token = token |> Option.def CancellationToken.None
//            value |> asyncPut timeout token
//
//        member x.TryGetAsync(?timeout: TimeSpan, ?token: CancellationToken) =
//            let timeout = timeout  |> Option.def Timeout.InfiniteTimeSpan
//            let token = token |> Option.def CancellationToken.None
//            asyncGet timeout token
//
//        member x.WaitPutAsync (?timeout: TimeSpan, ?token: CancellationToken) =
//            let timeout = timeout |> Option.def Timeout.InfiniteTimeSpan
//            let token = token |> Option.def CancellationToken.None
//            asyncPutWait timeout token
//
//        member x.WaitGetAsync (?timeout: TimeSpan, ?token: CancellationToken) =
//            let timeout = timeout |> Option.def Timeout.InfiniteTimeSpan
//            let token = token |> Option.def CancellationToken.None
//            asyncGetWait timeout token
//
//        member x.Count = queue.Count
//
//        member x.Dispose() = dispose ()
//
//        interface IDisposable with
//            member x.Dispose () = dispose ()
