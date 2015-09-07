namespace FSharp.Fx

module Option =
    let inline def d v = defaultArg v d
    let inline alt d v = match v with | None -> d | _ -> v
