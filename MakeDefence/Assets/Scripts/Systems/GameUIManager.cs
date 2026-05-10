using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    [Header("Enemy HP Bar")]
    [SerializeField] private float barWidth  = 40f;
    [SerializeField] private float barHeight = 5f;
    [SerializeField] private float yOffset   = 20f;

    [Header("Tower Range")]
    [SerializeField] private Color rangeColor    = new Color(1f, 1f, 0f, 0.8f);
    [SerializeField] private int   rangeSegments = 64;

    private Texture2D _bgTex;
    private Texture2D _fillTex;
    private Material  _rangeMat;

    private void Awake()
    {
        _bgTex   = MakeTex(Color.gray);
        _fillTex = MakeTex(Color.green);

        var shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null)
        {
            Debug.LogWarning("[GameUIManager] Hidden/Internal-Colored 셰이더를 찾을 수 없음");
            return;
        }
        _rangeMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        _rangeMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
    }

    private void OnDestroy()
    {
        Destroy(_bgTex);
        Destroy(_fillTex);
        if (_rangeMat != null) Destroy(_rangeMat);
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        var cam = Camera.main;
        if (cam == null) return;

        var enemies = Enemy.ActiveEnemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null || e.MaxHp <= 0f) continue;

            Vector3 screenPos = cam.WorldToScreenPoint(e.transform.position);
            if (screenPos.z < 0f) continue;

            float x    = screenPos.x - barWidth * 0.5f;
            float y    = Screen.height - screenPos.y - yOffset - barHeight;
            float fill = Mathf.Clamp01(e.CurrentHp / e.MaxHp);

            GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), _bgTex);
            GUI.DrawTexture(new Rect(x, y, barWidth * fill, barHeight), _fillTex);
        }
    }

    private void OnRenderObject()
    {
        if (_rangeMat == null) return;
        var tower = InventorySystem.Instance?.SelectedTower;
        if (tower == null) return;

        _rangeMat.SetPass(0);

        Vector3 center = tower.transform.position;
        float   radius = tower.AttackRange;
        float   step   = 2f * Mathf.PI / rangeSegments;

        GL.Begin(GL.LINES);
        GL.Color(rangeColor);

        for (int i = 0; i < rangeSegments; i++)
        {
            float a0 = step * i;
            float a1 = step * (i + 1);
            GL.Vertex3(center.x + Mathf.Cos(a0) * radius, center.y + Mathf.Sin(a0) * radius, center.z);
            GL.Vertex3(center.x + Mathf.Cos(a1) * radius, center.y + Mathf.Sin(a1) * radius, center.z);
        }

        GL.End();
    }

    private static Texture2D MakeTex(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
}
