public class InteractionOption
{
    public InteractionActionData Data { get; }
    public bool IsAvailable { get; }
    public string UnavailableReason { get; }
    public string ResultMessage { get; }
    public OpeningPlacementError FeedbackType { get; }

    public InteractionOption(InteractionActionData data, bool isAvailable = true, string unavailableReason = "", string resultMessage = "", OpeningPlacementError feedbackType = OpeningPlacementError.None)
    {
        Data = data;
        IsAvailable = isAvailable;
        UnavailableReason = unavailableReason;
        ResultMessage = resultMessage;
        FeedbackType = feedbackType;
    }
}