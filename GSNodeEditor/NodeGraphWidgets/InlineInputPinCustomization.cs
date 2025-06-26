using Gradientspace.NodeGraph;
using Gradientspace.UI;
using System.Diagnostics;

namespace GSNodeEditor
{
    public interface IInlinePinWidget
    {
        float RequiredWidth { get; }
    }

    public interface IInlinePinWidgetProvider
    {
        Widget? CreateNewWidget(INodeGraph Graph, int OwningNodeIdentifier, NodeInputPinWidget pinWidget, object? defaultValue, bool bDefaultIsDefined);
    }


    public sealed class InlinePinWidgetSystem
    {
        // singleton pattern
        static InlinePinWidgetSystem() { }
        private InlinePinWidgetSystem() { }
        private static readonly InlinePinWidgetSystem instance = new InlinePinWidgetSystem();
        public static InlinePinWidgetSystem Instance {
            get {
                return instance;
            }
        }


        private Dictionary<string, IInlinePinWidgetProvider> Library = new Dictionary<string, IInlinePinWidgetProvider>();

        public void RegisterProvider(Type PinDataType, IInlinePinWidgetProvider provider)
        {
            Debug.Assert(PinDataType.FullName != null);
            if (Library.ContainsKey(PinDataType.FullName))
                throw new Exception("InlinePinWidgetSystem: tried to register duplicate data type " + PinDataType.FullName);
               
            Library.Add(PinDataType.FullName, provider);
        }


        public IInlinePinWidgetProvider? FindProvider(Type PinDataType) 
        {
            if (PinDataType.FullName == null) return null;
            if ( Library.TryGetValue(PinDataType.FullName, out IInlinePinWidgetProvider? provider) )
                return provider;
            return null;
        }


    }

}
