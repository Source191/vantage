namespace Source191.Torni.Ports;

/// <summary>Where the diagnostic log's lines go besides memory: the <c>TORNI_LOG</c> file. A sink never throws.</summary>
public interface IDiagnosticSink
{
    void Write(string line);
}