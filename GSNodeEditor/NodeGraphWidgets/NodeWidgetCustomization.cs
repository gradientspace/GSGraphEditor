using Gradientspace.NodeGraph;
using Gradientspace.UI;
using System.Diagnostics;

namespace GSNodeEditor
{
    public interface INodeWidgetProvider
    {
        NodeWidget? CreateNewWidget(NodeGraphView Graph, INodeInfo nodeInfo);
    }


    public sealed class NodeWidgetCustomizationSystem
    {
        // singleton pattern
        static NodeWidgetCustomizationSystem() { }
        private NodeWidgetCustomizationSystem() { }
        private static readonly NodeWidgetCustomizationSystem instance = new NodeWidgetCustomizationSystem();
        public static NodeWidgetCustomizationSystem Instance
        {
            get
            {
                return instance;
            }
        }

        private Dictionary<Type, INodeWidgetProvider> Library = new Dictionary<Type, INodeWidgetProvider>();

        public void RegisterProvider(Type nodeClassType, INodeWidgetProvider provider)
        {
            if (Library.ContainsKey(nodeClassType))
                throw new Exception("NodeWidgetCustomizationSystem: tried to register duplicate node type " + nodeClassType.Name);

            Library.Add(nodeClassType, provider);
        }


        public INodeWidgetProvider? FindProvider(Type nodeClassType)
        {
            if (Library.TryGetValue(nodeClassType, out INodeWidgetProvider? provider)) {
                return provider;
            }
            return null;
        }


    }

}
