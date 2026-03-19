using System;
using UnityEngine;

public class PartyCurrency : MonoBehaviour
{
    public static PartyCurrency Instance;

    [SerializeField] private int startingMoney = 100;
    [SerializeField] private bool persistAcrossScenes = true;

    public event Action<int> OnMoneyChanged;

    public int CurrentMoney { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CurrentMoney = Mathf.Max(0, startingMoney);

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);
    }

    public bool CanAfford(int amount)
    {
        return CurrentMoney >= Mathf.Max(0, amount);
    }

    public bool TrySpend(int amount)
    {
        amount = Mathf.Max(0, amount);
        if (CurrentMoney < amount)
            return false;

        CurrentMoney -= amount;
        OnMoneyChanged?.Invoke(CurrentMoney);
        return true;
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0)
            return;

        CurrentMoney += amount;
        OnMoneyChanged?.Invoke(CurrentMoney);
    }

    public void SetMoney(int amount)
    {
        CurrentMoney = Mathf.Max(0, amount);
        OnMoneyChanged?.Invoke(CurrentMoney);
    }
}
