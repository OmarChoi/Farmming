using System;
using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance;

    public int CurrentDay { get; private set; } = 1;
    public bool IsNight { get; private set; } = false;

    public event Action OnMorningStart;
    public event Action OnNightStart;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.N))
        {
            if(!IsNight)
            {
                StartNight();
            }
            else
            {
                StartMorning();
            }
        }
    }

    private void StartNight()
    {
        IsNight = true;
        Debug.Log($"{CurrentDay}일차 밤 시작");
        OnNightStart?.Invoke();
    }

    private void StartMorning()
    {
        IsNight = false;
        CurrentDay++;
        Debug.Log($"{CurrentDay}일차 아침 시작");
        OnMorningStart?.Invoke();
    }
}
