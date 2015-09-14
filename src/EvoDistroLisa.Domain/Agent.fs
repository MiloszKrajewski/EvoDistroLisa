namespace EvoDistroLisa.Domain

open System
open Scene

type RenderedScene = 
    { Scene: Scene; Fitness: decimal }
    static member Zero = { Scene = Scene.Zero; Fitness = 0m }

type BootstrapScene = 
    { Pixels: Pixels; Scene: RenderedScene }

[<Interface>]
type IAgent = 
    abstract member Push: int64 -> unit
    abstract member Push: RenderedScene -> unit
    abstract member Mutated: IObservable<int64>
    abstract member Improved: IObservable<RenderedScene>
    abstract member Pixels: Pixels
    abstract member Best: RenderedScene
    abstract member Mutations: int64
