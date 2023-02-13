using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObject/BSP_Asset")]
// [System.Serializable]
public class BSP_Asset : ScriptableObject
{
    public int gridW, gridH, minRoom, maxRoom, offset;
    public char wall = '■';
    public char room = '□';
    public char aisle = '#';
}
