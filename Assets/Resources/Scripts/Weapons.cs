using System;
using System.Collections.Generic;

using UnityEngine;

public interface IWeapon
{
    string Name { get; }
    int APCost { get; }
    int Damage { get; }
    int MaxRange { get; }
    int Mark { get; }
    bool Silenced { get; }
    Dictionary<int, int> AccuracyChart { get; }

    int GetAccuracyAtRange(int range);
}

static class Accuracy
{
    public static Dictionary<int, int> GetAccuracyChart(IWeapon weapon)
    {
        var a = (95 - 66) / (weapon.Mark - 1);
        var b = (66 - 5) / (weapon.MaxRange - weapon.Mark);

        var chart = new Dictionary<int, int>();
        chart[1] = 95; // max accuracy is 95
        
        // curve a
        for (int i = 2; i <= weapon.Mark; i++)
            chart[i] = chart[i - 1] - a;

        // curve b
        for (int i = weapon.Mark + 1; i <= weapon.MaxRange; i++)
            chart[i] = chart[i - 1] - b;

        return chart;
    }

    public static bool AttemptHit(IWeapon weapon, int range, float accMod)
    {
        var accuracy = weapon.AccuracyChart[range] * accMod;
        var chance = UnityEngine.Random.Range(0, 100);
        return (chance <= accuracy);
    }
}

public class Pistol : IWeapon
{
    public string Name { get { return "Pistol"; } }
    public int APCost { get { return 3; } }
    public int Damage { get { return 1; } }
    public int MaxRange { get { return 8; } }
    public int Mark { get { return 4; } }
    public bool Silenced { get { return false; } }    
    public Dictionary<int, int> AccuracyChart { get; private set; }

    public Pistol()
    {
        AccuracyChart = Accuracy.GetAccuracyChart(this);
    }

    public int GetAccuracyAtRange(int range)
    {
        if (AccuracyChart.ContainsKey(range))
            return AccuracyChart[range];
        else
            return 0;
    }
}

public class SilencedPistol : IWeapon
{
    public string Name { get { return "Silenced Pistol"; } }
    public int APCost { get { return 3; } }
    public int Damage { get { return 1; } }
    public int MaxRange { get { return 8; } }
    public int Mark { get { return 5; } }
    public bool Silenced { get { return true; } }    
    public Dictionary<int, int> AccuracyChart { get; private set; }

    public SilencedPistol()
    {
        AccuracyChart = Accuracy.GetAccuracyChart(this);
    }

    public int GetAccuracyAtRange(int range)
    {
        if (AccuracyChart.ContainsKey(range))
            return AccuracyChart[range];
        else
            return 0;
    }
}

public class Rifle : IWeapon
{
    public string Name { get { return "Rifle"; } }
    public int APCost { get { return 2; } }
    public int Damage { get { return 2; } }
    public int MaxRange { get { return 14; } }
    public int Mark { get { return 7; } }
    public bool Silenced { get { return false; } }
    public Dictionary<int, int> AccuracyChart { get; private set; }

    public Rifle()
    {
        AccuracyChart = Accuracy.GetAccuracyChart(this);
    }

    public int GetAccuracyAtRange(int range)
    {
        if (AccuracyChart.ContainsKey(range))
            return AccuracyChart[range];
        else
            return 0;
    }
}
