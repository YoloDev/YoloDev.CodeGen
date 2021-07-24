using FluentAssertions;
using Xunit;

namespace YoloDev.CodeGen.Tests
{
    public class CodeBuilderTests
    {
        [Fact]
        public void Test()
        {
            var builder = new CodeBuilder();
            builder.AppendFormat("foo{0}bar\n{1}baz", "\n", "oy");

            using (builder.Indent())
            {
                builder.AppendFormat("foo{0}bar\n{1}baz", "\n", "oy");
            }

            builder.AppendFormat("foo{0}bar\n{1}baz", "\n", "oy");

            var result = builder.ToString();
            result.Should().Be("foo\nbar\noybazfoo\n    bar\n    oybazfoo\nbar\noybaz");
        }

        [Fact]
        public void Test2()
        {
            var builder = new CodeBuilder();
            using (builder.Block("namespace Foo"))
            {
                using (builder.Block("class Bar"))
                {
                    builder.AppendLine("private readonly int _value;");
                }
            }

            var result = builder.ToString();
            result.Should().Be("namespace Foo\n{\n    class Bar\n    {\n        private readonly int _value;\n    }\n}\n");
        }

        [Fact]
        public void Test3()
        {
            var builder = new CodeBuilder(2);
            using (builder.Block("namespace Foo"))
            {
                using (builder.Block("class Bar"))
                {
                    builder.AppendLine("private readonly int _value;");
                }
            }

            var result = builder.ToString();
            result.Should().Be("namespace Foo\n{\n  class Bar\n  {\n    private readonly int _value;\n  }\n}\n");
        }
    }
}
