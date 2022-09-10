using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CIARE.Roslyn
{
    private string FileName{ get; set; }
    private string BinaryPath{ get; set; }

    private string CsProjTemplate= @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0-windows</TargetFramework>
	  <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='Debug|AnyCPU'"">
    <Optimize>True</Optimize>
  </PropertyGroup>
</Project>
";

    public class CsProjCompile
    {
        public CsProjCompile(string binaryName, string binaryPath)
        {
            FileName = binaryName;
            BinaryPath = binaryPath;
        }

        public void Build(RichTextBox logOutput)
        {

        }
    }
}
