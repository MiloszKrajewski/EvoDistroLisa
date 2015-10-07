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

---

### Point

	[lang=fs]
	type Point = { X: double; Y: double }

---

### Color & Brush

 	[lang=fs]
	type Color = { R: double; G: double; B: double }
	type Brush = { A: double; Color: Color }

---

### Polyline & Polygon

	[lang=fs]
	type Polyline = Point list
	type Polygon = { Brush: Brush; Points: Polyline }

---

### Pixels

	[lang=fs]
	type Pixels = { Width: int; Height: int; Pixels: uint32 array }

---

### Scene & RenderedScene

	[lang=fs]
	type Scene = { Polygons: Polygon list }
	type RenderedScene = { Scene: Scene; Fitness: double }

---

### Renderer & Fitter

	[lang=fs]
	type Renderer = Scene -> Pixels
	type Fitter = Pixels -> double

---

### Mutator

	[lang=fs]
	type Mutator = Scene -> Scene

---

### RNG

	[lang=fs]
	type RNG = unit -> double

---

### Factories

	[lang=fs]
	type MutatorFactory = RNG -> Mutator
	type FitterFactory = Pixels -> Fitter


### What are design patterns?

> [...] general reusable solution to a commonly occurring problem within a given context [...] -- **Wikipedia**

---

### What's wrong with OO design patterns?

- Are design patterns missing language features?
- Some patterns are actually anti-patterns
- Abuse of design patterns
- Design patterns are often too heavy

---

#### Are design patterns missing language features?

- Iterator (IEnumerator)
- Comparer (IComparer)

---

'Original' imperative iterator pattern:

	[lang=cs]
	var iterator = collection.GetEnumerator();
	while (iterator.MoveNext())
	{
		Console.WriteLine("item: {0}", iterator.Current);
	}

---

With 'foreach' feature:

	[lang=cs]
	foreach (var item in collection)
	{
		Console.WriteLine("item: {0}", item);
	}

---

Using 'extension method' and 'lambda' feature:

	[lang=cs]
	collection.ForEach(item => Console.WriteLine("item: {0}", item));

---

#### Pattern or anti-pattern?

- Singleton
- ServiceLocator

***

> "These days software is too complex. We can’t afford to speculate what else it should do. We need to really focus on what it needs." –- **Erich Gamma**

---

> "The problem with object-oriented languages is they've got all this implicit environment that they carry around with them. You wanted a banana but what you got was a gorilla holding the banana and the entire jungle." -- **Joe Armstrong**

---

> "[...] soon we had Mappers, Factories, MapperFactories, RepositoryFactories, MapperRepositoryFactories and MapperRepositoryFactoryFactories. Does this story sound familiar?" -- **Mark Seemann**, Functional Architecture with F#, Pluralsight

***

### Functional patterns are different

(so says Scott Wlaschin)

- No abstraction is too small
- Hide boilerplate code, expose business logic
- Functional patterns are unit of reusability
- Expressive and declarative

[Scott Wlaschin, Functional programming design patterns](https://vimeo.com/113588389)

---

### Expressive and declarative

Focus on intention not implementation.

Focus on **what** you are doing, not **how** you are doing it.

Do the same **what**, but using different **how**.

Reuse **how** for different **what**.

---

### What is wrong with 'how'?

	[lang=cs]
	X = X + Y;
	Y = X - Y;
	X = X - Y;

---

### Maybe we need comments?

	[lang=cs]
	X = X + Y; // add X and Y and store the result in X
	Y = X - Y; // subtract Y from X and store the result in Y
	X = X - Y; // subtract Y from X and store the result in X

---

### Let's try again using 'what'

	[lang=cs]
	Swap(ref X, ref Y);

---

### Focus on abstraction not implementation

Technically, it is very clear what it does:

	[lang=cs]
	if (portfolioIdsByTraderId[trader.Id].Contains(portfolio.Id))
	{
	    ...
	}

---

...although, I prefer this one:

	[lang=cs]
	if (trader.CanView(portfolio))
	{
	    ...
	}

[Kevlin Henney, Seven Ineffective Coding Habits of Many Programmers](https://vimeo.com/97329157)

***

### Functional design

#### You get some 'P's for free:

- Single Responsibility Principle
- Open/Closed Principle
- Liskov Substitution Principle
- Interface Segregation Principle
- Dependency Inversion Principle
- Reused Abstractions Principle
- Composite Reuse Principle

---

### Single Responsibility Principle

[...] class should have only a single resposibility [...]

---

### Open/Closed Principle

[...] open for extension, closed for modification [...]

---

### Liskov Substitution Principle

[...] formal, precise and verifiable interface specification [...]

---

### Interface Segregation Principle

[...] many client-specific interfaces, instead of one general-purpose interface [...]

---

### Dependency Inversion Principle

[...] depend on abstraction, not implementation [...]

---

### Reused Abstractions Principle

[...] abstraction is discovered not designed [...]

---

### Composite Reuse Principle

[...] composition over inheritance [...]

![FP design](images/composition.jpg)

***

### Real-life example

Let's delete some folders:

	[lang=cs]
	public void DeleteDirectoryTree(DirectoryInfo directory)
	{
		foreach (var child in directory.GetDirectories())
		{
			DeleteDirectoryTree(child);
		}

		foreach (var file in directory.GetFiles())
		{
			file.Delete();
		}

		directory.Delete();
	}

Let's call it 'business logic'.

Neat?

---

### Not so fast

...`directory.GetDirectories()` may throw exception.

First we need to take it out of `foreach`.

	[lang=cs]
	var children = directory.GetDirectories();

	foreach (var child in children)
	{
		DeleteDirectoryTree(child);
	}

---

We would also like to **Ignore** but **Log** first...

	[lang=cs]
	DirectoryInfo[] children = null;
	try
	{
		children = directory.GetDirectories();
	}
	catch (Exception e)
	{
		Trace.TraceError("{0}", e);
	}
	if (children != null)
	{
		foreach (var child in children)
		{
			DeleteDirectoryTree(child);
		}
	}

(find 'business logic')

---

...same with `directory.GetFiles()`...

	[lang=cs]
	FileInfo[] files = null;
	try
	{
		files = directory.GetFiles();
	}
	catch (Exception e)
	{
		Trace.TraceError("{0}", e);
	}
	if (files != null)
	{
		foreach (var file in files)
		{
			file.Delete();
		}
	}

(find 'business logic')

---

I think `file.Delete()` can definitely throw exception...

	[lang=cs]
	if (files != null)
	{
		foreach (var file in files)
		{
			try
			{
				file.Delete();
			}
			catch (Exception e)
			{
				Trace.TraceError("{0}", e);
			}
		}
	}

(find 'business logic')

---

...as well as `directory.Delete()`...

	[lang=cs]
	try
	{
		directory.Delete();
	}
	catch (Exception e)
	{
		Trace.TraceError("{0}", e);
	}

(find 'business logic')

---

It would be useful if we could retry few times in case file is in use...

	[lang=cs]
	try
	{
		var count = 0;
		while (true)
		{
			count++;
			try
			{
				file.Delete();
				break; // if success
			}
			catch (Exception e)
			{
				if (count >= 5)
					throw;
				Trace.TraceWarning("{0}", e);
				Thread.Sleep(200);
			}
		}
	}
	catch (Exception e)
	{
		Trace.TraceError("{0}", e);
	}

(find 'business logic')

---

So, the whole solution got inflated ~6 times (11 lines vs 64 lines), with no value added.

If you look at source code (Solution2), in my opinion, you will agree that original feeling 'what this piece of code does' is lost.

***

### Functional approach

In C# `void` is not regular type, like in F#.

	[lang=cs]
	typeof(Action) == typeof(Func<void, void>)
	typeof(Action<T>) == typeof(Func<T, void>)
	typeof(Func<T>) == typeof(Func<void, T>)

If it was, we would need only one implementation of many operators.

---

To avoid multiple implementations, we will 'cheat' with `Void` type...

	[lang=cs]
	public sealed class Void
	{
		public static readonly Void Instance = new Void();

		private Void() { }

		public override string ToString() { return "Void"; }
		public override bool Equals(object obj) { return obj is Void; }
		public override int GetHashCode() { return 0; }
	}

---

...and some 'wrist saving' Fx class (proudly called Functional eXtensions)...

	[lang=cs]
	public static class Fx
	{
		public static readonly Void Void = Void.Instance;

		public static Func<Void> ToFunc(this Action action)
		{
			return () => {
				action();
				return Void;
			};
		}

		public static Func<T, Void> ToFunc<T>(this Action<T> action)
		{
			return t => {
				action(t);
				return Void;
			};
		}
	}

---

We also don't like empty enumerables:

	[lang=cs]
	public static IEnumerable<T> NotNull(this IEnumerable<T> collection)
	{
		return collection ?? Enumerable.Empty<T>();
	}

(yes, it just uses Enumerable.Empty but allowing type inference)

---

So, we were saying we want to 'forgive' some exceptions, **Ignore** but **Log** first:

	[lang=cs]
	public static T Forgive<T>(this Func<T> func, T defaultValue = default(T))
	{
		try
		{
			return func();
		}
		catch (Exception e)
		{
			Trace.TraceWarning("{0}", e);
			return defaultValue;
		}
	}

	public static void Forgive(this Action action)
	{
		Forgive(action.ToFunc()); // wrist saving already...
	}

---

So, recursive dive is changed from:

	[lang=cs]
	foreach (var child in directory.GetDirectories())
	{
		DeleteDirectoryTree(child);
	}

...to:

	[lang=cs]
	foreach (var child in Fx.Forgive(() => directory.GetDirectories()).NotNull())
	{
		DeleteDirectoryTree(child);
	}

---

...it could be actually:

	[lang=cs]
	Fx.Forgive(() => directory.GetDirectories(), Enumerable.Empty<DirectoryInfo>())

...but I do like type inference.

---

Same thing with files:

	[lang=cs]
	foreach (var file in Fx.Forgive(() => directory.GetFiles()).NotNull())
	{
		Fx.Forgive(() => file.Delete());
	}

...and folders:

	[lang=cs]
	Fx.Forgive(() => directory.Delete());

---

`Retry` seems to be a little bit more complicated:

	[lang=cs]
	public static T Retry<T>(
		Func<T> action, Func<int, TimeSpan, bool> retry, Action<TimeSpan> wait = null)
	{
		var count = 0;
		var started = DateTimeOffset.Now;
		var exceptions = new List<Exception>();
		wait = wait ?? (_ => { });

		while (true)
		{
			count++;

			try
			{
				return action();
			}
			catch (Exception e)
			{
				Trace.TraceWarning("{0}", e);
				exceptions.Add(e);
			}

			var elapsed = DateTimeOffset.Now.Subtract(started);
			if (!retry(count, elapsed))
				break;
			wait(elapsed);
		}

		throw new AggregateException(exceptions);
	}

---

...and we need alternative implementation for `Action`:

	[lang=cs]
	public static void Retry(
		Action action, Func<int, TimeSpan, bool> retry, Action<TimeSpan> wait = null)
	{
		Retry(action.ToFunc(), retry, wait);
	}

---

...but the whole file deletion loop will look like this now:

	[lang=cs]
	foreach (var file in Forgive(() => directory.GetFiles()).NotNull())
	{
		Fx.Forgive(
			() => Fx.Retry(
				() => file.Delete(),
				(c, _) => c < 5,
				_ => Thread.Sleep(200)));
	}

---

...which makes whole solution still fitting one page:

	[lang=cs]
	public static void DeleteDirectoryTree(DirectoryInfo directory)
	{
		foreach (var child in Fx.Forgive(() => directory.GetDirectories()).NotNull())
		{
			DeleteDirectoryTree(child);
		}

		foreach (var file in Fx.Forgive(() => directory.GetFiles()).NotNull())
		{
			Fx.Forgive(
				() => Fx.Retry(
					() => file.Delete(),
					(c, _) => c < 5,
					_ => Thread.Sleep(200)));
		}

		Fx.Forgive(() => directory.Delete());
	}

---

...actually, with some behind-the-scene magic, it could be something like:

    [lang=cs]
    // ...
    files.ForEach(DeleteFile.Retry((c, _) => c < 5, _ => Thread.Sleep(200)).Forgive())
    // ...

...but magic is quite heavy, and outside the scope of this presentation.

***

### Passing behaviour

Note, that behaviour modification like `Forgive` and `Retry` can be passed around and externally injected:

	[lang=cs]
	public static void DeleteDirectoryTree(
		DirectoryInfo directory,
		Action<Action> directoryDeletionStrategy = null,
		Action<Action> fileDeletionStrategy = null)
	{
		fileDeletionStrategy = fileDeletionStrategy ?? (a => a());
		directoryDeletionStrategy = directoryDeletionStrategy ?? (a => a());

		foreach (var child in Fx.Forgive(() => directory.GetDirectories()).NotNull())
		{
			DeleteDirectoryTree(child);
		}

		foreach (var file in Fx.Forgive(() => directory.GetFiles()).NotNull())
		{
			fileDeletionStrategy(() => file.Delete());
		}

		directoryDeletionStrategy(() => directory.Delete());
	}

---

### Extract recursion

Actually, the whole recursion model here is a DFS/post-order algorithm:

	[lang=cs]
	public static IEnumerable<T> RecursivelySelectMany<T>(
		this IEnumerable<T> collection,
		Func<T, IEnumerable<T>> selector)
	{
		foreach (var item in collection)
		{
			foreach (var subitem in RecursivelySelectMany(selector(item), selector))
				yield return subitem;
			yield return item;
		}
	}

---

The function to delete all files matching criteria in directory tree could be expressed as:

	[lang=cs]
	public void DeleteFilesInTree(
		IEnumerable<DirectoryInfo> roots,
		Func<DirectoryInfo, bool> directoryFilter,
		Func<FileInfo, bool> fileFilter)
	{
		roots
		.RecursivelySelectMany(d => Fx.Forgive(() => d.GetDirectories()).NotNull())
		.Where(d => directoryFilter == null || directoryFilter(d))
		.Lookahead()
		.ForEach(directory =>
			directory
			.SelectMany(d => Fx.Forgive(() => d.GetFile()).NotNull())
			.Where(f => fileFilter == null || fileFileter(f))
			.AsParallel()
			.ForAll(f => Fx.Forgive(() => Fx.Retry(() =>
				f.Delete(), (c, _) => c < 5, _ => Thread.Sleep(200))))
		);
	}

---

Where `Lookahead` is prefetching items in separate thread:

	[lang=cs]
	public static IEnumerable<T> Lookahead<T>(
		this IEnumerable<T> collection, CancellationToken? token = null)
	{
		token = token ?? CancellationToken.None;
		var queue = new BlockingCollection<T>(new ConcurrentQueue<T>());

		Task.Factory.StartNew(() => {
			try
			{
				collection.ForEach(i => queue.Add(i, token.Value));
			}
			finally
			{
				queue.CompleteAdding();
			}
		}, token.Value, TaskCreationOptions.LongRunning, TaskScheduler.Default);

		return queue.GetConsumingEnumerable(token.Value);
	}

---

Even, if it is quite complex now, it has features which just would be a nightmare to implement in imperative code.

***

### Lazy

	[lang=cs]
	public static Func<T> Lazy<T>(Func<T> factory)
	{
		var variable = new Lazy<T>(factory);
		return () => variable.Value;
	}

---

### Weak

	[lang=cs]
	public static Func<T> Weak<T>(T value)
		where T: class
	{
		var reference = new WeakReference(value);
		return () => (T)reference.Target;
	}

---

### Cache

	[lang=cs]
	public static Func<T> Cache<T>(Func<T> factory)
		where T: class
	{
		var factorySync = new object();
		Func<T> reference = () => null;
		return () => {
			var value = reference();
			if (null == value)
			{
				lock (factorySync)
				{
					value = reference();
					if (null == value)
					{
						value = factory();
						reference = Weak(value);
					}
				}
			}
			return value;
		};
	}

***

### Delay

	[lang=cs]
	private static void Delay(
		int timeout, CancellationToken cancellationToken, Action action)
	{
		var id = Guid.NewGuid();
		var timer = default(Timer);
		var ready = new ManualResetEventSlim(false);
		var handler = new TimerCallback(_ => {
			ready.Wait();
			ready.Dispose();

			try
			{
				Timer removed;
				var execute =
					!cancellationToken.IsCancellationRequested &&
					_timerMap.TryRemove(id, out removed); // assert true
				if (execute)
					action();
			}
			finally
			{
				timer.Dispose();
			}
		});

		timer = new Timer(handler, null, timeout, Timeout.Infinite);
		_timerMap.TryAdd(id, timer); // assert true
		ready.Set();
	}

***

### Questions?

***

### Turn back!

***

### Don't go there!

***

### Told you!

***

Degrees of Freedom
Remove boilerplate, but don't be scared
Identify moving parts, cheaper to extract

Forgive
Retry


RecursivelySelectMany
Lookahead
Defer
Scope
Apply
Lazy
Weak
Pack/Unpack
Null
Unit
Curry
Partial
Match

Option
Result
