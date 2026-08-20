namespace Case1_FitTheShape.Scripts
{
    public enum ShapeType
    {
        Yellow,
        Purple,
        Green,
        Blue,
        Red
    }
    
    [System.Serializable]
    public class Shape
    {
        public ShapeType Type;
    }
}