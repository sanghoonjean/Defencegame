using System.Collections.Generic;
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

    [Header("Skill AoE Hit")]
    [SerializeField] private Color aoeHitColor    = new Color(1f, 0.4f, 0f, 0.9f);
    [SerializeField] private int   aoeSegments    = 48;
    [SerializeField] private float aoeHitDuration = 0.5f;

    [Header("Damage Text")]
    [SerializeField] private float dmgFloatSpeed  = 30f;
    [SerializeField] private float dmgDuration    = 1.2f;
    [SerializeField] private float dmgXOffset     = -20f;
    [SerializeField] private float dmgYOffset     = 10f;

    private struct AoeCircle
    {
        public Vector2 pos;
        public float   radius;
        public float   expireTime;
    }

    private struct DamageText
    {
        public Vector2 worldPos;
        public string  text;
        public bool    isCrit;
        public float   startTime;
        public float   expireTime;
    }

    private static GameUIManager _instance;
    private readonly List<AoeCircle>   _aoeCircles   = new();
    private readonly List<DamageText>  _damageTexts  = new();

    private Texture2D _bgTex;
    private Texture2D _fillTex;
    private Material  _rangeMat;

    private GUIStyle _dmgStyle;
    private GUIStyle _critStyle;

    private void Awake()
    {
        _instance = this;
        _bgTex    = MakeTex(Color.gray);
        _fillTex  = MakeTex(Color.green);

        _dmgStyle  = new GUIStyle();
        _critStyle = new GUIStyle();
        _dmgStyle.normal.textColor  = Color.black;
        _critStyle.normal.textColor = Color.red;

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
        if (_instance == this) _instance = null;
        Destroy(_bgTex);
        Destroy(_fillTex);
        if (_rangeMat != null) Destroy(_rangeMat);
    }

    public static void ShowDamage(Vector2 worldPos, float damage, bool isCrit)
    {
        if (_instance == null) return;
        _instance._damageTexts.Add(new DamageText
        {
            worldPos   = worldPos,
            text       = Mathf.RoundToInt(damage).ToString(),
            isCrit     = isCrit,
            startTime  = Time.time,
            expireTime = Time.time + _instance.dmgDuration
        });
    }

    public static void ShowAoeHit(Vector2 pos, float radius)
    {
        if (_instance == null || radius <= 0f) return;
        _instance._aoeCircles.Add(new AoeCircle
        {
            pos        = pos,
            radius     = radius,
            expireTime = Time.time + _instance.aoeHitDuration
        });
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

        DrawDamageTexts(cam);
    }

    private void DrawDamageTexts(Camera cam)
    {
        float now = Time.time;
        for (int i = _damageTexts.Count - 1; i >= 0; i--)
        {
            var d = _damageTexts[i];
            if (now >= d.expireTime) { _damageTexts.RemoveAt(i); continue; }

            float progress  = (now - d.startTime) / dmgDuration;
            Vector3 sp      = cam.WorldToScreenPoint(d.worldPos);
            if (sp.z < 0f) continue;

            float sx = sp.x + dmgXOffset;
            float sy = Screen.height - sp.y + dmgYOffset - progress * dmgFloatSpeed;

            GUI.Label(new Rect(sx, sy, 60f, 20f), d.text, d.isCrit ? _critStyle : _dmgStyle);
        }
    }

    private void OnRenderObject()
    {
        if (_rangeMat == null) return;

        _rangeMat.SetPass(0);
        GL.Begin(GL.LINES);

        DrawTowerRange();
        DrawAoeCircles();

        GL.End();
    }

    private void DrawTowerRange()
    {
        var tower = InventorySystem.Instance?.SelectedTower;
        if (tower == null) return;

        GL.Color(rangeColor);

        Vector3 center = tower.transform.position;
        float   radius = tower.AttackRange;
        float   step   = 2f * Mathf.PI / rangeSegments;

        for (int i = 0; i < rangeSegments; i++)
        {
            float a0 = step * i;
            float a1 = step * (i + 1);
            GL.Vertex3(center.x + Mathf.Cos(a0) * radius, center.y + Mathf.Sin(a0) * radius, center.z);
            GL.Vertex3(center.x + Mathf.Cos(a1) * radius, center.y + Mathf.Sin(a1) * radius, center.z);
        }
    }

    private void DrawAoeCircles()
    {
        float now  = Time.time;
        float step = 2f * Mathf.PI / aoeSegments;

        for (int i = _aoeCircles.Count - 1; i >= 0; i--)
        {
            var c = _aoeCircles[i];
            if (now >= c.expireTime) { _aoeCircles.RemoveAt(i); continue; }

            GL.Color(aoeHitColor);
            for (int s = 0; s < aoeSegments; s++)
            {
                float a0 = step * s;
                float a1 = step * (s + 1);
                GL.Vertex3(c.pos.x + Mathf.Cos(a0) * c.radius, c.pos.y + Mathf.Sin(a0) * c.radius, 0f);
                GL.Vertex3(c.pos.x + Mathf.Cos(a1) * c.radius, c.pos.y + Mathf.Sin(a1) * c.radius, 0f);
            }
        }
    }

    private static Texture2D MakeTex(Color color)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
}
