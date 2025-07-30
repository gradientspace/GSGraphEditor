using g3;
using Gradientspace.UI;

namespace GSNodeEditor
{
    public class NodeWidgetStyle
    {
        public WidgetStateStyle NodeStyle { get; set; }

        public NodeWidgetStyle()
        {
            NodeStyle = NodeWidgetStyles.DefaultNodeStyleSet;
        }
    }


    public static class NodeWidgetStyles
    {
        public const float NodeTextSize = 16;
        //public const float PinTextSize = 14;
        public const string NodeFont = "Calibri";
        public static WidgetMargins NodeMargins = new WidgetMargins(2, 5);


        public static WidgetStyle DefaultNodeStyle = new WidgetStyle() { BackgroundColor = Colorf.SlateGrey, TextSize = NodeTextSize, FontName = NodeFont, Margins = NodeMargins };
        public static WidgetStyle DefaultNodeHoverStyle = new WidgetStyle() { BackgroundColor = Colorf.Orange };
        public static WidgetStyle DefaultNodePressedStyle = new WidgetStyle() { BackgroundColor = Colorf.Gold };
        public static WidgetStateStyle DefaultNodeStyleSet = new WidgetStateStyle(DefaultNodeStyle, DefaultNodeHoverStyle, DefaultNodePressedStyle);

        public static WidgetStyle NodeErrorStyle = new WidgetStyle() { BackgroundColor = Colorf.VideoRed, TextSize = NodeTextSize, FontName = NodeFont, Margins = NodeMargins };
        public static WidgetStateStyle NodeErrorStyleSet = new WidgetStateStyle(NodeErrorStyle, DefaultNodeHoverStyle, DefaultNodePressedStyle);

        public static WidgetStyle NodePlaceholderStyle = new WidgetStyle() { BackgroundColor = Colorf.LightGrey, TextSize = NodeTextSize, FontName = NodeFont, Margins = NodeMargins };
        public static WidgetStateStyle NodePlaceholderStyleSet = new WidgetStateStyle(NodePlaceholderStyle, DefaultNodeHoverStyle, DefaultNodePressedStyle);

        public static WidgetStyle NodeDebugStyle = new WidgetStyle() { BackgroundColor = Colorf.VideoYellow, TextSize = NodeTextSize, FontName = NodeFont, Margins = NodeMargins };
        public static WidgetStateStyle NodeDebugStyleSet = new WidgetStateStyle(NodeDebugStyle, DefaultNodeHoverStyle, DefaultNodePressedStyle);

        public static WidgetStyle FunctionDefNodeStyle = new WidgetStyle() { BackgroundColor = new Colorf(161, 64, 193, 255), TextSize = NodeTextSize, FontName = NodeFont, Margins = NodeMargins };
        public static WidgetStateStyle FunctionDefNodeStyleSet = new WidgetStateStyle(FunctionDefNodeStyle, DefaultNodeHoverStyle, DefaultNodePressedStyle);

        public static WidgetStyle FunctionCallNodeStyle = new WidgetStyle() { BackgroundColor = new Colorf(161, 128, 193, 255), TextSize = NodeTextSize, FontName = NodeFont, Margins = NodeMargins };
        public static WidgetStateStyle FunctionCallNodeStyleSet = new WidgetStateStyle(FunctionCallNodeStyle, DefaultNodeHoverStyle, DefaultNodePressedStyle);



        public static NodeWidgetStyle DefaultNode = new NodeWidgetStyle();
        public static NodeWidgetStyle PlaceholderNode = new NodeWidgetStyle() { NodeStyle = NodePlaceholderStyleSet };
        public static NodeWidgetStyle FunctionDefNode = new NodeWidgetStyle() { NodeStyle = FunctionDefNodeStyleSet };
        public static NodeWidgetStyle FunctionCallNode = new NodeWidgetStyle() { NodeStyle = FunctionCallNodeStyleSet };
    }

}
