// jpnkn Fast payload handling, 2026-10-19. GPLv2.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace SpeechCastMe
{
    public sealed class JpnknFastTarget
    {
        public string Board { get; private set; }
        public string Thread { get; private set; }
        public string Topic { get { return "bbs/" + Board; } }
        public string BoardUrl { get { return "https://bbs.jpnkn.com/" + Board + "/"; } }
        public static bool TryParse(string input, bool boardOnly, out JpnknFastTarget target)
        {
            target = null;
            input = (input ?? "").Trim();
            if (boardOnly && Regex.IsMatch(input, @"^[A-Za-z0-9_\-]+$"))
            {
                target = new JpnknFastTarget { Board = input };
                return true;
            }
            Uri url;
            if (!Uri.TryCreate(input, UriKind.Absolute, out url) ||
                (url.Scheme != "https" && url.Scheme != "http") ||
                !string.Equals(url.Host, "bbs.jpnkn.com", StringComparison.OrdinalIgnoreCase) ||
                !url.IsDefaultPort || !string.IsNullOrEmpty(url.UserInfo)) return false;
            var thread = Regex.Match(url.AbsolutePath, @"^/test/read\.cgi/([A-Za-z0-9_\-]+)/(\d+)(?:/.*)?$");
            var board = Regex.Match(url.AbsolutePath, @"^/([A-Za-z0-9_\-]+)/?$");
            if (thread.Success)
            {
                target = new JpnknFastTarget { Board = thread.Groups[1].Value, Thread = boardOnly ? null : thread.Groups[2].Value };
                return true;
            }
            if (boardOnly && board.Success)
            {
                target = new JpnknFastTarget { Board = board.Groups[1].Value };
                return true;
            }
            return false;
        }
    }

    public sealed class JpnknFastMessage
    {
        public string Board { get; private set; }
        public string Thread { get; private set; }
        public int Number { get; private set; }
        public string Body { get; private set; }
        public string Key { get { return Board + "/" + Thread + "/" + Number; } }
        public static bool TryParse(string topic, string json, out JpnknFastMessage message)
        {
            message = null;
            if (string.IsNullOrEmpty(json) || json.Length > 1024 * 1024) return false;
            try
            {
                var data = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(json);
                if (data == null || !data.ContainsKey("bbsid") || !data.ContainsKey("threadkey") || !data.ContainsKey("no") || !data.ContainsKey("body")) return false;
                string board = data["bbsid"] as string;
                string thread = Convert.ToString(data["threadkey"], CultureInfo.InvariantCulture);
                string body = data["body"] as string;
                int number;
                if (board == null || !Regex.IsMatch(board, @"^[A-Za-z0-9_\-]+$") || topic != "bbs/" + board ||
                    !Regex.IsMatch(thread, @"^\d+$") || body == null || body.Split(new[] { "<>" }, StringSplitOptions.None).Length < 4 ||
                    !int.TryParse(Convert.ToString(data["no"], CultureInfo.InvariantCulture), out number) || number <= 0) return false;
                // DAT lines end with "<>title" (empty after res 1); accept a body
                // without the trailing field so Response.SetRawText sees 5 fields.
                if (body.Split(new[] { "<>" }, StringSplitOptions.None).Length == 4) body += "<>";
                message = new JpnknFastMessage { Board = board, Thread = thread, Number = number, Body = body };
                return true;
            }
            catch (ArgumentException) { return false; }
            catch (InvalidOperationException) { return false; }
            catch (FormatException) { return false; }
            catch (OverflowException) { return false; }
        }

        internal Response ToResponse(int ordinal, bool boardFeed, string title)
        {
            var response = new Response { Number = ordinal, SourceNumber = Number,
                // No "スレ <key>" label in the board feed: the long thread key is noise; the reply number is enough.
                SourceThreadUrl = "https://bbs.jpnkn.com/test/read.cgi/" + Board + "/" + Thread + "/" };
            if (!response.SetRawText(Body)) return null;
            response.ThreadTitle = title;
            return response;
        }
    }
}
