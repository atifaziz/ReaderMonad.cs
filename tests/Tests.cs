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
    using System.Collections.Generic;
    using System.Linq;
    using MoreLinq;
    using ReaderMonad;
    using ReaderMonad.Enumerators;
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

    public class EnumeratorReaderTests
    {
        static readonly IEnumerable<int> PositiveIntegers = MoreEnumerable.Generate(1, x => checked(x + 1));

        public class Read
        {
            [Fact]
            public void EnumeratesSubsequentElements()
            {
                var result =
                    PositiveIntegers
                        .Read(e =>
                            from x in e.Read()
                            from y in e.Read()
                            from z in e.Read()
                            select new { X = x, Y = y, Z = z });

                Assert.Equal(new { X = 1, Y = 2, Z = 3 }, result);
            }

            [Fact]
            public void ThrowsIfSequenceHasEnded()
            {
                Assert.Throws<InvalidOperationException>(() =>
                    PositiveIntegers
                        .Take(2)
                        .Read(e =>
                            from x in e.Read()
                            from y in e.Read()
                            from z in e.Read()
                            select 0));
            }
        }

        [Fact]
        public void TryRead()
        {
            var result =
                PositiveIntegers
                    .Take(2)
                    .Read(e =>
                        from x in e.TryRead()
                        from y in e.TryRead()
                        from z in e.TryRead()
                        select new { X = x, Y = y, Z = z });

            var expected = new
            {
                X = (true , 1),
                Y = (true , 2),
                Z = (false, 0),
            };

            Assert.Equal(expected, result);
        }

        [Fact]
        public void ReadOrDefault()
        {
            var result =
                PositiveIntegers
                    .Take(2)
                    .Read(e =>
                        from x in e.ReadOrDefault()
                        from y in e.ReadOrDefault()
                        from z in e.ReadOrDefault()
                        select new { X = x, Y = y, Z = z });

            Assert.Equal(new { X = 1, Y = 2, Z = 0 }, result);
        }

        [Fact]
        public void ReadOr()
        {
            var result =
                PositiveIntegers
                    .Take(2)
                    .Read(e =>
                        from x in e.ReadOr(-1)
                        from y in e.ReadOr(-2)
                        from z in e.ReadOr(-3)
                        select new { X = x, Y = y, Z = z });

            Assert.Equal(new { X = 1, Y = 2, Z = -3 }, result);
        }

        public class Skip
        {
            [Fact]
            public void SkipsCountOfElements()
            {
                var result =
                    PositiveIntegers
                        .Take(10)
                        .Read(e =>
                            from s1 in e.Skip(1)
                            from x  in e.Read()
                            from s2 in e.Skip(0)
                            from y  in e.Read()
                            from s3 in e.Skip(3)
                            from z  in e.Read()
                            from s4 in e.Skip(100)
                            select new
                            {
                                X = x, Y = y, Z = z,
                                Skip1 = s1, Skip2 = s2, Skip3 = s3, Skip4 = s4,
                            });

                var expected = new
                {
                    X = 2, Y = 3, Z = 7,
                    Skip1 = 1, Skip2 = 0, Skip3 = 3, Skip4 = 3
                };

                Assert.Equal(expected, result);
            }

            [Fact]
            public void WithNegativeCountSkipsNothing()
            {
                var result =
                    PositiveIntegers
                        .Take(10)
                        .Read(e =>
                            from x in e.Read()
                            from s in e.Skip(-1)
                            from y in e.Read()
                            select new { X = x, Y = y, Skips = s });

                Assert.Equal(new { X = 1, Y = 2, Skips = 0 }, result);
            }
        }

        public class SkipWhile
        {
            [Fact]
            public void SkipsWhileConditionIsBeingMet()
            {
                var result =
                    PositiveIntegers
                        .Read(e =>
                            from x in e.Read()
                            from c in e.SkipWhile(n => n < 5)
                            from y in e.Read()
                            select new { X = x, Y = y, SkipCount = c });

                Assert.Equal(new { X = 1, Y = 5, SkipCount = 3 }, result);
            }

            [Fact]
            public void SkipsAllWhenAllMatchCondition()
            {
                var result =
                    PositiveIntegers
                        .Take(100)
                        .Read(e =>
                            from c in e.SkipWhile(n => n != 0)
                            from x in e.TryRead()
                            select new { X = x, SkipCount = c });

                Assert.Equal(new { X = (false, 0), SkipCount = 100 }, result);
            }
        }

        [Fact]
        public void ReadAll()
        {
            var result =
                PositiveIntegers
                    .Take(5)
                    .Read(e =>
                        from x in e.Read()
                        from y in e.Read()
                        from z in e.Read()
                        from t in e.ReadAll()
                        select new
                        {
                            X = x,
                            Y = y,
                            Z = z,
                            Tail = (t[0], t[1])
                        });

            Assert.Equal(new { X = 1, Y = 2, Z = 3, Tail = (4, 5) }, result);
        }

        [Fact]
        public void ReadWhile()
        {
            var result =
                PositiveIntegers
                    .Read(e =>
                        from x in e.Read()
                        from y in e.Read()
                        from m in e.ReadWhile(n => n <= 5)
                        from z in e.Read()
                        select new
                        {
                            X = x,
                            Y = y,
                            Z = z,
                            Matches = (m[0], m[1], m[2]),
                        });

            Assert.Equal(new { X = 1, Y = 2, Z = 6, Matches = (3, 4, 5) }, result);
        }
    }
}
