using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.UI
{
    public class StringEditor
    {
        public string InitialString { get; private set; }
        public string CurrentString { get; private set; }

        public int CursorLocation { get; private set; }

        public int SelectionStartLocation { get; private set; }
        public int SelectionEndLocation { get; private set; }
        public bool HasSelection { get { return SelectionStartLocation >= 0; } }


        public Predicate<char> CharacterFilterFunc = (c) => true;
        public Predicate<string> StringFilterFunc = (s) => true;

        public delegate void StringEditedEventHandler(StringEditor editor, string newString);
        //! this delegate will fire every time the CurrentString is modified
        public event StringEditedEventHandler? OnStringEditUpdate;

        public StringEditor(string initialString)
        {
            InitialString = initialString;
            CurrentString = InitialString;
            CursorLocation = CurrentString.Length;
            SelectionStartLocation = SelectionEndLocation = -1;
        }


        public void ConfigureForInteger()
        {
            CharacterFilterFunc = StringValidators.IsIntegerCharacter;
            StringFilterFunc = StringValidators.IsIntegerString;
        }
        public void ConfigureForReal()
        {
            CharacterFilterFunc = StringValidators.IsRealCharacter;
            StringFilterFunc = StringValidators.IsRealString_TextEntry;
        }



        public bool OnNextKey(KeyState keyState)
        {
            if (keyState.KeyType == KeyType.CharacterKey)
            {
                OnTryInsertCharacter(keyState);
                return true;
            }

            switch (keyState.KeyName)
            {
                case KeyNames.LeftArrow:
                    if ( keyState.bShiftDown )
                    {
                        if ( HasSelection )
                        {
                            if (CursorLocation > SelectionStartLocation)
                            {
                                SelectionEndLocation -= 1;
                                CursorLocation -= 1;
                            }
                            else if (CursorLocation > 0)
                            {
                                SelectionStartLocation -= 1;
                                CursorLocation -= 1;
                            }
                            if (SelectionStartLocation == SelectionEndLocation)
                                ClearSelection();
                        }
                        else if ( CursorLocation > 0 )
                        {
                            SelectionEndLocation = CursorLocation;
                            SelectionStartLocation = SelectionEndLocation - 1;
                            CursorLocation -= 1;
                        }
                    }
                    else
                    {
                        ClearSelection();
                        CursorLocation = Math.Max(0, CursorLocation - 1);
                    }
                    return true;

                case KeyNames.RightArrow:
                    if (keyState.bShiftDown)
                    {
                        if (HasSelection)
                        {
                            if ( CursorLocation < SelectionEndLocation )
                            {
                                SelectionStartLocation += 1;
                                CursorLocation += 1;
                            }
                            else if ( CursorLocation < CurrentString.Length )
                            {
                                SelectionEndLocation += 1;
                                CursorLocation += 1;
                            }
                            if (SelectionStartLocation == SelectionEndLocation)
                                ClearSelection();
                        }
                        else if (CursorLocation < CurrentString.Length)
                        {
                            SelectionStartLocation = CursorLocation;
                            SelectionEndLocation = SelectionStartLocation + 1;
                            CursorLocation += 1;
                        }
                    }
                    else
                    {
                        ClearSelection();
                        CursorLocation = Math.Min(CurrentString.Length, CursorLocation + 1);
                    }
                    return true;


                case KeyNames.Backspace:
                    if (HasSelection)
                    {
                        DeleteSelection();
                    }
                    else if (CursorLocation > 0)
                    {
                        CurrentString = CurrentString.Remove(CursorLocation - 1, 1);
                        CursorLocation--;
                        OnStringEditUpdate?.Invoke(this, CurrentString);
                    }
                    return true;

                case KeyNames.Delete:
                    if ( HasSelection)
                    {
                        DeleteSelection();
                    }
                    else if (CursorLocation < CurrentString.Length)
                    {
                        CurrentString = CurrentString.Remove(CursorLocation, 1);
                        CursorLocation = Math.Min(CursorLocation, CurrentString.Length);
                        OnStringEditUpdate?.Invoke(this, CurrentString);
                    }
                    return true;
            }
            return false;
        }


        public void TryPaste(string PastedString)
        {
            string TempString = CurrentString;

            int NewCursorLocation = -1;
            int NumSelected = SelectionEndLocation - SelectionStartLocation;
            if (NumSelected == 0)
            {
                TempString = TempString.Insert(CursorLocation, PastedString);
                NewCursorLocation += PastedString.Length;
            }
            else
            {
                TempString = TempString.Remove(SelectionStartLocation, NumSelected);
                NewCursorLocation = SelectionStartLocation;
                TempString = TempString.Insert(NewCursorLocation, PastedString);
            }

            if (TryUpdateString(TempString))
            {
                CursorLocation = NewCursorLocation;
                ClearSelection();
            }
        }


        protected bool TryUpdateString(string newString)
        {
            bool bOK = StringFilterFunc(newString);
            if ( bOK )
            {
                CurrentString = newString;
                OnStringEditUpdate?.Invoke(this, CurrentString);
                return true;
            }
            return false;
        }

        
        protected void OnTryInsertCharacter(KeyState keyState)
        {
            if (CharacterFilterFunc(keyState.Character) == false)
                return;

            if (HasSelection)
            {
                int NumSelected = SelectionEndLocation - SelectionStartLocation;
                string NewString = CurrentString.Remove(SelectionStartLocation, NumSelected);
                NewString = NewString.Insert(SelectionStartLocation, keyState.Character.ToString());
                if (TryUpdateString(NewString))
                {
                    CursorLocation = SelectionStartLocation + 1;
                    ClearSelection();
                }
            }
            else
            {
                string NewString = CurrentString.Insert(CursorLocation, keyState.Character.ToString());
                if (TryUpdateString(NewString))
                    CursorLocation++;
            }
        }

        public void SelectAll()
        {
            SelectionStartLocation = 0;
            SelectionEndLocation = CurrentString.Length;
            CursorLocation = SelectionEndLocation;
        }

        public void ClearSelection()
        {
            SelectionStartLocation = SelectionEndLocation = -1;
        }

        public void DeleteSelection()
        {
            int NumSelected = SelectionEndLocation - SelectionStartLocation;
            if (NumSelected == 0) return;
            CurrentString = CurrentString.Remove(SelectionStartLocation, NumSelected);
            CursorLocation = SelectionStartLocation;
            OnStringEditUpdate?.Invoke(this, CurrentString);
            ClearSelection();
        }

        public bool GetCurrentSelection(out string selected)
        {
            int NumSelected = SelectionEndLocation - SelectionStartLocation;
            if (NumSelected == 0) { selected = string.Empty; return false; }
            selected = CurrentString.Substring(SelectionStartLocation, NumSelected);
            return true;
        }

        public void ResetEdit()
        {
            CurrentString = InitialString;
            CursorLocation = CurrentString.Length;
            OnStringEditUpdate?.Invoke(this, CurrentString);
        }


        public void SetCursorLocation(int Index)
        {
			ClearSelection();
			CursorLocation = Math.Clamp(Index, 0, CurrentString.Length);
        }

        //! cursor will be placed at ToIndex - range can be inverted
        public void SetSelectionRange(int FromIndex, int ToIndex)
        {
			ClearSelection();
			CursorLocation = Math.Clamp(ToIndex, 0, CurrentString.Length);
			SelectionStartLocation = Math.Clamp(Math.Min(FromIndex, ToIndex), 0, CurrentString.Length);
			SelectionEndLocation = Math.Clamp(Math.Max(FromIndex, ToIndex), 0, CurrentString.Length);
        }

    }
}
