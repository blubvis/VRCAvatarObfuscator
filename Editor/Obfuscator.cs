using System;
using System.Text;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using System.Runtime.Remoting.Messaging;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace BlubvisHroi.VRC.Obfuscator
{
    public class Obfuscator
    {
        string avatarAssetFolder; 
        string beeMovie1 = "\t\t\t According to all known laws of aviation";
        string beeMovie2 = "there is no way a bee should be able to fly; Its wings are too small to get its fat little body off the ground; The bee, of course, flies anyway because bees don't care what humans think is impossible";
        string[] standardParameters = {"VRCEmote","VRCEmoteSpeed","IsLocal", "Viseme", "Voice", "GestureLeft", "GestureRight", "GestureLeftWeight", "GestureRightWeight", "AngularY", "VelocityX", "VelocityY", "VelocityZ", "VelocityMagnitude", "Upright", "Grounded", "Seated", "AFK", "Expression1", "Expression2", "Expression3", "Expression4", "Expression5", "Expression6", "Expression7", "Expression8", "Expression9", "Expression10", "Expression11", "Expression12", "Expression13", "Expression14", "Expression15", "Expression16", "TrackingType", "VRMode", "MuteSelf", "InStation", "Earmuffs", "IsOnFriendsList", "AvatarVersionK"};
        string[] rickroll = {"Never", "gonna", "give", "you", "up", "Never", "gonna","let","you","down","Never","gonna","run","around","and","desert","you","Never","gonna","make","you","cry","Never","gonna","say","goodbye","Never","gonna","tell","a","lie","and","hurt","you"};
        string[] physBoneParamEndings = { "_IsGrabbed", "_IsPosed", "_Angle", "_Stretch", "_Squish" };
        AvatarAssetFolder folder = null;

        public void Obfuscate(VRCAvatarDescriptor descriptor)
        {
            folder = AvatarAssetFolder.FromAvatarName(descriptor.gameObject.name);
            
            var clonedAvatar = CloneAvatar(descriptor);
            descriptor = clonedAvatar.GetComponent<VRCAvatarDescriptor>();
            AnimatorController animator = Functions.FindAnimator(descriptor, VRCAvatarDescriptor.AnimLayerType.FX);
            var newNameDict = GetNewParamNames(animator);
            // PrintNewNameDict(newNameDict);
            ScrambleExpressionParameters(newNameDict,descriptor);
            ScrambleMenu(newNameDict, descriptor.expressionsMenu);
            ScrambleAvatarComponents(newNameDict, ImmutableList<string>.Empty, descriptor.transform);
            ScrambleAnimationNames(descriptor);
            ScrambleAnimator(descriptor, newNameDict);

        }
        public void PrintNewNameDict(Dictionary<string, string> newNameDict)
        {
            Debug.Log("Printing new name dict.");
            foreach (var (key, value) in newNameDict)
            {
                Debug.Log("Key: \"" + key + "\"; Value: \"" + value + "\"");
            }
            Debug.Log("Done printing.");
        }
        GameObject CloneAvatar(VRCAvatarDescriptor descriptor)
        {
            GameObject clonedAvatar = UnityEngine.Object.Instantiate(descriptor.gameObject);
            VRCAvatarDescriptor clonedDescriptor = clonedAvatar.GetComponent<VRCAvatarDescriptor>();
            for(int i = 0; i < clonedDescriptor.baseAnimationLayers.Length; i++)
            {
                AnimatorController animator = clonedDescriptor.baseAnimationLayers[i].animatorController as AnimatorController;
                if(animator == null )
                {
                    Debug.Log(i);
                    continue;
                    
                }
                AnimatorController ClonedController = folder.CopyGenericAsset(animator);
                clonedDescriptor.baseAnimationLayers[i].animatorController=ClonedController;
            }
            VRCExpressionParameters clonedParameters = folder.CopyGenericAsset(clonedDescriptor.expressionParameters);
            clonedDescriptor.expressionParameters=clonedParameters;
            clonedDescriptor.expressionsMenu = CloneRecursiveMenu(descriptor.expressionsMenu);
            return clonedAvatar;
        }
        void ScrambleAnimator(VRCAvatarDescriptor descriptor,Dictionary<string, string> newNameDict)
        {
            for(int i = 0; i < descriptor.baseAnimationLayers.Length; i++)
            {
                AnimatorController animator = descriptor.baseAnimationLayers[i].animatorController as AnimatorController;
                if(animator == null )
                {
                    Debug.Log(i);
                    continue;
                    
                }
                ScrambleAnimatorParamsFile(newNameDict, animator);
                SendToShadowRealm(animator);
            }
        }
        VRCExpressionsMenu CloneRecursiveMenu(VRCExpressionsMenu menu)
        {
            VRCExpressionsMenu clonedMenu = folder.CopyGenericAsset<VRCExpressionsMenu>(menu);
            foreach(VRCExpressionsMenu.Control control in clonedMenu.controls)
            {
                if(control.subMenu != null)
                {
                    control.subMenu = CloneRecursiveMenu(control.subMenu);
                }
            }
            return clonedMenu;
        }
        
        static string MakeRandomStringModifications(System.Random random, int numModifications, string str) {
            string allChars = "abcdefghijklmnopqrstuvxyz ,.";

            var sb = new System.Text.StringBuilder(str);
            for (int i = 0; i < numModifications; i++)
            {
                int modificationPlace = random.Next(0, str.Length);
                char newChar = allChars[random.Next(0, allChars.Length)];
                bool isUpper = char.IsUpper(str[modificationPlace]);
                if (isUpper) newChar = char.ToUpper(newChar);
                else newChar = char.ToLower(newChar);
                sb[modificationPlace] = newChar;
            }
            return sb.ToString();
        }
        string GetNewNameNotInList(System.Random random, List<string> list)
        {
            string newName = "";
            // Make sure we don't make a duplicate name
            do {
                newName = beeMovie1 + " " + MakeRandomStringModifications(random, 2, beeMovie2);
            } while (list.Contains(newName));
            return newName;
        }
        //finds all the parameters in the FX layer
        Dictionary<string, string> GetNewParamNames(AnimatorController animator)
        {
            var oldNames = new List<string>();
            var newNames = new List<string>();
            var physBoneNames = new List<(string, string)>();
            var random = new System.Random();
            for (int i = 0; i < animator.parameters.Length; i++)
            {
                var parameter = animator.parameters[i];

                // If it is a standard VRC parameter it can't be scrambled
                if(standardParameters.Contains(parameter.name)) {
                    oldNames.Add(parameter.name);
                    newNames.Add(parameter.name);
                    continue;
                }

                string newName = GetNewNameNotInList(random, newNames);
                
                // This parameter is a physbone thing, can't be scrambled yet
                var (withoutEnding, ending) = Functions.RemoveEndings(physBoneParamEndings, parameter.name);
                if (ending != null) {
                    if (!oldNames.Contains(withoutEnding))
                    {
                        oldNames.Add(withoutEnding);
                        newNames.Add(newName);
                    }
                    physBoneNames.Add((withoutEnding, ending));
                    continue;
                }

                // We can generate a new name for it
                oldNames.Add(parameter.name);
                newNames.Add(newName);
            }

            // Now we can safely scramble the physbone parameters
            var newNameDict = oldNames.Zip(newNames, (k, v) => new { k, v }).ToDictionary(x => x.k, x => x.v);
            foreach (var (withoutEnding, ending) in physBoneNames)
            {
                string newName = Functions.GetName(newNameDict, withoutEnding, "ScrambleParams");
                if (newName == null) continue;

                newNameDict.Add(withoutEnding + ending, newName + ending);
            }

            return newNameDict;
        }
        static string YAMLEscapeString(string str)
        {
            var sb = new StringBuilder(str.Length);
            bool needsEscaping = false;
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (!char.IsLetter(c) || c < 128) needsEscaping = true;
                if (c == '"') sb.Append('\\');
                sb.Append(c);
            }

            if (!needsEscaping) return sb.ToString();
            else return "\"" + sb.ToString() + "\"";
        }
        void ScrambleAnimatorParamsFile(Dictionary<string, string> newNameDict, AnimatorController animator)
        {
            var path = AssetDatabase.GetAssetPath(animator);
            // Substring 7 removes the Assets/ which is also in Application.dataPath
            string filename = $"{Application.dataPath}/{path.Substring(7)}";
            StringBuilder result = new StringBuilder();
            Debug.Log($"path: {filename}");
    
            using (StreamReader streamReader = new StreamReader(filename))
            {
                String line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    var newLine = line;

                    if (newLine.StartsWith("---") || newLine.StartsWith("%")) {
                        // Handing for non-YAML Unity things
                    } else if (newLine.EndsWith(":")) {
                        // Handling for parent key of next lines
                    } else {
                        // Handling for regular key value pairs
                        char[] trimChars = {' ', '-'};
                        int splitLoc = newLine.IndexOf(':');
                        string beforeSeperator = newLine.Substring(0, splitLoc);
                        string key = beforeSeperator.Trim(trimChars);
                        string value = newLine.Substring(splitLoc + 1).Trim(trimChars);
                        string newValue;
                        if (newNameDict.TryGetValue(value, out newValue))
                        {
                            newLine = $"{beforeSeperator}: " + YAMLEscapeString(newValue);
                        }
                    }

                    result.Append(newLine + Environment.NewLine);
                }
            }
        
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename))
            {
                Debug.Log("Writing back the YAML file.");
                file.WriteLine(result.ToString());
                file.Close();
            }

            // Tell Unity I did the thing
            EditorUtility.SetDirty(animator);
            AssetDatabase.Refresh();
        }

        void ScrambleAvatarComponents(Dictionary<string, string> newNameDict, ImmutableList<string> currPath, Transform transform) 
        {
            currPath = currPath.Add(transform.name);

            // Rename the contact receives on the current game object
            var contactReceivers = transform.gameObject.GetComponents<VRCContactReceiver>();
            foreach (var contactReceiver in contactReceivers)
            {
                var path = string.Join(" -> ", currPath) + " -> contact receiver";
                string newName = Functions.GetName(newNameDict, contactReceiver.parameter, path);
                if (newName == null) continue;
                contactReceiver.parameter = newName;
            }

            // Rename the physbones, handle the parameters they automatically generate like *_IsGrounded
            VRCPhysBone[] physBones = transform.gameObject.GetComponents<VRCPhysBone>();
            foreach (VRCPhysBone physBone in physBones)
            {
                var path = string.Join(" -> ", currPath) + " -> phys bone";
                if (physBone.parameter == "") continue;
                string newName = Functions.GetName(newNameDict, physBone.parameter, path);
                if (newName == null) continue;
                physBone.parameter = newName;
            }

            // Recurse through all the child GameObjects
            for (int i = 0; i < transform.childCount; i++)
            {
                ScrambleAvatarComponents(newNameDict, currPath, transform.GetChild(i));
            }
        }
        void ScrambleExpressionParameters(Dictionary<string, string> newNameDict, VRCAvatarDescriptor descriptor) 
        {
            var expressionParameters = descriptor.expressionParameters;
            
            foreach(var parameter in expressionParameters.parameters)
            {
                string newName = Functions.GetName(newNameDict, parameter.name, "expression parameters");
                if (newName == null) continue;
                parameter.name = newName;
            }
        }
        void ScrambleMenu(Dictionary<string, string> newNameDict, VRCExpressionsMenu expressionmenu)
        {
            foreach(var control in expressionmenu.controls)
            {
                if(control.subMenu != null)
                {
                    
                    ScrambleMenu(newNameDict, control.subMenu);
                    Debug.Log("renamed 1 submenu");
                }
                if(control.parameter.name != "")
                {
                    string newParam = Functions.GetName(newNameDict, control.parameter.name, "in menu");
                    
                    if (newParam == null) continue;
                    control.parameter.name = newParam;
                    Debug.Log("renamed 1 submenu parameter");
                }
                for(int i =0; i < control.subParameters.Length; i++)
                {
                    string newParam = Functions.GetName(newNameDict, control.subParameters[i].name, "in menu");
                    if (newParam == null) continue;
                    control.subParameters[i].name = newParam;
                    Debug.Log("renamed 1 submenu parameter");
                }
                   
                

            }
        }
        Motion ScrambleMotion(Motion motion)
        {
            BlendTree blendTree = motion as BlendTree;

            if (motion == null) {
                return null;
            } else if (blendTree != null) {
                var blendTreeChildren = blendTree.children;
                for (int i = 0; i < blendTreeChildren.Length; i++)
                {
                    blendTreeChildren[i].motion = ScrambleMotion(blendTreeChildren[i].motion);
                }
                blendTree.children = blendTreeChildren;
                return blendTree;
            } else {
                AnimationClip oldAnimation = motion as AnimationClip;
                if (oldAnimation == null) {
                    Debug.LogWarning("Unknown motion found: \"" + motion.name + "\"");
                    return motion;
                }
                AnimationClip animation = folder.CopyGenericAsset(oldAnimation);
                return animation;
            }
        }
        void ScrambleAnimationNames(VRCAvatarDescriptor descriptor)
        {
            for(int i = 0; i < descriptor.baseAnimationLayers.Length; i++)
            {   
                AnimatorController animator = descriptor.baseAnimationLayers[i].animatorController as AnimatorController;
                if(animator == null )
                {
                    continue;
                }
                foreach (var layer in animator.layers)
                {
                    ChildAnimatorState[] states = layer.stateMachine.states;
                    
                    for (int j = 0; j < states.Length; j++)
                    {   
                        states[j].state.motion = ScrambleMotion(states[j].state.motion);
                    }
                }
            }
        }
        
        void SendToShadowRealm(AnimatorController animator)
        {
            int randomizationDistance = 999999;
            AnimatorControllerLayer[] layers = animator.layers;
            for (int j = 0; j < animator.layers.Length; j++)
            {
                AnimatorControllerLayer layer = animator.layers[j];
                layers[j].name = "\t\t\t" + rickroll[j % rickroll.Length];
                
                var random = new System.Random();
                int y = random.Next(-randomizationDistance,randomizationDistance);
                int x = random.Next(-randomizationDistance,randomizationDistance);

                Vector3 shadowRealm = new Vector3(x, y);
                layer.stateMachine.entryPosition = shadowRealm;
                layer.stateMachine.anyStatePosition = shadowRealm;
                layer.stateMachine.exitPosition = shadowRealm;

                ChildAnimatorState[] childStates = layer.stateMachine.states;
                for (int i = 0; i < layer.stateMachine.states.Length; i++)
                {
                    y = random.Next(-randomizationDistance,randomizationDistance);
                    x = random.Next(-randomizationDistance,randomizationDistance);
                    shadowRealm = new Vector3(x, y);
                    childStates[i].position = shadowRealm;

                    childStates[i].state.name = i.ToString();
                }

                layer.stateMachine.states = childStates;
            }
            animator.layers = layers;
        }
    }
}