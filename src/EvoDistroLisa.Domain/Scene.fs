namespace EvoDistroLisa.Domain

module Data =
    open FSharp.Fx

    let inline allow rng rate = (rng () |> Random.toFloat (0.0, float rate)) <= 1.0
    let inline mutate dirty rng rate func arg = 
        match allow rng rate with 
        | false -> arg
        | _ -> dirty |> Flag.set; func arg 

module Brush =
    open FSharp.Fx
    open Data

    type Brush = { A: float; R: float; G: float; B: float }

    let alphaRate = Settings.brushAlphaRate
    let colorRate = Settings.brushColorRate

    let alphaRange = Settings.brushAlphaRange
    let colorRange = Settings.brushColorRange

    let inline createAlpha rng = rng () |> Random.toFloat alphaRange
    let inline createColor rng = rng () |> Random.toFloat colorRange

    let createBrush rng =
        { A = createAlpha rng; R = createColor rng; G = createColor rng; B = createColor rng }

    let mutateBrush dirty rng brush =
        let inline mutateAlpha brush = { brush with A = createAlpha rng }
        let inline mutateRed brush = { brush with R = createColor rng }
        let inline mutateGreen brush = { brush with G = createColor rng }
        let inline mutateBlue brush = { brush with B = createColor rng }

        brush 
        |> mutate dirty rng alphaRate mutateAlpha 
        |> mutate dirty rng colorRate mutateRed
        |> mutate dirty rng colorRate mutateGreen
        |> mutate dirty rng colorRate mutateBlue

module Point =
    open FSharp.Fx
    open Data

    let maxRate = Settings.pointMoveMaxRate
    let midRate = Settings.pointMoveMidRate
    let minRate = Settings.pointMoveMinRate
    let maxXYRange = Settings.pointXYRange
    let midMoveRange = Settings.pointMoveMidRange
    let minMoveRange = Settings.pointMoveMinRange

    type Point = { X: float; Y: float }

    let inline createXY rng = rng () |> Random.toFloat maxXYRange

    let createPoint rng =
        { X = createXY rng; Y = createXY rng }

    let movePoint rng range point =
        let inline move v = (rng () |> Random.toFloat range) + v |> cap maxXYRange
        { point with X = move point.X; Y = move point.Y }

    let mutatePoint dirty rng point =
        let inline mutateMax _ = createPoint rng
        let inline mutateMid point = movePoint rng midMoveRange point
        let inline mutateMin point = movePoint rng minMoveRange point

        point
        |> mutate dirty rng maxRate mutateMax
        |> mutate dirty rng midRate mutateMid
        |> mutate dirty rng minRate mutateMin

module Polygon =
    open FSharp.Fx
    open Data
    open Point
    open Brush

    let insertPointRate = Settings.polygonInsertPointRate
    let deletePointRate = Settings.polygonDeletePointRate
    let polygonSizeRange = Settings.polygonSizeRange
    let minimumPolygonSize, maximumPolygonSize = polygonSizeRange

    type Polygon = { Brush: Brush; Points: Point array }

    let createPoints rng =
        let count = rng () |> Random.toInt32 polygonSizeRange
        Array.init count (fun _ -> createPoint rng)

    let createPolygon rng =
        { Brush = createBrush rng; Points = createPoints rng }

    let insertPoint dirty rng rate points =
        let length = Array.length points
        if length < maximumPolygonSize && allow rng rate then
            dirty |> Flag.set
            let index = rng () |> Random.toInt32 (0, length)
            let this = Array.get points ((index + 0) % length)
            let next = Array.get points ((index + 1) % length)
            let mid = { X = this.X + next.X / 2.0; Y = this.Y + next.Y / 2.0 }
            points |> Array.insert index mid
        else
            points

    let deletePoint dirty rng rate points =
        let length = Array.length points
        if length > minimumPolygonSize && allow rng rate then
            dirty |> Flag.set
            let index = rng () |> Random.toInt32 (0, length - 1)
            points |> Array.remove index
        else
            points

    let mutatePoints dirty rng points = 
        points |> Array.map (mutatePoint dirty rng)

    let clonePolygon polygon =
        { polygon with Points = polygon.Points |> Array.copy }

    let mutatePolygon dirty rng polygon =
        let inline mutateBrush polygon = { polygon with Brush = mutateBrush dirty rng polygon.Brush }
        let inline insertPoint polygon = { polygon with Points = insertPoint dirty rng insertPointRate polygon.Points }
        let inline deletePoint polygon = { polygon with Points = deletePoint dirty rng deletePointRate polygon.Points }
        let inline mutatePoints polygon = { polygon with Points = mutatePoints dirty rng polygon.Points }
    
        polygon 
        |> mutateBrush 
        |> insertPoint 
        |> deletePoint 
        |> mutatePoints

module Scene =
    open FSharp.Fx
    open Data
    open Point
    open Polygon

    let insertPolygonRate = Settings.sceneInsertPolygonRate
    let deletePolygonRate = Settings.sceneDeletePolygonRate
    let movePolygonRate = Settings.sceneMovePolygonRate

    let sceneSizeRange = Settings.sceneSizeRange
    let minimumSceneSize, maximumSceneSize = sceneSizeRange

    type Pixels = { Width: int; Height: int; Pixels: uint32[] }
    type Scene = { Polygons: Polygon array }
    type RenderedScene = { Scene: Scene; Fitness: double }
    type BootstrapScene = { Pixels: Pixels; Scene: RenderedScene }

    let emptyScene = { Polygons = Array.empty }
    let initialScene = { Scene = emptyScene; Fitness = 0.0 }

    let testScene =
        let points = 
            [ (0.1, 0.1); (0.5, 0.9); (0.9, 0.5) ] 
            |> Seq.map (fun (x, y) -> { X = x; Y = y })
        let polygon = { 
            Points = points |> Seq.toArray
            Brush = { A = 1.0; R = 0.5; G = 0.0; B = 1.0 } 
        }
        { Polygons = [| polygon |] }

    let createScene rng =
        let count = rng () |> Random.toInt32 sceneSizeRange
        { Polygons = Array.init count (fun _ -> createPolygon rng) }

    let insertPolygon dirty rng scene =
        let length = Array.length scene.Polygons
        if length < maximumSceneSize && allow rng insertPolygonRate then
            dirty |> Flag.set
            let index = rng () |> Random.toInt32 (0, length)
            { scene with Polygons = scene.Polygons |> Array.insert index (createPolygon rng) }
        else
            scene

    let deletePolygon dirty rng scene =
        let length = Array.length scene.Polygons
        if length > minimumSceneSize && allow rng deletePolygonRate then
            dirty |> Flag.set
            let index = rng () |> Random.toInt32 (0, length - 1)
            { scene with Polygons = scene.Polygons |> Array.remove index }
        else
            scene

    let movePolygon dirty rng scene =
        let length = Array.length scene.Polygons
        if length > 1 && allow rng movePolygonRate then
            let source = rng () |> Random.toInt32 (0, length - 1)
            let target = rng () |> Random.toInt32 (0, length - 1)
            if source <> target then
                dirty |> Flag.set
                { scene with Polygons = scene.Polygons |> Array.move source target }
            else
                scene
        else
            scene

    let mutatePolygons dirty rng scene =
        { scene with Polygons = scene.Polygons |> Array.map (mutatePolygon dirty rng) }

    let cloneScene scene = 
        { scene with Polygons = scene.Polygons |> Array.map clonePolygon }

    let mutateScene dirty rng scene =
        scene
        |> insertPolygon dirty rng
        |> deletePolygon dirty rng
        |> movePolygon dirty rng
        |> mutatePolygons dirty rng
