using UnityEngine;

[CreateAssetMenu(menuName = "StarFormer/Collection Book Content")]
public sealed class HelpBookContent : ScriptableObject
{
    [SerializeField] private HelpBookUI.HelpPage[] pages;
    public HelpBookUI.HelpPage[] Pages => pages;
}
