public class CommonPopupParam
{
    public string Title { get; }
    public string Content { get; }
    public bool HasTwoButtons { get; }
    public string FirstButtonText { get; }
    public string SecondButtonText { get; }

    public CommonPopupParam(string title, string content, bool hasTwoButtons = false,
        string firstButtonText = "OK", string secondButtonText = "Cancel")
    {
        Title = title;
        Content = content;
        HasTwoButtons = hasTwoButtons;
        FirstButtonText = firstButtonText;
        SecondButtonText = secondButtonText;
    }
}
