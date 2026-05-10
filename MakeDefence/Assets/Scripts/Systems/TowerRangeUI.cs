using UnityEngine;

public class TowerRangeUI : MonoBehaviour
{
    [SerializeField] private Color lineColor = new Color(1f, 1f, 0f, 0.8f);
    [SerializeField] private int   segments  = 64;

    private Material _mat;

    private void Awake()
    {
        var shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null)
        {
            Debug.LogWarning("[TowerRangeUI] Hidden/Internal-Colored 셰이더를 찾을 수 없음");
            return;
        }
        _mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        _mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
    }

    private void OnDestroy()
    {
        if (_mat != null) Destroy(_mat);
    }

    private void OnRenderObject()
    {
        if (_mat == null) return;
        var tower = InventorySystem.Instance?.SelectedTower;
        if (tower == null) return;

        _mat.SetPass(0);

        Vector3 center = tower.transform.position;
        float   radius = tower.AttackRange;

        GL.Begin(GL.LINES);
        GL.Color(lineColor);

        float step = 2f * Mathf.PI / segments;
        for (int i = 0; i < segments; i++)
        {
            float a0 = step * i;
            float a1 = step * (i + 1);
            GL.Vertex3(center.x + Mathf.Cos(a0) * radius, center.y + Mathf.Sin(a0) * radius, center.z);
            GL.Vertex3(center.x + Mathf.Cos(a1) * radius, center.y + Mathf.Sin(a1) * radius, center.z);
        }

        GL.End();
    }
}
