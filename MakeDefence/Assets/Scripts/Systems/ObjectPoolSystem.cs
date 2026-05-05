using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolSystem : MonoBehaviour
{
    public static ObjectPoolSystem Instance { get; private set; }

    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private int initialPoolSize = 30;

    private readonly Queue<Enemy> _pool = new();

    private void Awake()
    {
        Instance = this;
        for (int i = 0; i < initialPoolSize; i++)
            CreateEnemy();
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
}
