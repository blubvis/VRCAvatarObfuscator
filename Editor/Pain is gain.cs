using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Dynamics.Contact.Components;
using static VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using UnityEditor.Animations;
using ICSharpCode.SharpZipLib.Core;
using Unity.XR.Oculus;
using System.Runtime.InteropServices;


namespace BlubvisHroi.VRC.Obfuscator 
{
    public class ObfuscatorWindow : EditorWindow
    {
        [MenuItem("Tools/Obfuscator")]
        public static void ShowExample()
        {
            var wnd = GetWindow<ObfuscatorWindow>();
            wnd.titleContent = new GUIContent("Obfuscator");
        }

        ObjectField objectField;
        Button obfuscateButton;

        public void CreateGUI()
        {
            var root = rootVisualElement;

            // Title
            var label = new Label("Obfuscator");
            label.style.unityTextAlign = TextAnchor.UpperCenter;
            label.style.fontSize = 18;
            label.style.marginTop = 10;
            label.style.marginBottom = 10;
            root.Add(label);

            objectField = new ObjectField();
            objectField.objectType = typeof(VRCAvatarDescriptor);
            objectField.label = "Avatar:";
            objectField.RegisterCallback((ChangeEvent<UnityEngine.Object> evt) => {
                VRCAvatarDescriptor avatarDescriptor = (VRCAvatarDescriptor)evt.newValue;
                Debug.Log("Registered callback running: " + avatarDescriptor);
                if (avatarDescriptor == null)
                    obfuscateButton.SetEnabled(false);
                else
                    obfuscateButton.SetEnabled(true);
            });
            root.Add(objectField);

            obfuscateButton = new Button();
            obfuscateButton.style.flexGrow = 1;
            obfuscateButton.name = "Obfuscate";
            obfuscateButton.text = "obfuscate";
            obfuscateButton.SetEnabled(false);
            obfuscateButton.clicked += () => new Obfuscator().Obfuscate((VRCAvatarDescriptor)objectField.value);
            root.Add(obfuscateButton);
        }
    }
}