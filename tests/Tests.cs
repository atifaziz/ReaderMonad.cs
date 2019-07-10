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

namespace Tests
{
    using System;
    using ReaderMonad;
    using ReaderMonad.Linq;
    using Xunit;

    public sealed class ReaderTests
    {
        [Fact]
        public void ReturnReturnsValue()
        {
            Assert.Equal(42, Reader.Return(42).Read());
        }

        [Fact]
        public void FunctionInvokesFunction()
        {
            var read = false;
            var reader = Reader.Function((object e) =>
            {
                read = true;
                return e;
            });
            var env = new object();
            var result = reader.Read(env);
            Assert.True(read);
            Assert.Same(env, result);
        }

        [Fact]
        public void FunctionWithNullFunctionThrows()
        {
            var e = Assert.Throws<ArgumentNullException>(() =>
                Reader.Function<object, object>(null));
            Assert.Equal("reader", e.ParamName);
        }

        [Theory]
        [InlineData(123)]
        [InlineData(456)]
        [InlineData(786)]
        public void Bind(int n)
        {
            var result =
                Reader.Return(n)
                      .Bind(x => Reader.Return(new { X = x, Y = x * 2 }))
                      .Read();

            Assert.Equal(n, result.X);
            Assert.Equal(n * 2, result.Y);
        }

        [Theory]
        [InlineData("one", 3)]
        [InlineData("three", 5)]
        [InlineData("five", 4)]
        [InlineData("eleven", 6)]
        public void Map(string str, int length)
        {
            var result =
                Reader.Return(str)
                      .Map(s => new { Str = s, s.Length })
                      .Read();

            Assert.Equal(str, result.Str);
            Assert.Equal(length, result.Length);
        }

        [Theory]
        [InlineData("one", 3)]
        [InlineData("three", 5)]
        [InlineData("five", 4)]
        [InlineData("eleven", 6)]
        public void Select(string str, int length)
        {
            var reader =
                from s in Reader.Return(str)
                select new { Str = s, s.Length };

            var result = reader.Read();

            Assert.Equal(str, result.Str);
            Assert.Equal(length, result.Length);
        }

        public class SelectMany
        {
            [Theory]
            [InlineData("one", 3)]
            [InlineData("three", 5)]
            [InlineData("five", 4)]
            [InlineData("eleven", 6)]
            public void UnitUnit(string str, int length)
            {
                var reader =
                    from s in Reader.Return(str)
                    from l in Reader.Return(s.Length)
                    select new { Str = s, Length = l };

                var result = reader.Read();

                Assert.Equal(str, result.Str);
                Assert.Equal(length, result.Length);
            }

            [Theory]
            [InlineData("one", 3)]
            [InlineData("three", 5)]
            [InlineData("five", 4)]
            [InlineData("eleven", 6)]
            public void EnvUnit(string str, int length)
            {
                var reader =
                    from s in Reader.Return<object, string>(str)
                    from l in Reader.Return(s.Length)
                    from e in Reader.Env<object>()
                    select new { Str = s, Length = l, Env = e };

                var env = new object();
                var result = reader.Read(env);

                Assert.Equal(str, result.Str);
                Assert.Equal(length, result.Length);
                Assert.Same(env, result.Env);
            }

            [Theory]
            [InlineData("one", 3)]
            [InlineData("three", 5)]
            [InlineData("five", 4)]
            [InlineData("eleven", 6)]
            public void UnitEnv(string str, int length)
            {
                var reader =
                    from s in Reader.Return(str)
                    from l in Reader.Return<object, int>(s.Length)
                    from e in Reader.Env<object>()
                    select new { Str = s, Length = l, Env = e };

                var env = new object();
                var result = reader.Read(env);

                Assert.Equal(str, result.Str);
                Assert.Equal(length, result.Length);
                Assert.Equal(env, result.Env);
            }

            [Theory]
            [InlineData("one", 3)]
            [InlineData("three", 5)]
            [InlineData("five", 4)]
            [InlineData("eleven", 6)]
            public void Env(string str, int length)
            {
                var reader =
                    from s in Reader.Return<object, string>(str)
                    from l in Reader.Return<object, int>(s.Length)
                    from e in Reader.Env<object>()
                    select new { Str = s, Length = l, Env = e };

                var env = new object();
                var result = reader.Read(env);

                Assert.Equal(str, result.Str);
                Assert.Equal(length, result.Length);
                Assert.Equal(env, result.Env);
            }
        }

        [Fact]
        public void EnvReturnsEnvironment()
        {
            var env = new object();
            var result = Reader.Env<object>().Read(env);
            Assert.Same(env, result);
        }
    }
}
