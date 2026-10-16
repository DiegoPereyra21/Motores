using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Menu del editor para armar la radio de la cabaña sin hacer todo a mano.
// Zombie Cabin > Radio > Crear y colocar en la escena:
//   1) crea el prefab Assets/Prefabs/Radio.prefab (modelo placeholder hecho con primitivas + collider en capa Interact
//      + RadioInteractable con los 3 mensajes del guion ya cargados)
//   2) crea el prefab Assets/Prefabs/UI/UIRadio.prefab (UIDocument + RadioMessageUI)
//   3) los instancia en la escena abierta, cerca del Player, y conecta las referencias
// Despues hay que mover la radio a su lugar dentro de la cabaña. Si ya existen los prefabs no los pisa.
public static class RadioSetup
{
    private const string RadioPrefabPath = "Assets/Prefabs/Radio.prefab";
    private const string UIPrefabPath = "Assets/Prefabs/UI/UIRadio.prefab";
    private const string MaterialsFolder = "Assets/Materials/Radio";
    private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

    [MenuItem("Zombie Cabin/Radio/Crear y colocar en la escena")]
    public static void CreateAndPlaceRadio()
    {
        // si la escena ya tiene una radio no creamos otra
        RadioInteractable existing = Object.FindFirstObjectByType<RadioInteractable>();

        if (existing != null)
        {
            Debug.LogWarning("[RadioSetup] La escena ya tiene una radio, se selecciona esa.", existing);
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        GameObject radioPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RadioPrefabPath) ?? BuildRadioPrefab();
        GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UIPrefabPath) ?? BuildUIPrefab();

        if (radioPrefab == null || uiPrefab == null)
            return;

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();

        GameObject radio = (GameObject)PrefabUtility.InstantiatePrefab(radioPrefab);
        GameObject ui = (GameObject)PrefabUtility.InstantiatePrefab(uiPrefab);
        Undo.RegisterCreatedObjectUndo(radio, "Crear radio");
        Undo.RegisterCreatedObjectUndo(ui, "Crear UI de radio");

        // la deja 1.2 m delante del player, mirandolo. Despues hay que ubicarla bien en la cabaña
        if (player != null)
        {
            radio.transform.position = player.transform.position + player.transform.forward * 1.2f;
            radio.transform.rotation = Quaternion.LookRotation(-player.transform.forward, Vector3.up);
        }
        else if (SceneView.lastActiveSceneView != null)
        {
            radio.transform.position = SceneView.lastActiveSceneView.pivot;
        }

        // conecta las referencias de la escena
        SerializedObject radioSO = new SerializedObject(radio.GetComponent<RadioInteractable>());
        radioSO.FindProperty("messageUI").objectReferenceValue = ui.GetComponent<RadioMessageUI>();
        radioSO.FindProperty("playerController").objectReferenceValue = player;
        radioSO.FindProperty("cameraPivot").objectReferenceValue = Object.FindFirstObjectByType<CameraPivot>();
        radioSO.FindProperty("playerAttack").objectReferenceValue = Object.FindFirstObjectByType<PlayerAttack>();
        radioSO.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(radio.scene);
        Selection.activeGameObject = radio;

        Debug.Log("[RadioSetup] Radio creada. Movela a su lugar en la cabaña (sobre una mesa, por ejemplo) y poné al Player a su lado. " +
                  "Si el Player no aparecio en la escena, asigna a mano Player Controller, Camera Pivot y Player Attack en RadioInteractable.", radio);
    }

    // ---------- prefab de la radio ----------

    private static GameObject BuildRadioPrefab()
    {
        int interactLayer = LayerMask.NameToLayer("Interact");

        if (interactLayer < 0)
        {
            Debug.LogError("[RadioSetup] No existe la capa 'Interact' en Project Settings > Tags and Layers.");
            return null;
        }

        EnsureFolder("Assets/Prefabs");
        EnsureFolder(MaterialsFolder);

        Material body = GetOrCreateMaterial("Radio_Body", new Color(0.18f, 0.22f, 0.16f));
        Material dark = GetOrCreateMaterial("Radio_Dark", new Color(0.07f, 0.07f, 0.07f));
        Material dial = GetOrCreateMaterial("Radio_Dial", new Color(0.9f, 0.6f, 0.2f));
        Material metal = GetOrCreateMaterial("Radio_Metal", new Color(0.5f, 0.5f, 0.5f));

        GameObject root = new GameObject("Radio");

        // modelo placeholder: el origen queda en la base de la radio, el frente mira a +Z
        AddPart(root, PrimitiveType.Cube, "Cuerpo", new Vector3(0f, 0.12f, 0f), Vector3.zero, new Vector3(0.40f, 0.24f, 0.14f), body);
        AddPart(root, PrimitiveType.Cube, "Parlante", new Vector3(-0.08f, 0.12f, 0.075f), Vector3.zero, new Vector3(0.16f, 0.16f, 0.01f), dark);
        AddPart(root, PrimitiveType.Cube, "Dial", new Vector3(0.10f, 0.18f, 0.075f), Vector3.zero, new Vector3(0.12f, 0.05f, 0.01f), dial);
        AddPart(root, PrimitiveType.Cylinder, "Perilla1", new Vector3(0.07f, 0.09f, 0.078f), new Vector3(90f, 0f, 0f), new Vector3(0.04f, 0.01f, 0.04f), metal);
        AddPart(root, PrimitiveType.Cylinder, "Perilla2", new Vector3(0.14f, 0.09f, 0.078f), new Vector3(90f, 0f, 0f), new Vector3(0.04f, 0.01f, 0.04f), metal);
        AddPart(root, PrimitiveType.Cylinder, "Antena", new Vector3(0.15f, 0.38f, -0.03f), new Vector3(0f, 0f, -15f), new Vector3(0.008f, 0.15f, 0.008f), metal);

        // un solo collider para todo, un poco mas grande que la radio para que sea facil apuntarle con el raycast
        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0.05f, 0.22f, 0f);
        box.size = new Vector3(0.5f, 0.45f, 0.25f);

        // PlayerInteraction solo detecta la capa Interact
        SetLayerRecursively(root, interactLayer);

        RadioInteractable radio = root.AddComponent<RadioInteractable>();
        FillDefaultMessages(new SerializedObject(radio));

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, RadioPrefabPath);
        Object.DestroyImmediate(root);

        Debug.Log("[RadioSetup] Prefab creado: " + RadioPrefabPath);
        return prefab;
    }

    // carga en el componente los 3 mensajes redactados en el guion (Google Doc, MODEV1-30)
    private static void FillDefaultMessages(SerializedObject so)
    {
        string[][] lines =
        {
            new[]
            {
                "Este es un mensaje del Servicio de Radiodifusión de Emergencia.",
                "Informamos a la población que se ha declarado el toque de queda debido a la ola masiva de ataques en las calles.",
                "Se recomienda permanecer en sus hogares y contar con algún método de defensa.",
                "No intenten aventurarse al exterior y aguarden instrucciones oficiales."
            },
            new[]
            {
                "Se ha verificado que los atacantes pueden ser combatidos con armas contundentes reforzadas con metal.",
                "Además, recuerden hacer uso de los remedios naturales y comidas nutritivas para resistir posibles ataques."
            },
            new[]
            {
                "Confirmamos que se están efectuando sobrevuelos de rescate sobre el área.",
                "Se recomienda contar con medios para señalizar posibles aeronaves para ser llevado a los centros de supervivientes."
            }
        };

        string[] hints =
        {
            "Usa la mesa de trabajo para crear mejoras de arma, medicinas y barricadas para la cabaña.",
            "Nuevas recetas en la mesa de trabajo disponibles",
            "Construye la BENGALA IMPROVISADA para el rescate."
        };

        SerializedProperty messages = so.FindProperty("messages");
        messages.arraySize = lines.Length;

        for (int i = 0; i < lines.Length; i++)
        {
            SerializedProperty message = messages.GetArrayElementAtIndex(i);
            SerializedProperty subtitleLines = message.FindPropertyRelative("subtitleLines");
            subtitleLines.arraySize = lines[i].Length;

            for (int j = 0; j < lines[i].Length; j++)
                subtitleLines.GetArrayElementAtIndex(j).stringValue = lines[i][j];

            message.FindPropertyRelative("objectiveHint").stringValue = hints[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- prefab de la UI ----------

    private static GameObject BuildUIPrefab()
    {
        VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/RadioMessage.uxml");
        PanelSettings panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/PanelSettings.asset");

        if (uxml == null || panelSettings == null)
        {
            Debug.LogError("[RadioSetup] Falta Assets/UI/RadioMessage.uxml o Assets/UI/PanelSettings.asset (esperá a que Unity termine de importar y probá de nuevo).");
            return null;
        }

        // la accion Interact (E) del Input System, la misma que usa PlayerInteraction
        InputActionReference interactRef = AssetDatabase.LoadAllAssetsAtPath(InputActionsPath)
            .OfType<InputActionReference>()
            .FirstOrDefault(r => r.action != null && r.action.name == "Interact" && r.action.actionMap.name == "Player");

        if (interactRef == null)
            Debug.LogWarning("[RadioSetup] No se encontro la accion Player/Interact, asignala a mano en RadioMessageUI > Skip Action.");

        GameObject root = new GameObject("UIRadio");

        UIDocument document = root.AddComponent<UIDocument>();
        document.visualTreeAsset = uxml;
        document.panelSettings = panelSettings;
        document.sortingOrder = 20f; // el resto de las UI estan en 0, asi la radio queda por encima de todo

        RadioMessageUI radioUI = root.AddComponent<RadioMessageUI>();
        SerializedObject so = new SerializedObject(radioUI);
        so.FindProperty("skipAction").objectReferenceValue = interactRef;
        so.ApplyModifiedPropertiesWithoutUndo();

        EnsureFolder("Assets/Prefabs/UI");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, UIPrefabPath);
        Object.DestroyImmediate(root);

        Debug.Log("[RadioSetup] Prefab creado: " + UIPrefabPath);
        return prefab;
    }

    // ---------- helpers ----------

    private static void AddPart(GameObject parent, PrimitiveType type, string partName, Vector3 localPos, Vector3 localEuler, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = partName;
        part.transform.SetParent(parent.transform, false);
        part.transform.localPosition = localPos;
        part.transform.localEulerAngles = localEuler;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;

        // el collider lo tiene el objeto raiz, los de las partes sobran
        Object.DestroyImmediate(part.GetComponent<Collider>());
    }

    private static Material GetOrCreateMaterial(string materialName, Color color)
    {
        string path = $"{MaterialsFolder}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material != null)
            return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        material = new Material(shader);
        material.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    // crea la carpeta (y las de arriba) si no existe
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
