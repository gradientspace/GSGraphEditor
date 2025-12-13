using g3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    public static class PlacementUtils
    {
        public static Vector2f ApplyNodePositionConstraints(Vector2f InputPosition)
        {
            Vector2f FinalPos = InputPosition;
            if (Settings.EnableGridSnapping.Value == true) 
            {
                float SnapStep = Settings.GridSnappingSize.Value;
                FinalPos.x = (float)Snapping.SnapToIncrement(InputPosition.x, SnapStep);
                FinalPos.y = (float)Snapping.SnapToIncrement(InputPosition.y, SnapStep);
            }
            return FinalPos;
        }
    }
}
