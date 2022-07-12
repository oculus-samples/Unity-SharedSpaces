// Copyright (c) Facebook, Inc. and its affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-SharedSpaces/tree/main/Assets/SharedSpaces/LICENSE

using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SharedSpacesLog : MonoBehaviour
{
    private const int MAX_CONSOLE_LINES = 35;
    private const float MAX_CONSOLE_WIDTH = 800.0f;
    private const float MAX_WORD_WIDTH = MAX_CONSOLE_WIDTH - 115.0f;

    private static uint logID = 0;
    private static Queue<string> consoleLines = new Queue<string>(MAX_CONSOLE_LINES);
    private static Dictionary<LogType, string> logType = new Dictionary<LogType, string>
    {
        { LogType.Error,     "ERR| " },
        { LogType.Assert,    "AST| " },
        { LogType.Warning,   "WAR| " },
        { LogType.Log,       "DBG| " },
        { LogType.Exception, "EXC| " }
    };

    private SharedSpacesDebuggingPanel debuggingPanel;
    private TMP_Text logLine;

    private void Awake()
    {
        DontDestroyOnLoad(this);
        logLine = GetComponent<TMP_Text>();

        // if true, add empty strings for each line, to mimic behavior of console
        // filling from the bottom up.
        if (consoleLines.Count == 0)
        {
            for (int i = 0; i < MAX_CONSOLE_LINES; ++i)
            {
                consoleLines.Enqueue("");
            }
        }

        Application.logMessageReceived += LogCallback;
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= LogCallback;
    }

    public void SetDebuggingPanel(SharedSpacesDebuggingPanel debuggingPanel_)
    {
        debuggingPanel = debuggingPanel_;
        PrintConsoleLog();
    }

    private void AddLogToConsole(string message, LogType type)
    {
        message = message.Trim();
        message = message.Trim('\n');

        string id = logID.ToString("0000");

        message = id + "> " + logType[type] + message;

        // each line should be added to the console
        foreach (string line in message.Split('\n'))
        {
            string temp = "";

            // check if a word overflows the line
            foreach (string word in line.Split(' '))
            {
                temp += word + " ";
                logLine.text = word;

                // if true, need to wrap on character level because a single word in 
                // the current log is wider than the widest allowed word
                if (logLine.preferredWidth > MAX_WORD_WIDTH)
                {
                    logLine.text = "";

                    for (int i = 0; i < temp.Length; ++i)
                    {
                        logLine.text += temp[i];
                        if (logLine.preferredWidth > MAX_CONSOLE_WIDTH)
                        {
                            AddConsoleLine(logLine.text.Substring(0, logLine.text.Length - 1));
                            logLine.text = temp[i].ToString();
                        }
                    }
                }
                // check if we need to wrap on word level
                else
                {
                    logLine.text = temp;
                    //logLine.text += word + " ";

                    // if true, need to wrap on word level because the current log
                    // is wider than the widest allowed log
                    if (logLine.preferredWidth > MAX_CONSOLE_WIDTH)
                    {
                        AddConsoleLine(logLine.text.Substring(0, logLine.text.Length - word.Length - 1));
                        temp = word + " ";
                    }
                }
            }

            if (logLine.preferredWidth <= MAX_CONSOLE_WIDTH)
            {
                AddConsoleLine(logLine.text);
            }
        }
    }

    private void AddConsoleLine(string consoleLine)
    {
        if (consoleLines.Count == MAX_CONSOLE_LINES)
            consoleLines.Dequeue();

        consoleLines.Enqueue(consoleLine);
    }

    private void PrintConsoleLog()
    {
        if (!debuggingPanel) return;

        debuggingPanel.consoleLog.text = "";

        foreach (string line in consoleLines)
        {
            debuggingPanel.consoleLog.text += line + "\n";
        }
    }

    private void LogCallback(string condition, string stackTrace, LogType type)
    {
        AddLogToConsole(condition, type);
        PrintConsoleLog();
        ++logID;
    }
}
