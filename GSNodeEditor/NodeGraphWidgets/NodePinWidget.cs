using Gradientspace.NodeGraph;
using Gradientspace.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    /**
     * NodePinWidget is a base class for all input/output/sequence pins
     */
    public abstract class NodePinWidget : Widget
    {
        public NodePinWidgetStyle WidgetStyle { get; set; } = NodeWidgetStyles.DefaultInputStyleSet;
        public bool CompactMode { get; set; } = false;

        public GraphDataType DataType { get; init; }

        public abstract bool IsOutputPin { get; }


        public virtual string GetDataTypeAsString()
        {
            return TypeUtils.TypeToString(DataType.DataType);
        }

    }
}
