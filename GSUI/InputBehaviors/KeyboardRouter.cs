using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.UI
{
    public enum KeyType
    {
        CharacterKey,       // a key that can be in a string
        FunctionalKey,      // a key that cannot be in a string
        UnknownKey          // a key that is not yet supported
    }

    public enum KeyNames
    {
        Unnamed,

        Space,
        Tab,

        Escape,
        Shift,
        Ctrl,
        Alt,
        Delete,
        Backspace,
        Enter,

        UpArrow,
        DownArrow,
        LeftArrow,
        RightArrow
    }

    public struct KeyState
    {
        public KeyType KeyType = KeyType.CharacterKey;
        public KeyNames KeyName = KeyNames.Space;
        public char Character = ' ';               // this is a unicode character!

        public bool bCtrlDown = false;
        public bool bShiftDown = false;
        public bool bAltDown = false;

        public KeyState() { }
        public KeyState(KeyType keyType, KeyNames keyName, char character = ' ') {
            KeyType = keyType;
            KeyName = keyName;
            Character = character;
        }

        public bool IsSameKey(KeyState other)
        {
            if (KeyType == KeyType.CharacterKey) return other.KeyType == KeyType.CharacterKey && Character == other.Character;
            else if (KeyType == KeyType.FunctionalKey) return other.KeyType == KeyType.FunctionalKey && KeyName == other.KeyName;
            else return false;      // Unknown keys are never equal
        }

        public bool IsKnownKey {  get { return KeyType != KeyType.UnknownKey; } }
        public bool IsFunctionalKey {  get { return KeyType == KeyType.FunctionalKey; } }
        public bool IsCharacterKey { get { return KeyType == KeyType.CharacterKey; } }

        public static KeyState Unknown = new KeyState(KeyType.UnknownKey, KeyNames.Unnamed);
        public static KeyState Escape = new KeyState(KeyType.FunctionalKey, KeyNames.Escape);
        public static KeyState Shift = new KeyState(KeyType.FunctionalKey, KeyNames.Shift);
        public static KeyState Ctrl = new KeyState(KeyType.FunctionalKey, KeyNames.Ctrl);
        public static KeyState Alt = new KeyState(KeyType.FunctionalKey, KeyNames.Alt);
        public static KeyState Delete = new KeyState(KeyType.FunctionalKey, KeyNames.Delete);
        public static KeyState Backspace = new KeyState(KeyType.FunctionalKey, KeyNames.Backspace);
        public static KeyState Enter = new KeyState(KeyType.FunctionalKey, KeyNames.Enter);

        public static KeyState KeyW = new KeyState(KeyType.CharacterKey, KeyNames.Unnamed, 'w');
        public static KeyState KeyA = new KeyState(KeyType.CharacterKey, KeyNames.Unnamed, 'a');
        public static KeyState KeyS = new KeyState(KeyType.CharacterKey, KeyNames.Unnamed, 's');
        public static KeyState KeyD = new KeyState(KeyType.CharacterKey, KeyNames.Unnamed, 'd');

		public static KeyState KeyQ = new KeyState(KeyType.CharacterKey, KeyNames.Unnamed, 'q');

		public static KeyState Space = new KeyState(KeyType.CharacterKey, KeyNames.Space, ' ');
        public static KeyState Tab = new KeyState(KeyType.CharacterKey, KeyNames.Tab);     // AAHH tab should be character?

        public static KeyState LeftArrow = new KeyState(KeyType.FunctionalKey, KeyNames.LeftArrow);
        public static KeyState RightArrow = new KeyState(KeyType.FunctionalKey, KeyNames.RightArrow);
        public static KeyState UpArrow = new KeyState(KeyType.FunctionalKey, KeyNames.UpArrow);
        public static KeyState DownArrow = new KeyState(KeyType.FunctionalKey, KeyNames.DownArrow);

        public void ConfigureModifiers(in InputDeviceState deviceState)
        {
            bAltDown = deviceState.AltButton.bDown;
            bCtrlDown = deviceState.CtrlButton.bDown;
            bShiftDown = deviceState.ShiftButton.bDown;
        }


        static public KeyState MakeKeyStateFromCharacter(char Character, in InputDeviceState deviceState)
        {
            KeyState keyState = KeyState.Unknown;
            if ( Char.IsControl(Character) )
            {
                switch ((uint)Character)
                {
                    case 8:   keyState = KeyState.Backspace; break;
                    case 9:   keyState = KeyState.Tab; break;
                    case 13:  keyState = KeyState.Enter; break;
                    case 27:  keyState = KeyState.Escape; break;
                    case 127: keyState = KeyState.Delete; break;
                }
            }
            else
            {
                keyState.KeyType = KeyType.CharacterKey;
                keyState.KeyName = KeyNames.Unnamed;
                keyState.Character = Character;
            }

            keyState.ConfigureModifiers(deviceState);
            return keyState;
        }

    }


    [System.Runtime.CompilerServices.InlineArray(3)]
    public struct KeySequence3
    {
        private KeyState _element0;
    }

    // todo this probably should be replaced w/ just an array of keys that includes the order of ctrl/shift/etc being pressed...
    public struct KeyChord
    {
        public bool bCtrlDown = false;
        public bool bShiftDown = false;
        public bool bAltDown = false;

        public int NumKeys = 0;
        public KeySequence3 KeySequence = new KeySequence3();

        public KeyChord()
        {
        }

        public bool IsSingleSpecialKey(KeyNames specialKey)
        {
            return NumKeys == 1 && KeySequence[0].KeyName == specialKey;
        }
        public bool IsChord2(KeyNames modifierKey, char Character)
        {
            return NumKeys == 2 && KeySequence[0].KeyName == modifierKey && KeySequence[1].Character == Character;
        }
        public bool IsChord3(KeyNames modifierKey1, KeyNames modifierKey2, char Character, bool bUnordered = true)
        {
            return NumKeys == 3 
                && ((KeySequence[0].KeyName == modifierKey1 && KeySequence[1].KeyName == modifierKey2) || (KeySequence[0].KeyName == modifierKey2 && KeySequence[1].KeyName == modifierKey1))
                && KeySequence[2].Character == Character;
        }
    }



    public interface ITextEntryFocusTarget
    {
        //! return true if key is consumed
        bool OnNextKey(KeyState keyState);

        void OnPasteText(string pasteString);
        bool GetCurrentSelectedText(out string text);

        //! called for keys like Escape, Enter, that otherwise will commit/terminate a live text edit
        bool TryHandleTextEntryHotkey(KeyState keyState);

        void OnSelectAll();

        enum EndFocusType
        {
            Commit,
            Cancel
        }

        void OnEndFocus(EndFocusType endType);
    }


    public interface IHotkeyTarget
    {
        bool OnKeyChordUpdated(in KeyChord ActiveChord);
    }

    public interface IRawKeyInputTarget
    {
        bool OnKeyPress(KeyState PressedKey);
        bool OnKeyRelease(KeyState ReleasedKey);
        bool OnKeyRepeat(KeyState RepeatKey, in KeyChord ActiveChord);
    }


    public class KeyboardRouter
    {
        public KeyboardRouter() { }

        List<KeyState> ActivePressedKeys = new List<KeyState>();


        // temporary hack for sending hotkeys...
        public delegate void NewPressedKeyEventHandler(KeyboardRouter sender, KeyState[] NewKeyChord);
        public event NewPressedKeyEventHandler? OnNewPressedKey;

        // this event is emitted on a ctrl+c text-copy, if some text was extracted from current text-entry/etc
        public delegate void TextCopiedEventHandler(KeyboardRouter sender, string NewCopiedText);
        public event TextCopiedEventHandler? OnTextCopied;


        public KeyChord GetCurrentKeyChord()
        {
            if (ActivePressedKeys.Count == 0)
                return new KeyChord();

            KeyChord chord = new KeyChord();
            chord.bShiftDown = ActivePressedKeys[0].bShiftDown;
            chord.bCtrlDown = ActivePressedKeys[0].bCtrlDown;
            chord.bAltDown = ActivePressedKeys[0].bAltDown;
            for ( int i = 0; i < Math.Min(3, ActivePressedKeys.Count); ++i )
            {
                chord.KeySequence[i] = ActivePressedKeys[i];
                chord.NumKeys++;
            }
            return chord;
        }

        public bool InActiveChord {
            get { return ActivePressedKeys.Count > 0; }
        }


        bool bWaitForAllKeysUpPending = false;

        public virtual void OnRawKeyDown(KeyState keyState)
        {
            // ignore unknown keys
            if (keyState.KeyType == KeyType.UnknownKey) return;

            // try to fix up if we got into invalid state somehow
            if (bWaitForAllKeysUpPending && ActivePressedKeys.Count == 0)
                bWaitForAllKeysUpPending = false;

            if (bWaitForAllKeysUpPending)
                return;

            // if no chord is in progress, allow raw keyinput targets to see this event
            if ( ActivePressedKeys.Count == 0 ) {
                foreach (IRawKeyInputTarget rawTarget in rawKeyInputTargets) {
                    if ( rawTarget.OnKeyPress(keyState) )
                        return;
                }
            }

            int DownIndex = ActivePressedKeys.FindIndex(k => k.IsSameKey(keyState));
            if (DownIndex < 0 )
            {
                ActivePressedKeys.Add(keyState);
                KeyChord currentChord = GetCurrentKeyChord();

                bool bConsumed = false;

                // If there is an active focused text-entry target, it may want to consume various text-editing keys.
                // Eg delete, backspace, arrows, etc. We have to give it a chance to handle this before we
                // pass to more general hotkey handlers.
                // TODO: possibly more complex chords should circumvent this? ie if we have ctrl+ or alt+ ...
                if (activeTextTarget != null)
                {
                    if (currentChord.IsChord2(KeyNames.Ctrl, 'V'))
                    {
                        if (LastClipboardText.Length > 0)
                            activeTextTarget.OnPasteText(LastClipboardText);
                        bWaitForAllKeysUpPending = true;
                        bConsumed = true;
                    } 
                    else if (currentChord.IsChord2(KeyNames.Ctrl, 'C')) {
                        if (activeTextTarget.GetCurrentSelectedText(out string CopiedText)) {
                            LastClipboardText = CopiedText;
                            OnTextCopied?.Invoke(this, CopiedText);
                        }
                        bWaitForAllKeysUpPending = true;
                        bConsumed = true;
                    }
                    else if (currentChord.IsChord2(KeyNames.Ctrl, 'A')) {
                        activeTextTarget.OnSelectAll();
                        bWaitForAllKeysUpPending = true;
                        bConsumed = true;
                    }
                    else
                        bConsumed = AppendKeyDownToFocusTarget(keyState);
                }
                if (bConsumed)      // wait for pending?
                    return;

                // check if anything in the active hotkey stack wants to consume the current key chord
                if (activeHotkeyStack.Count > 0)
                {
                    for ( int i = 0; i < activeHotkeyStack.Count; ++i )
                    {
                        if (activeHotkeyStack[i].OnKeyChordUpdated(currentChord))
                        {
                            bConsumed = true;
                            break;
                        }
                    }
                }
                // if something consumed it, we are done
                if ( bConsumed )
                {
                    bWaitForAllKeysUpPending = true;
                    return;
                }

                OnNewPressedKey?.Invoke(this, ActivePressedKeys.ToArray());
            }
        }


        public virtual void OnRawKeyUp(KeyState keyState)
        {
            if (keyState.KeyType == KeyType.UnknownKey) return;

            // if no chord is in progress, allow raw keyinput targets to see this event
            if ( ActivePressedKeys.Count == 0 ) {
                foreach (IRawKeyInputTarget rawTarget in rawKeyInputTargets) {
                    if ( rawTarget.OnKeyRelease(keyState) )
                        return;
                }
            }

            // todo should we consider sending to activeHotkeyStack on key-up? probably not...

            // this happens if we discarded the active key set on focus change,
            // which currently includes mouse-press. Maybe have to figure out some
            // other way to handle mouse-press to avoid getting into weird states.
            int DownIndex = ActivePressedKeys.FindIndex(k => k.IsSameKey(keyState));
            if (DownIndex >= 0)
            {
                ActivePressedKeys.RemoveAt(DownIndex);
            }

            if (bWaitForAllKeysUpPending && ActivePressedKeys.Count == 0) {
                bWaitForAllKeysUpPending = false;
            }
        }


        public virtual bool OnRawKeyRepeat(KeyState keyState)
        {
            foreach (IRawKeyInputTarget rawTarget in rawKeyInputTargets) {
                if ( rawTarget.OnKeyRepeat(keyState, GetCurrentKeyChord()) )
                    return true;
            }
            return false;
        }


        List<IHotkeyTarget> activeHotkeyStack = new List<IHotkeyTarget>();

        public virtual void PushHotkeyTarget(IHotkeyTarget hotkeyTarget)
        {
            if (activeHotkeyStack.Contains(hotkeyTarget) == false)
                activeHotkeyStack.Insert(0, hotkeyTarget);
        }

        public virtual void PopHotkeyTarget(IHotkeyTarget hotkeyTarget)
        {
            activeHotkeyStack.Remove(hotkeyTarget);
        }


        List<IRawKeyInputTarget> rawKeyInputTargets = new List<IRawKeyInputTarget>();

        public virtual void PushRawKeyTarget(IRawKeyInputTarget rawkeyTarget)
        {
            if (rawKeyInputTargets.Contains(rawkeyTarget) == false)
                rawKeyInputTargets.Insert(0, rawkeyTarget);
        }

        public virtual void PopRawKeyTarget(IRawKeyInputTarget rawkeyTarget)
        {
            rawKeyInputTargets.Remove(rawkeyTarget);
        }


        ITextEntryFocusTarget? activeTextTarget = null;

        public bool HasTextEntryFocusTarget { get { return activeTextTarget != null; } }

        public virtual void SetTextEntryFocusTarget(ITextEntryFocusTarget Target)
        {
            activeTextTarget = Target;
        }
        public virtual void ClearTextEntryFocusTarget(ITextEntryFocusTarget.EndFocusType endType = ITextEntryFocusTarget.EndFocusType.Cancel)
        {
            if (activeTextTarget != null)
            {
                activeTextTarget.OnEndFocus(endType);
                activeTextTarget = null;
            }
        }
        protected virtual void AppendCharacterToFocusTarget(KeyState keyState)
        {
            if (activeTextTarget != null) 
                activeTextTarget.OnNextKey(keyState);
        }
        protected virtual bool AppendKeyDownToFocusTarget(KeyState keyState)
        {
            // send functional keys on down, as some do not come in as a character
            if ( keyState.IsFunctionalKey && activeTextTarget != null ) {
                return activeTextTarget.OnNextKey(keyState);
            }
            return false;
        }





        public virtual void OnCharacter(KeyState keyState)
        {
            if (activeTextTarget == null) return;

            if (keyState.IsCharacterKey) {
                AppendCharacterToFocusTarget(keyState);
            }
            else if (keyState.KeyName == KeyNames.Escape) {
                if (activeTextTarget.TryHandleTextEntryHotkey(keyState) == false)
                {
                    activeTextTarget.OnEndFocus(ITextEntryFocusTarget.EndFocusType.Cancel);
                    activeTextTarget = null;
                }
            } else if ( keyState.KeyName == KeyNames.Enter) {
                if (activeTextTarget.TryHandleTextEntryHotkey(keyState) == false)
                {
                    activeTextTarget.OnEndFocus(ITextEntryFocusTarget.EndFocusType.Commit);
                    activeTextTarget = null;
                }
            } else {
                // key is not a character...cannot append to string so we will just ignore it
                //AppendCharacterToFocusTarget(keyState);
            }
        }


        public bool AnyKeysDown { get { return ActivePressedKeys.Count > 0; } }


        // currently this is called both on window focus change and when a click-capture happens
        public virtual void OnChangeWindowFocus(bool bCommitActiveEdit = true)
        {
            ClearTextEntryFocusTarget(
                (bCommitActiveEdit) ? ITextEntryFocusTarget.EndFocusType.Commit : ITextEntryFocusTarget.EndFocusType.Cancel );
            if ( ActivePressedKeys.Count > 0 )
                ActivePressedKeys.Clear();

            bWaitForAllKeysUpPending = false;
        }



        protected string LastClipboardText = "";
        public virtual void SetCurrentSystemClipboardText(string Text)
        {
            LastClipboardText = Text;
        }

    }






    public sealed class SystemKeyboardRouter
    {
        private static readonly KeyboardRouter instance = new KeyboardRouter();
        static SystemKeyboardRouter() { }
        private SystemKeyboardRouter() { }
        public static KeyboardRouter Instance {
            get {
                return instance;
            }
        }
    }


}
