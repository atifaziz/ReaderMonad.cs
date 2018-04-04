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

namespace ReaderMonad
{
    using System;

    public interface IReader<in E, out T>
    {
        T Read(E env);
    }

    sealed class Reader<E, T> : IReader<E, T>
    {
        readonly Func<E, T> _reader;
        public Reader(Func<E, T> reader) =>
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        public T Read(E env) => _reader(env);
    }

    public static class Reader<E>
    {
        public static IReader<E, T> Return<T>(T value) =>
            Function(_ => value);

        public static IReader<E, T> Function<T>(Func<E, T> reader) =>
            new Reader<E, T>(reader);
    }

    public static class ReaderExtensions
    {
        public static IReader<E, U>
            Bind<E, T, U>(this IReader<E, T> reader, Func<T, IReader<E, U>> f) =>
            Reader<E>.Function(e => f(reader.Read(e)).Read(e));

        public static IReader<E, U>
            Map<E, T, U>(this IReader<E, T> reader, Func<T, U> mapper) =>
            Reader<E>.Function(e => mapper(reader.Read(e)));
    }
}

namespace ReaderMonad.Linq
{
    using System;

    public static class ReaderExtensions
    {
        public static IReader<E, U>
            Select<E, T, U>(this IReader<E, T> reader, Func<T, U> selector) =>
            reader.Map(selector);

        public static IReader<E, V>
            SelectMany<E, T, U, V>(
                this IReader<E, T> reader,
                Func<T, IReader<E, U>> selector,
                Func<T, U, V> resultSelector) =>
            reader.Bind(x => selector(x).Map(y => resultSelector(x, y)));
    }
}
