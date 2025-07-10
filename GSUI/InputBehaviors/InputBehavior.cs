// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.UI
{
    public struct InputCaptureRequest
    {
        public object? SourceObject = null;
        public InputBehavior? SourceBehavior = null;

        // ZDepth is the hit depth and Priority is currently un-used
        // Maybe Priority should be what the 'Depth' of an InputBehavior maps to?
        // and then ZDepth comparison should consider priority first, or as a modifier to depth, or 
        // priority is what takes effect if depths are equal?

        public float ZDepth = -float.MaxValue;
        public float Priority = 1.0f;

        public InputCaptureRequest() { }
        public InputCaptureRequest(Object? sourceObject, float depth) { SourceObject = sourceObject; ZDepth = depth; }
        public InputCaptureRequest(Object? sourceObject, InputBehavior sourceBehavior, float depth) { SourceObject = sourceObject; SourceBehavior = sourceBehavior; ZDepth = depth; }

        public static InputCaptureRequest None = new InputCaptureRequest();

        public static bool operator==(InputCaptureRequest left, InputCaptureRequest right) {
            return left.SourceObject == right.SourceObject && left.SourceBehavior == right.SourceBehavior;
        }
        public static bool operator!=(InputCaptureRequest left, InputCaptureRequest right) {
            return !(left == right);
        }
        public override bool Equals(object? obj) {
            return obj is InputCaptureRequest && ( (InputCaptureRequest)obj == this );
        }
        public override int GetHashCode() {
            return HashCode.Combine(SourceObject?.GetHashCode() ?? 0, SourceBehavior?.GetHashCode() ?? 0);
        }

        public InputCaptureRequest SelectCapture(in InputCaptureRequest otherRequest)
        {
            return (otherRequest.ZDepth > this.ZDepth) ? otherRequest : this;
        }
    }

    public abstract class InputBehavior
    {
		// evolve into something like priority....
		// TODO: this should nearly always be zero and only used to handle cases where
		// there are multiple input behaviors on the same object, or to handle 'always-hit' / 'do-on-miss' type
		// behaviors. Maybe needs to be a struct with a type to handle these different cases...

		// (maybe split into separate optional Depth, and a Priority, like InputCaptureRequest)
		public int Depth { get; set; } = 0;

        public abstract InputCaptureRequest CheckForCapture(in InputDeviceState deviceState);
        public abstract void BeginCapture(in InputDeviceState deviceState, in InputCaptureRequest fromRequest);
        public abstract void UpdateCapture(in InputDeviceState deviceState);
        public abstract void EndCapture(in InputDeviceState deviceState);
        public abstract void AbortCapture();

        public abstract InputCaptureRequest CheckForHover(in InputDeviceState deviceState);
        public abstract void BeginHover(in InputDeviceState deviceState);
        public abstract void UpdateHover(in InputDeviceState deviceState, out bool bContinueHover);
        public abstract void EndHover(in InputDeviceState deviceState);
    }


    public interface IExtendedInputBehavior
    {
        InputCaptureRequest CheckForCapture(in InputDeviceState deviceState, InputCaptureRequest baseRequest);
        void BeginCapture(in InputDeviceState deviceState, in InputCaptureRequest fromRequest);
        void UpdateCapture(in InputDeviceState deviceState);
        void EndCapture(in InputDeviceState deviceState);
        void AbortCapture();
    }


    public interface ISimpleCaptureTarget
    {
        public enum ECaptureState { Begin, Update, End, Abort }
        void UpdateCapture(ECaptureState State, in InputDeviceState deviceState);

        public enum EHoverState { Begin = 0, Update = 1, End = 2 }
        void UpdateHover(EHoverState State, in InputDeviceState deviceState, out bool bContinueHover);
    }



    public interface IInputBehaviorCollection
    {
        InputCaptureRequest CheckForHoverCapture(in InputDeviceState deviceState);
        InputCaptureRequest CheckForDeviceCapture(in InputDeviceState deviceState);
    }


    public struct InputCaptureCollector
    {
        public InputCaptureRequest SelectedRequest { get; private set; } = InputCaptureRequest.None;

        public InputCaptureCollector() { }

        public void ConsiderRequest(InputCaptureRequest captureRequest)
        {
            SelectedRequest = SelectedRequest.SelectCapture(captureRequest);
        }
    }



    public class InputBehaviorSet : IInputBehaviorCollection
    {
        List<InputBehavior> Behaviors = new List<InputBehavior>();

        public void AddBehavior(InputBehavior behavior) { Behaviors.Add(behavior); }
        public void RemoveBehavior(InputBehavior behavior) { Behaviors.Remove(behavior); }

        public InputCaptureRequest CheckForHoverCapture(in InputDeviceState deviceState)
        {
            InputCaptureCollector Collector = new InputCaptureCollector();
            foreach (InputBehavior behavior in Behaviors)
            {
                InputCaptureRequest captureRequest = behavior.CheckForHover(deviceState);
                Collector.ConsiderRequest(captureRequest);
            }
            return Collector.SelectedRequest;
        }

        public InputCaptureRequest CheckForDeviceCapture(in InputDeviceState deviceState)
        {
            InputCaptureCollector Collector = new InputCaptureCollector();
            foreach (InputBehavior behavior in Behaviors)
            {
                InputCaptureRequest captureRequest = behavior.CheckForCapture(deviceState);
                Collector.ConsiderRequest(captureRequest);
            }
            return Collector.SelectedRequest;
        }
    }




    public class InputBehaviorCollectionSet : IInputBehaviorCollection
    {
        List<IInputBehaviorCollection> Collections = new List<IInputBehaviorCollection>();

        public void AddCollection(IInputBehaviorCollection collection) { Collections.Add(collection); }
        public void RemoveBehavior(IInputBehaviorCollection collection) { Collections.Remove(collection); }

        public InputCaptureRequest CheckForHoverCapture(in InputDeviceState deviceState)
        {
            InputCaptureCollector Collector = new InputCaptureCollector();
            foreach (IInputBehaviorCollection collection in Collections)
            {
                InputCaptureRequest captureRequest = collection.CheckForHoverCapture(deviceState);
                Collector.ConsiderRequest(captureRequest);
            }
            return Collector.SelectedRequest;
        }

        public InputCaptureRequest CheckForDeviceCapture(in InputDeviceState deviceState)
        {
            InputCaptureCollector Collector = new InputCaptureCollector();
            foreach (IInputBehaviorCollection collection in Collections)
            {
                InputCaptureRequest captureRequest = collection.CheckForDeviceCapture(deviceState);
                Collector.ConsiderRequest(captureRequest);
            }
            return Collector.SelectedRequest;
        }
    }



}
