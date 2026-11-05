namespace Source191.Torni.Ports;

/// <summary>The last update check, so the console asks at most once a day; only a check that was made is written.</summary>
public interface IUpdateCheckRecords
{
    Task<UpdateCheckRecord> ReadUpdateCheckAsync();

    Task WriteUpdateCheckAsync(UpdateCheckRecord.Checked record);
}