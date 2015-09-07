namespace FSharp.Fx

module Seq =
    open System
    open System.Linq

    let piter f (s: 'a seq) = s.AsParallel().ForAll(Action<'a>(f))
    let pmap f (s: 'a seq) = s.AsParallel().Select(Func<'a, 'b>(f)).AsEnumerable()
