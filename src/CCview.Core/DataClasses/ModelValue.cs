using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCview.Core.DataClasses
{
    public readonly struct ModelValue : IEquatable<ModelValue>
    {
        public readonly int ItemId, Aleph, ThmId;
        public ModelValue(int a, int b, int c)
        {
            ItemId = a;
            Aleph = b;
            ThmId = c;
        }
        public ModelValue(int[] array)
        {
            if (array == null || array.Length != 3)
            {
                throw new ArgumentException("Array must be of length 3.");
            }
            ItemId = array[0];
            Aleph = array[1];
            ThmId = array[2];
        }
        public ModelValue(List<int> list)
        {
            if (list == null || list.Count != 3)
            {
                throw new ArgumentException("List must be of length 3.");
            }
            ItemId = list[0];
            Aleph = list[1];
            ThmId = list[2];
        }
        public bool Equals(ModelValue other)
        {
            return ItemId == other.ItemId
                && Aleph == other.Aleph
                && ThmId == other.ThmId;
        }
        public override bool Equals(object? obj)
        {
            return obj is ModelValue other && Equals(other);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(ItemId, Aleph, ThmId);
        }
        public int[] ToArray() => [ItemId, Aleph, ThmId];
        public List<int> ToList() => [ItemId, Aleph, ThmId];
        public HashSet<int> ToHashSet() => [ItemId, Aleph, ThmId];
        public static bool operator ==(ModelValue left, ModelValue right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(ModelValue left, ModelValue right)
        {
            return !(left == right);
        }
        public static implicit operator ModelValue(List<int> list) => new(list);
        public static implicit operator ModelValue(int[] array) => new(array);
    }
}
