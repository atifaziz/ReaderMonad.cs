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

        public static IReader<IEnumerator<T>, (List<T> Matches, (bool HasValue, T Value) Mismatch)>
            ReadWhile<T>(this IReader<IEnumerator<T>, IEnumerator<T>> reader,
                         Func<T, bool> predicate) =>
            reader.ReadWhile((e, _) => predicate(e));

        public static IReader<IEnumerator<T>, (List<T> Matches, (bool HasValue, T Value) Mismatch)>
            ReadWhile<T>(this IReader<IEnumerator<T>, IEnumerator<T>> reader,
                         Func<T, int, bool> predicate) =>
            reader.Select(e =>
            {
                var list = new List<T>();
                bool moved;
                while ((moved = e.MoveNext()) && predicate(e.Current, 0))
                    list.Add(e.Current);
                return (list, moved ? (true, e.Current) : default);
            });
        /*
        public static Suspended<T>
            ReadWhile2<T>(this IReader<IEnumerator<T>, IEnumerator<T>> reader,
                         Func<T, bool> predicate) =>
            reader.ReadWhile2((e, _) => predicate(e));

        public static Suspended<T>
            ReadWhile2<T>(this IReader<IEnumerator<T>, IEnumerator<T>> reader,
                          Func<T, int, bool> predicate) =>
            new Suspended<T>(reader, predicate);

        public static IReader<IEnumerator<T>, List<T>>
            ReadWhile3<T>(this IReader<IEnumerator<T>, IEnumerator<T>> reader,
                         Func<T, bool> predicate) =>
            reader.ReadWhile3((e, _) => predicate(e));

        public static IReader<IEnumerator<T>, List<T>>
            ReadWhile3<T>(this IReader<IEnumerator<T>, IEnumerator<T>> reader,
                         Func<T, int, bool> predicate) =>
            reader.Select(e =>
            {
                var list = new List<T>();
                bool moved;
                while ((moved = e.MoveNext()) && predicate(e.Current, 0))
                    list.Add(e.Current);
                return f(list, Env<IEnumerator<T>>()).Read(moved ? _(e.Current) : e);
                IEnumerator<T> _(T head)
                {
                    yield return head;
                    while (e.MoveNext())
                        yield return e.Current;
                }
            });
            */
    }

    /*
    public sealed class Suspended<T>
    {
        readonly IReader<IEnumerator<T>, IEnumerator<T>> _reader;
        readonly Func<T, int, bool> _predicate;

        public Suspended(IReader<IEnumerator<T>, IEnumerator<T>> reader, Func<T, int, bool> predicate)
        {
            _reader = reader;
            _predicate = predicate;
        }

        public IReader<IEnumerator<T>, List<T>> Ignore() =>
            _reader.Select(e =>
            {
                var list = new List<T>();
                while (e.MoveNext() && _predicate(e.Current, 0))
                    list.Add(e.Current);
                return list;
            });

        public IReader<IEnumerator<T>, TResult> Then<TResult>(Func<List<T>, IReader<IEnumerator<T>, IEnumerator<T>>, IReader<IEnumerator<T>, TResult>> f) =>
            _reader.Select(e =>
            {
                var list = new List<T>();
                bool moved;
                while ((moved = e.MoveNext()) && _predicate(e.Current, 0))
                    list.Add(e.Current);
                return f(list, Env<IEnumerator<T>>()).Read(moved ? _(e.Current) : e);
                IEnumerator<T> _(T head)
                {
                    yield return head;
                    while (e.MoveNext())
                        yield return e.Current;
                }
            });
    }
    */
}
