- title : EvoDistroLisa - EvoLisa reborn
- description : EvoDistroLisa - EvoLisa reborn
- author : Milosz Krajewski
- theme : beige
- transition : default

***

## EvoDistroLisa - EvoLisa resurected by Agents

youtube: http://goo.gl/g6hnnm
blog: http://goo.gl/UY48nn
github: https://goo.gl/9kBDiI

***

### About me

- Milosz Krajewski
- @MrKrashan
- BLOBAs @ Sepura
- first line of code written in ~1984
- C, C++, C#, SQL, Java
- (Iron)Python, F#, Scala

---

### Background

- Algorithms
- Data Structures
- Algorithm Complexity
- Graph Theory
- Design Patterns

---

### Most recently

- Parallel
- Distributed
- Reactive
- Functional

***

### Domain

Note: the code presented is usually a little bit idealized with "implementation induced ugliness" removed

---

### Point

	[lang=fs]
	type Point = { X: double; Y: double }

---

### Brush

 	[lang=fs]
	type Brush = { A: double; R: double; G: double; B: double }

---

### Polygon

	[lang=fs]
	type Polygon = { Brush: Brush; Points: Point array }

---

### Pixels

	[lang=fs]
	type Pixels = { Width: int; Height: int; Pixels: uint32 array }

`uint32 array` is used for performance reasons and should store pixels in PARGB32 format.

---

### Scene & RenderedScene

	[lang=fs]
	type Scene = Polygon array
	type RenderedScene = { Scene: Scene; Fitness: double }

---

### Publish

	[lang=fs]
	let improved = Event<RenderedScene>()
	let publish scene = improved.Trigger scene; scene

	// let inline apply func arg = func arg |> ignore; arg

---

### Select

	[lang=fs]
	let select champion challengers =
		let fitnessOf (scene: RenderedScene) = scene.Fitness
		let challenger = challengers |> Seq.maxBy fitnessOf
		match fitnessOf champion >= fitnessOf challenger with
		| true -> champion
		| _ -> challenger |> publish

---

### Improve

	[lang=fs]
	type Mutator = Scene -> Scene
	type Renderer = Scene -> Pixels
	type Fitter = Pixels -> double

	let improve mutate render fit champion =
		let challenger = champion |> mutate
		let fitness = challenger |> render |> fit
		{ Scene = challenger; Fitness = fitness }

---

	[lang=fs]
	let activeLoop mutate render fit champion inbox = async {
		let! challengers = inbox |> Agent.recvMany
		let champion = challengers |> select publisher champion
		let challenger = champion |> improve mutate render fit
		inbox |> Agent.send challenger
		do! activeLoop mutate render fit champion inbox
	}

---

	[lang=fs]
	let passiveLoop champion inbox = async {
		let! challengers = inbox |> Agent.recvMany
		let champion = challengers |> select publish champion
		do! passiveLoop champion inbox
	}

---

### Random Number Generator

	[lang=fs]
	type RNG = unit -> double

---

### Let's mutatate

	[lang=fs]
	let mutateScene rng scene =
		polygons
		|> removePolygon rng
		|> insertPolygon rng
		|> shufflePolygons rng
		|> Array.map (mutatePolygon rng)

---

	[lang=fs]
	let mutatePolygon rng polygon =
		{ polygon with
			Brush = mutateBrush rng polygon.Brush
			Points = mutatePoints rng polygon.Points }

---

	[lang=fs]
	let mutateBrush rng brush =
		{ brush with
			A = mutateValue rng brush.A
			R = mutateValue rng brush.R
			G = mutateValue rng brush.G
			B = mutateValue rng brush.B }

---

	[lang=fs]
	let mutatePoints rng points =
		points
		|> removePoint rng
		|> insertPoint rng
		|> shufflePoints rng
		|> Array.map (mutatePoint rng)

---

	[lang=fs]
	let mutatePoint rng point =
		{ point with
			X = mutateValue rng point.X
			Y = mutateValue rng point.Y }

***

### Questions?

***

### Turn back!

***

### Don't go there!

***

### Told you!
