public class InteractionOption
{
    public InteractionActionData Data { get; }
    public bool IsAvailable { get; }
    public string UnavailableReason { get; }
    public string ResultMessage { get; }

    public string DisplayName { get; }
    public OpeningPlacementError FeedbackType { get; }

    public InteractionOption(InteractionActionData data, bool isAvailable = true, string unavailableReason = "", string resultMessage = "", OpeningPlacementError feedbackType = OpeningPlacementError.None, string displayName = null)
    {
        Data = data;
        IsAvailable = isAvailable;
        UnavailableReason = unavailableReason;
        ResultMessage = resultMessage;
        FeedbackType = feedbackType;

        DisplayName = string.IsNullOrWhiteSpace(displayName) ? data.DisplayName : displayName;
    }
}