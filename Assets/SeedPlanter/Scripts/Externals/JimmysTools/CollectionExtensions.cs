using System;
using System.Collections.Generic;
using System.Linq;

using Random = UnityEngine.Random;

namespace JimmysUnityUtilities
{
    public static class CollectionExtensions
    {

        public static bool ContainsIndex(this Array array, int index, int dimension)
        {
            if (index < 0)
                return false;

            return index < array.GetLength(dimension);
        }
    }
}