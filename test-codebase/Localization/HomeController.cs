using Microsoft.AspNetCore.Mvc.Localization;

namespace TestCodebase.Localization
{
    /// <summary>
    /// LOC010: Display string not localized
    /// LOC011: String interpolation in localizable context
    /// LOC013: Dynamic resource key
    /// LOC015: Punctuation outside string
    /// </summary>
    public class HomeController
    {
        private readonly IStringLocalizer<HomeController> _localizer;
        private readonly IHtmlLocalizer<HomeController> _htmlLocalizer;

        public HomeController(IStringLocalizer<HomeController> localizer, IHtmlLocalizer<HomeController> htmlLocalizer)
        {
            _localizer = localizer;
            _htmlLocalizer = htmlLocalizer;
        }

        // LOC011: String interpolation in localizer indexer
        public string GetGreeting(string name)
        {
            return _localizer[$"Hello {name}"];
        }

        // LOC011: String interpolation in HTML localizer
        public string GetWelcomeMessage(string userName)
        {
            return _htmlLocalizer[$"Welcome back, {userName}!"];
        }

        // LOC013: Dynamic resource key via concatenation
        public string GetErrorMessage(int errorCode)
        {
            return _localizer["Error_" + errorCode];
        }

        // LOC013: Dynamic resource key via interpolation
        public string GetStatusMessage(string status)
        {
            return _localizer[$"Status_{status}"];
        }

        // LOC013: Dynamic resource key via variable
        public string GetMessage(string key)
        {
            string resourceKey = "Message_" + key;
            return _localizer[resourceKey];
        }

        // LOC015: Punctuation outside translatable string
        public string GetLabel(string label)
        {
            return label + ":";
        }

        // LOC015: Punctuation as separate string
        public string GetDescription(string text)
        {
            return text + ".";
        }

        // ACCEPTABLE: Format string (not interpolation)
        public string GetFormattedGreeting(string name)
        {
            return _localizer["Hello {0}", name];
        }

        // ACCEPTABLE: Literal resource key
        public string Get固定的Key()
        {
            return _localizer["WelcomeMessage"];
        }
    }

    // LOC010: Display string not localized (via UI property)
    public class Label
    {
        public string Text { get; set; }
    }

    public class Button
    {
        public string Content { get; set; }
    }

    public class LocalizationDemo
    {
        // LOC010: Display string assigned directly to UI property
        public void SetLabel(Label label)
        {
            label.Text = "Click here to continue";
        }

        // LOC010: Display string assigned to button
        public void SetButton(Button button)
        {
            button.Content = "Submit Order";
        }

        // ACCEPTABLE: Using localizer
        public void SetLocalizedLabel(Label label, IStringLocalizer<LocalizationDemo> localizer)
        {
            label.Text = localizer["ClickHere"];
        }
    }
}
