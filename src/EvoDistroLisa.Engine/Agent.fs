namespace EvoDistroLisa.Engine

module Agent =
    open System
    open System.Threading
    open FSharp.Fx
    open EvoDistroLisa
    open EvoDistroLisa.Domain
    open EvoDistroLisa.Mutate

    // type private Agent<'a> = MailboxProcessor<'a>

    type private BestScene = 
        | Champion of RenderedScene
        | Challenger of RenderedScene

    [<Literal>] 
    let private mutationsInterval = 16L

    /// Choses better scene depending on fitness with bias for current champion.
    let private selectScene champion challengers =
        let fitnessOf (scene: RenderedScene) = scene.Fitness
        let challenger = challengers |> Seq.maxBy fitnessOf
        match champion.Fitness, challenger.Fitness with
        | a, b when a >= b -> Champion champion 
        | _ -> Challenger challenger

    let createMutator () =
        let rng = Random.createSequential ()
        fun scene ->
            let dirty = Flag.create ()
            let rec loop () = 
                let result = mutateScene dirty rng scene
                match dirty |> Flag.get with | true -> result | _ -> loop ()
            loop ()

    let createPassiveAgent 
            (pixels: Pixels)
            (best: RenderedScene) 
            (token: CancellationToken) =

        let bestScene = ref best
        let mutationCount = ref 0L
        let improvedEvent = Event<RenderedScene>()
        let mutatedEvent = Event<int64>()

        let grind = 
            let rec loop inbox = async {
                let! scenes = Agent.recvMany token 1 inbox
                match selectScene bestScene.Value scenes with
                | Challenger scene -> bestScene |> Interlocked.setRef scene |> forgive (trigger improvedEvent) |> ignore
                | _ -> ()
                do! loop inbox
            }
            let agent, _ = Async.startAgent token loop
            fun scene -> scene |> Agent.send agent

        let collect = 
            let rec loop inbox = async {
                let! count = Agent.recvMany token 1 inbox
                let count = count |> Seq.sum
                mutationCount |> Interlocked.addLong count |> ignore
                count |> forgive (trigger mutatedEvent) |> ignore
                do! loop inbox
            }
            let agent, _ = Async.startAgent token loop
            fun count -> count |> Agent.send agent

        { new IAgent with
            member x.Push(count: int64) = count |> collect
            member x.Push(scene: RenderedScene) = scene |> grind
            member x.Mutated = mutatedEvent.Publish :> IObservable<_>
            member x.Improved = improvedEvent.Publish :> IObservable<_>
            member x.Pixels = pixels
            member x.Best = Interlocked.getRef bestScene
            member x.Mutations = Interlocked.getLong mutationCount
        }

    let createAgent
            (pixels: Pixels)
            (mutate: Scene -> Scene) 
            (render: Pixels -> Scene -> decimal) 
            (best: RenderedScene)
            (token: CancellationToken) =

        // adapter for render function
        let render = render pixels
        let render scene = { Scene = scene; Fitness = render scene }

        let bestScene = ref best
        let mutationCount = ref 0L
        let improvedEvent = Event<RenderedScene>()
        let mutatedEvent = Event<int64>()

        let collect = 
            let rec loop inbox = async {
                let! count = Agent.recvMany token 1 inbox
                let count = count |> Seq.sum
                mutationCount |> Interlocked.addLong count |> ignore
                count |> forgive (trigger mutatedEvent) |> ignore
                do! loop inbox
            }
            let agent, _ = Async.startAgent token loop
            fun count -> count |> Agent.send agent

        let grind = 
            let publishImproved (scene: RenderedScene) =
                // let clone = { scene with Scene = scene.Scene |> cloneScene }
                let clone = scene
                bestScene |> Interlocked.setRef clone |> forgive (trigger improvedEvent) |> ignore
                scene

            let publishMutated count =
                match count with
                | c when c < mutationsInterval -> c
                | c -> c |> collect; 0L

            let rec loop mutations champion inbox = async {
                let! challengers = Agent.recvMany token 1 inbox
                let winner = 
                    match selectScene champion challengers with
                    | Champion scene -> scene
                    | Challenger scene -> scene |> publishImproved
                winner.Scene |> mutate |> render |> Agent.send inbox
                let mutations' = mutations + 1L |> publishMutated
                do! loop mutations' winner inbox
            }
            let agent, _ = Async.startAgent token (loop 0L best)
            fun scene -> scene |> Agent.send agent

        { new IAgent with
            member x.Push(count: int64) = count |> collect
            member x.Push(scene: RenderedScene) = scene |> grind
            member x.Mutated = mutatedEvent.Publish :> IObservable<_>
            member x.Improved = improvedEvent.Publish :> IObservable<_>
            member x.Pixels = pixels
            member x.Best = Interlocked.getRef bestScene
            member x.Mutations = Interlocked.getLong mutationCount
        }

    let attachAgent (slave: IAgent) (master: IAgent) =
        slave.Improved |> Observable.subscribe master.Push |> ignore
        master.Improved |> Observable.subscribe slave.Push |> ignore
        slave.Mutated |> Observable.subscribe master.Push |> ignore
