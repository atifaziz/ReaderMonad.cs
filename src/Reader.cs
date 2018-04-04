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

    public interface IReader<in TEnv, out T>
    {
        T Read(TEnv env);
    }

    sealed class Reader<TEnv, T> : IReader<TEnv, T>
    {
        readonly Func<TEnv, T> _reader;

        public Reader(Func<TEnv, T> reader) =>
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));

        public T Read(TEnv env) => _reader(env);
    }

    public static class Reader<TEnv>
    {
        public static IReader<TEnv, T> Return<T>(T value) =>
            Function(_ => value);

        public static IReader<TEnv, T> Function<T>(Func<TEnv, T> reader) =>
            new Reader<TEnv, T>(reader);
    }

    public static class ReaderExtensions
    {
        public static IReader<TEnv, TResult>
            Bind<TEnv, T, TResult>(this IReader<TEnv, T> reader, Func<T, IReader<TEnv, TResult>> f) =>
            Reader<TEnv>.Function(e => f(reader.Read(e)).Read(e));

        public static IReader<TEnv, TResult>
            Map<TEnv, T, TResult>(this IReader<TEnv, T> reader, Func<T, TResult> mapper) =>
            Reader<TEnv>.Function(e => mapper(reader.Read(e)));
    }
}

