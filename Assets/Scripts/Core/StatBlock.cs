using System;
using UnityEngine;

[Serializable]
public class StatBlock
{
    [Header("Core")]
    public int hp;
    public int atk;
    public int def;
    public int ap;

    [Header("Combat")]
    [Range(0f, 100f)]
    public float crit;

    public StatBlock Clone()
    {
        return new StatBlock
        {
            hp = hp,
            atk = atk,
            def = def,
            ap = ap,
            crit = crit
        };
    }

    public static StatBlock Add(StatBlock a, StatBlock b)
    {
        return new StatBlock
        {
            hp = a.hp + b.hp,
            atk = a.atk + b.atk,
            def = a.def + b.def,
            ap = a.ap + b.ap,
            crit = a.crit + b.crit
        };
    }

    public void ClampAsFinalStats()
    {
        hp = Mathf.Max(1, hp);
        atk = Mathf.Max(0, atk);
        def = Mathf.Max(0, def);
        ap = Mathf.Max(0, ap);
        crit = Mathf.Clamp(crit, 0f, 100f);
    }
}