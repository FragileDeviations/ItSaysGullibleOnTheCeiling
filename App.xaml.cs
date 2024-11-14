using System.Reflection;

namespace Gullible;

public partial class App
{
    internal static Assembly ProgramAsm { get; private set; }
    
    public App()
    {
        ProgramAsm = Assembly.GetExecutingAssembly();
    }
}