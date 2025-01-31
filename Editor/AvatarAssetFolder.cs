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
using UnityEditor.Animations;
using ICSharpCode.SharpZipLib.Core;

namespace BlubvisHroi.VRC.Obfuscator 
{
    public class AvatarAssetFolder
    {
        private readonly static string basePath = "Assets/Obfuscator/New copies";
        private string avatarId;

        public AvatarAssetFolder()
        {
            avatarId = System.Guid.NewGuid().ToString();
        }

        public AvatarAssetFolder(string avatarId)
        {
            this.avatarId = avatarId;
        }

        public static AvatarAssetFolder FromAssetPath(string assetPath)
        {
            if (!assetPath.StartsWith(basePath)) return null;
            if (basePath.Count((c) => c == '/') >= assetPath.Count((c) => c == '/')) return null;
            var avatarIdChars = assetPath.Skip(basePath.Length).TakeWhile((c) => c != '/');
            return new AvatarAssetFolder(String.Concat(avatarIdChars));
        }

        public static AvatarAssetFolder FromAvatarName(string avatarName)
        {
            var avatarId = System.Guid.NewGuid().ToString();
            return new AvatarAssetFolder($"{avatarName}_{avatarId}");
        }

        private string GetFolderPath()
        {
            var path = basePath + "/" + avatarId;
            if (AssetDatabase.IsValidFolder(path)) return path;
            else {
                Debug.Log("Creating new folder.");
                string newFolderGuid = AssetDatabase.CreateFolder(basePath, avatarId);
                if (newFolderGuid == "") Debug.LogError("Failed to create folder: " + path);
                return path;
            }
        }

        public T CopyGenericAsset<T>(T asset) where T : UnityEngine.Object
        {
            string path = AssetDatabase.GetAssetPath(asset);
            string fileId = System.Guid.NewGuid().ToString();
            string extension = Path.GetExtension(path);
            string newPath = GetFolderPath() + "/" + fileId + extension;
            if (!AssetDatabase.CopyAsset(path, newPath))
                throw new Exception("Failed to copy Asset \"" + path + "\"!");

            var newAsset = AssetDatabase.LoadAssetAtPath<T>(newPath);
            EditorUtility.SetDirty(newAsset);
            AssetDatabase.Refresh();
            return newAsset;
        }
    }
}