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
    using Unit = System.ValueTuple;

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

    public static class Reader
    {
        // Factory

        public static IReader<TEnv, T> Function<TEnv, T>(Func<TEnv, T> reader) =>
            new Reader<TEnv, T>(reader);

        // Functor

        public static IReader<TEnv, TResult>
            Map<TEnv, T, TResult>(this IReader<TEnv, T> reader, Func<T, TResult> mapper)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            return Function((TEnv env) => mapper(reader.Read(env)));
        }

        // Monad

        public static IReader<Unit, T> Return<T>(T value) =>
            Function((Unit _) => value);

        public static IReader<TEnv, T> Return<TEnv, T>(T value) =>
            Function((TEnv _) => value);

        public static IReader<TEnv, TResult>
            Bind<TEnv, T, TResult>(this IReader<TEnv, T> reader, Func<T, IReader<TEnv, TResult>> function)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (function == null) throw new ArgumentNullException(nameof(function));

            return Function((TEnv env) => function(reader.Read(env)).Read(env));
        }

        // Others

        public static IReader<T, T> Env<T>() =>
            EnvReader<T>.Instance;

        static class EnvReader<T>
        {
            public static readonly IReader<T, T> Instance = Function((T env) => env);
        }

        public static T Read<T>(this IReader<Unit, T> reader) =>
            reader != null ? reader.Read(default)
                           : throw new ArgumentNullException(nameof(reader));
    }
}
