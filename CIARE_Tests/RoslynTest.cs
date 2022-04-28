using System;
using Xunit;
using CIARE;

namespace CIARE_Tests
{
    public class RoslynTest
    {
        [Fact]
        public void RoslynRun()
        {
            string code = @"using System;
// You can add more dependencies.

namespace Test_Code
{
  public class Test
  {	
     public void Main(string arg)
     {
   	Console.WriteLine(""Hello"");
     }
  }
}
";
          //Assert.Equal("Hello",CIARE.Roslyn.RoslynRun.CompileAndRun(code,"",""));

        }
    }
}
