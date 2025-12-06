using System;
using System.Collections.Generic;
using System.Text;

namespace Usemam.Ledger.CommandLine;

public class InputEditor
{
    private readonly List<string> _commandHistory;
    private readonly List<string> _autocompleteWords;
    private readonly int _maxHistorySize;
    private readonly StringBuilder _currentInput = new();
    private int _historyIndex;
    private int _caretPosition = 0;
    private List<string> _autocompleteMatches = new();
    private int _autocompleteIndex = 0;
    private int _autocompleteStart = -1;


    public InputEditor(
        List<string> sharedHistory,
        List<string> autocompleteWords,
        int maxHistorySize = 100)
    {
        _commandHistory = sharedHistory;
        _autocompleteWords = autocompleteWords;
        _maxHistorySize = maxHistorySize;
        _historyIndex = _commandHistory.Count;
    }

    public string ReadLine()
    {
        _currentInput.Clear();
        _caretPosition = 0;
        _historyIndex = _commandHistory.Count;

        RenderLine();

        while (true)
        {
            var keyInfo = Console.ReadKey(intercept: true);
            bool isCtrl = (keyInfo.Modifiers & ConsoleModifiers.Control) != 0;

            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    string result = _currentInput.ToString();
                    AddToHistory(result);
                    return result;

                case ConsoleKey.Backspace:
                    if (isCtrl)
                        DeletePreviousWord();
                    else
                        HandleBackspace();
                    break;

                case ConsoleKey.LeftArrow:
                    MoveCaretLeft();
                    break;

                case ConsoleKey.RightArrow:
                    MoveCaretRight();
                    break;

                case ConsoleKey.UpArrow:
                    RecallPreviousCommand();
                    break;

                case ConsoleKey.DownArrow:
                    RecallNextCommand();
                    break;
                
                case ConsoleKey.Tab:
                    HandleAutocomplete();
                    break;

                default:
                    if (!char.IsControl(keyInfo.KeyChar))
                    {
                        InsertCharacter(keyInfo.KeyChar);
                    }
                    break;
            }

            RenderLine();
        }
    }

    private void AddToHistory(string command)
    {
        // Add non-empty entries to history
        if (!string.IsNullOrWhiteSpace(command))
        {
            // Only add if it's not a duplicate of the last command
            if (_commandHistory.Count == 0 || !string.Equals(_commandHistory[^1], command, StringComparison.OrdinalIgnoreCase))
            {
                _commandHistory.Add(command);
                // Trim history to max size
                if (_commandHistory.Count > _maxHistorySize)
                {
                    _commandHistory.RemoveAt(0); // remove oldest
                }
            }

            _historyIndex = _commandHistory.Count;
        }
    }
    
    private void HandleAutocomplete()
    {
        if (_autocompleteMatches.Count == 0)
        {
            // First Tab press: find word to the left of caret
            int wordStart = _caretPosition - 1;

            while (wordStart >= 0 && !IsAutocompleteBoundary(_currentInput[wordStart]))
                wordStart--;

            wordStart++; // move to start of word

            int length = _caretPosition - wordStart;
            if (length <= 0) return;

            string currentWord = _currentInput.ToString(wordStart, length);

            _autocompleteMatches = _autocompleteWords
                .FindAll(word => word.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase));

            _autocompleteIndex = 0;
            _autocompleteStart = wordStart;
        }

        if (_autocompleteMatches.Count > 0)
        {
            string match = _autocompleteMatches[_autocompleteIndex];

            // Replace current word
            int length = _caretPosition - _autocompleteStart;
            _currentInput.Remove(_autocompleteStart, length);
            _currentInput.Insert(_autocompleteStart, match);
            _caretPosition = _autocompleteStart + match.Length;

            // Cycle to next suggestion for next Tab press
            _autocompleteIndex = (_autocompleteIndex + 1) % _autocompleteMatches.Count;
        }
    }
    
    private bool IsAutocompleteBoundary(char c)
    {
        return char.IsWhiteSpace(c) || c == '"';
    }
    
    private void ResetAutocomplete()
    {
        _autocompleteMatches.Clear();
        _autocompleteIndex = 0;
        _autocompleteStart = -1;
    }

    private void HandleBackspace()
    {
        ResetAutocomplete();
        if (_caretPosition > 0)
        {
            _currentInput.Remove(_caretPosition - 1, 1);
            _caretPosition--;
        }
    }

    private void DeletePreviousWord()
    {
        ResetAutocomplete();
        if (_caretPosition == 0)
            return;

        int wordStart = _caretPosition - 1;

        while (wordStart >= 0 && char.IsWhiteSpace(_currentInput[wordStart]))
            wordStart--;

        while (wordStart >= 0 && !char.IsWhiteSpace(_currentInput[wordStart]))
            wordStart--;

        int deleteFrom = wordStart + 1;
        int deleteLength = _caretPosition - deleteFrom;

        if (deleteLength > 0)
        {
            _currentInput.Remove(deleteFrom, deleteLength);
            _caretPosition = deleteFrom;
        }
    }

    private void MoveCaretLeft()
    {
        ResetAutocomplete();
        if (_caretPosition > 0)
            _caretPosition--;
    }

    private void MoveCaretRight()
    {
        ResetAutocomplete();
        if (_caretPosition < _currentInput.Length)
            _caretPosition++;
    }

    private void RecallPreviousCommand()
    {
        ResetAutocomplete();
        if (_commandHistory.Count == 0 || _historyIndex <= 0)
            return;

        _historyIndex--;
        LoadFromHistory();
    }

    private void RecallNextCommand()
    {
        ResetAutocomplete();
        if (_commandHistory.Count == 0 || _historyIndex >= _commandHistory.Count)
            return;

        _historyIndex++;
        LoadFromHistoryOrClear();
    }

    private void LoadFromHistory()
    {
        if (_historyIndex >= 0 && _historyIndex < _commandHistory.Count)
        {
            _currentInput.Clear();
            _currentInput.Append(_commandHistory[_historyIndex]);
            _caretPosition = _currentInput.Length;
        }
    }

    private void LoadFromHistoryOrClear()
    {
        if (_historyIndex < _commandHistory.Count)
        {
            LoadFromHistory();
        }
        else
        {
            _currentInput.Clear();
            _caretPosition = 0;
        }
    }

    private void InsertCharacter(char character)
    {
        ResetAutocomplete();
        _currentInput.Insert(_caretPosition, character);
        _caretPosition++;
        _historyIndex = _commandHistory.Count;
    }

    private void RenderLine()
    {
        int top = Console.CursorTop;
        Console.SetCursorPosition(0, top);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, top);

        // Draw the prompt
        Console.Write("> ");

        // Write input with caret
        for (int i = 0; i < _currentInput.Length; i++)
        {
            if (i == _caretPosition)
                Console.Write("_");
            Console.Write(_currentInput[i]);
        }

        // If caret is at end, draw it
        if (_caretPosition == _currentInput.Length)
            Console.Write("_");

        // Place cursor after prompt + caret position
        Console.SetCursorPosition(2 + _caretPosition, top);
    }
}
