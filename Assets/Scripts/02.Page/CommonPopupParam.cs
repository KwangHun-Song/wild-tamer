public class CommonPopupParam
{
    public string Title { get; }
    public string Content { get; }
    public bool HasTwoButtons { get; }

    public CommonPopupParam(string title, string content, bool hasTwoButtons = false)
    {
        Title = title;
        Content = content;
        HasTwoButtons = hasTwoButtons;
    }
}
