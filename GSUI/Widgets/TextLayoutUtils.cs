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
                // figure out break point in the string. 
                // this will break at a newline if it comes before width.
                // otherwise it will prefer to break at a space, unless it's
                // not possible, then it will break mid-word (currently no dashes are shown)
                int idx = LineBreak(remainingText, paint, width);
                if (idx == 0) {
                    break;
                }

                // extract the line. remove any trailing newline characters. optionally trim spaces.
                string lastLine = remainingText.Substring(0, idx);
                lastLine = lastLine.ReplaceLineEndings(string.Empty);
                lines.Add( (bTrimStrings) ? lastLine.Trim() : lastLine);

                // it's possible we broke right at the end of the line (due to width) without
                // including the trailing newline characters. In that case, they can be dropped.
                while (idx < remainingText.Length && (remainingText[idx] == '\r' || remainingText[idx] == '\n') )
                    idx++;

                remainingText = remainingText.Substring(idx);

            } while (!string.IsNullOrEmpty(remainingText));
            return lines;
        }

        public static int LineBreak(string text, SKPaint paint, float width)
        {
            int next_newline_idx = text.IndexOf('\n');

            int idx = 0, last = 0;
            int max_chars_in_width = (int)paint.BreakText(text, width);     // todo does BreakText include newline boxes??

            // if we have a newline before max chars, we break at the newline
            if (next_newline_idx > 0 && next_newline_idx < max_chars_in_width)
                return (next_newline_idx+1);
            
            // if all chars fit, we do not have to break
            if (max_chars_in_width == text.Length)
                return max_chars_in_width;

            // don't quite understand this loop yet...
            while (idx < text.Length) {
                int next = text.IndexOfAny(new char[] { ' ', '\n' }, idx);
                if (next == -1) {
                    if (idx == 0) {
                        return max_chars_in_width;
                    } else {
                        // Ellipsize if it's the last line
                        if (max_chars_in_width == text.Length
                        // || text.IndexOfAny (new char [] { ' ', '\n' }, lengthBreak + 1) == -1
                        ) {
                            return max_chars_in_width;
                        }
                        // Split at the last word;
                        return last;
                    }
                }
                if (text[idx] == '\n') {
                    return idx;
                }
                if (next > max_chars_in_width) {
                    return max_chars_in_width;
                }
                last = next;
                idx = next + 1;
            }
            return last;
        }



    }
}
