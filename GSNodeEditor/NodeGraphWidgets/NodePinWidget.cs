// Copyright Gradientspace Corp. All Rights Reserved.
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
        public NodePinWidgetStyle WidgetStyle { get; set; } = PinWidgetStyles.DefaultInputStyleSet;
        public bool CompactMode { get; set; } = false;

        public GraphDataType DataType { get; init; }

        public abstract bool IsOutputPin { get; }


        public virtual string GetDataTypeAsString()
        {
            return TypeUtils.TypeToString(DataType.CSType);
        }


        protected NodeGraphView? FindParentGraphView()
        {
            Widget? curParent = parentWidget;
            while (curParent != null)
            {
                if (curParent is NodeWidget nodeWidget)
                    return nodeWidget.ParentGraphWidget;
                else
                    curParent = curParent.ParentWidget;
			}
            return null;
        }
        protected NodeGraphView FindParentGraphViewChecked()
        {
            NodeGraphView? found = FindParentGraphView();
            System.Diagnostics.Debug.Assert(found != null);
            return found;
        }

	}
}
