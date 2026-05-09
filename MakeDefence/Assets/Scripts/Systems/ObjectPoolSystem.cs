using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolSystem : MonoBehaviour
{
    public static ObjectPoolSystem Instance { get; private set; }

    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int initialPoolSize = 30;

    [SerializeField] private GameObject[] projectilePrefabs;

    private readonly Queue<Enemy> _pool = new();
    private readonly Dictionary<Type, Queue<ProjectileBase>> _projectilePools = new();
    private readonly Dictionary<Type, GameObject> _projectilePrefabMap = new();

    private void Awake()
    {
        Instance = this;
        for (int i = 0; i < initialPoolSize; i++)
            CreateEnemy();

        if (projectilePrefabs != null)
        {
            foreach (var prefab in projectilePrefabs)
            {
                if (prefab == null) continue;
                var comp = prefab.GetComponent<ProjectileBase>();
                if (comp != null)
                    _projectilePrefabMap[comp.GetType()] = prefab;
            }
        }
    }

    private void CreateEnemy()
    {
        var go = Instantiate(enemyPrefab, transform);
        go.SetActive(false);
        _pool.Enqueue(go.GetComponent<Enemy>());
    }

    public Enemy Get()
    {
        if (_pool.Count == 0) CreateEnemy();
        var enemy = _pool.Dequeue();
        enemy.gameObject.SetActive(true);
        return enemy;
    }

    public void Return(Enemy enemy)
    {
        enemy.gameObject.SetActive(false);
        _pool.Enqueue(enemy);
    }

    public T GetProjectile<T>() where T : ProjectileBase
    {
        var type = typeof(T);
        if (!_projectilePools.TryGetValue(type, out var pool))
        {
            pool = new Queue<ProjectileBase>();
            _projectilePools[type] = pool;
        }

        if (pool.Count == 0)
        {
            if (!_projectilePrefabMap.TryGetValue(type, out var prefab))
            {
                Debug.LogError($"[ObjectPoolSystem] 프리팹 미등록: {type.Name}");
                return null;
            }
            var go = Instantiate(prefab, transform);
            go.SetActive(false);
            pool.Enqueue(go.GetComponent<T>());
        }

        var proj = (T)pool.Dequeue();
        proj.gameObject.SetActive(true);
        return proj;
    }

    public void ReturnProjectile(ProjectileBase proj)
    {
        proj.gameObject.SetActive(false);
        var type = proj.GetType();
        if (!_projectilePools.TryGetValue(type, out var pool))
        {
            pool = new Queue<ProjectileBase>();
            _projectilePools[type] = pool;
        }
        pool.Enqueue(proj);
    }
}
