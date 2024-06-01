using FluentAssertions;

using System.Threading.Tasks;

using Xunit;

namespace Rocksmith2014.XML.Tests
{
    public static class InstrumentalArrangementTests
    {
        [Fact]
        public static async Task CanRemoveDD()
        {
            var arr = InstrumentalArrangement.Load("instrumental.xml");

            await arr.RemoveDD();

            arr.Levels.Should().HaveCount(1);
        }
    }
}
