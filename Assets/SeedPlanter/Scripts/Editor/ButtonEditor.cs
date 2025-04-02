using UnityEngine;
using System.Reflection;
using UnityEditor;

[CustomEditor(typeof(MonoBehaviour), true)] // Works on any MonoBehaviour
public class ButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI(); // Draw default inspector fields

        // Get all methods with the ButtonAttribute
        var methods = target.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var method in methods)
        {
            if (method.GetCustomAttribute<ButtonAttribute>() != null)
            {
                if (GUILayout.Button(method.Name)) // Create a button with the method name
                {
                    method.Invoke(target, null); // Execute the method
                }
            }
        }
    }
}
