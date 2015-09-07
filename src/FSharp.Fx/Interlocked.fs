namespace FSharp.Fx

module Interlocked =
    open System.Threading

    let inline getRef (r: 'a ref) =
        Interlocked.CompareExchange(r, Unchecked.defaultof<'a>, Unchecked.defaultof<'a>)

    let inline setRef (v: 'a) (r: 'a ref) =
        Interlocked.Exchange(r, v)

    let inline getLong (r: int64 ref) =
        Interlocked.Read(r)

    let inline addLong (v: int64) (r: int64 ref) =
        Interlocked.Add(r, v)
