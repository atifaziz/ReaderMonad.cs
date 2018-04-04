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

namespace ReaderMonad.Linq
{
    using System;

    public static class ReaderExtensions
    {
        public static IReader<TEnv, TResult>
            Select<TEnv, T, TResult>(this IReader<TEnv, T> reader, Func<T, TResult> selector) =>
            reader.Map(selector);

        public static IReader<TEnv, TResult>
            SelectMany<TEnv, T, TSecond, TResult>(
                this IReader<TEnv, T> reader,
                Func<T, IReader<TEnv, TSecond>> secondSelector,
                Func<T, TSecond, TResult> resultSelector) =>
            reader.Bind(x => secondSelector(x).Map(y => resultSelector(x, y)));
    }
}
