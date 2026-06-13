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

    private struct RectAoe
    {
        public Vector2 pos;
        public Vector2 dir;
        public float   width;
        public float   length;
        public float   expireTime;
    }

    private struct ConeAoe
    {
        public Vector2 pos;
        public Vector2 dir;
        public float   halfAngleDeg;
        public float   radius;
        public float   expireTime;
    }

    private struct DamageText
    {
        public Vector2    worldPos;
        public string     text;
        public bool       isCrit;
        public DamageType damageType;
        public float      startTime;
        public float      expireTime;
        public float      extraYOffset;
    }

    private static GameUIManager _instance;
    private readonly List<AoeCircle>  _aoeCircles  = new();
    private readonly List<RectAoe>    _rectAoes    = new();
    private readonly List<ConeAoe>    _coneAoes    = new();
    private readonly List<DamageText> _damageTexts = new();

    private Texture2D _bgTex;
    private Texture2D _fillTex;
    private Material  _rangeMat;

    private GUIStyle _dmgStyle;
    private GUIStyle _critStyle;
    private GUIStyle _fireDmgStyle;
    private GUIStyle _fireCritStyle;
    private GUIStyle _energyDmgStyle;
    private GUIStyle _coldDmgStyle;
    private GUIStyle _lightningDmgStyle;
    private GUIStyle _poisonDmgStyle;

    private void Awake()
    {
        _instance = this;
        _bgTex    = MakeTex(Color.gray);
        _fillTex  = MakeTex(Color.green);

        _dmgStyle      = new GUIStyle();
        _critStyle     = new GUIStyle();
        _fireDmgStyle  = new GUIStyle();
        _fireCritStyle = new GUIStyle();
        _dmgStyle.normal.textColor      = Color.black;
        _critStyle.normal.textColor     = Color.red;
        _fireDmgStyle.normal.textColor  = new Color(1f, 0.45f, 0f);
        _fireCritStyle.normal.textColor = new Color(1f, 0.15f, 0f);
        _energyDmgStyle = new GUIStyle();
        _energyDmgStyle.normal.textColor = new Color(0.4f, 0.8f, 1f);
        _coldDmgStyle = new GUIStyle();
        _coldDmgStyle.normal.textColor = new Color(0.5f, 0.85f, 1f);
        _lightningDmgStyle = new GUIStyle();
        _lightningDmgStyle.normal.textColor = new Color(1f, 0.95f, 0.2f);
        _poisonDmgStyle = new GUIStyle();
        _poisonDmgStyle.normal.textColor = new Color(0.3f, 0.9f, 0.3f);

        var shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null)
        {
            Debug.LogWarning("[GameUIManager] Hidden/Internal-Colored 셰이더를 찾을 수 없음");
            return;
        }
        _rangeMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        _rangeMat.SetInt("_ZTest",    (int)UnityEngine.Rendering.CompareFunction.Always);
        _rangeMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _rangeMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
        Destroy(_bgTex);
        Destroy(_fillTex);
        if (_rangeMat != null) Destroy(_rangeMat);
    }

    public static void ShowDamage(Vector2 worldPos, float damage, bool isCrit,
                                   DamageType damageType = DamageType.Physical)
    {
        if (_instance == null) return;

        float yExtra = 0f;
        float now    = Time.time;
        foreach (var d in _instance._damageTexts)
        {
            if ((d.worldPos - worldPos).sqrMagnitude < 0.01f &&
                now - d.startTime < 0.1f)
                yExtra += 15f;
        }

        _instance._damageTexts.Add(new DamageText
        {
            worldPos     = worldPos,
            text         = Mathf.RoundToInt(damage).ToString(),
            isCrit       = isCrit,
            damageType   = damageType,
            startTime    = now,
            expireTime   = now + _instance.dmgDuration,
            extraYOffset = yExtra
        });
    }

    public static void ShowAoeHit(Vector2 pos, float radius, GameObject fxPrefab = null)
    {
        if (_instance == null || radius <= 0f) return;
        bool useAnim = SettingsSystem.AoeDisplayMode == AoeDisplayMode.Animation;
        if (useAnim && fxPrefab != null)
        {
            _instance.SpawnAoeFx(pos, radius, fxPrefab);
        }
        else
        {
            float now = Time.time;
            _instance._aoeCircles.Add(new AoeCircle
            {
                pos        = pos,
                radius     = radius,
                expireTime = now + _instance.aoeHitDuration
            });
        }
    }

    public static void ShowRectAoeHit(Vector2 pos, Vector2 dir, float width, float length,
        GameObject fxPrefab = null)
    {
        if (_instance == null) return;
        bool useAnim = SettingsSystem.AoeDisplayMode == AoeDisplayMode.Animation;
        if (useAnim && fxPrefab != null)
        {
            _instance.SpawnRectAoeFx(pos, dir.normalized, width, length, fxPrefab);
            return;
        }
        _instance._rectAoes.Add(new RectAoe
        {
            pos        = pos,
            dir        = dir.normalized,
            width      = width,
            length     = length,
            expireTime = Time.time + _instance.aoeHitDuration
        });
    }

    public static void ShowConeAoeHit(Vector2 pos, Vector2 dir, float halfAngleDeg, float radius,
        GameObject fxPrefab = null)
    {
        if (_instance == null) return;
        bool useAnim = SettingsSystem.AoeDisplayMode == AoeDisplayMode.Animation;
        if (useAnim && fxPrefab != null)
        {
            _instance.SpawnConeAoeFx(pos, dir.normalized, radius, fxPrefab);
            return;
        }
        _instance._coneAoes.Add(new ConeAoe
        {
            pos          = pos,
            dir          = dir.normalized,
            halfAngleDeg = halfAngleDeg,
            radius       = radius,
            expireTime   = Time.time + _instance.aoeHitDuration
        });
    }

    private void SpawnAoeFx(Vector2 pos, float radius, GameObject fxPrefab)
    {
        var go = Instantiate(fxPrefab, new Vector3(pos.x, pos.y, -1f), Quaternion.identity);
        float diameter = radius * 2f;

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            float nativeWidth = sr.sprite.bounds.size.x;
            float scale = nativeWidth > 0f ? diameter / nativeWidth : diameter;
            go.transform.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            go.transform.localScale = new Vector3(diameter, diameter, 1f);
        }

        Destroy(go, aoeHitDuration);
    }

    private void SpawnRectAoeFx(Vector2 origin, Vector2 forward, float width, float length,
        GameObject fxPrefab)
    {
        // 사각형 중앙에 배치 (origin → forward × length/2)
        Vector2 center = origin + forward * (length * 0.5f);
        float   angle  = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;

        var go = Instantiate(fxPrefab,
            new Vector3(center.x, center.y, -1f),
            Quaternion.Euler(0f, 0f, angle));

        // 스프라이트 네이티브 크기 보정 (PR #215 패턴 동일)
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            Vector2 native = sr.sprite.bounds.size;
            float scaleX = native.x > 0f ? length / native.x : length;
            float scaleY = native.y > 0f ? width  / native.y : width;
            go.transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }
        else
        {
            go.transform.localScale = new Vector3(length, width, 1f);
        }

        Destroy(go, aoeHitDuration);
    }

    private void SpawnConeAoeFx(Vector2 origin, Vector2 forward, float radius,
        GameObject fxPrefab)
    {
        // 원뿔 꼭짓점은 origin
        float angle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;

        var go = Instantiate(fxPrefab,
            new Vector3(origin.x, origin.y, -1f),
            Quaternion.Euler(0f, 0f, angle));

        float diameter = radius * 2f;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            float nativeWidth = sr.sprite.bounds.size.x;
            float scale = nativeWidth > 0f ? diameter / nativeWidth : diameter;
            go.transform.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            go.transform.localScale = new Vector3(diameter, diameter, 1f);
        }

        Destroy(go, aoeHitDuration);
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
            float sy = Screen.height - sp.y + dmgYOffset + d.extraYOffset - progress * dmgFloatSpeed;

            GUIStyle style = d.damageType switch
            {
                DamageType.Fire      => d.isCrit ? _fireCritStyle : _fireDmgStyle,
                DamageType.Energy    => _energyDmgStyle,
                DamageType.Cold      => _coldDmgStyle,
                DamageType.Lightning => _lightningDmgStyle,
                DamageType.Poison    => _poisonDmgStyle,
                _                    => d.isCrit ? _critStyle : _dmgStyle
            };
            GUI.Label(new Rect(sx, sy, 60f, 20f), d.text, style);
        }
    }

    private void OnRenderObject()
    {
        if (_rangeMat == null) return;

        _rangeMat.SetPass(0);
        GL.Begin(GL.LINES);
        DrawTowerRange();
        DrawAoeCircles();
        DrawRectAoes();
        DrawConeAoes();
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

        GL.Color(aoeHitColor);
        for (int i = _aoeCircles.Count - 1; i >= 0; i--)
        {
            var c = _aoeCircles[i];
            if (now >= c.expireTime) { _aoeCircles.RemoveAt(i); continue; }

            for (int s = 0; s < aoeSegments; s++)
            {
                float a0 = step * s;
                float a1 = step * (s + 1);
                GL.Vertex3(c.pos.x + Mathf.Cos(a0) * c.radius, c.pos.y + Mathf.Sin(a0) * c.radius, 0f);
                GL.Vertex3(c.pos.x + Mathf.Cos(a1) * c.radius, c.pos.y + Mathf.Sin(a1) * c.radius, 0f);
            }
        }
    }

    private void DrawRectAoes()
    {
        float now = Time.time;
        GL.Color(aoeHitColor);
        for (int i = _rectAoes.Count - 1; i >= 0; i--)
        {
            var c = _rectAoes[i];
            if (now >= c.expireTime) { _rectAoes.RemoveAt(i); continue; }

            Vector2 fwd   = c.dir;
            Vector2 right = new Vector2(-fwd.y, fwd.x);
            Vector2 halfW = right * (c.width * 0.5f);
            Vector2 fwdL  = fwd * c.length;

            Vector2 bl = c.pos - halfW;
            Vector2 br = c.pos + halfW;
            Vector2 fl = c.pos + fwdL - halfW;
            Vector2 fr = c.pos + fwdL + halfW;

            GL.Vertex3(bl.x, bl.y, 0f); GL.Vertex3(br.x, br.y, 0f);
            GL.Vertex3(br.x, br.y, 0f); GL.Vertex3(fr.x, fr.y, 0f);
            GL.Vertex3(fr.x, fr.y, 0f); GL.Vertex3(fl.x, fl.y, 0f);
            GL.Vertex3(fl.x, fl.y, 0f); GL.Vertex3(bl.x, bl.y, 0f);
        }
    }

    private void DrawConeAoes()
    {
        float now  = Time.time;
        const int arcSegs = 16;
        GL.Color(aoeHitColor);
        for (int i = _coneAoes.Count - 1; i >= 0; i--)
        {
            var c = _coneAoes[i];
            if (now >= c.expireTime) { _coneAoes.RemoveAt(i); continue; }

            Vector2 leftEdge  = AoeUtils.Rotate(c.dir, c.halfAngleDeg) * c.radius;
            Vector2 rightEdge = AoeUtils.Rotate(c.dir, -c.halfAngleDeg) * c.radius;

            GL.Vertex3(c.pos.x, c.pos.y, 0f);
            GL.Vertex3(c.pos.x + leftEdge.x, c.pos.y + leftEdge.y, 0f);
            GL.Vertex3(c.pos.x, c.pos.y, 0f);
            GL.Vertex3(c.pos.x + rightEdge.x, c.pos.y + rightEdge.y, 0f);

            float baseAngle  = Mathf.Atan2(c.dir.y, c.dir.x) * Mathf.Rad2Deg - c.halfAngleDeg;
            float step       = (c.halfAngleDeg * 2f) / arcSegs;
            for (int s = 0; s < arcSegs; s++)
            {
                float a0 = (baseAngle + step * s)       * Mathf.Deg2Rad;
                float a1 = (baseAngle + step * (s + 1)) * Mathf.Deg2Rad;
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
