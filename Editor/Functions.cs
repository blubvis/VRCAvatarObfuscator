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

namespace BlubvisHroi.VRC.Obfuscator 
{
    public static class Functions
    {
        public static AnimatorController FindAnimator(VRCAvatarDescriptor descriptor, VRCAvatarDescriptor.AnimLayerType type)
        {
            foreach (var layer in descriptor.baseAnimationLayers)
            {
                if (layer.type == type) {
                    return (AnimatorController)layer.animatorController;
                }
            }

            return null;
        }
    
        public static Transform FindGrandChild(Transform parent, string name) 
        {
            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(parent);
            while (queue.Count > 0)
            {
                Transform c = queue.Dequeue();
                if (c.name == name)
                    return c;
                foreach(Transform t in c)
                    queue.Enqueue(t);
            }
            return null;
        }
        public static Transform FindBone(VRCAvatarDescriptor descriptor,string name)
        {
            GameObject avatar = descriptor.gameObject;
            
            Avatar AvatarHumanoid = descriptor.GetComponent<Animator>().avatar;
            HumanBone[] skeleton = AvatarHumanoid.humanDescription.human;
            string targetBoneName = null;
            Transform foundBone = null;
            foreach(HumanBone bone in skeleton)
            {   
                
                if(bone.humanName.Equals(name)== true)
                {
                    targetBoneName = bone.boneName;
                }
            }
            if(targetBoneName != null)
            {
                foundBone = FindGrandChild(avatar.transform.Find("Armature").transform, targetBoneName);
            }
            else
            {
                Debug.LogWarning("no bone for "+name+" found");
            }
            return foundBone;
        }
        public static AnimatorController CopyAnimator(AvatarAssetFolder folder, VRCAvatarDescriptor descriptor, VRCAvatarDescriptor.AnimLayerType type)
        {
            AnimatorController animator = Functions.FindAnimator(descriptor, type);
            var newAnimator = folder.CopyGenericAsset(animator);

            // Assign the new animator to the avatar descriptor
            for (int i = 0; i < descriptor.baseAnimationLayers.Length; i++)
            {
                if (descriptor.baseAnimationLayers[i].type == type) {
                    descriptor.baseAnimationLayers[i].animatorController = newAnimator;
                }
            }
            return newAnimator;
        }

        public static AnimatorState GetLayerState(AnimatorControllerLayer layer, string name)
        {
            foreach (var childState in layer.stateMachine.states)
            {
                if (childState.state.name == name)
                {
                    return childState.state;
                }
            }
            return null;
        }

        public static (string, string) RemoveEndings(string[] endings, string str)
        {
            foreach (var ending in endings)
            {
                if (str.EndsWith(ending))
                {
                    var withoutEnding = str.Remove(str.Length - ending.Length);
                    Debug.Log($"Actually found something!{withoutEnding}-{ending}");
                    return (withoutEnding, ending);
                }
            }

            return (str, null);
        }

        public static string GetName(Dictionary<string, string> dict, string name, string location)
        {
            string value;
            if (dict.TryGetValue(name, out value))
            {
                return value;
            } else {
                Debug.LogWarning($"Failed to rename \"{location} -> " + name + "\". No such parameter found.");
                return null;
            }
        }
    }
}