using UnityEngine;

public class GridDebug : MonoBehaviour
{
    public int gridWidth = 20;
    public int gridHeight = 20;

    public float cellSize = 1f;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        for (int x = -gridWidth; x < gridWidth; x++)
        {
            for (int y = -gridHeight; y < gridHeight; y++)
            {
                Vector3 pos = new Vector3(
                    x + 0.5f,
                    y + 0.5f,
                    0
                );

                Gizmos.DrawSphere(pos, 0.05f);
            }
        }
    }
}