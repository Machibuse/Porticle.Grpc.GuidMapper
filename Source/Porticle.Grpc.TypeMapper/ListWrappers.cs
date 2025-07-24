namespace Porticle.Grpc.TypeMapper;

public static class ListWrappers
{
    public static string RepeatedFieldGuidWrapper =
        """
        class RepeatedFieldGuidWrapper : System.Collections.Generic.IList<System.Guid>
        {
            private readonly Google.Protobuf.Collections.RepeatedField<string> _internList;

            public RepeatedFieldGuidWrapper(Google.Protobuf.Collections.RepeatedField<string> internList)
            {
                this._internList = internList;
            }

            public System.Collections.Generic.IEnumerator<System.Guid> GetEnumerator()
            {
                using var enumerator = _internList.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    yield return System.Guid.Parse(enumerator.Current);
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(System.Guid item)
            {
                _internList.Add(item.ToString("D"));
            }

            public void Clear()
            {
                _internList.Clear();
            }

            public bool Contains(System.Guid item)
            {
                return _internList.Contains(item.ToString("D"));
            }

            public void CopyTo(System.Guid[] array, int arrayIndex)
            {
                System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(_internList, System.Guid.Parse)).CopyTo(array, arrayIndex);
            }

            public bool Remove(System.Guid item)
            {
                return _internList.Remove(item.ToString("D"));
            }

            public int Count => _internList.Count;
            
            public bool IsReadOnly => _internList.IsReadOnly;
            
            public int IndexOf(System.Guid item)
            {
                return _internList.IndexOf(item.ToString("D"));
            }

            public void Insert(int index, System.Guid item)
            {
                _internList.Insert(index, item.ToString("D"));
            }

            public void RemoveAt(int index)
            {
                _internList.RemoveAt(index);
            }

            public System.Guid this[int index]
            {
                get =>  System.Guid.Parse(_internList[index]);
                set => _internList[index] = value.ToString("D");
            }
        }
        """;
}