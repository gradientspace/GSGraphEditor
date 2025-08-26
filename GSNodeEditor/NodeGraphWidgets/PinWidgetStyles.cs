// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    public class NodePinWidgetStyle : WidgetStateStyle
    {
        public NodePinWidgetStyle(WidgetStyle standard, WidgetStyle hovered, WidgetStyle pressed) : base(standard, hovered, pressed)
        {
        }
    }

    public static class PinWidgetStyles
    {
        public const float PinTextSize = 14;
        public const string PinFont = "Calibri";


        public static WidgetStyle DefaultInputStandardStyle = new WidgetStyle() { BackgroundColor = Colorf.DarkGrey, TextSize = PinTextSize, FontName = PinFont };
        public static WidgetStyle DefaultInputHoverStyle = new WidgetStyle() { BackgroundColor = Colorf.Orange };
        public static WidgetStyle DefaultInputPressedStyle = new WidgetStyle() { BackgroundColor = Colorf.LightGrey };
        public static NodePinWidgetStyle DefaultInputStyleSet = new NodePinWidgetStyle(DefaultInputStandardStyle, DefaultInputHoverStyle, DefaultInputPressedStyle);

        public static WidgetStyle InputStandardStyle_Conversion = new WidgetStyle() { BackgroundColor = Colorf.Goldenrod, TextSize = PinTextSize, FontName = PinFont };
        public static NodePinWidgetStyle InputStyleSet_Conversion = new NodePinWidgetStyle(InputStandardStyle_Conversion, DefaultInputHoverStyle, DefaultInputPressedStyle);

        public static WidgetStyle InputStandardStyle_Dynamic = new WidgetStyle() { BackgroundColor = Colorf.Cyan, TextSize = PinTextSize, FontName = PinFont };
        public static NodePinWidgetStyle InputStyleSet_Dynamic = new NodePinWidgetStyle(InputStandardStyle_Dynamic, DefaultInputHoverStyle, DefaultInputPressedStyle);

        public static WidgetStyle DefaultOutputStandardStyle = new WidgetStyle() { BackgroundColor = Colorf.DarkGrey, TextSize = PinTextSize, FontName = PinFont };
        public static WidgetStyle DefaultOutputHoverStyle = new WidgetStyle() { BackgroundColor = Colorf.Orange };
        public static WidgetStyle DefaultOutputPressedStyle = new WidgetStyle() { BackgroundColor = Colorf.LightGrey };
        public static NodePinWidgetStyle DefaultOutputStyleSet = new NodePinWidgetStyle(DefaultOutputStandardStyle, DefaultOutputHoverStyle, DefaultOutputPressedStyle);

        public static WidgetStyle OutputStandardStyle_ControlFlow = new WidgetStyle() { BackgroundColor = Colorf.VideoWhite, TextSize = PinTextSize, FontName = PinFont };
        public static NodePinWidgetStyle OutputStyleSet_ControlFlow = new NodePinWidgetStyle(OutputStandardStyle_ControlFlow, DefaultInputHoverStyle, DefaultInputPressedStyle);

        // style for dynamically/spawned 'missing' input/output pins
        public static WidgetStyle InputOutputStyle_Missing = new WidgetStyle() { BackgroundColor = Colorf.Red, TextSize = PinTextSize, FontName = PinFont };
        public static NodePinWidgetStyle InputOutputStyleSet_Missing = new NodePinWidgetStyle(InputOutputStyle_Missing, DefaultInputHoverStyle, DefaultInputPressedStyle);


        public static WidgetStyle DefaultSequenceStandardStyle = new WidgetStyle() { BackgroundColor = Colorf.DarkSlateGrey, ForegroundColor = Colorf.VideoWhite, TextSize = PinTextSize, FontName = PinFont };
        public static WidgetStyle DefaultSequenceHoverStyle = new WidgetStyle() { BackgroundColor = Colorf.Orange };
        public static WidgetStyle DefaultSequencePressedStyle = new WidgetStyle() { BackgroundColor = Colorf.Wheat };
        public static NodePinWidgetStyle DefaultSequenceStyleSet = new NodePinWidgetStyle(DefaultSequenceStandardStyle, DefaultSequenceHoverStyle, DefaultSequencePressedStyle);
    }

}
