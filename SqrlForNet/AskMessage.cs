using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace SqrlForNet
{
    public class AskMessage
    {
        public string Message { get; set; } = string.Empty;
        public AskMessageButton? Button1 { get; set; }
        public AskMessageButton? Button2 { get; set; }

        public class AskMessageButton
        {
            public string Text { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;

            internal string ToAskMessage()
            {
                var buttonValue = Text;
                if (!string.IsNullOrEmpty(Url))
                {
                    buttonValue += ";" + Url;
                }
                return Base64UrlTextEncoder.Encode(Encoding.ASCII.GetBytes(buttonValue));
            }

        }

        internal string ToAskMessage()
        {
            var buttonValue = string.Empty;
            if (Button1 is not null)
            {
                buttonValue += "~" + Button1.ToAskMessage();
            }

            if (Button2 is not null)
            {
                buttonValue += "~" + Button2.ToAskMessage();
            }
            return Base64UrlTextEncoder.Encode(Encoding.ASCII.GetBytes(Message)) + buttonValue;
        }

    }
}
