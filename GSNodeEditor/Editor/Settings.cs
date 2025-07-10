// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using Gradientspace.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    public class WatchableSetting<T>
    {
        private T _value;

        public delegate void ModifiedEventHandler(WatchableSetting<T> Setting);
        public event ModifiedEventHandler? OnModified;

        public T Value {
            get { return _value; }
            set { _value = value; OnModified?.Invoke(this); }
        }

        public WatchableSetting(T initialValue)
        {
            _value = initialValue;
        }
    }


    public class WatchableWrappedSetting<T>
    {
        private Func<T> GetValueFunc;
        private Action<T> SetValueFunc;

        public delegate void ModifiedEventHandler(WatchableWrappedSetting<T> Setting);
        public event ModifiedEventHandler? OnModified;

        public T Value
        {
            get { return GetValueFunc(); }
            set { SetValueFunc(value); OnModified?.Invoke(this); }
        }

        public WatchableWrappedSetting(Func<T> getValue, Action<T> setValue)
        {
            GetValueFunc = getValue;
            SetValueFunc = setValue;
        }
    }


    public static class Settings
    {
        public static WatchableWrappedSetting<bool> EnableDebugging = new WatchableWrappedSetting<bool>(
            () => { return DebugManager.GlobalEnableGraphDebugging; },
            (bool bEnable) => { DebugManager.GlobalEnableGraphDebugging = bEnable; });

        public static WatchableSetting<bool> EnableGridSnapping = new WatchableSetting<bool>(true);
        public static WatchableSetting<int> GridSnappingSize = new WatchableSetting<int>(10);
    }
}
