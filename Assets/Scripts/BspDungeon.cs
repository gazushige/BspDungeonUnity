using UnityEngine;
using UnityEngine.UI;

public class BspDungeon : MonoBehaviour
{
    public BSP_Asset bsp;
    public GameObject resultDisplay;
    public char[,] grid { private set; get; }

    void Start()
    {
        grid = new BspGenerator(bsp).grid;
        Display();
    }
    void Display()
    {
        string text = "";
        for (int y = 0; y < bsp.gridH; y++)
        {
            for (int x = 0; x < bsp.gridW; x++) text += grid[x, y];
            text += "\n";
        }
        resultDisplay.GetComponent<Text>().text = text;
    }
}