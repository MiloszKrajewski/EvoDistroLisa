type Point = { X: double; Y: double }
type Brush = { A: double; R: double; G: double; B: double }

type Polyline = Point array
type Polygon = { Brush: Brush; Points: Polyline }

type Scene = { Polygons: Polygon array }
type RenderedScene = { Scene: Scene; Fitness: double }

let tryImprove mutator renderer fitter (champion: RenderedScene) =
    let challenger = champion.Scene |> mutator
    { Scene = challenger; Fitness = challenger |> renderer |> fitter }
