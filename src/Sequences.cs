#region Copyright 2018 Atif Aziz. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

namespace ReaderMonad.Enumerators
{
    using System;
    using System.Collections.Generic;
    using Linq;
    using static Reader;

    public static class EnumeratorReader
    {
        public static TResult Read<T, TResult>(
            this IEnumerable<T> source,
            Func<IReader<IEnumerator<T>, IEnumerator<T>>, IReader<IEnumerator<T>, TResult>> reader)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            using (var e = source.GetEnumerator())
                return reader(Env<IEnumerator<T>>()).Read(e);
        }

        public static IReader<IEnumerator<T>, T> Read<T>(this IReader<IEnumerator<T>, IEnumerator<T>> reader) =>
            from e in reader
            select e.MoveNext()
                 ? e.Current
                 : throw new InvalidOperationException();

        public static IReader<IEnumerator<T>, (bool HasValue, T Value)> TryRead<T>(this IReader<IEnumerator<T>, IEnumerator<T>> reader) =>
            from e in reader
            select e.MoveNext()
                 ? (true, e.Current)
                 : default;

        public static IReader<IEnumerator<T>, T> ReadOrDefault<T>(this IReader<IEnumerator<T>, IEnumerator<T>> reader) =>
            reader.ReadOr(default);

        public static IReader<IEnumerator<T>, T> ReadOr<T>(this IReader<IEnumerator<T>, IEnumerator<T>> reader, T defaultValue) =>
            from e in reader.TryRead()
            select e.HasValue ? e.Value : defaultValue;

        public static IReader<IEnumerator<T>, int> Skip<T>(this IReader<IEnumerator<T>, IEnumerator<T>> reader, int count) =>
            reader.Map(e =>
            {
                var read = 0;
                for (; count > 0 && e.MoveNext(); count--)
                    read++;
                return read;
            });

        public static IReader<IEnumerator<T>, T> ReadWhen<T>(this IReader<IEnumerator<T>, IEnumerator<T>> reader, Func<T, bool> predicate) =>
            reader.ReadWhen((e, _) => predicate(e));

        public static IReader<IEnumerator<T>, T> ReadWhen<T>(this IReader<IEnumerator<T>, IEnumerator<T>> reader, Func<T, int, bool> predicate) =>
            reader.Select(e =>
            {
                bool moved;
                for (var i = 0; (moved = e.MoveNext()) && !predicate(e.Current, i);)
                    i++;
                return moved ? e.Current : throw new InvalidOperationException();
            });

        public static IReader<IEnumerator<T>, List<T>> ReadAll<T>(this IReader<IEnumerator<T>, IEnumerator<T>> reader) =>
            reader.Select(e =>
            {
                var list = new List<T>();
                while (e.MoveNext())
                    list.Add(e.Current);
                return list;
            });
    }
}
