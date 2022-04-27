using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(HexCoordinates))] // 将之与HexCoordinates类建立联系
public class HexCoordinatesDrawer : PropertyDrawer // 用于更改GUI界面的类
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        HexCoordinates coordinates = new HexCoordinates(
            property.FindPropertyRelative("x").intValue,
            property.FindPropertyRelative("z").intValue
        );

        position = EditorGUI.PrefixLabel(position, label); // 显示FieldName，并返回对应的position
        GUI.Label(position, coordinates.ToString());
    }
}
