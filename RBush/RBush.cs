using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RBush
{
	// Submodule test
	public partial class RBush<T> : ISpatialDatabase<T>, ISpatialIndex<T> where T : ISpatialData
	{
		private const int DefaultMaxEntries = 9;
		private const int MinimumMaxEntries = 4;
		private const int MinimumMinEntries = 2;
		private const double DefaultFillFactor = 0.4;

		private int maxEntries;
		private int minEntries;
		internal Node root;

		public RBush() : this(DefaultMaxEntries) { }
		public RBush(int maxEntries)
			: this(maxEntries, EqualityComparer<T>.Default) { }
		public RBush(int maxEntries, EqualityComparer<T> comparer)
		{
			this.maxEntries = Math.Max(MinimumMaxEntries, maxEntries);
			this.minEntries = Math.Max(MinimumMinEntries, (int)Math.Ceiling(this.maxEntries * DefaultFillFactor));

			this.Clear();
		}

		public int Count { get; private set; }

		// Returns the total envelope.
		public Envelope Envelope => root.Envelope;

		public void Clear()
		{
			this.root = new Node(new List<ISpatialData>(), 1);
			this.Count = 0;
		}

		// OLD.
		internal IReadOnlyList<T> Search_Old() => GetAllChildren_Old(this.root).ToList();
		// NEW, generally more efficient.
		public IReadOnlyList<T> Search() => GetAllChildren(this.root).ToList();

		// OLD.
		internal IReadOnlyList<T> Search_Old(in Envelope boundingBox)
		{
			return DoSearch(boundingBox).Select(x => (T)x.Peek()).ToList();
		}
		// NEW, generally more efficient.
		public IReadOnlyList<T> Search(in Envelope boundingBox)
		{
			return DoSearchDirect(boundingBox);
		}

		public void Insert(T item)
		{
			Insert(item, this.root.Height);
			this.Count++;
		}

		public void BulkLoad(IEnumerable<T> items)
		{
			var data = items.Cast<ISpatialData>().ToList();
			if (data.Count == 0) return;

			if (this.root.IsLeaf &&
				this.root.Children.Count + data.Count < maxEntries)
			{
				foreach (var i in data)
					Insert((T)i);
				return;
			}

			if (data.Count < this.minEntries)
			{
				foreach (var i in data)
					Insert((T)i);
				return;
			}

			var dataRoot = BuildTree(data);
			this.Count += data.Count;

			if (this.root.Children.Count == 0)
				this.root = dataRoot;
			else if (this.root.Height == dataRoot.Height)
			{
				if (this.root.Children.Count + dataRoot.Children.Count <= this.maxEntries)
				{
					foreach (var isd in dataRoot.Children)
						this.root.Add(isd);
				}
				else
					SplitRoot(dataRoot);
			}
			else
			{
				if (this.root.Height < dataRoot.Height)
				{
					var tmp = this.root;
					this.root = dataRoot;
					dataRoot = tmp;
				}

				this.Insert(dataRoot, this.root.Height - dataRoot.Height);
			}
		}

		public bool Delete(T item)
		{
			bool success = false;
			var candidates = DoSearch(item.Envelope);

			foreach (var c in candidates
				.Where(c => object.Equals(item, c.Peek())))
			{
				var path = c.Pop();

				Node currentNode = path.Peek() as Node;

				success = currentNode.Children.Remove(item);
				if (success) this.Count--;

				while (!path.IsEmpty)
				{
					currentNode.ResetEnvelope();
					path = path.Pop();
				}
			}

			return success;
		}
	}
}
