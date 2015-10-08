namespace EvoDistroLisa

module Domain =
    type Value = float
    type Point = { X: Value; Y: Value }
    type Brush = { A: Value; R: Value; G: Value; B: Value }
    type Polygon = { Brush: Brush; Points: Point array }
    type Pixels = { Width: int; Height: int; Pixels: uint32[] }
    type Scene = 
        { Polygons: Polygon array }
        static member Zero = { Polygons = Array.empty }
    type RenderedScene = 
        { Scene: Scene; Fitness: decimal }
        static member Zero = { Scene = Scene.Zero; Fitness = 0m }
    type BootstrapScene = { Pixels: Pixels; Scene: RenderedScene }