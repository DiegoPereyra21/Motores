using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemData))]
public class ItemDataEditor : Editor
{
    private SerializedProperty itemIdProp;
    private SerializedProperty itemNameProp;
    private SerializedProperty iconProp;
    private SerializedProperty maxStackSizeProp;
    private SerializedProperty originProp;
    private SerializedProperty categoryProp;
    private SerializedProperty specializedDataProp;

    private void OnEnable()
    {
        itemIdProp = serializedObject.FindProperty("itemId");

        itemNameProp = serializedObject.FindProperty("itemName");

        iconProp = serializedObject.FindProperty("icon");

        maxStackSizeProp = serializedObject.FindProperty("maxStackSize");

        originProp = serializedObject.FindProperty("origin");

        categoryProp = serializedObject.FindProperty("category");

        specializedDataProp = serializedObject.FindProperty("specializedData");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawGeneralSection();
        DrawInventorySection();
        DrawClassificationSection();
        DrawSpecializedSection();

        EditorGUILayout.Space(10);
        DrawSeparator();
        EditorGUILayout.Space(10);

        DrawIdentifierValidation();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawGeneralSection()
    {
        EditorGUILayout.LabelField("INFORMACION GENERAL", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(itemIdProp);
        EditorGUILayout.PropertyField(itemNameProp);
        EditorGUILayout.PropertyField(iconProp);

        EditorGUILayout.Space(10);
    }

    private void DrawInventorySection()
    {
        EditorGUILayout.LabelField("INVENTARIO", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(maxStackSizeProp);

        EditorGUILayout.Space(10);
    }

    private void DrawClassificationSection()
    {
        EditorGUILayout.LabelField("CLASIFICACION", EditorStyles.boldLabel);

        ItemOrigin previousOrigin = (ItemOrigin)originProp.enumValueIndex;

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(originProp);

        bool originChanged = EditorGUI.EndChangeCheck();
        ItemOrigin origin = (ItemOrigin)originProp.enumValueIndex;

        if (originChanged && origin == ItemOrigin.Raw)
        {
            bool hasInvalidData =
                categoryProp.enumValueIndex != (int)ItemCategory.None || specializedDataProp.managedReferenceValue != null;

            if (hasInvalidData)
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Convertir objeto a Raw",
                    "Se eliminara la configuracion especializada y la categoria pasara a None. Esta accion no se puede deshacer desde este editor.",
                    "Continuar",
                    "Cancelar"
                );

                if (confirmed)
                {
                    categoryProp.enumValueIndex = (int)ItemCategory.None;
                    specializedDataProp.managedReferenceValue = null;
                }
                else
                {
                    originProp.enumValueIndex = (int)previousOrigin;
                    origin = previousOrigin;
                }
            }
        }

        if (origin == ItemOrigin.Raw)
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.EnumPopup("Category", ItemCategory.None);
            }
        }
        else
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(categoryProp);

            if (EditorGUI.EndChangeCheck())
            {
                CreateSpecializedData();
            }
        }

        EditorGUILayout.Space(10);
    }

    private void DrawSpecializedSection()
    {
        EditorGUILayout.LabelField("ESPECIFICACIONES DEL OBJETO", EditorStyles.boldLabel);

        ItemOrigin origin = (ItemOrigin)originProp.enumValueIndex;

        ItemCategory category = (ItemCategory)categoryProp.enumValueIndex;

        if (origin == ItemOrigin.Raw || category == ItemCategory.None)
        {
            string message = origin == ItemOrigin.Raw ? "Este objeto no necesita datos especializados." : "Este objeto puede tener datos especializados.";

            EditorGUILayout.HelpBox(message, MessageType.Info);

            return;
        }

        // comprueba que la especializacion coincida con la categoria
        bool validData = category switch
        {
            ItemCategory.Weapon => specializedDataProp.managedReferenceValue is WeaponData,

            ItemCategory.Consumable => specializedDataProp.managedReferenceValue is ConsumableData,

            ItemCategory.Placeable => specializedDataProp.managedReferenceValue is PlaceableData,

            ItemCategory.Trap => specializedDataProp.managedReferenceValue is TrapData,

            _ => false
        };

        if (!validData)
        {
            EditorGUILayout.HelpBox("La categoria y los datos especializados no coinciden. Cambia la categoria para regenerar la configuracion.", MessageType.Error);

            return;
        }

        string title = category switch
        {
            ItemCategory.Weapon => "ARMA",
            ItemCategory.Consumable => "CONSUMIBLE",
            ItemCategory.Placeable => "OBJETO COLOCABLE",
            ItemCategory.Trap => "TRAMPA",
            _ => "OBJETO"
        };

        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        DrawSpecializedProperties();
    }

    private void DrawSpecializedProperties()
    {
        SerializedProperty property = specializedDataProp.Copy();

        SerializedProperty endProperty = property.GetEndProperty();

        bool enterChildren = true;

        while (property.NextVisible(enterChildren) && !SerializedProperty.EqualContents(property, endProperty))
        {
            enterChildren = false;

            EditorGUILayout.PropertyField(property, new GUIContent(property.displayName), true);
        }
    }

    private void CreateSpecializedData()
    {
        ItemCategory category = (ItemCategory)categoryProp.enumValueIndex;

        switch (category)
        {
            case ItemCategory.None:
                specializedDataProp.managedReferenceValue = null;
                break;

            case ItemCategory.Weapon:
                specializedDataProp.managedReferenceValue = new WeaponData();
                break;

            case ItemCategory.Consumable:
                specializedDataProp.managedReferenceValue = new ConsumableData();
                break;

            case ItemCategory.Placeable:
                specializedDataProp.managedReferenceValue = new PlaceableData();
                break;

            case ItemCategory.Trap:
                specializedDataProp.managedReferenceValue = new TrapData();
                break;
        }
    }

    private void DrawIdentifierValidation()
    {
        EditorGUILayout.LabelField("VALIDACION DEL IDENTIFICADOR", EditorStyles.boldLabel);

        string id = itemIdProp.stringValue;

        if (string.IsNullOrWhiteSpace(id))
        {
            EditorGUILayout.HelpBox("El ID es obligatorio.", MessageType.Error);

            return;
        }

        if (!Regex.IsMatch(id, @"^[a-zA-Z0-9_]+$"))
        {
            EditorGUILayout.HelpBox("Usa solamente letras, numeros y guiones bajos. Evita espacios, tildes o simbolos.", MessageType.Warning);
        }

        ItemData current = (ItemData)target;

        string[] assetGuids = AssetDatabase.FindAssets("t:ItemData");

        foreach (string guid in assetGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            ItemData other = AssetDatabase.LoadAssetAtPath<ItemData>(path);

            if (other == null || other == current)
                continue;

            if (string.Equals(other.itemId, id, System.StringComparison.OrdinalIgnoreCase))
            {
                EditorGUILayout.HelpBox($"Este ID ya pertenece al objeto '{other.name}'.", MessageType.Error);

                return;
            }
        }

        if (Regex.IsMatch(id, @"^[a-zA-Z0-9_]+$"))
        {
            EditorGUILayout.HelpBox("ID correcto y sin duplicados.", MessageType.Info);
        }
    }

    private void DrawSeparator()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);

        rect.height = 1;

        EditorGUI.DrawRect(rect, new Color(0.35f, 0.35f, 0.35f, 1f));
    }
}