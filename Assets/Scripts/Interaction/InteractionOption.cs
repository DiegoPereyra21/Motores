public class InteractionOption
{
    public InteractionActionData Data { get; }
    public bool IsAvailable { get; }
    public string UnavailableReason { get; }
    public string ResultMessage { get; }

    public InteractionOption(InteractionActionData data, bool isAvailable = true, string unavailableReason = "", string resultMessage = "")
    {
        Data = data;
        IsAvailable = isAvailable;
        UnavailableReason = unavailableReason;
        ResultMessage = resultMessage;
    }
}