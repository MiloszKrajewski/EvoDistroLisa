open System

module Agent =
    let recvMany (inbox: MailboxProcessor<_>) = async { return [] }
    let send (item: 'a) (inbox: MailboxProcessor<'a>) = ()
    let start (loop: MailboxProcessor<'a> -> Async<unit>) = MailboxProcessor<'a>.Start(loop)

module Pickler =
    let save message = Array.empty<byte>
    let load<'message> (bytes: byte[]) = Unchecked.defaultof<'message>

module Socket = 
    type Socket = obj
    let send (message: byte[]) (socket: Socket) = ()
    let observe (socket: Socket) = Event<byte[]>().Publish :> IObservable<_>

type Point = { X: double; Y: double }
type Brush = { A: double; R: double; G: double; B: double }
type Polygon = { Brush: Brush; Points: Point array }
type Pixels = { Width: int; Height: int; Pixels: uint32 array }
type Scene = Polygon array
type RenderedScene = { Scene: Scene; Fitness: double }

let inline fitnessOf (scene: RenderedScene) = scene.Fitness

let select publish champion challengers =
    let challenger = challengers |> Seq.maxBy fitnessOf
    match fitnessOf champion >= fitnessOf challenger with
    | true -> champion
    | _ -> challenger |> publish; challenger

let inline sceneOf (scene: RenderedScene) = scene.Scene

let improve mutate render fit champion =
    let challenger = champion |> sceneOf |> mutate
    let fitness = challenger |> render |> fit
    { Scene = challenger; Fitness = fitness }

let rec passiveLoop publish champion inbox = async {
    let! challengers = inbox |> Agent.recvMany
    let champion' = challengers |> select publish champion
    do! passiveLoop publish champion' inbox
}

let rec activeLoop mutate render fit publish champion inbox = async {
    let! challengers = inbox |> Agent.recvMany
    let champion' = challengers |> select publish champion
    let challenger' = champion |> improve mutate render fit
    inbox |> Agent.send challenger'
    do! activeLoop mutate render fit publish champion' inbox
}

type IAgent = 
    abstract member Push: RenderedScene -> unit
    abstract member Improved: IObservable<RenderedScene>

let createAgent loop champion =
    let improved = Event<RenderedScene>()
    let publish scene = scene |> improved.Trigger
    let agent = Agent.start (loop publish champion)
    { new IAgent with 
        member x.Push(scene: RenderedScene) = agent |> Agent.send scene
        member x.Improved = improved.Publish :> IObservable<_> 
    }

let createPassiveAgent champion = 
    champion |> createAgent passiveLoop

let createActiveAgent mutate render fit champion = 
    champion |> createAgent (activeLoop mutate render fit)

let attachAgent (slave: IAgent) (master: IAgent) =
    master.Improved |> Observable.subscribe slave.Push |> ignore
    slave.Improved |> Observable.subscribe master.Push |> ignore

let createCompositeAgent mutate render fit champion count = 
    let master = createActiveAgent mutate render fit champion
    { 2..count } |> Seq.iter (fun _ ->
        let slave = createActiveAgent mutate render fit champion
        master |> attachAgent slave
    )
    master

let encode message = message |> Pickler.save
let decode<'a> bytes = bytes |> Pickler.load<'a>

let createSocketAgent subSocket pubSocket champion =
    let send scene = pubSocket |> Socket.send (scene |> encode)
    let received = subSocket |> Socket.observe |> Observable.map decode
    let agent = createPassiveAgent champion
    received |> Observable.subscribe agent.Push |> ignore
    agent.Improved |> Observable.subscribe send |> ignore
    agent
