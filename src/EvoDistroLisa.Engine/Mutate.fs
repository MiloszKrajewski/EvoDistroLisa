namespace EvoDistroLisa

module Mutate =
    open FSharp.Fx
    open EvoDistroLisa
    open EvoDistroLisa.Domain

    let inline allow rng rate = (rng () |> Random.toFloat (0.0, float rate)) <= 1.0
    let inline mutate dirty rng rate func arg = 
        match allow rng rate with 
        | false -> arg
        | _ -> dirty |> Flag.set; func arg

    let inline createAlpha rng = rng () |> Random.toFloat Settings.brushAlphaRange
    let inline createColor rng = rng () |> Random.toFloat Settings.brushColorRange

    let createBrush rng =
        { A = createAlpha rng; R = createColor rng; G = createColor rng; B = createColor rng }

    let mutateBrush dirty rng brush =
        let inline mutateAlpha brush = { brush with A = createAlpha rng }
        let inline mutateRed brush = { brush with R = createColor rng }
        let inline mutateGreen brush = { brush with G = createColor rng }
        let inline mutateBlue brush = { brush with B = createColor rng }
        let mutate = mutate dirty rng

        brush 
        |> mutate Settings.brushAlphaRate mutateAlpha 
        |> mutate Settings.brushColorRate mutateRed
        |> mutate Settings.brushColorRate mutateGreen
        |> mutate Settings.brushColorRate mutateBlue

    let inline createXY rng = rng () |> Random.toFloat Settings.pointXYRange

    let createPoint rng =
        { X = createXY rng; Y = createXY rng }

    let movePoint rng range point =
        let inline move v = (rng () |> Random.toFloat range) + v |> cap Settings.pointXYRange
        { point with X = move point.X; Y = move point.Y }

    let inline fiddlePoint rng point = 
        movePoint rng Settings.pointMoveMinRange point

    let mutatePoint dirty rng point =
        let inline mutateMax _ = createPoint rng
        let inline mutateMid point = movePoint rng Settings.pointMoveMidRange point
        let inline mutateMin point = movePoint rng Settings.pointMoveMinRange point
        let mutate = mutate dirty rng

        point
        |> mutate Settings.pointMoveMaxRate mutateMax
        |> mutate Settings.pointMoveMidRate mutateMid
        |> mutate Settings.pointMoveMinRate mutateMin

    let createPoints rng =
        let count = rng () |> Random.toInt32 Settings.polygonSizeRange
        Array.init count (fun _ -> createPoint rng)

    let createLargePolygon rng =
        { Brush = createBrush rng; Points = createPoints rng }

    let createSmallPolygon rng =
        let point = createPoint rng
        let points = Seq.init 3 (fun _ -> point |> fiddlePoint rng)
        { Brush = createBrush rng; Points = points |> Array.ofSeq }

    let insertPoint dirty rng rate points =
        let length = Array.length points
        let _, maximumPolygonSize = Settings.polygonSizeRange
        if length < maximumPolygonSize && allow rng rate then
            dirty |> Flag.set
            let index0 = rng () |> Random.toInt32 (0, length - 1)
            let index1 = (index0 + 1) % length
            let this = points.[index0]
            let next = points.[index1]
            let mid = 
                { X = this.X + next.X / 2.0; Y = this.Y + next.Y / 2.0 } 
                |> fiddlePoint rng
            points |> Array.insert index1 mid
        else
            points

    let deletePoint dirty rng rate points =
        let length = Array.length points
        let minimumPolygonSize, _ = Settings.polygonSizeRange
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
        let inline mutateBrush polygon = 
            { polygon with Brush = polygon.Brush |> mutateBrush dirty rng }
        let inline insertPoint polygon = 
            { polygon with Points = polygon.Points |> insertPoint dirty rng Settings.polygonInsertPointRate }
        let inline deletePoint polygon = 
            { polygon with Points = polygon.Points |> deletePoint dirty rng Settings.polygonDeletePointRate }
        let inline mutatePoints polygon = 
            { polygon with Points = polygon.Points |> mutatePoints dirty rng }
    
        polygon 
        |> mutateBrush 
        |> insertPoint 
        |> deletePoint 
        |> mutatePoints

    let insertPolygon dirty rng scene =
        let length = Array.length scene.Polygons
        let _, maximumSceneSize = Settings.sceneSizeRange
        if length < maximumSceneSize && allow rng Settings.sceneInsertPolygonRate then
            dirty |> Flag.set
            let index = rng () |> Random.toInt32 (0, length)
            { scene with Polygons = scene.Polygons |> Array.insert index (createSmallPolygon rng) }
        else
            scene

    let deletePolygon dirty rng scene =
        let length = Array.length scene.Polygons
        let minimumSceneSize, _ = Settings.sceneSizeRange
        if length > minimumSceneSize && allow rng Settings.sceneDeletePolygonRate then
            dirty |> Flag.set
            let index = rng () |> Random.toInt32 (0, length - 1)
            { scene with Polygons = scene.Polygons |> Array.remove index }
        else
            scene

    let movePolygon dirty rng scene =
        let length = Array.length scene.Polygons
        if length > 1 && allow rng Settings.sceneMovePolygonRate then
            let source = rng () |> Random.toInt32 (0, length - 1)
            let target = rng () |> Random.toInt32 (0, length - 1)
            if source <> target && source <> target + 1 then
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
