using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using EvoDistroLisa.Domain;
using EvoDistroLisa.Engine;
using EvoDistroLisa.Engine.ZMQ;
using GenArt.Core.AST;
using GenArt.Core.AST.Mutation;
using Microsoft.FSharp.Core;
using Brush = EvoDistroLisa.Domain.Brush;
using Point = EvoDistroLisa.Domain.Point;

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

		public Scene.Scene Mutate(Scene.Scene scene)
		{
			var drawing = scene.Cargo as DnaDrawing;
			drawing = drawing == null ? new DnaDrawing(200, 200) : drawing.Clone();
			while (!drawing.IsDirty) drawing.Mutate();
			return ReconstructScene(drawing);
		}

		private Scene.Scene ReconstructScene(DnaDrawing drawing)
		{
			return new Scene.Scene(drawing.Polygons.Select(ReconstructPoly).ToArray(), drawing);
		}

		private Polygon.Polygon ReconstructPoly(DnaPolygon polygon)
		{
			return new Polygon.Polygon(
				ReconstructBrush(polygon.Brush), 
				polygon.Points.Select(ReconstructPoint).ToArray());
		}

		private Domain.Brush.Brush ReconstructBrush(DnaBrush brush)
		{
			return new Brush.Brush(
				brush.Alpha / 255.0, 
				brush.Red / 255.0, brush.Green / 255.0, brush.Blue / 255.0);
		}

		private Domain.Point.Point ReconstructPoint(DnaPoint point)
		{
			return new Point.Point(point.X / 200.0, point.Y / 200.0);
		}

		public ShellViewModel()
		{
			var token = new CancellationTokenSource();

			var imgPath = Path.Combine(Environment.CurrentDirectory, "monalisa.png");
			var evoPath = imgPath + ".evoboot";

			Scene.Pixels pixels;
			RenderedScene scene0;

			if (File.Exists(evoPath))
			{
				var message = Pickler.load<BootstrapScene>(
					File.ReadAllBytes(evoPath));
				pixels = message.Pixels;
				scene0 = message.Scene;
			}
			else
			{
				using (var bitmap = new Bitmap(imgPath))
					pixels = Win32Fitness.createPixels(bitmap);
				scene0 = RenderedScene.Zero;
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

			//RenderTargetBitmap target = null;
			//agent0.Improved
			//	.Sample(TimeSpan.FromMilliseconds(200))
			//	.ObserveOn(DispatcherScheduler.Current)
			//	.Subscribe(rendered => {
			//		if (target == null)
			//			target = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
			//		var scene = rendered.Scene;
			//		WpfRender.render(target, scene);
			//		Image = target;
			//	});

			WriteableBitmap target = null;
			agent0.Improved
				.Sample(TimeSpan.FromMilliseconds(200))
				.ObserveOn(DispatcherScheduler.Current)
				.Subscribe(rendered => {
					if (target == null)
						target = new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
					var scene = rendered.Scene;
					WBxRender.render(target, scene);
					Image = target;
				});

			agent0.Push(scene0);
		}
	}
}