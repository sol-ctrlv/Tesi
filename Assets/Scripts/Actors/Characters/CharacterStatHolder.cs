using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
public enum Stats
{
    str,
    dex,
    cos,
    wis,
    intellect,
    cha
}
public class CharacterStatHolder : MonoBehaviour
{
    public Action<Stats,float> OnChangedStat;

    [Header("BASE VALUES")]
    public float StatIncrementValue = 0.1f;
    public float Base_AtkSpeedMultiplier;
    public float Base_AtkDamageMultiplier;

    [Header("RawStats")]
    public float[] rawStats;
    
    public float atkRadius;

    [Header("CalculatedStats")]
    public float AtkSpeedMultiplier;
    public float AtkDamageMultiplier;
    public void InitStat(CharacterSO ch)
    {
        rawStats = new float[6];
        rawStats[(int)Stats.str] = ch.str;
        rawStats[(int)Stats.dex] = ch.dex;
        rawStats[(int)Stats.cos] = ch.cos;
        rawStats[(int)Stats.wis] = ch.wis;
        rawStats[(int)Stats.intellect] = ch.intellect;
        rawStats[(int)Stats.cha] = ch.cha;
        atkRadius = ch.BaseAttackRadius;


        AtkSpeedMultiplier = StatMulti(Stats.dex) * Base_AtkSpeedMultiplier;
        AtkDamageMultiplier = StatMulti(Stats.str) * Base_AtkDamageMultiplier;

    }
    public float StatMulti(Stats stat)
    {
        return (1f + rawStats[(int)stat] * StatIncrementValue);
    }

    public void StatModify(Stats stat, float amount)
    {
        rawStats[(int)stat] = Mathf.Max(0.01f, rawStats[(int)stat]-amount);
        OnChangedStat.Invoke(stat, amount);
    }
}
