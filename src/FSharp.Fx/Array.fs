namespace FSharp.Fx

module Array =
    let insert index item array =
        let length = Array.length array
        let result = Array.zeroCreate (length + 1)
        if index > 0 then
            Array.blit array 0 result 0 index
        if index < length then
            Array.blit array index result (index + 1) (length - index)
        result.[index] <- item
        result

    let remove index array =
        let length = Array.length array
        let result = Array.zeroCreate (length - 1)
        if length > 1 then
            if index > 0 then 
                Array.blit array 0 result 0 index
            if index < length then
                Array.blit array (index + 1) result index (length - index - 1)
        result

    let swapInPlace source target array =
        if source = target then
            array
        else
            let source' = Array.get array source
            let target' = Array.get array target
            target' |> Array.set array source
            source' |> Array.set array target
            array

    let moveInPlace source target array =
        if source = target then
            array
        else
            let item = Array.get array source
            if target < source then
                Array.blit array target array (target + 1) (source - target)
            else
                Array.blit array (source + 1) array source (target - source)
            item |> Array.set array target
            array

    let move source target array = 
        if source = target then
            array
        else
            let length = Array.length array
            let result = Array.zeroCreate length
            if target < source then
                Array.blit array 0 result 0 target
                Array.get array source |> Array.set result target
                Array.blit array target result (target + 1) (source - target)
                Array.blit array (source + 1) result (source + 1) (length - source - 1)
            else
                Array.blit array 0 result 0 source
                Array.blit array (source + 1) result source (target - source - 1)
                Array.get array source |> Array.set result (target - 1)
                Array.blit array target result target (length - target)
            result

    let inline pmap func array = Array.Parallel.map func array
