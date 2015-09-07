namespace fszmq

module Socket = 
    open System
    open System.Net.NetworkInformation
    open System.Collections.Generic
    open fszmq

    let private randomPort min max (ports: int seq) = 
        let next = 
            let rng = Random()
            fun () -> rng.Next(min, max)
        let ports = HashSet(ports)
        Seq.initInfinite (fun _ -> next ())
        |> Seq.filter (fun p -> ports.Contains(p) |> not)

    let randomTcpPort min max =
        let ports = 
            IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners()
            |> Seq.map (fun ep -> ep.Port)
        randomPort min max ports
            
    let randomUdpPort min max =
        let ports = 
            IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners()
            |> Seq.map (fun ep -> ep.Port)
        randomPort min max ports

    let bindToAny socket (addr: int -> string) (ports: int seq) =
        let next =
            let enumerator = ports.GetEnumerator()
            fun () -> 
                match enumerator.MoveNext() with
                | false -> None
                | _ -> Some enumerator.Current
        let rec loop iter excl = 
            let value = iter ()
            match value, excl with
            | None, [] -> invalidOp "Port collection is empty"
            | None, excl -> AggregateException(excl |> List.rev) |> raise
            | Some port, excl -> 
                let exc = 
                    try Socket.bind socket (addr port); None
                    with | :? ZMQError as e -> Some (e :> Exception)
                match exc with
                | None -> port
                | Some e -> loop iter (e :: excl)
        loop next []
