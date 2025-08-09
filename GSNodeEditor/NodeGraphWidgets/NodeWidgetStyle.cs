// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
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

        public static WidgetStyle VariableDefNodeStyle = new WidgetStyle() { BackgroundColor = new Colorf(16, 164, 164), TextSize = NodeTextSize, FontName = NodeFont, Margins = NodeMargins };
        public static WidgetStateStyle VariableDefNodeStyleSet = new WidgetStateStyle(VariableDefNodeStyle, DefaultNodeHoverStyle, DefaultNodePressedStyle);

        public static WidgetStyle VariableAccessNodeStyle = new WidgetStyle() { BackgroundColor = new Colorf(96, 164, 164), TextSize = NodeTextSize, FontName = NodeFont, Margins = NodeMargins };
        public static WidgetStateStyle VariableAccessNodeStyleSet = new WidgetStateStyle(VariableAccessNodeStyle, DefaultNodeHoverStyle, DefaultNodePressedStyle);


        public static WidgetStyle FunctionDefNodeStyle = new WidgetStyle() { BackgroundColor = new Colorf(161, 64, 193), TextSize = NodeTextSize, FontName = NodeFont, Margins = NodeMargins };
        public static WidgetStateStyle FunctionDefNodeStyleSet = new WidgetStateStyle(FunctionDefNodeStyle, DefaultNodeHoverStyle, DefaultNodePressedStyle);

        public static WidgetStyle FunctionCallNodeStyle = new WidgetStyle() { BackgroundColor = new Colorf(161, 128, 193), TextSize = NodeTextSize, FontName = NodeFont, Margins = NodeMargins };
        public static WidgetStateStyle FunctionCallNodeStyleSet = new WidgetStateStyle(FunctionCallNodeStyle, DefaultNodeHoverStyle, DefaultNodePressedStyle);



        public static NodeWidgetStyle DefaultNode = new NodeWidgetStyle();
        public static NodeWidgetStyle PlaceholderNode = new NodeWidgetStyle() { NodeStyle = NodePlaceholderStyleSet };
        public static NodeWidgetStyle VariableNode = new NodeWidgetStyle() { NodeStyle = VariableDefNodeStyleSet };
        public static NodeWidgetStyle VariableAccessNode = new NodeWidgetStyle() { NodeStyle = VariableAccessNodeStyleSet };
        public static NodeWidgetStyle FunctionDefNode = new NodeWidgetStyle() { NodeStyle = FunctionDefNodeStyleSet };
        public static NodeWidgetStyle FunctionCallNode = new NodeWidgetStyle() { NodeStyle = FunctionCallNodeStyleSet };




        public static NodeWidgetStyle GetStyleByNodeType(INode? Node)
        {
            if (Node is PlaceholderNodeBase)
                return NodeWidgetStyles.PlaceholderNode;
            else if (Node is FunctionDefinitionNode)
                return NodeWidgetStyles.FunctionDefNode;
            else if (Node is FunctionCallNode || Node is FunctionReturnNode)
                return NodeWidgetStyles.FunctionCallNode;
            else if (Node is DefineVariableBaseNode)
                return NodeWidgetStyles.VariableNode;
            else if (Node is AccessVariableNode)
                return NodeWidgetStyles.VariableAccessNode;

            return NodeWidgetStyles.DefaultNode;
        }
    }

}
