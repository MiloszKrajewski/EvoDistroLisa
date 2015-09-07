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

		public ShellViewModel()
		{
			var token = new CancellationTokenSource();

			var imgPath = Path.Combine(Environment.CurrentDirectory, "monalisa.orig.bmp");
			// var imgPath = Path.Combine(Environment.CurrentDirectory, "fsharp.png");
			var evoPath = imgPath + ".evoboot";

			Scene.Pixels pixels;
			Scene.RenderedScene scene0;

			if (File.Exists(evoPath))
			{
				var message = Pickler.load<Scene.BootstrapScene>(
					File.ReadAllBytes(evoPath));
				pixels = message.Pixels;
				scene0 = message.Scene;
			}
			else
			{
				using (var bitmap = new Bitmap(imgPath))
					pixels = Win32Fitness.createPixels(bitmap);
				scene0 = Scene.initialScene;
			}

			var width = pixels.Width;
			var height = pixels.Height;

			var agent0 = Agent.createAgent(
				pixels,
				Agent.createMutator(),
				Win32Fitness.createRendererFactory(true),
				Scene.initialScene,
				token.Token);

			var agent1 = Agent.createAgent(
				pixels,
				Agent.createMutator(),
				WpfFitness.createRendererFactory(),
				Scene.initialScene,
				token.Token);
			Agent.attachAgent(agent1, agent0);

			var agent2 = Agent.createAgent(
				pixels,
				Agent.createMutator(),
				WpfFitness.createRendererFactory(),
				Scene.initialScene,
				token.Token);
			Agent.attachAgent(agent2, agent0);

			var agent3 = Agent.createAgent(
				pixels,
				Agent.createMutator(),
				WpfFitness.createRendererFactory(),
				Scene.initialScene,
				token.Token);
			Agent.attachAgent(agent3, agent0);

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

			agent0.Improved
				.Sample(TimeSpan.FromMilliseconds(200))
				.ObserveOn(DispatcherScheduler.Current)
				.Subscribe(rendered => {
					var scene = rendered.Scene;
					var target = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
					WpfRender.render(target, scene);
					Image = target;
				});

			agent0.Push(scene0);
		}
	}
}