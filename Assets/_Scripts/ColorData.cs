using System.Collections.Generic;

public class ColorData : PersistentSingleton<ColorData>
{
    public List<string> allColors = new List<string>();
    public List<string> activeColors = new List<string>();

    protected override void Awake()
    {
        base.Awake();

        allColors.Add("Red");
        allColors.Add("Green");
        allColors.Add("Blue");
        allColors.Add("Yellow");
        allColors.Add("Magenta");
        allColors.Add("Cyan");

        activeColors.AddRange(allColors);
    }
}
