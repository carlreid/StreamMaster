namespace StreamMaster.Domain.Models;

public class SMChannelStreamLink
{
    public int SMChannelId { get; set; }

    public virtual int SMStreamM3UFileId { get; set; }

    public SMChannel SMChannel { get; set; }

    public string SMStreamId { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public SMStream SMStream { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public int Rank { get; set; }
}