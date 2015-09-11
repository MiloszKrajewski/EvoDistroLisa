namespace EvoDistroLisa.Domain

module Settings =
    let brushAlphaRate = 1500.0
    let brushColorRate = 1500.0

    let brushAlphaRange = (0.1, 0.25)
    let brushColorRange = (0.0, 1.0)

    let pointMoveMaxRate = 1500.0
    let pointMoveMidRate = 1500.0
    let pointMoveMinRate = 1500.0

    let pointXYRange = (0.0, 1.0)
    let pointMoveMinRange = (-0.015, 0.015)
    let pointMoveMidRange = (-0.1, 0.1)

    let polygonInsertPointRate = 1500.0
    let polygonDeletePointRate = 1500.0
    let polygonSizeRange = (3, 10)

    let sceneInsertPolygonRate = 700.0
    let sceneDeletePolygonRate = 1500.0
    let sceneMovePolygonRate = 700.0

    let sceneSizeRange = (0, 256)
