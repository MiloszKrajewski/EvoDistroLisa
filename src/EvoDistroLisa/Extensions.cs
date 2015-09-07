using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace EvoDistroLisa
{
	public static class Extensions
	{
		public static IObservable<double> SlidingAverage<T>(
			this IObservable<T> observable,
			Func<T, double> selector,
			TimeSpan interval)
		{
			var source = observable.Select(selector).Timestamp();
			return Observable.Create<double>(o => {
				var queue = new Queue<Timestamped<double>>();
				return source.Subscribe(
					i => {
						queue.Enqueue(i);
						var limit = i.Timestamp.Subtract(interval);
						var deleted = false;
						while (queue.Count >= 1 && queue.Peek().Timestamp < limit)
						{
							deleted = true;
							queue.Dequeue();
						}
						if (deleted && queue.Count >= 1)
							o.OnNext(queue.Average(ts => ts.Value));
					},
					o.OnError,
					o.OnCompleted);
			});
		}
	}
}
