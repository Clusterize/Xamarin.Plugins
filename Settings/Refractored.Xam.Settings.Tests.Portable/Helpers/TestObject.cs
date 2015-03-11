using System;

namespace Refractored.Xam.Settings.Tests.Portable.Helpers
{
  public class TestObject
  {
    public string Name { get; set; }

    public double Value { get; set; }

    public override bool Equals (object obj)
    {
      var tObj = obj as TestObject;
      return tObj != null && tObj.Name == this.Name && tObj.Value == this.Value;
    }

    public override int GetHashCode ()
    {
      unchecked
      {
        var hash = 17;
        if (this.Name != null)
        {
          hash = hash * 23 + this.Name.GetHashCode();
        }
        hash = hash * 23 + this.Value.GetHashCode();
        return hash;
      }
    }
  }
}