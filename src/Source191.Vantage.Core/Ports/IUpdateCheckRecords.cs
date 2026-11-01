namespace Source191.Vantage.Ports;

/// <summary>The last update check, so Vantage asks at most once a day; only a check that was made is written.</summary>
public interface IUpdateCheckRecords
{
    Task<UpdateCheckRecord> ReadUpdateCheckAsync();

    Task WriteUpdateCheckAsync(UpdateCheckRecord.Checked record);
}