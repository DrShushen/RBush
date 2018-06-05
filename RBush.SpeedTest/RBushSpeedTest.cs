using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using System.Threading;
using RBush;

namespace RBush.SpeedTest
{
	public class Item : ISpatialData
	{
		private readonly Envelope _envelope;

		public Item(double minX, double minY, double maxX, double maxY)
		{
			_envelope = new Envelope(minX, minY, maxX, maxY);
		}

		public ref readonly Envelope Envelope
		{
			get
			{
				return ref _envelope;
			}
		}
	}

	public static class ItemListGenerator
	{
		static Random _random = new Random();

		private static Item generateItem(double scale)
		{
			List<double> points = new List<double>() { _random.NextDouble() * scale, _random.NextDouble() * scale, _random.NextDouble() * scale, _random.NextDouble() * scale };
			points.Sort();
			return new Item(points[0], points[2], points[1], points[3]);
		}

		public static IEnumerable<Item> GenerateItems(int numberOfItems, double spaceScale)
		{
			List<Item> result = new List<Item>(numberOfItems);
			for (int i = 0; i < numberOfItems; i++)
			{
				result.Add(generateItem(spaceScale));
			}
			return result.AsEnumerable();
		}
	}

	public class RBushSpeedTester
	{
		public void RunTest(string name, int itemCount, Action execute)
		{
			long elapsedMs = 0;
			var watch = System.Diagnostics.Stopwatch.StartNew();

			Console.Write(String.Format("{0:n0}", itemCount) + " items: " + name + "... ");

			watch.Reset();
			watch.Start();

			execute();

			watch.Stop();
			elapsedMs = watch.ElapsedMilliseconds;
			
			Console.Write("Time (ms): " + String.Format("{0:n0}", elapsedMs));
			Console.WriteLine();
		}

		public void RunTestGroup(int maxEntries, int numberOfItems)
		{
			RBush<Item> rBush = new RBush<Item>(maxEntries);
			
			double spaceScale = 50;
			IEnumerable<Item> items = ItemListGenerator.GenerateItems(numberOfItems, spaceScale);

			Console.WriteLine("maxEntries = " + maxEntries);

			// Tests:

			RunTest("BulkLoad", numberOfItems, () => { rBush.BulkLoad(items); });

			RunTest("Search OLD", numberOfItems, () => { rBush.Search_Old(); });
			RunTest("Search NEW", numberOfItems, () => { rBush.Search(); });

			RunTest("Search envelope (Inf. bounds) OLD", numberOfItems, () => { rBush.Search_Old(Envelope.InfiniteBounds); });
			RunTest("Search envelope (Inf. bounds) NEW", numberOfItems, () => { rBush.Search(Envelope.InfiniteBounds); });

			RunTest("Iterate through IEnumerable [for comparison]", numberOfItems, () => { foreach (Item i in items) { } });

			Console.ReadLine();
		}
	}

	class RBushSpeedTest
	{
		static void Main(string[] args)
		{

			RBushSpeedTester tester = new RBushSpeedTester();

			tester.RunTestGroup(9, 10000);
			tester.RunTestGroup(9, 50000);
			tester.RunTestGroup(9, 100000);

			tester.RunTestGroup(16, 10000);
			tester.RunTestGroup(16, 100000);
			tester.RunTestGroup(16, 200000);

			tester.RunTestGroup(32, 10000);
			tester.RunTestGroup(32, 100000);
			tester.RunTestGroup(32, 500000);
			//tester.RunTestGroup(32, 1000000);  // Slowest.

		}
	}
}
