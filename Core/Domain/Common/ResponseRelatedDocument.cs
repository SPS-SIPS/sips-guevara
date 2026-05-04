namespace SPSS.Domain.Common;

public sealed class ResponseRelatedDocument
{
    public string? From { get; set; }
    public string? To { get; set; }
    public string? BizMsgIdr { get; set; }
    public string? MsgDefIdr { get; set; }
    public DateTimeOffset? CreDt { get; set; }
}