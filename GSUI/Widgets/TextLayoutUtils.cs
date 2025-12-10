using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.UI
{
    public static class TextLayoutUtils
    {

        // found this code at https://github.com/mono/SkiaSharp/issues/692

        public static List<string> BreakLines(string text, SKPaint paint, float width)
        {
            List<string> lines = new List<string>();

            // TODO: this removes both spaces and newlines. 
            // We don't always want to remove these...it depends on how the returned strings will be used...
            // (maybe caller has to trim the result)
            const bool bTrimStrings = false;

            //if (text.EndsWith(' '))
            //    Debugger.Break();

            string remainingText = (bTrimStrings) ? text.Trim() : text;

            do {
                int idx = LineBreak(remainingText, paint, width);
                if (idx == 0) {
                    break;
                }

                string lastLine = remainingText.Substring(0, idx);
                lines.Add( (bTrimStrings) ? lastLine.Trim() : lastLine);

                remainingText = remainingText.Substring(idx);

            } while (!string.IsNullOrEmpty(remainingText));
            return lines;
        }

        public static int LineBreak(string text, SKPaint paint, float width)
        {
            int idx = 0, last = 0;
            int MaxCharsInWidth = (int)paint.BreakText(text, width);
            if (MaxCharsInWidth == text.Length)
                return MaxCharsInWidth;     // if all chars fit we do not have to break anything (except what about '\n' ?)

            while (idx < text.Length) {
                int next = text.IndexOfAny(new char[] { ' ', '\n' }, idx);
                if (next == -1) {
                    if (idx == 0) {
                        return MaxCharsInWidth;
                    } else {
                        // Ellipsize if it's the last line
                        if (MaxCharsInWidth == text.Length
                        // || text.IndexOfAny (new char [] { ' ', '\n' }, lengthBreak + 1) == -1
                        ) {
                            return MaxCharsInWidth;
                        }
                        // Split at the last word;
                        return last;
                    }
                }
                if (text[idx] == '\n') {
                    return idx;
                }
                if (next > MaxCharsInWidth) {
                    return idx;
                }
                last = next;
                idx = next + 1;
            }
            return last;
        }



    }
}
