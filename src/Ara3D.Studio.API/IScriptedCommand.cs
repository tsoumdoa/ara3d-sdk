namespace Ara3D.Studio.API;

public interface IScriptedCommand
{
    string Name { get; }
    void Execute(IHostApplication app);
    bool CanExecute(IHostApplication app);
}