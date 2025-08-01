using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ResourceManager : MonoBehaviour
{
    [Header("Starting Resources")]
    [SerializeField] private int startingCoins = 100;

    [Header("Events")]
    public UnityEvent<int> OnCoinsChanged;

    public static ResourceManager Instance;

    private int currentCoins;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            currentCoins = startingCoins;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Notify UI of initial coin amount
        OnCoinsChanged?.Invoke(currentCoins);
    }

    public int GetCoins()
    {
        return currentCoins;
    }

    public bool CanAfford(int cost)
    {
        return currentCoins >= cost;
    }

    public bool SpendCoins(int amount)
    {
        if (currentCoins >= amount)
        {
            currentCoins -= amount;
            OnCoinsChanged?.Invoke(currentCoins);
            Debug.Log($"Spent {amount} coins. Remaining: {currentCoins}");
            return true;
        }
        else
        {
            Debug.Log($"Cannot afford {amount} coins. Have: {currentCoins}");
            return false;
        }
    }

    public void AddCoins(int amount)
    {
        currentCoins += amount;
        OnCoinsChanged?.Invoke(currentCoins);
        Debug.Log($"Gained {amount} coins. Total: {currentCoins}");
    }

    // Method for buildings to add periodic income
    public void AddPeriodicIncome(int amount)
    {
        AddCoins(amount);
    }
}