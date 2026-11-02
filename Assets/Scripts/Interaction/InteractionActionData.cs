
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewInteractionAction",
    menuName = "Zombie Cabin/Interaction/Action"
)]
public class InteractionActionData : ScriptableObject
{
    [Header("Identificacion")]
    [SerializeField] private string actionId;

    [Header("Presentacion")]
    [SerializeField] private string displayName;
    [TextArea(2, 4)]
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    public string ActionId => actionId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
}
