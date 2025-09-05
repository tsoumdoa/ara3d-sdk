/// <summary>
/// Shows a message box with the text: "Hello world!"
/// </summary>
public class HelloWorld : IScriptedCommand
{
    public string Name 
        => "Hello World!";

    public void Execute()
        => MessageBox.Show(Name);
}