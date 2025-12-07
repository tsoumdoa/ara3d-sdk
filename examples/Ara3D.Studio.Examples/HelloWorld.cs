/// <summary>
/// Shows a message box with the text: "Hello world!"
/// </summary>
public class HelloWorld : SimpleCommand
{
    public override string Name 
        => "Hello World";

    public override void Execute()
        => MessageBox.Show(Name);
}