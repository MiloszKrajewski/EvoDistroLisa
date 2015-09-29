using Caliburn.Micro;
using EvoDistroLisa.Engine;
using GenArt.Core.AST;
using GenArt.Core.AST.Mutation;
using Microsoft.FSharp.Core;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EvoDistroLisa
{
	public class ShellViewModel: PropertyChangedBase, IShell
	{
		private BitmapSource _image;
		private string _speed;

		public BitmapSource Image
		{
			get { return _image; }
			set
			{
				_image = value;
				NotifyOfPropertyChange(() => Image);
			}
		}

		public string Speed
		{
			get { return _speed; }
			set
			{
				_speed = value;
				NotifyOfPropertyChange(() => Speed);
			}
		}

		public Domain.Scene Mutate(Domain.Scene scene)
		{
			var drawing = scene.Cargo as DnaDrawing;
			drawing = drawing == null ? new DnaDrawing(200, 200) : drawing.Clone();
			while (!drawing.IsDirty) drawing.Mutate();
			return ReconstructScene(drawing);
		}

		private Domain.Scene ReconstructScene(DnaDrawing drawing)
		{
			return new Domain.Scene(drawing.Polygons.Select(ReconstructPoly).ToArray(), drawing);
		}

		private Domain.Polygon ReconstructPoly(DnaPolygon polygon)
		{
			return new Domain.Polygon(
				ReconstructBrush(polygon.Brush), 
				polygon.Points.Select(ReconstructPoint).ToArray());
		}

		private Domain.Brush ReconstructBrush(DnaBrush brush)
		{
			return new Domain.Brush(
				brush.Alpha / 255.0, 
				brush.Red / 255.0, brush.Green / 255.0, brush.Blue / 255.0);
		}

		private Domain.Point ReconstructPoint(DnaPoint point)
		{
			return new Domain.Point(point.X / 200.0, point.Y / 200.0);
		}

		public ShellViewModel()
		{
			var token = new CancellationTokenSource();

			var imgPath = Path.Combine(Environment.CurrentDirectory, "monalisa.png");
			var evoPath = imgPath + ".evoboot";

			Domain.Pixels pixels;
			Domain.RenderedScene scene0;

			if (File.Exists(evoPath))
			{
				var message = Pickler.load<Domain.BootstrapScene>(
					File.ReadAllBytes(evoPath));
				pixels = message.Pixels;
				scene0 = message.Scene;
			}
			else
			{
				using (var bitmap = new Bitmap(imgPath))
					pixels = Win32Fitness.createPixels(bitmap);
				scene0 = Domain.RenderedScene.Zero;
			}

			var width = pixels.Width;
			var height = pixels.Height;

			var agent0 = Agent.createAgent(
				pixels,
				Agent.createMutator(),
				// FuncConvert.ToFSharpFunc<Scene.Scene, Scene.Scene>(Mutate),
				// WpfFitness.createRendererFactory(),
				Win32Fitness.createRendererFactory(true),
				// WBxFitness.createRendererFactory(),
				scene0,
				token.Token);

			//var agent1 = Agent.createAgent(
			//	pixels,
			//	Agent.createMutator(),
			//	WpfFitness.createRendererFactory(),
			//	Scene.initialScene,
			//	token.Token);
			//Agent.attachAgent(agent1, agent0);

			//var agent2 = Agent.createAgent(
			//	pixels,
			//	Agent.createMutator(),
			//	WpfFitness.createRendererFactory(),
			//	Scene.initialScene,
			//	token.Token);
			//Agent.attachAgent(agent2, agent0);

			//var agent3 = Agent.createAgent(
			//	pixels,
			//	Agent.createMutator(),
			//	WpfFitness.createRendererFactory(),
			//	Scene.initialScene,
			//	token.Token);
			//Agent.attachAgent(agent3, agent0);

			//var agentZ = ZmqServer.createServer(
			//	5801, 
			//	pixels, 
			//	Scene.initialScene,
			//	token.Token);
			//Agent.attachAgent(agentZ, agent0);

			var started = DateTimeOffset.Now;

			agent0.Mutated
				.Select(_ => agent0.Mutations)
				.Timestamp()
				.Select(c => c.Value / (c.Timestamp.Subtract(started).TotalSeconds))
				.SlidingAverage(c => c, TimeSpan.FromSeconds(5))
				.Sample(TimeSpan.FromSeconds(1))
				.ObserveOn(DispatcherScheduler.Current)
				.Subscribe(args => Speed = string.Format("{0:0.0000}/s", args));

			//agent0.Mutated
			//	.Select(_ => agent0.Mutations)
			//	.Sample(TimeSpan.FromSeconds(1))
			//	.ObserveOn(DispatcherScheduler.Current)
			//	.Subscribe(m => Speed = string.Format("{0}/{1}", m, agent0.Best.Scene.Polygons.Length));

			var target = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
			agent0.Improved
				//.Sample(TimeSpan.FromMilliseconds(200))
				.ObserveOn(DispatcherScheduler.Current)
				.Subscribe(rendered => {
					WpfRender.render(target, rendered.Scene);
					Image = target;
				});

			//var target = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
			//agent0.Improved
			//	//.Sample(TimeSpan.FromMilliseconds(1000))
			//	.ObserveOn(DispatcherScheduler.Current)
			//	.Subscribe(rendered => {
			//		WBxRender.render(target, rendered.Scene);
			//		Image = target;
			//	});

			agent0.Push(scene0);
		}
	}
}