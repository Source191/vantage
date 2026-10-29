namespace Source191.Vantage.Ports;

/// <summary>Where the diagnostic log's lines go besides memory: the <c>VANTAGE_LOG</c> file. A sink never throws.</summary>
public interface IDiagnosticSink
{
    void Write(string line);
}
