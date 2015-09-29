namespace EvoDistroLisa

open System
open Domain

[<Interface>]
type IAgent = 
    abstract member Push: int64 -> unit
    abstract member Push: RenderedScene -> unit
    abstract member Mutated: IObservable<int64>
    abstract member Improved: IObservable<RenderedScene>
    abstract member Pixels: Pixels
    abstract member Best: RenderedScene
    abstract member Mutations: int64
