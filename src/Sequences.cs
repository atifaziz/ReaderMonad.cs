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

    public interface IEnumeratorReader<T> : IDisposable
    {
        bool TryPeek(out T value);
        void MoveNext();
    }

    sealed class EnumeratorReader<T> : IEnumeratorReader<T>
    {
        bool _disposed;
        IEnumerator<T> _enumerator;
        (bool, T) _current;

        public EnumeratorReader(IEnumerator<T> enumerator) =>
            _enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));

        void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EnumeratorReader<T>));
        }

        public bool TryPeek(out T value)
        {
            ThrowIfDisposed();

            if (_current is (true, var current))
            {
                value = current;
                return true;
            }

            if (_enumerator is IEnumerator<T> e)
            {
                if (e.MoveNext())
                {
                    _current = (true, value = e.Current);
                    return true;
                }
                else
                {
                    e.Dispose();
                    _enumerator = default;
                }
            }

            value = default;
            _current = default;
            return false;
        }

        public void MoveNext()
        {
            ThrowIfDisposed();
            _current = default;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            var e = _enumerator;

            _disposed = true;
            _enumerator = default;
            _current = default;

            e.Dispose();
        }
    }

    public static class EnumerableReader
    {
        public static TResult Read<T, TResult>(
            this IEnumerable<T> source,
            Func<EnumeratorOperations<T>, IReader<IEnumeratorReader<T>, TResult>> reader)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            using (var e = source.GetEnumerator())
                return reader(EnumeratorOperations<T>.Instance).Read(new EnumeratorReader<T>(e));
        }
    }

    public sealed class EnumeratorOperations<T>
    {
        public static readonly EnumeratorOperations<T> Instance = new EnumeratorOperations<T>();

        static class Free
        {
            public static readonly IReader<IEnumeratorReader<T>, (bool HasValue, T Value)> TryRead =
                Function((IEnumeratorReader<T> e) =>
                {
                    if (!e.TryPeek(out var item))
                        return default;
                    e.MoveNext();
                    return (true, item);
                });

            public static readonly IReader<IEnumeratorReader<T>, T> Read =
                from e in TryRead
                select e.HasValue ? e.Value : throw new InvalidOperationException();

            public static readonly IReader<IEnumeratorReader<T>, T> ReadOrDefault =
                from e in TryRead
                select e.HasValue ? e.Value : default;

            public static readonly IReader<IEnumeratorReader<T>, List<T>> ReadAll =
                Instance.Aggregate(new List<T>(),
                                   (list, item) => { list.Add(item); return list; },
                                   list => list);
        }

        public IReader<IEnumeratorReader<T>, T> Read() => Free.Read;

        public IReader<IEnumeratorReader<T>, (bool HasValue, T Value)> TryRead() => Free.TryRead;

        public IReader<IEnumeratorReader<T>, T> ReadOrDefault() => Free.ReadOrDefault;

        public IReader<IEnumeratorReader<T>, T> ReadOr(T defaultValue) =>
            from item in TryRead()
            select item.HasValue ? item.Value : defaultValue;

        static readonly IReader<IEnumeratorReader<T>, int>[] SkipCountCache = new IReader<IEnumeratorReader<T>, int>[10];

        public IReader<IEnumeratorReader<T>, int> Skip(int count)
        {
            var i = Math.Max(0, count);
            return i < SkipCountCache.Length
                 ? SkipCountCache[i] ?? (SkipCountCache[i] = SkipImpl(count))
                 : SkipImpl(count);
        }

        static IReader<IEnumeratorReader<T>, int> SkipImpl(int count) =>
            AggregateWhile((Remaining: count, Skipped: 0),
                           (cnt, _) => cnt.Remaining > 0 ? (true, (cnt.Remaining - 1, cnt.Skipped + 1))
                                                         : default,
                           cnt => cnt.Skipped);

        public IReader<IEnumeratorReader<T>, int> SkipWhile(Func<T, bool> predicate) =>
            SkipWhile((e, _) => predicate(e));

        public IReader<IEnumeratorReader<T>, int> SkipWhile(Func<T, int, bool> predicate) =>
            AggregateWhile(0, (count, item) => predicate(item, count) ? (true, count + 1) : default,
                           count => count);

        public IReader<IEnumeratorReader<T>, List<T>> ReadAll() => Free.ReadAll;

        public IReader<IEnumeratorReader<T>, List<T>> ReadWhile(Func<T, bool> predicate) =>
            ReadWhile((e, _) => predicate(e));

        public IReader<IEnumeratorReader<T>, List<T>> ReadWhile(Func<T, int, bool> predicate) =>
            AggregateWhile(
                new List<T>(),
                (list, item) =>
                {
                    if (!predicate(item, list.Count))
                        return default;
                    list.Add(item);
                    return (true, list);
                },
                list => list);

        public IReader<IEnumeratorReader<T>, TResult>
            Aggregate<TState, TResult>(TState seed,
                                       Func<TState, T, TState> accumulator,
                                       Func<TState, TResult> resultSelector) =>
            AggregateWhile(seed, (state, item) => (true, accumulator(state, item)), resultSelector);

        static IReader<IEnumeratorReader<T>, TResult>
            AggregateWhile<TState, TResult>(TState seed,
                                            Func<TState, T, (bool, TState)> accumulator,
                                            Func<TState, TResult> resultSelector) =>
            Function((IEnumeratorReader<T> e) =>
            {
                var state = seed;
                while (e.TryPeek(out var item))
                {
                    var (cont, ns) = accumulator(state, item);
                    if (!cont)
                        break;
                    state = ns;
                    e.MoveNext();
                }
                return resultSelector(state);
            });
    }
}
