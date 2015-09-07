namespace FSharp.Fx

module Flag =
    open System.Threading

    let create () = ref 0
    let set (flag: int ref) = Interlocked.Exchange(flag, -1) |> ignore
    let get (flag: int ref) = Interlocked.CompareExchange(flag, 1, 1) <> 0
