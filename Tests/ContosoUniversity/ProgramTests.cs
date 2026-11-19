using Xunit;

namespace ContosoUniversity
{
    public class ProgramTests
    {
        [Fact]
        public void AssemblyContainsAllControllers()
        {
            var asm = typeof(ContosoUniversity.Controllers.HomeController).Assembly;
            foreach (var name in new[] { "HomeController", "StudentsController", "CourseController", "InstructorController" })
            {
                Assert.NotNull(asm.GetType("ContosoUniversity.Controllers." + name));
            }
        }
    }
}
