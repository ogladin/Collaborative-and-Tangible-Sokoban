﻿// Copyright (c) 2015 Bartłomiej Wołk (bartlomiejwolk@gmail.com)
//
// This file is part of the FileLogger extension for Unity.
// Licensed under the MIT license. See LICENSE file in the project root folder.

#define DEBUG_LOGGER

using ATP.ReorderableList;
using UnityEditor;
using UnityEngine;

namespace FileLogger {

    [CustomEditor(typeof (Logger))]
    public class LoggerEditor : Editor {

        private Logger Script { get; set; }

        #region SERIALIZED PROPERTIES

        private SerializedProperty classFilter;
        private SerializedProperty echoToConsole;
        private SerializedProperty enableOnPlay;
        private SerializedProperty fileNamePrefix;
        private SerializedProperty fileNameSuffix;
        private SerializedProperty participant1ID;
        private SerializedProperty participant2ID;
        private SerializedProperty indentLine;
        private SerializedProperty loggingEnabled;
        private SerializedProperty logInRealTime;
        private SerializedProperty methodFilter;
        private SerializedProperty qualifiedClassName;
        private SerializedProperty append;
        private SerializedProperty clearOnPlay;

        #endregion SERIALIZED PROPERTIES

        #region UNITY MESSAGES

        public override void OnInspectorGUI() {
            serializedObject.Update();

            DrawVersionNo();
            DrawFilePathField();
            DrawParicipant1Field();
            DrawParicipant2Field();
            DrawFileSuffixField();

            EditorGUILayout.Space();

            GUILayout.Label("Logging Options", EditorStyles.boldLabel);

            DrawEnableOnPlayToggle();
            DrawClearOnPlayToggle();
            DrawLogInRealTimeToggle();
            DrawAppendToggle();
            DrawEchoToConsoleToggle();

            EditorGUILayout.Space();

            GUILayout.Label("Message Options", EditorStyles.boldLabel);

            DrawIndentLineToggle();
            DrawFullyQualifiedNameToggle();
            DrawAppendDropdown();

            EditorGUILayout.Space();

            GUILayout.Label("Filters", EditorStyles.boldLabel);
            DrawEnabledMethodsDropdown();

            EditorGUILayout.Space();

            DrawMyClassHelpBox();
            ReorderableListGUI.Title("Class Filter");
            ReorderableListGUI.ListField(classFilter);

            DrawOnEnableHelpBox();
            ReorderableListGUI.Title("Method Filter");
            ReorderableListGUI.ListField(methodFilter);

            EditorGUILayout.BeginHorizontal();
            HandleDrawingStartStopButton();
            DrawClearLogFileButton();
            EditorGUILayout.EndHorizontal();

            // Save changes
            serializedObject.ApplyModifiedProperties();
        }
        private void OnEnable() {
            Script = (Logger) target;

            fileNamePrefix = serializedObject.FindProperty("fileNamePrefix");
            participant1ID = serializedObject.FindProperty("participant1ID");
            participant2ID = serializedObject.FindProperty("participant2ID");
            fileNameSuffix = serializedObject.FindProperty("fileNameSuffix");
            logInRealTime = serializedObject.FindProperty("logInRealTime");
            echoToConsole = serializedObject.FindProperty("echoToConsole");
            loggingEnabled = serializedObject.FindProperty("loggingEnabled");
            enableOnPlay = serializedObject.FindProperty("enableOnPlay");
            serializedObject.FindProperty("appendCallerClassName");
            qualifiedClassName =
                serializedObject.FindProperty("qualifiedClassName");
            indentLine = serializedObject.FindProperty("indentLine");
            classFilter = serializedObject.FindProperty("classFilter");
            methodFilter = serializedObject.FindProperty("methodFilter");
            append = serializedObject.FindProperty("append");
            clearOnPlay = serializedObject.FindProperty("clearOnPlay");
        }

        #endregion UNITY MESSAGES

        #region INSPECTOR
        private void HandleDrawingStartStopButton() {
            loggingEnabled.boolValue =
                InspectorControls.DrawStartStopButton(
                    Script.LoggingEnabled,
                    Script.EnableOnPlay,
                    null);
        }

        private void DrawClearOnPlayToggle() {
            clearOnPlay.boolValue = EditorGUILayout.Toggle(
                new GUIContent(
                    "Clear On Play",
                    "Clear log file on enter play mode."),
                clearOnPlay.boolValue);
        }


        private void DrawAppendDropdown() {
            Script.DisplayOptions =
                (AppendOptions) EditorGUILayout.EnumFlagsField(
                    new GUIContent(
                        "Display",
                        "Additional info that should be attached to a single" +
                        "log message."),
                    Script.DisplayOptions);
        }

        private void DrawAppendToggle() {
            var disabled = logInRealTime.boolValue ? true : false;

            EditorGUI.BeginDisabledGroup(disabled);

            append.boolValue = EditorGUILayout.Toggle(
                new GUIContent(
                    "Always Append",
                    "Always append messages to the log file."),
                append.boolValue);

            EditorGUI.EndDisabledGroup();
        }

        private void DrawClearLogFileButton() {
            // Don't allow reseting log file while logging.
            if (Script.LoggingEnabled) return;

            if (GUILayout.Button(
                "Clear Log File",
                GUILayout.Width(100))) {

                Script.ClearLogFile();
            }
        }

        private void DrawEchoToConsoleToggle() {
            EditorGUILayout.PropertyField(
                echoToConsole,
                new GUIContent(
                    "Echo To Console",
                    "Echo logged messages also to the Unity's console. " +
                    "It can be really slow."));
        }

        private void DrawEnabledMethodsDropdown() {
            Script.EnabledMethods =
                (EnabledMethods) EditorGUILayout.EnumFlagsField(
                    new GUIContent(
                        "Enabled Methods",
                        "Select Logger methods that should be active. Inactive "
                        +
                        "methods won't log anything."),
                    Script.EnabledMethods);
        }

        private void DrawEnableOnPlayToggle() {
            EditorGUILayout.PropertyField(
                enableOnPlay,
                new GUIContent(
                    "Enable On Play",
                    "Start logger on enter play mode."));
        }

        private void DrawFilePathField() {

            EditorGUILayout.PropertyField(
	            fileNamePrefix,
                new GUIContent(
                    "File Prefix",
                    "File prefix for the generated log file."));
        }

        private void DrawFileSuffixField() {

	        EditorGUILayout.PropertyField(
		        fileNameSuffix,
		        new GUIContent(
			        "File extension",
			        "File extension for the generated log file."));
        }

        private void DrawParicipant1Field() {

	        EditorGUILayout.PropertyField(
		        participant1ID,
		        new GUIContent(
			        "Participant #1 ID",
			        "Participant #1 ID for the generated log file."));
        }

        private void DrawParicipant2Field() {

	        EditorGUILayout.PropertyField(
		        participant2ID,
		        new GUIContent(
			        "Participant #2 ID",
			        "Participant #2 ID for the generated log file."));
        }

        private void DrawFullyQualifiedNameToggle() {
            EditorGUILayout.PropertyField(
                qualifiedClassName,
                new GUIContent(
                    "Full Class Name",
                    "If enabled, class name will be fully qualified."));
        }

        private void DrawIndentLineToggle() {

            EditorGUILayout.PropertyField(
                indentLine,
                new GUIContent(
                    "Indent On",
                    "Indent log messages accordingly to the call stack."));
        }

        private void DrawLogInRealTimeToggle() {

            EditorGUILayout.PropertyField(
                logInRealTime,
                new GUIContent(
                    "Log In Real Time",
                    "Each log message will be written to the file " +
                    "in real time instead of when logging stops."));
        }

        private void DrawMyClassHelpBox() {

            EditorGUILayout.HelpBox(
                "Example: MyClass",
                UnityEditor.MessageType.Info);
        }

        private void DrawOnEnableHelpBox() {
            EditorGUILayout.HelpBox(
                "Example: OnEnable",
                UnityEditor.MessageType.Info);
        }
        private void DrawVersionNo() {
            EditorGUILayout.LabelField(Logger.VERSION);
        }

        #endregion INSPECTOR

        #region METHODS
        [MenuItem("Component/FileLogger")]
        private static void AddLoggerComponent() {
            if (Selection.activeGameObject != null) {
                Selection.activeGameObject.AddComponent(typeof (Logger));
            }
        }

        #endregion METHODS
    }

}
