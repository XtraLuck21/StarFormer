using UnityEngine;

[CreateAssetMenu(menuName = "StarFormer/Help Book Content")]
public sealed class HelpBookContent : ScriptableObject
{
    [SerializeField] private HelpBookUI.HelpPage[] pages;
    public HelpBookUI.HelpPage[] Pages => pages;
}
