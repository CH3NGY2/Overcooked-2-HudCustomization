using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace OC2HudCustomization
{
    internal sealed class RecipeAccess
    {
        internal readonly FieldInfo Active = Field("m_widgets", typeof(List<RecipeFlowGUI.RecipeWidgetData>));
        internal readonly FieldInfo Dying = Field("m_dyingWidgets", typeof(List<RecipeFlowGUI.RecipeWidgetData>));
        internal readonly FieldInfo Ordered = Field("m_ordererWidgets", typeof(List<RecipeFlowGUI.RecipeWidgetData>));
        internal readonly FieldInfo Gap = Field("m_distanceBetweenOrders", typeof(float));
        internal readonly FieldInfo Margin = Field("m_distanceFromEndOfScreen", typeof(float));
        internal readonly FieldInfo Tiles = Required.Field(typeof(RecipeWidgetUIController), "m_recipeTiles", typeof(List<RecipeWidgetTile>));
        internal readonly FieldInfo Container = Required.Field(typeof(UISubElementContainer), "m_container", typeof(GameObject));
        internal readonly FieldInfo HeadPaper = Required.Field(typeof(RecipeWidgetTile), "m_background", typeof(Image));
        internal readonly FieldInfo TopPaper = Required.Field(typeof(RecipeWidgetTile), "m_backgroundTop", typeof(Image));
        private static FieldInfo Field(string name, Type type) { return Required.Field(typeof(RecipeFlowGUI), name, type); }
    }

    internal sealed class ContentsAccess
    {
        internal readonly FieldInfo Instance = Required.Field(typeof(ClientIngredientContentGUI), "m_uiInstance", typeof(IngredientContentsUIContainer));
        internal readonly FieldInfo Container = Required.Field(typeof(UISubElementContainer), "m_container", typeof(GameObject));
        internal readonly FieldInfo Icons = Required.Field(typeof(IngredientContentsUIContainer), "m_icons", typeof(List<Image>));
        internal readonly FieldInfo Empty = Required.Field(typeof(IngredientContentsUIContainer), "m_emptySprite", typeof(Sprite));
    }

    internal static class Required
    {
        internal static FieldInfo Field(Type owner, string name, Type type)
        {
            FieldInfo field = AccessTools.Field(owner, name);
            if (field == null || field.FieldType != type) throw new MissingFieldException(owner.FullName, name + ": " + type);
            return field;
        }

        internal static MethodInfo Method(Type owner, string name, params Type[] parameters)
        {
            MethodInfo method = AccessTools.DeclaredMethod(owner, name, parameters);
            if (method == null) throw new MissingMethodException(owner.FullName, name);
            return method;
        }
    }
}
