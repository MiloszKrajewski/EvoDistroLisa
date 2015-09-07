namespace FSharp.Fx

module Random =
    open System
    open System.Threading

    let inheritSequential rng = 
        let seed = (lock rng rng) * (float UInt32.MaxValue) |> round |> uint32 |> int
        let generator = Random(seed)
        fun () -> generator.NextDouble()

    let createSequential () =
        let generator = Random()
        fun () -> generator.NextDouble()

    let createNaiveParallel () =
        let generator = Random()
        fun () -> lock generator (fun () -> generator.NextDouble())

    let createParallel count =
        let count = count |> max 1
        let seed = Random()
        let generators = Array.init count (fun _ -> new Random(seed.Next()))
        let pointer = ref 0
        let inline next () = ((Interlocked.Increment(pointer) |> uint32) % (count |> uint32)) |> int
        fun () ->
            let generator = generators.[next ()]
            lock generator (fun () -> generator.NextDouble())

    let inline private fcap v = v |> max 0.0 |> min 1.0
    let inline private fext lo hi v = v * (hi - lo) + lo

    let inline toFloat (lo, hi) v = 
        v |> fcap |> fext (float lo) (float hi)
    let inline toInt32 (lo, hi) v = 
        v |> fcap |> fext (float lo) (float hi) |> round |> int
