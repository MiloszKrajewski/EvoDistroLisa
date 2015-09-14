namespace EvoDistroLisa.Engine.ZMQ

module Message = 
    open EvoDistroLisa
    open EvoDistroLisa.Domain

    type Message =
        | Handshake
        | Bootstrap of int * int * BootstrapScene
        | Improved of RenderedScene
        | Mutated of int64
        | Error of string

    let internal encode (o: Message) = [| o |> Pickler.save |]
    let internal decode m = 
        match m with
        | [| b |] -> b |> Pickler.load<Message>
        | m -> m |> sprintf "Message cannot be decoded: %A" |> Error
