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
        bool TrySeek(out T value);
        void MoveNext();
    }

    sealed class EnumeratorReader<T> : IEnumeratorReader<T>
    {
        bool _disposed;
        IEnumerator<T> _enumerator;
        bool _hasCurrent;
        T _current;

        public EnumeratorReader(IEnumerator<T> enumerator) =>
            _enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));

        void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(EnumeratorReader<T>));
        }

        public bool TrySeek(out T value)
        {
            ThrowIfDisposed();

            if (_hasCurrent)
            {
                value = _current;
                return true;
            }

            if (_enumerator is IEnumerator<T> e)
            {
                if (e.MoveNext())
                {
                    _hasCurrent = true;
                    value = _current = e.Current;
                    return true;
                }
                else
                {
                    e.Dispose();
                    _enumerator = default;
                    _hasCurrent = false;
                }
            }

            value = default;
            _current = default;
            return false;
        }

        public void MoveNext()
        {
            ThrowIfDisposed();
            _hasCurrent = false;
            _current = default;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            var e = _enumerator;

            _disposed = true;
            _enumerator = default;
            _hasCurrent = false;
            _current = default;

            e.Dispose();
        }
    }

    public static class EnumeratorReader
    {
        public static TResult Read<T, TResult>(
            this IEnumerable<T> source,
            Func<IReader<IEnumeratorReader<T>, IEnumeratorReader<T>>, IReader<IEnumeratorReader<T>, TResult>> reader)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            using (var e = source.GetEnumerator())
                return reader(Env<IEnumeratorReader<T>>()).Read(new EnumeratorReader<T>(e));
        }

        public static IReader<IEnumeratorReader<T>, T> Read<T>(this IReader<IEnumeratorReader<T>, IEnumeratorReader<T>> reader) =>
            from e in reader.TryRead()
            select e.HasValue ? e.Value : throw new InvalidOperationException();

        public static IReader<IEnumeratorReader<T>, (bool HasValue, T Value)> TryRead<T>(this IReader<IEnumeratorReader<T>, IEnumeratorReader<T>> reader) =>
            reader.Select(e =>
            {
                if (!e.TrySeek(out var item))
                    return default;
                e.MoveNext();
                return (true, item);
            });

        public static IReader<IEnumeratorReader<T>, T> ReadOrDefault<T>(this IReader<IEnumeratorReader<T>, IEnumeratorReader<T>> reader) =>
            reader.ReadOr(default);

        public static IReader<IEnumeratorReader<T>, T> ReadOr<T>(this IReader<IEnumeratorReader<T>, IEnumeratorReader<T>> reader, T defaultValue) =>
            from item in reader.TryRead()
            select item.HasValue ? item.Value : defaultValue;

        public static IReader<IEnumeratorReader<T>, int> Skip<T>(this IReader<IEnumeratorReader<T>, IEnumeratorReader<T>> reader, int count) =>
            reader.Map(e =>
            {
                var read = 0;
                for (; count > 0 && e.TrySeek(out _); count--)
                {
                    e.MoveNext();
                    read++;
                }
                return read;
            });

        public static IReader<IEnumeratorReader<T>, int> SkipWhile<T>(this IReader<IEnumeratorReader<T>, IEnumeratorReader<T>> reader, Func<T, bool> predicate) =>
            reader.SkipWhile((e, _) => predicate(e));

        public static IReader<IEnumeratorReader<T>, int> SkipWhile<T>(this IReader<IEnumeratorReader<T>, IEnumeratorReader<T>> reader, Func<T, int, bool> predicate) =>
            reader.Select(e =>
            {
                var i = 0;
                for (; e.TrySeek(out var item); i++)
                {
                    if (!predicate(item, i))
                        break;
                    e.MoveNext();
                }
                return i;
            });

        public static IReader<IEnumeratorReader<T>, List<T>> ReadAll<T>(this IReader<IEnumeratorReader<T>, IEnumeratorReader<T>> reader) =>
            reader.Select(e =>
            {
                var list = new List<T>();
                while (e.TrySeek(out var item))
                {
                    e.MoveNext();
                    list.Add(item);
                }
                return list;
            });

        public static IReader<IEnumeratorReader<T>, List<T>>
            ReadWhile<T>(this IReader<IEnumeratorReader<T>, IEnumeratorReader<T>> reader,
                         Func<T, bool> predicate) =>
            reader.ReadWhile((e, _) => predicate(e));

        public static IReader<IEnumeratorReader<T>, List<T>>
            ReadWhile<T>(this IReader<IEnumeratorReader<T>, IEnumeratorReader<T>> reader,
                         Func<T, int, bool> predicate) =>
            reader.Select(e =>
            {
                var list = new List<T>();
                for (var i = 0; e.TrySeek(out var item); i++)
                {
                    if (!predicate(item, i))
                        break;
                    list.Add(item);
                    e.MoveNext();
                }
                return list;
            });
    }
}
