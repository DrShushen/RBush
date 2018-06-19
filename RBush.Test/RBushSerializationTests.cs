using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Xunit;

namespace RBush.Test
{
	public static class BinarySerializeExtensions
	{
		public static byte[] BinarySerialize(this Object obj)
		{
			if (obj == null)
			{
				return null;
			}

			using (var memoryStream = new MemoryStream())
			{
				var binaryFormatter = new BinaryFormatter();

				binaryFormatter.Serialize(memoryStream, obj);

				return memoryStream.ToArray();
			}
		}

		public static Object BinaryDeserialize(this byte[] arrBytes)
		{
			using (var memoryStream = new MemoryStream())
			{
				var binaryFormatter = new BinaryFormatter();

				memoryStream.Write(arrBytes, 0, arrBytes.Length);
				memoryStream.Seek(0, SeekOrigin.Begin);

				return binaryFormatter.Deserialize(memoryStream);
			}
		}
	}

	public class RBushSerializationTests
	{
		[Serializable]
		private class Point : ISpatialData, IComparable<Point>
		{
			private Envelope _envelope;

			public Point(double minX, double minY, double maxX, double maxY)
			{
				_envelope = new Envelope(
					minX: minX,
					minY: minY,
					maxX: maxX,
					maxY: maxY);
			}

			public ref readonly Envelope Envelope => ref _envelope;

			public int CompareTo(Point other)
			{
				if (this.Envelope.MinX != other.Envelope.MinX)
					return this.Envelope.MinX.CompareTo(other.Envelope.MinX);
				if (this.Envelope.MinY != other.Envelope.MinY)
					return this.Envelope.MinY.CompareTo(other.Envelope.MinY);
				if (this.Envelope.MaxX != other.Envelope.MaxX)
					return this.Envelope.MaxX.CompareTo(other.Envelope.MaxX);
				if (this.Envelope.MaxY != other.Envelope.MaxY)
					return this.Envelope.MaxY.CompareTo(other.Envelope.MaxY);
				return 0;
			}
		}

		private static double[,] data =
		{
			{0, 0, 0, 0},       {10, 10, 10, 10},   {20, 20, 20, 20},   {25, 0, 25, 0},     {35, 10, 35, 10},   {45, 20, 45, 20},   {0, 25, 0, 25},     {10, 35, 10, 35},
			{20, 45, 20, 45},   {25, 25, 25, 25},   {35, 35, 35, 35},   {45, 45, 45, 45},   {50, 0, 50, 0},     {60, 10, 60, 10},   {70, 20, 70, 20},   {75, 0, 75, 0},
			{85, 10, 85, 10},   {95, 20, 95, 20},   {50, 25, 50, 25},   {60, 35, 60, 35},   {70, 45, 70, 45},   {75, 25, 75, 25},   {85, 35, 85, 35},   {95, 45, 95, 45},
			{0, 50, 0, 50},     {10, 60, 10, 60},   {20, 70, 20, 70},   {25, 50, 25, 50},   {35, 60, 35, 60},   {45, 70, 45, 70},   {0, 75, 0, 75},     {10, 85, 10, 85},
			{20, 95, 20, 95},   {25, 75, 25, 75},   {35, 85, 35, 85},   {45, 95, 45, 95},   {50, 50, 50, 50},   {60, 60, 60, 60},   {70, 70, 70, 70},   {75, 50, 75, 50},
			{85, 60, 85, 60},   {95, 70, 95, 70},   {50, 75, 50, 75},   {60, 85, 60, 85},   {70, 95, 70, 95},   {75, 75, 75, 75},   {85, 85, 85, 85},   {95, 95, 95, 95}
		};

		static Point[] points =
			Enumerable.Range(0, data.GetLength(0))
				.Select(i => new Point(
					minX: data[i, 0],
					minY: data[i, 1],
					maxX: data[i, 2],
					maxY: data[i, 3]))
				.ToArray();

		[Fact]
		public void BinarySerializeBasicTest()
		{
			RBush<Point> treeOriginal = new RBush<Point>(maxEntries: 4);
			treeOriginal.BulkLoad(points);
			Envelope searchEnvelope = new Envelope(20, 15, 60, 31);

			byte[] byteArr = treeOriginal.BinarySerialize();
			RBush<Point> treeDeserialized = byteArr.BinaryDeserialize() as RBush<Point>;
			
			Assert.True(treeOriginal.Count == treeDeserialized.Count);
			Assert.True(treeOriginal.Envelope == treeDeserialized.Envelope);
			IReadOnlyList<Point> treeOriginalSearchResult = treeOriginal.Search(searchEnvelope);
			IReadOnlyList<Point> treeDeserializedSearchResult = treeDeserialized.Search(searchEnvelope);
			Assert.True(treeOriginalSearchResult.Count == treeDeserializedSearchResult.Count);
			Assert.True(treeOriginalSearchResult[0].Envelope == treeDeserializedSearchResult[0].Envelope);
		}
	}
}
